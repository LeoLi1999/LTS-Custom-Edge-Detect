Shader "LTS/PostProcessing/EdgeDetect"
{
    Properties
    {
        [HideInInspector] _MainTex ("", 2D) = "white"{}
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        // 后处理传参
        uniform real4 _EdgeColor;
        uniform real4 _BackgroundColor;
        uniform real _EdgeOnly;

        // 公共寄存器
        SAMPLER(sampler_LinearClamp);
        SAMPLER(sampler_LinearRepeat);

        TEXTURE2D(_MainTex);
        TEXTURE2D(_CameraDepthNormalTexture);
        TEXTURE2D(_CameraDepthNormalDecodeTexture);

        CBUFFER_START(UnityPerMaterial)
        uniform real4 _MainTex_TexelSize;
        CBUFFER_END

        real Luminance1 (real4 color)  // 加个1是因为unity内置了同名函数
        {
            return 0.2125 * color.r + 0.7154 * color.g + 0.0721 * color.b;
            // 也可以这么写
            // return dot(color.rgb, real3(0.2125, 0.7154, 0.0721));
        }

        real Sobel (Texture2D tex, real2 uv[9])
        {
            const real Gx[9] = {-1, -2, -1,
                                 0,  0,  0,
                                 1,  2,  1};
            const real Gy[9] = {-1,  0,  1,
                                -2,  0,  2,
                                -1,  0,  1};
            real color;
            real edgeX, edgeY = 0;
            for (int i = 0; i < 9; i++)
            {
                color = Luminance1(SAMPLE_TEXTURE2D(tex, sampler_LinearClamp, uv[i]));
                edgeX += color * Gx[i];
                edgeY += color * Gy[i];
            }
            real edge = 1 - abs(edgeX) - abs(edgeY);
            return edge;
        }

        real Prewitt (Texture2D tex, real2 uv[9])
        {
            const real Gx[9] = {-1, -1, -1,
                                 0,  0,  0,
                                 1,  1,  1};
            const real Gy[9] = {-1,  0,  1,
                                -1,  0,  1,
                                -1,  0,  1};
            real color;
            real edgeX, edgeY = 0;
            for (int i = 0; i < 9; i++)
            {
                color = Luminance1(SAMPLE_TEXTURE2D(tex, sampler_LinearClamp, uv[i]));
                edgeX += color * Gx[i];
                edgeY += color * Gy[i];
            }
            real edge = 1 - abs(edgeX) - abs(edgeY);
            return edge;
        }

        struct VertexInput
        {
            real3 positionOS : POSITION;
            real2 uv         : TEXCOORD0;
        };

        struct VertexOutput
        {
            real4 positionCS : SV_POSITION;
            real2 uv[9]      : TEXCOORD0;
        };

        VertexOutput vert(VertexInput i)
        {
            VertexOutput o = (VertexOutput)0;
            o.positionCS = TransformObjectToHClip(i.positionOS);
            real2 uv = i.uv;
            o.uv[0] = uv + _MainTex_TexelSize.xy * real2(-1, -1);
            o.uv[1] = uv + _MainTex_TexelSize.xy * real2( 0, -1);
            o.uv[2] = uv + _MainTex_TexelSize.xy * real2( 1, -1);
            o.uv[3] = uv + _MainTex_TexelSize.xy * real2(-1,  0);
            o.uv[4] = uv + _MainTex_TexelSize.xy * real2( 0,  0);
            o.uv[5] = uv + _MainTex_TexelSize.xy * real2( 1,  0);
            o.uv[6] = uv + _MainTex_TexelSize.xy * real2(-1,  1);
            o.uv[7] = uv + _MainTex_TexelSize.xy * real2( 0,  1);
            o.uv[8] = uv + _MainTex_TexelSize.xy * real2( 1,  1);
            return o;
        }

        real4 frag(VertexOutput i):SV_TARGET
        {
            real4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_LinearClamp, i.uv[4]);
            real renderMask = step(0.0001, SAMPLE_TEXTURE2D(_CameraDepthNormalTexture, sampler_LinearClamp, i.uv[4]).r);
            real4 finalColor = mainColor;
            #if _UseEdgeDetect
                real edgeDetect = Sobel(_MainTex, i.uv);
                #if _UseDepthNormal
                    edgeDetect = Sobel(_CameraDepthNormalTexture, i.uv);
                    #if _UseDecodeDepthNormal
                        edgeDetect = Sobel(_CameraDepthNormalDecodeTexture, i.uv);
                    #endif
                #endif
            real4 withEdgeColor = lerp(_EdgeColor, mainColor, edgeDetect);
            // 由于使用屏幕图片的贴图使用的是bilt，所以无法使用layermask去做剔除，所以这里添加了一步，利用深度法线图去生成一个遮罩
            withEdgeColor = lerp(mainColor, withEdgeColor, renderMask);
            real4 onlyEdgeColor = lerp(_EdgeColor, _BackgroundColor, edgeDetect);
            onlyEdgeColor = lerp(_BackgroundColor, onlyEdgeColor, renderMask);
            finalColor = lerp(withEdgeColor, onlyEdgeColor, _EdgeOnly);
            #endif

            return finalColor;
        }
        ENDHLSL

        pass
        {
            Name "Edge Detect"
            Tags {"LightMode"="EdgeDetect"}
            HLSLPROGRAM

            // 后处理传Keyeord
            #pragma shader_feature_local_fragment _UseEdgeDetect
            #pragma shader_feature_local_fragment _UseDepthNormal
            #pragma shader_feature_local_fragment _UseDecodeDepthNormal

            #pragma vertex vert
            #pragma fragment frag

            ENDHLSL
        }
    }
}