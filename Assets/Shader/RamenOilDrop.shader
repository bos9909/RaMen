Shader "Custom/RamenOilDrop"
{
    Properties
    {
        // [SpriteRenderer 필수 슬롯] 유니티 스프라이트 렌더러 에러 방지용
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _BaseMap ("Base Map", 2D) = "white" {}

        [Header(Base Oil Settings)]
        _BaseColor ("기름 기본 색상 (반투명 호박색)", Color) = (0.96, 0.72, 0.18, 0.65)
        
        [Header(Rim Light Meniscus)]
        _RimColor ("가장자리 림라이트 색상", Color) = (1.0, 0.92, 0.55, 0.9)
        _RimWidth ("림라이트 두께", Range(0.01, 0.2)) = 0.08
        
        [Header(Lighting Glint)]
        _GlintColor ("조명 반사광 색상", Color) = (1.0, 1.0, 1.0, 0.85)
        _GlintPos ("반사광 위치 (XY)", Vector) = (0.35, 0.65, 0, 0)
        _GlintRadius ("반사광 크기", Range(0.02, 0.2)) = 0.08

        [Header(Liquid Surface Wobble)]
        _WobbleSpeed ("찰랑거림 속도", Range(0.0, 10.0)) = 3.5
        _WobbleAmount ("찰랑거림 강도", Range(0.0, 0.03)) = 0.008
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane" 
            "CanUseSpriteAtlas"="True"
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

            // 유니티 2D 스프라이트 텍스처 정의 (필수)
            Texture2D _MainTex;
            SamplerState sampler_MainTex;

            float4 _BaseColor;
            float4 _RimColor;
            float _RimWidth;
            float4 _GlintColor;
            float4 _GlintPos;
            float _GlintRadius;
            float _WobbleSpeed;
            float _WobbleAmount;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // 1. 미세한 액체 표면 찰랑거림 (원형 테두리가 잔물결을 침)
                float2 centerOffset = uv - float2(0.5, 0.5);
                float angle = atan2(centerOffset.y, centerOffset.x);
                float ripple = sin(angle * 5.0 + _Time.y * _WobbleSpeed) * _WobbleAmount;

                // 중심으로부터의 거리 계산
                float dist = length(centerOffset) - ripple;

                // 2. 완벽하게 부드러운 원형 외곽선 마스크 (Anti-aliasing)
                float circleMask = smoothstep(0.5, 0.485, dist);
                if (circleMask <= 0.001)
                    discard;

                // 3. 표면장력 림라이트 (볼록 솟은 테두리에 맺히는 밝은 굴절선)
                float rimStart = 0.5 - _RimWidth;
                float rim = smoothstep(rimStart, 0.48, dist) * (1.0 - smoothstep(0.485, 0.5, dist));

                // 4. 천장 조명 하이라이트 (좌상단에 맺히는 맑은 광택)
                float glintDist = length(uv - _GlintPos.xy);
                float glint = smoothstep(_GlintRadius, _GlintRadius * 0.3, glintDist);

                // 5. 최종 액체 색상 합성
                float3 finalRGB = _BaseColor.rgb;
                finalRGB = lerp(finalRGB, _RimColor.rgb, rim * _RimColor.a);
                finalRGB += _GlintColor.rgb * glint * _GlintColor.a;

                // 투명도 계산 (기본 투명도 + 림라이트/광택 부분은 선명하게)
                float finalAlpha = saturate((_BaseColor.a + rim * 0.3 + glint) * circleMask);

                return float4(finalRGB, finalAlpha) * input.color;
            }
            ENDHLSL
        }
    }
}