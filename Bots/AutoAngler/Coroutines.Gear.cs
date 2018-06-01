using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Styx;
using Styx.CommonBot.Coroutines;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace HighVoltz.AutoAngler
{
	static partial class Coroutines
	{
		private static bool _checkedForFishingHat;

		static void Gear_OnStart()
		{
			_checkedForFishingHat = false;
		}

		static void Gear_OnStop()
		{
			if (Utility.EquipWeapons())
				AutoAnglerBot.Log("Equipping weapons");

			if (Utility.EquipMainHat())
				AutoAnglerBot.Log("Switched to my normal hat");
		}

	    static WoWItem SpecialPole()
	    {
	        return Me.BagItems.FirstOrDefault(i => i.Entry == 118381);
	    }

	    static bool canUpgrade(WoWItem mainHand)
	    {
	        return mainHand.Entry != 118381 && Me.BagItems.Any(i => i.Entry == 118381);
	    }
		public async static Task<bool> EquipPole()
		{
			var mainHand = StyxWoW.Me.Inventory.Equipped.MainHand;
			// equip fishing pole if there's none equipped
			if (mainHand != null && mainHand.ItemInfo.WeaponClass == WoWItemWeaponClass.FishingPole && !canUpgrade(mainHand))
				return false;
		    WoWItem pole;
            if (canUpgrade(mainHand))
		    {
		        pole = SpecialPole();
		    }
		    else
		    {

		        pole = Me.BagItems
		            .Where(i => i != null && i.IsValid
		                        && i.ItemInfo.WeaponClass == WoWItemWeaponClass.FishingPole)
		            .OrderByDescending(i => i.ItemInfo.Level)
		            .FirstOrDefault();
		    }
		    if (pole == null)
				return false;

			return await EquipItem(pole, WoWInventorySlot.MainHand);
		}

		public async static Task<bool> EquipHat()
		{
			if (_checkedForFishingHat || Me.Combat)
				return false;

			var hat = Utility.GetFishingHat();

			if (hat != null && StyxWoW.Me.Inventory.Equipped.Head != hat)
			{
				if (!Utility.EquipItem(hat, WoWInventorySlot.Head) ||
					!await Coroutine.Wait(4000, () => StyxWoW.Me.Inventory.Equipped.Head == hat))
				{
					return false;
				}
			}
			_checkedForFishingHat = true;
			return true;
		}

		public async static Task<bool> EquipItem(WoWItem item, WoWInventorySlot slot)
		{
			if (!Utility.EquipItem(item, slot))
				return false;
			await CommonCoroutines.SleepForLagDuration();
			if (!await Coroutine.Wait(4000, () => !item.IsDisabled))
				return false;
			return true;
		}
	}
}
