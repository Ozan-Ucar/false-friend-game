Shader "Custom/SpriteDashedOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Seam Settings)]
        [HDR] _OutlineColor ("Thread Color (HDR)", Color) = (0.9, 0.2, 0.2, 1.0)
        _SeamOffset ("Seam Offset Inward (Pixels)", Range(1.0, 30.0)) = 6.0
        _StitchThickness ("Stitch Width", Range(0.5, 5.0)) = 1.5
        _SlantAmount ("Stitch Slant Angle", Float) = 12.0
        
        [Header(Animation Settings)]
        _DashCount ("Stitch Density / Count", Float) = 40.0
        _Speed ("Seam Speed", Float) = 1.0
        _DashSpaceRatio ("Stitch Length Ratio", Range(0.1, 0.9)) = 0.5
        
        [Header(Thread 3D Shading and Specular)]
        _ThreadGlossiness ("Thread 3D Roundness", Range(1, 20)) = 6.0
        _ThreadHighlightColor ("Thread Highlight Specular", Color) = (1, 1, 1, 0.8)
        
        [Header(Drop Shadow Settings)]
        _ShadowOffsetVal ("Shadow Offset (Pixels)", Range(0.5, 10.0)) = 2.5
        _ShadowIntensity ("Shadow Opacity", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
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
            #pragma target 3.0
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
            float4 _OutlineColor;
            
            float _SeamOffset;
            float _StitchThickness;
            float _SlantAmount;
            
            float _DashCount;
            float _Speed;
            float _DashSpaceRatio;
            
            float _ThreadGlossiness;
            float4 _ThreadHighlightColor;
            
            float _ShadowOffsetVal;
            float _ShadowIntensity;

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
                
                float2 texelSize = _MainTex_TexelSize.xy;
                
                // Wir tasten zwei Radien ab: R1 (innerhalb des Pfades) und R2 (außerhalb des Pfades)
                float r1_dist = _SeamOffset - _StitchThickness;
                float r2_dist = _SeamOffset + _StitchThickness;
                
                // 4-Point Sampling für R1 (Schnittgrenze innen)
                float minAlphaR1 = c.a;
                float2 offsetR1 = texelSize * r1_dist;
                minAlphaR1 = min(minAlphaR1, tex2D(_MainTex, input.uv + float2(offsetR1.x, 0)).a);
                minAlphaR1 = min(minAlphaR1, tex2D(_MainTex, input.uv + float2(-offsetR1.x, 0)).a);
                minAlphaR1 = min(minAlphaR1, tex2D(_MainTex, input.uv + float2(0, offsetR1.y)).a);
                minAlphaR1 = min(minAlphaR1, tex2D(_MainTex, input.uv + float2(0, -offsetR1.y)).a);
                
                // 4-Point Sampling für R2 (Schnittgrenze außen)
                float minAlphaR2 = c.a;
                float2 offsetR2 = texelSize * r2_dist;
                minAlphaR2 = min(minAlphaR2, tex2D(_MainTex, input.uv + float2(offsetR2.x, 0)).a);
                minAlphaR2 = min(minAlphaR2, tex2D(_MainTex, input.uv + float2(-offsetR2.x, 0)).a);
                minAlphaR2 = min(minAlphaR2, tex2D(_MainTex, input.uv + float2(0, offsetR2.y)).a);
                minAlphaR2 = min(minAlphaR2, tex2D(_MainTex, input.uv + float2(0, -offsetR2.y)).a);

                // Naht-Linie: Pixel ist solide, R1 ist solide (> 0.8), aber R2 nähert sich der Kante (< 0.8)
                bool isOnSeamLine = (c.a > 0.8f && minAlphaR1 > 0.8f && minAlphaR2 < 0.8f);

                // --- PROCEDURAL 3D DROP SHADOW LOGIK ---
                // Wir berechnen einen Schattenwurf, der leicht diagonal nach unten verschoben ist
                float2 shadowOffset = float2(texelSize.x, -texelSize.y) * _ShadowOffsetVal;
                float2 shadowUV = input.uv - shadowOffset;
                
                float shadowMinAlphaR1 = tex2D(_MainTex, shadowUV).a;
                float shadowMinAlphaR2 = shadowMinAlphaR1;
                
                shadowMinAlphaR1 = min(shadowMinAlphaR1, tex2D(_MainTex, shadowUV + float2(offsetR1.x, 0)).a);
                shadowMinAlphaR1 = min(shadowMinAlphaR1, tex2D(_MainTex, shadowUV + float2(-offsetR1.x, 0)).a);
                shadowMinAlphaR1 = min(shadowMinAlphaR1, tex2D(_MainTex, shadowUV + float2(0, offsetR1.y)).a);
                shadowMinAlphaR1 = min(shadowMinAlphaR1, tex2D(_MainTex, shadowUV + float2(0, -offsetR1.y)).a);
                
                shadowMinAlphaR2 = min(shadowMinAlphaR2, tex2D(_MainTex, shadowUV + float2(offsetR2.x, 0)).a);
                shadowMinAlphaR2 = min(shadowMinAlphaR2, tex2D(_MainTex, shadowUV + float2(-offsetR2.x, 0)).a);
                shadowMinAlphaR2 = min(shadowMinAlphaR2, tex2D(_MainTex, shadowUV + float2(0, offsetR2.y)).a);
                shadowMinAlphaR2 = min(shadowMinAlphaR2, tex2D(_MainTex, shadowUV + float2(0, -offsetR2.y)).a);

                bool isShadowOnSeamLine = (tex2D(_MainTex, shadowUV).a > 0.8f && shadowMinAlphaR1 > 0.8f && shadowMinAlphaR2 < 0.8f);

                bool isShadowPixel = false;
                if (isShadowOnSeamLine && !isOnSeamLine)
                {
                    // Berechne Schattenwurf der Stiche
                    float2 centeredUV = shadowUV - float2(0.5f, 0.5f);
                    float angle = atan2(centeredUV.y, centeredUV.x);
                    float angleNorm = (angle + 3.14159265f) * 0.15915494f;
                    float r = length(centeredUV);
                    
                    float dashInput = angleNorm * _DashCount + (r * _SlantAmount) - _Time.y * _Speed;
                    float dashPattern = frac(dashInput);
                    
                    if (dashPattern < _DashSpaceRatio)
                    {
                        isShadowPixel = true;
                    }
                }

                // --- STICH-RENDERING ---
                if (isOnSeamLine)
                {
                    float2 centeredUV = input.uv - float2(0.5f, 0.5f);
                    float angle = atan2(centeredUV.y, centeredUV.x);
                    float angleNorm = (angle + 3.14159265f) * 0.15915494f;
                    float r = length(centeredUV);
                    
                    // Schräge Stiche erzeugen (Einbeziehung des Radius 'r' erzeugt einen perfekten Fadenwinkel)
                    float dashInput = angleNorm * _DashCount + (r * _SlantAmount) - _Time.y * _Speed;
                    float dashPattern = frac(dashInput);
                    
                    if (dashPattern < _DashSpaceRatio)
                    {
                        // Lokale Stitch-Koordinate normiert auf [0, 1]
                        float localT = dashPattern / _DashSpaceRatio;
                        
                        // Sinus-Wölbung für 3D-Volumen (Wölbung des Fadens nach oben)
                        float bulge = sin(localT * 3.14159265f);
                        
                        // 3D-Shading des Fadens (Highlight in der Mitte, dunkles Einstechen an den Enden)
                        half4 stitchColor = _OutlineColor;
                        
                        // Grundschattierung des Fadens (Rundung)
                        stitchColor.rgb *= (bulge * 0.6f + 0.4f);
                        
                        // Specular Highlight (Glanzlicht auf dem runden Faden)
                        float spec = pow(max(0.0f, bulge), _ThreadGlossiness);
                        stitchColor.rgb += _ThreadHighlightColor.rgb * spec * _ThreadHighlightColor.a;

                        return stitchColor;
                    }
                }

                // Wenn kein Faden, zeichne den Schattenwurf auf den Stoff
                if (isShadowPixel)
                {
                    // Dunkelt die Originaltextur ab (weicher Schlagschatten)
                    return lerp(c, half4(0, 0, 0, 1), _ShadowIntensity * tex2D(_MainTex, shadowUV).a);
                }

                return c;
            }
            ENDHLSL
        }
    }
}
