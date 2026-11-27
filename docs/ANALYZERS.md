# Tool.Analyzers

`Tool.Analyzers` is a custom Roslyn diagnostic analyzer suite that audits C# `struct` memory layout and passing/copying/returning semantics against the x64 CLR **16-byte struct guideline**.

It is automatically attached to every project in the solution via `Directory.Build.props` and complements `ZeroAlloc.Analyzers` and the built-in .NET analyzers.

---

## Design Principles

1. **Project-agnostic:** The analyzer has zero knowledge of the engine or any third-party libraries (`Vortice`, `SDL3-CS`, `ImGui.NET`, etc.).
2. **Strict advisory mode (no intent heuristics):** The analyzer does not attempt to guess whether a `ref` parameter, `ref` local, or `ref` return is intentionally mutating a small struct. If a struct is smaller than 16 bytes and uses `in`, `ref`, or `ref readonly`, it emits a warning as advice. Intentional exceptions can be suppressed locally.
3. **No guessing on memory layout:** If the size or layout of a struct cannot be determined with certainty (for example, a stripped reference-assembly stub or a runtime-dependent SIMD type), the analyzer emits `TOOL009` instead of producing a potentially wrong size estimate.

---

## The 16-Byte Size Rule

Based on the Microsoft .NET design and high-performance C# guidelines on x64 (`IntPtr.Size == 8`):

| Estimated Size | Policy | Rationale |
|---|---|---|
| **< 16 bytes** | Pass, copy, and return **by value** | Fits in 1–2 CPU registers; copying by value avoids pointer indirection and aliasing overhead. |
| **== 16 bytes** | **Neutral boundary** (no warnings) | Borderline size (2 machine words / 1 XMM register, e.g., `Vector4`, `Color4`, `Quaternion`, `Guid`, `Decimal`); either style is acceptable. |
| **> 16 bytes** | Pass by `in` / `ref readonly`, avoid copies | Copying exceeds 2 machine words; passing or returning a managed reference avoids unnecessary stack copies. |

> **Tip:** Pair `in` / `ref readonly` on large structs with `readonly struct` (or `readonly` members) so the C# compiler does not emit hidden defensive copies.

---

## Diagnostic Rules

| ID | Category | Trigger | Guidance |
|---|---|---|---|
| `TOOL001` | Performance | Parameter of struct `> 16` bytes passed by value | Pass using `in` or `ref readonly` |
| `TOOL002` | Performance | Parameter of struct `< 16` bytes passed by `in`, `ref`, or `ref readonly` | Pass by value |
| `TOOL003` | Performance | Sequential unmanaged struct has avoidable padding | Reorder fields from largest to smallest alignment |
| `TOOL004` | Performance | Local variable copies struct `> 16` bytes from a ref-addressable source | Use `ref readonly var` |
| `TOOL005` | Performance | `foreach` loop copies struct `> 16` bytes per iteration | Use `ref readonly var` (or `array.AsSpan()`) |
| `TOOL006` | Performance | Local or `foreach` variable of struct `< 16` bytes uses `ref` or `ref readonly` | Copy by value |
| `TOOL007` | Performance | Method, property, indexer, or local function returns struct `> 16` bytes by value from ref-returnable storage | Return `ref readonly` |
| `TOOL008` | Performance | Member, delegate, or local function returns struct `< 16` bytes by `ref` or `ref readonly` | Return by value |
| `TOOL009` | Performance | Struct layout could not be determined | Inspect the reported unknown type and update `StructLayoutCalculator` if it is a stripped BCL type |

All rules default to `Warning` severity.

---

## Rule Details & Examples

### Parameters (`TOOL001`, `TOOL002`)

**Analyzer:** `StructParameterAnalyzer`

Covers:
- Regular, `virtual`, `abstract`, and `interface` methods
- Constructors and primary constructors (`TOOL002` always; `TOOL001` skips primary constructors due to `CS8908`/`CS9109`)
- Classic `this` extension methods and C# 14 `extension(...)` block receivers (unnamed static-only `extension(MyStruct)` blocks skip `TOOL001`)
- `delegate` declarations
- Local functions

Applies to user structs, closed generic structs, value tuples (`ValueTuple<...>`), and `Nullable<T>` (`T?`). Built-in primitives (`int`, `float`, `bool`, enums) and `ref struct` types are excluded.

Automatically skipped where C# syntax or semantics forbid changing the modifier:
- `out` and `params` parameters
- `override` methods, explicit/implicit interface implementations, `extern`, `async` methods, and iterator methods containing `yield` (`TOOL001`)
- Indexer parameters, property `set`/`init` accessors, and user-defined operators
- Partial method implementations (reported once on the defining declaration)

