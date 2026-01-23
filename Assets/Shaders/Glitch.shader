Shader "HSLU/Glitch"
{
    Properties
    {
        _BaseMap("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _Alpha("Alpha", Range(0,1)) = 1

        _Metallic("Metallic", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        _RoughnessMap("Roughness Map", 2D) = "white" {}

        _BumpMap("Normal Map", 2D) = "bump" {}

        _SecondaryTexture("Secondary Texture", 2D) = "white" {}
        _SecondaryScale("Secondary Scale", Float) = 1
        _SecondaryAlpha("Secondary Alpha", Range(0,1)) = 1

        _Downsample("Downsample", Vector) = (100,100,100,0)
        _ScrollSpeed("Scroll Speed (X, Y)", Vector) = (0.01,0.01,0,0)
        
        _WaveFrequency("Wave Frequency", Float) = 1
        _WaveSpeed("Wave Speed", Float) = 1
        _WaveAmount("Wave Amount", Float) = 0

        [HDR]_EmissionColor("Emission Color", Color) = (0,0,0,0)
        _EmissionMap("Emission Map", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Geometry+1"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            TEXTURE2D(_RoughnessMap);       SAMPLER(sampler_RoughnessMap);
            TEXTURE2D(_SecondaryTexture);   SAMPLER(sampler_SecondaryTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BumpMap_ST;
                float4 _RoughnessMap_ST;
                float4 _EmissionMap_ST;
                
                float4 _BaseColor;
                float _Alpha;
                float _Metallic;
                float _Smoothness;

                float4 _SecondaryTexture_ST;
                float _SecondaryScale;
                float _SecondaryAlpha;

                float4 _EmissionColor;
                float3 _Downsample;
                float2 _ScrollSpeed;
                float _WaveFrequency;
                float _WaveSpeed;
                float _WaveAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float2 uv2        : TEXCOORD1;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 tangentWS  : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                float2 uvNoScroll : TEXCOORD4;
                float2 uv2        : TEXCOORD5;

                float4 shadowCoord : TEXCOORD6;
                half4 fogAndVertexLight : TEXCOORD7;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs nrmInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                // Apply downsample effect for blocky glitch
                float3 downsampledWS = posInputs.positionWS * _Downsample;
                downsampledWS = round(downsampledWS);
                downsampledWS = downsampledWS / _Downsample;

                // Apply wave animation along vertex normal
                float wave = sin((downsampledWS.x + downsampledWS.z) * _WaveFrequency + _Time.y * _WaveSpeed);
                downsampledWS += nrmInputs.normalWS * wave * _WaveAmount;

                OUT.positionCS = TransformWorldToHClip(downsampledWS);
                OUT.positionWS = downsampledWS;

                OUT.normalWS = nrmInputs.normalWS;
                OUT.tangentWS = float4(nrmInputs.tangentWS.xyz, IN.tangentOS.w);

                // Apply texture tiling/offset and animated scrolling
                float2 scrollOffset = _ScrollSpeed * _Time.y;
                OUT.uvNoScroll = IN.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                OUT.uv = OUT.uvNoScroll + scrollOffset;
                OUT.uv2 = IN.uv2;

                OUT.shadowCoord = GetShadowCoord(posInputs);

                half fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                half3 vLight = VertexLighting(posInputs.positionWS, nrmInputs.normalWS);
                OUT.fogAndVertexLight = half4(fogFactor, vLight);

                return OUT;
            }

            inline float4 SampleSecondaryTriplanar(float3 positionWS, float3 normalWS)
            {
                // Classic world-space triplanar mapping.
                // Uses _SecondaryScale for world tiling and _SecondaryTexture_ST for additional tiling/offset.
                float3 n = normalize(normalWS);
                float3 w = pow(abs(n), 4.0);
                w /= max(w.x + w.y + w.z, 1e-5);

                // Project onto the three axis-aligned planes.
                float2 uvX = positionWS.zy; // X-facing surfaces sample YZ
                float2 uvY = positionWS.xz; // Y-facing surfaces sample XZ
                float2 uvZ = positionWS.xy; // Z-facing surfaces sample XY

                // Flip one axis based on normal sign to reduce mirrored seams.
                if (n.x < 0.0) uvX.x = -uvX.x;
                if (n.y < 0.0) uvY.x = -uvY.x;
                if (n.z < 0.0) uvZ.x = -uvZ.x;

                uvX = uvX * _SecondaryScale * _SecondaryTexture_ST.xy + _SecondaryTexture_ST.zw;
                uvY = uvY * _SecondaryScale * _SecondaryTexture_ST.xy + _SecondaryTexture_ST.zw;
                uvZ = uvZ * _SecondaryScale * _SecondaryTexture_ST.xy + _SecondaryTexture_ST.zw;

                float4 sX = SAMPLE_TEXTURE2D(_SecondaryTexture, sampler_SecondaryTexture, uvX);
                float4 sY = SAMPLE_TEXTURE2D(_SecondaryTexture, sampler_SecondaryTexture, uvY);
                float4 sZ = SAMPLE_TEXTURE2D(_SecondaryTexture, sampler_SecondaryTexture, uvZ);

                return sX * w.x + sY * w.y + sZ * w.z;
            }

            inline SurfaceData BuildSurfaceData(float2 uv, float2 baseAlphaUV, half3 normalTS, float3 positionWS, float3 normalWS, float3 viewDirWS)
            {
                SurfaceData s;
                ZERO_INITIALIZE(SurfaceData, s);

                // Sample base albedo (uv already has _BaseMap_ST applied and animation)
                float4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
                float baseAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseAlphaUV).a * _BaseColor.a;

                // Sample secondary texture using world-space triplanar projection (RGB for color, A for mask)
                float4 secondarySample = SampleSecondaryTriplanar(positionWS, normalWS);
                float secondaryMask = secondarySample.a;
                
                // Blend albedo between base and secondary texture using mask
                s.albedo = lerp(baseSample.rgb, secondarySample.rgb, secondaryMask);
                s.alpha = lerp(baseAlpha * _Alpha, _SecondaryAlpha, secondaryMask);

                s.metallic = _Metallic;
                
                // Sample roughness map (apply proper UV transform)
                float2 roughnessUV = uv * _RoughnessMap_ST.xy / _BaseMap_ST.xy + (_RoughnessMap_ST.zw - _BaseMap_ST.zw);
                float roughness = SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, roughnessUV).r;
                s.smoothness = _Smoothness * (1.0 - roughness);

                // Use base normal map only
                s.normalTS = normalTS;

                s.occlusion = 1.0;

                // Sample emission map (apply proper UV transform)
                float2 emissionUV = uv * _EmissionMap_ST.xy / _BaseMap_ST.xy + (_EmissionMap_ST.zw - _BaseMap_ST.zw);
                float3 em = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, emissionUV).rgb * _EmissionColor.rgb;
                s.emission = em + secondarySample.rgb * secondaryMask * _EmissionColor.a;

                s.specular = 0;
                s.clearCoatMask = 0;
                s.clearCoatSmoothness = 0;

                return s;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);

                float3 tangentWS = normalize(IN.tangentWS.xyz);
                float3 normalWS  = normalize(IN.normalWS);
                float tangentSign = IN.tangentWS.w * GetOddNegativeScale();
                float3 bitangentWS = cross(normalWS, tangentWS) * tangentSign;
                float3x3 TBN = float3x3(tangentWS, bitangentWS, normalWS);

                // Sample normal map with proper UV transform
                float2 bumpUV = IN.uv * _BumpMap_ST.xy / _BaseMap_ST.xy + (_BumpMap_ST.zw - _BaseMap_ST.zw);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, bumpUV));

                SurfaceData surfaceData = BuildSurfaceData(IN.uv, IN.uvNoScroll, normalTS, IN.positionWS, normalWS, viewDirWS);
                float3 nWS = normalize(mul(surfaceData.normalTS, TBN));
                
         

                InputData inputData;
                ZERO_INITIALIZE(InputData, inputData);

                inputData.positionWS = IN.positionWS;
                inputData.normalWS = nWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = IN.shadowCoord;
                inputData.fogCoord = IN.fogAndVertexLight.x;
                inputData.vertexLighting = IN.fogAndVertexLight.yzw;
                inputData.bakedGI = SAMPLE_GI(IN.uv2, IN.positionWS, nWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(IN.uv2);

                half4 col = UniversalFragmentPBR(inputData, surfaceData);
                col.rgb = MixFog(col.rgb, inputData.fogCoord);
                
                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            TEXTURE2D(_BaseMap);              SAMPLER(sampler_BaseMap);
            TEXTURE2D(_SecondaryTexture);     SAMPLER(sampler_SecondaryTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Alpha;

                float4 _SecondaryTexture_ST;
                float _SecondaryScale;
                float _SecondaryAlpha;

                float2 _ScrollSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            inline float4 SampleSecondaryTriplanar(float3 positionWS, float3 normalWS)
            {
                float3 n = normalize(normalWS);
                float3 w = pow(abs(n), 4.0);
                w /= max(w.x + w.y + w.z, 1e-5);

                float2 uvX = positionWS.zy;
                float2 uvY = positionWS.xz;
                float2 uvZ = positionWS.xy;

                if (n.x < 0.0) uvX.x = -uvX.x;
                if (n.y < 0.0) uvY.x = -uvY.x;
                if (n.z < 0.0) uvZ.x = -uvZ.x;

                uvX = uvX * _SecondaryScale * _SecondaryTexture_ST.xy + _SecondaryTexture_ST.zw;
                uvY = uvY * _SecondaryScale * _SecondaryTexture_ST.xy + _SecondaryTexture_ST.zw;
                uvZ = uvZ * _SecondaryScale * _SecondaryTexture_ST.xy + _SecondaryTexture_ST.zw;

                float4 sX = SAMPLE_TEXTURE2D(_SecondaryTexture, sampler_SecondaryTexture, uvX);
                float4 sY = SAMPLE_TEXTURE2D(_SecondaryTexture, sampler_SecondaryTexture, uvY);
                float4 sZ = SAMPLE_TEXTURE2D(_SecondaryTexture, sampler_SecondaryTexture, uvZ);

                return sX * w.x + sY * w.y + sZ * w.z;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                OUT.uv = IN.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                OUT.positionWS = positionWS;
                OUT.normalWS = normalWS;

                OUT.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _MainLightPosition.xyz));
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);              SAMPLER(sampler_BaseMap);
            TEXTURE2D(_SecondaryTexture);     SAMPLER(sampler_SecondaryTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Alpha;

                float4 _SecondaryTexture_ST;
                float _SecondaryScale;
                float _SecondaryAlpha;

                float2 _ScrollSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            inline float4 SampleSecondaryTriplanar(float3 positionWS, float3 normalWS)
            {
                float3 n = normalize(normalWS);
                float3 w = pow(abs(n), 4.0);
                w /= max(w.x + w.y + w.z, 1e-5);

                float2 uvX = positionWS.zy;
                float2 uvY = positionWS.xz;
                float2 uvZ = positionWS.xy;

                if (n.x < 0.0) uvX.x = -uvX.x;
                if (n.y < 0.0) uvY.x = -uvY.x;
                if (n.z < 0.0) uvZ.x = -uvZ.x;

                uvX = uvX * _SecondaryScale * _SecondaryTexture_ST.xy + _SecondaryTexture_ST.zw;
                uvY = uvY * _SecondaryScale * _SecondaryTexture_ST.xy + _SecondaryTexture_ST.zw;
                uvZ = uvZ * _SecondaryScale * _SecondaryTexture_ST.xy + _SecondaryTexture_ST.zw;

                float4 sX = SAMPLE_TEXTURE2D(_SecondaryTexture, sampler_SecondaryTexture, uvX);
                float4 sY = SAMPLE_TEXTURE2D(_SecondaryTexture, sampler_SecondaryTexture, uvY);
                float4 sZ = SAMPLE_TEXTURE2D(_SecondaryTexture, sampler_SecondaryTexture, uvZ);

                return sX * w.x + sY * w.y + sZ * w.z;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = posInputs.positionCS;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
                OUT.positionWS = positionWS;
                OUT.normalWS = normalWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
