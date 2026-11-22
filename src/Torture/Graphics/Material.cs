namespace Torture.Graphics;

public readonly record struct Material(
    GraphicsPipelineHandle Pipeline,
    BindingSetHandle BindingSet = default,
    BufferHandle ConstantBuffer = default);
