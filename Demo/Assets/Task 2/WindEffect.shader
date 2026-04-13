Shader "Custom/WindEffect"
{
    Properties
    {
        _BaseColor      ("Base Color",              Color)   = (1,1,1,1)

        [Header(Wind)]
        [HDR]
        _WindColor      ("Wind Color (HDR)",        Color)   = (1.2, 0.82, 0.05, 1)
        _WindSpeed      ("Flow Speed",              Range(0.1, 6))   = 2.0
        _WindScale      ("Flow Scale",              Range(1, 12))    = 5.0
        _WindIntensity  ("Wind Intensity",          Range(0, 6))     = 2.5
        _BandSharpness  ("Band Sharpness",          Range(1, 20))    = 8.0
        _BandCount      ("Band Count",              Range(1, 8))     = 4.0
        _WarpStrength   ("Warp Strength",           Range(0, 3))     = 1.4
        _BreakStrength  ("Break / Cut Strength",    Range(0, 2))     = 0.9

        [Header(Rim)]
        [HDR]
        _RimColor       ("Rim Color (HDR)",         Color)   = (1.2, 0.80, 0.0, 1)
        _RimPower       ("Rim Power",               Range(0.5, 8))   = 2.5
        _RimIntensity   ("Rim Intensity",           Range(0, 6))     = 2.0

        [Header(Surface)]
        _Metallic       ("Metallic",                Range(0,1))      = 0.85
        _Smoothness     ("Smoothness",              Range(0,1))      = 0.75
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Opaque"
            "Queue"          = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;

                float4 _WindColor;
                float  _WindSpeed;
                float  _WindScale;
                float  _WindIntensity;
                float  _BandSharpness;
                float  _BandCount;
                float  _WarpStrength;
                float  _BreakStrength;

                float4 _RimColor;
                float  _RimPower;
                float  _RimIntensity;

                float  _Metallic;
                float  _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // --------------------------------------------------------
            // Sinüs tabanlı fraktal noise — texture yok, tamamen matematik
            // Her katman farklı frekans + yön → doğal görünüm
            // --------------------------------------------------------
            float sNoise3(float3 p)
            {
                float v = 0.0;
                v += sin(p.x * 1.00 + p.y * 0.43 + p.z * 0.77) * 0.500;
                v += sin(p.x * 2.37 + p.z * 1.71 + p.y * 1.23) * 0.250;
                v += sin(p.y * 4.83 + p.x * 3.19 + p.z * 2.11) * 0.125;
                v += sin(p.z * 9.61 + p.y * 6.73 + p.x * 4.37) * 0.063;
                return v * 0.533 + 0.5; // 0..1
            }

            float sNoise2(float2 p)
            {
                float v = 0.0;
                v += sin(p.x * 1.00 + p.y * 0.43) * 0.500;
                v += sin(p.x * 2.37 + p.y * 1.71) * 0.250;
                v += sin(p.x * 4.83 + p.y * 3.19) * 0.125;
                v += sin(p.x * 9.61 + p.y * 6.73) * 0.063;
                return v * 0.533 + 0.5;
            }

            // --------------------------------------------------------
            // Domain Warp: koordinatı büküp sonra noise al
            // → kıvrılan, dalgalanan akış şeritleri
            // --------------------------------------------------------
            float domainWarp(float3 p, float t)
            {
                float3 q;
                q.x = sNoise3(p + float3(0.00, 0.00, t * _WindSpeed));
                q.y = sNoise3(p + float3(5.20, 1.30, t * _WindSpeed * 0.8));
                q.z = sNoise3(p + float3(1.70, 9.20, t * _WindSpeed * 1.1));
                q = (q - 0.5) * 2.0 * _WarpStrength;

                return sNoise3(p + q + float3(t * _WindSpeed * 0.5, 0.0, 0.0));
            }

            // --------------------------------------------------------
            // Rüzgar şeridi
            // --------------------------------------------------------
            float windBand(float3 posWS, float t)
            {
                float3 p = posWS * _WindScale;

                // Domain warp ile kıvrılmış akış koordinatı
                float warp = domainWarp(p, t);

                // Akış yönü: X+Z karışımı + warp sapması + zaman ilerlemesi
                float flowCoord = posWS.x * 0.7
                                + posWS.z * 0.4
                                + warp * 1.5
                                - t * _WindSpeed * 0.4;

                // Keskin bantlar
                float banded = frac(flowCoord * _BandCount);
                float band   = pow(
                    smoothstep(0.0, 0.35, banded) * smoothstep(1.0, 0.65, banded),
                    _BandSharpness * 0.3
                );

                // Kırılma maskesi — şeritleri parçalara böler
                float breakNoise = sNoise3(p * 0.5 + float3(
                    t * _WindSpeed * 0.3,
                    t * _WindSpeed * 0.2,
                    0.0));
                float breakMask = smoothstep(
                    0.35 * _BreakStrength,
                    0.55 * _BreakStrength,
                    breakNoise);

                // İnce titreme (şeritlerin içindeki canlılık)
                float shimmer = sNoise2(float2(
                    posWS.x * _WindScale * 2.0 + t * _WindSpeed * 1.8,
                    posWS.y * _WindScale * 1.5 + t * _WindSpeed * 0.9));
                shimmer = smoothstep(0.45, 0.55, shimmer);

                return band * breakMask + shimmer * 0.3 * breakMask;
            }

            // --------------------------------------------------------
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrm = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS   = nrm.normalWS;
                OUT.viewDirWS  = GetWorldSpaceViewDir(pos.positionWS);
                OUT.fogFactor  = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float  t     = _Time.y;
                float3 N     = normalize(IN.normalWS);
                float3 V     = normalize(IN.viewDirWS);
                float  NdotV = saturate(dot(N, V));

                // Rim / Fresnel
                float  fresnel = pow(1.0 - NdotV, _RimPower);
                float3 rim     = _RimColor.rgb * fresnel * _RimIntensity;

                // Wind
                float  wVal    = windBand(IN.positionWS, t);
                float  viewFade = 0.2 + 0.8 * NdotV;
                float  rimBoost = 1.0 + fresnel * 1.5;
                float3 wind    = _WindColor.rgb * wVal * _WindIntensity * viewFade * rimBoost;

                // PBR
                InputData lightData = (InputData)0;
                lightData.positionWS              = IN.positionWS;
                lightData.normalWS                = N;
                lightData.viewDirectionWS         = V;
                lightData.shadowCoord             = TransformWorldToShadowCoord(IN.positionWS);
                lightData.fogCoord                = IN.fogFactor;
                lightData.bakedGI                 = SampleSH(N);
                lightData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo      = _BaseColor.rgb;
                surface.metallic    = _Metallic;
                surface.smoothness  = _Smoothness;
                surface.alpha       = 1.0;
                surface.emission    = wind + rim;

                half4 color = UniversalFragmentPBR(lightData, surface);
                color.rgb   = MixFog(color.rgb, IN.fogFactor);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
