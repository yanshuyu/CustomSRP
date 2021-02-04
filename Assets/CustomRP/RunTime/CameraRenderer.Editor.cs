﻿using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer : MonoBehaviour {
#if UNITY_EDITOR
    static Material errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));

    static ShaderTagId[] unSupportedShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM"),
    };

  
    partial void DebugDrawUnsupportedShaders() {
        var drawSettings = new DrawingSettings();
        var filterSettings = FilteringSettings.defaultValue;
        drawSettings.sortingSettings = new SortingSettings(_camera);
        drawSettings.overrideMaterial = errorMaterial;
        for (int i=0; i<unSupportedShaderTagIds.Length; i++) {
            drawSettings.SetShaderPassName(i, unSupportedShaderTagIds[i]);
        }

        _context.DrawRenderers(_cullResults, ref drawSettings, ref filterSettings);
    }


    partial void DebugDrawGizmos() {
        if (UnityEditor.Handles.ShouldRenderGizmos()) {
            _context.DrawGizmos(_camera, GizmoSubset.PreImageEffects);
            _context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
        }
    }


    partial void EmitSceneUIGeometry() {
        if (_camera.cameraType == CameraType.SceneView) {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(_camera);
        }
    }

#endif

}
