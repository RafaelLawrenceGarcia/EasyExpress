// ============================================================
//  OutlineEdge.shader
//  
//  Save this file at: Assets/Shaders/OutlineEdge.shader
//
//  HOW IT WORKS (Stencil Silhouette Method):
//  ──────────────────────────────────────────
//  This shader uses TWO passes and a stencil buffer to
//  ensure ONLY the outer silhouette edge is visible.
//  Internal edges, wireframe lines, and geometry seams
//  are completely hidden.
//
//  Pass 1 — MASK:
//    Renders the mesh at its normal size, writing a stencil
//    value everywhere the object covers pixels. This creates
//    a filled silhouette mask. Nothing is drawn to screen
//    (ColorMask 0) — it only writes stencil data.
//
//  Pass 2 — OUTLINE:
//    Renders the mesh again, enlarged along normals. But it
//    ONLY draws where the stencil was NOT written (NotEqual).
//    Since Pass 1 filled the inside, only the expanded rim
//    around the outside edge is visible.
//
//  Result: A clean outer contour with no internal edges.
//
//  SETUP:
//  1. Create a Material, set shader to "Custom/OutlineEdge"
//  2. _OutlineColor = your color (Gold #FFD700 recommended)
//  3. _OutlineWidth = 0.02 to 0.05 (adjust to taste)
//  4. Drag onto InspectionManager.outlineMaterial
//     and PlayerInteract.outlineMaterial
// ============================================================

Shader "Custom/OutlineEdge"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.85, 0.1, 1)
        _OutlineWidth ("Outline Width", Range(0.001, 0.1)) = 0.025
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry+1"
        }

        // =================================================
        //  PASS 1 — STENCIL MASK
        //  Draws the original-size mesh to the stencil
        //  buffer only. No pixels are written to the screen.
        //  This fills the interior so Pass 2 can exclude it.
        // =================================================
        Pass
        {
            Name "STENCIL_MASK"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            // Write stencil value 1 everywhere the mesh covers
            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            // Don't draw any color — stencil only
            ColorMask 0
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }

        // =================================================
        //  PASS 2 — OUTLINE EDGE
        //  Renders the mesh enlarged along normals, but
        //  ONLY where the stencil is NOT 1 (i.e. outside
        //  the original silhouette). Internal edges are
        //  hidden because they fall inside the stencil mask.
        // =================================================
        Pass
        {
            Name "OUTLINE_EDGE"
            Tags { "LightMode" = "UniversalForward" }

            // Only draw where stencil != 1 (outside the mask)
            Stencil
            {
                Ref 1
                Comp NotEqual
                Pass Keep
            }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Expand vertex along its normal to create the outline shell
                float3 expandedPos = input.positionOS.xyz + normalize(input.normalOS) * _OutlineWidth;
                output.positionCS = TransformObjectToHClip(expandedPos);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return half4(_OutlineColor.rgb, 1.0);
            }

            ENDHLSL
        }
    }

    Fallback Off
}
