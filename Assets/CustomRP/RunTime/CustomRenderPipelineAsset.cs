using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset {
    public BatchingSetting batchingSetting = BatchingSetting.Default;
    public ShadowSetting shadowSetting = ShadowSetting.Default;

    protected override RenderPipeline CreatePipeline() {
        return new CustomRenderPipeline(ref batchingSetting, ref shadowSetting);
    }
}
