using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ProLib.Components
{

    /// <summary>
    /// A higher priority orb will be drawn on the top of the deck. The opposite is true with the lowest priority. 
    /// If the priority is the same, then each orb of that amount will compete, but will always be above lower priorites.
    /// </summary>
    [RequireComponent(typeof(PachinkoBall))]
    public class Priority : MonoBehaviour
    {
        public const int LOW = -10;
        public const int LOWER_THAN_NORMAL = -5;
        public const int NORMAL = 0;
        public const int HIGHER_THAN_NORMAL = 5;
        public const int HIGH = 10;

        public int Weight = 0;
    }
}
