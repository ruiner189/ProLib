using ProLib.Utility;
using I2.Loc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Reflection;

namespace ProLib.Loaders
{
    public class LanguageLoader : MonoBehaviour, ILocalizationParamsManager
    {
        public static LanguageLoader Instance { get; private set; }

        public delegate void LocalizationRegistration(LanguageLoader loader);
        public static LocalizationRegistration RegisterLocalization = delegate (LanguageLoader loader) { };

        private readonly Dictionary<string, string> _parameters = new Dictionary<string, string>();
        private readonly List<Func<string, string>> _dynamicParameters = new List<Func<string, string>>();

        public static LanguageSourceData LanguageSource { get; private set; }

        public void Awake()
        {
            if(Instance == null) Instance = this;
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

        private void LoadResourceSources()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.IsDynamic)
                    foreach (String path in assembly.GetManifestResourceNames())
                    {
                        if (path.Contains(".Resources.Localization.") && path.EndsWith(".tsv")) { 
                            Plugin.Log.LogDebug($"Loading: {path}");
                            LoadResourceTSV(assembly, path);
                        }
                    }
            }
        }

        public void Start()
        {
            LoadResourceSources();
            RegisterLocalization(this);
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
            foreach(string line in text.Split('\n'))
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

        public string GetParameterValue(string Param)
        {
            if (_parameters.ContainsKey(Param)){
                return _parameters[Param];
            } 
            foreach(Func<string, string> function in _dynamicParameters)
            {
                string result = function.Invoke(Param);
                if (result != null) return result;
            }
            return null;
        }
    }
}
