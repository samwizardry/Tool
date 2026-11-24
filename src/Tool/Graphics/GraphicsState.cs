// Portions of this file are derived from NVRHI (https://github.com/NVIDIA-RTX/NVRHI)
// Copyright (c) 2014-2026 NVIDIA Corporation. All rights reserved.
// Licensed under the MIT License. See THIRD-PARTY-NOTICES.md in the project root for license information.

namespace Tool.Graphics;

public readonly record struct VertexBufferBinding(
    BufferHandle Buffer,
    uint Slot = 0,
    uint ByteOffset = 0);

public readonly record struct IndexBufferBinding(
    BufferHandle Buffer,
    uint ByteOffset = 0);

public record struct BindingSetVector
{
    public BindingSetHandle Set0;
    public BindingSetHandle Set1;
    public BindingSetHandle Set2;
    public BindingSetHandle Set3;
    //public BindingSetHandle Set4;
    //public BindingSetHandle Set5;
    //public BindingSetHandle Set6;
    //public BindingSetHandle Set7;

    public BindingSetVector(
        BindingSetHandle set0 = default,
        BindingSetHandle set1 = default,
        BindingSetHandle set2 = default,
        BindingSetHandle set3 = default)
        //BindingSetHandle set4 = default,
        //BindingSetHandle set5 = default,
        //BindingSetHandle set6 = default,
        //BindingSetHandle set7 = default)
    {
        Set0 = set0;
        Set1 = set1;
        Set2 = set2;
        Set3 = set3;
        //Set4 = set4;
        //Set5 = set5;
        //Set6 = set6;
        //Set7 = set7;
    }

    public BindingSetHandle this[int index]
    {
        readonly get => index switch
        {
            0 => Set0,
            1 => Set1,
            2 => Set2,
            3 => Set3,
            //4 => Set4,
            //5 => Set5,
            //6 => Set6,
            //7 => Set7,
            _ => throw new IndexOutOfRangeException(),
        };
        set
        {
            switch (index)
            {
                case 0: Set0 = value; break;
                case 1: Set1 = value; break;
                case 2: Set2 = value; break;
                case 3: Set3 = value; break;
                //case 4: Set4 = value; break;
                //case 5: Set5 = value; break;
                //case 6: Set6 = value; break;
                //case 7: Set7 = value; break;
                default: throw new IndexOutOfRangeException();
            }
        }
    }
}

public readonly record struct GraphicsState(
    GraphicsPipelineHandle Pipeline,
    BindingSetVector Bindings = default,
    VertexBufferBinding VertexBuffer = default,
    IndexBufferBinding IndexBuffer = default);
