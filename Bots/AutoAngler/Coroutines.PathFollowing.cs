using System.Threading.Tasks;
using Styx;
using Styx.CommonBot;
using Styx.CommonBot.Coroutines;
using Styx.CommonBot.POI;
using Styx.Pathing;

namespace HighVoltz.AutoAngler
{
	static partial class Coroutines
	{
		private async static Task<bool> FollowPath()
		{
			if (!AutoAnglerSettings.Instance.Poolfishing && !AutoAnglerBot.Instance.Profile.FishAtHotspot )
				return false;

			if (!AutoAnglerBot.Instance.Profile.FishAtHotspot 
				&& (BotPoi.Current.Type == PoiType.Harvest || LootTargeting.Instance.FirstObject != null))
				return false;

			if (await CheckLootFrame())
				return true;

			//  dks can refresh water walking while flying around.
			if (AutoAnglerSettings.Instance.UseWaterWalking 
				&& Me.Class == WoWClass.DeathKnight 
				&& !WaterWalking.IsActive
				&& await WaterWalking.Cast())
			{
				return true;
			}

			var moveto = AutoAnglerBot.Instance.Profile.CurrentPoint;

			if (moveto == WoWPoint.Zero)
				return false;

			if (AutoAnglerBot.Instance.Profile.FishAtHotspot && Navigator.AtLocation(moveto))
				return false;

			float precision = Me.IsFlying ? AutoAnglerSettings.Instance.PathPrecision : 3;

			if (Me.Location.Distance(moveto) <= precision)
				AutoAnglerBot.Instance.Profile.CycleToNextPoint();

			if (AutoAnglerSettings.Instance.Fly)
			{
				if (!StyxWoW.Me.Mounted && Flightor.MountHelper.CanMount)
				{
					var zenFlightAura = StyxWoW.Me.GetAuraByName("Zen Flight");
					if (zenFlightAura != null)
					{
						zenFlightAura.TryCancelAura();
						await CommonCoroutines.SleepForLagDuration();
					}
					Flightor.MountHelper.MountUp();
					return true;
				}

				Flightor.MoveTo(moveto);
			}
			else
			{
				if (!StyxWoW.Me.Mounted && Mount.ShouldMount(moveto) && Mount.CanMount())
					Mount.MountUp(() => moveto);
				var result = Navigator.MoveTo(moveto);
				if (result != MoveResult.Failed && result != MoveResult.PathGenerationFailed)
				{
					InactivityDetector.Reset();
				}
			}
			return true;
		}

	}
}
