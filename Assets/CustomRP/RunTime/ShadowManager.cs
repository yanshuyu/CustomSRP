using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ShadowManager {
    public static readonly int MAX_NUM_DIRECTIONAL_SHADOWS = 4;
    public static readonly int MAX_NUM_DIRECTIONAL_CASCADES = 4;
    public static readonly int MAX_NUM_POINT_SHADOWS = 2;
    public static readonly int MAX_NUM_SPOT_SHADOWS = 16;
    
    static string CmdBufferName = "Shadows";
    static int maxShadowDistanceTagId = Shader.PropertyToID("_MaxShadowDistance");
    static int fadeDistanceRatioTagId = Shader.PropertyToID("_FadeDistanceRatio");
    static int shadowMapSizesId = Shader.PropertyToID("_ShadowMapSizes");
    
    static int directionalShadowAtlasTagId = Shader.PropertyToID("_DirectioanlShadowAtlas");
    static int directionalShadowMatrixsTagId = Shader.PropertyToID("_DirectionalShadowMatrixs");
    static int directionalCascadeCullingSpheresTagId = Shader.PropertyToID("_DirectionalCascadeCullingSpheres");
    static int directionalCascadeCountTagId = Shader.PropertyToID("_DirectionalCascadeCount");

    static int pointShadowAtlasId = Shader.PropertyToID("_PointShadowAtlas");
    static int pointShadowMatrixsId = Shader.PropertyToID("_PointShadowMatrixs");

    static int spotShadowAtlasId = Shader.PropertyToID("_SpotShadowAtlas");
    static int spotShadowMatrixsId = Shader.PropertyToID("_SpotShadowMatrixs");
    static int spotShadowTileViewPortsId = Shader.PropertyToID("_SpotShadowTileViewPorts");

    static string KW_Shadow_Mask_Distance = "SHADOW_MASK_DISTANCE";
    static string KW_Shadow_Mask_Always = "SHADOW_MASK_ALWAYS";

    public struct ShadowedLight {
        public int visibleIndex;
        public Light light;
    }

    private CommandBuffer _cmdBuffer = new CommandBuffer() {name = CmdBufferName};
    private ScriptableRenderContext _srContext;
    private CullingResults _cullResults;
    private ShadowSetting _shadowSetting;
    
    private Vector4 _shadowMapSizes = Vector4.zero;

    //directional shadow data
    private ShadowedLight[] _shadowedDirLights = new ShadowedLight[MAX_NUM_DIRECTIONAL_SHADOWS];
    private Matrix4x4[] _dirShadowMatrixs = new Matrix4x4[MAX_NUM_DIRECTIONAL_SHADOWS * MAX_NUM_DIRECTIONAL_CASCADES];
    private Vector4[] _dirCascadeCullingSpheres = new Vector4[MAX_NUM_DIRECTIONAL_CASCADES];
    private int _shadowedDirLightCount = 0;

    // point shadow data
    private ShadowedLight[] _shadowedPointLights = new ShadowedLight[MAX_NUM_POINT_SHADOWS];
    private Matrix4x4[] _pointShadowMatrixs = new Matrix4x4[MAX_NUM_POINT_SHADOWS * 6];
    private int _shadowedPointLightCount = 0;

    // spot shadow data
    private ShadowedLight[] _shadowedSpotLights = new ShadowedLight[MAX_NUM_SPOT_SHADOWS];
    private Matrix4x4[] _spotShadowMatrixs = new Matrix4x4[MAX_NUM_SPOT_SHADOWS];
    private Vector4[] _spotShadowTileViewPorts = new Vector4[MAX_NUM_SPOT_SHADOWS];
    private int _shadowedSpotLightCount = 0;

    private bool _mixedLightUseShadowMask = false;

    public void SetUp(ref ScriptableRenderContext srContext, ref CullingResults cullResults, ref ShadowSetting shadowSetting) {
        _srContext = srContext;
        _cullResults = cullResults;
        _shadowSetting = shadowSetting;
        _shadowedDirLightCount = _shadowedPointLightCount = _shadowedSpotLightCount = 0;
        _mixedLightUseShadowMask = false;
    }

    public Vector4 GetDirectionalShadowData(int visibleLightIndex) {
        Vector4 shadowData = new Vector4(-1, 0, 0, -1);
        Light dirLight = _cullResults.visibleLights[visibleLightIndex].light;
        if (dirLight.type != LightType.Directional)
            return shadowData;

        if (dirLight.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed && dirLight.bakingOutput.mixedLightingMode == MixedLightingMode.Shadowmask) {
            shadowData.y = dirLight.shadowStrength; // when mixed light who' s shadow has baked to shadow mask did't cast shadow at run time, we want to use shadow in shadow mask
            shadowData.w = dirLight.bakingOutput.occlusionMaskChannel;
            _mixedLightUseShadowMask = true;
        }      
       
        if (_shadowedDirLightCount >= MAX_NUM_DIRECTIONAL_SHADOWS ||
                            dirLight.shadows == LightShadows.None || 
                            dirLight.shadowStrength <= 0 || 
                            !_cullResults.GetShadowCasterBounds(visibleLightIndex, out Bounds bounds) ) 
            return shadowData;
        
        _shadowedDirLights[_shadowedDirLightCount] = new ShadowedLight() {visibleIndex = visibleLightIndex, light = dirLight};
        shadowData.x = _shadowedDirLightCount * MAX_NUM_DIRECTIONAL_CASCADES;
        shadowData.y = dirLight.shadowStrength;
        shadowData.z = dirLight.shadowNormalBias * 2f - 1f;

        _shadowedDirLightCount++;
        
        return shadowData;
    }


    public Vector4 GetPointShadowData(int visibleLightIndex) {
        Vector4 shadowData = new Vector4(-1, 0, 0, -1);
        Light pointLight = _cullResults.visibleLights[visibleLightIndex].light;
        if (pointLight.type != LightType.Point)
            return shadowData;

        if (pointLight.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed && pointLight.bakingOutput.mixedLightingMode == MixedLightingMode.Shadowmask) {
            shadowData.y = pointLight.shadowStrength;
            shadowData.w = pointLight.bakingOutput.occlusionMaskChannel;
            _mixedLightUseShadowMask = true;
        }

        if (_shadowedPointLightCount >= MAX_NUM_POINT_SHADOWS || 
                        pointLight.shadows == LightShadows.None ||
                        pointLight.shadowStrength <= 0 || 
                        !_cullResults.GetShadowCasterBounds(visibleLightIndex, out Bounds bounds))
            return shadowData;

        _shadowedPointLights[_shadowedPointLightCount] = new ShadowedLight() { visibleIndex = visibleLightIndex, light = pointLight };
        shadowData.x = _shadowedPointLightCount * 6;
        shadowData.y = pointLight.shadowStrength;
        shadowData.z = pointLight.shadowNormalBias * 2f - 1f; 

        _shadowedPointLightCount++;

        return shadowData;
    }


    public Vector4 GetSpotLightShadowData(int visibleLightIndex) {
        Vector4 shadowData = new Vector4(-1, 0, 0, -1);
        Light spotLight = _cullResults.visibleLights[visibleLightIndex].light;
        if (spotLight.type != LightType.Spot)
            return shadowData;

        if (spotLight.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed && spotLight.bakingOutput.mixedLightingMode == MixedLightingMode.Shadowmask) {
            shadowData.y = spotLight.shadowStrength;
            shadowData.w = spotLight.bakingOutput.occlusionMaskChannel;
            _mixedLightUseShadowMask = true;
        }

        if (_shadowedSpotLightCount >= MAX_NUM_SPOT_SHADOWS ||
                    spotLight.shadows == LightShadows.None ||
                    spotLight.shadowStrength <= 0 ||
                    !_cullResults.GetShadowCasterBounds(visibleLightIndex, out Bounds bounds))
            return shadowData;

        _shadowedSpotLights[_shadowedSpotLightCount] = new ShadowedLight() { visibleIndex = visibleLightIndex, light = spotLight };
        shadowData.x = _shadowedSpotLightCount;
        shadowData.y = spotLight.shadowStrength;
        shadowData.z = spotLight.shadowNormalBias * 2f - 1f;

        _shadowedSpotLightCount++;

        return shadowData;
    }


    public void Render() {
        RenderDirectionalShadows();
        RenderPointShadows();
        RenderSpotShadows();
        
        _cmdBuffer.SetGlobalVector(shadowMapSizesId, _shadowMapSizes);
        _cmdBuffer.SetGlobalFloat(maxShadowDistanceTagId, _shadowSetting.maxDistance);
        _cmdBuffer.SetGlobalFloat(fadeDistanceRatioTagId, _shadowSetting.fadeDistanceRatio);
        if (_mixedLightUseShadowMask) {
            _cmdBuffer.EnableShaderKeyword( QualitySettings.shadowmaskMode == ShadowmaskMode.DistanceShadowmask ?
             KW_Shadow_Mask_Distance : KW_Shadow_Mask_Always);
        }
        ExecuteCommandBuffer();
    }


    public void CleanUp() {
        _cmdBuffer.ReleaseTemporaryRT(directionalShadowAtlasTagId);
        _cmdBuffer.ReleaseTemporaryRT(pointShadowAtlasId);
        _cmdBuffer.ReleaseTemporaryRT(spotShadowAtlasId);
        _cmdBuffer.DisableShaderKeyword(KW_Shadow_Mask_Distance);
        _cmdBuffer.DisableShaderKeyword(KW_Shadow_Mask_Always);
        ExecuteCommandBuffer();
        _shadowedDirLightCount = _shadowedPointLightCount = _shadowedSpotLightCount = 0;
        _mixedLightUseShadowMask = false;
    }


    void RenderDirectionalShadows() {
        if (_shadowedDirLightCount > 0) {
            int atlasSize = (int)_shadowSetting.directional.atlasSize;
            _cmdBuffer.GetTemporaryRT(directionalShadowAtlasTagId, atlasSize, atlasSize, 24, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            _cmdBuffer.SetRenderTarget(directionalShadowAtlasTagId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store); 
            _cmdBuffer.ClearRenderTarget(true, false, Color.clear);
            _cmdBuffer.BeginSample("directioanl");
            ExecuteCommandBuffer();

            int tileSplitFactor = _shadowedDirLightCount > 1 ? 2 : 1; // split render target to 4 tiles
            for(int i=0; i<_shadowedDirLightCount; i++) {
                RenderOneDirectionalShadow(i, tileSplitFactor);
            }

            _cmdBuffer.SetGlobalMatrixArray(directionalShadowMatrixsTagId, _dirShadowMatrixs);
            _cmdBuffer.SetGlobalInt(directionalCascadeCountTagId, _shadowSetting.directional.cascadeCount);
            _cmdBuffer.SetGlobalVectorArray(directionalCascadeCullingSpheresTagId, _dirCascadeCullingSpheres);
            _cmdBuffer.EndSample("directioanl");
            ExecuteCommandBuffer();

        } else { // if no directional shadows, bingding a dummy shadow map to avoid shader variation
            _cmdBuffer.GetTemporaryRT(directionalShadowAtlasTagId, 1, 1, 24, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }
    }

    void RenderPointShadows() {
        int atlasSize = _shadowedPointLightCount > 0 ? (int)_shadowSetting.point.atlasSize : 1;
        _cmdBuffer.GetTemporaryRT(pointShadowAtlasId, atlasSize, atlasSize, 24, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        if (_shadowedPointLightCount > 0) {
            _cmdBuffer.SetRenderTarget(pointShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            _cmdBuffer.ClearRenderTarget(true, false, Color.clear);
        }
        ExecuteCommandBuffer();

        if (_shadowedPointLightCount > 0) {
            int splitFactor = GetPointShadowAtlasSplitFactor();
            int tileSize = atlasSize / splitFactor;
            _shadowMapSizes.y = tileSize;

            _cmdBuffer.BeginSample("Point");
            ExecuteCommandBuffer();
            for (int i=0; i<_shadowedPointLightCount; i++) {
                RenderOnePointShadows(i, splitFactor, tileSize);
            }
            _cmdBuffer.SetGlobalMatrixArray(pointShadowMatrixsId, _pointShadowMatrixs);
            _cmdBuffer.EndSample("Point");
            ExecuteCommandBuffer();
        } 
    }

    void RenderSpotShadows() {
        int atlasSize = _shadowedSpotLightCount > 0 ? (int)_shadowSetting.spot.atlasSize : 1;
        _cmdBuffer.GetTemporaryRT(spotShadowAtlasId, atlasSize, atlasSize, 24, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        if (_shadowedSpotLightCount > 0) {
            _cmdBuffer.SetRenderTarget(spotShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            _cmdBuffer.ClearRenderTarget(true, false, Color.clear);
        }
        ExecuteCommandBuffer();

        if (_shadowedSpotLightCount > 0) {
            int splitFactor = GetSpotShadowAtlasSplitFactor();
            int tileSize = atlasSize / splitFactor;
            _shadowMapSizes.z = tileSize;

            _cmdBuffer.BeginSample("Spot");
            ExecuteCommandBuffer();
            for (int i=0; i<_shadowedSpotLightCount; i++) {
                RenderOneSpotShadows(i, splitFactor, tileSize);
            }
            _cmdBuffer.SetGlobalMatrixArray(spotShadowMatrixsId, _spotShadowMatrixs);
            _cmdBuffer.SetGlobalVectorArray(spotShadowTileViewPortsId, _spotShadowTileViewPorts);
            _cmdBuffer.EndSample("Spot");
            ExecuteCommandBuffer();
        } 
    }

    void RenderOneDirectionalShadow(int lightIdx, int tileSplitFactor) {
        ShadowedLight shadowedLight = _shadowedDirLights[lightIdx];
        int cascadeSplitFactor = _shadowSetting.directional.cascadeCount > 1 ? 2 : 1;
        int tileSize = (int)_shadowSetting.directional.atlasSize / tileSplitFactor;
        int cascadeSize = (int)_shadowSetting.directional.atlasSize / (tileSplitFactor * cascadeSplitFactor);
        int visibleIndex = shadowedLight.visibleIndex;
        _shadowMapSizes.x = cascadeSize;

        _cmdBuffer.SetGlobalDepthBias(0, shadowedLight.light.shadowBias);
        for (int cascadeIdx = 0; cascadeIdx < _shadowSetting.directional.cascadeCount; cascadeIdx++) {
            _cullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(visibleIndex, 
                                                                                cascadeIdx, 
                                                                                _shadowSetting.directional.cascadeCount, 
                                                                                _shadowSetting.directional.cascadeSplitRatio,
                                                                                cascadeSize, 
                                                                                shadowedLight.light.shadowNearPlane, 
                                                                                out Matrix4x4 viewMatrix, 
                                                                                out Matrix4x4 projMatrix, 
                                                                                out ShadowSplitData shadowSplitData);
            _cmdBuffer.SetViewProjectionMatrices(viewMatrix, projMatrix);
            
            Vector2 tileOffset = new Vector2(lightIdx % 2, lightIdx / 2);
            Vector2 cascadeOffset = new Vector2(cascadeIdx % 2, cascadeIdx / 2);
            Rect viewPortOffset = new Rect(tileOffset.x * tileSize + cascadeOffset.x * cascadeSize, tileOffset.y * tileSize + cascadeOffset.y * cascadeSize, cascadeSize, cascadeSize);
            _cmdBuffer.SetViewport(viewPortOffset);
            ExecuteCommandBuffer();

            _dirShadowMatrixs[lightIdx * MAX_NUM_DIRECTIONAL_CASCADES + cascadeIdx] = ShadowMatrixToAtlasTileMatrix(projMatrix * viewMatrix, viewPortOffset, (int)_shadowSetting.directional.atlasSize);
            if (lightIdx == 0) { // all directional light share the same cascade culling spheres
                _dirCascadeCullingSpheres[cascadeIdx] = shadowSplitData.cullingSphere;
            }

            ShadowDrawingSettings shadowDrawSetting = new ShadowDrawingSettings(_cullResults, visibleIndex) { splitData = shadowSplitData };
            _srContext.DrawShadows(ref shadowDrawSetting);
        }
        _cmdBuffer.SetGlobalDepthBias(0, 0);
    }

    void RenderOnePointShadows(int shadowedLightIdx, int atlasSplitFactor, int atlasTileSize) {
        ShadowedLight shadowedLight = _shadowedPointLights[shadowedLightIdx];;
        ShadowDrawingSettings shadowDrawSetting = new ShadowDrawingSettings(_cullResults, shadowedLight.visibleIndex);
        for (int faceIdx = 0; faceIdx < 6; faceIdx++) {
            _cullResults.ComputePointShadowMatricesAndCullingPrimitives(shadowedLight.visibleIndex,
                                                                         (CubemapFace)faceIdx, 
                                                                            0,
                                                                            out Matrix4x4 viewMatrix,
                                                                            out Matrix4x4 projMatrix, 
                                                                            out ShadowSplitData shadowSplitData);
            viewMatrix.m11 *= -1;   // unity render point light shadow upside down
            viewMatrix.m12 *= -1;
            viewMatrix.m13 *= -1;
            _cmdBuffer.SetViewProjectionMatrices(viewMatrix, projMatrix);

            Rect tileViewPort = Rect.zero;
            int tileIdx = shadowedLightIdx * 6 + faceIdx;
            tileViewPort.x = tileIdx % atlasSplitFactor * atlasTileSize;
            tileViewPort.y = tileIdx / atlasSplitFactor * atlasTileSize;
            tileViewPort.width = tileViewPort.height = atlasTileSize;
            _cmdBuffer.SetViewport(tileViewPort);
            _cmdBuffer.SetGlobalDepthBias(0, shadowedLight.light.shadowBias);
            ExecuteCommandBuffer();

            int atlasSize = atlasSplitFactor * atlasTileSize;
            Vector4 normalizeViewPort = Vector4.zero;
            float texelSize = 1f / atlasSize;
            _pointShadowMatrixs[tileIdx] = ShadowMatrixToAtlasTileMatrix(projMatrix * viewMatrix, tileViewPort, atlasSize);
            
            shadowDrawSetting.splitData = shadowSplitData;
            _srContext.DrawShadows(ref shadowDrawSetting);
            _cmdBuffer.SetGlobalDepthBias(0, 0);
        }
    }


    void RenderOneSpotShadows(int shadowedLightIdx, int atlasSplitFactor, int atlasTileSize) {
        ShadowedLight shadowedLight = _shadowedSpotLights[shadowedLightIdx];   
        _cullResults.ComputeSpotShadowMatricesAndCullingPrimitives(shadowedLight.visibleIndex, out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData);
        _cmdBuffer.SetViewProjectionMatrices(viewMatrix, projMatrix);

        Rect tileViewPort = Rect.zero;
        int tileIdx = shadowedLightIdx;
        tileViewPort.x = tileIdx % atlasSplitFactor * atlasTileSize;
        tileViewPort.y = tileIdx / atlasSplitFactor * atlasTileSize;
        tileViewPort.width = tileViewPort.height = atlasTileSize;
        _cmdBuffer.SetViewport(tileViewPort);
        _cmdBuffer.SetGlobalDepthBias(0, shadowedLight.light.shadowBias);
        ExecuteCommandBuffer();

        int atlasSize = atlasSplitFactor * atlasTileSize;
        Vector4 normalizeViewPort = Vector4.zero;
        float texelSize = 1f / atlasSize;
        normalizeViewPort.x = tileViewPort.x / atlasSize + texelSize;
        normalizeViewPort.y = tileViewPort.y / atlasSize + texelSize;
        normalizeViewPort.z = tileViewPort.width / atlasSize - texelSize;
        normalizeViewPort.w = tileViewPort.height / atlasSize - texelSize;
        _spotShadowMatrixs[tileIdx] = ShadowMatrixToAtlasTileMatrix(projMatrix * viewMatrix, tileViewPort, atlasSize);
        _spotShadowTileViewPorts[tileIdx] = normalizeViewPort;

        ShadowDrawingSettings shadowDrawSetting = new ShadowDrawingSettings(_cullResults, shadowedLight.visibleIndex);
        shadowDrawSetting.splitData = shadowSplitData;
        _srContext.DrawShadows(ref shadowDrawSetting);
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


    int GetPointShadowAtlasSplitFactor() {
        int i = 1;
        while (i * i < _shadowedPointLightCount * 6) {
            i *= 2;
        }
        return i;
    }

    int GetSpotShadowAtlasSplitFactor() {
        int i=1;
        while(i*i < _shadowedSpotLightCount) {
            i *= 2;
        }
        return i;
    }


    void ExecuteCommandBuffer() {
        _srContext.ExecuteCommandBuffer(_cmdBuffer);
        _cmdBuffer.Clear();
    }

}
