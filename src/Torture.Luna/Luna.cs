using System.Numerics;
using System.Runtime.InteropServices;

using StbImageSharp;

using Torture.Core;
using Torture.Core.Collections;
using Torture.Core.Graphics;
using Torture.Core.Input;

using Vortice.Mathematics;

namespace Torture.Luna;

[SDL3.SDL.GenerateMain]
public sealed partial class Luna : Application<Luna>
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct MatrixBufferType
    {
        public Matrix4x4 World;
        public Matrix4x4 View;
        public Matrix4x4 Proj;
    };

    private readonly bool _showFpsInTitle = true;
    private float _elapsedSeconds;
    private int _frameCount;

    RasterizerState _solid = default!;
    RasterizerState _wireframe = default!;
    RasterizerState _rasterizerState = default!;
    (List<VertexPositionNormalTexture> Vertices, List<ushort> Indices) _modelData = default!;
    Core.Graphics.Buffer _vb = default!;
    Core.Graphics.Buffer _ib = default!;
    Core.Graphics.Buffer _cb = default!;
    Shader _vs = default!;
    Shader _ps = default!;
    InputLayout _inputLayout = default!;
    Texture _container = default!;
    Texture _containerSpecular = default!;
    Sampler _sampler = default!;
    Sampler _bilinear = default!;
    Sampler _anisotropic16 = default!;

    public Luna()
    {
        Title = "Luna";
        Identifier = "Semisoft.Luna";
        PreferredWindowSize = new SizeI(1280, 720);
        WindowMode = WindowMode.Windowed;
    }


    protected override void Initialize()
    {
        GraphicsDevice.VSync = true;
        GraphicsDevice.ClearColor = new Color4(0.2f, 0.3f, 0.3f, 1.0f);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _solid = GraphicsDevice.CreateRasterizerState(RasterizerStateDescriptionBuilder.CullNone.Build());
        _wireframe = GraphicsDevice.CreateRasterizerState(RasterizerStateDescriptionBuilder.Wireframe.Build());
        _rasterizerState = _solid;

        _modelData = Geometry.ComputeBox(Vector3.One * 4.0f, true, false);

        _cb = GraphicsDevice.CreateBuffer(BufferDescription.Constant<MatrixBufferType>());
        _vb = GraphicsDevice.CreateBuffer(_modelData.Vertices.AsSpan(), BufferDescription.Vertex(0, 0));
        _ib = GraphicsDevice.CreateBuffer(_modelData.Indices.AsSpan(), BufferDescription.Index(0));

        var triangleCode = File.ReadAllBytes(Path.Combine("Shaders", "Texture.hlsl"));

        _vs = GraphicsDevice.CreateShader(new ShaderDescription(
            ShaderType.Vertex,
            triangleCode,
            "VSMain",
            true,
            false,
            "Triangle_VS"));

        _ps = GraphicsDevice.CreateShader(new ShaderDescription(
            ShaderType.Pixel,
            triangleCode,
            "PSMain",
            true,
            false,
            "Triangle_PS"));

        _inputLayout = GraphicsDevice.CreateInputLayout(VertexPositionNormalTexture.Layout, _vs);

        // Load texture
        // Create texture resource
        using (var stream = File.OpenRead("resources/textures/container2.png"))
        {
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            _container = GraphicsDevice.CreateTexture(
                image.Data.AsSpan(),
                TextureDescription.Texture2D((uint)image.Width, (uint)image.Height, mipLevels: 0));
        }

        using (var stream = File.OpenRead("resources/textures/container2_specular.png"))
        {
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            _containerSpecular = GraphicsDevice.CreateTexture(
                image.Data.AsSpan(),
                TextureDescription.Texture2D((uint)image.Width, (uint)image.Height, mipLevels: 0));
        }

        _bilinear = GraphicsDevice.CreateSampler(SamplerDescription.Bilinear);
        _anisotropic16 = GraphicsDevice.CreateSampler(SamplerDescription.AnisotropicWrap(16.0f));
        _sampler = _bilinear;
    }

    float _angle = 0.0f;

    protected override unsafe void Update(GameTime gameTime)
    {
        if (Keyboard.IsKeyPressed(KeyboardKey.Escape))
        {
            Exit();
        }

        if (_showFpsInTitle)
        {
            _frameCount++;
            _elapsedSeconds += gameTime.DeltaSeconds;
            if (_elapsedSeconds >= 1.0f)
            {
                double fps = _frameCount / _elapsedSeconds;
                //Console.WriteLine($"Luna | FPS: {fps:F1}");
                _elapsedSeconds -= 1.0f;
                _frameCount = 0;
            }
        }

        if (Keyboard.IsKeyPressed(KeyboardKey.A))
        {
            _sampler = _bilinear;
        }
        if (Keyboard.IsKeyPressed(KeyboardKey.S))
        {
            _sampler = _anisotropic16;
        }

        if (Keyboard.IsKeyPressed(KeyboardKey.Q))
        {
            _rasterizerState = _solid;
        }
        if (Keyboard.IsKeyPressed(KeyboardKey.W))
        {
            _rasterizerState = _wireframe;
        }

        _angle += 10.0f * gameTime.DeltaSeconds;
        MatrixBufferType mvp = new()
        {
            World = Matrix4x4.CreateRotationY(float.DegreesToRadians(_angle)) * Matrix4x4.CreateRotationX(0.4f),
            View = Matrix4x4.CreateLookAt(new Vector3(0.0f, 0.0f, -5.0f), Vector3.UnitZ, Vector3.UnitY),
            Proj = Matrix4x4.CreatePerspectiveFieldOfView(float.DegreesToRadians(85.0f), 1280.0f / 720.0f, 0.1f, 1000.0f)
        };

        GraphicsDevice.UpdateBuffer(_cb, ref mvp);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRasterizerState(_rasterizerState);
        GraphicsDevice.SetVertexShader(_vs);
        GraphicsDevice.SetPixelShader(_ps);
        GraphicsDevice.SetInputLayout(_inputLayout);
        GraphicsDevice.SetConstantBuffer(_cb, ShaderType.Vertex, 0);
        GraphicsDevice.SetTexture(_container, ShaderType.Pixel, 0);
        GraphicsDevice.SetTexture(_containerSpecular, ShaderType.Pixel, 1u);
        GraphicsDevice.SetSampler(_sampler, ShaderType.Pixel, 0);
        GraphicsDevice.SetVertexBuffer(_vb);
        GraphicsDevice.SetIndexBuffer(_ib);
        GraphicsDevice.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
        GraphicsDevice.DrawIndexed(_ib.Count, 0, 0);
    }

    protected override void UnloadContent()
    {
        _solid?.Dispose();
        _wireframe?.Dispose();
        _inputLayout?.Dispose();
        _ps?.Dispose();
        _cb?.Dispose();
        _vs?.Dispose();
        _ib?.Dispose();
        _vb?.Dispose();
        _container?.Dispose();
        _containerSpecular?.Dispose();
        _bilinear?.Dispose();
        _anisotropic16?.Dispose();
    }

    protected override void Cleanup()
    {
    }

    protected override GraphicsDevice CreateGraphicsDevice(GraphicsAPI api, NativeWindowInfo window, GraphicsDeviceOptions options)
    {
        if (api == GraphicsAPI.Default)
        {
#if SUPPORT_DIRECTX
            api = GraphicsAPI.D3D11;
#elif SUPPORT_VULKAN
            //api = GraphicsAPI.Vulkan;
#endif
        }

        return api switch
        {
#if SUPPORT_DIRECTX
            GraphicsAPI.D3D11
                => new Graphics.Direct3D11.D3D11GraphicsDevice(options, window.WindowHandle, window.ClientSize),
#endif
#if SUPPORT_VULKAN
            //GraphicsAPI.Vulkan
            //    => new VulkanGraphicsDevice(),
#endif
            _ => throw new PlatformNotSupportedException($"{api} is not supported on this platform.")
        };
    }
}
