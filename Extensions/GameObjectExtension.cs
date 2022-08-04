using System;
using UnityEngine;

namespace ProLib.Extensions
{
    public static class GameObjectExtension
    {
        public static void HideAndDontSave(this GameObject gameObject, bool includeChildren = true)
        {
            if (includeChildren)
            {

                foreach (Transform child in gameObject.GetComponentsInChildren<Transform>(true))
                {
                    child.gameObject.hideFlags = HideFlags.HideAndDontSave;
                }
            }
            gameObject.hideFlags = HideFlags.HideAndDontSave;
        }

        public static GameObject FindChild(this GameObject gameObject, params String[] names)
        {
            Transform transform = gameObject.transform;
            foreach (String name in names)
            {
                transform = transform.Find(name);
                if (transform == null) return null;
            }
            return transform.gameObject;
        }
    }
}
