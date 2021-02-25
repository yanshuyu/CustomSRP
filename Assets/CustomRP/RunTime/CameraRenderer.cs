using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer {
    static ShaderTagId UnlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId LitShaderTagId = new ShaderTagId("CustomLit");
    static int IntermediateRT = Shader.PropertyToID("_IntermediateRT");

    private ScriptableRenderContext _context;
    private Camera _camera;
    private CullingResults _cullResults;
    private bool _allowHDR;
    
    private CommandBuffer _cmdBuf = new CommandBuffer();
    private LightManager _lightMgr = new LightManager();
    private PostFXStack _postFXStack = new PostFXStack();

    public void Render(ScriptableRenderContext context, Camera camera, 
                        ref RenderingSetting renderingSetting, 
                        ref BatchingSetting batchingSetting, 
                        ref ShadowSetting shadowSetting,
                        ref PostFXSetting postFXSetting) {
        _context = context;
        _camera = camera;
        _cmdBuf.name = camera.name;
        
        EmitSceneUIGeometry();

        if (!Cull(ref shadowSetting))
            return;
        
        _postFXStack.SetUp(context, camera, ref postFXSetting, renderingSetting.allowHDR);
        SetUp(ref renderingSetting, ref shadowSetting);

        DrawVisibleGeometry(ref batchingSetting);
        DrawUnsupportedShaders();
        DrawGizmos();

        if (_postFXStack.isActive) {
            _postFXStack.Render(IntermediateRT);
        }

        Submit();
    }

    private void SetUp(ref RenderingSetting renderingSetting, ref ShadowSetting shadowSetting) {
        _cmdBuf.BeginSample(_cmdBuf.name);
        ExecuteCommandBuffer();
        _lightMgr.SetUp(ref _context, ref _cullResults, ref shadowSetting);
        _cmdBuf.EndSample(_cmdBuf.name);

        _context.SetupCameraProperties(_camera);
        SetUpIntermediateRT(ref renderingSetting);
 
        CameraClearFlags clearFlags = _camera.clearFlags;
        _cmdBuf.ClearRenderTarget(clearFlags <= CameraClearFlags.Depth, clearFlags <= CameraClearFlags.Color, clearFlags <= CameraClearFlags.Color ? _camera.backgroundColor.linear : Color.clear);
        _cmdBuf.BeginSample(_cmdBuf.name);
        ExecuteCommandBuffer();
    }

    private void DrawVisibleGeometry(ref BatchingSetting batchingSetting) {
        var sortSettings = new SortingSettings(_camera) { criteria = SortingCriteria.CommonOpaque };
        var drawSettings = new DrawingSettings();
        var filterSettings = new FilteringSettings(RenderQueueRange.opaque);
        
        drawSettings.SetShaderPassName(0, UnlitShaderTagId);
        drawSettings.SetShaderPassName(1, LitShaderTagId);
        drawSettings.perObjectData = PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightProbeProxyVolume 
                                    | PerObjectData.ShadowMask | PerObjectData.OcclusionProbe | PerObjectData.OcclusionProbeProxyVolume
                                    | PerObjectData.ReflectionProbes;
        drawSettings.sortingSettings = sortSettings;
        drawSettings.enableDynamicBatching = batchingSetting.useDynamicBatching;
        drawSettings.enableInstancing = batchingSetting.useGPUInstancing;

        _context.DrawRenderers(_cullResults, ref drawSettings,  ref filterSettings); // draw opaques
        
        _context.DrawSkybox(_camera);

        filterSettings.renderQueueRange = RenderQueueRange.transparent;
        sortSettings.criteria = SortingCriteria.CommonTransparent;
        drawSettings.sortingSettings = sortSettings;
        _context.DrawRenderers(_cullResults, ref drawSettings, ref filterSettings); // draw transparent
    }


    private void Submit() {
        _cmdBuf.EndSample(_cmdBuf.name);
        ExecuteCommandBuffer();
       
        _lightMgr.CleanUp();
        _postFXStack.CleanUp();

        CleanUpIntermediateRT();
       
        _context.Submit();
    }


    private bool Cull(ref ShadowSetting shadowSetting) {
        if (_camera.TryGetCullingParameters(out ScriptableCullingParameters cullParas)) {
            cullParas.shadowDistance = Mathf.Min(shadowSetting.maxDistance, _camera.farClipPlane);
            _cullResults = _context.Cull(ref cullParas);
            return true;
        }

        return false;
    }


    private void ExecuteCommandBuffer() {
        _context.ExecuteCommandBuffer(_cmdBuf);
        _cmdBuf.Clear();
    }


    partial void DrawUnsupportedShaders();

    partial void DrawGizmos();

    partial void EmitSceneUIGeometry();

    void SetUpIntermediateRT(ref RenderingSetting renderingSetting) {
        if (!_postFXStack.isActive)
            return;

        _cmdBuf.GetTemporaryRT(IntermediateRT, _camera.pixelWidth, _camera.pixelHeight, 24, FilterMode.Bilinear, renderingSetting.allowHDR && _camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default, RenderTextureReadWrite.sRGB, (int)renderingSetting.antiAliasing);
        _cmdBuf.SetRenderTarget(IntermediateRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        ExecuteCommandBuffer();
    }

    void CleanUpIntermediateRT() {
        if (!_postFXStack.isActive)
            return;

        _cmdBuf.ReleaseTemporaryRT(IntermediateRT);
        ExecuteCommandBuffer();
    }

}
