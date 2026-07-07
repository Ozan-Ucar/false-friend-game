Shader "Custom/SpriteHighlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _HighlightColor ("Highlight Color", Color) = (1,1,0,1)
        _OutlineWidth ("Outline Width", Range(0, 100)) = 2
        _PulseSpeed ("Pulse Speed", Float) = 2.0
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 1.0 // 1 = Volles Pulsieren, 0 = Statisch
        _OutlineMinAlpha ("Outline Min Alpha", Range(0, 1)) = 0.0
        _InnerGlowMaxOpacity ("Inner Glow Max Opacity", Range(0, 1)) = 0.35
        _InnerGlowSharpness ("Inner Glow Sharpness", Range(1, 30)) = 8.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Color;
            float4 _HighlightColor;
            float _OutlineWidth;
            float _PulseSpeed;
            float _PulseAmount;
            float _OutlineMinAlpha;
            float _InnerGlowMaxOpacity;
            float _InnerGlowSharpness;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 c = tex2D(_MainTex, input.uv) * input.color;
                
                float2 texelSize = _MainTex_TexelSize.xy * _OutlineWidth;
                float alpha = c.a;
                
                alpha = max(alpha, tex2D(_MainTex, input.uv + float2(texelSize.x, 0)).a);
                alpha = max(alpha, tex2D(_MainTex, input.uv + float2(-texelSize.x, 0)).a);
                alpha = max(alpha, tex2D(_MainTex, input.uv + float2(0, texelSize.y)).a);
                alpha = max(alpha, tex2D(_MainTex, input.uv + float2(0, -texelSize.y)).a);
                alpha = max(alpha, tex2D(_MainTex, input.uv + float2(texelSize.x, texelSize.y) * 0.707).a);
                alpha = max(alpha, tex2D(_MainTex, input.uv + float2(-texelSize.x, texelSize.y) * 0.707).a);
                alpha = max(alpha, tex2D(_MainTex, input.uv + float2(texelSize.x, -texelSize.y) * 0.707).a);
                alpha = max(alpha, tex2D(_MainTex, input.uv + float2(-texelSize.x, -texelSize.y) * 0.707).a);

                // Basis Sinus-Welle (0.0 bis 1.0)
                float rawSin = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;

                // Outline-Logik: pulsiert zwischen dem eingestellten Minimum (jetzt 0%) und 100%
                float outlineWave = lerp(_OutlineMinAlpha, 1.0, rawSin);
                float finalOutlinePulse = lerp(1.0, outlineWave, _PulseAmount);

                // Innere Füllung Logik: Bleibt fast immer 0 und "blinkt" nur ganz kurz auf
                // _InnerGlowSharpness: Höherer Wert = Kürzere Blinkzeit, längere Dunkelphase
                float innerWave = pow(rawSin, _InnerGlowSharpness);
                float finalInnerPulse = lerp(1.0, innerWave, _PulseAmount);

                if (c.a < 0.1 && alpha > 0.1)
                {
                    // Outline zeichnen
                    return _HighlightColor * finalOutlinePulse;
                }
                else if (c.a >= 0.1)
                {
                    // Inneres des Sprites einfärben
                    float overlayFade = saturate(_OutlineWidth);
                    
                    // _InnerGlowMaxOpacity bestimmt, wie deckend die Farbe im Peak ist
                    float overlayIntensity = _InnerGlowMaxOpacity * overlayFade * finalInnerPulse; 
                    
                    c.rgb = lerp(c.rgb, _HighlightColor.rgb, overlayIntensity);
                    return c;
                }

                return c;
            }
            ENDHLSL
        }
    }
}
