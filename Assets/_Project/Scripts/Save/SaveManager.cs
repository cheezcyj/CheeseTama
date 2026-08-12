using System;
using System.IO;
using CheeseTama.Utilities;
using UnityEngine;

namespace CheeseTama.Save
{
    public sealed class SaveManager : MonoBehaviour
    {
        [SerializeField] private string saveFileName = "cheesetama_save.json";

        public string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);
        public bool HasSaveFile => File.Exists(SaveFilePath);

        public CheeseTamaSaveData LoadOrCreate()
        {
            if (!File.Exists(SaveFilePath))
            {
                var created = CreateDefaultSave();
                Save(created);
                return created;
            }

            var json = File.ReadAllText(SaveFilePath);
            var loaded = JsonUtility.FromJson<CheeseTamaSaveData>(json) ?? CreateDefaultSave();
            loaded.EnsureRuntimeDefaults();
            return loaded;
        }

        public void Save(CheeseTamaSaveData saveData)
        {
            if (saveData == null)
            {
                return;
            }

            saveData.EnsureRuntimeDefaults();
            saveData.cheeseTama.lastSavedAtIso = TimeUtility.NowIso();
            var json = JsonUtility.ToJson(saveData, true);
            Directory.CreateDirectory(Path.GetDirectoryName(SaveFilePath));
            File.WriteAllText(SaveFilePath, json);
        }

        public bool DeleteSave()
        {
            if (!File.Exists(SaveFilePath))
            {
                return false;
            }

            File.Delete(SaveFilePath);
            return true;
        }

        public static CheeseTamaSaveData CreateDefaultSave()
        {
            var now = DateTimeOffset.Now.ToString("O");
            var save = new CheeseTamaSaveData();
            save.EnsureRuntimeDefaults();
            save.cheeseTama.createdAtIso = now;
            save.cheeseTama.lastSavedAtIso = now;
            return save;
        }
    }
}
