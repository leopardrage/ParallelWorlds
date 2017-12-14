using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Vignette : MonoBehaviour
{
    [Range(0, 1)] public float minRadius = 0.3f;
    [Range(0, 1)] public float maxRadius = 1.0f;
    [Range(0, 1)] public float saturation = 1.0f;

    [SerializeField] private Shader _shader;

    private Material _material;

    private void OnEnable()
    {
        _material = new Material(_shader);
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        _material.SetFloat("_MinRadius", minRadius);
        _material.SetFloat("_MaxRadius", maxRadius);
        _material.SetFloat("_Saturation", saturation);

        Graphics.Blit(src, dst, _material, 0);
    }
}