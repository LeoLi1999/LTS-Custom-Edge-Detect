Shader "LTS/PostProcessing/DepthNormalDecode"
{
    Properties
    {

    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" "IgnoreProjector" = "True"}

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_CameraDepthNormalTexture);          SAMPLER(sampler_CameraDepthNormalTexture);
        uniform real4 _MainTex_TexelSize;

        CBUFFER_START(UnityPerMaterial)
        
        CBUFFER_END

        float3 DecodeViewNormalStereo(float4 enc4)
        {
            float kScale = 1.7777;
            float3 nn = enc4.xyz * float3(2 * kScale, 2 * kScale, 0) + float3(-kScale, -kScale, 1);
            float g = 2.0 / dot(nn.xyz, nn.xyz);
            float3 n;
            n.xy = g * nn.xy;
            n.z = g - 1;
            return n;
        }

        struct VerrtexInput
        {
            real4 positionOS : POSITION;
            real2 uv         : TEXCOORD0; 
        };

        struct VertexOutput
        {
            real4 positionCS : SV_POSITION;
            real2 uv         : TEXCOORD0;
        };

        VertexOutput vert(VerrtexInput v)
        {
            VertexOutput o = (VertexOutput)0;
            o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
            o.uv = v.uv;
            //当有多个RenderTarget时，需要自己处理UV翻转问题
            #if UNITY_UV_STARTS_AT_TOP //DirectX之类的
                if (_MainTex_TexelSize.y < 0) //开启了抗锯齿
                o.uv.y = 1 - o.uv.y; //满足上面两个条件时uv会翻转，因此需要转回来
            #endif
            return o;
        }

        real4 frag(VertexOutput i):SV_TARGET
        {
            real4 var_DepthNormalsTexture = SAMPLE_TEXTURE2D(_CameraDepthNormalTexture, sampler_CameraDepthNormalTexture, UnityStereoTransformScreenSpaceTex(i.uv));
            real3 depthNormal = DecodeViewNormalStereo(var_DepthNormalsTexture);
            depthNormal = depthNormal*0.5+0.5; // 变换到0-1区间
            real4 finalColor = real4(depthNormal, 1);

            return finalColor;
        }
        ENDHLSL

        pass
        {
            Tags {"LightMode" = "UniversalForward"}

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            ENDHLSL
        }
    }
}