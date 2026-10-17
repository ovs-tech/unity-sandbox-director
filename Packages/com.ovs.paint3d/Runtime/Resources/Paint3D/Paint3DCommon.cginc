#ifndef PAINT3D_COMMON_INCLUDED
#define PAINT3D_COMMON_INCLUDED

// Shared texture-space painting helpers. Pipeline agnostic (Built-in, URP, HDRP) and GLES3 / WebGL2 safe.

#include "UnityCG.cginc"

float4x4 _Paint3DMatrix;        // object -> world
float4x4 _Paint3DNormalMatrix;  // inverse transpose of object -> world
float _Paint3DUVChannel;        // 0 = uv0, 1 = uv1
float _Paint3DMode;             // 0 Alpha, 1 Additive, 2 Subtract, 3 Multiply, 4 Erase
sampler2D _Paint3DOriginal;

struct Paint3DAttributes
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
};

struct Paint3DVaryings
{
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 worldPos : TEXCOORD1;
    float3 worldNormal : TEXCOORD2;
};

// Unwraps the mesh into texture space: each triangle is rasterized where its UVs are, carrying its world position.
Paint3DVaryings Paint3DVert(Paint3DAttributes v)
{
    Paint3DVaryings o;
    float2 uv = _Paint3DUVChannel > 0.5 ? v.uv1 : v.uv0;
    o.position = float4(uv * 2.0 - 1.0, 0.5, 1.0);
#if UNITY_UV_STARTS_AT_TOP
    o.position.y = -o.position.y;
#endif
    o.uv = uv;
    o.worldPos = mul(_Paint3DMatrix, float4(v.vertex.xyz, 1.0)).xyz;
    o.worldNormal = mul((float3x3)_Paint3DNormalMatrix, v.normal);
    return o;
}

// Facing test: 1 when the surface faces the brush, fading to 0 below the threshold.
// direction.w == 0 or threshold < -1 disables the test (through painting).
float Paint3DFacing(float3 worldNormal, float4 direction, float threshold)
{
    if (threshold < -1.5 || direction.w < 0.5)
    {
        return 1.0;
    }
    float facing = dot(normalize(worldNormal), -direction.xyz);
    return smoothstep(threshold, threshold + 0.15, facing);
}

// Brush color, or the original texture when erasing.
float4 Paint3DSourceColor(float4 brushColor, float2 uv)
{
    if (_Paint3DMode > 3.5)
    {
        return float4(tex2D(_Paint3DOriginal, uv).rgb, 1.0);
    }
    return brushColor;
}

// Final fragment value for the hardware blend configured by PaintEngine.
float4 Paint3DOutput(float4 color, float strength)
{
    strength = saturate(strength);
    if (_Paint3DMode > 2.5 && _Paint3DMode < 3.5)
    {
        return lerp(float4(1.0, 1.0, 1.0, 1.0), color, strength);
    }
    return float4(color.rgb, color.a * strength);
}

#endif
