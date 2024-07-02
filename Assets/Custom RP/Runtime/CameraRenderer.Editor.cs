
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// TODO2 不使用errormaterial, 3个红色的球的表现不一致

public partial class CameraRenderer
{
    partial void PrepareBuffer();
    partial void PrepareForSceneWindow();
    partial void DrawGizmos();
    partial void DrawUnsupportedShaders();

#if UNITY_EDITOR
    string SamplerName { get; set; }
#else
    string SamplerName => bufferName;
#endif

#if UNITY_EDITOR
    // https://docs.unity3d.com/Manual/shader-predefined-pass-tags-built-in.html
    static ShaderTagId[] legacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("ForwardAdd"),
        new ShaderTagId("Deferred"),
        new ShaderTagId("ShadowCaster"),
        // TODO1 打开绘制失效
        //new ShaderTagId("MotionVectors"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM"),
        new ShaderTagId("Meta")
    };

    static Material errorMaterial;

    partial void PrepareBuffer()
    {
        buffer.name = SamplerName = camera.name;
    }

    partial void PrepareForSceneWindow()
    {
        if (camera.cameraType == CameraType.SceneView)
        {
            // TODO3
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }

    partial void DrawUnsupportedShaders()
    {
        if (errorMaterial == null)
        {
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera))
        {
            overrideMaterial = errorMaterial
        };
        for (int index=1; index<legacyShaderTagIds.Length; index++)
        {
            drawingSettings.SetShaderPassName(index, legacyShaderTagIds[index]);
        }

        var filterSettings = FilteringSettings.defaultValue;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filterSettings);
    }

    partial void DrawGizmos()
    {
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }
#endif // UNITY_EDITOR
}
