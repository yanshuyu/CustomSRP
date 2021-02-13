using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class LightManager : MonoBehaviour {
    static int MAX_NUM_DIR_LIGHT = 4;
    static string CmdBufferName = "LightMgrCmdBuffer";

    static int DirLightCountId = Shader.PropertyToID("_DirLightCount");
    static int DirLightColorsId = Shader.PropertyToID("_DirLightColors");
    static int DirLightDirectionsId = Shader.PropertyToID("_DirLightDirections");

    private Vector4[] _dirLightColors = new Vector4[MAX_NUM_DIR_LIGHT];
    private Vector4[] _dirLightDirections = new Vector4[MAX_NUM_DIR_LIGHT];
    private int _dirLightCount = 0;

    private CommandBuffer _cmdBuffer = new CommandBuffer() { name = CmdBufferName };
    private ScriptableRenderContext _srContext;
    
    public void SetUp(ref CullingResults cullingResults, ref ScriptableRenderContext srContext) {
        _srContext = srContext;
        _cmdBuffer.BeginSample(CmdBufferName);
        
        _dirLightCount = 0;
        foreach(var visibleLight in cullingResults.visibleLights) {
            if (_dirLightCount > MAX_NUM_DIR_LIGHT) 
                break;

            if (visibleLight.lightType == LightType.Directional && visibleLight.light.intensity > 0) {
                _dirLightColors[_dirLightCount] = visibleLight.finalColor.linear;
                _dirLightDirections[_dirLightCount] = -visibleLight.light.transform.forward;
                _dirLightCount++;
            } 
        }

        _cmdBuffer.SetGlobalInt(DirLightCountId, _dirLightCount);
        _cmdBuffer.SetGlobalVectorArray(DirLightColorsId, _dirLightColors);
        _cmdBuffer.SetGlobalVectorArray(DirLightDirectionsId, _dirLightDirections);
        _cmdBuffer.EndSample(CmdBufferName);

        ExecuteCmdBuffer();
    }

    private void ExecuteCmdBuffer() {
        _srContext.ExecuteCommandBuffer(_cmdBuffer);
        _cmdBuffer.Clear();
    }
}
