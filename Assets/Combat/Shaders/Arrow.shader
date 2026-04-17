Shader "Custom/Arrow2"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWeight ("Outline Thickness", float) = 0.1
        _PlaneSize ("Size of Plane", Vector) = (10, 10, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Off

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

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _OutlineColor;
                float _OutlineWeight;
                float2 _PlaneSize;
                float2 _Verts[7];
            CBUFFER_END

            int all(float3 v){
                return (v.x == 1 && v.y == 1 && v.z == 1);
            }

            float3 not(float3 v){
                return 1.0 - v;
            }

            // The signed-distance function for an arbitrary polygon with seven vertices
            float SDPolygon(float2 v[7], float2 p)
            {
                float d = dot(p-v[0],p-v[0]);
                float s = 1.0;
                for(int i=0, j=7-1; i<7; j=i, i++)
                {
                    float2 e = v[j] - v[i];
                    float2 w =    p - v[i];
                    float2 b = w - e*clamp(dot(w,e)/dot(e,e), 0.0, 1.0);
                    d = min(d, dot(b,b));
                    float3 c = float3(p.y>=v[i].y,p.y<v[j].y,e.x*w.y>e.y*w.x);
                    if( all(c) || all(not(c)) ) s*=-1.0;  
                }
                return s*sqrt(d);
            }

            float2 objecttouv(float2 obj){
                return obj / _PlaneSize;
            }

            float2 uvtoobject(float2 uv){
                return uv * _PlaneSize;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = uvtoobject(IN.uv - 0.5);
                float distance = SDPolygon(_Verts, uv);

                half4 color = half4(0.0, 0.0, 0.0, 1.0);
                if (distance < 0.0){
                    color = _BaseColor;
                }
                else if (distance < _OutlineWeight){
                    color = _OutlineColor;
                }
                else{
                    discard;
                }

                return color;
            }
            ENDHLSL
        }
    }
}
