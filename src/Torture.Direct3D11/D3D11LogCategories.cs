namespace Torture.Direct3D11;

public static class D3D11LogCategories
{
    public static readonly CoreLogCategory D3D11 = new("D3D11", CoreLogSeverity.Warning);
    public static readonly CoreLogCategory Dxgi = new("DXGI");
    public static readonly CoreLogCategory SharpGen = new("SharpGen");
}
