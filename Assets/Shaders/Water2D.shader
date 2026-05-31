Shader "Custom/Water2D"
{
    Properties
    {
        // --- FARBEN ---
        [Header(Farben)]
        _SurfaceColor ("Oberflaeche", Color) = (0.25, 0.62, 0.88, 0.75)
        _DeepColor    ("Tiefe", Color)       = (0.04, 0.12, 0.35, 0.92)
        _FoamColor    ("Schaum Highlight", Color) = (0.85, 0.95, 1.0, 0.9)

        // --- WELLEN ---
        [Header(Wellen)]
        _WaveSpeed     ("Wellen-Speed", Float)              = 1.2
        _WaveAmplitude ("Wellen-Hoehe", Range(0, 0.15))     = 0.035
        _WaveFrequency ("Wellen-Frequenz", Float)           = 4.0

        _Wave2Speed     ("Nebenwelle Speed", Float)          = 2.0
        _Wave2Amplitude ("Nebenwelle Hoehe", Range(0, 0.1))  = 0.015
        _Wave2Frequency ("Nebenwelle Frequenz", Float)       = 9.0

        _Wave3Speed     ("Mikrowelle Speed", Float)          = 3.5
        _Wave3Amplitude ("Mikrowelle Hoehe", Range(0, 0.05)) = 0.008
        _Wave3Frequency ("Mikrowelle Frequenz", Float)       = 17.0

        // --- SCHAUM ---
        [Header(Schaum)]
        _FoamWidth     ("Schaum-Breite", Range(0, 0.2))  = 0.045
        _FoamSoftness  ("Schaum-Weichheit", Range(0.001, 0.1)) = 0.02
        _FoamNoiseScale("Schaum-Noise", Float) = 12.0
        _FoamNoiseSpeed("Schaum-Noise Speed", Float) = 1.5

        // --- KAUSTIK ---
        [Header(Kaustik)]
        _CausticSpeed     ("Kaustik Speed", Float)           = 0.8
        _CausticScale     ("Kaustik Groesse", Float)         = 6.0
        _CausticIntensity ("Kaustik Intensitaet", Range(0, 0.6)) = 0.2
        _CausticSharpness ("Kaustik Schaerfe", Range(1, 8))  = 3.0

        // --- STROEMUNG ---
        [Header(Stroemung)]
        _FlowSpeed     ("Stroemung Speed", Float) = 0.5
        _FlowScale     ("Stroemung Groesse", Float) = 5.0
        _FlowIntensity ("Stroemung Intensitaet", Range(0, 0.3)) = 0.08

        // --- GLITZER ---
        [Header(Glitzer)]
        _ShimmerSpeed     ("Glitzer Speed", Float) = 3.0
        _ShimmerScale     ("Glitzer Groesse", Float) = 25.0
        _ShimmerIntensity ("Glitzer Intensitaet", Range(0, 0.5)) = 0.12
        _ShimmerThreshold ("Glitzer Schwelle", Range(0, 1)) = 0.85

        // --- KANTEN ---
        [Header(Kanten)]
        _EdgeSoftness ("Kanten-Weichheit", Range(0.001, 0.05)) = 0.008
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }
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

            // --- Structs ---
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                float3 worldPos   : TEXCOORD1;
            };

            // --- Uniforms ---
            float4 _SurfaceColor;
            float4 _DeepColor;
            float4 _FoamColor;

            float _WaveSpeed, _WaveAmplitude, _WaveFrequency;
            float _Wave2Speed, _Wave2Amplitude, _Wave2Frequency;
            float _Wave3Speed, _Wave3Amplitude, _Wave3Frequency;

            float _FoamWidth, _FoamSoftness, _FoamNoiseScale, _FoamNoiseSpeed;

            float _CausticSpeed, _CausticScale, _CausticIntensity, _CausticSharpness;

            float _FlowSpeed, _FlowScale, _FlowIntensity;

            float _ShimmerSpeed, _ShimmerScale, _ShimmerIntensity, _ShimmerThreshold;

            float _EdgeSoftness;


            // ====================================================
            //  HILFSFUNKTIONEN
            // ====================================================

            // Berechnet die Wellenhoehe an einer bestimmten X-Position
            float calcWave(float x, float time)
            {
                float w1 = sin(x * _WaveFrequency  + time * _WaveSpeed)  * _WaveAmplitude;
                float w2 = sin(x * _Wave2Frequency + time * _Wave2Speed + 1.37) * _Wave2Amplitude;
                float w3 = sin(x * _Wave3Frequency + time * _Wave3Speed + 3.91) * _Wave3Amplitude;
                return w1 + w2 + w3;
            }

            // Einfacher Hash fuer pseudo-zufaellige Werte (GPU-freundlich, kein Texture-Lookup noetig)
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // 2D Value-Noise (weicher als einfacher Hash, billiger als Perlin)
            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                // Kubische Interpolation fuer Weichheit
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = hash(i + float2(0.0, 0.0));
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Kaustik-Muster: Ueberlagerte Sinuswellen in verschiedenen Winkeln
            float causticPattern(float2 uv, float time)
            {
                float c1 = sin(uv.x * _CausticScale * 1.0 + uv.y * _CausticScale * 0.7 + time * _CausticSpeed);
                float c2 = sin(uv.x * _CausticScale * 0.8 - uv.y * _CausticScale * 1.1 + time * _CausticSpeed * 0.7 + 2.1);
                float c3 = sin(uv.x * _CausticScale * 1.3 + uv.y * _CausticScale * 0.4 - time * _CausticSpeed * 1.1 + 4.7);

                // Multiplizieren und normalisieren -> ergibt das typische Rautenmuster
                float pattern = (c1 * c2 + c2 * c3 + c3 * c1 + 3.0) / 6.0;
                // Schaerfe hochdrehen
                return pow(pattern, _CausticSharpness);
            }


            // ====================================================
            //  VERTEX SHADER
            // ====================================================

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Vertex in World-Space fuer stabile Wellen (unabhaengig von Sprite-Position)
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);

                // Nur die oberen Vertices verschieben (uv.y > 0.95 ~ obere Kante des Quads)
                if (input.uv.y > 0.95)
                {
                    float wave = calcWave(worldPos.x, _Time.y);
                    worldPos.y += wave;
                }

                output.positionCS = TransformWorldToHClip(worldPos);
                output.uv = input.uv;
                output.color = input.color;
                output.worldPos = worldPos;
                return output;
            }


            // ====================================================
            //  FRAGMENT SHADER - Hier entsteht die ganze Magie
            // ====================================================

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;

                // Die maximale Wellenhoehe, damit wir den UV-Raum korrekt skalieren koennen
                float maxAmp = _WaveAmplitude + _Wave2Amplitude + _Wave3Amplitude;

                // ------------------------------------
                // 1. WELLENOBERFLAECHE
                // ------------------------------------
                // Wir berechnen die Welle an der World-X-Position dieses Pixels
                float wave = calcWave(input.worldPos.x, time);

                // Wo ist die Wasserlinie in UV-Space?
                // surfaceLine = 1.0 wenn keine Welle, bewegt sich mit der Welle
                float surfaceLine = 1.0 - maxAmp + wave;

                // Alles ueber der Wasserlinie wird unsichtbar (mit weichem Rand)
                float surfaceMask = smoothstep(surfaceLine + _EdgeSoftness, surfaceLine - _EdgeSoftness, uv.y);

                // Wenn komplett ueber der Welle, frueh abbrechen
                if (surfaceMask < 0.001) 
                {
                    return half4(0, 0, 0, 0);
                }

                // ------------------------------------
                // 2. TIEFE (depth = 0 an der Oberflaeche, 1 am Boden)
                // ------------------------------------
                float depth = saturate(1.0 - (uv.y / max(surfaceLine, 0.001)));

                // ------------------------------------
                // 3. GRUNDFARBE (Verlauf Oberflaeche -> Tiefe)
                // ------------------------------------
                // Nicht-linearer Verlauf fuer realistischeren Look
                float colorDepth = pow(depth, 1.4);
                half4 baseColor = lerp(_SurfaceColor, _DeepColor, colorDepth);

                // ------------------------------------
                // 4. SCHAUM-LINIE an der Oberflaeche
                // ------------------------------------
                float distToSurface = surfaceLine - uv.y;

                // Etwas Noise auf den Schaum, damit er lebendig aussieht
                float foamNoise = valueNoise(float2(input.worldPos.x * _FoamNoiseScale, time * _FoamNoiseSpeed));
                float foamWidth = _FoamWidth * (0.6 + 0.4 * foamNoise); // Breite variiert

                float foam = smoothstep(foamWidth + _FoamSoftness, foamWidth * 0.2, distToSurface);
                foam *= smoothstep(0.0, _FoamSoftness * 2.0, distToSurface); // Nicht ganz am Rand

                baseColor.rgb = lerp(baseColor.rgb, _FoamColor.rgb, foam * _FoamColor.a);
                baseColor.a = max(baseColor.a, foam * _FoamColor.a * 0.5);

                // ------------------------------------
                // 5. KAUSTIK-LICHTMUSTER
                // ------------------------------------
                float caustics = causticPattern(
                    float2(input.worldPos.x, uv.y), 
                    time
                );
                // Kaustik wird in der Tiefe schwaecher und an der Oberflaeche am staerksten
                float causticMask = (1.0 - depth * 0.6) * _CausticIntensity;
                baseColor.rgb += caustics * causticMask;

                // ------------------------------------
                // 6. STROEMUNGS-LINIEN (innere Current-Bewegung)
                // ------------------------------------
                float2 flowUV = float2(input.worldPos.x * _FlowScale + time * _FlowSpeed, uv.y * _FlowScale * 0.5);
                float flow1 = sin(flowUV.x + flowUV.y * 2.0 + time * 0.3) * 0.5 + 0.5;
                float flow2 = sin(flowUV.x * 1.7 - flowUV.y * 1.3 + time * 0.5) * 0.5 + 0.5;
                float flowPattern = flow1 * flow2;
                flowPattern = smoothstep(0.3, 0.7, flowPattern);

                // Stroemung eher in der Mitte sichtbar, nicht ganz oben oder unten
                float flowMask = sin(depth * 3.14159) * _FlowIntensity;
                baseColor.rgb += flowPattern * flowMask * _SurfaceColor.rgb * 0.5;

                // ------------------------------------
                // 7. GLITZER / SHIMMER
                // ------------------------------------
                float2 shimmerUV = float2(
                    input.worldPos.x * _ShimmerScale,
                    uv.y * _ShimmerScale * 0.6
                );
                float shimmer1 = sin(shimmerUV.x + time * _ShimmerSpeed * 1.3);
                float shimmer2 = sin(shimmerUV.y * 1.4 - time * _ShimmerSpeed);
                float shimmer3 = sin((shimmerUV.x + shimmerUV.y) * 0.7 + time * _ShimmerSpeed * 0.6);
                
                float shimmer = shimmer1 * shimmer2 * shimmer3;
                // Nur die allerhellsten Spitzen behalten -> kleine, scharfe Funken
                shimmer = saturate((shimmer - _ShimmerThreshold) / (1.0 - _ShimmerThreshold));
                shimmer = shimmer * shimmer; // Extra schaerfen

                // Glitzer nur nahe der Oberflaeche und nicht ganz am Rand
                float shimmerMask = (1.0 - depth) * smoothstep(0.0, 0.1, distToSurface);
                baseColor.rgb += shimmer * _ShimmerIntensity * shimmerMask;

                // ------------------------------------
                // 8. FINALE ALPHA
                // ------------------------------------
                // Tiefe macht das Wasser undurchsichtiger
                baseColor.a = lerp(_SurfaceColor.a, _DeepColor.a, depth);
                // Die weiche Oberkante anwenden
                baseColor.a *= surfaceMask;
                // Vertex-Color-Alpha respektieren (fuer Fade-Outs im Inspector)
                baseColor.a *= input.color.a;

                return baseColor;
            }
            ENDHLSL
        }
    }
}
