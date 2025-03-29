Shader "Unlit/DebugParticle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LowColor ("Min Color", Color) = (0, 0, 0, 1)
        _HighColor ("Max Color", Color) = (1, 1, 1, 1)
        _MinSpeed ("Minimum Speed", Float) = 0
        _MaxSpeed ("Maximum Speed", Float) = 1.0

        _RadiusMult ("Relative Radius", Float) = 1.0

        _Velocity("Current Velocity", Vector) = (1, 1, 1, 0)
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _LowColor;
            float4 _HighColor;

            float _MinSpeed;
            float _MaxSpeed;
            float _RadiusMult;

            // programatic properties
            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float3, _Velocity)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 vel = UNITY_ACCESS_INSTANCED_PROP(Props, _Velocity).xyz;
                float min_sq = _MinSpeed * _MinSpeed;
                float max_sq = _MaxSpeed * _MaxSpeed;
                float spd_sq = dot(vel, vel);

                float range = clamp(spd_sq, min_sq, max_sq) / (max_sq - min_sq);
                float4 spd_col = lerp(_LowColor, _HighColor, range);

                float2 mid_dist = 0.5 - i.uv;
                float d = 0.25 * _RadiusMult - dot(mid_dist, mid_dist);
                float m = max(0, d) / d;
                return spd_col * m;
            }
            ENDCG
        }
    }
}
