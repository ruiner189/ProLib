using Battle.Attacks;
using Relics;

namespace ProLib.Relics
{
    public interface IDamageModifier
    {
        public float GetDamageModifier(Attack attack, RelicManager relicManager, int critCount, float damage);
    }
}
