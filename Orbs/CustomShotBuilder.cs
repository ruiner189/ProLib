using ProLib.Extensions;
using ProLib.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProLib.Orbs
{
    public class CustomShotBuilder
    {
        public Sprite Sprite;
        public GameObject Prefab;
        public String Name;

        public CustomShotBuilder()
        {
            Prefab = OrbManager.Instance.ShotPrefab;
        }

        public CustomShotBuilder SetSprite(Sprite sprite)
        {
            Sprite = sprite;
            return this;
        }

        public CustomShotBuilder SetName(String name)
        {
            Name = name;
            return this;
        }

        public CustomShotBuilder WithPrefab(GameObject prefab)
        {
            Prefab = prefab;
            return this;
        }

        public GameObject Build()
        {
            GameObject result = GameObject.Instantiate(Prefab);
            result.name = Name;

            if (Sprite != null)
            {
                result.GetComponent<SpriteRenderer>().sprite = Sprite;
            }

            result.transform.SetParent(Plugin.PrefabHolder.transform);
            result.HideAndDontSave();

            return result;

        }

    }

}
