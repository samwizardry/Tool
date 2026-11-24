using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace Tool.Direct3D11;

internal readonly unsafe struct D3D11Shader
{
    public readonly Graphics.ShaderDescription Desc;
    public readonly ID3D11DeviceChild Native;
    public readonly byte[]? Bytecode;

    private D3D11Shader(in Graphics.ShaderDescription desc, ID3D11DeviceChild shader, byte[]? bytecode)
    {
        Desc = desc;
        Native = shader;
        Bytecode = bytecode;
    }

    public void Destroy()
    {
        Native?.Dispose();
    }

    public Graphics.ShaderType ShaderType => Desc.ShaderType;

    public static D3D11Shader Create(ID3D11Device5 device, ReadOnlySpan<byte> source, in Graphics.ShaderDescription desc)
    {
        Blob code = default!;
        Blob errorMsgs = default!;

        try
        {
            string profile = desc.ShaderType switch
            {
                Graphics.ShaderType.Vertex => "vs_5_0",
                Graphics.ShaderType.Pixel => "ps_5_0",
                _ => throw new NotSupportedException()
            };

            ShaderFlags flags = ShaderFlags.None;

            if (desc.PackAsRowMajor)
            {
                flags |= ShaderFlags.PackMatrixRowMajor;
            }

            if (desc.Debug)
            {
                flags |= ShaderFlags.Debug | ShaderFlags.SkipOptimization;
            }
            else
            {
                flags |= ShaderFlags.OptimizationLevel3;
            }

            SharpGen.Runtime.Result result;

            fixed (byte* srcData = source)
            {
                // sourceName (receiving DebugName) can accept a null value
                result = Compiler.Compile(
                    srcData,
                    (uint)source.Length,
                    desc.DebugName!,
                    null,
                    null,
                    desc.EntryPoint,
                    profile,
                    flags,
                    EffectFlags.None,
                    out code, out errorMsgs);
            }

            if (result.Failure)
            {
                string error = errorMsgs != null ? errorMsgs.AsString() : "Unknown shader compilation error.";
                throw new InvalidOperationException($"Shader compilation failed. {desc.DebugName}: {error}");
            }

            ID3D11DeviceChild shader = desc.ShaderType switch
            {
                Graphics.ShaderType.Vertex => device.CreateVertexShader(code),
                Graphics.ShaderType.Pixel => device.CreatePixelShader(code),
                _ => throw new NotSupportedException(),
            };

            shader.DebugName = desc.DebugName;

            byte[]? bytecode = null;
            if (desc.ShaderType == Graphics.ShaderType.Vertex)
            {
                bytecode = code.AsBytes();
            }

            return new D3D11Shader(in desc, shader, bytecode);
        }
        finally
        {
            code?.Dispose();
            errorMsgs?.Dispose();
        }
    }
}
