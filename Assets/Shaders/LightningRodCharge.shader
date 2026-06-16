Shader "Custom/LightningRodCharge"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ChargeColor ("Charge Color", Color) = (0, 1, 1, 1)
        _ChargeAmount ("Charge Amount", Range(0,1)) = 0
        _BobAmount ("Hover Amount", Float) = 0.15
        _BobSpeed ("Hover Speed", Float) = 3.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
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
            float4 _Color;
            float4 _ChargeColor;
            float _ChargeAmount;
            float _BobAmount;
            float _BobSpeed;

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Shader-basiertes Schweben (nur visuell, beeinflusst Physik nicht!)
                float3 posOS = input.positionOS.xyz;
                posOS.y += sin(_Time.y * _BobSpeed) * _BobAmount;

                output.positionCS = TransformObjectToHClip(posOS);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 c = tex2D(_MainTex, input.uv) * input.color;
                
                // Abstand zur Mitte berechnen (UV 0.5, 0.5 ist die Mitte)
                float dist = distance(input.uv, float2(0.5, 0.5));
                
                // Ein ausgefüllter Kreis, der von der Mitte aus anwächst!
                // Bei _ChargeAmount=1 erreicht der Kreis exakt den Rand des Sprites (dist = 0.5)
                if (dist <= 0.5 * _ChargeAmount && c.a > 0.1)
                {
                    // Leichtes Pulsieren hinzufügen (Puls zwischen 1.0 und 1.4 für super Leuchten)
                    float pulse = (sin(_Time.y * 20.0) * 0.2) + 1.2;
                    
                    // Wir überschreiben die Farbe extrem stark mit hellem Cyan
                    half3 chargedRGB = lerp(c.rgb, _ChargeColor.rgb * pulse, 0.85);
                    
                    return half4(chargedRGB, c.a);
                }
                
                return c;
            }
            ENDHLSL
        }
    }
}
