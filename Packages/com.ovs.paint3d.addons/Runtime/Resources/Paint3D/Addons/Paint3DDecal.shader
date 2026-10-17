Shader "Hidden/Paint3D/Decal"
{
    Properties
    {
        _SrcBlend ("Src Blend", Float) = 5
        _DstBlend ("Dst Blend", Float) = 10
        _SrcBlendA ("Src Blend Alpha", Float) = 1
        _DstBlendA ("Dst Blend Alpha", Float) = 10
        _BlendOp ("Blend Op", Float) = 0
        _DecalTex ("Decal", 2D) = "white" {}
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

            #include "Packages/com.ovs.paint3d/Runtime/Resources/Paint3D/Paint3DCommon.cginc"

            float4x4 _DecalMatrix; // world -> decal unit cube
            sampler2D _DecalTex;
            float4 _BrushDirection;
            float4 _BrushParams; // x unused, y hardness (depth fade), z opacity, w normal threshold
            float4 _BrushColor;

            float4 Frag(Paint3DVaryings i) : SV_Target
            {
                float3 local = mul(_DecalMatrix, float4(i.worldPos, 1.0)).xyz;
                if (any(abs(local) > 0.5))
                {
                    discard;
                }

                float4 decal = tex2D(_DecalTex, local.xy + 0.5);
                float depthFade = saturate((0.5 - abs(local.z)) / max(0.5 * (1.0 - _BrushParams.y), 1e-4));
                float strength = decal.a * depthFade * _BrushParams.z * Paint3DFacing(i.worldNormal, _BrushDirection, _BrushParams.w);

                float4 color = float4(decal.rgb * _BrushColor.rgb, 1.0);
                return Paint3DOutput(Paint3DSourceColor(color, i.uv), strength);
            }
            ENDCG
        }
    }
}