```csharp
// Bad
void Draw(Matrix4x4 transform) { }        // TOOL001 (~64 bytes)
void SetPos(in Vector3 position) { }      // TOOL002 (~12 bytes)

// Good
void Draw(in Matrix4x4 transform) { }
void SetPos(Vector3 position) { }
```

---

### Struct Padding (`TOOL003`)

**Analyzer:** `StructPaddingAnalyzer`

Checks non-generic, unmanaged source structs (no GC reference fields) that do not declare explicit layout control (`[StructLayout]`, `[ExtendedLayout]`, `[FieldOffset]`, or `[InlineArray]`).

```csharp
// Bad — 16 bytes due to alignment padding (TOOL003: 16 -> 12 bytes, saves 4 bytes)
public struct Example
{
    public byte FlagA;
    public int Value;
    public byte FlagB;
}

// Good — 8 bytes
public struct Example
{
    public int Value;
    public byte FlagA;
    public byte FlagB;
}
```

Structs containing GC references or generic type parameters (including structs nested inside generic types) are skipped because CoreCLR automatically lays them out with `LayoutKind.Auto`.

---

### Local Variables & `foreach` Loops (`TOOL004`, `TOOL005`, `TOOL006`)

**Analyzer:** `StructLocalCopyAnalyzer`

- **`TOOL004`** triggers when a large struct (`> 16` bytes) is copied by value into a local variable from a ref-addressable source (class/static field, array element, `[InlineArray]` element, or `ref`/`ref readonly`-returning method/property). Non-addressable rvalues (e.g., `new LargeStruct()` or by-value method calls), `using var`, and `async` contexts are skipped.
- **`TOOL005`** triggers when a `foreach` loop copies a large struct (`> 16` bytes) by value over a 1D array (suggesting `array.AsSpan()`), `Span<T>`, `ReadOnlySpan<T>`, `[InlineArray]`, or any collection whose enumerator `Current` returns by `ref`/`ref readonly`. `await foreach` and `async` contexts are skipped.
- **`TOOL006`** triggers whenever a local variable or `foreach` loop variable of a small struct (`< 16` bytes) is declared with `ref` or `ref readonly`.

```csharp
// Bad
var state = _states[i];                            // TOOL004 (if LargeStruct > 16 bytes)
foreach (var item in largeArray) { }               // TOOL005
foreach (ref readonly var v in smallSpan) { }      // TOOL006 (if SmallStruct < 16 bytes)

// Good
ref readonly var state = ref _states[i];
foreach (ref readonly var item in largeArray.AsSpan()) { }
foreach (var v in smallSpan) { }
```

---

### Return Values & Getters (`TOOL007`, `TOOL008`)

**Analyzer:** `StructReturnAnalyzer`

- **`TOOL007`** checks non-virtual, non-override, non-async, non-iterator methods, get-only properties/indexers, and local functions that return a large struct (`> 16` bytes) by value. It reports only when **every** `return` path points to legally ref-returnable storage (class/static fields, array elements, `[InlineArray]` elements, non-`scoped` ref/in parameters, or ref-returning calls/properties).
- **`TOOL008`** checks methods, properties, indexers, delegates, and local functions returning a small struct (`< 16` bytes) by `ref` or `ref readonly`.

```csharp
// Bad
public Matrix4x4 WorldMatrix => _worldMatrix;      // TOOL007 (~64 bytes)
public ref readonly Vector3 GetPos() => ref _pos;  // TOOL008 (~12 bytes)

// Good
public ref readonly Matrix4x4 WorldMatrix => ref _worldMatrix;
public Vector3 GetPos() => _pos;
```

---

### Unknown Struct Layout (`TOOL009`)

Emitted whenever any analyzer encounters a struct whose x64 CLR size cannot be computed reliably:
- A struct from a .NET reference assembly (`Microsoft.NETCore.App.Ref`) whose private fields were replaced by GenAPI with `_dummy` / `_dummyPrimitive` and is not yet listed in `StructLayoutCalculator.BuildKnownLayouts`.
- A struct whose runtime size is CPU-ISA-dependent (such as `System.Numerics.Vector<T>`).
- A `.NET 10` `[ExtendedLayout]` struct.
- An external compiled type whose PE metadata could not be read, an `ExplicitLayout` struct missing a field offset, or a 0-field metadata struct with no explicit `Size`.

> Note: Open generic structs depending on an unresolved type parameter `T` (e.g., `SlotMap<T>.Slot`) are classified as `GenericOrUnresolved` and intentionally produce **no** warnings (including no `TOOL009`), because their size varies per instantiation.

