using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline/Post FX Setting")]
public class PostFXSetting : ScriptableObject {
    [System.Serializable]
    public struct Bloom {
        [Min(1)] public int iteration;
        [Range(0, 10)] public float threshold;
        [Range(0, 5)] public float strength;
    }

    [System.Serializable]
    public struct ToneMapping {
        public enum Mode {
            None,
            Reinhard,
            Neutral,
            ACES,
        }

        public Mode mode;
    }

    [System.Serializable]
    public struct ColorAjustment {
        [Range(-10, 10)] public float exposure;
        [Range(-100,100)] public float contrast;
        [Range(-100, 100)] public float saturation;
        [Range(-180, 180)] public float hue;
        [ColorUsage(false, true)] public Color filter;
    }

    [System.Serializable]
    public struct WhiteBalance {
        [Range(-100, 100)] public float temperature;
        [Range(-100, 100)] public float tint;
    }

    [System.Serializable]
    public struct SplitTone {
        [ColorUsage(false)]
        public Color shadow, highLight;
        [Range(-100, 100)]
        public float balance;
    }

    [SerializeField]
    private Shader fxShader;

    private Material _fxMaterial;
    [HideInInspector] public Material fxMaterial {
        get {
            if (fxShader == null) {
                _fxMaterial = null;
                return null;
            }

            if (_fxMaterial == null) {
                _fxMaterial = new Material(fxShader);
                _fxMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            return _fxMaterial;
        }
    }

    public static readonly string BloomThresholdPass = "BloomThreshold";
    public static readonly string BloomDownSamplingPass = "BloomDownSampling";
    public static readonly string BloomUpSamplingPass = "BloomUpSampling";
    public static readonly string BloomAddPass = "BloomAdd";
    public static readonly string ToneMappingReinhardPass = "ToneMappingReinhard";
    public static readonly string ToneMappingNeutralPass = "ToneMappingNeutral";
    public static readonly string ToneMappingACESPass = "ToneMappingACES";
    public static readonly string ColorAjustmentPass = "ColorAjustment";
    
    public Bloom bloom = new Bloom() {
        iteration = 4,
        threshold = 1,
        strength = 1,
    };

    public ColorAjustment colorAjustment = new ColorAjustment() {
        exposure = 0,
        contrast = 0,
        saturation = 0,
        hue = 0,
        filter = Color.white,
    };

    public WhiteBalance whiteBalance = default;

    public SplitTone splitTone = new SplitTone() {
        shadow = Color.gray,
        highLight = Color.gray,
        balance = 0,
    };

    public ToneMapping toneMapping = new ToneMapping() {
        mode = ToneMapping.Mode.Neutral
    };


    public int GetPass(string name) {
        return fxMaterial ? fxMaterial.FindPass(name) : -1;
    }

}
