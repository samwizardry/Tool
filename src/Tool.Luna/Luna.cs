using System.Numerics;

using Tool.Assets;
using Tool.Collections;
using Tool.Core;
using Tool.Graphics;
using Tool.Input;
using Tool.Logging;

using Vortice.Mathematics;

namespace Tool.Luna;

[SDL3.SDL.GenerateMain]
public sealed partial class Luna : Application<Luna>
{
    public static readonly LogCategory LogCat = new("Luna");

    private Camera _camera = null!;

    // Stats
    private readonly bool _showFpsInTitle = true;
    private float _elapsedSeconds;
    private int _frameCount;

    // Scene
    AssetScope _globalScope = null!;
    AssetScope _sceneScope = null!;

    private readonly List<Actor> _actors = [];

    public Luna()
    {
        Title = "Luna";
        Identifier = "semisoft.tool.luna";
    }

    protected override GraphicsDevice CreateGraphicsDevice(GraphicsAPI api, NativeWindowInfo window)
    {
        if (api == GraphicsAPI.Default)
        {
#if SUPPORT_DIRECTX
            api = GraphicsAPI.D3D11;
#elif SUPPORT_VULKAN
            api = GraphicsAPI.Vulkan;
#endif
        }

        return api switch
        {
#if SUPPORT_DIRECTX
            GraphicsAPI.D3D11
                => new Direct3D11.D3D11GraphicsDevice(window.WindowHandle, window.ClientSize),
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

        AssetManager.RegisterShaderLoader(new FileShaderLoader());
        AssetManager.RegisterTextureLoader(new StbTextureLoader());

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _globalScope = new AssetScope(AssetManager);
        _sceneScope = new AssetScope(AssetManager);

        _camera = new Camera(60.0f, PreferredWindowSize.Width, PreferredWindowSize.Height);

        Resources.Load(GraphicsDevice, _globalScope);
        Phong.Load(GraphicsDevice, _globalScope);
        InfGrid.Load(GraphicsDevice, _globalScope);

        // Create box
        var data = Geometry.ComputeBox(Vector3.One * 2.0f);
        var mesh = Mesh.Create(GraphicsDevice,
            GraphicsDevice.CreateBuffer(data.Vertices.AsSpan(), BufferDescription.Vertex(0, 0)),
            GraphicsDevice.CreateBuffer(data.Indices.AsSpan(), BufferDescription.Index(0)),
            IndexFormat.UInt16);

        _actors.Add(new Box(GraphicsDevice, _sceneScope.CreateModel(mesh, Phong.BoxMaterial)));

        // Create backpack
        AssetManager.RegisterModelLoader(new AssimpModelLoader());
        _actors.Add(new Backpack(GraphicsDevice, _sceneScope.LoadModel("resources/objects/backpack/backpack.obj")));

        // Create car
        AssetManager.RegisterModelLoader(new LunaModelLoader());
        _actors.Add(new Car(GraphicsDevice, _sceneScope.LoadModel("resources/objects/car.txt")));
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
                //Log.Info(LogCat, $"FPS: {fps:F1}");
                _elapsedSeconds -= 1.0f;
                _frameCount = 0;
            }
        }

        _camera.Update(gameTime.DeltaSeconds);

        //_gridPerFrameData.SetViewProj(_camera.ViewProj);
        //_gridPerFrameData.SetCamWorldPos(_camera.Position);
        //_gridPerFrameData.Update(GraphicsDevice);

        Phong.PerFrame.SetViewProj(_camera.ViewProj);
        Phong.PerFrame.SetViewPos(_camera.Position);
        Phong.PerFrame.SetSpotLightPosition(_camera.Position);
        Phong.PerFrame.SetSpotLightDirection(_camera.Direction);

        Phong.PerFrame.Update(GraphicsDevice);

        foreach (var actor in _actors)
        {
            actor.Update(gameTime.DeltaSeconds);
        }
    }

    protected override void Draw()
    {
        //GraphicsDevice.SetGraphicsState(new(ResourceFactoryCache.InfGridPipeline, Bindings: new(set0: ResourceFactoryCache.InfGridPerFrameBindSet)));
        //GraphicsDevice.Draw(6);

        foreach (var actor in _actors)
        {
            actor.Draw(AssetManager);
        }
    }

    protected override void OnResize(int width, int height)
    {
        _camera.OnResize(width, height);
    }

    protected override void UnloadContent()
    {
        foreach (var actor in _actors)
        {
            actor?.Dispose();
        }

        Phong.Unload(GraphicsDevice);
        InfGrid.Unload(GraphicsDevice);
        _sceneScope.Dispose();
        _globalScope.Dispose();
        Resources.Unload(GraphicsDevice);
    }

    protected override void Cleanup()
    {
    }
}
