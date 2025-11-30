Shader "Custom/SandSprite2D"
{
    Properties
    {
        _MainTex           ("Sprite Texture", 2D) = "white" {}
        _SandLight         ("Sand Light Color", Color) = (0.94, 0.86, 0.65, 1)
        _SandDark          ("Sand Dark Color",  Color) = (0.80, 0.72, 0.52, 1)

        _RippleScale       ("Ripple Scale", Float) = 6.0
        _RippleStrength    ("Ripple Strength", Range(0, 1)) = 0.4

        _GrainScale        ("Grain Scale", Float) = 40.0
        _GrainStrength     ("Grain Strength", Range(0, 1)) = 0.25

        _OverallBrightness ("Overall Brightness", Range(0, 2)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Blend One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;

            fixed4 _SandLight;
            fixed4 _SandDark;

            float _RippleScale;
            float _RippleStrength;

            float _GrainScale;
            float _GrainStrength;

            float _OverallBrightness;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color  = v.color;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float valueNoise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                float2 u = f * f * (3.0 - 2.0 * f);

                float lerpX1 = lerp(a, b, u.x);
                float lerpX2 = lerp(c, d, u.x);
                return lerp(lerpX1, lerpX2, u.y); // 0–1
            }

            float fbm(float2 p)
            {
                float value = 0.0;
                float amp   = 0.5;
                float freq  = 1.0;

                [unroll]
                for (int i = 0; i < 4; ++i)
                {
                    value += amp * valueNoise2D(p * freq);
                    freq  *= 2.0;
                    amp   *= 0.5;
                }
                return value;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 vertexColor = i.color;

                float alpha = tex.a * vertexColor.a;

                float2 uv = i.uv;

                float rippleBase = sin(uv.y * _RippleScale);
                float rippleNoise = fbm(uv * (_RippleScale * 0.35));
                float ripple = rippleBase * (1.0 - rippleNoise);
                ripple = ripple * 0.5 + 0.5;
                ripple = lerp(0.5, ripple, _RippleStrength); // fade to neutral

                float grain = fbm(uv * _GrainScale);
                float grainMask = lerp(0.5, grain, _GrainStrength); // keep subtle

                float shade = (ripple * 0.6 + grainMask * 0.4);

                float3 sandCol = lerp(_SandDark.rgb, _SandLight.rgb, shade);
                sandCol *= _OverallBrightness;

                sandCol *= vertexColor.rgb;

                return fixed4(sandCol, alpha);
            }
            ENDCG
        }
    }

    FallBack Off
}
