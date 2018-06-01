using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using Bots.Grind;
using Buddy.Coroutines;

using Styx;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.Frames;
using Styx.CommonBot.POI;
using Styx.CommonBot.Routines;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

namespace HighVoltz.AutoAngler
{
    partial class Coroutines
	{
		private static Composite _deathBehavior;
		private static Composite _lootBehavior;
		private static DateTime _pulseTimestamp;
		static readonly WaitTimer AntiAfkTimer = new WaitTimer(TimeSpan.FromMinutes(2));
		private static readonly WaitTimer LootTimer = WaitTimer.FiveSeconds;

		static LocalPlayer Me { get { return StyxWoW.Me; }}

		static Composite LootBehavior
		{
			get { return _lootBehavior ?? (_lootBehavior = LevelBot.CreateLootBehavior()); }
		}

		static Composite DeathBehavior
		{
			get { return _deathBehavior ?? (_deathBehavior = LevelBot.CreateDeathBehavior()); }
		}


	    internal static void OnStart()
	    {
		    Gear_OnStart();
	    }

		internal static void OnStop()
		{
			Gear_OnStop();
		}

		public async static Task<bool> RootLogic()
		{
			CheckPulseTime();
			AnitAfk();
			// Is bot dead? if so, release and run back to corpse
			if (await DeathBehavior.ExecuteCoroutine())
				return true;

			if (await HandleCombat())
			{
				// reset the autoBlacklist timer 
				MoveToPoolTimer.Reset();
				return true;
			}

			if (await HandleVendoring())
				return true;

			if (!StyxWoW.Me.IsAlive || StyxWoW.Me.Combat || RoutineManager.Current.NeedRest)
				return false;

			if (BotPoi.Current.Type == PoiType.None && LootTargeting.Instance.FirstObject != null)
				SetLootPoi(LootTargeting.Instance.FirstObject);

			// Fishing Logic

			if (await DoFishing())
				return true;

			var poiGameObject = BotPoi.Current.AsObject as WoWGameObject;

			// only loot when POI is not set to a fishing pool.
			if (!StyxWoW.Me.IsFlying 
				&& (BotPoi.Current.Type != PoiType.Harvest 
				|| (poiGameObject != null && poiGameObject.SubType != WoWGameObjectType.FishingHole))
				&& await LootBehavior.ExecuteCoroutine())
			{
				return true;
			}

			return await FollowPath();
		}

	    private static WoWPoint _lastMoveTo;
	    private static readonly WaitTimer MoveToLogTimer = WaitTimer.OneSecond;

	    public async static Task<bool> MoveTo(WoWPoint destination, string destinationName = null)
	    {
		    if (destination.DistanceSqr(_lastMoveTo) > 5*5)
		    {
			    if (MoveToLogTimer.IsFinished)
			    {
				    if (string.IsNullOrEmpty(destinationName))
					    destinationName = destination.ToString();
					AutoAnglerBot.Log("Moving to {0}", destinationName);
					MoveToLogTimer.Reset();
			    }
			    _lastMoveTo = destination;
		    }
		    var moveResult = Navigator.MoveTo(destination);
		    return moveResult != MoveResult.Failed && moveResult != MoveResult.PathGenerationFailed;
	    }

		public async static Task<bool> FlyTo(WoWPoint destination, string destinationName = null)
		{
			if (destination.DistanceSqr(_lastMoveTo) > 5 * 5)
			{
				if (MoveToLogTimer.IsFinished)
				{
					if (string.IsNullOrEmpty(destinationName))
						destinationName = destination.ToString();
					AutoAnglerBot.Log("Flying to {0}", destinationName);
					MoveToLogTimer.Reset();
				}
				_lastMoveTo = destination;
			}
			Flightor.MoveTo(destination);
			return true;
		}

		public async static Task<bool> Logout()
	    {
		    var activeMover = WoWMovement.ActiveMover;
		    if (activeMover == null)
			    return false;

		    var hearthStone =
			    Me.BagItems.FirstOrDefault(
				    h => h != null && h.IsValid && h.Entry == 6948 
						&& h.CooldownTimeLeft == TimeSpan.FromMilliseconds(0));
		    if (hearthStone == null)
		    {
			    AutoAnglerBot.Log("Unable to find a hearthstone");
				return false;
		    }

		    if (activeMover.IsMoving)
		    {
			    WoWMovement.MoveStop();
			    if (!await Coroutine.Wait(4000, () => !activeMover.IsMoving))
				    return false;
		    }

			hearthStone.UseContainerItem();
		    if (await Coroutine.Wait(15000, () => Me.Combat))
			    return false;

			AutoAnglerBot.Log("Logging out");
			Lua.DoString("Logout()");
			TreeRoot.Stop();
		    return true;
	    }

		static private void AnitAfk()
		{
			// keep the bot from going afk.
			if (AntiAfkTimer.IsFinished)
			{
				StyxWoW.ResetAfk();
				AntiAfkTimer.Reset();
			}
		}

		static void CheckPulseTime()
		{
			if (_pulseTimestamp == DateTime.MinValue)
			{
				_pulseTimestamp = DateTime.Now;
				return;
			} 

			var pulseTime = DateTime.Now - _pulseTimestamp;
			if (pulseTime >= TimeSpan.FromSeconds(3))
			{
				AutoAnglerBot.Err(
					"Warning: It took {0} seconds to pulse.\nThis can cause missed bites. To fix try disabling all plugins",
					pulseTime.TotalSeconds);
			}
			_pulseTimestamp = DateTime.Now;
		}

		static void SetLootPoi(WoWObject lootObj)
		{
			if (BotPoi.Current.Type != PoiType.None || lootObj == null || !lootObj.IsValid)
				return;

			if (lootObj is WoWGameObject)
			{
				BotPoi.Current = new BotPoi(lootObj, PoiType.Harvest);
			}
			else
			{
				var unit = lootObj as WoWUnit;
				if (unit != null)
				{
					if (unit.CanLoot)
						BotPoi.Current = new BotPoi(lootObj, PoiType.Loot);
					else if (unit.CanSkin)
						BotPoi.Current = new BotPoi(lootObj, PoiType.Skin);
				}
			}
		}

		private static async Task<bool> CheckLootFrame()
		{
			if (!LootTimer.IsFinished)
			{
				// loot everything.
				if (AutoAnglerBot.Instance.LootFrameIsOpen)
				{
					for (int i = 0; i < LootFrame.Instance.LootItems; i++)
					{
						LootSlotInfo lootInfo = LootFrame.Instance.LootInfo(i);
						if (AutoAnglerBot.Instance.FishCaught.ContainsKey(lootInfo.LootName))
							AutoAnglerBot.Instance.FishCaught[lootInfo.LootName] += (uint)lootInfo.LootQuantity;
						else
							AutoAnglerBot.Instance.FishCaught.Add(lootInfo.LootName, (uint)lootInfo.LootQuantity);
					}
					LootFrame.Instance.LootAll();
					LootTimer.Stop();
					await CommonCoroutines.SleepForLagDuration();
				}
				return true;
			}
			return false;
		}
	}
}
