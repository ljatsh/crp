using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    static MaterialPropertyBlock block;
    static int baseColorId = Shader.PropertyToID("_BaseColor");
    static int cutOffId = Shader.PropertyToID("_Cutoff");

    [SerializeField]
    Color baseColor = Color.white;
    [SerializeField, Range(0f, 1f)]
    float cutOff = 0.5f;

    void Awake()
    {
        OnValidate();
    }

    void OnValidate()
    {
        if (block == null)
            block = new MaterialPropertyBlock();

        block.SetColor(baseColorId, baseColor);
        block.SetFloat(cutOffId, cutOff);
        GetComponent<Renderer>().SetPropertyBlock(block);
    }
}
