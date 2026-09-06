Shader "CheeseTama/Growth Palette"
{
    Properties
    {
        _Color ("Reaction Tint", Color) = (1, 1, 1, 1)
        _MainTex ("Base Color", 2D) = "white" {}
        _PaletteHue ("Body Hue", Range(0, 1)) = 0.14
        _PaletteSaturation ("Body Saturation", Range(0, 1)) = 0.5
        _PaletteValueScale ("Body Brightness Scale", Range(0.5, 1.5)) = 1
        _PaletteValueOffset ("Body Brightness", Range(-0.35, 0.35)) = 0
        _PaletteStrength ("Body Recolor Strength", Range(0, 1)) = 1
        _PaletteEmission ("Body Self Illumination", Range(0, 1)) = 0
        _EraseBlush ("Erase Blush", Range(0, 1)) = 0
        _ConditionMask ("Condition Body Mask", 2D) = "black" {}
        [HideInInspector] _FaceCleanupRegion ("Face Cleanup Region", Vector) = (0, 0, 0, 0)
        [HideInInspector] _FaceCleanupSurface0 ("Face Cleanup Surface 0", Vector) = (0, 0, 0, 0)
        [HideInInspector] _FaceCleanupSurface1 ("Face Cleanup Surface 1", Vector) = (0, 0, 0, 0)
        _ConditionColor ("Condition Body Color", Color) = (1, 1, 1, 1)
        _ConditionStrength ("Condition Body Strength", Range(0, 1)) = 0
        _ConditionValueScale ("Condition Body Brightness", Range(0.25, 1.25)) = 1
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Glossiness ("Smoothness", Range(0, 1)) = 0.25
        [HideInInspector] _EmissionColor ("Emission", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _ConditionMask;
        fixed4 _Color;
        half _PaletteHue;
        half _PaletteSaturation;
        half _PaletteValueScale;
        half _PaletteValueOffset;
        half _PaletteStrength;
        half _PaletteEmission;
        half _EraseBlush;
        float4 _FaceCleanupRegion;
        float4 _FaceCleanupSurface0;
        float4 _FaceCleanupSurface1;
        fixed4 _ConditionColor;
        half _ConditionStrength;
        half _ConditionValueScale;
        half _Metallic;
        half _Glossiness;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        inline half FaceCleanupMask(float3 objectPosition)
        {
            half2 radius = max(abs(_FaceCleanupRegion.zw), half2(0.0001h, 0.0001h));
            half2 normalizedOffset = (objectPosition.xy - _FaceCleanupRegion.xy) / radius;
            half ellipseRadius = length(normalizedOffset);
            half feather = 1.0h - smoothstep(0.72h, 1.05h, ellipseRadius);
            return saturate(feather
                * step(0.0h, objectPosition.z)
                * _FaceCleanupSurface1.w);
        }

        void vert(inout appdata_full vertex)
        {
            if (_FaceCleanupSurface1.w <= 0.0001h)
            {
                return;
            }

            float2 delta = vertex.vertex.xy - _FaceCleanupRegion.xy;
            half cleanupMask = FaceCleanupMask(vertex.vertex.xyz);
            float fittedZ = _FaceCleanupSurface0.x
                + _FaceCleanupSurface0.y * delta.x
                + _FaceCleanupSurface0.z * delta.y
                + _FaceCleanupSurface0.w * delta.x * delta.x
                + _FaceCleanupSurface1.x * delta.x * delta.y
                + _FaceCleanupSurface1.y * delta.y * delta.y;
            // The crater includes a raised rim as well as a recessed core, so
            // converge the whole visible front patch to the fitted surface.
            // The depth gate excludes rear/internal vertices that overlap the
            // same XY ellipse in this single, monolithic imported mesh.
            half visibleFront = smoothstep(0.06h, 0.11h, vertex.vertex.z);
            float correction = (fittedZ - vertex.vertex.z) * cleanupMask * visibleFront;
            vertex.vertex.z += correction;

            float dzdx = _FaceCleanupSurface0.y
                + 2.0 * _FaceCleanupSurface0.w * delta.x
                + _FaceCleanupSurface1.x * delta.y;
            float dzdy = _FaceCleanupSurface0.z
                + _FaceCleanupSurface1.x * delta.x
                + 2.0 * _FaceCleanupSurface1.y * delta.y;
            float3 fittedNormal = normalize(float3(-dzdx, -dzdy, 1.0));
            half normalBlend = cleanupMask * visibleFront;
            vertex.normal = normalize(lerp(vertex.normal, fittedNormal, normalBlend));
        }

        inline half3 RgbToHsv(half3 color)
        {
            const half4 k = half4(0.0h, -0.3333333h, 0.6666667h, -1.0h);
            half4 p = lerp(half4(color.bg, k.wz), half4(color.gb, k.xy), step(color.b, color.g));
            half4 q = lerp(half4(p.xyw, color.r), half4(color.r, p.yzx), step(p.x, color.r));
            half delta = q.x - min(q.w, q.y);
            const half epsilon = 1.0e-4h;
            return half3(
                abs(q.z + (q.w - q.y) / (6.0h * delta + epsilon)),
                delta / (q.x + epsilon),
                q.x);
        }

        inline half3 HsvToRgb(half3 hsv)
        {
            half3 channels = abs(frac(hsv.xxx + half3(0.0h, 0.6666667h, 0.3333333h)) * 6.0h - 3.0h);
            return hsv.z * lerp(half3(1.0h, 1.0h, 1.0h), saturate(channels - 1.0h), hsv.y);
        }

        void surf(Input input, inout SurfaceOutputStandard output)
        {
            fixed4 source = tex2D(_MainTex, input.uv_MainTex);
            half3 sourceHsv = RgbToHsv(source.rgb);

            // Imported character atlases use warm yellow/orange for the body.
            // Restrict recoloring to that range so eyes, mouth, blush, and white
            // highlights keep their authored colors.
            half warmHue = smoothstep(0.061h, 0.089h, sourceHsv.x)
                * (1.0h - smoothstep(0.161h, 0.20h, sourceHsv.x));
            half coloredBody = smoothstep(0.35h, 0.58h, sourceHsv.y);
            half visibleBody = smoothstep(0.30h, 0.60h, sourceHsv.z);
            half authoredBodyMask = saturate(warmHue * coloredBody * visibleBody);
            half bodyMask = authoredBodyMask * _PaletteStrength;
            // The explicit black/white role mask comes from the original
            // material assignment. It keeps gold milestone stars out of care
            // tinting even though their HSV range overlaps the cheese body.
            half conditionRoleMask = smoothstep(
                0.25h,
                0.75h,
                tex2D(_ConditionMask, input.uv_MainTex).r);
            half blushHue = 1.0h - smoothstep(0.095h, 0.115h, sourceHsv.x);
            half blushColor = smoothstep(0.05h, 0.18h, sourceHsv.y);
            half blushBrightness = smoothstep(0.65h, 0.90h, sourceHsv.z);
            half blushEraseMask = saturate(blushHue * blushColor * blushBrightness * _EraseBlush);
            half recolorMask = max(bodyMask, blushEraseMask);

            half3 paletteHsv = half3(
                _PaletteHue,
                _PaletteSaturation,
                saturate(sourceHsv.z * _PaletteValueScale + _PaletteValueOffset));
            half3 recoloredBody = HsvToRgb(paletteHsv);
            half3 paletteColor = lerp(source.rgb, recoloredBody, recolorMask);

            // Stage 4-6 source models contain one large baked crater on the
            // forehead. The vertex function fills its geometry from the
            // surrounding quadratic surface; this matching color pass removes
            // the dark baked crater pixels while keeping eyes, cheeks, mouth,
            // body holes, and milestone stars outside the cleanup ellipse.
            float3 objectPosition = mul(unity_WorldToObject, float4(input.worldPos, 1.0)).xyz;
            half faceCleanupMask = FaceCleanupMask(objectPosition);
            half cleanFaceValue = max(paletteHsv.z, _FaceCleanupSurface1.z);
            half3 cleanFaceColor = HsvToRgb(half3(
                _PaletteHue,
                _PaletteSaturation,
                cleanFaceValue));
            paletteColor = lerp(paletteColor, cleanFaceColor, faceCleanupMask);

            // Care conditions recolor only the warm body pixels. Scale the
            // current palette value so light and dark conditions retain the
            // authored texture shading while eyes, mouth, blush, and white
            // highlights stay untouched.
            half3 currentPaletteHsv = RgbToHsv(paletteColor);
            half3 conditionHsv = RgbToHsv(_ConditionColor.rgb);
            half3 conditionedBody = HsvToRgb(half3(
                conditionHsv.x,
                lerp(currentPaletteHsv.y, conditionHsv.y, saturate(_ConditionStrength)),
                saturate(currentPaletteHsv.z * _ConditionValueScale)));
            // Egg uses a white authored upper shell in addition to its warm
            // lower shell. _EraseBlush is enabled only for Egg, so it safely
            // gates the extra white-body coverage without tinting highlights
            // on the later growth stages.
            half eggWhiteBody = (1.0h - smoothstep(0.12h, 0.32h, sourceHsv.y))
                * smoothstep(0.55h, 0.82h, sourceHsv.z)
                * _EraseBlush;
            half conditionBodyMask = saturate(max(
                faceCleanupMask,
                max(conditionRoleMask, max(blushEraseMask, eggWhiteBody))));
            half conditionMask = conditionBodyMask * step(0.001h, _ConditionStrength);
            paletteColor = lerp(paletteColor, conditionedBody, conditionMask);

            fixed4 finalColor = fixed4(paletteColor, source.a) * _Color;

            half selfIllumination = recolorMask * _PaletteEmission;
            output.Albedo = finalColor.rgb * (1.0h - selfIllumination);
            output.Emission = finalColor.rgb * selfIllumination;
            output.Metallic = _Metallic;
            output.Smoothness = _Glossiness;
            output.Alpha = finalColor.a;
        }
        ENDCG
    }

    FallBack "Standard"
}
