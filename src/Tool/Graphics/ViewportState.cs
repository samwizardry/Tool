using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Vortice.Mathematics;

namespace Tool.Graphics;

[InlineArray(Constants.MaxViewports)]
public struct ViewportArray : IEquatable<ViewportArray>
{
    private Viewport _viewport0;

    public ViewportArray(params ReadOnlySpan<Viewport> viewports)
    {
        if (viewports.Length > Constants.MaxViewports)
        {
            throw new ArgumentOutOfRangeException(nameof(viewports));
        }

        for (int i = 0; i < viewports.Length; i++)
        {
            this[i] = viewports[i];
        }
    }

    public readonly bool Equals(ViewportArray other)
    {
        for (int i = 0; i < Constants.MaxViewports; i++)
        {
            if (this[i] != other[i])
            {
                return false;
            }
        }

        return true;
    }

    public override readonly bool Equals([NotNullWhen(true)] object? obj) =>
        obj is ViewportArray other && Equals(other);

    public override readonly int GetHashCode()
    {
        HashCode hash = default;

        for (int i = 0; i < Constants.MaxViewports; i++)
        {
            hash.Add(this[i]);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(ViewportArray left, ViewportArray right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ViewportArray left, ViewportArray right)
    {
        return !(left == right);
    }
}

[InlineArray(Constants.MaxViewports)]
public struct ScissorRectArray : IEquatable<ScissorRectArray>
{
    private RectI _scissorRect0;

    public ScissorRectArray(params ReadOnlySpan<RectI> scissorRects)
    {
        if (scissorRects.Length > Constants.MaxViewports)
        {
            throw new ArgumentOutOfRangeException(nameof(scissorRects));
        }

        for (int i = 0; i < scissorRects.Length; i++)
        {
            this[i] = scissorRects[i];
        }
    }

    public readonly bool Equals(ScissorRectArray other)
    {
        for (int i = 0; i < Constants.MaxViewports; i++)
        {
            if (this[i] != other[i])
            {
                return false;
            }
        }

        return true;
    }

    public override readonly bool Equals([NotNullWhen(true)] object? obj) =>
        obj is ScissorRectArray other && Equals(other);

    public override readonly int GetHashCode()
    {
        HashCode hash = default;

        for (int i = 0; i < Constants.MaxViewports; i++)
        {
            hash.Add(this[i]);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(ScissorRectArray left, ScissorRectArray right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ScissorRectArray left, ScissorRectArray right)
    {
        return !(left == right);
    }
}

public readonly record struct ViewportState : IEquatable<ViewportState>
{
    public readonly ViewportArray Viewports;
    public readonly ScissorRectArray ScissorRects;
    public readonly byte ViewportCount;
    public readonly byte ScissorRectCount;

    public ViewportState(ReadOnlySpan<Viewport> viewports, ReadOnlySpan<RectI> scissorRects = default)
    {
        Viewports = new ViewportArray(viewports);
        ScissorRects = new ScissorRectArray(scissorRects);
        ViewportCount = (byte)viewports.Length;
        ScissorRectCount = (byte)scissorRects.Length;
    }

    public ViewportState(in Viewport viewport)
    {
        Viewports = new ViewportArray(viewport);
        ScissorRects = default;
        ViewportCount = 1;
        ScissorRectCount = 0;
    }

    public bool Equals(ViewportState other)
    {
        if (ViewportCount != other.ViewportCount || ScissorRectCount != other.ScissorRectCount)
            return false;

        for (int i = 0; i < ViewportCount; i++)
            if (Viewports[i] != other.Viewports[i])
                return false;

        for (int i = 0; i < ScissorRectCount; i++)
            if (ScissorRects[i] != other.ScissorRects[i])
                return false;

        return true;
    }

    public bool Equals(in ViewportState other)
    {
        if (ViewportCount != other.ViewportCount || ScissorRectCount != other.ScissorRectCount)
            return false;

        for (int i = 0; i < ViewportCount; i++)
            if (Viewports[i] != other.Viewports[i])
                return false;

        for (int i = 0; i < ScissorRectCount; i++)
            if (ScissorRects[i] != other.ScissorRects[i])
                return false;

        return true;
    }

    public override int GetHashCode()
    {
        HashCode hash = default;

        hash.Add(ViewportCount);
        hash.Add(ScissorRectCount);

        for (int i = 0; i < ViewportCount; i++)
            hash.Add(Viewports[i]);

        for (int i = 0; i < ScissorRectCount; i++)
            hash.Add(ScissorRects[i]);

        return hash.ToHashCode();
    }
}
