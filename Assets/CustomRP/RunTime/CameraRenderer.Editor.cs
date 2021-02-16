using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer {
#if UNITY_EDITOR
    static Material errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));

    static ShaderTagId[] unSupportedShaderTagIds = {
        new ShaderTagId("Always"),
        //new ShaderTagId("ShadowCaster"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("ForwardAdd"),
        new ShaderTagId("PrepassBase"), // legacy deferred lighting
        new ShaderTagId("PrepassFinal"), // legacy deferred lighting
        new ShaderTagId("Deferred"),
        new ShaderTagId("Vertex"), //legacy vert lit
        new ShaderTagId("VertexLMRGBM"), // legacy vert lit
        new ShaderTagId("VertexLM"), // legacy vert lit
    };

  
    partial void DrawUnsupportedShaders() {
        var drawSettings = new DrawingSettings();
        var filterSettings = FilteringSettings.defaultValue;
        drawSettings.sortingSettings = new SortingSettings(_camera);
        drawSettings.overrideMaterial = errorMaterial;
        for (int i=0; i<unSupportedShaderTagIds.Length; i++) {
            drawSettings.SetShaderPassName(i, unSupportedShaderTagIds[i]);
        }

        _context.DrawRenderers(_cullResults, ref drawSettings, ref filterSettings);
    }


    partial void DrawGizmos() {
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
