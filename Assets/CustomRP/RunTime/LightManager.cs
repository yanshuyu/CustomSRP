using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LightManager {
    public static readonly int MAX_NUM_DIRECTIONAL_LIGHT = 4;
    static readonly string CmdBufferName = "Lights";

    static int DirLightCountId = Shader.PropertyToID("_DirLightCount");
    static int DirLightColorsId = Shader.PropertyToID("_DirLightColors");
    static int DirLightDirectionsId = Shader.PropertyToID("_DirLightDirections");
    static int DirLightShadowDataId = Shader.PropertyToID("_DirLightShadowData");
    static int DirLightShadowTileIndicesId = Shader.PropertyToID("_DirLightShadowTileIndices");

    private Vector4[] _dirLightColors = new Vector4[MAX_NUM_DIRECTIONAL_LIGHT];
    private Vector4[] _dirLightDirections = new Vector4[MAX_NUM_DIRECTIONAL_LIGHT];
    private Vector4[] _dirLightShadowData = new Vector4[MAX_NUM_DIRECTIONAL_LIGHT];
    private float[] _dirLightShadowTileIndices = new float[MAX_NUM_DIRECTIONAL_LIGHT];
    private int _dirLightCount = 0;

    private CommandBuffer _cmdBuffer = new CommandBuffer() { name = CmdBufferName };
    private ScriptableRenderContext _srContext;
    
    private ShadowManager _shadowMgr = new ShadowManager();

    public void SetUp(ref ScriptableRenderContext srContext, ref CullingResults cullResults, ref ShadowSetting shadowSetting) {
        _shadowMgr.SetUp(ref srContext);

        _srContext = srContext;
     
        _dirLightCount = 0;
        int visibleIndex = 0;
        foreach(var visibleLight in cullResults.visibleLights) {
            if (_dirLightCount > MAX_NUM_DIRECTIONAL_LIGHT) 
                break;
           
            if (visibleLight.lightType == LightType.Directional && visibleLight.light.intensity > 0) {
                _dirLightColors[_dirLightCount] = visibleLight.finalColor.linear;
                _dirLightDirections[_dirLightCount] = -visibleLight.light.transform.forward;
                _dirLightShadowTileIndices[_dirLightCount] = _shadowMgr.GetDirectionalShadowData(ref cullResults, visibleIndex, out Vector4 shadowData);
                _dirLightShadowData[_dirLightCount] = shadowData;
                _dirLightCount++;
            } 

            visibleIndex++;
        }

        _shadowMgr.Render(ref cullResults, ref shadowSetting); // generate shadow map

        _cmdBuffer.BeginSample(CmdBufferName);
        ExecuteCmdBuffer();

        _cmdBuffer.SetGlobalInt(DirLightCountId, _dirLightCount);
        _cmdBuffer.SetGlobalVectorArray(DirLightColorsId, _dirLightColors);
        _cmdBuffer.SetGlobalVectorArray(DirLightDirectionsId, _dirLightDirections);
        _cmdBuffer.SetGlobalVectorArray(DirLightShadowDataId, _dirLightShadowData);
        _cmdBuffer.SetGlobalFloatArray(DirLightShadowTileIndicesId, _dirLightShadowTileIndices);

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
