//!CompilerOption:Optimize:On

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Media;
using System.Xml.Linq;
using CommonBehaviors.Actions;
using Styx;
using Styx.Common;
using Styx.Common.Helpers;
using Styx.CommonBot;
using Styx.CommonBot.Profiles;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;

namespace HighVoltz.AutoAngler
{
    public enum PathingType
    {
        Circle,
        Bounce
    }

    public class AutoAnglerBot : BotBase
    {
        private readonly List<uint> _poolsToFish = new List<uint>();

		private PathingType _pathingType = PathingType.Circle;
	    private string _prevProfilePath;
	    private static WaitTimer _loadProfileTimer = new WaitTimer(TimeSpan.FromSeconds(1));
        private static DateTime _botStartTime;

        internal static readonly Version Version = new Version(2, new Svn().Revision);

        public AutoAnglerBot()
        {
            Instance = this;
            BotEvents.Profile.OnNewOuterProfileLoaded += Profile_OnNewOuterProfileLoaded;
            Styx.CommonBot.Profiles.Profile.OnUnknownProfileElement += Profile_OnUnknownProfileElement;
        }

		internal bool LootFrameIsOpen { get; private set; }
	
		internal bool ShouldFaceWaterNow { get;  set; }

		internal Dictionary<string, uint> FishCaught { get; private set; }
		
		internal AutoAnglerProfile Profile { get; private set; }
        internal static AutoAnglerBot Instance { get; private set; }

        #region overrides

        private readonly InventoryType[] _2HWeaponTypes =
        {
            InventoryType.TwoHandWeapon,
            InventoryType.Ranged,
        };

        private Composite _root;

        public override string Name
        {
            get { return "AutoAngler"; }
        }

        public override PulseFlags PulseFlags
        {
            get { return PulseFlags.All & (~PulseFlags.CharacterManager); }
        }

        public override Composite Root
        {
            get { return _root ?? (_root = new ActionRunCoroutine(ctx => Coroutines.RootLogic())); }
        }

        public override bool IsPrimaryType
        {
            get { return true; }
        }

        public override Form ConfigurationForm
        {
            get { return new MainForm(); }
        }

        public override void Pulse() {}

        public override void Initialize()
        {
            try
            {
				WoWItem mainhand = (AutoAnglerSettings.Instance.MainHand != 0
					? StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == AutoAnglerSettings.Instance.MainHand) 
					: null) ?? FindMainHand();

				WoWItem offhand = AutoAnglerSettings.Instance.OffHand != 0 
					? StyxWoW.Me.BagItems.FirstOrDefault(i => i.Entry == AutoAnglerSettings.Instance.OffHand) 
					: null;

                if ((mainhand == null || !_2HWeaponTypes.Contains(mainhand.ItemInfo.InventoryType)) && offhand == null)
                    offhand = FindOffhand();

				if (mainhand != null)
                    Log("Using {0} for mainhand weapon", mainhand.Name);

                if (offhand != null)
                    Log("Using {0} for offhand weapon", offhand.Name);

	            
				_prevProfilePath = ProfileManager.XmlLocation;

	            if (AutoAnglerSettings.Instance.Poolfishing && File.Exists(AutoAnglerSettings.Instance.LastLoadedProfile))
			            ProfileManager.LoadNew(AutoAnglerSettings.Instance.LastLoadedProfile);
	            else
		            ProfileManager.LoadEmpty();
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }
        }

        public override void Start()
        {
	        DumpConfiguration();
            _botStartTime = DateTime.Now;
            FishCaught = new Dictionary<string, uint>();
	        LootTargeting.Instance.IncludeTargetsFilter += LootFilters.IncludeTargetsFilter;
            Lua.Events.AttachEvent("LOOT_OPENED", LootFrameOpenedHandler);
            Lua.Events.AttachEvent("LOOT_CLOSED", LootFrameClosedHandler);
			Lua.Events.AttachEvent("UNIT_SPELLCAST_FAILED", UnitSpellCastFailedHandler);

	        Coroutines.OnStart();
        }


