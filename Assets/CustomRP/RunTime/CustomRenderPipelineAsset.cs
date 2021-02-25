using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline/SRP Asset")]
public class CustomRenderPipelineAsset : RenderPipelineAsset {
    public RenderingSetting renderingSetting = RenderingSetting.Default;
    public BatchingSetting batchingSetting = BatchingSetting.Default;
    public ShadowSetting shadowSetting = ShadowSetting.Default;
    public PostFXSetting postFXSetting = default;

    protected override RenderPipeline CreatePipeline() {
        return new CustomRenderPipeline(ref renderingSetting, ref batchingSetting, ref shadowSetting, ref postFXSetting);
    }
}
