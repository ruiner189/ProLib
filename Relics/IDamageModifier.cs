using Battle.Attacks;
using Relics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProLib.Relics
{
    public interface IDamageModifier
    {
        public float GetDamageModifier(Attack attack, RelicManager relicManager, int critCount, float damage);
    }
}
