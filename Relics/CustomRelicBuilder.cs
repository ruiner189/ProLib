using Relics;
using System;
using UnityEngine;

namespace ProLib.Relics
{
    public class CustomRelicBuilder
    {

        private String _name;
        private String _altDesc;
        private String _id;
        private Sprite _sprite;
        private int _countdown;
        private int _uses;
        private RelicEffect _effect = RelicEffect.NONE;
        private RelicRarity _rarity = RelicRarity.NONE;
        private bool _isEnabled = true;
        private Type _relicIconType = typeof(RelicIcon);
        private bool _alwaysUnlocked = false;
        private bool _includeInCustomLoadout = true;

        public CustomRelicBuilder SetName(String name)
        {
            _name = name;
            return this;
        }

        public CustomRelicBuilder SetAlternativeDescription(String altDesc)
        {
            _altDesc = altDesc;
            return this;
        }

        public CustomRelicBuilder SetSprite(Sprite sprite)
        {
            _sprite = sprite;
            return this;
        }

        public CustomRelicBuilder SetRelicEffect(int id)
        {
            _effect = (RelicEffect)id;
            return this;
        }

        public CustomRelicBuilder SetRarity(RelicRarity rarity)
        {
            _rarity = rarity;
            return this;
        }

        public CustomRelicBuilder SetId(String id)
        {
            _id = id;
            return this;
        }

        public CustomRelicBuilder SetCountdown(int countdown)
        {
            _countdown = countdown;
            return this;
        }

        public CustomRelicBuilder SetUses(int uses)
        {
            _uses = uses;
            return this;
        }

        public CustomRelicBuilder SetEnabled(bool enabled)
        {
            _isEnabled = enabled;
            return this;
        }

        public CustomRelicBuilder IncludeInCustomLoadout(bool include)
        {
            _includeInCustomLoadout = include;
            return this;
        }

        public CustomRelicBuilder AlwaysUnlocked(bool alwaysUnlocked)
        {
            _alwaysUnlocked = alwaysUnlocked;
            return this;
        }

        public CustomRelicBuilder SetRelicIcon(Type relicIconType)
        {
            if (typeof(RelicIcon).IsAssignableFrom(relicIconType))
                _relicIconType = relicIconType;
            return this;
        }

        public CustomRelic Build()
        {
            return Build<CustomRelic>();
        }

        public T Build<T>() where T : CustomRelic
        {
            T relic = ScriptableObject.CreateInstance<T>();
            relic.name = _name;
            relic.Id = _id ?? _name;
            relic.locKey = _name;
            relic.sprite = _sprite;
            relic.effect = _effect;
            relic.globalRarity = _rarity;
            relic.IsEnabled = _isEnabled;
            relic.CustomRelicIconType = _relicIconType;
            relic.descMod = _altDesc;
            relic.IncludeInCustomLoadout = _includeInCustomLoadout;
            relic.AlwaysUnlocked = _alwaysUnlocked;

            if (_countdown > 0)
                CustomRelicManager.AddCountdown(relic, _countdown);

            if (_uses > 0)
                CustomRelicManager.AddUses(relic, _uses);

            Plugin.Log.LogDebug($"{relic.locKey} successfully registered.");
            return relic;
        }
    }
}
