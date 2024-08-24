
using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    // https://docs.unity3d.com/Manual/SL-PassTags.html
    private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    private static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");
    
    private const string bufferName = "Render Camera";
    private ScriptableRenderContext context;
    private Camera camera;
    private CommandBuffer buffer;
    private CullingResults cullingResults;

    private Lighting lighting = new Lighting();

    public CameraRenderer()
    {
        buffer = new CommandBuffer() {
            name = bufferName
        };
    }

    public void Render(ScriptableRenderContext context, Camera camera, bool enableDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings)
    {
        this.context = context;
        this.camera = camera;

        PrepareBuffer();
        PrepareForSceneWindow();

        // Cull和GPU无关 TODO5 放在Setup后面, Profile很奇怪
        if (!Cull(shadowSettings.maxDistance))
            return;

        buffer.BeginSample(SamplerName);
        ExecuteBuffer();
        lighting.Setup(context, cullingResults, shadowSettings);
        buffer.EndSample(SamplerName);
        Setup();

        DrawVisibleObjects(enableDynamicBatching, useGPUInstancing);
        DrawUnsupportedShaders();
        DrawGizmos();

        lighting.Cleanup();
        Submit();
    }

    private void Setup()
    {
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
        buffer.ClearRenderTarget(flags <= CameraClearFlags.Nothing,
            flags <= CameraClearFlags.Color, // TODO4 y not flags == CameraClearFlags.Color
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        buffer.BeginSample(SamplerName);
        ExecuteBuffer();
    }

    private void DrawVisibleObjects(bool enableDynamicBatching, bool useGPUInstancing)
    {
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
        {
            enableDynamicBatching = enableDynamicBatching,
            enableInstancing = useGPUInstancing
        };
        drawingSettings.SetShaderPassName(1, litShaderTagId);
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        if (camera.clearFlags == CameraClearFlags.Skybox)
            context.DrawSkybox(camera);

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    private void Submit()
    {
        buffer.EndSample(SamplerName);
        ExecuteBuffer();

        context.Submit();
    }

    private void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private bool Cull(float maxShadowDistance)
    {
        if (!camera.TryGetCullingParameters(out ScriptableCullingParameters parameters))
        {
            return false;
        }

        parameters.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
        cullingResults = context.Cull(ref parameters);
        return true;
    }
}
