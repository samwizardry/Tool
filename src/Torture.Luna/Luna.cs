using System.Numerics;
using System.Runtime.CompilerServices;
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

    public Luna()
    {
        Title = "Luna";
        Identifier = "Semisoft.Luna";
        PreferredWindowSize = new SizeI(1280, 720);
        WindowMode = WindowMode.Windowed;
    }


    (List<VertexPositionNormalTexture> Vertices, List<ushort> Indices) _modelData;
    Core.Graphics.Buffer _vb;
    Core.Graphics.Buffer _ib;
    Core.Graphics.Buffer _cb;
    Shader _vs;
    Shader _ps;
    InputLayout _inputLayout;
    Texture _container;
    Texture _containerSpecular;

    protected override void Initialize()
    {
        GraphicsDevice.VSync = true;
        GraphicsDevice.ClearColor = new Color4(0.2f, 0.3f, 0.3f, 1.0f);

        base.Initialize();
    }

    protected override void LoadContent()
    {

        //_modelData = Geometry.ComputeGeoSphere(1.0f, 5, false, false);
        _modelData = Geometry.ComputeTeapot(2.0f, 5, false, false);

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
            _container = GraphicsDevice.CreateTexture(image.Data.AsSpan(), TextureDescription.Texture2D((uint)image.Width, (uint)image.Height));
        }

        //using (var stream = File.OpenRead("resources/textures/container2_specular.png"))
        //{
        //    ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        //}
    }

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
                Console.WriteLine($"Luna | FPS: {fps:F1}");
                _elapsedSeconds -= 1.0f;
                _frameCount = 0;
            }
        }

        MatrixBufferType mvp = new()
        {
            World = Matrix4x4.Identity,
            View = Matrix4x4.CreateLookAt(new Vector3(.0f, .0f, -5.0f), Vector3.UnitZ, Vector3.UnitY),
            Proj = Matrix4x4.CreatePerspectiveFieldOfView(float.DegreesToRadians(85.0f), 1280.0f / 720.0f, 0.1f, 1000.0f)
        };

        GraphicsDevice.UpdateBuffer(_cb, ref mvp);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetVertexShader(_vs);
        GraphicsDevice.SetPixelShader(_ps);
        GraphicsDevice.SetInputLayout(_inputLayout);
        GraphicsDevice.SetConstantBuffer(_cb, ShaderType.Vertex, 0);
        GraphicsDevice.SetTexture(_container, ShaderType.Pixel, 0);
        GraphicsDevice.SetVertexBuffer(_vb);
        GraphicsDevice.SetIndexBuffer(_ib);
        GraphicsDevice.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
        GraphicsDevice.DrawIndexed(_ib.Count, 0, 0);
    }

    protected override void UnloadContent()
    {
        _inputLayout?.Dispose();
        _ps?.Dispose();
        _cb?.Dispose();
        _vs?.Dispose();
        _ib?.Dispose();
        _vb?.Dispose();
        _container?.Dispose();
        _containerSpecular?.Dispose();
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
