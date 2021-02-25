using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline {
    private CameraRenderer _cameraRenderer = new CameraRenderer();

    private BatchingSetting _batchingSetting;
    private ShadowSetting _shadowSetting;
    private RenderingSetting _renderingSetting;
    private PostFXSetting _postFXSetting;

    public CustomRenderPipeline(ref RenderingSetting renderingSetting, ref BatchingSetting batchingSetting, ref ShadowSetting shadowSetting, ref PostFXSetting postFXSetting) {
        _batchingSetting = batchingSetting;
        _shadowSetting = shadowSetting;
        _renderingSetting = renderingSetting;
        _postFXSetting = postFXSetting;
        GraphicsSettings.useScriptableRenderPipelineBatching = _batchingSetting.useSRPBatching;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
        foreach(var camera in cameras) {
            _cameraRenderer.Render(context, camera, ref _renderingSetting,ref _batchingSetting, ref _shadowSetting, ref _postFXSetting);
        }
    }
}
