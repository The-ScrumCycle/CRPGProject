Shader "Custom/HexGrid"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _HexColor("Hex Color", Color) = (0, 0, 0, 1)
        _BaseMap("Base Map", 2D) = "white"
        _HexTex ("Hex Texture", 2D) = "white"
        _HexScale("Hex Scale", float) = 1.0
        _GridScale("Grid Scale", Vector) = (1, 1, 0, 0)
        _GridDim("Grid Dimensions", Vector) = (1, 1, 0, 0)
        _LineWeight("Line Thickness", float) = 1.0
        _ClipEdges("Clip Edges", int) = 1
        _ActiveHex("Active Hex", Vector) = (0, 0, 0, 0)
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

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _HexColor;
                float4 _BaseMap_ST;
                float _HexScale;
                float2 _GridScale;
                float _LineWeight;
                float2 _GridDim;
                int _ClipEdges;
                float2 _ActiveHex;
            CBUFFER_END

            // Calculates distance from edge of hexagon given uv coords
            float hex(in float2 p){
                const float hexSize = .1;
                const float2 s = float2(1, 1.7320508);
                
                p = abs(p);
                return max(dot(p, s*0.5), p.x);
            }

            // Translates from uv to hex grid coordinates
            // local distance from the center of the hex is stored in the xy components
            // hex center position is stored in the zw components
            float4 getHex(float2 p)
            {   
                const float2 s = float2(1, 1.7320508);
                
                float4 hC = floor(float4(p, p - float2(0.5, 1.0))/s.xyxy) + 0.5;
                
                float4 h = float4(p - hC.xy*s, p - (hC.zw + 0.5)*s);
            
                return dot(h.xy, h.xy) < dot(h.zw, h.zw) 
                    ? float4(h.xy, hC.xy) 
                    : float4(h.zw, hC.zw + 0.5);
            }

            // 3-in 3-out hash function
            float3 hash( float3 p )
            {
                p = float3( dot(p,float3(127.1,311.7, 74.7)),
                        dot(p,float3(269.5,183.3,246.1)),
                        dot(p,float3(113.5,271.9,124.6)));

                return frac(sin(p)*43758.5453123);
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
                // 1.732058 = sqrt(3), length of hexagon side
                const float2 s = float2(1, 1.7320508);
                half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // Scaled uv coords
                float2 u = float2(IN.uv.x * _GridScale.x, IN.uv.y * _GridScale.y) * _HexScale;
                u.y += 0.1;

                float4 h = getHex(u);

                // Do not draw partial hexagons which are outside the grid
                if (_ClipEdges == 1 && (h.z >= _GridDim.x || h.z <= 0.0 || h.w >= ceil(_GridDim.y/s.y) || h.w <= 0.0)){
                    discard;
                }

                float hex_dist = hex(h.xy);

                if (hex_dist > (1.0 - _LineWeight)*0.5){
                    color = SAMPLE_TEXTURE2D(_HexTex, sampler_HexTex, IN.uv) * _HexColor;
                }

                color.rgb = float3(0.0, 0.0, 0.0);
                color.rb = h.zw;

                float2 active_hex = _ActiveHex+1.0;
                if (distance(float2(active_hex.x - (active_hex.y%2 == 0 ? 0.0 : 0.5), active_hex.y * 0.5), h.zw) < 0.01){
                    color.rgb = float3(0.0, 45.0, 45.0);
                }
                return color;
            }
            ENDHLSL
        }
    }
}
