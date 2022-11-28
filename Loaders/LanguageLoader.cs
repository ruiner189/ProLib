using ProLib.Utility;
using I2.Loc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Reflection;
using TMPro;
using PeglinUI.OrbDisplay;
using System.Linq;
using HarmonyLib;
using System.Text.RegularExpressions;
using UnityEngine.TextCore;

namespace ProLib.Loaders
{
    public class LanguageLoader : MonoBehaviour, ILocalizationParamsManager
    {
        public static LanguageLoader Instance { get; private set; }

        public delegate void LocalizationRegistration(LanguageLoader loader);
        public static LocalizationRegistration RegisterLocalization = delegate (LanguageLoader loader) { };

        private readonly Dictionary<string, string> _parameters = new Dictionary<string, string>();
        private readonly List<Func<string, string>> _dynamicParameters = new List<Func<string, string>>();
        private readonly List<KeywordDescriptionPair> _keywordDescriptionPairs = new List<KeywordDescriptionPair>();

        public static LanguageSourceData LanguageSource { get; private set; }

        public void Awake()
        {
            if (Instance == null) Instance = this;
            if (Instance != this)
            {
                Destroy(this);
                return;
            }

            LanguageSource = new LanguageSourceData();
            LanguageSource.AddLanguage("English", "en");
            LanguageSource.AddLanguage("Français", "fr");
            LanguageSource.AddLanguage("Español", "es");
            LanguageSource.AddLanguage("Deutsch", "de");
            LanguageSource.AddLanguage("Nederlands", "nl");
            LanguageSource.AddLanguage("Italiano", "it");
            LanguageSource.AddLanguage("Português do Brasil", "pt-BR");
            LanguageSource.AddLanguage("Русский", "ru");
            LanguageSource.AddLanguage("简体中文", "zh-CN");
            LanguageSource.AddLanguage("繁体中文", "zh-TW");
            LanguageSource.AddLanguage("日本語", "ja");
            LanguageSource.AddLanguage("한국어", "ko");
            LanguageSource.AddLanguage("Svenska", "sv");
            LanguageSource.AddLanguage("Polski", "pl");
            LanguageSource.AddLanguage("Türkçe", "tr");
            LocalizationManager.AddSource(LanguageSource);
        }

        public void Start()
        {
            LoadResourceSources();
            RegisterLocalization(this);
        }

