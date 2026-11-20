using System.Numerics;

using Torture.Core;
using Torture.Graphics;
using Torture.Input;
using Torture.Logging;

using Vortice.Mathematics;

namespace Torture.Luna;

[SDL3.SDL.GenerateMain]
public sealed partial class Luna : Application<Luna>
{
    public static readonly LogCategory LogCat = new("Luna");

    private readonly Camera _camera;

    // Stats
    private readonly bool _showFpsInTitle = true;
    private float _elapsedSeconds;
    private int _frameCount;

    private readonly List<Actor> _actors = [];
    private PhongPerFrameData _sceneData = default!;

    public Luna()
    {
        Title = "Luna";
        Identifier = "semisoft.torture.luna";
        _camera = new Camera(60.0f, PreferredWindowSize.Width, PreferredWindowSize.Height);
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
                => new Direct3D11.D3D11GraphicsDevice(options, window.WindowHandle, window.ClientSize),
#endif
#if SUPPORT_VULKAN
            //GraphicsAPI.Vulkan
            //    => new VulkanGraphicsDevice(),
#endif
            _ => throw new PlatformNotSupportedException($"{api} is not supported on this platform.")
        };
    }

    protected override void Initialize()
    {
        GraphicsDevice.VSync = true;
        GraphicsDevice.ClearColor = new Color4(0.3f, 0.3f, 0.3f, 1.0f);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        ResourceFactoryCache.Load(GraphicsDevice);

        AssimpModelLoader.GraphicsDevice = GraphicsDevice;

        _sceneData = new PhongPerFrameData(ResourceFactoryCache.PerFrameCb);

        _actors.Add(new Box(GraphicsDevice, ResourceFactoryCache.BoxMaterial));
        _actors.Add(new Car(GraphicsDevice, ResourceFactoryCache.CarMaterial));
        _actors.Add(new Backpack(GraphicsDevice));

        _sceneData.SetDirLight(new DirectionalLight
        {
            Direction = Vector3.Normalize(new Vector3(-0.2f, -1.0f, -0.3f)),
            Ambient = new Vector3(0.1f, 0.1f, 0.1f),
            Diffuse = new Vector3(0.4f, 0.4f, 0.4f),
            Specular = new Vector3(0.5f, 0.5f, 0.5f)
        });

        _sceneData.SetPointLight(new PointLight()
        {
            Position = new Vector3(1.2f, 1.0f, 2.0f),
            Diffuse = new Vector3(0.6f, 0.6f, 0.6f),
            Specular = new Vector3(1.0f, 1.0f, 1.0f),
            Constant = 1.0f,
            Linear = 0.22f,
            Quadratic = 0.20f
        });

        _sceneData.SetSpotLight(new SpotLight()
        {
            Position = _camera.Position,
            Direction = _camera.Direction,
            Diffuse = new Vector3(0.6f, 0.6f, 0.6f),
            Specular = new Vector3(1.0f, 1.0f, 1.0f),
            CutOff = MathF.Cos(float.DegreesToRadians(12.5f)),
            OuterCutOff = MathF.Cos(float.DegreesToRadians(17.5f)),
            Linear = 0.09f,
            Quadratic = 0.032f,
        });
    }

    protected override void Update(GameTime gameTime)
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
                // (LunaCat, $"FPS: {fps:F1}");
                _elapsedSeconds -= 1.0f;
                _frameCount = 0;
            }
        }

        _camera.Update(gameTime.DeltaSeconds);

        if (Keyboard.IsKeyPressed(KeyboardKey.F1))
        {
            //_pipelineState.UseSolid();
        }
        if (Keyboard.IsKeyPressed(KeyboardKey.F2))
        {
            //_pipelineState.UseWireframe();
        }
        if (Keyboard.IsKeyPressed(KeyboardKey.F3))
        {
            //_pipelineState.UseBilinear();
        }
        if (Keyboard.IsKeyPressed(KeyboardKey.F4))
        {
            //_pipelineState.UseAnisotropic();
        }

        _sceneData.SetViewProj(_camera.ViewProj);
        _sceneData.SetViewPos(_camera.Position);
        _sceneData.SetSpotLightPosition(_camera.Position);
        _sceneData.SetSpotLightDirection(_camera.Direction);

        _sceneData.Update(GraphicsDevice);

        foreach (var actor in _actors)
        {
            actor.Update(gameTime.DeltaSeconds);
        }
    }

    protected override void Draw()
    {
        foreach (var actor in _actors)
        {
            actor.Draw();
        }
    }

    protected override void OnResize(int width, int height)
    {
        _camera.OnResize(width, height);
    }

    protected override void UnloadContent()
    {
        AssimpModelLoader.Dispose();

        foreach (var actor in _actors)
        {
            actor?.Dispose();
        }

        ResourceFactoryCache.Unload();
    }

    protected override void Cleanup()
    {
    }
}
