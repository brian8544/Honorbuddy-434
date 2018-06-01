using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Windows.Documents;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace HighVoltz.AutoAngler
{
	static partial class Coroutines
	{
		private readonly static WaitTimer BaitRecastTimer = WaitTimer.TenSeconds;
        struct Bait
        {
            public uint itemID;
            public uint AuraID;
            public uint zoneID;
            public string name;

            public Bait(uint i, uint a, uint z, string n)
            {
                itemID = i;
                AuraID = a;
                zoneID = z;
                name = n;
            }
        }

	    private static readonly List<uint> GarrisonsZonesID = new List<uint>
	    {
	        7078, // Lunarfall - Ally
	        7004, // Frostwall - Horde
	    };

		private static readonly List<Bait> Baits = new List<Bait>
		{
			new Bait(110274,158031,6721,"Jawless Skulker Bait"), // Gorgrond
			new Bait(110289,158034,6755,"Fat Sleeper Bait"), // Nagrand
			new Bait(110290,158035,6719,"Blind Lake Sturgeon Bait"), // Shadowmoon Valley
			new Bait(110290,158035,7083,"Blind Lake Sturgeon Bait"), // Defense of Karabor
			new Bait(110291,158036,6720,"Fire Ammonite Bait"), // Frostfrire Ridge
			new Bait(110292,158037,6941,"Sea Scorpion Bait"), // Ocean, Ashran
			new Bait(110292,158037,7332,"Sea Scorpion Bait"), // Ocean, Stormshield
			new Bait(110292,158037,7333,"Sea Scorpion Bait"), // Ocean, Warspear
			new Bait(110293,158038,6722,"Abyssal Gulper Eel Bait"), // Spires of Arak
			new Bait(110294,158039,6662,"Blackwater Whiptail Bait"), // Talador
			new Bait(110294,158039,6980,"Blackwater Whiptail Bait") // Shattrah
		};

		// does nothing if no baits are in bag
		public async static Task<bool> Applybait()
		{
			if (StyxWoW.Me.IsCasting || IsBait)
				return false;

            if (!BaitRecastTimer.IsFinished)
				return false;

            BaitRecastTimer.Reset();
			var mainHand = StyxWoW.Me.Inventory.Equipped.MainHand;

			if (mainHand == null || mainHand.ItemInfo.WeaponClass != WoWItemWeaponClass.FishingPole)
				return false;

            foreach (var baitInBag in GetBaits())
            {

                if (baitInBag != null && baitInBag.Use())
                {
                    AutoAnglerBot.Log("Applying bait: {0}", baitInBag.GetItemName());
					await CommonCoroutines.SleepForLagDuration();
					return true;
				}
			}
			return false;
		}

        //internal static void RandomizeList(IEnumerable<WoWItem> myList)
        //{
        //    List<WoWItem> list = new List<WoWItem>(myList);
        //    Random rnd = new Random(/* Eventually provide some random seed here. */);
        //    for (int i = list.Count() - 1; i > 0; --i)
        //    {
        //        int j = rnd.Next(i + 1);
        //        WoWItem tmp = list[i];
        //        list[i] = list[j];
        //        list[j] = tmp;
        //    }
        //    myList = list;
        //}

        internal static bool IsAuthorizedInGarrison(uint bait)
        {
            switch (bait)
            {
                case 110274:
                    return AutoAnglerSettings.Instance.JawlessSkulkerBait;
                case 110289:
                    return AutoAnglerSettings.Instance.FatSleeperBait;
                case 110290:
                    return AutoAnglerSettings.Instance.BlindLakeSturgeonBait;
                case 110291:
                    return AutoAnglerSettings.Instance.FireAmmoniteBait;
                case 110292:
                    return AutoAnglerSettings.Instance.SeaScorpionBait;
                case 110293:
                    return AutoAnglerSettings.Instance.AbyssalGulperEelBaits;
                case 110294:
                    return AutoAnglerSettings.Instance.BlackwaterWhiptailBait;
                default:
                    break;
            }
            return true;
        }

		internal static IEnumerable<WoWItem> GetBaits()
		{
            var baits = Baits.Where(b => b.zoneID == Me.ZoneId || (InGarrison() && IsAuthorizedInGarrison(b.itemID)));
		    var items = StyxWoW.Me.BagItems.Where(i => baits.Any(b => b.itemID == i.Entry));
		    if (InGarrison())
		    {
		            return items.Randomize();
            }
		    return items;
		}

	    internal static bool InGarrison()
        {
	        return GarrisonsZonesID.Contains(Me.ZoneId);
	    }

		public static bool IsBait
		{
			get
			{
				//if poolfishing, dont need bait say we have one
				if (AutoAnglerSettings.Instance.Poolfishing && !AutoAnglerBot.Instance.Profile.FishAtHotspot)
					return true;
                //if not in zone for which baits are for (or garrison) or don't have bait in bags then we say we are good
                var baits = Baits.Where(b => (b.zoneID == Me.ZoneId || (InGarrison() && IsAuthorizedInGarrison(b.itemID))) && IsinBags(b.itemID));
                if (!baits.Any())
                    return true;

                // return if we have a valid bait on us
                return Baits.Any(b => (b.zoneID == Me.ZoneId || (InGarrison() && IsAuthorizedInGarrison(b.itemID))) 
                    && StyxWoW.Me.Auras.Values.Any(aura => b.AuraID == aura.SpellId));
			}
		}
        public static bool IsinBags(uint itemId)
        {
            return StyxWoW.Me.BagItems.Any(item => itemId == item.Entry);
        }
	}
    /// From: http://www.codeproject.com/Tips/265752/Lets-randomize-IEnumerable
    /// <summary>
    /// Extension class for IEnumerable&lt;T&gt;
    /// </summary>
    static class IEnumerableExtension
    {
        /// <summary>
        /// Randomizes the specified collection.
        /// </summary>
        /// <typeparam name="T">The type of the collection.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <returns>The randomized collection</returns>
        public static IEnumerable<T> Randomize<T>(this IEnumerable<T> collection)
        {
            // Put all items into a list.
            var list = new List<T>(collection);
            var randomizer = new Random();
            // And pluck them out randomly.
            for (int i = list.Count; i > 0; i--)
            {
                int r = randomizer.Next(0, i);
                yield return list[r];
                list.RemoveAt(r);
            }
        }

    }
}