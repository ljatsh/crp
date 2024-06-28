
using System;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    // https://docs.unity3d.com/Manual/SL-PassTags.html
    private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    
   private const string bufferName = "Render Camera";
    private ScriptableRenderContext context;
    private Camera camera;
    private CommandBuffer buffer;
    private CullingResults cullingResults;

    public CameraRenderer()
    {
        buffer = new CommandBuffer() {
            name = bufferName
        };
    }

    public void Render(ScriptableRenderContext context, Camera camera)
    {
        this.context = context;
        this.camera = camera;

        // Cull和GPU无关
        if (!Cull())
            return;

        Setup();

        DrawVisibleObjects();
        DrawUnsupportedShaders();
        DrawGizmos();

        Submit();
    }

    private void Setup()
    {
        context.SetupCameraProperties(camera);
        buffer.ClearRenderTarget(true, true, Color.clear);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();
    }

    private void DrawVisibleObjects()
    {
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        context.DrawSkybox(camera);

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    private void Submit()
    {
        buffer.EndSample(bufferName);
        ExecuteBuffer();

        context.Submit();
    }

    private void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private bool Cull()
    {
        if (!camera.TryGetCullingParameters(out ScriptableCullingParameters parameters))
        {
            return false;
        }

        cullingResults = context.Cull(ref parameters);
        return true;
    }
}
