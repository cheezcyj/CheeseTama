using System;
using System.Collections.Generic;
using CheeseTama.Gameplay.Story;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CheeseTama.Editor
{
    public sealed class MilkroomMysteryContentBuildValidator : IPreprocessBuildWithReport
    {
        public const string ContentAssetPath =
            "Assets/_Project/Resources/Content/MilkroomMysteryChapters.json";

        public int callbackOrder => -900;

        [MenuItem("Tools/CheeseTama/Validate Milkroom Mystery Content")]
        public static void ValidateFromMenu()
        {
            ValidateOrThrow();
            Debug.Log("Milkroom mystery content validation passed.");
        }

        public static IReadOnlyList<string> ValidateProjectContent()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(ContentAssetPath);
            if (asset == null)
            {
                return new[] { $"미스터리 콘텐츠를 찾을 수 없습니다: {ContentAssetPath}" };
            }

            return MilkroomMysteryContentLoader.TryLoadFromJson(
                asset.text,
                out _,
                out var errors)
                ? Array.Empty<string>()
                : errors;
        }

        public static void ValidateOrThrow()
        {
            var errors = ValidateProjectContent();
            if (errors.Count == 0)
            {
                return;
            }

            var message = "Milkroom mystery content validation failed:\n- "
                + string.Join("\n- ", errors);
            throw new BuildFailedException(message);
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            ValidateOrThrow();
        }
    }
}
