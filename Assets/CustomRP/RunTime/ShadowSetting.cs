using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ShadowSetting {
    public static readonly ShadowSetting Default = new ShadowSetting(100);

    public enum TextureSize {
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 5096,
        _8192 = 8192,
    }

    [System.Serializable]
    public struct Directional {
        public static readonly Directional Default = new Directional(TextureSize._1024);
        
        public TextureSize atlasSize;
        
        public Directional(TextureSize atlasSize) {
            this.atlasSize = atlasSize;
        }
    }

    [Min(0)] public float maxDistance;
    public Directional directional;

    public ShadowSetting(float maxShadowDistance) {
        this.maxDistance = maxShadowDistance;
        directional = Directional.Default;
    }

}
