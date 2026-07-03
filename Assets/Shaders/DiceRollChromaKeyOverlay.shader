// Composites DiceRollController's arena RenderTexture over the 2D UI beneath
// it. URP was found (live, via a direct RenderTexture pixel readback) to force
// alpha=1 for every pixel a camera renders into a RenderTexture, regardless of
// the camera's own background alpha - so the arena's "empty" background was
// opaque and hid the board underneath instead of letting it show through
// during the roll. This shader keys out the arena camera's known, distinctive
// background color instead of relying on the texture's alpha channel at all.
Shader "Quintessence/DiceRollChromaKeyOverlay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _KeyColor ("Key Color", Color) = (0, 1, 0.4, 1)
        _Threshold ("Threshold", Float) = 0.05
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _KeyColor;
            float _Threshold;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                float dist = distance(col.rgb, _KeyColor.rgb);
                col.a = dist < _Threshold ? 0 : 1;
                return col;
            }
            ENDCG
        }
    }
}
