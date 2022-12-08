using ProLib.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ProLib.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SceneModifierAttribute : Attribute
    {
        private static bool _registered = false;
        public static void RegisterSceneObjects()
        {
            if (_registered) return;

            IEnumerable<Type> types = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes()
                .Where(t => t.IsDefined(typeof(SceneModifierAttribute))));

            foreach (Type type in types)
            {
                MethodInfo onSceneLoaded = type.GetMethod("OnSceneLoaded", new Type[] { typeof(String), typeof(bool) });
                MethodInfo lateOnSceneLoaded = type.GetMethod("LateOnSceneLoaded", new Type[] { typeof(String), typeof(bool) });
                MethodInfo onSceneUnloaded = type.GetMethod("OnSceneUnloaded", new Type[] { typeof(String) });
                MethodInfo lateOnSceneUnloaded = type.GetMethod("LateOnSceneUnloaded", new Type[] { typeof(String) });

                if (onSceneLoaded != null && onSceneLoaded.IsStatic)
                {
                    SceneInfoManager.OnSceneLoaded += (SceneInfoManager.SceneLoad)Delegate.CreateDelegate(typeof(SceneInfoManager.SceneLoad), onSceneLoaded);
                }

                if (lateOnSceneLoaded != null && lateOnSceneLoaded.IsStatic)
                {
                    SceneInfoManager.LateOnSceneLoaded += (SceneInfoManager.SceneLoad)Delegate.CreateDelegate(typeof(SceneInfoManager.SceneLoad), lateOnSceneLoaded);
                }

                if (onSceneUnloaded != null && onSceneUnloaded.IsStatic)
                {
                    SceneInfoManager.OnSceneUnloaded += (SceneInfoManager.SceneUnload)Delegate.CreateDelegate(typeof(SceneInfoManager.SceneUnload), onSceneUnloaded);
                }

                if (lateOnSceneUnloaded != null && lateOnSceneUnloaded.IsStatic)
                {
                    SceneInfoManager.LateOnSceneUnloaded += (SceneInfoManager.SceneUnload)Delegate.CreateDelegate(typeof(SceneInfoManager.SceneUnload), lateOnSceneUnloaded);
                }
                _registered = true;
            }
        }
    }
}
