using SharpGen.Runtime;

using Torture.Core.Graphics;

using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace Torture.Graphics.Direct3D11;

internal sealed unsafe class D3D11Shader : Shader
{
    public D3D11Shader(ID3D11Device5 device, in ShaderDescription desc)
    {
        Blob code = default!;
        Blob errorMsgs = default!;

        try
        {
            string profile = desc.ShaderType switch
            {
                ShaderType.Vertex => "vs_5_0",
                ShaderType.Pixel => "ps_5_0",
                _ => throw new NotSupportedException($"Shader type {desc.ShaderType} not supported.")
            };

            ShaderFlags flags = ShaderFlags.None;

            if (desc.PackAsRowMajor)
            {
                flags |= ShaderFlags.PackMatrixRowMajor;
            }

            if (desc.Debug)
            {
                flags |= ShaderFlags.Debug;
            }
            else
            {
                flags |= ShaderFlags.OptimizationLevel3;
            }

            Result result;

            fixed (byte* srcData = desc.ShaderSource)
            {
                // sourceName (receiving DebugName) can accept a null value
                result = Compiler.Compile(
                    srcData,
                    (uint)desc.ShaderSource.Length,
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
                throw new InvalidOperationException($"Shader compilation failed ({desc.DebugName}): {error}");
            }

            Native = desc.ShaderType switch
            {
                ShaderType.Vertex => device.CreateVertexShader(code),
                ShaderType.Pixel => device.CreatePixelShader(code),
                _ => throw new NotSupportedException($"Shader type {desc.ShaderType} not supported."),
            };

            Bytecode = code.AsBytes();
        }
        finally
        {
            code?.Dispose();
            errorMsgs?.Dispose();
        }
    }

    public ID3D11DeviceChild Native { get; private set; }

    public byte[] Bytecode { get; private set; }

    public override void Dispose()
    {
        if (_disposed)
            return;

        Native.Dispose();
        Native = null!;
        Bytecode = null!;

        _disposed = true;
    }
}
