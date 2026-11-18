using System.Numerics;
using System.Runtime.InteropServices;

using Torture.Core;
using Torture.Graphics;
using Torture.Input;

using Vortice.Mathematics;

namespace Torture.Luna;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Material
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
public struct PerFrameData
{
    public Matrix4x4 ViewProj;
    public Light Light;
    public Vector3 ViewPosition;
};

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PerObjectData
{
    public Matrix4x4 Model;
    public Matrix4x4 NormalMatrix;
    public Material Material;
}

[SDL3.SDL.GenerateMain]
public sealed partial class Luna : Application<Luna>
{
    private readonly Camera _camera;

    // Stats
    private readonly bool _showFpsInTitle = true;
    private float _elapsedSeconds;
    private int _frameCount;

    private PipelineState _pipelineState = default!;
    private readonly List<Actor> _actors = [];
    private Model<VertexPositionNormal, uint> _model = default!;
    private PerFrameData _perFrameData = default;

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
        GraphicsDevice.ClearColor = new Color4(0.05f, 0.05f, 0.05f, 1.0f);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _pipelineState = new PipelineState(GraphicsDevice);
        _actors.Add(new Box(GraphicsDevice));

        _perFrameData.Light.DirLight.Direction = Vector3.Normalize(new Vector3(-0.2f, -1.0f, -0.3f));
        _perFrameData.Light.DirLight.Ambient = new Vector3(0.1f, 0.1f, 0.1f);
        _perFrameData.Light.DirLight.Diffuse = new Vector3(0.4f, 0.4f, 0.4f);
        _perFrameData.Light.DirLight.Specular = new Vector3(0.5f, 0.5f, 0.5f);

        _perFrameData.Light.PointLight.Position = new Vector3(1.2f, 1.0f, 2.0f);
        _perFrameData.Light.PointLight.Diffuse = new Vector3(0.6f, 0.6f, 0.6f);
        _perFrameData.Light.PointLight.Specular = new Vector3(1.0f, 1.0f, 1.0f);
        _perFrameData.Light.PointLight.Constant = 1.0f;
        _perFrameData.Light.PointLight.Linear = 0.22f;
        _perFrameData.Light.PointLight.Quadratic = 0.20f;

        _perFrameData.Light.SpotLight.Position = _camera.Position;
        _perFrameData.Light.SpotLight.Direction = _camera.Direction;
        _perFrameData.Light.SpotLight.Diffuse = new Vector3(0.6f, 0.6f, 0.6f);
        _perFrameData.Light.SpotLight.Specular = new Vector3(1.0f, 1.0f, 1.0f);
        _perFrameData.Light.SpotLight.CutOff = MathF.Cos(float.DegreesToRadians(12.5f));
        _perFrameData.Light.SpotLight.OuterCutOff = MathF.Cos(float.DegreesToRadians(17.5f));
        _perFrameData.Light.SpotLight.Linear = 0.09f;
        _perFrameData.Light.SpotLight.Quadratic = 0.032f;

        //_model = Model<VertexPositionNormal, uint>.FromLunaModel(GraphicsDevice, _pipelineState.LunaVS, "resources/objects/car.txt");
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

        _camera.Update(gameTime.DeltaSeconds);

        if (Keyboard.IsKeyPressed(KeyboardKey.F1))
        {
            _pipelineState.UseSolid();
        }
        if (Keyboard.IsKeyPressed(KeyboardKey.F2))
        {
            _pipelineState.UseWireframe();
        }

        if (Keyboard.IsKeyPressed(KeyboardKey.F3))
        {
            _pipelineState.UseBilinear();
        }
        if (Keyboard.IsKeyPressed(KeyboardKey.F4))
        {
            _pipelineState.UseAnisotropic();
        }

        _perFrameData.ViewProj = _camera.ViewProj;
        _perFrameData.ViewPosition = _camera.Position;

        _perFrameData.Light.SpotLight.Position = _camera.Position;
        _perFrameData.Light.SpotLight.Direction = _camera.Direction;

        _pipelineState.SetPerFrameData(ref _perFrameData);

        foreach (var actor in _actors)
        {
            actor.Update(gameTime.DeltaSeconds);
        }
    }

    protected override void Draw()
    {
        _pipelineState.Apply();

        foreach (var actor in _actors)
        {
            actor.Draw();
        }

        //_model.Draw();
    }

    protected override void OnResize(int width, int height)
    {
        _camera.OnResize(width, height);
    }

    protected override void UnloadContent()
    {
        _pipelineState.Dispose();

        foreach (var actor in _actors)
        {
            actor.Dispose();
        }

        _model?.Dispose();
    }

    protected override void Cleanup()
    {
    }
}
