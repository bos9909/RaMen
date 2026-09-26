Shader "Custom/RamenSoup"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _BaseMap ("Base Map", 2D) = "white" {}

        [Header(Broth Color Depth)]
        _DeepColor ("국물 깊은 곳 색상 (짙은 갈색)", Color) = (0.35, 0.15, 0.05, 1.0)
        _ShallowColor ("국물 얕은 곳 색상 (호박색)", Color) = (0.55, 0.28, 0.1, 1.0)
        _RimEdgeColor ("가장자리 반사광", Color) = (0.7, 0.45, 0.2, 1.0)

        [Header(Ambient Swirl)]
        _SwirlSpeed ("국물 대류 속도", Range(0.1, 3.0)) = 0.8
        _SwirlScale ("국물 결 무늬 크기", Range(1.0, 10.0)) = 4.0

        [Header(Interactive Ripple)]
        _RippleCenter ("파문 발생 중심 (UV)", Vector) = (0.5, 0.5, 0, 0)
        _RippleTime ("파문 진행 시간", Float) = 100.0
        _RippleStrength ("파문 왜곡 강도", Range(0.0, 0.1)) = 0.025
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Texture2D _MainTex;
            SamplerState sampler_MainTex;

            float4 _DeepColor;
            float4 _ShallowColor;
            float4 _RimEdgeColor;
            float _SwirlSpeed;
            float _SwirlScale;
            float4 _RippleCenter;
            float _RippleTime;
            float _RippleStrength;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            // 가벼운 절차적 2D 노이즈 함수 (국물 대류용)
            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash(i), Hash(i + float2(1, 0)), f.x),
                            lerp(Hash(i + float2(0, 1)), Hash(i + float2(1, 1)), f.x), f.y);
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 centerOffset = uv - float2(0.5, 0.5);
                float distFromCenter = length(centerOffset);

                // 원형 마스크
                float circleMask = smoothstep(0.5, 0.49, distFromCenter);
                if (circleMask <= 0.001) discard;

                // 1. [상호작용 파문] 마우스/기름방울이 일으키는 동심원 물결
                float2 rippleOffset = uv - _RippleCenter.xy;
                float rippleDist = length(rippleOffset);
                float waveSpeed = 2.2;
                float waveFreq = 28.0;

                // 시간 경과에 따라 바깥으로 퍼져나가며 서서히 감쇄하는 사인 파동
                float wavePhase = rippleDist * waveFreq - (_RippleTime * waveSpeed);
                float waveDamping = exp(-rippleDist * 3.5) * exp(-_RippleTime * 1.8);
                float ripple = sin(wavePhase) * waveDamping * _RippleStrength;

                // 파문으로 인한 UV 굴절 왜곡
                float2 distortedUV = uv + (rippleOffset / (rippleDist + 0.001)) * ripple;

                // 2. [은은한 대류 소용돌이] 따뜻한 국물이 천천히 흐르는 무늬
                float time = _Time.y * _SwirlSpeed;
                float n1 = Noise(distortedUV * _SwirlScale + float2(time * 0.2, time * 0.1));
                float n2 = Noise(distortedUV * (_SwirlScale * 1.5) - float2(time * 0.15, time * 0.25));
                float swirl = (n1 + n2) * 0.5;

                // 3. [국물 색상 입체 그라데이션] (중심부는 깊고 진하게, 가장자리는 맑게)
                float3 soupColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, swirl * 0.7 + (distFromCenter * 0.6));

                // 가장자리 도자기 반사 테두리 림
                float rim = smoothstep(0.42, 0.49, distFromCenter);
                soupColor = lerp(soupColor, _RimEdgeColor.rgb, rim * 0.4);

                // 물결 파동의 마루(정점)에 은은한 빛 반사 하이라이트
                soupColor += float3(1.0, 0.9, 0.7) * saturate(ripple * 8.0);

                return float4(soupColor, circleMask) * input.color;
            }
            ENDHLSL
        }
    }
}