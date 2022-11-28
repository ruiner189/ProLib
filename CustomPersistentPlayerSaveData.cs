using HarmonyLib;
using ProLib.Relics;
using Saving;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolBox.Serialization;
using UnityEngine;

namespace ProLib
{
    public class CustomPersistentPlayerSaveData : SaveObjectData
    {
        public const String KEY = "ProLib.PersistantPlayerSaveData";
        public override string Name => KEY;

        [SerializeField]
        private readonly String[] _unlockedRelics;

        public String[] UnlockedRelics => _unlockedRelics;

        public CustomPersistentPlayerSaveData(String[] unlockedRelics) : base(false) {
            _unlockedRelics = unlockedRelics;
        }

        [HarmonyPatch(typeof(PersistentPlayerData), nameof(PersistentPlayerData.InitFromSaveFile))]
        public static class LoadData
        {
            public static void Postfix()
            {
                CustomRelicManager.UnlockedRelics.Clear();
                if (DataSerializer.Load<SaveObjectData>(KEY) is CustomPersistentPlayerSaveData data)
                {
                    if(data.UnlockedRelics != null)
                        foreach (String relicName in data.UnlockedRelics)
                            CustomRelicManager.UnlockedRelics.Add(relicName);
                }
            }
        }

        [HarmonyPatch(typeof(PersistentPlayerData), nameof(PersistentPlayerData.SavePersistentData))]
        public static class SaveData
        {
            public static void Postfix()
            {
                new CustomPersistentPlayerSaveData(CustomRelicManager.UnlockedRelics.ToArray()).Save();
            }
        }

    }
}
