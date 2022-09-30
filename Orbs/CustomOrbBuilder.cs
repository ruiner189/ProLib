using Battle.Attacks;
using I2.Loc;
using ProLib.Extensions;
using ProLib.Loaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProLib.Orbs
{
    public class CustomOrbBuilder
    {

        public String Name;
        public OrbLoader Loader;
        public GameObject Prefab;
        public GameObject NormalShotPrefab;
        public GameObject CriticalShotPrefab;
        public Dictionary<String, String> LocalParams = new Dictionary<String, String>();

        public Type AttackType;
        public int Level = 1;
        public bool Include = false;
        public String[] DescriptionKeys;
        public Sprite Sprite;

        private bool Scale = false;
        public Vector3 SpriteScale;

        public float DamagePerPeg;
        public float CritDamagePerPeg;

        public CustomOrbBuilder()
        {
            Loader = OrbLoader.Instance;
            Prefab = Loader.OrbPrefab;
            NormalShotPrefab = Loader.ShotPrefab;
            CriticalShotPrefab = Loader.ShotPrefab;
        }

        public CustomOrbBuilder(GameObject prefab) : this()
        {
            LoadFromPrefab(prefab);
        }

        public CustomOrbBuilder(String orbName) : this()
        {
            GameObject prefab = Resources.Load<GameObject>($"$Prefabs/Orbs/{orbName}");
            if (prefab != null)
                LoadFromPrefab(prefab);
        }

        private void LoadFromPrefab(GameObject prefab)
        {
            Attack attack = prefab.GetComponent<Attack>();
            GameObject sprite = prefab.transform.GetChild(0).gameObject;
            SpriteRenderer renderer = sprite.GetComponent<SpriteRenderer>();

            WithPrefab(prefab)
                .SetDescription(attack.locDescStrings)
                .SetName(attack.locNameString)
                .SetLevel(attack.Level)
                .SetDamage(attack.DamagePerPeg, attack.CritDamagePerPeg)
                .SetSprite(renderer.sprite);

            if (attack is ProjectileAttack projectile)
            {
                SetShot(projectile._shotPrefab, projectile._criticalShotPrefab);
            }
        }

        public CustomOrbBuilder WithPrefab(GameObject gameObject)
        {
            Prefab = gameObject;
            return this;
        }

        public CustomOrbBuilder WithPrefab(String orbName)
        {
            GameObject prefab = Resources.Load<GameObject>($"$Prefabs/Orbs/{orbName}");
            if (prefab != null) return WithPrefab(prefab);
            return this;
        }

        public CustomOrbBuilder SetShot(GameObject normal, GameObject crit = null)
        {
            NormalShotPrefab = normal;
            CriticalShotPrefab = crit ?? normal;
            return this;
        }

        public CustomOrbBuilder WithAttack(Type attack)
        {
            if (typeof(Attack).IsAssignableFrom(attack))
                AttackType = attack;

            return this;
        }

        public CustomOrbBuilder SetDamage(float normal, float crit)
        {
            DamagePerPeg = normal;
            CritDamagePerPeg = crit;
            return this;
        }

        public CustomOrbBuilder WithAttack<T>() where T : Attack
        {
            return WithAttack(typeof(T));
        }

        public CustomOrbBuilder SetName(String name)
        {
            Name = name;
            return this;
        }

        public CustomOrbBuilder SetLevel(int level)
        {
            Level = level;
            return this;
        }

        public CustomOrbBuilder SetSprite(Sprite sprite)
        {
            Sprite = sprite;
            return this;
        }

        public CustomOrbBuilder SetSpriteScale(Vector3 scale)
        {
            Scale = true;
            SpriteScale = scale;
            return this;
        }

        public CustomOrbBuilder SetDescription(String[] descriptionKeys)
        {
            DescriptionKeys = descriptionKeys;
            return this;
        }

        public CustomOrbBuilder AddParameter(String key, String value)
        {
            LocalParams[key] = value;
            return this;
        }

        public CustomOrbBuilder AddToDescription(String desc, int position = -1)
        {
            if (DescriptionKeys == null || DescriptionKeys.Length == 0) {
                DescriptionKeys = new String[] { desc };
                return this;
            }

            if (position == -1) position = DescriptionKeys.Length;
            bool containsDesc = false;
            foreach (String s in DescriptionKeys)
            {
                if (s == desc)
                {
                    containsDesc = true;
                    break;
                }
            }
            if (!containsDesc)
            {
                String[] newDesc = new String[DescriptionKeys.Length + 1];
                int i = 0;
                for (int j = 0; j < newDesc.Length; j++)
                {
                    if (j == position)
                        newDesc[j] = desc;
                    else
                    {
                        newDesc[j] = DescriptionKeys[i];
                        i++;
                    }

                }

                DescriptionKeys = newDesc;
            }
            return this;
        }

        public CustomOrbBuilder ReplaceDescription(String desc, int position)
        {
            if (DescriptionKeys.Length > position)
                DescriptionKeys[position] = desc;
            else
                AddToDescription(desc);

            return this;
        }



        public CustomOrbBuilder IncludeInOrbPool(bool include)
        {
            Include = include;
            return this;
        }

        public GameObject Build()
        {
            if (Prefab == null)
            {
                Plugin.Log.LogError($"Failed to create orb {Name}. Prefab is null.");
                return null;
            }

            GameObject result = GameObject.Instantiate(Prefab);

            if(AttackType != null && typeof(Attack).IsAssignableFrom(AttackType))
            {
                GameObject.DestroyImmediate(result.GetComponent<Attack>());
                result.AddComponent(AttackType);
            }

            Attack attack = result.GetComponent<Attack>();

            attack.Level = Level;

            if(Name != null)
            {
                attack.locName = Name.ToLower();
                attack.locNameString = Name.ToLower();
                attack.name = $"{Name}-Lvl{Level}";
            }

            attack.locDescStrings = DescriptionKeys;
            attack.NextLevelPrefab = null;
            attack.DamagePerPeg = DamagePerPeg;
            attack.CritDamagePerPeg = CritDamagePerPeg;

            if (attack is ProjectileAttack projectile)
            {
                if(NormalShotPrefab != null)
                    projectile._shotPrefab = NormalShotPrefab;

                if(CriticalShotPrefab != null)
                    projectile._criticalShotPrefab = CriticalShotPrefab;
            }

            GameObject sprite = result.transform.GetChild(0).gameObject;

            if (Sprite != null)
            {
                SpriteRenderer renderer = sprite.GetComponent<SpriteRenderer>();

                renderer.sprite = Sprite;
            }

            if (Scale)
                sprite.transform.localScale = SpriteScale;

            if(LocalParams.Count > 0)
            {
                if (result.GetComponent<LocalizationParamsManager>() == null) result.AddComponent<LocalizationParamsManager>();
                LocalizationParamsManager paramManager = result.GetComponent<LocalizationParamsManager>();

                foreach(KeyValuePair<String,String> pair in LocalParams)
                {
                    paramManager.SetParameterValue(pair.Key, pair.Value);
                }
            }

            result.transform.SetParent(Plugin.PrefabHolder.transform);
            result.HideAndDontSave();

            if (Include)
            {
                Loader.AddOrbToPool(result);
            }

            return result;
        }

        public CustomOrbBuilder Clone()
        {
            CustomOrbBuilder builder = new CustomOrbBuilder()
                .WithPrefab(Prefab)
                .WithAttack(AttackType)
                .SetShot(NormalShotPrefab, CriticalShotPrefab)
                .SetName(Name)
                .SetDescription(DescriptionKeys)
                .SetDamage(DamagePerPeg, CritDamagePerPeg)
                .SetSprite(Sprite)
                .IncludeInOrbPool(Include)
                .SetSpriteScale(SpriteScale);

            builder.Scale = Scale;

            return builder;
        }

        public static void JoinLevels(params GameObject[] gameObjects)
        {
            if (gameObjects.Length < 2) return;

            GameObject current = gameObjects[0];

            for(int i = 1; i < gameObjects.Length; i++)
            {
                Attack attack = current.GetComponent<Attack>();
                attack.NextLevelPrefab = gameObjects[i];
                current = gameObjects[i];
            }
        }
    }
}
