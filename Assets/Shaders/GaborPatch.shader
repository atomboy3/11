Shader "VisionTherapy/GaborPatch"
{
    Properties
    {
        _Frequency     ("Spatial Frequency",  Range(1,32))   = 4.0
        _Orientation   ("Orientation (rad)",  Range(0,3.14)) = 0.785
        _GaborContrast ("Gabor Contrast",     Range(0,1))    = 0.8
        _Sigma         ("Gaussian Sigma",     Range(0.05,2)) = 0.3
        _Phase         ("Phase",              Range(0,6.28)) = 0.0
        _Contrast      ("Inter-Ocular Contrast", Range(0,1)) = 1.0
        _Color         ("Tint",               Color)         = (1,1,1,1)
        _BGColor       ("Background Gray",    Color)         = (0.5,0.5,0.5,1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float  _Frequency;
                float  _Orientation;
                float  _GaborContrast;
                float  _Sigma;
                float  _Phase;
                float  _Contrast;
                float4 _Color;
                float4 _BGColor;
            CBUFFER_END

            struct Attributes { float4 posOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 posCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings o;
                o.posCS = TransformObjectToHClip(IN.posOS.xyz);
                o.uv    = IN.uv - 0.5; // center at 0
                return o;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // Rotate UV by orientation
                float cosA =  cos(_Orientation), sinA = sin(_Orientation);
                float xRot =  cosA * uv.x + sinA * uv.y;

                // Sine carrier
                float carrier = sin(2.0 * PI * _Frequency * xRot + _Phase);

                // Gaussian envelope
                float r2      = dot(uv, uv);
                float gauss   = exp(-r2 / (2.0 * _Sigma * _Sigma));

                // Gabor = Gaussian × Sine, remapped [0,1]
                float lum     = 0.5 + 0.5 * gauss * carrier * _GaborContrast;

                float3 col    = lerp(_BGColor.rgb, float3(lum, lum, lum), gauss);

                return half4(col * _Color.rgb, _Color.a * _Contrast);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/InternalErrorShader"
}