---

## How Layout Calculation Works

### Architecture (`src/Tool.Analyzers/`)

| File | Responsibility |
|---|---|
| `DiagnosticIds.cs` | Public `TOOL001`–`TOOL009` diagnostic ID constants |
| `DiagnosticDescriptors.cs` | `DiagnosticDescriptor` definitions and message formats |
| `StructLayoutInfo.cs` | Immutable layout result (`Known`, `GenericOrUnresolved`, `UnknownLayout`) |
| `MetadataLayoutReader.cs` | Reads ECMA-335 `LayoutMask`, `ClassLayout`, and `FieldLayout` from compiled DLLs |
| `StructLayoutCalculator.cs` | Compilation-scoped, thread-safe x64 CLR size, alignment, and padding calculator |
| `SymbolHelpers.cs` | Shared symbol classification, formatting, and C# legality helpers |
| `StructParameterAnalyzer.cs` | `TOOL001`, `TOOL002`, `TOOL009` |
| `StructPaddingAnalyzer.cs` | `TOOL003`, `TOOL009` |
| `StructLocalCopyAnalyzer.cs` | `TOOL004`, `TOOL005`, `TOOL006`, `TOOL009` |
| `StructReturnAnalyzer.cs` | `TOOL007`, `TOOL008`, `TOOL009` |

### Source vs. External Metadata

Roslyn's public `ISymbol.GetAttributes()` exposes `[StructLayout]` and `[FieldOffset]` for source-declared types, but omits them for compiled metadata assemblies because ECMA-335 encodes them as pseudo-custom attributes in PE tables (`TypeDef.Flags`, `ClassLayout`, `FieldLayout`).

`MetadataLayoutReader` solves this cleanly without disk I/O or third-party hardcoding by calling `IModuleSymbol.GetMetadata().GetMetadataReader()` on Roslyn's in-memory PE metadata snapshot and caching one lookup table per referenced module per compilation. This accurately handles:
- `LayoutKind.Sequential`, `LayoutKind.Auto`, `LayoutKind.Explicit`, and `.NET 10` `ExtendedLayout`
- Explicit `Pack` and `Size` values (for example, `Pack = 4` on `Vortice.Mathematics.Color4`)
- C-style overlapping `[FieldOffset]` unions (for example, `SDL.Event`, `VkClearValue`, `ShaderResourceViewDescription`)
- Fixed-size buffers (`fixed float M[16]`), C# 12 `[InlineArray(N)]`, and C# 11 `ref` fields
- CoreCLR `Auto` promotion for structs containing GC references and generic structs

For core .NET BCL / `System.Runtime.Intrinsics` / `System.Numerics` types whose private fields are stripped in `Microsoft.NETCore.App.Ref`, `StructLayoutCalculator` includes an explicit x64 layout table (plus composite handlers for `Nullable<T>`, `KeyValuePair<TKey, TValue>`, and `ValueTask<TResult>`).

---

## Configuration & Suppression

### Local Suppression

Because small-struct rules (`TOOL002`, `TOOL006`, `TOOL008`) intentionally do not track mutations, suppress specific call sites where in-place mutation of a `< 16`-byte struct via `ref` is required:

```csharp
#pragma warning disable TOOL006
ref SmallStruct item = ref span[i];
item.Counter++;
#pragma warning restore TOOL006
```

### Severity Overrides via `.editorconfig`

You can customize the severity of any rule globally or per directory in `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.TOOL001.severity = warning
dotnet_diagnostic.TOOL002.severity = suggestion
dotnet_diagnostic.TOOL006.severity = suggestion
```

---

## Build & Maintenance Notes

- **`TargetFramework`:** `Tool.Analyzers.csproj` targets `netstandard2.0` (required by Roslyn analyzer hosting rules and `<EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>`), with `<LangVersion>latest</LangVersion>` and `<IsRoslynComponent>true</IsRoslynComponent>`.
- **Automatic wiring:** `Directory.Build.props` attaches `Tool.Analyzers` to all non-analyzer projects via `OutputItemType="Analyzer" ReferenceOutputAssembly="false"`.
- **Verifying analyzer changes:** IDEs (Visual Studio / Rider) may cache the loaded analyzer assembly in memory across edits. After changing code in `src/Tool.Analyzers`, run a clean CLI build to see fresh diagnostics:

```powershell
dotnet build --no-incremental
```

- **Verifying a struct's exact runtime size:** To cross-check an analyzer size estimate against the .NET x64 JIT, inspect `Unsafe.SizeOf<T>()` in a unit test or debug check.