using Battle.Attacks;
using Relics;
using System;
using System.Collections.Generic;

namespace ProLib.Relics
{
    public class CustomRelic : Relic
    {
        public string Id;
        public static List<CustomRelic> AllCustomRelics = new List<CustomRelic>();
        public bool IsEnabled = true;


        private Type _customRelicIconType = typeof(RelicIcon);
        public Type CustomRelicIconType
        {
            set
            {
                if (typeof(RelicIcon).IsAssignableFrom(_customRelicIconType))
                    _customRelicIconType = value;
            }
            get { return _customRelicIconType; }
        }
 

        public static CustomRelic GetCustomRelic(string id)
        {
            return AllCustomRelics.Find(relic => relic.Id == id);
        }

        public static bool TryGetCustomRelic(string id, out CustomRelic relic)
        {
            relic = GetCustomRelic(id);
            if (relic == null) return false;
            return true;
        }

        public CustomRelic()
        {
            AllCustomRelics.Add(this);
        }

        public virtual void OnRelicAdded(RelicManager relicManager) { }
        public virtual void OnRelicRemoved(RelicManager relicManager) { }
        public virtual void OnRelicUsed(RelicManager relicManager) { }
        public virtual void OnRelicEnabled(RelicManager relicManager) { }
        public virtual void OnRelicDisabled(RelicManager relicManager) { }
        public virtual void OnCountdownDecremented(RelicManager relicManager, int remainingCountdown) {}
        public virtual void OnArmBallForShot(BattleController battleController) { }
        public virtual int DamageModifier(Attack attack, int critCount) {  return 0; }
        public virtual int CritModifier(Attack attack, int critCount) { return 0; }
    }
}
