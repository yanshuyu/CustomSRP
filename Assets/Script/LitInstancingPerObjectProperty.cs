using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class LitInstancingPerObjectProperty : MonoBehaviour
{
    [Header("Base")]
    public Color color = Color.white;
    public Color emission = Color.black;
    [Range(0, 1)] public float alphaCutOff = 0.1f;
    [Range(0, 1)] public float metallic = 0;
    [Range(0, 1)] public float smoothness = 0.5f;
    [Range(0, 1)] public float occllusion = 1f;
    [Range(0, 1)] public float frensel = 1f;
    [Range(0, 1)] public float normalScale = 1f;

    [Header("Detail")]
    [Range(0, 1)] public float detailAlbedo = 1f;
    [Range(0, 1)] public float detailSmoothness = 1f;
    [Range(0, 1)] public float detailNormalScale = 1f;

    private static MaterialPropertyBlock mtlPropBlock;

    private void Awake() {
        OnValidate();
    }

    private void OnValidate() {
        if (mtlPropBlock == null)
            mtlPropBlock = new MaterialPropertyBlock();
        mtlPropBlock.SetColor("_Color", color);
        mtlPropBlock.SetFloat("_CutOff", alphaCutOff);
        mtlPropBlock.SetFloat("_Metallic", metallic);
        mtlPropBlock.SetFloat("_Smoothness", smoothness);
        mtlPropBlock.SetFloat("_Frensel", frensel);
        mtlPropBlock.SetColor("_Emission", emission);
        mtlPropBlock.SetFloat("_Occllusion", occllusion);
        mtlPropBlock.SetFloat("_DetailAlbedo", detailAlbedo);
        mtlPropBlock.SetFloat("_DetailSmoothness", detailSmoothness);
        mtlPropBlock.SetFloat("_NormalScale", normalScale);
        mtlPropBlock.SetFloat("_DetailNormalScale", detailNormalScale);
        
        GetComponent<Renderer>().SetPropertyBlock(mtlPropBlock);
    }
}
