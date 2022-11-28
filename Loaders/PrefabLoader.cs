using Data;
using Data.Scenarios;
using HarmonyLib;
using Map;
using ProLib.Attributes;
using ProLib.Extensions;
using ProLib.UI;
using RNG.Scenarios;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Worldmap;

namespace ProLib.Loaders
{

    public class PrefabLoader : MonoBehaviour
    {
        /* A temporary boolean to deactivate this feature. We don't want to add a 15 second loading screen if we're not using it*/
        public static readonly bool IsActive = false;

        private bool _complete = false;
        public static PrefabLoader Instance;

        private Dictionary<String, GameObject> ScenarioPrefabs = new Dictionary<string, GameObject>();
        private Dictionary<String, GameObject> BattlePrefabs = new Dictionary<string, GameObject>();

        private GameObject ScenarioPrefabHolder;
        private GameObject BattlePrefabHolder;
        private GameObject LoadingScreen;

        public bool IsComplete => !IsActive || _complete;

        public void Awake()
        {
            if (Instance == null) Instance = this;
            if (this != Instance)
            {
                Destroy(this);
                return;
            }

            ScenarioPrefabHolder = new GameObject("ScenarioPrefabHolder");
            ScenarioPrefabHolder.transform.SetParent(transform);
            ScenarioPrefabHolder.SetActive(false);

            BattlePrefabHolder = new GameObject("BattlePrefabHolder");
            BattlePrefabHolder.transform.SetParent(transform);
            BattlePrefabHolder.SetActive(false);

            gameObject.HideAndDontSave();

            if (IsActive)
                CreateLoadingScreen();

        }

        public void Start()
        {
            if (IsActive)
                Instance.StartCoroutine(Instance.GrabPrefabs(SceneLoader.ForestMap, SceneLoader.CastleMap, SceneLoader.MinesMap));
        }

        public void CreateLoadingScreen()
        {
            LoadingScreen = new GameObject("Loading Screen");
            LoadingScreen.AddComponent<LoadingScreen>();
            LoadingScreen.transform.SetParent(transform);
            LoadingScreen.HideAndDontSave();
        }

        private IEnumerator GrabPrefabs(params String[] scenes)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            foreach (String scene in scenes)
            {
                var task = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
                while (!task.isDone || MapController.instance == null)
                {
                    yield return null;
                }
                Grab(scene);

                StaticGameData.dataToLoad = null;
                StaticGameData.currentNode = null;
                if (MapController.instance != null)
                {
                    UnityEngine.Object.Destroy(MapController.instance.gameObject);
                }
            }

            _complete = true;

            var task2 = SceneManager.LoadSceneAsync(SceneLoader.MainMenu, LoadSceneMode.Single);
            while (!task2.isDone)
            {
                yield return null;
            }
            yield return new WaitForEndOfFrame();

            LoadingScreen.SetActive(false);
            stopWatch.Stop();

            Plugin.Log.LogMessage($"Grabbed all default prefabs. Took {stopWatch.ElapsedMilliseconds} ms");
        }

        private void Grab(String sceneName)
        {
            MapController controller = MapController.instance;
            if (controller != null)
            {
                foreach (GameObject frame in controller.scenarioPegboardFrame)
                    CopyGameObject(ScenarioPrefabHolder, ScenarioPrefabs, frame);

                foreach (MapDataScenario scenario in controller._potentialRandomScenarios)
                    CopyScenarioData(scenario);

                ScenarioPrefabHolder.HideAndDontSave();

                foreach (GameObject frame in controller.battlePegboardFrame)
                    CopyGameObject(BattlePrefabHolder, BattlePrefabs, frame);

                foreach (MapDataBattle battle in controller._potentialEasyBattles.Union(controller._potentialEliteBattles).Union(controller._potentialRandomBattles))
                    CopyBattleData(battle);

                BattlePrefabHolder.HideAndDontSave();
            }
        }

        private void CopyScenarioData(MapDataScenario scenario)
        {
            CopyGameObject(ScenarioPrefabHolder, ScenarioPrefabs, scenario.assignedBackground);
            CopyGameObject(ScenarioPrefabHolder, ScenarioPrefabs, scenario.assignedPegboardFrame);
            CopyGameObject(ScenarioPrefabHolder, ScenarioPrefabs, scenario.background);
            CopyGameObject(ScenarioPrefabHolder, ScenarioPrefabs, scenario.backgroundOverride);

            foreach (GameObject doodad in scenario.scenarioDoodads)
                CopyGameObject(ScenarioPrefabHolder, ScenarioPrefabs, doodad);
        }

        private void CopyBattleData(MapDataBattle battle)
        {
            CopyGameObject(BattlePrefabHolder, BattlePrefabs, battle.assignedBackground);
            CopyGameObject(BattlePrefabHolder, BattlePrefabs, battle.backgroundOverride);
            CopyGameObject(BattlePrefabHolder, BattlePrefabs, battle.assignedPegboardFrame);
            CopyGameObject(BattlePrefabHolder, BattlePrefabs, battle.pegLayout.PegLayoutPrefab);

            foreach (GameObject doodad in battle.battleDoodads)
                CopyGameObject(BattlePrefabHolder, BattlePrefabs, doodad);

            foreach (StarterSpawn starterSpawn in battle.starterSpawns)
                CopyGameObject(BattlePrefabHolder, BattlePrefabs, starterSpawn.spawnData.enemyPrefab);

            foreach (WaveGroup group in battle.waveGroups)
                foreach (WaveData data in group.waveData)
                    CopyGameObject(BattlePrefabHolder, BattlePrefabs, data.spawnData.enemyPrefab);
        }

        private void CopyGameObject(GameObject container, Dictionary<String, GameObject> dictionary, GameObject original)
        {
            if (original == null) return;
            if (dictionary.ContainsKey(original.name)) return;

            Plugin.Log.LogDebug("Grabbing Prefab " + original.name);

            GameObject clone = GameObject.Instantiate(original, container.transform);
            dictionary.Add(original.name, clone);
        }

        [HarmonyPatch(typeof(MainMenuInit), nameof(MainMenuInit.Start))]
        public static class PreventMainMenu
        {
            public static bool Prefix()
            {
                return Instance.IsComplete;
            }
        }

        [HarmonyPatch(typeof(MapController), nameof(MapController.Awake))]
        public static class PreventMapControllerAwake
        {
            public static bool Prefix(MapController __instance)
            {
                if (Instance.IsComplete) return true;
                MapController.instance = __instance;
                return false;
            }
        }

        [HarmonyPatch(typeof(MapController), nameof(MapController.Start))]
        public static class PreventMapControllerStart
        {
            public static bool Prefix(MapController __instance)
            {
                return Instance.IsComplete;
            }
        }


        [HarmonyPatch(typeof(PauseMenu), nameof(PauseMenu.Awake))]
        public static class HidePauseMenu
        {
            public static bool Prefix(PauseMenu __instance)
            {
                if (Instance.IsComplete) return true;
                __instance.gameObject.SetActive(false);
                return false;
            }
        }

        [HarmonyPatch(typeof(BGMController), nameof(BGMController.SetMapAudio))]
        public static class MuteMapMusic
        {
            public static bool Prefix()
            {
                return Instance.IsComplete;
            }
        }

    }
}
