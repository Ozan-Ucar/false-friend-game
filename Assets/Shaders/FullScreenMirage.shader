Shader "Custom/FullScreenMirage"
{
    Properties
    {
        _Distortion ("Verzerrung", Range(0.0, 0.05)) = 0.001
        _Speed ("Geschwindigkeit", Float) = 2.0
        _Frequency ("Wellen Anzahl", Float) = 30.0
    }

    SubShader
    {
        // Keine Sprite-Hacks mehr, das ist natives URP Fullscreen!
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        
        Pass
        {
            Name "MiragePass"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // Native Unity 6 URP Blitter Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // URP 6 reicht uns das fertige Kamerabild automatisch über _BlitTexture rein!
            // (Wird bereits automatisch durch Blit.hlsl bereitgestellt, daher keine Deklaration nötig)

            float _Distortion;
            float _Speed;
            float _Frequency;
            float _GlobalMirageActive; // Wird vom C# Skript gesteuert (0 = aus, 1 = an)

            float4 Frag(Varyings input) : SV_Target
            {
                // input.texcoord ist unsere exakte Position auf dem Bildschirm (0 bis 1)
                float2 uv = input.texcoord;

                // Absolute sichere, mathematische Fata Morgana
                float time = _Time.y * _Speed;
                
                // Zwei Sinuswellen übereinandergelegt, damit es schön chaotisch wie heiße Luft aussieht
                float wave1 = sin(uv.y * _Frequency + time);
                float wave2 = sin(uv.y * _Frequency * 2.5 - time * 1.5);

                // Das Bild sanft auf der X-Achse verzerren (mal _GlobalMirageActive, damit wir es abschalten können!)
                uv.x += (wave1 + wave2) * 0.5 * _Distortion * _GlobalMirageActive;

                // Hole die Farbe von der verschobenen Position aus dem Originalbild
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            }
            ENDHLSL
        }
    }
}
