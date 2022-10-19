using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class LTSFeatureEdgeDetect : ScriptableRendererFeature
{
    static readonly string profilerTag = "LTS Edge Detect";

    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public Shader shader = null;
    }
    public Settings settings = new Settings();

    class CustomRenderPass: ScriptableRenderPass
    {
        private Settings settings;
        private Material material;
        
        public CustomRenderPass(Settings settings)
        {
            this.settings = settings;
            this.renderPassEvent = settings.renderPassEvent;
            settings.shader = Shader.Find("LTS/PostProcessing/EdgeDetect");
            this.material = CoreUtils.CreateEngineMaterial(settings.shader);
        }

        // 声明VolumeComponent
        LTSVolumeEdgeDetect volume;

        // 声明RT相关参数
        static readonly int tempPid = Shader.PropertyToID("_TempTex");
        RenderTargetIdentifier temp = new RenderTargetIdentifier(tempPid);

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // 实例化VolumeComponent并获取VolumeComponent参数
            var stack = VolumeManager.instance.stack;
            volume = stack.GetComponent<LTSVolumeEdgeDetect>();

            int RTWidth = renderingData.cameraData.camera.scaledPixelWidth;
            int RTHeight = renderingData.cameraData.camera.scaledPixelHeight;
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(RTWidth, RTHeight);
            cmd.GetTemporaryRT(tempPid, descriptor, FilterMode.Bilinear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref CameraData cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;
            var source = cameraData.renderer.cameraColorTarget;
            
            if (!cameraData.postProcessEnabled) return;
            if (!cameraData.isSceneViewCamera && camera.name != "Main Camera") return;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, new ProfilingSampler(profilerTag)))
            {
                if (volume.IsActive())
                {
                    material.SetKeyword(new LocalKeyword(settings.shader, "_UseEdgeDetect"), volume.useEdgeDetect.value);
                    material.SetKeyword(new LocalKeyword(settings.shader, "_UseDepthNormal"), volume.useDepthNormal.value);
                    material.SetKeyword(new LocalKeyword(settings.shader, "_UseDecodeDepthNormal"), volume.useDecodeDepthNormal.value);
                    material.SetColor("_EdgeColor", volume.edgeColor.value);
                    material.SetColor("_BackgroundColor", volume.backgroundColor.value);
                    material.SetFloat("_EdgeOnly", volume.edgeOnly.value);
                }
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                cmd.Blit(source, temp, material, 0);
                cmd.Blit(temp, source);
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            // 释放RT
            cmd.ReleaseTemporaryRT(tempPid);
        }
    }
    
    CustomRenderPass myPass;

    public override void Create()
    {
        myPass = new CustomRenderPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(myPass);
    }
}
