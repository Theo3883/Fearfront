Shader "Custom/OutlineHighlight"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.3, 0.7, 1, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.03
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        
        // Outline pass - render enlarged backfaces
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            float _OutlineWidth;
            float4 _OutlineColor;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                
                // Extrude vertex along normal
                float3 norm = normalize(v.normal);
                float3 extrudedPos = v.vertex.xyz + norm * _OutlineWidth;
                
                o.pos = UnityObjectToClipPos(float4(extrudedPos, 1));
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
    
    Fallback "Diffuse"
}
