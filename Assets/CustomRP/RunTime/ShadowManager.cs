using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ShadowManager {
    public static readonly int MAX_NUM_DIRECTIONAL_SHADOWS = 4;
    public static readonly int MAX_NUM_DIRECTIONAL_CASCADES = 4;
    
    static string CmdBufferName = "Shadows";
    static int maxShadowDistanceTagId = Shader.PropertyToID("_MaxShadowDistance");
    static int fadeDistanceRatioTagId = Shader.PropertyToID("_FadeDistanceRatio");
    

    static int directionalShadowAtlasTagId = Shader.PropertyToID("_DirectioanlShadowAtlas");
    static int directionalShadowMatrixsTagId = Shader.PropertyToID("_DirectionalShadowMatrixs");
    static int directionalCascadeCullingSpheresTagId = Shader.PropertyToID("_DirectionalCascadeCullingSpheres");
    static int directionalCascadeCountTagId = Shader.PropertyToID("_DirectionalCascadeCount");
    static int directionalCascadeTileSizeTagId = Shader.PropertyToID("_DirCasecadeTileSize");

    
    public struct ShadowedDirectionalLight {
        public int visibleIndex;
        public Light light;
    }

    private CommandBuffer _cmdBuffer = new CommandBuffer() {name = CmdBufferName};
    private ScriptableRenderContext _srContext;
    
    private ShadowedDirectionalLight[] _shadowedDirLights = new ShadowedDirectionalLight[MAX_NUM_DIRECTIONAL_SHADOWS];
    private Matrix4x4[] _dirShadowMatrixs = new Matrix4x4[MAX_NUM_DIRECTIONAL_SHADOWS * MAX_NUM_DIRECTIONAL_CASCADES];
    private Vector4[] _dirCascadeCullingSpheres = new Vector4[MAX_NUM_DIRECTIONAL_CASCADES];
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
        if (dirLight.type != LightType.Directional || dirLight.shadows == LightShadows.None || dirLight.shadowStrength <= 0 || !cullResults.GetShadowCasterBounds(visibleLightIndex, out Bounds bounds) )
            return -1;
        
        _shadowedDirLights[_shadowedDirLightCount] = new ShadowedDirectionalLight() {visibleIndex = visibleLightIndex, light = dirLight};
        shadowData.x = dirLight.shadowStrength;;
        shadowData.y = dirLight.shadowNormalBias;
        shadowData.z = dirLight.shadowBias;
        shadowData.w = dirLight.shadowNearPlane;

        int tileIdx = _shadowedDirLightCount * MAX_NUM_DIRECTIONAL_CASCADES;
        _shadowedDirLightCount++;
        
        return tileIdx;
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

            int tileSplitFactor = _shadowedDirLightCount > 1 ? 2 : 1; // split render target to 4 tiles
            for(int i=0; i<_shadowedDirLightCount; i++) {
                RenderOneDirectionalShadow(i, _shadowedDirLights[i], ref cullResults, ref shadowSetting, tileSplitFactor);
            }


            _cmdBuffer.SetGlobalFloat(maxShadowDistanceTagId, shadowSetting.maxDistance);
            _cmdBuffer.SetGlobalFloat(fadeDistanceRatioTagId, shadowSetting.fadeDistanceRatio);
            _cmdBuffer.SetGlobalMatrixArray(directionalShadowMatrixsTagId, _dirShadowMatrixs);
            //_cmdBuffer.SetGlobalTexture(directionalShadowAtlasTagId, directionalShadowAtlasTagId);
            _cmdBuffer.SetGlobalInt(directionalCascadeCountTagId, shadowSetting.directional.cascadeCount);
            _cmdBuffer.SetGlobalVectorArray(directionalCascadeCullingSpheresTagId, _dirCascadeCullingSpheres);
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

    void RenderOneDirectionalShadow(int lightIdx, ShadowedDirectionalLight shadowedLight, ref CullingResults cullResults, ref ShadowSetting shadowSetting, int tileSplitFactor) {
        int cascadeSplitFactor = shadowSetting.directional.cascadeCount > 1 ? 2 : 1;
        int tileSize = (int)shadowSetting.directional.atlasSize / tileSplitFactor;
        int cascadeSize = (int)shadowSetting.directional.atlasSize / (tileSplitFactor * cascadeSplitFactor);
        int visibleIndex = _shadowedDirLights[lightIdx].visibleIndex;
        
        _cmdBuffer.SetGlobalInt(directionalCascadeTileSizeTagId, cascadeSize);
        _cmdBuffer.SetGlobalDepthBias(0, shadowedLight.light.shadowBias);
        for (int cascadeIdx = 0; cascadeIdx < shadowSetting.directional.cascadeCount; cascadeIdx++) {
            cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(visibleIndex, cascadeIdx, shadowSetting.directional.cascadeCount, shadowSetting.directional.cascadeSplitRatio, cascadeSize, shadowedLight.light.shadowNearPlane, out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);
            _cmdBuffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
            
            Vector2 tileOffset = new Vector2(lightIdx % 2, lightIdx / 2);
            Vector2 cascadeOffset = new Vector2(cascadeIdx % 2, cascadeIdx / 2);
            Rect viewPortOffset = new Rect(tileOffset.x * tileSize + cascadeOffset.x * cascadeSize, tileOffset.y * tileSize + cascadeOffset.y * cascadeSize, cascadeSize, cascadeSize);
            _cmdBuffer.SetViewport(viewPortOffset);
            ExecuteCommandBuffer();

            _dirShadowMatrixs[lightIdx * MAX_NUM_DIRECTIONAL_CASCADES + cascadeIdx] = ShadowMatrixToAtlasTileMatrix(projMatrix * viewMatrix, viewPortOffset, (int)shadowSetting.directional.atlasSize);
            if (lightIdx == 0) { // all directional light share the same cascade culling spheres
                _dirCascadeCullingSpheres[cascadeIdx] = shadowSplitData.cullingSphere;
            }

            ShadowDrawingSettings shadowDrawSetting = new ShadowDrawingSettings(cullResults, visibleIndex) { splitData = shadowSplitData };
            _srContext.DrawShadows(ref shadowDrawSetting);
        }
        _cmdBuffer.SetGlobalDepthBias(0, 0);
    }

    Matrix4x4 ShadowMatrixToAtlasTileMatrix(Matrix4x4 shadowMatrix, Rect viewPortOffset, int atlasSize) {
        if (SystemInfo.usesReversedZBuffer) {
            shadowMatrix.m20 = -shadowMatrix.m20;
            shadowMatrix.m21 = -shadowMatrix.m21;
            shadowMatrix.m22 = -shadowMatrix.m22;
            shadowMatrix.m23 = -shadowMatrix.m23;
        }

        Matrix4x4 ndcTransform = Matrix4x4.TRS(new Vector3(0.5f, 0.5f, 0.5f), Quaternion.identity, new Vector3(0.5f, 0.5f, 0.5f));
        Matrix4x4 tileTransform = Matrix4x4.TRS(new Vector3(viewPortOffset.x/atlasSize, viewPortOffset.y/atlasSize, 0), Quaternion.identity, new Vector3(viewPortOffset.width/atlasSize, viewPortOffset.height/atlasSize, 1));

        return tileTransform * ndcTransform * shadowMatrix;
    }

}
