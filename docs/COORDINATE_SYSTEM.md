# Coordinate System, Matrix, and Shader Conventions

This document defines the mathematical and graphics conventions used across the **Tool** engine (`System.Numerics` + **NVRHI**-style architecture + **HLSL**).

---

## 1. Quick Reference

| Property | Tool Standard | Configured In |
| :--- | :--- | :--- |
| **World / View Space** | **Right-Handed (RHS)**: `+X` Right, `+Y` Up, **`-Z` Forward** (`+Z` points out of the screen toward the viewer) | `Matrix4x4.CreateLookAt`, `Matrix4x4.CreatePerspectiveFieldOfView` |
| **Clip Space (NDC)** | `X: [-1 .. 1]`, `Y: [-1 .. 1]` (upward), **`Z: [0 .. 1]`** (Near to Far) | D3D11 / D3D12 / Vulkan (with negative viewport height) |
| **Texture Coordinates (UV)** | `(0, 0)` at Top-Left, `(1, 1)` at Bottom-Right | DirectX / NVRHI standard |
| **Triangle Winding Order** | **Counter-Clockwise (`CCW`)** for front-facing triangles | `RasterStateDescription.FrontCounterClockwise = true` |
| **Face Culling** | Back-face culling (`CW` faces are culled) | `RasterCullMode.Back` |
| **Vector Convention** | **Row-Vector Convention**: $v' = v \cdot M$ | `System.Numerics.Matrix4x4` |
| **Translation Storage** | Stored in the **4th row** (`M41, M42, M43`) | `Matrix4x4.Translation` |
| **Matrix Memory Layout** | **Row-Major** (no CPU transposition before uploading) | `ShaderDescription.PackAsRowMajor = true` (`ShaderFlags.PackMatrixRowMajor`) |
| **C# Matrix Multiplication** | Left-to-right: `Scale * Rotation * Translation`, `Model * View * Proj` | `System.Numerics` |
| **HLSL Multiplication** | Vector as the **first** argument: `mul(vector, matrix)` | All `.hlsl` shaders |

---

## 2. Coordinate System (Right-Handed System, RHS)

The engine uses a **Right-Handed Coordinate System (RHS)**, matching industry standards such as **glTF 2.0**, **OpenUSD**, **Blender**, and **NVIDIA Donut**:

```text
        +Y (Up)
         |
         |
         +------ +X (Right)
        /
       /
     +Z (Backward / Out of screen)
    (-Z is Forward / Into the screen)
```

### Camera Matrices in `System.Numerics`
In .NET, `Matrix4x4` methods without the `LeftHanded` suffix default to **RHS** with a $[0, 1]$ depth range:

```csharp
// View matrix (Right-Handed)
Matrix4x4 view = Matrix4x4.CreateLookAt(cameraPosition, cameraPosition + cameraFront, cameraUp);

// Projection matrix (Right-Handed, depth [0..1])
Matrix4x4 proj = Matrix4x4.CreatePerspectiveFieldOfView(fovRadians, aspectRatio, nearPlane, farPlane);

// Combined View-Projection matrix (multiplied left-to-right)
Matrix4x4 viewProj = view * proj;
```

### Camera Orientation Vectors (Yaw / Pitch)
At `yaw = 0°` and `pitch = 0°`, the camera looks straight ahead along **`-Z`** `(0, 0, -1)`:

```csharp
(float sinYaw, float cosYaw) = MathF.SinCos(float.DegreesToRadians(yaw));
(float sinPitch, float cosPitch) = MathF.SinCos(float.DegreesToRadians(pitch));

Vector3 front = Vector3.Normalize(new Vector3(
    sinYaw * cosPitch,
    sinPitch,
    -cosYaw * cosPitch));

Vector3 right = Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY));
Vector3 up = Vector3.Normalize(Vector3.Cross(right, front));
```

---

## 3. Winding Order (`CCW`) and Face Culling

Front-facing triangles (when viewed from the outside of a mesh) use **Counter-Clockwise (`CCW`)** vertex winding:

```text
       v0
      /  \
     /    \      Winding v0 -> v1 -> v2 (counter-clockwise)
    v1 ---- v2   is treated as the Front Face.
```

* `RasterStateDescription` defaults to `FrontCounterClockwise = true` and `CullMode = RasterCullMode.Back`.
* Procedural geometry in `Tool.Core.Graphics.Geometry` generates all meshes in `RHS + CCW`.
* External 3D assets authored in **glTF 2.0 / Blender / OBJ** natively follow `RHS + CCW` and can be loaded directly into vertex/index buffers without axis conversion or index flipping.

---

## 4. Passing Matrices and Vectors (C# `System.Numerics` to HLSL)

### Memory Layout of `Matrix4x4` in C#
`System.Numerics.Matrix4x4` uses **row vectors** and is laid out in memory in **Row-Major** order. The translation component occupies the **4th row**:

$$
M = \begin{bmatrix}
M_{11} & M_{12} & M_{13} & 0 \\
M_{21} & M_{22} & M_{23} & 0 \\
M_{31} & M_{32} & M_{33} & 0 \\
\mathbf{T_x} & \mathbf{T_y} & \mathbf{T_z} & 1
\end{bmatrix}
$$

### Matrix Packing in HLSL
By default, HLSL expects matrices in constant buffers to be `column_major` (which would implicitly transpose a row-major C# matrix upon reading).
To ensure C# memory and HLSL `cbuffer` memory match **byte-for-byte**:

1. Shaders are compiled with `PackAsRowMajor = true` (`ShaderFlags.PackMatrixRowMajor`).
2. On the CPU, `Model`, `View`, `Proj`, and `ViewProj` matrices **must NOT be transposed** before uploading to constant buffers.
3. In HLSL, the vector must always be passed as the **first argument** to `mul(vector, matrix)`:

```hlsl
cbuffer PerFrameBuffer : register(b0)
{
    float4x4 gViewProj;
};

cbuffer PerObjectBuffer : register(b1)
{
    float4x4 gModel;
    float4x4 gNormalMatrix;
};

// Correct row-vector multiplication order:
float4 worldPos = mul(float4(input.Position, 1.0f), gModel);
float4 clipPos  = mul(worldPos, gViewProj);
```

---

## 5. Normal Matrix Transformation

Surface normals cannot be transformed directly by `gModel` when an object undergoes **non-uniform scaling**, as that would break perpendicularity to the surface tangent plane.

Mathematically, the normal transformation matrix is the **Inverse-Transpose** of the upper-left $3 \times 3$ portion of the `Model` matrix:

$$G = \left(M^{-1}\right)^T$$

> **Note:** Calling `Matrix4x4.Transpose(inverseModel)` here is required by the mathematical derivation of normal transformation ($M \cdot G^T = I$), **not** because of HLSL memory layout.

### CPU Calculation (C#):
```csharp
if (Matrix4x4.Invert(model, out Matrix4x4 inverseModel))
{
    // Clear translation before transposing so row 4 does not pollute column 4
    inverseModel.Translation = Vector3.Zero;
    perObjectData.NormalMatrix = Matrix4x4.Transpose(inverseModel);
}
else
{
    perObjectData.NormalMatrix = Matrix4x4.Identity;
}
```

### Shader Usage (HLSL):
```hlsl
float3 worldNormal = normalize(mul(input.Normal, (float3x3)gNormalMatrix));
```