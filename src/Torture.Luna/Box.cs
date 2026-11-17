using System.Numerics;

using StbImageSharp;

using Torture.Core.Collections;
using Torture.Core.Graphics;

namespace Torture.Luna;

public class Box : Actor
{
    private readonly Core.Graphics.Buffer _vb = default!;
    private readonly Core.Graphics.Buffer _ib = default!;
    private readonly Texture _container = default!;
    private readonly Texture _containerSpecular = default!;

    public Box(GraphicsDevice graphicsDevice)
        : base(graphicsDevice)
    {
        var modelData = Geometry.ComputeBox(Vector3.One * 2.0f);

        _vb = graphicsDevice.CreateBuffer(modelData.Vertices.AsSpan(), BufferDescription.Vertex(0, 0));
        _ib = graphicsDevice.CreateBuffer(modelData.Indices.AsSpan(), BufferDescription.Index(0));

        using (var stream = File.OpenRead("resources/textures/container2.png"))
        {
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            _container = graphicsDevice.CreateTexture(
                image.Data.AsSpan(),
                TextureDescription.Texture2D((uint)image.Width, (uint)image.Height, mipLevels: 0));
        }

        using (var stream = File.OpenRead("resources/textures/container2_specular.png"))
        {
            ImageResult image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            _containerSpecular = graphicsDevice.CreateTexture(
                image.Data.AsSpan(),
                TextureDescription.Texture2D((uint)image.Width, (uint)image.Height, mipLevels: 0));
        }

        _perObjectData.Material = new()
        {
            Shininess = 32.0f
        };
    }

    float _angle = 0.0f;

    public override void Update(float dt)
    {
        //_angle += 30.0f * dt;
        _perObjectData.Model = Matrix4x4.CreateRotationY(float.DegreesToRadians(_angle));
        
        if (Matrix4x4.Invert(_perObjectData.Model, out var inverseModel))
        {
            inverseModel.Translation = Vector3.Zero;
            _perObjectData.NormalMatrix = Matrix4x4.Transpose(inverseModel);
        }
        else
        {
            _perObjectData.NormalMatrix = Matrix4x4.Identity;
        }
    }

    public override void Draw()
    {
        base.Draw();
        _graphicsDevice.SetTexture(_container, ShaderType.Pixel, 0);
        _graphicsDevice.SetTexture(_containerSpecular, ShaderType.Pixel, 1u);
        _graphicsDevice.SetVertexBuffer(_vb);
        _graphicsDevice.SetIndexBuffer(_ib);
        _graphicsDevice.DrawIndexed(_ib.Count, 0, 0);
    }

    public override void Dispose()
    {
        if (_disposed)
            return;

        _vb?.Dispose();
        _ib?.Dispose();
        _container?.Dispose();
        _containerSpecular?.Dispose();

        base.Dispose();

        _disposed = true;
    }
}
