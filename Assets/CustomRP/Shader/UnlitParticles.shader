Shader "Custom RP/UnlitParticles"
{
    Properties
    {   
        _MainTex ("Main Map(RGBA)", 2D) = "white" {}
        _Color ("Color", Color) = (1, 1, 1, 1)
        _CutOff ("Alpha Cut Off", Range(0, 1)) = 0.1
        _EmissiveTex ("Emission Map(RGB)", 2D) = "white" {}
        [HDR] _Emission ("Emission", Color) = (0, 0, 0, 1)
        _NearFadeDistance ("Near Fade Distance", Range(0, 10)) = 1
        _NearFadeRange ("Near Fade Range", Range(0, 10)) = 1

        [HideInInspector] _SrcBlend ("Src Blend", Float) = 1
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 0
        [HideInInspector] _ZWrite ("Z Write", Float) = 1
    }

    SubShader
    {
        LOD 100
        ZWrite [_ZWrite]
        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma shader_feature _ RENDER_MODE_CUTOFF RENDER_MODE_FADE
            #pragma shader_feature _ VERTEX_COLOR
            #pragma shader_feature _ BLENDING_SHEET_ANIMATION
            #pragma shader_feature _ PARTICLE_NEAR_FADE

            #define SURFACE_INPUT_UNLIT

            #include "../ShaderLibrary/Common.hlsl"
            #include "../ShaderLibrary/SurfaceInput.hlsl"


            struct Attributes
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 posL : POSITION;
                float4 color : COLOR;
                float4 uv : TEXCOORD0;
                float animBlend : TEXCOORD1;
            };

            struct Varyings
            {
                UNITY_VERTEX_INPUT_INSTANCE_ID
                float4 posH : SV_POSITION;
                float2 uv : TEXCOORD0;

                #if defined(BLENDING_SHEET_ANIMATION)
                float3 animBlendUVW : TEXCOORD1;
                #endif 

                #if defined(VERTEX_COLOR)
                float4 color : VAR_COLOR;
                #endif
            };


            Varyings vert (Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                
                float3 posW = TransformObjectToWorld(input.posL);
                output.posH = TransformWorldToHClip(posW);
                output.uv = TransformBaseUV(input.uv.xy);
                
                #if defined(BLENDING_SHEET_ANIMATION)
                output.animBlendUVW = float3(TransformBaseUV(input.uv.zw), input.animBlend);
                #endif
                
                #if defined(VERTEX_COLOR)
                output.color = input.color;
                #endif
                
                return output;
            }

            real4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                real4 Col = GetBaseColor(input.uv);

                #if defined(BLENDING_SHEET_ANIMATION) 
                real4 blendCol = GetBaseColor(input.animBlendUVW.xy);
                Col = lerp(Col, blendCol, input.animBlendUVW.z);
                #endif
                
                #if defined(VERTEX_COLOR)
                Col *= input.color;
                #endif
                
                #if defined(RENDER_MODE_CUTOFF)
                clip(Col.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _CutOff));
                #endif

                Col.rgb += GetEmission(input.uv);

                #if defined(PARTICLE_NEAR_FADE)
                real fadeDistance = GetParticleNearFadeDistance();
                real fadeRange = GetParticleNearFadeRange();
                real viewDepth = ScreenPositionToViewDepth(input.posH);
                real fadeFactor = smoothstep(fadeDistance + _ProjectionParams.y, fadeDistance + fadeRange + _ProjectionParams.y, viewDepth);
                Col.a *= fadeFactor;
                #endif
                
                return Col;
            }

            ENDHLSL
        }
    }

    CustomEditor "UnlitParticlesShaderEditor"
}
