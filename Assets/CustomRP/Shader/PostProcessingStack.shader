Shader "Custom RP/PostProcessingStack"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "black" {}
        _BloomThreshold ("Bloom Threshold", Range(0, 10)) = 1
        _BloomStrength ("Bloom Strength", Range(0, 10)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        ZWrite Off
        ZTest Always
        

        HLSLINCLUDE

        #include "../ShaderLibrary/Common.hlsl"

        struct Attributes {
            float4 posL : POSITION;
            float2 uv : TEXCOORD;
        };

        struct Varyings {
            float4 posH : SV_POSITION;
            float2 uv : TEXCOORD;
        };

        
        Varyings vert(Attributes input) {
            Varyings output;
            output.posH = TransformObjectToHClip(input.posL);
            output.uv = input.uv;
            
            return output;
        }


        #define BOX_SAMPLING(tex, sampler, uv, texelSz, offset, outCol) \
        real3 pixelLB = SAMPLE_TEXTURE2D_LOD(tex, sampler, uv + texelSz * real2(-offset, -offset), 0).rgb; \
        real3 pixelLT = SAMPLE_TEXTURE2D_LOD(tex, sampler, uv + texelSz * real2(-offset, offset), 0).rgb; \
        real3 pixelRT = SAMPLE_TEXTURE2D_LOD(tex, sampler, uv + texelSz * real2(offset, offset), 0).rgb; \
        real3 pixelRB = SAMPLE_TEXTURE2D_LOD(tex, sampler, uv + texelSz * real2(offset, -offset), 0).rgb; \
        outCol = real4((pixelLB + pixelLT + pixelRT + pixelRB) * 0.25, 1);


        ENDHLSL

        Pass
        {
            Name "BloomThreshold"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_Maintex);

            real _BloomThreshold;

            half4 ApplyThreshold(real4 col, real threshold) {
                float brightness = max(max(col.r, col.g), col.b);
                float contribute = saturate(brightness - threshold) / max(brightness, 0.00001);
                return half4(col.rgb * contribute, 1);
            }

            real4 frag(Varyings input) : SV_Target {
                real4 col = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_Maintex, input.uv, 0);
                return ApplyThreshold(col, _BloomThreshold);
            }

            ENDHLSL
        }


        Pass 
        {
            Name "BloomDownSampling"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
           
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_Maintex);
            
            real2 _MainTex_TexelSize;

            real4 frag(Varyings input) : SV_Target {
               real4 Col;
               BOX_SAMPLING(_MainTex, sampler_Maintex, input.uv, _MainTex_TexelSize, 0.5, Col);
               return Col;
            }

            ENDHLSL
        }


        Pass 
        {
            Name "BloomUpSampling"
            Blend One One

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
           
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_Maintex);
            
            real2 _MainTex_TexelSize;

            real4 frag(Varyings input) : SV_Target {
                real4 col;
                BOX_SAMPLING(_MainTex, sampler_Maintex, input.uv, _MainTex_TexelSize, -0.5, col);
                return col;
            }

            ENDHLSL
        }

        Pass 
        {
            Name "BloomAdd"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_Maintex);

            TEXTURE2D(_BloomTex);
            
            real2 _MainTex_TexelSize;
            real _BloomStrength;

            real4 frag(Varyings input) : SV_Target {
                real3 srcCol = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_Maintex, input.uv, 0).rgb;
                real4 bloomCol;
                BOX_SAMPLING(_BloomTex, sampler_Maintex, input.uv, _MainTex_TexelSize, -0.5, bloomCol);
                bloomCol.rgb = lerp(real3(0, 0, 0), bloomCol.rgb, _BloomStrength);
                return real4(srcCol + bloomCol.rgb, 1);
            }
            
            ENDHLSL
        }


        Pass
        {
            Name "ToneMappingReinhard"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            real4 frag(Varyings input) : SV_Target {
                real3 col = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, input.uv, 0).rgb;
                col = col / (1 + col);
                return real4(col, 1);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ToneMappingNeutral"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            real4 frag(Varyings input) : SV_Target {
                real3 col = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, input.uv, 0).rgb;
                col = NeutralTonemap(col);
                return real4(col, 1);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ToneMappingACES"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ACES.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            real4 frag(Varyings input) : SV_Target {
                real3 col = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, input.uv, 0).rgb;
                col = AcesTonemap(unity_to_ACES(col));
                return real4(col, 1);
            }

            ENDHLSL
        }


    }
}
