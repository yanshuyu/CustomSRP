using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline {
    private CameraRenderer _cameraRenderer = new CameraRenderer();

    private BatchingSetting _batchingSetting;
    private ShadowSetting _shadowSetting;

    public CustomRenderPipeline(ref BatchingSetting batchingSetting, ref ShadowSetting shadowSetting) {
        _batchingSetting = batchingSetting;
        _shadowSetting = shadowSetting;
        GraphicsSettings.useScriptableRenderPipelineBatching = _batchingSetting.useSRPBatching;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras) {
        foreach(var camera in cameras) {
            _cameraRenderer.Render(context, camera, ref _batchingSetting, ref _shadowSetting);
        }
    }
}
