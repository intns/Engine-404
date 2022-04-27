Shader "Project 404/Particle Wobbly Additive"
{
  Properties
  {
    _MainTex ("Texture", 2D) = "white" {}
    _Displacement ("Displacement Map", 2D) = "white" {}
    _DisplacementStrength ("Displacement Strength", Range(0,32)) = 0.0
    _DisplacementSpeed ("Displacement Speed", float) = 1
  }
  SubShader
  {
    Tags { "RenderType"="Transparent" "Queue"="Transparent" }
    LOD 100
    Blend SrcAlpha One
    Zwrite Off
    Cull Back
    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      // make fog work
      #pragma multi_compile_fog
      
      #include "UnityCG.cginc"
      struct appdata
      {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
        float4 color : COLOR;
      };
      struct v2f
      {
        float2 uv : TEXCOORD0;
        UNITY_FOG_COORDS(1)
        float4 vertex : SV_POSITION;
        float4 color : COLOR;
      };
      sampler2D _MainTex;
      float4 _MainTex_ST;
      sampler2D _Displacement;
      half _DisplacementStrength;
      half _DisplacementSpeed;
      
      v2f vert (appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.uv, _MainTex);
        UNITY_TRANSFER_FOG(o,o.vertex);
        o.color = v.color;
        return o;
      }
      
      fixed4 frag (v2f i) : SV_Target
      {
        // sample the texture
        float2 uv = i.uv;
        uv += fixed2((_Time.y)*_DisplacementSpeed, (_Time.y)*0);
        float displacement = (tex2D(_Displacement, uv).rgb);

        fixed4 col = tex2D(_MainTex, i.uv

        +(lerp(-1,1,displacement)*_DisplacementStrength)

       // +((tex2D(_Displacement, uv).rgb*_DisplacementStrength) - ((tex2D(_Displacement, uv).rgb*_DisplacementStrength)/2))
       )
        * i.color;
        // apply fog


        UNITY_APPLY_FOG(i.fogCoord, col);
        //return (displacement-(displacement/2)*2);
       	return col;
      }
      ENDCG
    }
  }
}