        public override void Stop()
        {
			Coroutines.OnStop();

            Log("In {0} days, {1} hours and {2} minutes we have caught",
                (DateTime.Now - _botStartTime).Days,
                (DateTime.Now - _botStartTime).Hours,
                (DateTime.Now - _botStartTime).Minutes);

            foreach (var kv in FishCaught)
            {
                Log("{0} x{1}", kv.Key, kv.Value);
            }

			LootTargeting.Instance.IncludeTargetsFilter -= LootFilters.IncludeTargetsFilter;
            Lua.Events.DetachEvent("LOOT_OPENED", LootFrameOpenedHandler);
            Lua.Events.DetachEvent("LOOT_CLOSED", LootFrameClosedHandler);
			Lua.Events.DetachEvent("UNIT_SPELLCAST_FAILED", UnitSpellCastFailedHandler);

			if (!string.IsNullOrEmpty(_prevProfilePath) && File.Exists(_prevProfilePath))
				ProfileManager.LoadNew(_prevProfilePath);
        }

        #endregion

        #region Handlers

        private void LootFrameClosedHandler(object sender, LuaEventArgs args)
        {
            LootFrameIsOpen = false;
        }

        private void LootFrameOpenedHandler(object sender, LuaEventArgs args)
        {
            LootFrameIsOpen = true;
        }

		private void UnitSpellCastFailedHandler(object sender, LuaEventArgs args)
		{
			var spell = GetWoWSpellFromSpellCastFailedArgs(args);
			if (spell != null && spell.IsValid && spell.Name == "Fishing")
				ShouldFaceWaterNow = true;	
		}



        #endregion

        #region Profile

        private void Profile_OnNewOuterProfileLoaded(BotEvents.Profile.NewProfileLoadedEventArgs args)
        {
            try
            {
				Profile = new AutoAnglerProfile(args.NewProfile, _pathingType, _poolsToFish);
	            if (!string.IsNullOrEmpty(ProfileManager.XmlLocation))
	            {
		            AutoAnglerSettings.Instance.LastLoadedProfile = ProfileManager.XmlLocation;
					AutoAnglerSettings.Instance.Save();
	            }
            }
            catch (Exception ex)
            {
                Logging.WriteException(ex);
            }
        }

        public void Profile_OnUnknownProfileElement(object sender, UnknownProfileElementEventArgs e)
        {
			// hackish way to set variables to default states before loading new profile... wtb OnNewOuterProfileLoading event
			if (_loadProfileTimer.IsFinished)
			{
				_poolsToFish.Clear();
				_pathingType = PathingType.Circle;
				_loadProfileTimer.Reset();
			}

            if (e.Element.Name == "FishingSchool")
            {
                XAttribute entryAttrib = e.Element.Attribute("Entry");
                if (entryAttrib != null)
                {
                    uint entry;
                    UInt32.TryParse(entryAttrib.Value, out entry);
					if (!_poolsToFish.Contains(entry))
                    {
						_poolsToFish.Add(entry);
                        XAttribute nameAttrib = e.Element.Attribute("Name");
                        if (nameAttrib != null)
                            Log( "Adding Pool Entry: {0} to the list of pools to fish from", nameAttrib.Value);
                        else
                            Log("Adding Pool Entry: {0} to the list of pools to fish from", entry);
                    }
                }
                else
                {
                    Err(
                        "<FishingSchool> tag must have the 'Entry' Attribute, e.g <FishingSchool Entry=\"202780\"/>\nAlso supports 'Name' attribute but only used for display purposes");
                }
                e.Handled = true;
            }
            else if (e.Element.Name == "Pathing")
            {
                XAttribute typeAttrib = e.Element.Attribute("Type");
                if (typeAttrib != null)
                {
                    _pathingType = (PathingType)
                        Enum.Parse(typeof (PathingType), typeAttrib.Value, true);
                    
					Log("Setting Pathing Type to {0} Mode", _pathingType);
                }
                else
                {
                    Err(
                        "<Pathing> tag must have the 'Type' Attribute, e.g <Pathing Type=\"Circle\"/>");
                }
                e.Handled = true;
            }
        }

        #endregion

	    WoWSpell GetWoWSpellFromSpellCastFailedArgs(LuaEventArgs args)
	    {
		    if (args.Args.Length < 5)
			    return null;
			return WoWSpell.FromId((int)((double)args.Args[4]));
	    }

        private WoWItem FindMainHand()
        {
			WoWItem mainHand = StyxWoW.Me.Inventory.Equipped.MainHand;
	        if (mainHand == null || mainHand.ItemInfo.WeaponClass == WoWItemWeaponClass.FishingPole)
	        {
		        mainHand = StyxWoW.Me.CarriedItems.OrderByDescending(u => u.ItemInfo.Level).
			        FirstOrDefault(
				        i => i.IsSoulbound && (i.ItemInfo.InventoryType == InventoryType.WeaponMainHand ||
												i.ItemInfo.InventoryType == InventoryType.TwoHandWeapon) &&
							StyxWoW.Me.CanEquipItem(i));

		        if (mainHand != null)
			        AutoAnglerSettings.Instance.MainHand = mainHand.Entry;
		        else
			        Err("Unable to find a mainhand weapon to swap to when in combat");
	        }
	        else
	        {
		        AutoAnglerSettings.Instance.MainHand = mainHand.Entry;
	        }
			AutoAnglerSettings.Instance.Save();
            return mainHand;
        }

