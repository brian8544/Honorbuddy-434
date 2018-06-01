using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace HighVoltz.AutoAngler
{
	static partial class Coroutines
	{
		private readonly static WaitTimer LureRecastTimer = WaitTimer.TenSeconds;

		private const int AncientPandarenFishingCharmItemId = 85973;

		private const int AncientPandarenFishingCharmAuraId = 125167;

		private static readonly Dictionary<uint, string> Lures = new Dictionary<uint, string>
																{
																	{118391, "Worm Supreme"},
																	{68049, "Heat-Treated Spinning Lure"},
																	{62673, "Feathered Lure"},
																	{34861, "Sharpened Fish Hook"},
																	{46006, "Glow Worm"},
																	{6533, "Aquadynamic Fish Attractor"},
																	{7307, "Flesh Eating Worm"},
																	{6532, "Bright Baubles"},
																	{6530, "Nightcrawlers"},
																	{6811, "Aquadynamic Fish Lens"},
																	{6529, "Shiny Bauble"},
																	{67404, "Glass Fishing Bobber"},
																};

		// does nothing if no lures are in bag
		public async static Task<bool> Applylure()
		{
			if (StyxWoW.Me.IsCasting || IsLureOnPole)
				return false;

			if (!LureRecastTimer.IsFinished)
				return false;
			
			LureRecastTimer.Reset();
			var mainHand = StyxWoW.Me.Inventory.Equipped.MainHand;

			if (mainHand == null || mainHand.ItemInfo.WeaponClass != WoWItemWeaponClass.FishingPole)
				return false;

			// Ancient Pandaren Fishing Charm
			WoWItem ancientPandarenFishingCharm = StyxWoW.Me.BagItems
				.FirstOrDefault(r => r.Entry == AncientPandarenFishingCharmItemId);

			if (ancientPandarenFishingCharm != null && !StyxWoW.Me.HasAura(AncientPandarenFishingCharmAuraId))
			{
				AutoAnglerBot.Log("Appling Ancient Pandaren Fishing Charm lure");
				ancientPandarenFishingCharm.Use();
				await CommonCoroutines.SleepForLagDuration();
				return true;
			}

			// Fishing Hats
			WoWItem head = StyxWoW.Me.Inventory.Equipped.Head;

            if (head != null && Utility.FishingHatIds.Any(hat => hat == head.Entry && hat != 118393)) // Checking for Draenor tentacle hat
			{
				AutoAnglerBot.Log("Appling Fishing Hat lure to fishing pole");
				head.Use();
				await CommonCoroutines.SleepForLagDuration();
				return true;
			}

			foreach (var kv in Lures)
			{
				WoWItem lureInBag = Utility.GetItemInBag(kv.Key);
				if (lureInBag != null && lureInBag.Use())
				{
					AutoAnglerBot.Log("Appling {0} to fishing pole", kv.Value);
					await CommonCoroutines.SleepForLagDuration();
					return true;
				}
			}
			return false;
		}

		internal static IEnumerable<WoWItem> GetLures()
		{
			return StyxWoW.Me.BagItems.Where(
				i => i.Entry == AncientPandarenFishingCharmItemId
					|| Utility.FishingHatIds.Contains(i.Entry)
					|| Lures.ContainsKey(i.Entry));
		}

		public static bool IsLureOnPole
		{
			get
			{
				bool useHatLure = false;

				var head = StyxWoW.Me.Inventory.GetItemBySlot((uint)WoWEquipSlot.Head);
				if (head != null && Utility.FishingHatIds.Contains(head.Entry))
					useHatLure = true;

				var ancientPandarenFishingCharm = StyxWoW.Me.BagItems.FirstOrDefault(r => r.Entry == AncientPandarenFishingCharmItemId);
				if (AutoAnglerSettings.Instance.Poolfishing && ancientPandarenFishingCharm != null && !StyxWoW.Me.HasAura(AncientPandarenFishingCharmAuraId))
				{
					return false;
				}

				//if poolfishing, dont need lure say we have one
				if (AutoAnglerSettings.Instance.Poolfishing && !useHatLure && !AutoAnglerBot.Instance.Profile.FishAtHotspot)
					return true;

				var ret = Lua.GetReturnValues("return GetWeaponEnchantInfo()");
				return ret != null && ret.Count > 0 && ret[0] == "1";
			}
		}

	}
}
