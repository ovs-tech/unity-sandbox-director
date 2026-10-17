Shader "Hidden/Paint3D/Sphere"
{
    Properties
    {
        _SrcBlend ("Src Blend", Float) = 5
        _DstBlend ("Dst Blend", Float) = 10
        _SrcBlendA ("Src Blend Alpha", Float) = 1
        _DstBlendA ("Dst Blend Alpha", Float) = 10
        _BlendOp ("Blend Op", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendA] [_DstBlendA]
            BlendOp [_BlendOp]

            CGPROGRAM
            #pragma vertex Paint3DVert
            #pragma fragment Frag
            #pragma target 3.0

            #include "Paint3DCommon.cginc"

            float4 _BrushPosition;
            float4 _BrushDirection;
            float4 _BrushParams; // x radius, y hardness, z opacity, w normal threshold
            float4 _BrushColor;

            float4 Frag(Paint3DVaryings i) : SV_Target
            {
                float distance01 = distance(i.worldPos, _BrushPosition.xyz) / max(_BrushParams.x, 1e-6);
                if (distance01 >= 1.0)
                {
                    discard;
                }

                float falloff = saturate((1.0 - distance01) / max(1.0 - _BrushParams.y, 1e-4));
                float strength = falloff * _BrushParams.z * Paint3DFacing(i.worldNormal, _BrushDirection, _BrushParams.w);
                return Paint3DOutput(Paint3DSourceColor(_BrushColor, i.uv), strength);
            }
            ENDCG
        }
    }
}
