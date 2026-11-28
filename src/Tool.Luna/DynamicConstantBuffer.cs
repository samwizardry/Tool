using Tool.Graphics;

namespace Tool.Luna;

public abstract class DynamicConstantBuffer<T>
    where T : unmanaged
{
    protected T _data;
    protected bool _isDirty = true;

    private readonly BufferHandle _buffer;

    protected DynamicConstantBuffer(BufferHandle buffer)
    {
        _buffer = buffer;
    }

    public void Update(GraphicsDevice device)
    {
        if (_isDirty)
        {
            device.UpdateBuffer(_buffer, in _data, 0);
            _isDirty = false;
        }
    }
}
