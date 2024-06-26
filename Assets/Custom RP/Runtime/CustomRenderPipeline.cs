
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{

    private CameraRenderer renderer;
    public CustomRenderPipeline()
    {
        renderer = new CameraRenderer();
    }
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach(Camera camera in cameras)
        {
            renderer.Render(context, camera);
        }
    }
}
