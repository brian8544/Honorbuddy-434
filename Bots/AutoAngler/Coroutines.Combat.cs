using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bots.Grind;
using Buddy.Coroutines;
using Styx;
using Styx.Common;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.CommonBot.POI;
using Styx.CommonBot.Profiles;
using Styx.Helpers;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Action = System.Action;

namespace HighVoltz.AutoAngler
{
	partial class Coroutines
	{

		private static Composite _combatBehavior;
		static Composite CombatBehavior
		{
			get { return _combatBehavior ?? (_combatBehavior = LevelBot.CreateCombatBehavior()); }
		}

		public static async Task<bool> HandleCombat()
		{
			if (!Me.IsFlying && Me.IsActuallyInCombat)
			{
				var mainHand = Me.Inventory.Equipped.MainHand;
				if ((mainHand == null || mainHand.Entry != AutoAnglerSettings.Instance.MainHand)
					&& Utility.EquipWeapons())
				{
					return true;
				}
					
				if (await CombatBehavior.ExecuteCoroutine())
					return true;
			}

			if (BotPoi.Current.Type == PoiType.Kill)
			{
				var unit = BotPoi.Current.AsObject as WoWUnit;
				if (unit == null)
					BotPoi.Clear("Target not found");
				else if (unit.IsDead)
					BotPoi.Clear("Target is dead");
			}

			return false;
		}
	}
}
