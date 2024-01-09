Shader "Unlit/preserve-alpha"
{
    Properties
    {
        _Color("Main Color", Color) = (1, 0.6656064, 0, 0.4823529)
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite On
        Blend SrcAlpha OneMinusSrcAlpha
        Cull back
        LOD 100

        Stencil
        {
            Ref 2
            Comp NotEqual
            Pass Replace
        }

        Pass
        {
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            //#pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                //float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                //float2 uv : TEXCOORD0;
                //UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            //sampler2D _MainTex;
            float4 _Color;
            //float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v); //Insert
                UNITY_INITIALIZE_OUTPUT(v2f, o); //Insert
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); //Insert
                o.vertex = UnityObjectToClipPos(v.vertex);
                //o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                //UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                //fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, col);
                return _Color;
            }
            ENDCG
        }
    }
}
