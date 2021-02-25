using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct RenderingSetting {
    [System.Serializable]
    public enum AntiAliasing {
        X1 = 1,
        X2 = 2,
        X4 = 4,
    }

    public static readonly RenderingSetting Default = new RenderingSetting() {allowHDR=true, antiAliasing=AntiAliasing.X2};

    public bool allowHDR;
    public AntiAliasing antiAliasing;

}
