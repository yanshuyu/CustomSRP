using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct ShadowSetting {
    public static readonly ShadowSetting Default = new ShadowSetting(100, 0.1f);

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

    [System.Serializable]
    public struct Point {
        public static readonly Point Default = new Point(TextureSize._1024);

        public TextureSize atlasSize;

        public Point(TextureSize atlasSize) {
            this.atlasSize = atlasSize;
        }
    }

    [System.Serializable]
    public struct Spot
    {
        public static readonly Spot Default = new Spot(TextureSize._1024);

        public TextureSize atlasSize;

        public Spot(TextureSize atlasSize)
        {
            this.atlasSize = atlasSize;
        }
    }

    [Min(0)] public float maxDistance;
    [Range(0.001f, 1)] public float fadeDistanceRatio;
    public Directional directional;
    public Point point;
    public Spot spot;

    public ShadowSetting(float maxShadowDistance, float fadeDistanceRatio) {
        this.maxDistance = maxShadowDistance;
        this.fadeDistanceRatio = Mathf.Clamp(fadeDistanceRatio, 0.001f, 1f);
        directional = Directional.Default;
        point = Point.Default;
        spot = Spot.Default;
    }

}
