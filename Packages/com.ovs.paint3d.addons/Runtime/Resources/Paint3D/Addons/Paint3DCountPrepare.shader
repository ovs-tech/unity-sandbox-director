Shader "Hidden/Paint3D/CountPrepare"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always

            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment Frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _Paint3DMask;

            // rgb = paint color, a = UV island coverage (used to skip texels that are not on the mesh).
            float4 Frag(v2f_img i) : SV_Target
            {
                float3 color = tex2D(_MainTex, i.uv).rgb;
                float coverage = tex2D(_Paint3DMask, i.uv).r;
                return float4(color, coverage);
            }
            ENDCG
        }
    }
}
