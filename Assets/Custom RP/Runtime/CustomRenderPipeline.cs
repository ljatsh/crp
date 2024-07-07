
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{

    private CameraRenderer renderer;
    public CustomRenderPipeline()
    {
        renderer = new CameraRenderer();

        // TODO6
        // https://unity.com/cn/blog/engine-platform/srp-batcher-speed-up-your-rendering
        // https://docs.unity3d.com/Manual/SRPBatcher.html
        GraphicsSettings.useScriptableRenderPipelineBatching = true;
    }
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach(Camera camera in cameras)
        {
            renderer.Render(context, camera);
        }
    }
}
