using System;
using CheeseTama.Collections;
using CheeseTama.Data;
using CheeseTama.Gameplay;
using CheeseTama.Gameplay.Stats;
using CheeseTama.Save;
using UnityEngine;

namespace CheeseTama.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private DataRegistry dataRegistry;
        [SerializeField] private SaveManager saveManager;

        private readonly TimeProgressionSystem timeProgressionSystem = new TimeProgressionSystem();
        private readonly CollectionSystem collectionSystem = new CollectionSystem();

        public static GameManager Instance { get; private set; }
        public DataRegistry DataRegistry => dataRegistry;
        public CheeseTamaSaveData CurrentSave { get; private set; }
        public CheeseTamaModel CurrentTama => CurrentSave?.cheeseTama;
        public TimeProgressionResult LastTimeProgression { get; private set; }
        public string SaveFilePath => saveManager != null ? saveManager.SaveFilePath : string.Empty;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            dataRegistry = dataRegistry != null ? dataRegistry : GetComponent<DataRegistry>();
            saveManager = saveManager != null ? saveManager : GetComponent<SaveManager>();
            LoadOrCreateGame();
        }

        public void LoadOrCreateGame()
        {
            if (saveManager == null)
            {
                Debug.LogWarning("SaveManager is missing. Runtime save data was not loaded.");
                return;
            }

            CurrentSave = saveManager.LoadOrCreate();
            LastTimeProgression = timeProgressionSystem.ApplyOfflineProgress(CurrentTama, DateTimeOffset.Now);
            if (LastTimeProgression.applied)
            {
                saveManager.Save(CurrentSave);
            }
        }

        public void ReloadGame()
        {
            LoadOrCreateGame();
        }

        public void ResetGame()
        {
            if (saveManager == null)
            {
                CurrentSave = SaveManager.CreateDefaultSave();
                return;
            }

            saveManager.DeleteSave();
            CurrentSave = saveManager.LoadOrCreate();
            LastTimeProgression = TimeProgressionResult.None();
        }

        public void SaveGame()
        {
            if (saveManager == null || CurrentSave == null)
            {
                return;
            }

            saveManager.Save(CurrentSave);
        }

        public TimeProgressionResult ApplyTimeSkipHours(int hours)
        {
            if (CurrentTama == null || hours <= 0)
            {
                LastTimeProgression = TimeProgressionResult.None();
                return LastTimeProgression;
            }

            LastTimeProgression = timeProgressionSystem.ApplyCareTicks(CurrentTama, hours);
            SaveGame();
            return LastTimeProgression;
        }

        public void RegisterMilkDiscovery(string milkId)
        {
            if (CurrentSave == null)
            {
                return;
            }

            CurrentSave.EnsureRuntimeDefaults();
            collectionSystem.RegisterMilk(CurrentSave.collections, milkId);
            SaveGame();
        }

        public void RegisterCurrentEvolutionDiscovery()
        {
            if (CurrentSave == null || CurrentTama == null)
            {
                return;
            }

            CurrentSave.EnsureRuntimeDefaults();
            var evolutionId = string.IsNullOrWhiteSpace(CurrentTama.evolutionId)
                ? CurrentTama.form
                : CurrentTama.evolutionId;
            collectionSystem.RegisterEvolution(CurrentSave.collections, evolutionId);
            SaveGame();
        }
    }
}
