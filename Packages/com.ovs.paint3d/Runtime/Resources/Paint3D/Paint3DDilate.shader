Shader "Hidden/Paint3D/Dilate"
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
            float4 _MainTex_TexelSize;
            sampler2D _Paint3DMask;

            static const float2 PAINT3D_OFFSETS[8] =
            {
                float2(-1, 0), float2(1, 0), float2(0, -1), float2(0, 1),
                float2(-1, -1), float2(1, -1), float2(-1, 1), float2(1, 1)
            };

            // Texels outside UV islands copy the nearest island texel (up to 2 texels away) so bilinear filtering
            // and mip mapping never pull unpainted texels across seams.
            float4 Frag(v2f_img i) : SV_Target
            {
                float4 color = tex2D(_MainTex, i.uv);
                if (tex2Dlod(_Paint3DMask, float4(i.uv, 0, 0)).r > 0.5)
                {
                    return color;
                }

                [unroll]
                for (int ring = 1; ring <= 2; ring++)
                {
                    [unroll]
                    for (int k = 0; k < 8; k++)
                    {
                        float2 uv = i.uv + PAINT3D_OFFSETS[k] * _MainTex_TexelSize.xy * ring;
                        if (tex2Dlod(_Paint3DMask, float4(uv, 0, 0)).r > 0.5)
                        {
                            return tex2Dlod(_MainTex, float4(uv, 0, 0));
                        }
                    }
                }
                return color;
            }
            ENDCG
        }
    }
}