        private void LoadResourceSources()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.IsDynamic)
                    foreach (String path in assembly.GetManifestResourceNames())
                    {
                        if (path.Contains(".Resources.Localization.") && path.EndsWith(".tsv"))
                        {
                            Plugin.Log.LogDebug($"Loading: {path}");
                            LoadResourceTSV(assembly, path);
                        }
                    }
            }
        }

        public void OnEnable()
        {
            if (!LocalizationManager.ParamManagers.Contains(this))
            {
                LocalizationManager.ParamManagers.Add(this);

                LocalizationManager.LocalizeAll(true);
            }
        }

        public void OnDisable()
        {
            LocalizationManager.ParamManagers.Remove(this);
        }

        public void AddLocalizationParam(string param, string result)
        {
            _parameters[param] = result;
        }

        public void AddDynamicLocalizationParam(Func<string, string> function)
        {
            _dynamicParameters.Add(function);
        }

        public void LoadTSVSource(string content)
        {
            List<string[]> terms = TranslateTSVFile(content);
            LoadSource(terms);
        }

        public void LoadResourceTSV(Assembly assembly, String path)
        {
            try
            {
                Stream stream = assembly.GetManifestResourceStream(path);
                StreamReader reader = new StreamReader(stream);
                LoadTSVSource(reader.ReadToEnd());
                reader.Dispose();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e.StackTrace);
            }
        }

        public void LoadLocalizationSource(Localization localization)
        {
            RegisterTerm(localization);
        }

        public void LoadSource(List<string[]> terms)
        {
            foreach (string[] term in terms)
                RegisterTerm(term);
        }

        public void LoadGoogleSheetTSVSource(string url, string localFile = null)
        {
            SheetSource source = new SheetSource(url, localFile);
            StartCoroutine(LoadSheetSource(source));
        }

        private IEnumerator LoadSheetSource(SheetSource source)
        {
            if (source.PreviousTSVText != null)
            {
                LoadTSVSource(source.PreviousTSVText);
            }

            UnityWebRequest www = UnityWebRequest.Get(source.Url);
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError)
            {
                Plugin.Log.LogWarning($"Could not connect to localization sheet {source.Url}");
            }
            else
            {
                source.CurrentTSVText = www.downloadHandler.text;
            }
            www.Dispose();

            if (source.CurrentTSVText != null && !source.CurrentTSVText.Equals(source.PreviousTSVText))
            {
                LoadTSVSource(source.CurrentTSVText);
                source.SaveTranslationFile();
            }
        }

        public List<string[]> TranslateTSVFile(string text)
        {
            List<string[]> results = new List<string[]>();
            foreach (string line in text.Split('\n'))
            {
                string[] split = line.Split('\t');
                results.Add(split);
            }
            return results;
        }

        public void RegisterTerm(string[] term)
        {
            string key = term[0];
            if (key == "Keys") return;
            string[] values = new string[term.Length - 1];
            for (int i = 1; i < term.Length; i++)
            {
                if (term[i] != "")
                    values[i - 1] = term[i];
                else
                    values[i - 1] = null;
            }

            LanguageSource.AddTerm(key).Languages = values;
        }

        public void RegisterTerm(Localization localization)
        {
            RegisterTerm(localization.AsTerm());
        }

        /// <summary>
        /// Registers a new style for <see cref="TMPro.TMProUGUI"/>
        /// </summary>
        /// <param name="style"></param>
        public void RegisterStyle(TMP_Style style)
        {
            TMP_StyleSheet styleSheet = TMP_Settings.defaultStyleSheet;
            styleSheet.styles.Add(style);
        }

        /// <summary>
        /// Registers a new style for TMProUGUI. <paramref name="opening"/> and <paramref name="closing"/> must follow html formats.
        /// </summary>
        /// <param name="styleName"></param>
        /// <param name="opening"></param>
        /// <param name="closing"></param>
        public void RegisterStyle(String styleName, String opening, String closing)
        {
            RegisterStyle(new TMP_Style(styleName, opening, closing));
        }

        /// <summary>
        /// Adds a keyword used to add a tooltip to the side of an orb or relic. Keywords are identified from the style name in the raw text.
        /// </summary>
        /// <param name="pair"></param>
        public void RegisterTooltipKeyword(KeywordDescriptionPair pair)
        {
            _keywordDescriptionPairs.Add(pair);
        }

        /// <summary>
        /// Adds a keyword used to add a tooltip to the side of an orb or relic. Keywords are identified from the style name in the raw text.
        /// </summary>
        /// <param name="keyword"></param>
        /// <param name="reference"></param>
        public void RegisterTooltipKeyword(string keyword, string reference)
        {
            KeywordDescriptionPair pair = new KeywordDescriptionPair();
            pair.Keyword = keyword;
            pair.DescriptionLoc = reference;

            RegisterTooltipKeyword(pair);
        }


        /// <summary>
        /// Adds a TMP_SpriteAsset for sprites used in <see cref="TMPro.TMProUGUI"/>
        /// </summary>
        /// <param name="asset"></param>
        public void AddSpriteAsset(TMP_SpriteAsset asset)
        {
            // We want the properties of the default SpriteAsset material, due to missing properties such as _CullMode causing errors in the console.
            TMP_SpriteAsset def = TMP_Settings.defaultSpriteAsset;
            Texture texture = asset.material.mainTexture;

            asset.material.CopyPropertiesFromMaterial(def.material);
            asset.material.mainTexture = texture;

            TMP_Settings.defaultSpriteAsset.fallbackSpriteAssets.Add(asset);
        }

        public string GetParameterValue(string Param)
        {
            if (_parameters.ContainsKey(Param))
            {
                return _parameters[Param];
            }
            foreach (Func<string, string> function in _dynamicParameters)
            {
                string result = function.Invoke(Param);
                if (result != null) return result;
            }
            return null;
        }

        [HarmonyPatch(typeof(TooltipKeywordDescriptions), nameof(TooltipKeywordDescriptions.UpdateString))]
        private static class CustomSecondaryTerms
        {
            private static void Postfix(TooltipKeywordDescriptions __instance, String englishDesc, ref int __result)
            {
                Regex regex = new Regex(__instance.keywordRegexPattern, RegexOptions.IgnoreCase);
                List<string> list = new List<string>();
                Match match = regex.Match(englishDesc);
                while (match.Success)
                {
                    Group group = match.Groups[1];
                    foreach (KeywordDescriptionPair keywordDescriptionPair in Instance._keywordDescriptionPairs)
                    {
                        if (group.Captures[0].ToString() == keywordDescriptionPair.Keyword && !list.Contains(keywordDescriptionPair.DescriptionLoc))
                        {
                            list.Add(keywordDescriptionPair.DescriptionLoc);
                        }
                    }
                    match = match.NextMatch();
                }
                bool flag = __instance.GetComponentInParent<Canvas>().renderMode != RenderMode.WorldSpace;
                foreach (string str in list)
                {
                    (flag ? UnityEngine.Object.Instantiate<GameObject>(__instance._keywordDescriptionPrefabScreenSpace, __instance.transform) : UnityEngine.Object.Instantiate<GameObject>(__instance._keywordDescriptionPrefab, __instance.transform)).GetComponentInChildren<Localize>().Term = "Statuses/" + str;
                }
                __result += list.Count;
            }
        }
    }
}
