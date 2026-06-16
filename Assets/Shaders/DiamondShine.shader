Shader "Custom/2D/DiamondShine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Glow)]
        [HDR] _GlowColor ("Glow Color", Color) = (0, 0.8, 1, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 1.5
        
        [Header(Shine Effect)]
        [HDR] _ShineColor ("Shine Color", Color) = (1, 1, 1, 1)
        _ShineWidth ("Shine Width", Range(0.01, 0.5)) = 0.15
        _ShineSpeed ("Shine Speed", Range(0, 5)) = 0.8
        _ShineAngle ("Shine Angle", Range(0, 360)) = 45.0
        
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
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
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            
            float4 _GlowColor;
            float _GlowIntensity;
            
            float4 _ShineColor;
            float _ShineWidth;
            float _ShineSpeed;
            float _ShineAngle;

            sampler2D _AlphaTex;
            float _EnableExternalAlpha;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 SampleSpriteTexture (float2 uv)
            {
                fixed4 color = tex2D (_MainTex, uv);

#if ETC1_EXTERNAL_ALPHA
                fixed4 alpha = tex2D (_AlphaTex, uv);
                color.a = lerp (color.a, alpha.r, _EnableExternalAlpha);
#endif

                return color;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture (IN.texcoord) * IN.color;
                
                // Basis-Glow hinzufügen
                c.rgb += c.rgb * _GlowColor.rgb * _GlowIntensity;
                
                // Shine / Bling Effekt berechnen
                float rad = _ShineAngle * 3.14159265 / 180.0;
                float2 dir = float2(cos(rad), sin(rad));
                
                // UVs entlang der Winkel-Richtung projizieren
                float projectedUV = dot(IN.texcoord - 0.5, dir);
                
                // Animieren: Die Linie wandert von -0.5 bis 1.5
                float timeOffset = _Time.y * _ShineSpeed;
                float shinePos = frac(timeOffset) * 2.0 - 0.5; 
                
                // Distanz zur Shine-Linie
                float dist = abs(projectedUV - shinePos);
                
                // Weiche Kante für den Glanz
                float shineMask = 1.0 - smoothstep(0.0, _ShineWidth, dist);
                
                // Den Glanz nur auf die sichtbaren Pixel (Alpha) des Diamanten anwenden
                c.rgb += _ShineColor.rgb * shineMask * c.a * 2.0;
                
                c.rgb *= c.a; // Premultiply Alpha für sauberes Blending in Unity 2D
                return c;
            }
            ENDCG
        }
    }
}
