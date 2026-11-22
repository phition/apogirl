// 탐험 보이는 맛 2단계 ⑦ 도트에 맞춘 빛 (URP 2D Sprite-Lit-Default를 바탕으로).
// - 화소 맞춤: 빛을 그림의 한 화소(텍셀) 가운데에서 한 번만 읽어, 빛 경계가 도트 격자를 따라 계단지게.
// - 빛 계단: 빛 세기를 몇 단계로 끊어 레트로 음영처럼 (지각 밝기 기준, 세기는 섞는 비율).
// - 전역 값 (ExplorationLook이 exploration_look.json PixelLight로 정한다, F9로 끔):
//   _ApoPixelLightOn 0/1, _ApoPixelSnap 0/1, _ApoPixelSnapMaxTexel, _ApoLightSteps 단계 수, _ApoLightStepStrength 0~1
// - 3단계 노멀 (NormalsRendering 패스, 아래): 그림에서 바로 만든 화소 단위 노멀, 전역 _ApoNormalStrength·_ApoNormalLumaWeight
Shader "ApoGirl/ExplorationPixelLit"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex LitVertex
            #pragma fragment PixelLitFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color        : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightVariables.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            // 전역 값 (머티리얼 값이 아님)
            float _ApoPixelLightOn;
            float _ApoPixelSnap;
            float _ApoPixelSnapMaxTexel;   // 화면 높이 대비 한 화소 크기가 이보다 크면 맞추지 않는다 (작은 그림을 크게 늘린 단색 사각형)
            float _ApoLightSteps;
            float _ApoLightStepStrength;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            Varyings LitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.uv = input.uv;
                o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            // 빛 읽을 자리: 이 화소(텍셀)의 가운데. 스프라이트 안에서 uv와 화면 위치는 1차 관계라 미분으로 정확히 옮긴다.
            float2 SnappedLightingUV(float2 uv, float2 lightingUV)
            {
                float2 dx = ddx(uv);
                float2 dy = ddy(uv);
                // 텍스처 크기는 직접 읽는다 (_MainTex_TexelSize를 머티리얼 밖에 두면 스프라이트 배칭이 깨져 남의 그림을 그린다)
                uint width, height;
                _MainTex.GetDimensions(width, height);
                float2 size = float2(width, height);
                float det = dx.x * dy.y - dx.y * dy.x;
                if (_ApoPixelLightOn < 0.5 || _ApoPixelSnap < 0.5 || size.x < 2.0 || abs(det) < 1e-12)
                {
                    return lightingUV;
                }

                // 한 화소가 화면에서 차지하는 크기 (픽셀)
                float texelPixels = 1.0 / max(length(dx * size), length(dy * size));
                if (texelPixels > _ApoPixelSnapMaxTexel * _ScreenParams.y)
                {
                    return lightingUV;
                }

                float2 center = (floor(uv * size) + 0.5) / size;
                float2 duv = center - uv;
                // uv 차이 → 화면 픽셀 차이 (2x2 역행렬)
                float2 dpix = float2(dy.y * duv.x - dy.x * duv.y, -dx.y * duv.x + dx.x * duv.y) / det;
                return lightingUV + dpix / _ScreenParams.xy;
            }

            // 빛 계단: 지각 밝기(감마)로 끊고, 색 비율은 그대로.
            half3 StepLight(half3 light)
            {
                if (_ApoPixelLightOn < 0.5 || _ApoLightSteps < 1.0 || _ApoLightStepStrength <= 0.0)
                {
                    return light;
                }

                half peak = max(max(light.r, light.g), light.b);
                if (peak <= 1e-4)
                {
                    return light;
                }

                half perceived = pow(peak, 1.0 / 2.2);
                half stepped = round(perceived * _ApoLightSteps) / _ApoLightSteps;
                half3 quantized = light * (pow(stepped, 2.2) / peak);
                return lerp(light, quantized, _ApoLightStepStrength);
            }

            half4 SampleShapeLights(float2 lightingUV, half4 mask, out half4 additive)
            {
                half4 modulate = 0;
                additive = 0;
            #if USE_SHAPE_LIGHT_TYPE_0
                half4 l0 = SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, lightingUV);
                if (any(_ShapeLightMaskFilter0))
                {
                    half4 m0 = (1 - _ShapeLightInvertedFilter0) * mask + _ShapeLightInvertedFilter0 * (1 - mask);
                    l0 *= dot(m0, _ShapeLightMaskFilter0);
                }
                modulate += l0 * _ShapeLightBlendFactors0.x;
                additive += l0 * _ShapeLightBlendFactors0.y;
            #endif
            #if USE_SHAPE_LIGHT_TYPE_1
                half4 l1 = SAMPLE_TEXTURE2D(_ShapeLightTexture1, sampler_ShapeLightTexture1, lightingUV);
                if (any(_ShapeLightMaskFilter1))
                {
                    half4 m1 = (1 - _ShapeLightInvertedFilter1) * mask + _ShapeLightInvertedFilter1 * (1 - mask);
                    l1 *= dot(m1, _ShapeLightMaskFilter1);
                }
                modulate += l1 * _ShapeLightBlendFactors1.x;
                additive += l1 * _ShapeLightBlendFactors1.y;
            #endif
            #if USE_SHAPE_LIGHT_TYPE_2
                half4 l2 = SAMPLE_TEXTURE2D(_ShapeLightTexture2, sampler_ShapeLightTexture2, lightingUV);
                if (any(_ShapeLightMaskFilter2))
                {
                    half4 m2 = (1 - _ShapeLightInvertedFilter2) * mask + _ShapeLightInvertedFilter2 * (1 - mask);
                    l2 *= dot(m2, _ShapeLightMaskFilter2);
                }
                modulate += l2 * _ShapeLightBlendFactors2.x;
                additive += l2 * _ShapeLightBlendFactors2.y;
            #endif
            #if USE_SHAPE_LIGHT_TYPE_3
                half4 l3 = SAMPLE_TEXTURE2D(_ShapeLightTexture3, sampler_ShapeLightTexture3, lightingUV);
                if (any(_ShapeLightMaskFilter3))
                {
                    half4 m3 = (1 - _ShapeLightInvertedFilter3) * mask + _ShapeLightInvertedFilter3 * (1 - mask);
                    l3 *= dot(m3, _ShapeLightMaskFilter3);
                }
                modulate += l3 * _ShapeLightBlendFactors3.x;
                additive += l3 * _ShapeLightBlendFactors3.y;
            #endif
                return modulate;
            }

            half4 PixelLitFragment(Varyings input) : SV_Target
            {
                // 미분은 discard 전에 (쿼드 단위)
                float2 lightingUV = SnappedLightingUV(input.uv, input.lightingUV);
                half4 main = input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                if (main.a == 0.0)
                {
                    discard;
                }

            #if !USE_SHAPE_LIGHT_TYPE_0 && !USE_SHAPE_LIGHT_TYPE_1 && !USE_SHAPE_LIGHT_TYPE_2 && !USE_SHAPE_LIGHT_TYPE_3
                return main;
            #else
                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                half4 additive;
                half4 modulate = SampleShapeLights(lightingUV, mask, additive);
                modulate.rgb = StepLight(modulate.rgb);
                additive.rgb = StepLight(additive.rgb);
                half4 result = _HDREmulationScale * (main * modulate + additive);
                result.a = main.a;
                return max(0, result);
            #endif
            }
            ENDHLSL
        }

        // 3단계 노멀: 노멀맵 파일 없이 그림에서 바로 만든다 (높이 = 알파 × 밝기, 화소 가운데끼리 차이 → 기울기).
        // 화소마다 한 값이라 빛이 도트 단위로 굴곡을 탄다. 투명한 가장자리는 높이 0이라 테두리가 둥글게 빛을 받는다.
        // 전역: _ApoNormalStrength (0이면 평평), _ApoNormalLumaWeight (밝기를 높이에 얼마나 섞나, 0이면 외곽선만)
        Pass
        {
            Tags { "LightMode" = "NormalsRendering"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex NormalsVertex
            #pragma fragment NormalsFragment

            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                float4 color        : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float _ApoNormalStrength;
            float _ApoNormalLumaWeight;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            Varyings NormalsVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.uv = input.uv;
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half Height(float2 uv)
            {
                half4 c = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0);
                half luma = dot(c.rgb, half3(0.299, 0.587, 0.114));
                return c.a * lerp(1.0, luma, _ApoNormalLumaWeight);
            }

            half4 NormalsFragment(Varyings input) : SV_Target
            {
                SetUpSpriteInstanceProperties();
                half4 main = input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                uint width, height;
                _MainTex.GetDimensions(width, height);
                float2 size = float2(max(width, 1u), max(height, 1u));
                float2 texel = 1.0 / size;
                float2 center = (floor(input.uv * size) + 0.5) * texel;

                half3 normalTS = half3(0, 0, 1);
                if (_ApoNormalStrength > 0.0 && width > 2u)
                {
                    half dx = Height(center + float2(texel.x, 0)) - Height(center - float2(texel.x, 0));
                    half dy = Height(center + float2(0, texel.y)) - Height(center - float2(0, texel.y));
                    normalTS = normalize(half3(-dx * _ApoNormalStrength, -dy * _ApoNormalStrength, 1.0));
                }

                // 스프라이트는 화면을 향한 평면: 접선 (1,0,0), 종접선 (0,1,0), 법선 (0,0,-1) (기본 스프라이트 메시와 같다)
                normalTS.xy *= unity_SpriteProps.xy;   // 좌우 뒤집기
                half3 normalWS = half3(normalTS.x, normalTS.y, -normalTS.z);
                return half4(0.5 * (normalWS + 1.0), main.a);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Lit-Default"
}
