using I2.Loc;
using System;
using TMPro;

namespace ProLib.Utility
{
    public static class Utility
    {
        public static TMP_FontAsset GetFont(String path)
        {
            foreach (LanguageSourceData source in LocalizationManager.Sources)
            {
                foreach (UnityEngine.Object asset in source.Assets)
                {
                    if (path == asset.name && asset is TMP_FontAsset font)
                    {
                        return font;
                    }
                }
            }
            return null;
        }
    }
}
