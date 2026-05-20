Shader "Custom/LightningRodCharge"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ChargeColor ("Charge Color", Color) = (0, 1, 1, 1)
        _ChargeAmount ("Charge Amount", Range(0,1)) = 0
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
                
                // Leuchten von unten nach oben, basierend auf Charge Amount (0 bis 1)
                // uv.y geht von 0 (unten) bis 1 (oben)
                if (input.uv.y <= _ChargeAmount)
                {
                    // Leichtes Pulsieren hinzufügen
                    float pulse = (sin(_Time.y * 15.0) * 0.2) + 0.8;
                    
                    // Wir mischen die Originalfarbe mit hellem Cyan
                    half3 chargedRGB = lerp(c.rgb, _ChargeColor.rgb, 0.75) * pulse;
                    
                    return half4(chargedRGB, c.a);
                }
                
                return c;
            }
            ENDHLSL
        }
    }
}
