Shader "UI/StormHoleUI"
{
    Properties
    {
        _Color("Outside Color", Color) = (0.6,0.2,0.8,1)
        _Center("Center (UV)", Vector) = (0.5,0.5,0,0)
        _Radius("Radius (UV)", Float) = 0.4
        _Feather("Feather", Float) = 0.02
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float4 _Center;
            float _Radius;
            float _Feather;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.uv;
                float2 c = _Center.xy;

                float d = distance(p, c);

                // 0 dentro, 1 fuera (con borde suave)
                float mask = smoothstep(_Radius, _Radius + _Feather, d);

                fixed4 col = _Color;
                col.a *= mask; // dentro alpha ~0, fuera alpha ~1
                return col;
            }
            ENDCG
        }
    }
}