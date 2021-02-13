using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class LitInstancingPerObjectProperty : MonoBehaviour
{
    public Color color = Color.white;
    [Range(0, 1)] public float alphaCutOff = 0.1f;
    [Range(0, 1)] public float metallic = 0;
    [Range(0, 1)] public float smoothness = 0.5f;

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
        GetComponent<Renderer>().SetPropertyBlock(mtlPropBlock);
    }
}
