Shader "Hidden/Paint3D/UVMask"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always

            CGPROGRAM
            #pragma vertex Paint3DVert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Paint3DCommon.cginc"

            float4 Frag(Paint3DVaryings i) : SV_Target
            {
                return float4(1.0, 1.0, 1.0, 1.0);
            }
            ENDCG
        }
    }
}
