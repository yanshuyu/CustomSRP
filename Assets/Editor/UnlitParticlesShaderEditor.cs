using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class UnlitParticlesShaderEditor : ShaderGUI {
    public enum RenderMode {
        Opaque,
        CutOff,
        Fade,
    }

    static string PROP_mainTex = "_MainTex";
    static string PROP_EmissibeTex = "_EmissiveTex";
    static string PROP_Emission = "_Emission";
    static string PROP_Color = "_Color";
    static string PROP_CutOff = "_CutOff";
    static string PROP_ZWrite = "_ZWrite";
    static string PROP_SrcBlend = "_SrcBlend";
    static string PROP_DstBlend = "_DstBlend";
    static string PROP_NearFadeDistance = "_NearFadeDistance";
    static string PROP_NearFadeRange = "_NearFadeRange";

    static string KW_RenderMode_CutOff = "RENDER_MODE_CUTOFF";
    static string KW_RenderMode_Fade = "RENDER_MODE_FADE";
    static string KW_VertexColor = "VERTEX_COLOR";
    static string KW_BlendingSheetAnim = "BLENDING_SHEET_ANIMATION";
    static string KW_NearFade = "PARTICLE_NEAR_FADE";

    private Material _target;

    RenderMode GetRenderMode() {
        RenderMode renderMode = RenderMode.Opaque;
        if (_target.IsKeywordEnabled(KW_RenderMode_CutOff)) {
            renderMode = RenderMode.CutOff;
        } else if (_target.IsKeywordEnabled(KW_RenderMode_Fade)) {
            renderMode = RenderMode.Fade;
        }

        return renderMode;
    }

    void SetRenderMode(RenderMode renderMode) {
        _target.DisableKeyword(KW_RenderMode_CutOff);
        _target.DisableKeyword(KW_RenderMode_Fade);

        if (renderMode == RenderMode.Fade) {
            _target.EnableKeyword(KW_RenderMode_Fade);
            _target.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            _target.SetOverrideTag("RenderType", "Transparent");
     
        } else if (renderMode == RenderMode.CutOff) {
            _target.EnableKeyword(KW_RenderMode_CutOff);
            _target.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            _target.SetOverrideTag("RenderType", "Opaque");

        } else {
            _target.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            _target.SetOverrideTag("RenderType", "Opaque");
        }
        
        if (renderMode == RenderMode.Fade) {
            _target.SetFloat(PROP_ZWrite, 0);
            _target.SetFloat(PROP_SrcBlend, (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _target.SetFloat(PROP_DstBlend, (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        } else {
            _target.SetFloat(PROP_ZWrite, 1);
            _target.SetFloat(PROP_SrcBlend, (float)UnityEngine.Rendering.BlendMode.One);
            _target.SetFloat(PROP_DstBlend, (float)UnityEngine.Rendering.BlendMode.Zero);
        }
    }

    
    public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties) {
        _target = editor.target as Material;
        RenderMode renderMode = GetRenderMode();
        EditorGUI.BeginChangeCheck();
        renderMode = (RenderMode)EditorGUILayout.EnumPopup("Render Mode", renderMode);
        if (EditorGUI.EndChangeCheck()) {
            editor.RegisterPropertyChangeUndo("Render Mode");
            SetRenderMode(renderMode);
        }

        if (renderMode == RenderMode.CutOff) {
            var cutOff = FindProperty(PROP_CutOff, properties);
            editor.ShaderProperty(cutOff, cutOff.displayName);
        }

        // main texture & tint color
        var mainMap = FindProperty(PROP_mainTex, properties);
        var col = FindProperty(PROP_Color, properties);
        editor.TexturePropertySingleLine(new GUIContent("Base", mainMap.displayName), mainMap, col);

        // emission
        var emissiveMap = FindProperty(PROP_EmissibeTex, properties);
        var emission = FindProperty(PROP_Emission, properties);
        editor.TexturePropertySingleLine(new GUIContent("Emission", emissiveMap.displayName), emissiveMap, emission);

        editor.TextureScaleOffsetProperty(mainMap);
        
        EditorGUI.BeginChangeCheck();
        bool vertexColEnabled = GUILayout.Toggle(_target.IsKeywordEnabled(KW_VertexColor), "Use Vertex Color");
        if (EditorGUI.EndChangeCheck()) {
            if (vertexColEnabled)
                _target.EnableKeyword(KW_VertexColor);
            else 
                _target.DisableKeyword(KW_VertexColor);
        }

        EditorGUI.BeginChangeCheck();
        bool animBlending = GUILayout.Toggle(_target.IsKeywordEnabled(KW_BlendingSheetAnim), "Blend Sheet Animation");
        if (EditorGUI.EndChangeCheck())
        {
            if (animBlending)
                _target.EnableKeyword(KW_BlendingSheetAnim);
            else
                _target.DisableKeyword(KW_BlendingSheetAnim);
        }

        bool isNearFade = _target.IsKeywordEnabled(KW_NearFade);
        EditorGUI.BeginChangeCheck();
        isNearFade = GUILayout.Toggle(isNearFade, "Fade Near");
        if (EditorGUI.EndChangeCheck()) {
            if (isNearFade) 
                _target.EnableKeyword(KW_NearFade);
            else 
                _target.DisableKeyword(KW_NearFade);
        }

        if (isNearFade) {
            var nearDistance = FindProperty(PROP_NearFadeDistance, properties);
            var nearFadeRange = FindProperty(PROP_NearFadeRange, properties);
            editor.ShaderProperty(nearDistance, nearDistance.displayName);
            editor.ShaderProperty(nearFadeRange, nearFadeRange.displayName);
        }


        editor.EnableInstancingField();

    }
}
