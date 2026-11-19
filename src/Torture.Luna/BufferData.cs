using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Torture.Graphics;

namespace Torture.Luna;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PhongMaterial
{
    public float Shininess;
    public Vector3 Pad0;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DirectionalLight
{
    public Vector3 Direction;
    public float Pad0;
    public Vector3 Ambient;
    public float Pad1;
    public Vector3 Diffuse;
    public float Pad2;
    public Vector3 Specular;
    public float Pad3;
};

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PointLight
{
    public Vector3 Position;
    public float Constant;
    public Vector3 Diffuse;
    public float Linear;
    public Vector3 Specular;
    public float Quadratic;
};

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SpotLight
{
    public Vector3 Position;
    public float CutOff;
    public Vector3 Direction;
    public float OuterCutOff;
    public Vector3 Diffuse;
    public float Linear;
    public Vector3 Specular;
    public float Quadratic;
};

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Light
{
    public DirectionalLight DirLight;
    public PointLight PointLight;
    public SpotLight SpotLight;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PhongPerFrame
{
    public Matrix4x4 ViewProj;
    public Light Light;
    public Vector3 ViewPos;
};

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PhongObject
{
    public Matrix4x4 ModelMatrix;
    public Matrix4x4 NormalMatrix;
    public PhongMaterial Material;
}

public abstract class BufferData<T>
    where T : unmanaged
{
    protected T _data;
    protected bool _isDirty = true;

    private readonly Graphics.Buffer _buffer;

    protected BufferData(Graphics.Buffer buffer)
    {
        _buffer = buffer;
    }

    public void Update(GraphicsDevice device)
    {
        if (_isDirty)
        {
            device.UpdateBuffer(_buffer, ref _data, 0);
            _isDirty = false;
        }
    }
}

public sealed class PhongPerFrameData : BufferData<PhongPerFrame>
{
    public PhongPerFrameData(Graphics.Buffer buffer)
        : base(buffer)
    {
        _data = default!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetViewProj(in Matrix4x4 viewProj)
    {
        _data.ViewProj = viewProj;
        _isDirty = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetViewPos(Vector3 viewPos)
    {
        _data.ViewPos = viewPos;
        _isDirty = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetDirLight(in DirectionalLight light)
    {
        _data.Light.DirLight = light;
        _isDirty = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPointLight(in PointLight light)
    {
        _data.Light.PointLight = light;
        _isDirty = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSpotLight(in SpotLight light)
    {
        _data.Light.SpotLight = light;
        _isDirty = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSpotLightPosition(Vector3 position)
    {
        _data.Light.SpotLight.Position = position;
        _isDirty = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSpotLightDirection(Vector3 direction)
    {
        _data.Light.SpotLight.Direction = direction;
        _isDirty = true;
    }
}

public sealed class PhongPerObjectData : BufferData<PhongObject>
{
    public PhongPerObjectData(Graphics.Buffer buffer)
        : base(buffer)
    {
        _data = default!;
    }

    public ref readonly Matrix4x4 ModelMatrix => ref _data.ModelMatrix;
    public ref readonly Matrix4x4 NormalMatrix => ref _data.NormalMatrix;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetModelMatrix(in Matrix4x4 matrix)
    {
        _data.ModelMatrix = matrix;
        _isDirty = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetNormalMatrix(in Matrix4x4 matrix)
    {
        _data.NormalMatrix = matrix;
        _isDirty = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetMaterial(in PhongMaterial mat)
    {
        _data.Material = mat;
        _isDirty = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShininess(float shininess)
    {
        _data.Material.Shininess = shininess;
        _isDirty = true;
    }
}
