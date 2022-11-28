using System;
using System.IO;
using UnityEngine;

namespace ProLib.Utility
{
    public class SheetSource
    {
        public readonly String Url;
        public readonly String LocalizationFile;

        public String PreviousTSVText;
        public String CurrentTSVText;

        public SheetSource(String url, String file)
        {
            Url = url;
            LocalizationFile = file;

            if (LocalizationFile != null)
                LoadLocalFile();
        }

        public String GetTSV()
        {
            if (CurrentTSVText != null)
                return CurrentTSVText;
            return PreviousTSVText;
        }

        public bool ShouldUpdateLocal()
        {
            if (LocalizationFile != null && CurrentTSVText != null && !CurrentTSVText.Equals(PreviousTSVText))
                return true;
            return false;
        }

        public void LoadLocalFile()
        {
            if (LocalizationFile != null)
            {
                String path = GetTranslationFilePath(LocalizationFile);
                if (File.Exists(path))
                {
                    PreviousTSVText = File.ReadAllText(path);
                }
            }
        }

        public void SaveTranslationFile()
        {
            if (ShouldUpdateLocal())
            {
                String path = GetTranslationFilePath(LocalizationFile);
                String directory = Path.GetDirectoryName(path);
                Directory.CreateDirectory(directory);
                File.WriteAllText(path, CurrentTSVText);
            }
        }

        private String GetTranslationFilePath(String path)
        {
            return Path.Combine(Application.persistentDataPath, "ProLib", "Localization", path);
        }
    }
}
