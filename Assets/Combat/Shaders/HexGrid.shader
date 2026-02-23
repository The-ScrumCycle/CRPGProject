Shader "Custom/HexGrid"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _HexColor("Hex Color", Color) = (0, 0, 0, 1)
        _PlayerMoveColor("Player Move Color", Color) = (0, 0.8, 0.2, 1)
        _PlayerAttackColor("Player Attack Color", Color) = (0.9, 0.1, 0.1, 1)
        _AIMoveColor("AI Move Color", Color) = (1, 1, 0, 1)
        _AIAttackBaseColor("AI Attack Base", Color) = (1, 0.4, 0, 1)
        _AIAttackBrightColor("AI Attack Bright", Color) = (1, 0.7, 0.2, 1)
        _HoverColor("Hover Color", Color) = (0, 0.8, 0.8, 1)
        _HoverBrightness("Hover Brightness", float) = 1.3
        _PulseSpeed("Pulse Speed", float) = 4.0
        _BaseMap("Base Map", 2D) = "white" {}
        _HexTex ("Hex Texture", 2D) = "white" {}
        _HexScale("Hex Scale", float) = 1.0
        _GridScale("Grid Scale", Vector) = (1, 1, 0, 0)
        _GridDim("Grid Dimensions", Vector) = (1, 1, 0, 0)
        _LineWeight("Line Thickness", float) = 1.0
        _ClipEdges("Clip Edges", int) = 1
        _ActiveHex("Active Hex", Vector) = (0, 0, 0, 0)
        _HighlightCount("Highlight Count", int) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_HexTex);
            SAMPLER(sampler_HexTex);

            #define MAX_HIGHLIGHTS 256

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _HexColor;
                half4 _PlayerMoveColor;
                half4 _PlayerAttackColor;
                half4 _AIMoveColor;
                half4 _AIAttackBaseColor;
                half4 _AIAttackBrightColor;
                half4 _HoverColor;
                float _HoverBrightness;
                float _PulseSpeed;
                float4 _BaseMap_ST;
                float _HexScale;
                float2 _GridScale;
                float _LineWeight;
                float2 _GridDim;
                int _ClipEdges;
                float2 _ActiveHex;
                int _HighlightCount;
            CBUFFER_END

            float4 _Highlights[MAX_HIGHLIGHTS];

            float hex(in float2 p)
            {
                const float2 s = float2(1, 1.7320508);
                p = abs(p);
                return max(dot(p, s * 0.5), p.x);
            }

            float4 getHex(float2 p)
            {
                const float2 s = float2(1, 1.7320508);
                float4 hC = floor(float4(p, p - float2(0.5, 1.0)) / s.xyxy) + 0.5;
                float4 h = float4(p - hC.xy * s, p - (hC.zw + 0.5) * s);
                return dot(h.xy, h.xy) < dot(h.zw, h.zw)
                    ? float4(h.xy, hC.xy)
                    : float4(h.zw, hC.zw + 0.5);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                const float2 s = float2(1, 1.7320508);
                const float EPSILON = 0.01;

                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                float2 u = float2(IN.uv.x * _GridScale.x, IN.uv.y * _GridScale.y) * _HexScale;
                u.y += 0.1;
                float4 h = getHex(u);

                // Clip partial hexagons outside the grid
                if (_ClipEdges == 1 && (h.z >= _GridDim.x || h.z <= 0.0 || h.w >= ceil(_GridDim.y / s.y) || h.w <= 0.0))
                {
                    discard;
                }

                // Outline pass — early return, no highlighting on outlines
                float hex_dist = hex(h.xy);
                if (hex_dist > (1.0 - _LineWeight) * 0.5)
                {
                    color = SAMPLE_TEXTURE2D(_HexTex, sampler_HexTex, IN.uv) * _HexColor;
                    return color;
                }

                // --- Highlight resolution: find highest priority match ---
                int bestType = 0;

                [loop]
                for (int i = 0; i < _HighlightCount; i++)
                {
                    float2 hlCoords = _Highlights[i].xy + 1.0;
                    float2 hlCenter = float2(
                        hlCoords.x - (fmod(hlCoords.y, 2.0) == 0 ? 0.0 : 0.5),
                        hlCoords.y * 0.5
                    );

                    if (distance(hlCenter, h.zw) < EPSILON)
                    {
                        int hlType = (int)_Highlights[i].z;
                        if (hlType > bestType)
                        {
                            bestType = hlType;
                        }
                    }
                }

                // --- Map bestType to fill color ---
                half4 fillColor = _BaseColor;

                if (bestType == 1)
                {
                    fillColor = _PlayerMoveColor;
                }
                else if (bestType == 2)
                {
                    fillColor = _PlayerAttackColor;
                }
                else if (bestType == 3)
                {
                    fillColor = _AIMoveColor;
                }
                else if (bestType == 4)
                {
                    float pulse = 0.5 + 0.5 * sin(_Time.y * _PulseSpeed);
                    fillColor.rgb = lerp(_AIAttackBaseColor.rgb, _AIAttackBrightColor.rgb, pulse);
                    fillColor.a = 1.0;
                }

                // --- Hover modifier ---
                float2 activeHex = _ActiveHex + 1.0;
                float2 activeCenter = float2(
                    activeHex.x - (fmod(activeHex.y, 2.0) == 0 ? 0.0 : 0.5),
                    activeHex.y * 0.5
                );
                bool isHovered = distance(activeCenter, h.zw) < EPSILON;

                if (isHovered)
                {
                    if (bestType == 0)
                    {
                        fillColor = _HoverColor;
                    }
                    else
                    {
                        fillColor.rgb = min(fillColor.rgb * _HoverBrightness, float3(1.0, 1.0, 1.0));
                    }
                }

                color = fillColor;
                return color;
            }
            ENDHLSL
        }
    }
}