        // scans bags for offhand weapon if mainhand isn't 2h and none are equipped and uses the highest ilvl one
        private WoWItem FindOffhand()
        {
			WoWItem offHand = StyxWoW.Me.Inventory.Equipped.OffHand;
	        if (offHand == null)
	        {
		        offHand = StyxWoW.Me.CarriedItems.OrderByDescending(u => u.ItemInfo.Level).
			        FirstOrDefault(
				        i => i.IsSoulbound && (i.ItemInfo.InventoryType == InventoryType.WeaponOffHand ||
												i.ItemInfo.InventoryType == InventoryType.Weapon ||
												i.ItemInfo.InventoryType == InventoryType.Shield) &&
							AutoAnglerSettings.Instance.MainHand != i.Entry &&
							StyxWoW.Me.CanEquipItem(i));

		        if (offHand != null)
			        AutoAnglerSettings.Instance.OffHand = offHand.Entry;
		        else
			        Err("Unable to find an offhand weapon to swap to when in combat");
	        }
	        else
	        {
		        AutoAnglerSettings.Instance.OffHand = offHand.Entry;
	        }
			AutoAnglerSettings.Instance.Save();
            return offHand;
        }

        internal static void Log(string format, params object[] args)
        {
            Logging.Write(Colors.DodgerBlue, String.Format("AutoAngler {0}: {1}", Version, format), args);
        }

        internal static void Err(string format, params object[] args)
        {
            Logging.Write(Colors.Red, String.Format("AutoAngler {0}: {1}", Version, format), args);
        }

        internal static void Debug(string format, params object[] args)
        {
            Logging.WriteDiagnostic(Colors.DodgerBlue, String.Format("AutoAngler {0}: {1}", Version, format), args);
        }

	    private void DumpConfiguration()
	    {
		    Debug("AvoidLava: {0}", AutoAnglerSettings.Instance.AvoidLava);
			Debug("Fly: {0}", AutoAnglerSettings.Instance.Fly);
			Debug("LootNPCs: {0}", AutoAnglerSettings.Instance.LootNPCs);
			
			Debug("Hat Id: {0}", AutoAnglerSettings.Instance.Hat);
			Debug("MainHand Id: {0}", AutoAnglerSettings.Instance.MainHand);
			Debug("OffHand Id: {0}", AutoAnglerSettings.Instance.OffHand);

			Debug("MaxFailedCasts: {0}", AutoAnglerSettings.Instance.MaxFailedCasts);
            Debug("MaxTimeAtPool: {0}", AutoAnglerSettings.Instance.MaxTimeAtPool);
            Debug("NinjaNodes: {0}", AutoAnglerSettings.Instance.NinjaNodes);
			Debug("PathPrecision: {0}", AutoAnglerSettings.Instance.PathPrecision);
			Debug("Poolfishing: {0}", AutoAnglerSettings.Instance.Poolfishing);
			Debug("TraceStep: {0}", AutoAnglerSettings.Instance.TraceStep);
			Debug("UseWaterWalking: {0}", AutoAnglerSettings.Instance.UseWaterWalking);

            Debug("RandomGarrisonBaits: {0}", AutoAnglerSettings.Instance.RandomGarrisonBaits);
            Debug("JawlessSkulkerBait: {0}", AutoAnglerSettings.Instance.JawlessSkulkerBait);
            Debug("FatSleeperBait: {0}", AutoAnglerSettings.Instance.FatSleeperBait);
            Debug("BlindLakeSturgeonBait: {0}", AutoAnglerSettings.Instance.BlindLakeSturgeonBait);
            Debug("FireAmmoniteBait: {0}", AutoAnglerSettings.Instance.FireAmmoniteBait);
            Debug("SeaScorpionBait: {0}", AutoAnglerSettings.Instance.SeaScorpionBait);
            Debug("AbyssalGulperEelBaits: {0}", AutoAnglerSettings.Instance.AbyssalGulperEelBaits);
            Debug("BlindLakeSturgeonBait: {0}", AutoAnglerSettings.Instance.BlindLakeSturgeonBait);
	    }
    }
}