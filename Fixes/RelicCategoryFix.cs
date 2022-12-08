using HarmonyLib;
using PeglinUI.LoadoutManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProLib.Fixes
{
	[HarmonyPatch(typeof(ImageCarousel), nameof(ImageCarousel.PrecalculatePages))]
    public static class RelicCategoryFix
    {
		public static bool Prefix(ImageCarousel __instance, bool __runOriginal, int lastPage, int lastIndex, int listSize)
		{
            if (__runOriginal)
            {
				if (listSize > __instance.objectsPerPage && lastPage == -1)
				{
					int remainingItems = listSize;
					int currentPage = lastPage;
					int currentIndex = 0;
					while (remainingItems > 0)
					{
						int amountToAdd = (remainingItems >= __instance.objectsPerPage) ? __instance.objectsPerPage : remainingItems;
						__instance._precalculatedPageHigherBounds.Add(++currentPage, currentIndex + amountToAdd);
						currentIndex += amountToAdd;
						remainingItems -= __instance.objectsPerPage;
					}
					return false;
				}
			}
			return true;
		}
	}
}
