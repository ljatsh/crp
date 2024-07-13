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
    static int metallicId = Shader.PropertyToID("_Metallic");
    static int smoothnessId = Shader.PropertyToID("_Smoothness");

    [SerializeField]
    Color baseColor = Color.white;
    [SerializeField, Range(0f, 1f)]
    float cutOff = 0.5f;
    [SerializeField, Range(0f, 1f)]
    float metallic = 0;
    [SerializeField, Range(0f, 1f)]
    float smoothness = 0.5f;

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
        block.SetFloat(metallicId, metallic);
        block.SetFloat(smoothnessId, smoothness);
        GetComponent<Renderer>().SetPropertyBlock(block);
    }
}
