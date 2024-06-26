
using System;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraRenderer
{
    private const string bufferName = "Render Camera";
    private ScriptableRenderContext context;
    private Camera camera;
    private CommandBuffer buffer;

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

        Setup();

        DrawVisibleObject();

        Submit();
    }

    private void Setup()
    {
        context.SetupCameraProperties(camera);
        buffer.ClearRenderTarget(true, true, Color.clear);
        buffer.BeginSample(bufferName);
        ExecuteBuffer();
    }

    private void DrawVisibleObject()
    {
        context.DrawSkybox(camera);
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
}
