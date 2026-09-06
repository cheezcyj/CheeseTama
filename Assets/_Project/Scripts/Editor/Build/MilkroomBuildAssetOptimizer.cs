using System;
using UnityEditor;
using UnityEngine;

namespace CheeseTama.Editor
{
    public static class MilkroomBuildAssetOptimizer
    {
        private static readonly string[] TopBarIconPaths =
        {
            "Assets/_Project/Resources/UI/TopBarIcons/coin.png",
            "Assets/_Project/Resources/UI/TopBarIcons/milkdrop.png",
            "Assets/_Project/Resources/UI/TopBarIcons/collectionpuzzle.png"
        };

        [MenuItem("CheeseTama/빌드 자산 최적화 적용")]
        public static void OptimizeMilkroomBuildAssets()
        {
            ApplyOptimization(true);
        }

        public static void ApplyOptimization(bool logCompletion)
        {
            // The original model pipeline owns each stable mesh and material asset.
            // Repeated optimization must not create a second mesh copy or an old source folder.
            OriginalMilkroomPropAssets.ConfigureAllAssets();

            for (var index = 0; index < TopBarIconPaths.Length; index += 1)
            {
                ConfigureTexture(TopBarIconPaths[index], 256, true);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (logCompletion)
            {
                Debug.Log(
                    "새 밀크룸 소품 7종의 빌드 자산 설정을 적용하고 "
                    + "기존 상단바 아이콘 3종을 최대 256 크기로 설정했습니다.");
            }
        }

        private static void ConfigureTexture(string path, int maxSize, bool isUi)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                throw new InvalidOperationException($"Texture importer not found: {path}");
            }

            var changed = importer.maxTextureSize != maxSize
                || importer.textureCompression != TextureImporterCompression.CompressedHQ
                || !importer.crunchedCompression
                || importer.compressionQuality != 80
                || importer.mipmapEnabled != !isUi
                || (isUi && (importer.textureType != TextureImporterType.Sprite
                    || !importer.alphaIsTransparency));
            if (!changed)
            {
                return;
            }

            importer.maxTextureSize = maxSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.crunchedCompression = true;
            importer.compressionQuality = 80;
            importer.mipmapEnabled = !isUi;
            if (isUi)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.alphaIsTransparency = true;
            }

            importer.SaveAndReimport();
        }
    }
}
