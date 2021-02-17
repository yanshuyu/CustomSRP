using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class LitShaderEditor : ShaderGUI {
    public enum RenderMode {
        Opaque,
        CutOff,
        Fade,
        Transparent,
    }

    static string PROP_mainTex = "_MainTex";
    static string PROP_Color = "_Color";
    static string PROP_CutOff = "_CutOff";
    static string PROP_Metallic = "_Metallic";
    static string PROP_Smoothness = "_Smoothness";
    static string PROP_Frensel = "_Frensel";
    static string PROP_ZWrite = "_ZWrite";
    static string PROP_SrcBlend = "_SrcBlend";
    static string PROP_DstBlend = "_DstBlend";


    static string KW_RenderMode_CutOff = "RENDER_MODE_CUTOFF";
    static string KW_RenderMode_Fade = "RENDER_MODE_FADE";
    static string KW_RenderMode_Transparent = "RENDER_MODE_TRANSPARENT";

    private Material _target;

    RenderMode GetRenderMode() {
        RenderMode renderMode = RenderMode.Opaque;
        if (_target.IsKeywordEnabled(KW_RenderMode_CutOff)) {
            renderMode = RenderMode.CutOff;
        } else if (_target.IsKeywordEnabled(KW_RenderMode_Fade)) {
            renderMode = RenderMode.Fade;
        } else if (_target.IsKeywordEnabled(KW_RenderMode_Transparent))
            renderMode = RenderMode.Transparent;

        return renderMode;
    }

    void SetRenderMode(RenderMode renderMode) {
        _target.DisableKeyword(KW_RenderMode_CutOff);
        _target.DisableKeyword(KW_RenderMode_Fade);
        _target.DisableKeyword(KW_RenderMode_Transparent);

        if (renderMode == RenderMode.Fade || renderMode == RenderMode.Transparent) {
            _target.EnableKeyword( renderMode == RenderMode.Fade ? KW_RenderMode_Fade : KW_RenderMode_Transparent);
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
        
        if (renderMode == RenderMode.Fade || renderMode == RenderMode.Transparent) {
            _target.SetFloat(PROP_ZWrite, 0);
            _target.SetFloat(PROP_SrcBlend, renderMode == RenderMode.Fade ? (float)UnityEngine.Rendering.BlendMode.SrcAlpha : (float)UnityEngine.Rendering.BlendMode.One);
            _target.SetFloat(PROP_DstBlend, (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        } else {
            _target.SetFloat(PROP_ZWrite, 1);
            _target.SetFloat(PROP_SrcBlend, (float)UnityEngine.Rendering.BlendMode.One);
            _target.SetFloat(PROP_DstBlend, (float)UnityEngine.Rendering.BlendMode.Zero);
        }
    }

    
    public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties) {
        _target = editor.target as Material;
        // render mode
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
        var mainTex = FindProperty(PROP_mainTex, properties);
        editor.ShaderProperty(mainTex, mainTex.displayName);

        var col = FindProperty(PROP_Color, properties);
        editor.ShaderProperty(col, col.displayName);


        // metallic & smoothness
        var metallic = FindProperty(PROP_Metallic, properties);
        editor.ShaderProperty(metallic, metallic.displayName);

        var smoothness = FindProperty(PROP_Smoothness, properties);
        editor.ShaderProperty(smoothness, smoothness.displayName);


        // frensel
        var frensel = FindProperty(PROP_Frensel, properties);
        editor.ShaderProperty(frensel, frensel.displayName);

        editor.EnableInstancingField();

    }
}
