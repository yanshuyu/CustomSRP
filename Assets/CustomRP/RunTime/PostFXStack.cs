using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class PostFXStack {
    static readonly string CmdBufferName = "Post FX";
    static readonly int BloomThresholdPassRT = Shader.PropertyToID(PostFXSetting.BloomThresholdPass);
    static readonly int ToneMappingPassRT = Shader.PropertyToID("ToneMapping");
    static readonly int ColorAjustmentPassRT = Shader.PropertyToID(PostFXSetting.ColorAjustmentPass);
    
    private ScriptableRenderContext _srContext;
    private Camera _camera;
    private CommandBuffer _cmdBuffer = new CommandBuffer(){ name = CmdBufferName};
    
    private PostFXSetting _postFXSetting;
    
    public bool isActive {get; private set;}

    private bool _allowHDR;
    private bool _useHDR {
        get {
            return _allowHDR && _camera.allowHDR;
        }
    }

    public void SetUp(ScriptableRenderContext srContext, Camera camera, ref PostFXSetting postFXSetting, bool allowHDR) {
         _srContext = srContext;
        _camera = camera;
        _postFXSetting = postFXSetting;
        _allowHDR = allowHDR;
        isActive = true;

        if (postFXSetting == null || postFXSetting.fxMaterial == null) {
            isActive = false;
        }

        if (camera.cameraType != CameraType.SceneView && camera.cameraType != CameraType.Game) {
            isActive = false;
        }

        #if UNITY_EDITOR
        if (camera.cameraType == CameraType.SceneView && !SceneView.currentDrawingSceneView.sceneViewState.showImageEffects ) {
            isActive = false;
        }
        #endif
    }

    public void Render(int srcRT) {
        if (!isActive)
            return;

        if (_postFXSetting.fxMaterial == null) {
            DoCopy(srcRT, BuiltinRenderTextureType.CameraTarget);
            return;
        }

        int processedRT = srcRT;

        if (_postFXSetting.GetPass(PostFXSetting.BloomThresholdPass) >= 0 && 
            _postFXSetting.GetPass(PostFXSetting.BloomDownSamplingPass) >= 0 &&
            _postFXSetting.GetPass(PostFXSetting.BloomUpSamplingPass) >= 0 &&
            _postFXSetting.GetPass(PostFXSetting.BloomAddPass) >= 0 &&
            _postFXSetting.bloom.strength > 0 ) {
            int bloomedRT = DoBloom(processedRT);
            if (processedRT != srcRT) {
                _cmdBuffer.ReleaseTemporaryRT(processedRT);
                ExecuteCommandBuffer();
            }
            processedRT = bloomedRT;
        }

        if (_postFXSetting.GetPass(PostFXSetting.ColorAjustmentPass) >= 0) {
            int colAjustedRT = DoColorAjustments(processedRT);
            if (processedRT != srcRT){
                _cmdBuffer.ReleaseTemporaryRT(processedRT);
                ExecuteCommandBuffer();
            }
            processedRT = colAjustedRT;
        }

        if (_postFXSetting.toneMapping.mode != PostFXSetting.ToneMapping.Mode.None && _useHDR) {
            int toneMapedRT = DoToneMapping(processedRT);
            if (processedRT != srcRT) {
                _cmdBuffer.ReleaseTemporaryRT(processedRT);
                ExecuteCommandBuffer();
            }
            processedRT = toneMapedRT;
        }

        DoCopy(processedRT, BuiltinRenderTextureType.CameraTarget);

        if (processedRT != srcRT) {
            _cmdBuffer.ReleaseTemporaryRT(processedRT);
            ExecuteCommandBuffer();
        }
    }


    public void CleanUp() {

    }

    void ExecuteCommandBuffer() {
        _srContext.ExecuteCommandBuffer(_cmdBuffer);
        _cmdBuffer.Clear();
    }

    void DoCopy(RenderTargetIdentifier src, RenderTargetIdentifier dst) {
        _cmdBuffer.Blit(src, dst);
        ExecuteCommandBuffer();
    }

    int DoBloom(RenderTargetIdentifier src) {
        _cmdBuffer.BeginSample("Bloom");
        // threshold pass
        _cmdBuffer.GetTemporaryRT(BloomThresholdPassRT,
                                 _camera.pixelWidth, _camera.pixelHeight, 16, 
                                 FilterMode.Bilinear,
                                _useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
        _cmdBuffer.SetRenderTarget(BloomThresholdPassRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        _postFXSetting.fxMaterial.SetFloat("_BloomThreshold", _postFXSetting.bloom.threshold);
        _cmdBuffer.Blit(src, BloomThresholdPassRT, _postFXSetting.fxMaterial, _postFXSetting.GetPass(PostFXSetting.BloomThresholdPass));
        ExecuteCommandBuffer();

        // progressive down sampling passes
        int width = _camera.pixelWidth / 2;
        int height = _camera.pixelHeight / 2;
        int curDownSampleSrcRT = BloomThresholdPassRT;
        int curIteration = 0;
        int[] bloomSampleRTs = new int[_postFXSetting.bloom.iteration];
        for (; curIteration < _postFXSetting.bloom.iteration; curIteration++) {
            if (width <= 1 || height <= 1)
                break;

            int curDownSampleDstRT = Shader.PropertyToID(string.Format(PostFXSetting.BloomDownSamplingPass + "{0}X{1}", width, height));
            bloomSampleRTs[curIteration] = curDownSampleDstRT;

            _cmdBuffer.GetTemporaryRT(curDownSampleDstRT, 
                                        width, height, 16, 
                                        FilterMode.Bilinear,
                                        _useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            _cmdBuffer.SetRenderTarget(curDownSampleDstRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            _cmdBuffer.Blit(curDownSampleSrcRT, curDownSampleDstRT, _postFXSetting.fxMaterial, _postFXSetting.GetPass(PostFXSetting.BloomDownSamplingPass));
            ExecuteCommandBuffer();

            width /= 2;
            height /= 2;
            curDownSampleSrcRT = curDownSampleDstRT; 
        }

        int actualIteration = curIteration;

        // progressive up sampling passes
        while(--curIteration > 0) {
            int upSampleRT = bloomSampleRTs[curIteration - 1];
            _cmdBuffer.SetRenderTarget(upSampleRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            _cmdBuffer.Blit(bloomSampleRTs[curIteration], upSampleRT, _postFXSetting.fxMaterial, _postFXSetting.GetPass(PostFXSetting.BloomUpSamplingPass));
        }

        // apply final addtive bloom
        _cmdBuffer.SetRenderTarget(BloomThresholdPassRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        _postFXSetting.fxMaterial.SetFloat("_BloomStrength", _postFXSetting.bloom.strength);
        _cmdBuffer.SetGlobalTexture("_BloomTex", bloomSampleRTs[0]);
        _cmdBuffer.Blit(src, BloomThresholdPassRT, _postFXSetting.fxMaterial, _postFXSetting.GetPass(PostFXSetting.BloomAddPass));
        _cmdBuffer.EndSample("Bloom");
        ExecuteCommandBuffer();

        while (curIteration < actualIteration) {
            _cmdBuffer.ReleaseTemporaryRT(bloomSampleRTs[curIteration]);
            ExecuteCommandBuffer();
            curIteration++;
        }
        
        return BloomThresholdPassRT;
    }

    int DoToneMapping(RenderTargetIdentifier srcRT) {
        _cmdBuffer.BeginSample("ToneMapping");
        _cmdBuffer.GetTemporaryRT(ToneMappingPassRT, _camera.pixelWidth, _camera.pixelHeight, 16, FilterMode.Bilinear, RenderTextureFormat.Default);
        _cmdBuffer.SetRenderTarget(ToneMappingPassRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        int pass = -1;
        switch(_postFXSetting.toneMapping.mode) {
            case PostFXSetting.ToneMapping.Mode.Reinhard: {
                pass = _postFXSetting.GetPass(PostFXSetting.ToneMappingReinhardPass);
                break;
            }
            case PostFXSetting.ToneMapping.Mode.Neutral: {
                    pass = _postFXSetting.GetPass(PostFXSetting.ToneMappingNeutralPass);
                    break;
            }
            case PostFXSetting.ToneMapping.Mode.ACES: {
                    pass = _postFXSetting.GetPass(PostFXSetting.ToneMappingACESPass);
                    break;
            }
        }
        _cmdBuffer.Blit(srcRT, ToneMappingPassRT, _postFXSetting.fxMaterial, pass);
        _cmdBuffer.EndSample("ToneMapping");
        ExecuteCommandBuffer();

        return ToneMappingPassRT; 
    }

    int DoColorAjustments(RenderTargetIdentifier srcRT) {
        _cmdBuffer.BeginSample(PostFXSetting.ColorAjustmentPass);
        _cmdBuffer.GetTemporaryRT(ColorAjustmentPassRT, 
                                _camera.pixelWidth, _camera.pixelHeight, 16, 
                                FilterMode.Bilinear,
                                 _useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
        _cmdBuffer.SetRenderTarget(ColorAjustmentPassRT, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        float exposure = Mathf.Pow(2, _postFXSetting.colorAjustment.exposure);
        float contrast = _postFXSetting.colorAjustment.contrast * 0.01f + 1f;
        float hue = _postFXSetting.colorAjustment.hue / 360f;
        float saturation = _postFXSetting.colorAjustment.saturation * 0.01f + 1f;
        
        _postFXSetting.fxMaterial.SetVector("_ColorAjustments", new Vector4(exposure, contrast, hue, saturation));
        _postFXSetting.fxMaterial.SetColor("_FilterColor", _postFXSetting.colorAjustment.filter);
        _postFXSetting.fxMaterial.SetVector("_WhiteBalance", ColorUtils.ColorBalanceToLMSCoeffs(_postFXSetting.whiteBalance.temperature, _postFXSetting.whiteBalance.tint));
        Color shadowTone = _postFXSetting.splitTone.shadow;
        shadowTone.a = _postFXSetting.splitTone.balance * 0.01f;
        _postFXSetting.fxMaterial.SetColor("_ShadowTone", shadowTone);
        _postFXSetting.fxMaterial.SetColor("_HighLightTone", _postFXSetting.splitTone.highLight);
        
        _cmdBuffer.Blit(srcRT, ColorAjustmentPassRT, _postFXSetting.fxMaterial, _postFXSetting.GetPass(PostFXSetting.ColorAjustmentPass));
        _cmdBuffer.EndSample(PostFXSetting.ColorAjustmentPass);
        ExecuteCommandBuffer();

        return ColorAjustmentPassRT;
    }
}
