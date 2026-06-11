Shader "Custom/GodotRays"
{
    Properties
    {
        _MainTex ("Sprite Texture (Unused but needed for Sprite)", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1.0, 0.9, 0.65, 0.8)
        _Angle ("Angle", Float) = -0.3
        _Position ("Position", Float) = -0.2
        _Spread ("Spread", Range(0.0, 1.0)) = 0.5
        _Cutoff ("Cutoff", Range(-1.0, 1.0)) = 0.1
        _Falloff ("Falloff", Range(0.0, 1.0)) = 0.2
        _EdgeFade ("Edge Fade", Range(0.0, 1.0)) = 0.15
        
        _Speed ("Speed", Float) = 1.0
        _Ray1Density ("Ray 1 Density", Float) = 8.0
        _Ray2Density ("Ray 2 Density", Float) = 30.0
        _Ray2Intensity ("Ray 2 Intensity", Range(0.0, 1.0)) = 0.3
        
        _Seed ("Seed", Float) = 5.0

        [Header(Transparency Fluctuation)]
        _PulseSpeed ("Pulse Speed (0 = Off)", Float) = 0.0
        _MinAlpha ("Min Transparency", Range(0.0, 1.0)) = 0.3
        _MaxAlpha ("Max Transparency", Range(0.0, 1.0)) = 1.0
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
        }

        Cull Off
        Lighting Off
        ZWrite Off
        
        // Additive-Blending für einen schönen, leuchtenden Lichteffekt
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            fixed4 _Color;
            float _Angle;
            float _Position;
            float _Spread;
            float _Cutoff;
            float _Falloff;
            float _EdgeFade;
            float _Speed;
            float _Ray1Density;
            float _Ray2Density;
            float _Ray2Intensity;
            float _Seed;
            float _PulseSpeed;
            float _MinAlpha;
            float _MaxAlpha;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            float random(float2 uv)
            {
                return frac(sin(dot(uv.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);

                float a = random(i);
                float b = random(i + float2(1.0, 0.0));
                float c = random(i + float2(0.0, 1.0));
                float d = random(i + float2(1.0, 1.0));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(a, b, u.x) +
                        (c - a) * u.y * (1.0 - u.x) +
                        (d - b) * u.x * u.y;
            }

            float2x2 rotate(float angle)
            {
                float c = cos(angle);
                float s = sin(angle);
                return float2x2(c, -s, s, c);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                
                // UVs transformieren (Rotieren & Skew)
                float2 transformed_uv = mul(rotate(_Angle), (uv - _Position)) / ((uv.y + _Spread) - (uv.y * _Spread));
                
                // Ray Animation
                float2 ray1 = float2(transformed_uv.x * _Ray1Density + sin(_Time.y * 0.1 * _Speed) * (_Ray1Density * 0.2) + _Seed, 1.0);
                float2 ray2 = float2(transformed_uv.x * _Ray2Density + sin(_Time.y * 0.2 * _Speed) * (_Ray1Density * 0.2) + _Seed, 1.0);
                
                // Cut off
                float cut = step(_Cutoff, transformed_uv.x) * step(_Cutoff, 1.0 - transformed_uv.x);
                ray1 *= cut;
                ray2 *= cut;
                
                // Rauschen (die Strahlen selbst)
                float rays = clamp(noise(ray1) + (noise(ray2) * _Ray2Intensity), 0.0, 1.0);
                
                // Fading an den Rändern
                rays *= smoothstep(0.0, _Falloff, (1.0 - uv.y)); // Bottom
                rays *= smoothstep(0.0 + _Cutoff, _EdgeFade + _Cutoff, transformed_uv.x); // Left
                rays *= smoothstep(0.0 + _Cutoff, _EdgeFade + _Cutoff, 1.0 - transformed_uv.x); // Right
                
                // Transparenz-Schwankung (Pulsieren)
                float pulse = (sin(_Time.y * _PulseSpeed) + 1.0) * 0.5; // Ergibt einen Wert zwischen 0 und 1
                float currentAlpha = lerp(_MinAlpha, _MaxAlpha, pulse);

                // Farbe anwenden
                float3 shine = float3(rays, rays, rays) * IN.color.rgb;
                
                return fixed4(shine, rays * IN.color.a * currentAlpha);
            }
            ENDCG
        }
    }
}
