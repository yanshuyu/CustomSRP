using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer : MonoBehaviour {
    static ShaderTagId UnlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId LitShaderTagId = new ShaderTagId("CustomLit");

    private Camera _camera;
    private CommandBuffer _cmdBuf = new CommandBuffer();

    private ScriptableRenderContext _context;
    private CullingResults _cullResults;
    
    private LightManager _lightMgr = new LightManager();

    public void Render(ScriptableRenderContext context, Camera camera, ref BatchingSetting batchingSetting ) {
        _context = context;
        _camera = camera;
        
        EmitSceneUIGeometry();

        if (!Cull())
            return;

        SetUp();
        _lightMgr.SetUp(ref _cullResults, ref _context);
        DrawVisibleGeometry(ref batchingSetting);
        DrawUnsupportedShaders();
        DrawGizmos();
        Flush();
    }

    private void SetUp() {
        CameraClearFlags clearFlags = _camera.clearFlags;
        _cmdBuf.name = _camera.name;
        _context.SetupCameraProperties(_camera);
        _cmdBuf.ClearRenderTarget(clearFlags <= CameraClearFlags.Depth, clearFlags <= CameraClearFlags.Color, clearFlags <= CameraClearFlags.Color ? _camera.backgroundColor.linear : Color.clear);
        _cmdBuf.BeginSample(_cmdBuf.name);
        ExecuteCmdBuffer();
    }

    private void DrawVisibleGeometry(ref BatchingSetting batchingSetting) {
        var sortSettings = new SortingSettings(_camera) { criteria = SortingCriteria.CommonOpaque };
        var drawSettings = new DrawingSettings();
        var filterSettings = new FilteringSettings(RenderQueueRange.opaque);
        
        drawSettings.SetShaderPassName(0, UnlitShaderTagId);
        drawSettings.SetShaderPassName(1, LitShaderTagId);
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


    private void Flush() {
        _cmdBuf.EndSample(_cmdBuf.name);
        ExecuteCmdBuffer();
        _context.Submit();
    }


    private bool Cull() {
        if (_camera.TryGetCullingParameters(out ScriptableCullingParameters cullParas)) {
            _cullResults = _context.Cull(ref cullParas);
            return true;
        }

        return false;
    }


    private void ExecuteCmdBuffer() {
        _context.ExecuteCommandBuffer(_cmdBuf);
        _cmdBuf.Clear();
    }


    partial void DrawUnsupportedShaders();

    partial void DrawGizmos();

    partial void EmitSceneUIGeometry();

}
