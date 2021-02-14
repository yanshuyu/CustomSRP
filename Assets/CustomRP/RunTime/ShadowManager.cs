using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ShadowManager {
    public static readonly int MAX_NUM_DIRECTIONAL_SHADOWS = LightManager.MAX_NUM_DIRECTIONAL_LIGHT;
    
    static string CmdBufferName = "Shadows";
    static int directionalShadowAtlasTagId = Shader.PropertyToID("_DirectioanlShadowAtlas");
    static int directionalShadowMatrixsTagId = Shader.PropertyToID("_DirectionalShadowMatrixs");

    public struct ShadowedDirectionalLight {
        public int visibleIndex;
        public Light light;
    }

    private CommandBuffer _cmdBuffer = new CommandBuffer() {name = CmdBufferName};
    private ScriptableRenderContext _srContext;
    
    private ShadowedDirectionalLight[] _shadowedDirLights = new ShadowedDirectionalLight[MAX_NUM_DIRECTIONAL_SHADOWS];
    private Matrix4x4[] _dirShadowMatrixs = new Matrix4x4[MAX_NUM_DIRECTIONAL_SHADOWS];
    private int _shadowedDirLightCount = 0;

    public void SetUp(ref ScriptableRenderContext srContext) {
        _srContext = srContext;
        _shadowedDirLightCount = 0;
    }

    public int GetDirectionalShadowData(ref CullingResults cullResults, int visibleLightIndex, out Vector4 shadowData) {
        shadowData = Vector4.zero;
        if (_shadowedDirLightCount >= MAX_NUM_DIRECTIONAL_SHADOWS)
            return -1;
        
        Light dirLight = cullResults.visibleLights[visibleLightIndex].light;
        if (dirLight.type != LightType.Directional || dirLight.shadows == LightShadows.None || dirLight.shadowStrength <= 0 /*|| !cullResults.GetShadowCasterBounds(visibleLightIndex, out Bounds bounds)*/ ) //directinal light effect entire scene
            return -1;
        
        _shadowedDirLights[_shadowedDirLightCount] = new ShadowedDirectionalLight() {visibleIndex = visibleLightIndex, light = dirLight};
        shadowData.x = dirLight.shadowStrength;;
        shadowData.y = dirLight.shadowBias;
        shadowData.z = dirLight.shadowNormalBias;
        shadowData.w = dirLight.shadowNearPlane;
        
        return _shadowedDirLightCount++;
    }


    public void Render(ref CullingResults cullResults, ref ShadowSetting shadowSetting) {

        RenderDirectionalShadows(ref cullResults, ref shadowSetting);

    }


    public void CleanUp() {
        _cmdBuffer.ReleaseTemporaryRT(directionalShadowAtlasTagId);
        //_cmdBuffer.BeginSample(CmdBufferName);
        ExecuteCommandBuffer();
        _shadowedDirLightCount = 0;
    }


    void RenderDirectionalShadows(ref CullingResults cullResults, ref ShadowSetting shadowSetting) {
        if (_shadowedDirLightCount > 0) {
            int atlasSize = (int)shadowSetting.directional.atlasSize;
            _cmdBuffer.GetTemporaryRT(directionalShadowAtlasTagId, atlasSize, atlasSize, 24, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            _cmdBuffer.SetRenderTarget(directionalShadowAtlasTagId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store); 
            _cmdBuffer.ClearRenderTarget(true, false, Color.clear);
            _cmdBuffer.BeginSample(CmdBufferName);
            ExecuteCommandBuffer();

            int splitFactor = _shadowedDirLightCount > 1 ? 2 : 1; // split render target to 4 tiles
            int tileSize = atlasSize / splitFactor;
            for(int i=0; i<_shadowedDirLightCount; i++) {
                int visibleIndex = _shadowedDirLights[i].visibleIndex;
                cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(visibleIndex, 0, 1, Vector3.zero, tileSize, 0, out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);
                _cmdBuffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
                Vector2 tileOffset = new Vector2(i % 2, i / 2);
                _cmdBuffer.SetViewport(new Rect(tileOffset.x * tileSize,  tileOffset.y  * tileSize, tileSize, tileSize));
                ExecuteCommandBuffer();
               
                _dirShadowMatrixs[i] = ShadowMatrixToAtlasTileMatrix(projMatrix * viewMatrix, splitFactor, tileOffset);
                 
                ShadowDrawingSettings shadowDrawSetting = new ShadowDrawingSettings(cullResults, visibleIndex) { splitData = shadowSplitData };
                _srContext.DrawShadows(ref shadowDrawSetting);
            }

            _cmdBuffer.SetGlobalMatrixArray(directionalShadowMatrixsTagId, _dirShadowMatrixs);
            //_cmdBuffer.SetGlobalTexture(directionalShadowAtlasTagId, directionalShadowAtlasTagId);
            _cmdBuffer.EndSample(CmdBufferName);
            ExecuteCommandBuffer();
        } else { // if no directional shadows, bingding a dummy shadow map to avoid shader variation
            _cmdBuffer.GetTemporaryRT(directionalShadowAtlasTagId, 1, 1, 24, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }
    }

    void ExecuteCommandBuffer() {
        _srContext.ExecuteCommandBuffer(_cmdBuffer);
        _cmdBuffer.Clear();
    }

    Matrix4x4 ShadowMatrixToAtlasTileMatrix(Matrix4x4 shadowMatrix, int splitFactor, Vector2 tileOffset) {
        if (SystemInfo.usesReversedZBuffer) {
            shadowMatrix.m20 = -shadowMatrix.m20;
            shadowMatrix.m21 = -shadowMatrix.m21;
            shadowMatrix.m22 = -shadowMatrix.m22;
            shadowMatrix.m23 = -shadowMatrix.m23;
        }
        Matrix4x4 ndcTransform = Matrix4x4.TRS(new Vector3(0.5f, 0.5f, 0.5f), Quaternion.identity, new Vector3(0.5f, 0.5f, 0.5f));
        Matrix4x4 tileTansform = Matrix4x4.TRS(new Vector3(tileOffset.x/splitFactor, tileOffset.y/splitFactor, 0), Quaternion.identity, new Vector3(1f/splitFactor, 1f/splitFactor, 1f));

        return tileTansform * ndcTransform * shadowMatrix;
    }

}
