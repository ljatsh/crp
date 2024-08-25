
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SocialPlatforms;

public class Shadows
{
    struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
    };

    const string bufferName = "Shadows";
    const int maxShadowedDirectionalLightCount = 4;

    static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
    static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");

    static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount];

    CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName
    };

    ScriptableRenderContext context;
    CullingResults cullingResults;
    ShadowSettings settings;

    int shadowedDirectionalLightCount;
    ShadowedDirectionalLight[] shadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings)
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = settings;

        shadowedDirectionalLightCount = 0;
    }

    public Vector2 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (shadowedDirectionalLightCount >= maxShadowedDirectionalLightCount)
            return Vector2.zero;

        if (light.shadows == LightShadows.None || light.shadowStrength <= 0f)
            return Vector2.zero;

        if (!cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
            return Vector2.zero;

        shadowedDirectionalLights[shadowedDirectionalLightCount].visibleLightIndex = visibleLightIndex;

        return new Vector2(light.shadowStrength, shadowedDirectionalLightCount++);
    }

    public void Render()
    {
        if (shadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            buffer.GetTemporaryRT(dirShadowAtlasId, 1, 1, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }
    }

    public void Cleanup()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExecuteBuffer();
    }

    private void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private void RenderDirectionalShadows()
    {
        int atlasSize = (int)settings.directional.atlasSize;
        buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize,
            32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        buffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.ClearRenderTarget(true, false, Color.clear);

        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        int split = shadowedDirectionalLightCount <= 1 ? 1 : 2;
        int tileSize = atlasSize / split;

        for (int i=0; i<shadowedDirectionalLightCount; i++)
        {
            RenderDirectionalShadows(i, split, tileSize);
        }

        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);

        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    private void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = shadowedDirectionalLights[index];
        ShadowDrawingSettings shadowSettings = new ShadowDrawingSettings(
            cullingResults, light.visibleLightIndex,
            BatchCullingProjectionType.Orthographic
        );
        cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
            light.visibleLightIndex, 0, 1, Vector3.zero, tileSize, 0f,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData
        );
        shadowSettings.splitData = shadowSplitData;
        dirShadowMatrices[index] = ConvertToAtlasMatrix(projMatrix, viewMatrix,
            SetTileViewPort(index, split, tileSize), split);
        buffer.SetViewProjectionMatrices(viewMatrix, projMatrix);

        ExecuteBuffer();
        context.DrawShadows(ref shadowSettings);
    }

    private Vector2 SetTileViewPort(int index, int split, float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));

        return offset;
    }

    private Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 proj, Matrix4x4 view, Vector2 offset, int split)
    {
        // if (SystemInfo.usesReversedZBuffer)
        // {
        //     proj.m20 = -proj.m20;
        //     proj.m21 = -proj.m21;
        //     proj.m22 = -proj.m22;
        //     proj.m23 = -proj.m23;
        // }

        Matrix4x4 m = proj * view;

        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }

        // [-1, 1] => [0, 1]
        m.m00 = 0.5f * (m.m00 + m.m30);
		m.m01 = 0.5f * (m.m01 + m.m31);
		m.m02 = 0.5f * (m.m02 + m.m32);
		m.m03 = 0.5f * (m.m03 + m.m33);
		m.m10 = 0.5f * (m.m10 + m.m30);
		m.m11 = 0.5f * (m.m11 + m.m31);
		m.m12 = 0.5f * (m.m12 + m.m32);
		m.m13 = 0.5f * (m.m13 + m.m33);
		m.m20 = 0.5f * (m.m20 + m.m30);
		m.m21 = 0.5f * (m.m21 + m.m31);
		m.m22 = 0.5f * (m.m22 + m.m32);
		m.m23 = 0.5f * (m.m23 + m.m33);

        // 4 Directional Shadows
        //   3(0, 1)       3(1, 1)
        //   0(0, 0)       1(1, 0)

        float scale = 1f / split;
        Matrix4x4 t = Matrix4x4.identity;
        t.m00 = scale;
        t.m11 = scale;
        t.m03 = offset.x * scale;
        t.m13 = offset.y * scale;

        m = t * m;
        return m;
    }
}
