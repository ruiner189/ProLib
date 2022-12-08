using ProLib.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProLib.Managers
{
    public class SceneInfoManager : MonoBehaviour
    {
        public delegate void SceneLoad(String sceneName, bool firstLoad);
        public delegate void SceneUnload(String sceneName);
        public static SceneLoad OnSceneLoaded = delegate (String sceneName, bool firstLoad) { };
        public static SceneLoad LateOnSceneLoaded = delegate (String sceneName, bool firstLoad) { };
        public static SceneUnload OnSceneUnloaded = delegate (String sceneName) { };
        public static SceneUnload LateOnSceneUnloaded = delegate (String sceneName) { };

        // Scene Names
        public const String PreMainMenu = "PreMainMenu";
        public const String MainMenu = "MainMenu";
        public const String PostMainMenu = "PostMainMenu";
        public const String ForestMap = "ForestMap";
        public const String ForestWinScene = "ForestWinScene";
        public const String CastleMap = "CastleMap";
        public const String CastleWinScene = "CastleWinScene";
        public const String MinesMap = "MinesMap";
        public const String FinalWinScene = "FinalWinScene";
        public const String Battle = "Battle";
        public const String Treasure = "Treasure";
        public const String TextScenario = "TextScenario";
        public const String PegMinigame = "PegMinigame";
        public const String ShopScenario = "ShopScenario";
        public const String RunSummary = "RunSummary";

        private readonly HashSet<String> _previouslyLoaded = new HashSet<String>();

        public static SceneInfoManager Instance;

        public void Awake()
        {
            if (Instance == null) Instance = this;
            if (this != Instance) Destroy(this);
        }

        public void Start()
        {
            RegisterAttribute.Register();
            SceneModifierAttribute.RegisterSceneObjects();
        }

        public void OnEnable()
        {
            SceneManager.sceneLoaded += SceneLoaded;
            SceneManager.sceneUnloaded += SceneUnloaded;
        }

        public void OnDisable()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneUnloaded -= SceneUnloaded;
        }
        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            bool firstLoad = !_previouslyLoaded.Contains(scene.name);
            _previouslyLoaded.Add(scene.name);
            OnSceneLoaded(scene.name, firstLoad);
            StartCoroutine(LateSceneLoaded(scene, firstLoad));
        }

        private IEnumerator LateSceneLoaded(Scene scene, bool firstLoad)
        {
            yield return new WaitForEndOfFrame();
            LateOnSceneLoaded(scene.name, firstLoad);
        }

        private void SceneUnloaded(Scene scene)
        {
            OnSceneUnloaded(scene.name);
            StartCoroutine(LateSceneUnloaded(scene));
        }

        private IEnumerator LateSceneUnloaded(Scene scene)
        {
            yield return new WaitForEndOfFrame();
            LateOnSceneUnloaded(scene.name);
        }
    }
}
