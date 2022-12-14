using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using ProLib.Managers;
using HarmonyLib;
using System;
using TMPro;
using UI;
using UnityEngine;
using ProLib.Attributes;
using I2.Loc;
using System.IO;
using ProLib.Relics;
using Relics;
using ProLib.Extensions;

namespace ProLib
{
    [BepInPlugin(GUID, Name, Version)]
    public class Plugin : BaseUnityPlugin
    {
        public const String GUID = "com.ruiner.prolib";
        public const String Name = "ProLib";
        public const String Version = "1.3.0";

        private Harmony _harmony;
        public static ManualLogSource Log;
        public static ConfigFile ConfigFile;

        public static GameObject LibManager;
        public static GameObject PrefabHolder;

        private static ConfigEntry<bool> _allItemsUnlocked;
        public static bool AllItemsUnlocked => _allItemsUnlocked.Value;

        private void Awake()
        {
            Log = Logger;
            ConfigFile = Config;
            LoadConfig();
            CreateDirectory();

            _harmony = new Harmony(GUID);
            _harmony.PatchAll();

            LibManager = new GameObject("ProLib");
            LibManager.AddComponent<SceneInfoManager>();
            LibManager.AddComponent<LanguageManager>();
            LibManager.AddComponent<CustomRelicManager>();
            LibManager.AddComponent<OrbManager>();
            LibManager.AddComponent<PrefabManager>();

            PrefabHolder = new GameObject("ProLibPrefabs");
            PrefabHolder.transform.SetParent(LibManager.transform);
            PrefabHolder.SetActive(false);
            
            DontDestroyOnLoad(LibManager);
            LibManager.HideAndDontSave(true);
        }

        [Register]
        public static void Register()
        {
            LanguageManager.RegisterLocalization += new LanguageManager.LocalizationRegistration(RegisterLocalization);
            CustomRelicManager.Register += new CustomRelicManager.RelicRegister(RelicRegister);
        }

        private static void LoadConfig()
        {
            _allItemsUnlocked = ConfigFile.Bind<bool>("CustomStart", "AllItemsUnlocked", false, "If Enabled, all items are unlocked in Custom Start");
        }

        private static void RegisterLocalization(LanguageManager loader)
        {
            loader.LoadGoogleSheetTSVSource("https://docs.google.com/spreadsheets/d/e/2PACX-1vRe82XVSt8LOUz3XewvAHT5eDDzAqXr5MV0lt3gwvfN_2n9Zxj613jllVPtdPdQweAap2yOSJSgwpPt/pub?gid=1410350919&single=true&output=tsv", "Prolib_Translations.tsv");
            loader.AddLocalizationParam("MOD_AMOUNT", Chainloader.PluginInfos.Count.ToString());
            loader.AddLocalizationParam("PEGLIN_VERSION", Application.version);
        }

        private static void RelicRegister(CustomRelicManager manager)
        {
            Relic trophy = manager.RelicManager.consolationPrize;
            manager.RelicManager.consolationPrize = 
                new CustomRelicBuilder()
                .AlwaysUnlocked(false)
                .IncludeInCustomLoadout(false)
                .SetName(trophy.locKey)
                .SetSprite(trophy.sprite)
                .SetRarity(RelicRarity.UNAVAILABLE)
                .Build();
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

