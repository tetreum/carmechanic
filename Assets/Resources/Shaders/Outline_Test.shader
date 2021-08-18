Shader "Standard Outlined"
{
    Properties
    {
        [LM_Albedo] [LM_Transparency] _Color("Color", Color) = (1,1,1,1)  
        [LM_MasterTilingOffset] [LM_Albedo] _MainTex("Albedo", 2D) = "white" {}
     
        [LM_TransparencyCutOff] _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
 
        [LM_Glossiness] _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
        [LM_Metallic] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        [LM_Metallic] [LM_Glossiness] _MetallicGlossMap("Metallic", 2D) = "white" {}
 
         _BumpScale("Scale", Float) = 1.0
        [LM_NormalMap] _BumpMap("Normal Map", 2D) = "bump" {}
 
        _Parallax ("Height Scale", Range (0.005, 0.08)) = 0.02
        _ParallaxMap ("Height Map", 2D) = "black" {}
 
        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}
 
        [LM_Emission] _EmissionColor("Color", Color) = (0,0,0)
        [LM_Emission] _EmissionMap("Emission", 2D) = "white" {}
     
        _DetailMask("Detail Mask", 2D) = "white" {}
 
        _DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
        _DetailNormalMapScale("Scale", Float) = 1.0
        _DetailNormalMap("Normal Map", 2D) = "bump" {}
 
        [Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0
 
        // UI-only data
        [KeywordEnum(None, Realtime, Baked)]  _Lightmapping ("GI", Int) = 1
        [HideInInspector] _EmissionScaleUI("Scale", Float) = 0.0
        [HideInInspector] _EmissionColorUI("Color", Color) = (1,1,1)
 
        // Blending state
        [HideInInspector] _Mode ("__mode", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
 
        // Outline
        _Outline ("Outline Extrusion", Range(-1,1)) = 0.05
        _OutColor ("Outline Color", Color) = (1,1,1,1)
    }
 
    CGINCLUDE
        //@TODO: should this be pulled into a shader_feature, to be able to turn it off?
        #define _GLOSSYENV 1
        #define UNITY_SETUP_BRDF_INPUT MetallicSetup
    ENDCG
 
    SubShader
    {
        Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
        LOD 300
 
        // Outline addition starts here
        Cull Off
        ZWrite Off
        //ZTest Always // Uncomment for "see through"
 
        CGPROGRAM
            #pragma surface surf Solid vertex:vert
            struct Input {
                float4 color : COLOR;
            };
 
            fixed4 _OutColor;
            float _Outline;
 
        fixed4 LightingSolid (SurfaceOutput s, half3 lightDir, half atten) {
        return _OutColor;
        }
 
            void vert (inout appdata_full v) {
                v.vertex.xyz += v.normal * _Outline;
            }
 
            void surf (Input IN, inout SurfaceOutput o) {
                o.Albedo = _OutColor.rgb;
            }
        ENDCG
 
        Cull Back
        ZWrite On  
        ZTest LEqual
        // Outline addition ends here
 
        // ------------------------------------------------------------------
        //  Base forward pass (directional light, emission, lightmaps, ...)
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }
 
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
 
            CGPROGRAM
            #pragma target 3.0
            // TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
            #pragma exclude_renderers gles
         
            // -------------------------------------
                 
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _EMISSION
            //ALWAYS ON shader_feature _GLOSSYENV
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature ___ _DETAIL_MULX2
            #pragma shader_feature _PARALLAXMAP
         
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
             
            #pragma vertex vertForwardBase
            #pragma fragment fragForwardBase
 
            #include "UnityStandardCore.cginc"
 
            ENDCG
        }
        // ------------------------------------------------------------------
        //  Additive forward pass (one light per pass)
        Pass
        {
            Name "FORWARD_DELTA"
            Tags { "LightMode" = "ForwardAdd" }
            Blend [_SrcBlend] One
            Fog { Color (0,0,0,0) } // in additive pass fog should be black
            ZWrite Off
            ZTest LEqual
 
            CGPROGRAM
            #pragma target 3.0
            // GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
            #pragma exclude_renderers gles
 
            // -------------------------------------
 
         
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature ___ _DETAIL_MULX2
            #pragma shader_feature _PARALLAXMAP
         
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog
         
            #pragma vertex vertForwardAdd
            #pragma fragment fragForwardAdd
 
            #include "UnityStandardCore.cginc"
 
            ENDCG
        }
        // ------------------------------------------------------------------
        //  Shadow rendering pass
        Pass {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
         
            ZWrite On ZTest LEqual
 
            CGPROGRAM
            #pragma target 3.0
            // TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
            #pragma exclude_renderers gles
         
            // -------------------------------------
 
 
            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma multi_compile_shadowcaster
 
            #pragma vertex vertShadowCaster
            #pragma fragment fragShadowCaster
 
            #include "UnityStandardShadow.cginc"
 
            ENDCG
        }
        // ------------------------------------------------------------------
        //  Deferred pass
        Pass
        {
            Name "DEFERRED"
            Tags { "LightMode" = "Deferred" }
 
            CGPROGRAM
            #pragma target 3.0
            // TEMPORARY: GLES2.0 temporarily disabled to prevent errors spam on devices without textureCubeLodEXT
            #pragma exclude_renderers nomrt gles
         
 
            // -------------------------------------
 
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _EMISSION
            //ALWAYS ON shader_feature _GLOSSYENV
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature ___ _DETAIL_MULX2
            #pragma shader_feature _PARALLAXMAP
 
            #pragma multi_compile ___ UNITY_HDR_ON
            #pragma multi_compile LIGHTMAP_OFF LIGHTMAP_ON
            #pragma multi_compile DIRLIGHTMAP_OFF DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
            #pragma multi_compile DYNAMICLIGHTMAP_OFF DYNAMICLIGHTMAP_ON
         
            #pragma vertex vertDeferred
            #pragma fragment fragDeferred
 
            #include "UnityStandardCore.cginc"
 
            ENDCG
        }
 
        // ------------------------------------------------------------------
        // Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass it not used during regular rendering.
        Pass
        {
            Name "META"
            Tags { "LightMode"="Meta" }
 
            Cull Off
 
            CGPROGRAM
            #pragma vertex vert_meta
            #pragma fragment frag_meta
 
            #pragma shader_feature _EMISSION
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature ___ _DETAIL_MULX2
 
            #include "UnityStandardMeta.cginc"
            ENDCG
        }
    }
 
    SubShader
    {
        Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
        LOD 150
 
        // ------------------------------------------------------------------
        //  Base forward pass (directional light, emission, lightmaps, ...)
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }
 
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
 
            CGPROGRAM
            #pragma target 2.0
         
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _EMISSION
            // ALWAYS ON shader_feature _GLOSSYENV
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature ___ _DETAIL_MULX2
            // SM2.0: NOT SUPPORTED shader_feature _PARALLAXMAP
 
            #pragma skip_variants SHADOWS_SOFT DIRLIGHTMAP_COMBINED DIRLIGHTMAP_SEPARATE
 
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
 
            #pragma vertex vertForwardBase
            #pragma fragment fragForwardBase
 
            #include "UnityStandardCore.cginc"
 
            ENDCG
        }
        // ------------------------------------------------------------------
        //  Additive forward pass (one light per pass)
        Pass
        {
            Name "FORWARD_DELTA"
            Tags { "LightMode" = "ForwardAdd" }
            Blend [_SrcBlend] One
            Fog { Color (0,0,0,0) } // in additive pass fog should be black
            ZWrite Off
            ZTest LEqual
         
            CGPROGRAM
            #pragma target 2.0
 
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature ___ _DETAIL_MULX2
            // SM2.0: NOT SUPPORTED shader_feature _PARALLAXMAP
            #pragma skip_variants SHADOWS_SOFT
         
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog
         
            #pragma vertex vertForwardAdd
            #pragma fragment fragForwardAdd
 
            #include "UnityStandardCore.cginc"
 
            ENDCG
        }
        // ------------------------------------------------------------------
        //  Shadow rendering pass
        Pass {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
         
            ZWrite On ZTest LEqual
 
            CGPROGRAM
            #pragma target 2.0
 
            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma skip_variants SHADOWS_SOFT
            #pragma multi_compile_shadowcaster
 
            #pragma vertex vertShadowCaster
            #pragma fragment fragShadowCaster
 
            #include "UnityStandardShadow.cginc"
 
            ENDCG
        }
 
        // ------------------------------------------------------------------
        // Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass it not used during regular rendering.
        Pass
        {
            Name "META"
            Tags { "LightMode"="Meta" }
 
            Cull Off
 
            CGPROGRAM
            #pragma vertex vert_meta
            #pragma fragment frag_meta
 
            #pragma shader_feature _EMISSION
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature ___ _DETAIL_MULX2
 
            #include "UnityStandardMeta.cginc"
            ENDCG
        }
    }
 
    FallBack "VertexLit"
    //CustomEditor "StandardShaderGUI"
}
 