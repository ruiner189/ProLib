using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using ProLib.Loaders;
using HarmonyLib;
using System;
using TMPro;
using UI;
using UnityEngine;
using ProLib.Attributes;
using I2.Loc;
using System.IO;

namespace ProLib
{
    [BepInPlugin(GUID, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {

        public const String GUID = "com.ruiner.prolib";
        public const String Name = "ProLib";
        public const String Version = "1.0.2";

        private Harmony _harmony;
        public static ManualLogSource Log;
        public static ConfigFile ConfigFile;

        public static GameObject LibManager;
        public static GameObject CorePrefabHolder;

        private void Awake()
        {
            Log = Logger;
            ConfigFile = Config;
            CreateDirectory();

            _harmony = new Harmony(GUID);
            _harmony.PatchAll();

            LibManager = new GameObject("ProLib");
            LibManager.AddComponent<SceneLoader>();
            LibManager.AddComponent<LanguageLoader>();
            LibManager.AddComponent<RelicLoader>();
            LibManager.AddComponent<OrbLoader>();
            DontDestroyOnLoad(LibManager);
            LibManager.hideFlags = HideFlags.HideAndDontSave;
        }

        [Register]
        public static void Register()
        {
            LanguageLoader.RegisterLocalization += new LanguageLoader.LocalizationRegistration(RegisterLocalization);
        }

        private static void RegisterLocalization(LanguageLoader loader)
        {
            loader.LoadGoogleSheetTSVSource("https://docs.google.com/spreadsheets/d/e/2PACX-1vRe82XVSt8LOUz3XewvAHT5eDDzAqXr5MV0lt3gwvfN_2n9Zxj613jllVPtdPdQweAap2yOSJSgwpPt/pub?gid=1410350919&single=true&output=tsv", "Prolib_Translations.tsv");
            loader.AddLocalizationParam("MOD_AMOUNT", Chainloader.PluginInfos.Count.ToString());
            loader.AddLocalizationParam("PEGLIN_VERSION", Application.version);
        }

        private void CreateDirectory()
        {
            Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "ProLib", "Localization"));
        }
    }

    [HarmonyPatch(typeof(VersionDisplay), "Start")]
    public static class ModVersionDisplay
    {
        public static void Postfix(VersionDisplay __instance)
        {
            if (__instance.GetComponent<Localize>() == null)
            {
                Localize localize = __instance.gameObject.AddComponent<Localize>();
                localize.SetTerm("Menu/ModsLoaded", "Assets/Silver SDF-lfs");
            }
            __instance.transform.position += new Vector3(0, 0.5f, 0);
            TMP_Text text = __instance.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.fontStyle = FontStyles.Bold;
            }
        }
    }

}

