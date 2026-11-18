using Torture.Graphics;

namespace Torture.Luna;

public class PipelineState : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;

    private readonly RasterState _solid = default!;
    private readonly RasterState _wireframe = default!;
    private RasterState _rs = default!;
    private readonly Sampler _bilinear = default!;
    private readonly Sampler _anisotropic16 = default!;
    private Sampler _sampler = default!;
    private readonly Shader _vs = default!;
    private readonly Shader _ps = default!;
    private readonly Shader _lunaVS = default!;
    private readonly Shader _lunaPS = default!;
    private readonly InputLayout _inputLayout = default!;
    private readonly Graphics.Buffer _perFrame = default!;

    public PipelineState(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice;

        _solid = graphicsDevice.CreateRasterState(RasterStateDescription.CullBack);
        _wireframe = graphicsDevice.CreateRasterState(RasterStateDescription.Wireframe);
        _rs = _solid;

        _bilinear = graphicsDevice.CreateSampler(SamplerDescription.Bilinear);
        _anisotropic16 = graphicsDevice.CreateSampler(SamplerDescription.AnisotropicWrap(16.0f));
        _sampler = _anisotropic16;

        // Shaders
        var shaderSource = File.ReadAllBytes(Path.Combine("Shaders", "Phong.hlsl"));

        _vs = graphicsDevice.CreateShader(shaderSource, ShaderDescription.Vertex(
            debug: true,
            debugName: "Triangle_VS"));

        _ps = graphicsDevice.CreateShader(shaderSource, ShaderDescription.Pixel(
            debug: true,
            debugName: "Triangle_PS"));

        shaderSource = File.ReadAllBytes(Path.Combine("Shaders", "PosNormal.hlsl"));

        _lunaVS = graphicsDevice.CreateShader(shaderSource, ShaderDescription.Vertex(
            debug: true,
            debugName: "PosNormal_VS"));

        _lunaPS = graphicsDevice.CreateShader(shaderSource, ShaderDescription.Pixel(
            debug: true,
            debugName: "PosNormal_PS"));

        _inputLayout = graphicsDevice.CreateInputLayout(VertexPositionNormalTexture.Layout, _vs);

        _perFrame = graphicsDevice.CreateBuffer(BufferDescription.Constant<PerFrameData>());
    }

    public Shader LunaVS => _lunaVS;

    public void UseSolid() =>_rs = _solid;
    public void UseWireframe() => _rs = _wireframe;
    public void UseBilinear() => _sampler = _bilinear;
    public void UseAnisotropic() => _sampler = _anisotropic16;

    public void SetPerFrameData(ref PerFrameData data)
    {
        _graphicsDevice.UpdateBuffer(_perFrame, ref data);
    }

    public void Apply()
    {
        _graphicsDevice.SetRasterState(_rs);
        _graphicsDevice.SetSampler(_sampler, ShaderType.Pixel, 0);
        _graphicsDevice.SetVertexShader(_vs);
        _graphicsDevice.SetPixelShader(_ps);
        _graphicsDevice.SetInputLayout(_inputLayout);
        _graphicsDevice.SetConstantBuffer(_perFrame, ShaderType.Vertex | ShaderType.Pixel, 0);
        _graphicsDevice.SetPrimitiveTopology(PrimitiveTopology.TriangleList);


        //_graphicsDevice.SetVertexShader(_lunaVS);
        //_graphicsDevice.SetPixelShader(_lunaPS);
    }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
            return;

        _solid?.Dispose();
        _wireframe?.Dispose();
        _bilinear?.Dispose();
        _anisotropic16?.Dispose();
        _vs?.Dispose();
        _ps?.Dispose();
        _lunaVS?.Dispose();
        _lunaPS?.Dispose();
        _inputLayout?.Dispose();
        _perFrame?.Dispose();

        _disposed = true;
    }
}
