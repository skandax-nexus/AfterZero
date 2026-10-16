// AfterZero — hologram / temporal echo shader (URP, additive, unlit).
// Used for Future-state projections, echoes of other states, The Witness, clue highlights.
Shader "AfterZero/Hologram"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color", Color) = (0.1, 0.6, 1.0, 1)
        [HDR] _RimColor ("Rim Color", Color) = (0.4, 0.9, 1.0, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.5
        _Alpha ("Alpha", Range(0, 1)) = 0.6
        _Intensity ("Intensity", Range(0, 8)) = 1.5
        _ScanDensity ("Scanline Density", Float) = 60
        _ScanSpeed ("Scanline Speed", Float) = 4
        _GridScale ("World Grid Scale", Float) = 2
        _GridStrength ("World Grid Strength", Range(0, 1)) = 0.35
        _FlickerSpeed ("Flicker Speed", Float) = 6
        _GlitchStrength ("Glitch Strength", Range(0, 0.2)) = 0.02
        [Toggle] _UseEchoReveal ("Fade With Echo Sense", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "HologramForward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _RimColor;
                half _RimPower;
                half _Alpha;
                half _Intensity;
                float _ScanDensity;
                float _ScanSpeed;
                float _GridScale;
                half _GridStrength;
                float _FlickerSpeed;
                float _GlitchStrength;
                half _UseEchoReveal;
            CBUFFER_END

            // Set globally by TemporalVisuals (0 = echoes hidden, 1 = fully revealed).
            float _AZ_EchoReveal;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash11(float p)
            {
                return frac(sin(p * 127.1) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 pos = input.positionOS.xyz;
                float tick = floor(_Time.y * _FlickerSpeed);
                float glitchOn = step(0.93, Hash11(tick));
                pos.x += glitchOn * _GlitchStrength * sin(pos.y * 40.0 + _Time.y * 50.0);

                VertexPositionInputs vpi = GetVertexPositionInputs(pos);
                output.positionCS = vpi.positionCS;
                output.positionWS = vpi.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 n = normalize(input.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(input.positionWS));
                half rim = pow(1.0 - saturate(abs(dot(n, v))), _RimPower);

                half scan = 0.5 + 0.5 * sin(input.positionWS.y * _ScanDensity - _Time.y * _ScanSpeed);
                scan = smoothstep(0.3, 1.0, scan);

                float3 gp = frac(input.positionWS * _GridScale);
                float3 gl = step(0.965, gp);
                half grid = max(gl.x, max(gl.y, gl.z)) * _GridStrength;

                half flicker = 0.88 + 0.12 * sin(_Time.y * _FlickerSpeed * 7.3) * sin(_Time.y * 3.1);
                half reveal = lerp(1.0, saturate(_AZ_EchoReveal), _UseEchoReveal);

                half3 col = _BaseColor.rgb * (0.25 + scan * 0.5) + _RimColor.rgb * (rim + grid);
                half alpha = saturate((0.12 + rim * 0.88 + scan * 0.2 + grid) * _Alpha * flicker * reveal);
                return half4(col * _Intensity, alpha);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
