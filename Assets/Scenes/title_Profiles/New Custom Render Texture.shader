Shader "CustomRenderTexture/New Custom Render Texture"
 {
Properties {
_MainTex ("Texture", 2D) = "white" {}
_BlurSize ("Blur Size", Range(0.0, 10.0)) = 1.0
}
SubShader {
Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

sampler2D _MainTex;
float4 _MainTex_TexelSize;
float _BlurSize;

struct appdata_t {
float4 vertex : POSITION;
float2 uv : TEXCOORD0;
};

struct v2f {
float4 pos : SV_POSITION;
float2 uv : TEXCOORD0;
};

v2f vert(appdata_t v) {
v2f o;
o.pos = UnityObjectToClipPos(v.vertex);
o.uv = v.uv;
return o;
}

fixed4 frag(v2f i) : SV_Target {
fixed4 col = 0;
for (int x = -2; x <= 2; x++) {
for (int y = -2; y <= 2; y++) {
col += tex2D(_MainTex, i.uv + float2(x, y) * _MainTex_TexelSize.xy * _BlurSize);
}
}
return col / 25.0; // •½‹Ï‰»
}
ENDCG
}
}
}