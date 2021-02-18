using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class UlitInstancingPerObjectProperty : MonoBehaviour
{
    public Color color = Color.white;
    public Color emission = Color.black;
    [Range(0, 1)] public float alphaCutOff = 0.1f;
    private static MaterialPropertyBlock mtlPropBlock;

    private void Awake() {
        OnValidate();
    }

    private void OnValidate() {
        if (mtlPropBlock == null)
            mtlPropBlock = new MaterialPropertyBlock();
        mtlPropBlock.SetColor("_Color", color);
        mtlPropBlock.SetFloat("_CutOff", alphaCutOff);
        mtlPropBlock.SetColor("_Emission", emission);
        GetComponent<Renderer>().SetPropertyBlock(mtlPropBlock);
    }
}
