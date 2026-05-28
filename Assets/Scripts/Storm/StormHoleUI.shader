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
                v2f output;
                output.vertex = UnityObjectToClipPos(v.vertex);
                output.uv = v.uv;

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 position = input.uv;
                float2 center = _Center.xy;

                float distanceToCenter = distance(position, center);
                float mask = smoothstep(_Radius, _Radius + _Feather, distanceToCenter);

                fixed4 color = _Color;
                color.a *= mask;

                return color;
            }
            ENDCG
        }
    }
}