
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{
    private CameraRenderer renderer;
    private bool enableDynamicBatching = false;
    private bool useGPUInstancing = false;
    private ShadowSettings shadowSettings;

    public CustomRenderPipeline(bool enableDynamicBatching, bool useGPUInstancing, bool useSRPBatcher, ShadowSettings shadowSettings)
    {
        renderer = new CameraRenderer();

        this.enableDynamicBatching = enableDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;

        // TODO6
        // https://unity.com/cn/blog/engine-platform/srp-batcher-speed-up-your-rendering
        // https://docs.unity3d.com/Manual/SRPBatcher.html
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;

        this.shadowSettings = shadowSettings;
    }
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach(Camera camera in cameras)
        {
            renderer.Render(context, camera, enableDynamicBatching, useGPUInstancing, shadowSettings);
        }
    }
}
