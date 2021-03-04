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
    static string PROP_MaskTex = "_MaskTex";
    static string PROP_Metallic = "_Metallic";
    static string PROP_Smoothness = "_Smoothness";
    static string PROP_EmissibeTex = "_EmissiveTex";
    static string PROP_Emission = "_Emission";
    static string PROP_Occllusion = "_Occllusion";
    static string PROP_Frensel = "_Frensel";
    static string PROP_NormalTex = "_NormalTex";
    static string PROP_NormalScale = "_NormalScale";

    static string PROP_DetailTex = "_DetailTex";
    static string PROP_DetailNormalTex = "_DetailNormalTex";
    static string PROP_DetailAlbedo = "_DetailAlbedo";
    static string PROP_DetailSmoothness = "_DetailSmoothness";
    static string PROP_DetailNormalScale = "_DetailNormalScale";
    

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

        
        GUILayout.Label("Base Properties", EditorStyles.boldLabel);

        // main texture & tint color
        var mainMap = FindProperty(PROP_mainTex, properties);
        var col = FindProperty(PROP_Color, properties);
        editor.TexturePropertySingleLine(new GUIContent("Base", mainMap.displayName), mainMap, col);

        // emission
        var emissiveMap = FindProperty(PROP_EmissibeTex, properties);
        var emission = FindProperty(PROP_Emission, properties);
        editor.TexturePropertySingleLine(new GUIContent("Emission", emissiveMap.displayName), emissiveMap, emission);

        // normal map
        var normalMap = FindProperty(PROP_NormalTex, properties);
        editor.TexturePropertySingleLine(new GUIContent("Normal", normalMap.displayName), normalMap);

        // metallic/occlusion/detail/smoothness mask map 
        var modsMap = FindProperty(PROP_MaskTex, properties);
        editor.TexturePropertySingleLine(new GUIContent("MODS", modsMap.displayName), modsMap);

        // metallic & smoothness
        var metallic = FindProperty(PROP_Metallic, properties);
        editor.ShaderProperty(metallic, metallic.displayName);

        var smoothness = FindProperty(PROP_Smoothness, properties);
        editor.ShaderProperty(smoothness, smoothness.displayName);

        // occllusion
        var occllusion = FindProperty(PROP_Occllusion, properties);
        editor.ShaderProperty(occllusion, occllusion.displayName);

        // normal scale
        var normalScale = FindProperty(PROP_NormalScale, properties);
        editor.ShaderProperty(normalScale, normalScale.displayName);

        // frensel
        var frensel = FindProperty(PROP_Frensel, properties);
        editor.ShaderProperty(frensel, frensel.displayName);
        
        editor.TextureScaleOffsetProperty(mainMap);


        GUILayout.Label("Detail Properties", EditorStyles.boldLabel);

        var detailMap = FindProperty(PROP_DetailTex, properties);
        editor.TexturePropertySingleLine(new GUIContent("Detail", detailMap.displayName), detailMap);

        var detailNormalMap = FindProperty(PROP_DetailNormalTex, properties);
        editor.TexturePropertySingleLine(new GUIContent("Normal", detailNormalMap.displayName), detailNormalMap);
       
        var detailAlbedo = FindProperty(PROP_DetailAlbedo, properties);
        var detailSmoothness = FindProperty(PROP_DetailSmoothness, properties);
        var detailNormalScale = FindProperty(PROP_DetailNormalScale, properties);
        editor.ShaderProperty(detailAlbedo, detailAlbedo.displayName);
        editor.ShaderProperty(detailSmoothness, detailSmoothness.displayName);
        editor.ShaderProperty(detailNormalScale, detailNormalScale.displayName);

        editor.TextureScaleOffsetProperty(detailMap);


        GUILayout.Label("Advance Options", EditorStyles.boldLabel);
        editor.EnableInstancingField();

        EditorGUI.BeginChangeCheck();
        editor.LightmapEmissionProperty();
        if (EditorGUI.EndChangeCheck())
        {
            _target.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }

    }
}
