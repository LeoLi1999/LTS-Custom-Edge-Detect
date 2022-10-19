using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class LTSFeatureDepthNormalDecode : ScriptableRendererFeature
{
    static readonly string profilerTag = "LTS Depth Normal Decode Pass";

    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingGbuffer;
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
            settings.shader = Shader.Find("LTS/PostProcessing/DepthNormalDecode");
            this.material = CoreUtils.CreateEngineMaterial(settings.shader);
        }

        // 声明RT相关参数
        static readonly int tempPid = Shader.PropertyToID("_CameraDepthNormalDecodeTexture");
        RenderTargetIdentifier temp = new RenderTargetIdentifier(tempPid);

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            int RTWidth = renderingData.cameraData.camera.scaledPixelWidth;
            int RTHeight = renderingData.cameraData.camera.scaledPixelHeight;
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(RTWidth, RTHeight);
            descriptor.colorFormat = RenderTextureFormat.ARGB32;
            descriptor.depthBufferBits = 32;
            // 获取RT
            cmd.GetTemporaryRT(tempPid, descriptor);
            ConfigureTarget(temp);
            ConfigureClear(ClearFlag.All, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // 获取相机参数，可以根据相机参数去指定pass在某一特定相机下实现
            ref CameraData cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;
            var source = cameraData.renderer.cameraColorTarget;
            
            // 如果相机不是Scene相机，并且名字不是Main Camera则不执行
            if (!cameraData.isSceneViewCamera && camera.name != "Main Camera") return;
            // 如果相机没开后处理则不执行
            if (!cameraData.postProcessEnabled) return;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, new ProfilingSampler(profilerTag)))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                cmd.Blit(source, temp, material, 0);
                cmd.SetGlobalTexture(tempPid, temp);
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
