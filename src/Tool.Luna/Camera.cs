using System.Numerics;

using Tool.Input;

namespace Tool.Luna;

public class Camera
{
    private Matrix4x4 _view;
    private Matrix4x4 _proj;
    private Matrix4x4 _viewProj;
    private Vector3 _position = new(0.0f, 2.0f, 5.0f);
    private float _yaw;
    private float _pitch;
    private Vector3 _right = Vector3.UnitX;
    private Vector3 _front = -Vector3.UnitZ;
    private Vector3 _up = Vector3.UnitY;

    private float _fov;
    private float _nearPlane;
    private float _farPlane;

    public Camera(float fov, int width, int height, float nearPlane = 0.1f, float farPlane = 1000.0f)
    {
        _fov = fov;
        Width = width;
        Height = height;
        _nearPlane = nearPlane;
        _farPlane = farPlane;
        CalculateProjectionMatrix();
    }

    public ref readonly Matrix4x4 Proj => ref _proj;
    public ref readonly Matrix4x4 View => ref _view;
    public ref readonly Matrix4x4 ViewProj => ref _viewProj;
    public Vector3 Position => _position;
    public Vector3 Direction => _front;

    public float Speed { get; set; } = 10.0f;
    public float Sensetivity { get; set; } = 5f;

    public int Width { get; private set; }
    public int Height { get; private set; }

    public float Fov
    {
        get => _fov;
        set
        {
            _fov = value;
            CalculateProjectionMatrix();
        }
    }

    public float NearPlane
    {
        get => _nearPlane;
        set
        {
            _nearPlane = value;
            CalculateProjectionMatrix();
        }
    }

    public float FarPlane
    {
        get => _farPlane;
        set
        {
            _farPlane = value;
            CalculateProjectionMatrix();
        }
    }

    public void Update(float dt)
    {
        // Move
        Vector3 dir = Vector3.Zero;

        if (Keyboard.IsKeyDown(KeyboardKey.W))
        {
            dir += _front;
        }
        if (Keyboard.IsKeyDown(KeyboardKey.S))
        {
            dir -= _front;
        }
        if (Keyboard.IsKeyDown(KeyboardKey.A))
        {
            dir -= _right;
        }
        if (Keyboard.IsKeyDown(KeyboardKey.D))
        {
            dir += _right;
        }

        _position += dir * Speed * dt;

        // Look
        if (Mouse.IsButtonPressed(MouseButton.Right))
        {
            Mouse.IsRelativeMode = true;
        }
        if (Mouse.IsButtonReleased(MouseButton.Right))
        {
            Mouse.IsRelativeMode = false;
        }

        if (Mouse.IsRelativeMode)
        {
            var delta = Mouse.Delta * Sensetivity * 0.022f;
            _yaw += delta.X;
            _pitch -= delta.Y;
            _pitch = float.Clamp(_pitch, -89.0f, 89.0f);
        }

        (float sinYaw, float cosYaw) = MathF.SinCos(float.DegreesToRadians(_yaw));
        (float sinPitch, float cosPitch) = MathF.SinCos(float.DegreesToRadians(_pitch));

        _front = Vector3.Normalize(new(sinYaw * cosPitch, sinPitch, -cosYaw * cosPitch));

        _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitY));
        _up = Vector3.Normalize(Vector3.Cross(_right, _front));

        _view = Matrix4x4.CreateLookAt(_position, _position + _front, _up);
        _viewProj = _view * _proj;
    }

    public void OnResize(int width, int height)
    {
        Width = width;
        Height = height;
        CalculateProjectionMatrix();
    }

    private void CalculateProjectionMatrix()
    {
        _proj = Matrix4x4.CreatePerspectiveFieldOfView(float.DegreesToRadians(_fov), (float)Width / (float)Height, _nearPlane, _farPlane);
        _viewProj = _view * _proj;
    }
}
