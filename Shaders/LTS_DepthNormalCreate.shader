Shader "LTS/PostProcessing/DepthNormalCreate"
{
    Properties
    {
        [HideInInspector] _MainTex ("", 2D) = "white"{}
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


        CBUFFER_START(UnityPerMaterial)

        CBUFFER_END

        // 空间变换补充
        real3 TransformObjectToViewNormal(real3 normalOS)
        {
            return SafeNormalize(mul((float3x3)UNITY_MATRIX_IT_MV, normalOS));
        }

        // 深度补充
        real Linear01Depth(real3 positionOS)
        {
            real3 positionWS = TransformObjectToWorld(positionOS);
            real3 positionVS = TransformWorldToView(positionWS);
            return - (positionVS.z * _ProjectionParams.w);
        }

        // Encoding/decoding view space normals into 2D 0..1 vector
        float2 EncodeViewNormalStereo(float3 n)
        {
            float kScale = 1.7777;
            float2 enc;
            enc = n.xy / (n.z + 1);
            enc /= kScale;
            enc = enc * 0.5 + 0.5;
            return enc;
        }
        
        // Encoding/decoding [0..1) floats into 8 bit/channel RG. Note that 1.0 will not be encoded properly.
        float2 EncodeFloatRG(float v)
        {
            float2 kEncodeMul = float2(1.0, 255.0);
            float kEncodeBit = 1.0 / 255.0;
            float2 enc = kEncodeMul * v;
            enc = frac(enc);
            enc.x -= enc.y * kEncodeBit;
            return enc;
        }
        
        float4 EncodeDepthNormal(float depth, float3 normal)
        {
            float4 enc;
            enc.xy = EncodeViewNormalStereo(normal);
            enc.zw = EncodeFloatRG(depth);
            return enc;
        }

        struct a2v
        {
            real4 positionOS  : POSITION;
            real3 normalOS    : NORMAL; 
        };

        struct v2f
        {
            real4 positionCS  : SV_POSITION;
            real4 normalDepth : TEXCOORD0;
        };

        v2f vert(a2v v)
        {
            v2f o = (v2f)0;
            o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
            o.normalDepth.xyz = TransformObjectToViewNormal(v.normalOS);
            o.normalDepth.w = Linear01Depth(v.positionOS.xyz);
            return o;
        }

        real4 frag(v2f i):SV_TARGET
        {
            return EncodeDepthNormal(i.normalDepth.w, i.normalDepth.xyz);
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
        pass
        {
            Tags {"LightMode" = "DepthOnly"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            ENDHLSL
        }
    }
}