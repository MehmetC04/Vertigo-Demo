Shader "VertigoGames/WeaponVFX/WeaponWindMeshShell"
{
    Properties
    {
        [Header(Color and Glow)]
        [HDR] _CoreColor    ("Core Color",       Color)  = (1.5, 1.2, 0.2, 1.0)
        _EmissionBoost      ("Emission Boost",   Float)  = 3.0
        
        [Header(Mesh Extrusion)]
        _Extrusion          ("Mesh Extrusion", Range(0.0, 0.5)) = 0.05

        [Header(Sine Wave Settings)]
        _WaveSpeed          ("Flow Speed", Float) = -5.0
        _WaveFrequencyZ     ("Frequency (Z Length)", Float) = 12.0
        // Dikiş izi (seam) olmaması için Twist değeri kesinlikle tam sayı (Integer) olmalıdır!
        [IntRange] _WaveTwist ("Twist (Spiral Count)", Range(1, 10)) = 2 
        _LineThickness      ("Line Thickness", Range(0.0, 1.0)) = 0.85
        _FadeEdge           ("Edge Softness", Range(0.001, 0.5)) = 0.05

        [Header(Streak and Ribbon Masking)]
        _NoiseTex           ("Ribbon Mask (Noise)", 2D) = "white" {}
        _NoiseScale         ("Mask Scale", Float) = 2.0
        _NoiseSpeedZ        ("Mask Speed Z", Float) = -2.0
        _NoiseSpeedY        ("Mask Speed Y", Float) = 1.0
        _StreakDensity      ("Streak Density", Range(0.0, 1.0)) = 0.5

        [Header(Object Bounds Fading)]
        _FadeStartMin       ("Tail Fade Start", Float) = -0.2
        _FadeEndMin         ("Tail Fade End", Float) = -0.5
        _FadeStartMax       ("Head Fade Start", Float) = 0.5
        _FadeEndMax         ("Head Fade End", Float) = 0.8
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "WindShell"
            Blend SrcAlpha One      // Additive
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _CoreColor;
                float  _EmissionBoost;
                
                float  _Extrusion;
                
                float  _WaveSpeed;
                float  _WaveFrequencyZ;
                float  _WaveTwist;
                float  _LineThickness;
                float  _FadeEdge;

                float4 _NoiseTex_ST;
                float  _NoiseScale;
                float  _NoiseSpeedZ;
                float  _NoiseSpeedY;
                float  _StreakDensity;

                float  _FadeStartMin;
                float  _FadeEndMin;
                float  _FadeStartMax;
                float  _FadeEndMax;
            CBUFFER_END

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS  : TEXCOORD0; 
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Mesh Extrusion: Yüzeyi şişiriyoruz
                float3 expandedPos = IN.positionOS.xyz + (IN.normalOS * _Extrusion);

                OUT.positionHCS = TransformObjectToHClip(expandedPos);
                OUT.positionOS = IN.positionOS.xyz; 
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y;

                // 1. Z-AXIS FADING (Uçlarda Kaybolma)
                float tailFade = smoothstep(_FadeEndMin, _FadeStartMin, IN.positionOS.z);
                float headFade = smoothstep(_FadeEndMax, _FadeStartMax, IN.positionOS.z);
                float zMask = tailFade * headFade;

                clip(zMask - 0.01);

                // 2. YENİ SİNE WAVE (Gerçek Sarmal / Helix)
                // atan2 kullanarak objenin etrafındaki 360 derecelik açıyı (radyan) hesaplıyoruz.
                // Bu açı bizi topografik düzlemlerden kurtarıp silahın etrafında döndürüyor.
                float angle = atan2(IN.positionOS.y, IN.positionOS.x);
                
                // Z ekseni uzunluğu + etrafındaki açı + zaman = Kusursuz sarmal akış
                float waveCoord = (IN.positionOS.z * _WaveFrequencyZ) + (angle * _WaveTwist) + (time * _WaveSpeed);
                
                float wave = sin(waveCoord); 
                float lineMask = smoothstep(_LineThickness, _LineThickness + _FadeEdge, wave);

                // 3. NOISE MASKING (Sonsuz çizgileri kesip şeritlere ayırma)
                float2 noiseUV = float2(IN.positionOS.z * _NoiseScale + time * _NoiseSpeedZ, 
                                        IN.positionOS.y * _NoiseScale + time * _NoiseSpeedY);
                
                float noiseVal = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                float streakMask = smoothstep(1.0 - _StreakDensity, 1.0, noiseVal);

                // 4. FINAL ALPHA
                float finalAlpha = lineMask * streakMask * zMask;
                clip(finalAlpha - 0.01);

                // 5. RENK
                float3 col = _CoreColor.rgb * _EmissionBoost;

                return half4(col, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}