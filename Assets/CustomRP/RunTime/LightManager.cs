using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LightManager {
    public static readonly int MAX_NUM_DIRECTIONAL_LIGHT = 4;
    public static readonly int MAX_NUM_SPOT_LIGHT = 16;
    public static readonly int MAX_NUM_POINT_LIGHT = 16;
    static readonly string CmdBufferName = "Lights";

    static int DirLightCountId = Shader.PropertyToID("_DirLightCount");
    static int DirLightColorsId = Shader.PropertyToID("_DirLightColors");
    static int DirLightDirectionsId = Shader.PropertyToID("_DirLightDirections");
    static int DirLightShadowDataId = Shader.PropertyToID("_DirLightShadowData");
    //static int DirLightShadowTileIndicesId = Shader.PropertyToID("_DirLightShadowTileIndices");

    static int SpotLightCountId = Shader.PropertyToID("_SpotLightCount");
    static int SpotLightColorsId = Shader.PropertyToID("_SpotLightColors");
    static int SpotLightPositionsId = Shader.PropertyToID("_SpotLightPositions");
    static int SpotLightDirectionsId = Shader.PropertyToID("_SpotLightDirections");
    static int SpotLightAnglesId = Shader.PropertyToID("_SpotLightAngles");
    static int SpotLightShadowDataId = Shader.PropertyToID("_SpotLightShadowData");

    static int PointLightCountId = Shader.PropertyToID("_PointLightCount");
    static int PointLightColorsId = Shader.PropertyToID("_PointLightColors");
    static int PointLightPositionsId = Shader.PropertyToID("_PointLightPositions");
    static int PointLightShadowDataId = Shader.PropertyToID("_PointLightShadowData");

    // diretional lights data
    private Vector4[] _dirLightColors = new Vector4[MAX_NUM_DIRECTIONAL_LIGHT];
    private Vector4[] _dirLightDirections = new Vector4[MAX_NUM_DIRECTIONAL_LIGHT];
    private Vector4[] _dirLightShadowData = new Vector4[MAX_NUM_DIRECTIONAL_LIGHT];
    private int _dirLightCount = 0;

    // spot lights data
    private Vector4[] _spotLightColors = new Vector4[MAX_NUM_SPOT_LIGHT];
    private Vector4[] _spotLightPositions = new Vector4[MAX_NUM_SPOT_LIGHT];
    private Vector4[] _spotLightDirections = new Vector4[MAX_NUM_SPOT_LIGHT];
    private Vector4[] _spotLightAngles = new Vector4[MAX_NUM_SPOT_LIGHT];
    private Vector4[] _spotLightShadowData = new Vector4[MAX_NUM_SPOT_LIGHT];
    private int _spotLightCount = 0;

    // point lights data
    private Vector4[] _pointLightColors = new Vector4[MAX_NUM_POINT_LIGHT];
    private Vector4[] _pointLightPositions = new Vector4[MAX_NUM_POINT_LIGHT];
    private Vector4[] _pointLightShadowData = new Vector4[MAX_NUM_SPOT_LIGHT];
    private int _pointLightCount;

    private CommandBuffer _cmdBuffer = new CommandBuffer() { name = CmdBufferName };
    private ScriptableRenderContext _srContext;
    
    private ShadowManager _shadowMgr = new ShadowManager();

    public void SetUp(ref ScriptableRenderContext srContext, ref CullingResults cullResults, ref ShadowSetting shadowSetting) {
        _dirLightCount = _spotLightCount = _pointLightCount = 0;
        _shadowMgr.SetUp(ref srContext, ref cullResults, ref shadowSetting);
        _srContext = srContext;
        
        int visibleIndex = 0;
        foreach(var visibleLight in cullResults.visibleLights) {
            switch(visibleLight.lightType) {
                case LightType.Directional: {
                    if (_dirLightCount < MAX_NUM_DIRECTIONAL_LIGHT && visibleLight.light.intensity > 0) {
                        _dirLightColors[_dirLightCount] = visibleLight.finalColor.linear;
                        _dirLightDirections[_dirLightCount] = -visibleLight.light.transform.forward;
                        _dirLightShadowData[_dirLightCount] = _shadowMgr.GetDirectionalShadowData(visibleIndex);
                        _dirLightCount++;
                    }
                    break;
                }

                case LightType.Point: {
                    if (_pointLightCount < MAX_NUM_POINT_LIGHT && visibleLight.light.intensity > 0) {
                        _pointLightColors[_pointLightCount] = visibleLight.finalColor.linear;
                        _pointLightPositions[_pointLightCount] = visibleLight.light.transform.position;
                        _pointLightPositions[_pointLightCount].w = visibleLight.light.range;
                        _pointLightShadowData[_pointLightCount] = _shadowMgr.GetPointShadowData(visibleIndex);
                        _pointLightCount++;
                    }
                    break;
                }

                case LightType.Spot: {
                    if (_spotLightCount < MAX_NUM_SPOT_LIGHT && visibleLight.light.intensity > 0) {
                        _spotLightColors[_spotLightCount] = visibleLight.finalColor.linear;
                        _spotLightPositions[_spotLightCount] = visibleLight.light.transform.position;
                        _spotLightPositions[_spotLightCount].w = visibleLight.light.range;
                        _spotLightDirections[_spotLightCount] = -visibleLight.light.transform.forward;
                        _spotLightAngles[_spotLightCount].x = Mathf.Deg2Rad * visibleLight.light.innerSpotAngle;
                        _spotLightAngles[_spotLightCount].y = Mathf.Deg2Rad * visibleLight.spotAngle;
                        _spotLightShadowData[_spotLightCount] = _shadowMgr.GetSpotLightShadowData(visibleIndex);
                        _spotLightCount++;
                    }
                    break;
                }
                
            }

            visibleIndex++;
        }

        _shadowMgr.Render(); // generate shadow map

        _cmdBuffer.BeginSample(CmdBufferName);
        ExecuteCmdBuffer();

        _cmdBuffer.SetGlobalInt(DirLightCountId, _dirLightCount);
        if (_dirLightCount > 0) {
            _cmdBuffer.SetGlobalVectorArray(DirLightColorsId, _dirLightColors);
            _cmdBuffer.SetGlobalVectorArray(DirLightDirectionsId, _dirLightDirections);
            _cmdBuffer.SetGlobalVectorArray(DirLightShadowDataId, _dirLightShadowData);
        }

        _cmdBuffer.SetGlobalInt(SpotLightCountId, _spotLightCount);
        if (_spotLightCount > 0) {
            _cmdBuffer.SetGlobalVectorArray(SpotLightColorsId, _spotLightColors);
            _cmdBuffer.SetGlobalVectorArray(SpotLightPositionsId, _spotLightPositions);
            _cmdBuffer.SetGlobalVectorArray(SpotLightDirectionsId, _spotLightDirections);
            _cmdBuffer.SetGlobalVectorArray(SpotLightAnglesId, _spotLightAngles);
            _cmdBuffer.SetGlobalVectorArray(SpotLightShadowDataId, _spotLightShadowData);
        }

        _cmdBuffer.SetGlobalInt(PointLightCountId, _pointLightCount);
        if (_pointLightCount > 0) {
            _cmdBuffer.SetGlobalVectorArray(PointLightColorsId, _pointLightColors);
            _cmdBuffer.SetGlobalVectorArray(PointLightPositionsId, _pointLightPositions);
            _cmdBuffer.SetGlobalVectorArray(PointLightShadowDataId, _pointLightShadowData);
        }

        _cmdBuffer.EndSample(CmdBufferName);
        ExecuteCmdBuffer();
    }

    public void CleanUp() {
        _shadowMgr.CleanUp();
    }

    void ExecuteCmdBuffer() {
        _srContext.ExecuteCommandBuffer(_cmdBuffer);
        _cmdBuffer.Clear();
    }
}
