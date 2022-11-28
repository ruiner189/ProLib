using HarmonyLib;
using PeglinUI.MainMenu;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ProLib.Orbs
{
    public abstract class CustomOrb : ModifiedOrb
    {
        public static List<CustomOrb> AllCustomOrbs = new List<CustomOrb>();
        protected readonly Dictionary<int, GameObject> Prefabs = new Dictionary<int, GameObject>();

        public CustomOrb(String orbName) : base(orbName)
        {
            if (IsEnabled())
            {
                AllCustomOrbs.Add(this);
                CreatePrefabs();
            }
        }

        public GameObject this[int i]
        {
            get { return Prefabs[i]; }
            set { Prefabs[i] = value; }
        }

        public static CustomOrb GetCustomOrbByName(String name)
        {
            return AllCustomOrbs.Find(orb => orb.GetName().ToLower() == name.ToLower());
        }

        public virtual GameObject GetPrefab(int level)
        {
            return this[level];
        }

        public abstract void CreatePrefabs();
    }

    [HarmonyPatch(typeof(MainMenuRandomOrbDrop), nameof(MainMenuRandomOrbDrop.FirePachinkoBall))]
    public static class FixMenuDrop
    {
        public static void Prefix(PachinkoBall pBall)
        {
            pBall.gameObject.SetActive(true);
        }
    }

    [HarmonyPatch(typeof(PachinkoBall), nameof(PachinkoBall.SetTrajectorySimulationRadius))]
    public static class FixTrajectorySimulation
    {
        public static void Prefix(PachinkoBall __instance)
        {
            if (__instance._trajectorySimulation == null)
                __instance._trajectorySimulation = __instance.GetComponent<TrajectorySimulation>();
        }
    }

    [HarmonyPatch(typeof(Resources))]
    public static class LoadOrbs
    {
        [HarmonyTargetMethod]
        public static MethodBase CalculateMethod()
        {
            List<MethodInfo> methods = AccessTools.GetDeclaredMethods(typeof(Resources));
            foreach (MethodInfo method in methods)
            {
                if (method.Name == "Load")
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 2 && parameters[1].ParameterType == typeof(Type))
                    {
                        return method;
                    }
                }
            }
            return null;
        }

        public static bool Prefix(ref String path, ref UnityEngine.Object __result)
        {
            if (path.StartsWith("Prefabs/Orbs/"))
            {
                String str = path.Remove(0, 13);
                String[] name = str.Split(new string[] { "-Lvl" }, 2, StringSplitOptions.RemoveEmptyEntries);

                CustomOrb customOrb = CustomOrb.GetCustomOrbByName(name[0]);
                if (customOrb != null)
                {
                    try
                    {
                        GameObject gameObject = customOrb[Int32.Parse(name[1])];
                        if (gameObject != null)
                        {
                            __result = gameObject;
                            return false;
                        }
                    }
                    catch (Exception) { }
                    Plugin.Log.LogWarning($"Found custom orb but could not find level {name[1]}!");
                }

            }
            else if (path.StartsWith("$Prefabs/Orbs/"))
            {
                // Used to load vanilla orbs that we modify. This is to prevent a loop.
                path = path.Remove(0, 1);
            }
            return true;
        }
    }

}
