using ProLib.Loaders;
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
                MethodInfo onSceneLoaded = type.GetMethod("OnSceneLoaded", new Type[] {typeof(String), typeof(bool)});
                MethodInfo lateOnSceneLoaded = type.GetMethod("LateOnSceneLoaded", new Type[] {typeof(String), typeof(bool)});
                MethodInfo onSceneUnloaded = type.GetMethod("OnSceneUnloaded", new Type[] {typeof(String)});
                MethodInfo lateOnSceneUnloaded = type.GetMethod("LateOnSceneUnloaded", new Type[] {typeof(String)});

                if(onSceneLoaded != null && onSceneLoaded.IsStatic)
                {
                    SceneLoader.OnSceneLoaded += (SceneLoader.SceneLoad)Delegate.CreateDelegate(typeof(SceneLoader.SceneLoad), onSceneLoaded);
                }

                if (lateOnSceneLoaded != null && lateOnSceneLoaded.IsStatic)
                {
                    SceneLoader.LateOnSceneLoaded += (SceneLoader.SceneLoad)Delegate.CreateDelegate(typeof(SceneLoader.SceneLoad), lateOnSceneLoaded);
                }

                if (onSceneUnloaded != null && onSceneUnloaded.IsStatic)
                {
                    SceneLoader.OnSceneUnloaded += (SceneLoader.SceneUnload)Delegate.CreateDelegate(typeof(SceneLoader.SceneUnload), onSceneUnloaded);
                }

                if (lateOnSceneUnloaded != null  && lateOnSceneUnloaded.IsStatic)
                {
                    SceneLoader.LateOnSceneUnloaded += (SceneLoader.SceneUnload)Delegate.CreateDelegate(typeof(SceneLoader.SceneUnload), lateOnSceneUnloaded);
                }
                _registered = true;
            }
        }
    }
}
