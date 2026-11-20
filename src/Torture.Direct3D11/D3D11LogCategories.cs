using Torture.Logging;

namespace Torture.Direct3D11;

public static class D3D11LogCategories
{
    public static readonly LogCategory D3D11 = new("D3D11", LogSeverity.Warning);
    public static readonly LogCategory Dxgi = new("DXGI");
    public static readonly LogCategory SharpGen = new("SharpGen");
}
