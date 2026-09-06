using UnityEditor;
using UnityEngine;

namespace CheeseTama.Editor
{
    public static class MilkroomPropPrefabBuilder
    {
        [MenuItem("CheeseTama/밀크룸 소품 프리팹 생성")]
        public static void BuildMilkroomPropPrefabs()
        {
            OriginalMilkroomPropAssets.ApplyAll();
            MilkroomBuildAssetOptimizer.ApplyOptimization(false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("직접 제작한 밀크룸 소품 7종의 프리팹을 갱신했습니다.");
        }

        [MenuItem("CheeseTama/Build Rug Prefab")]
        public static void BuildRugPrefab()
        {
            OriginalMilkroomPropAssets.UpdatePrefab("Rug");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("직접 제작한 Rug 원본으로 프리팹을 갱신했습니다.");
        }

        [MenuItem("CheeseTama/Build Window Prefab")]
        public static void BuildWindowPrefab()
        {
            OriginalMilkroomPropAssets.UpdatePrefab("Window");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("새 Window 원본으로 프리팹을 갱신했습니다.");
        }

        [MenuItem("CheeseTama/Build Dresser Table Prefab")]
        public static void BuildDresserTablePrefab()
        {
            OriginalMilkroomPropAssets.UpdatePrefab("DresserTable");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("새 MilkCabinet 원본으로 DresserTable 프리팹을 갱신했습니다.");
        }

        [MenuItem("CheeseTama/Build Milk Shelf Prefab")]
        public static void BuildMilkShelfPrefab()
        {
            OriginalMilkroomPropAssets.UpdatePrefab("MilkShelf");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("새 MilkShelf 원본으로 프리팹을 갱신했습니다.");
        }

        [MenuItem("CheeseTama/Build Chalkboard Prefab")]
        public static void BuildChalkboardPrefab()
        {
            OriginalMilkroomPropAssets.UpdatePrefab("Chalkboard");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("새 Chalkboard 원본으로 프리팹을 갱신했습니다.");
        }

        [MenuItem("CheeseTama/Apply Natural Milkroom Materials")]
        public static void ApplyNaturalMilkroomMaterials()
        {
            OriginalMilkroomPropAssets.UpdatePrefab("CozyChair");
            OriginalMilkroomPropAssets.UpdatePrefab("Fridge");
            OriginalMilkroomPropAssets.UpdatePrefab("Chalkboard");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("새 원본 재질을 CozyChair, Fridge, Chalkboard에 적용했습니다.");
        }

        [MenuItem("CheeseTama/Apply Current Milkroom Visual Placements")]
        public static void ApplyCurrentMilkroomVisualPlacements()
        {
            OriginalMilkroomPropAssets.ApplyScenePlacements();
        }
    }
}
