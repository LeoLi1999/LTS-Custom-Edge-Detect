using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LTSFeatureDepthNormal : ScriptableRendererFeature
{
    static readonly string profilerTag = "LTS Depth Normal Pass";

    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
        public Shader shader = null;
        public enum RenderQueueType {All, Opaque, Transparent};
        public RenderQueueType queue = RenderQueueType.Opaque;
        public LayerMask layerMask = -1;
    }
    private static RenderQueueRange GetQueueRange(Settings.RenderQueueType queue)
    {
        switch(queue) 
        {
            case Settings.RenderQueueType.All          : return RenderQueueRange.all;
            case Settings.RenderQueueType.Opaque       : return RenderQueueRange.opaque;
            case Settings.RenderQueueType.Transparent  : return RenderQueueRange.transparent;
            default                                    : return RenderQueueRange.opaque;
        }
    }
    public Settings settings = new Settings();

    class CustomRenderPass: ScriptableRenderPass
    {
        private Settings settings;
        private Material material;
        private FilteringSettings filteringSettings;
      
        public CustomRenderPass(Settings settings)
        {
            this.settings = settings;
            this.renderPassEvent = settings.renderPassEvent;
            settings.shader = Shader.Find("LTS/PostProcessing/DepthNormalCreate");
            // settings.shader = Shader.Find("Hidden/Internal-DepthNormalsTexture");
            this.material = CoreUtils.CreateEngineMaterial(settings.shader);
            this.filteringSettings = new FilteringSettings(GetQueueRange(settings.queue), settings.layerMask);
        }

        static readonly int tempPid = Shader.PropertyToID("_CameraDepthNormalTexture");
        RenderTargetIdentifier temp = new RenderTargetIdentifier(tempPid);

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            int RTWidth = renderingData.cameraData.camera.scaledPixelWidth;
            int RTHeight = renderingData.cameraData.camera.scaledPixelHeight;
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(RTWidth, RTHeight);
            descriptor.colorFormat = RenderTextureFormat.ARGB32;
            descriptor.depthBufferBits = 32;
            cmd.GetTemporaryRT(tempPid, descriptor, FilterMode.Point);
            ConfigureTarget(temp);
            ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;

            if (!cameraData.isSceneViewCamera && camera.name != "Main Camera") return;
            if (!renderingData.cameraData.postProcessEnabled) return;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, new ProfilingSampler(profilerTag)))
            {
                var shaderTagId = new ShaderTagId("UniversalForward");
                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(shaderTagId, ref renderingData, sortFlags);
                drawSettings.overrideMaterial = material;
                drawSettings.perObjectData = PerObjectData.None;
                
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);
                cmd.SetGlobalTexture(tempPid, temp);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(tempPid);
        }
    }

    CustomRenderPass myRenderPass;

    public override void Create()
    {
        myRenderPass = new CustomRenderPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(myRenderPass);
    }
}