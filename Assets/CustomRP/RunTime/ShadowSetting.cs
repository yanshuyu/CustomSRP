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
        public static readonly Directional Default = new Directional(TextureSize._1024, 4, new Vector3(0.1f, 0.25f, 0.5f));
        
        public TextureSize atlasSize;
        
        [Range(1, 4)]
        public int cascadeCount;

        public Vector3 cascadeSplitRatio;
        
        public Directional(TextureSize atlasSize, int cascadeCount, Vector3 cascadeSplitRatio) {
            this.atlasSize = atlasSize;
            this.cascadeCount = cascadeCount;
            this.cascadeSplitRatio = cascadeSplitRatio;
        }
    }

    [Min(0)] public float maxDistance;
    public Directional directional;

    public ShadowSetting(float maxShadowDistance) {
        this.maxDistance = maxShadowDistance;
        directional = Directional.Default;
    }

}
