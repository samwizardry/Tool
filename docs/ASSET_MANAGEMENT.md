# Resource and Asset Management

This document defines resource ownership, lifetime rules, and usage conventions for `GraphicsDevice`, `AssetManager`, and `AssetScope` in the **Torture** engine.

---

## 1. Quick Reference

| Category | Types | Owner | Release |
| :--- | :--- | :--- | :--- |
| **GPU Immutable States** | `RasterStateHandle`, `SamplerHandle`, `InputLayoutHandle`, `BindingLayoutHandle`, `GraphicsPipelineHandle` | `GraphicsDevice` (cached by description) | Freed automatically on `GraphicsDevice.Dispose()`. Do not destroy manually. |
| **GPU Data Resources** | `BufferHandle`, `TextureHandle`, `ShaderHandle`, `BindingSetHandle` | Caller (`AssetManager`, render pass, etc.) | `GraphicsDevice.Destroy(handle)` -> queued for `FramesInFlight` frames. |
| **High-Level Assets** | `ShaderAsset`, `TextureAsset`, `MaterialAsset`, `ModelAsset` | `AssetManager` (reference-counted) | Released via `AssetScope.Dispose()` or `AssetManager.Release(handle)`. |
| **Ownership Scopes** | `AssetScope` (Global, Scene, Chunk) | Application / Scene / Chunk | `scope.Dispose()` releases tracked assets in reverse dependency order. |

---

## 2. Ownership and RefCount Hierarchy

```text
Application
 ├── GraphicsDevice       (owns GPU immutable states + deferred deletion queues)
 └── AssetManager         (owns AssetPools + RefCounts + loaders)
      ├── GlobalScope     (app lifetime: shared shaders, default textures, shared materials)
      └── SceneScope      (level/chunk lifetime: models, scene textures, scene materials)
```

Asset dependency chain inside `AssetManager`:

```text
AssetScope ──(ref)──> ModelAsset ──(ref)──> MaterialAsset ──(ref)──> TextureAsset
```

* `ModelAsset` owns its `Mesh[]` GPU buffers and holds references to its `MaterialAsset[]`.
* `MaterialAsset` owns its `BindingSetHandle` and optional `ConstantBuffer`, and holds references to its `TextureAsset` slots.
* When a handle's `RefCount` reaches `0`, `AssetManager` destroys its GPU handles and calls `Release()` on child assets.

---

## 3. Core Rules

### Handles vs. Asset Data
* Store `AssetHandle<T>` and GPU `*Handle` values in fields, components, and scenes.
* Do **not** store copies of `MaterialAsset`, `TextureAsset`, or `ModelAsset` structs across frames (material edits replace `BindingSetHandle` inside `AssetManager`).
* Read asset data locally via `Get(handle)` or `ResolveMaterial(handle)`.
* Do **not** hold a `ref readonly` return from `Get(...)` or `GetBufferDescription(...)` across calls that allocate or destroy items in the same pool (`SlotMap` may resize).

### Identification and Paths
* Always pass relative virtual paths to `AssetManager` / `AssetScope` (e.g., `"textures/container2.png"`). `AssetManager.ResolvePath` prepends `RootDirectory`.
* `AssetId` is a 128-bit `Guid` generated deterministically from a normalized path via `AssetId.FromPath(...)` (or `AssetId.New()` for procedural assets).
* Shaders include stage and entry point in their ID (`"{path}#{ShaderType}:{EntryPoint}"`) so vertex and pixel shaders in one `.hlsl` file do not collide.

### Using `AssetScope`
* Load and create assets through an `AssetScope` instead of calling `AssetManager.Acquire*` / `Release` manually.
* Objects that only render an asset (e.g., `Actor`) receive an `AssetHandle<ModelAsset>` without owning it and must **not** call `Release`.

### Direct `AssetManager` / `RefCount` Rules
* Every `Acquire*`, `Create*`, or `AddRef` increments `RefCount` by `1` and must be paired with `Release`.
* `CreateMaterial` automatically calls `AddRef` on every texture in `MaterialDescription.Textures`.
* `CreateModel` automatically calls `AddRef` on every material in `materials` and takes ownership of `meshes`.

### Writing an `IModelLoader`
* `IModelLoader.Load(GraphicsDevice device, AssetManager assets, string virtualPath)` receives `virtualPath`. Resolve disk paths with `assets.ResolvePath(virtualPath)`.
* Use a local `using var tempTextures = new AssetScope(assets);` to load textures before calling `assets.CreateMaterial(...)`. On return, `tempTextures.Dispose()` drops the loader's temporary references, leaving only the references owned by each `MaterialAsset`.
* If the loader reuses an existing shared material (instead of creating one via `assets.CreateMaterial`), call `assets.AddRef(materialHandle)` before returning `ModelAsset`.
* On failure, destroy any created `Mesh` buffers, `Release` any created `MaterialAsset` handles, and **rethrow** the exception. Do not return an empty `ModelAsset`.

### Editing Materials at Runtime
* **Constants (`Shininess`, colors, floats):** call `AssetManager.UpdateMaterialConstants(handle, ref data)`. Updates the constant buffer in place; does not recreate `BindingSet`.
* **Textures / Samplers:** call `AssetManager.SetMaterialTexture(...)` or `SetMaterialSampler(...)`. Updates `RefCount` (`AddRef` new before `Release` old) and sets `IsBindingSetDirty = true`. The `BindingSet` is rebuilt on the next `ResolveMaterial` / `DrawModel` call, and the previous `BindingSetHandle` goes to the deferred deletion queue.

---

## 4. Usage Example

### Initialization and Scopes
```csharp
// Initialize
AssetManager.RegisterShaderLoader(new FileShaderLoader());
AssetManager.RegisterTextureLoader(new StbTextureLoader());
AssetManager.RegisterModelLoader(new AssimpModelLoader());

// LoadContent
_globalScope = new AssetScope(AssetManager);
_sceneScope = new AssetScope(AssetManager);

var vs = _globalScope.LoadShader("Shaders/Phong.hlsl", ShaderDescription.Vertex());
var tex = _globalScope.LoadTexture("resources/textures/container2.png");
var mat = _globalScope.CreateMaterial(new MaterialDescription(
    Pipeline: pipeline,
    BindingLayout: layout,
    Textures: [new(0, tex)],
    Samplers: [new(0, sampler)]));

var model = _sceneScope.LoadModel("resources/objects/backpack/backpack.obj");

// Draw
AssetManager.DrawModel(model, perFrameSet, perObjectSet);
```

### Shutdown Order
`Application<TApp>.AppQuit` enforces the shutdown order:

1. `GraphicsDevice.WaitIdle()`
2. `UnloadContent()` -> dispose actors, `_sceneScope.Dispose()`, `_globalScope.Dispose()`, destroy manual per-frame GPU buffers/sets.
3. `AssetManager.Dispose()` -> logs warnings for any unreleased assets and drains remaining pools.
4. `GraphicsDevice.Dispose()` -> flushes deferred deletion queues, logs any leaked GPU handles, frees immutable state caches, destroys the native device.