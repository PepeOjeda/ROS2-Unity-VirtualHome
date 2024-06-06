
Shader "CRT/Color" {
    Properties {
     _Color ("Main Color", Color) = (1.000000,1.000000,1.000000,1.000000)
    }

    SubShader { 
        LOD 100
        Tags { "RenderType"="Opaque" }
        Pass 
        {
            Tags { "RenderType"="Opaque" }

            Name "New Custom Render Texture"

            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            float4      _Color;
            sampler2D   _MainTex;

            float4 frag(v2f_customrendertexture IN) : SV_Target
            {
                float2 uv = IN.localTexcoord.xy;
                return _Color;
            }
            ENDCG
        
        }
    }
}