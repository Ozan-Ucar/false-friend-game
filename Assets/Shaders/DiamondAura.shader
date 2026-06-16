Shader "Custom/2D/DiamondAura"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Aura)]
        [HDR] _AuraColor ("Aura Color", Color) = (0.2, 0.8, 1, 1)
        _AuraSoftness ("Aura Breite", Range(0.005, 0.15)) = 0.04
        _AuraOpacity ("Aura Transparenz", Range(0, 1)) = 0.35
        _AuraSpeed ("Puls Geschwindigkeit", Range(0, 5)) = 2.0
        
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
            #pragma vertex SpriteVert
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
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _AlphaTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _EnableExternalAlpha;
            
            float4 _AuraColor;
            float _AuraSoftness;
            float _AuraOpacity;
            float _AuraSpeed;

            v2f SpriteVert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif
                return OUT;
            }

            fixed4 SampleSprite(float2 uv)
            {
                fixed4 color = tex2D(_MainTex, uv);
                #if ETC1_EXTERNAL_ALPHA
                fixed4 alpha = tex2D(_AlphaTex, uv);
                color.a = lerp(color.a, alpha.r, _EnableExternalAlpha);
                #endif
                return color;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                
                // === SCHRITT 1: Original Sprite holen ===
                fixed4 sprite = SampleSprite(uv) * IN.color;
                
                // === SCHRITT 2: Aura berechnen (Blur vom Alpha-Kanal) ===
                float pulse = 0.85 + 0.15 * sin(_Time.y * _AuraSpeed);
                float s = _AuraSoftness * pulse;
                
                float blur = 0.0;
                blur += tex2D(_MainTex, uv + float2( s,  0)).a;
                blur += tex2D(_MainTex, uv + float2(-s,  0)).a;
                blur += tex2D(_MainTex, uv + float2( 0,  s)).a;
                blur += tex2D(_MainTex, uv + float2( 0, -s)).a;
                blur += tex2D(_MainTex, uv + float2( s,  s)).a * 0.7;
                blur += tex2D(_MainTex, uv + float2(-s,  s)).a * 0.7;
                blur += tex2D(_MainTex, uv + float2( s, -s)).a * 0.7;
                blur += tex2D(_MainTex, uv + float2(-s, -s)).a * 0.7;
                blur = saturate(blur / 6.8);
                
                // === SCHRITT 3: Aura NUR ausserhalb des Sprites ===
                // Wo der Diamant sichtbar ist (alpha > 0), zeigen wir NUR den Diamanten.
                // Wo der Diamant unsichtbar ist (alpha == 0), zeigen wir die Aura.
                
                float auraAlpha = blur * _AuraOpacity * _AuraColor.a * (1.0 - sprite.a);
                
                // === SCHRITT 4: Zusammenbauen ===
                // Premultiplied Alpha fuer Unity 2D
                float4 result;
                result.rgb = sprite.rgb * sprite.a + _AuraColor.rgb * auraAlpha;
                result.a = sprite.a + auraAlpha * (1.0 - sprite.a);
                
                return result;
            }
            ENDCG
        }
    }
}
