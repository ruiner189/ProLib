using System;

namespace ProLib.Utility
{
    public struct Localization
    {
        public readonly String Key;
        public readonly String English;
        public readonly String Français;
        public readonly String Español;
        public readonly String Deutsch;
        public readonly String Nederlands;
        public readonly String Italiano;
        public readonly String PortuguêsDoBrasil;
        public readonly String Русский;
        public readonly String 简体中文;
        public readonly String 繁体中文;
        public readonly String 日本語;
        public readonly String 한국어;
        public readonly String Svenska;
        public readonly String Polski;
        public readonly String Türkçe;

        public Localization(String key, String english, String français = null, String español = null,
            String deutsch = null, String nederlands = null, String italiano = null, String portuguêsDoBrasil = null,
            String pусский = null, String 简体中文 = null, String 繁体中文 = null, String 日本語 = null, String 한국어 = null,
            String svenska = null, String polski = null, String türkçe = null)
        {
            Key = key;
            English = english;
            Français = français;
            Español = español;
            Deutsch = deutsch;
            Nederlands = nederlands;
            Italiano = italiano;
            PortuguêsDoBrasil = portuguêsDoBrasil;
            Русский = pусский;
            this.简体中文 = 简体中文;
            this.繁体中文 = 繁体中文;
            this.日本語 = 日本語;
            this.한국어 = 한국어;
            Svenska = svenska;
            Polski = polski;
            Türkçe = türkçe;
        }

        public String[] AsTerm()
        {
            return new string[] {Key, English, Français, Español, Deutsch, Nederlands, Italiano, PortuguêsDoBrasil, Русский, 简体中文, 繁体中文, 日本語, 한국어, Svenska, Polski, Türkçe};
        }
    }
}
