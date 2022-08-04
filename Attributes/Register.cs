using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ProLib.Attributes
{
    public class RegisterAttribute : Attribute
    {
        private static bool _registered = false;
        public static void Register()
        {
            if (_registered) return;

            IEnumerable<MethodInfo> methods = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic)
                .SelectMany(assembly => assembly.GetTypes())
                .Where(types => types.IsClass)
                .SelectMany(clazz => clazz.GetMethods())
                .Where(method => method.IsStatic)
                .Where(method => method.GetCustomAttributes(typeof(RegisterAttribute), false).FirstOrDefault() != null);

            Plugin.Log.LogMessage($"{methods.Count()} Registers found. Invoking...");

            foreach(MethodInfo method in methods)
            {
                method.Invoke(null, new Object[] { });
            }
        }
    }
}
