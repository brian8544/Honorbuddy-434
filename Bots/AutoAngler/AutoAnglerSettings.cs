using System.ComponentModel;
using System.IO;
using Styx.Helpers;

namespace HighVoltz.AutoAngler
{
	public class AutoAnglerSettings : Settings
	{
		public static AutoAnglerSettings Instance { get; private set; }

		static AutoAnglerSettings()
		{
			Instance = new AutoAnglerSettings(Path.Combine(CharacterSettingsDirectory, "AutoAngler.xml"));
		}

		private AutoAnglerSettings(string path)
			: base(path)
		{
			Instance = this;
			Load();
		}


		[Setting, Styx.Helpers.DefaultValue(""), Browsable(false)]
		public string LastLoadedProfile { get; set; }

		[Setting, Styx.Helpers.DefaultValue(false), Description("If set to true bot will attempt to loot any dead lootable NPCs")]
		public bool LootNPCs { get; set; }

		#region Gear

		[Setting, Styx.Helpers.DefaultValue(0u),
		Description("Wowhead Id of the hat to switch to when not fishing"), Category("Gear")]
		public uint Hat { get; set; }

		[Setting, Styx.Helpers.DefaultValue(0u), Description("Wowhead Id of the mainhand weapon to switch to when in combat"),
		Category("Gear")]
		public uint MainHand { get; set; }

		[Setting, Styx.Helpers.DefaultValue(0u), Description("Wowhead Id of the offhand weapon to switch to when in combat"),
		Category("Gear")]
		public uint OffHand { get; set; }

		#endregion

		#region Fishing

		[Setting, Styx.Helpers.DefaultValue(true),
		Description("Set this to true if you want to fish from pools, otherwise set to false."), Category("Fishing")]
		public bool Poolfishing { get; set; }

		[Setting, Styx.Helpers.DefaultValue(true),
		Description("Set to true to enable flying,false to use ground based navigation"), Category("Fishing")]
		public bool Fly { get; set; }

		[Setting, Styx.Helpers.DefaultValue(true),
		Description("If set to true bot will use water walking, either use class abilities or pots"), Category("Fishing")]
		public bool UseWaterWalking { get; set; }

		[Setting, Styx.Helpers.DefaultValue(false),
		Description(
			"If set to true, bot will try to avoid landing in lava. Some pools by floating objects such as ice floes will get blacklisted if this is set to true"
			), Category("Fishing")]
		public bool AvoidLava { get; set; }

		[Setting, Styx.Helpers.DefaultValue(false), Description("If set to true bot will 'ninja' nodes from other players."),
		Category("Fishing")]
        public bool NinjaNodes { get; set; }

        #endregion

        #region Garrison

        [Setting, Styx.Helpers.DefaultValue(true),
        Description(
            "If set to false, bot will deactivate random baits in Garrison. The baits selected below will be the one used."
            ), Category("Garrison")]
        public bool RandomGarrisonBaits { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
        Description(
            "If set to false, bot will never use Jawless Skulker baits in Garrison."
            ), Category("Garrison")]
        public bool JawlessSkulkerBait { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
        Description(
            "If set to false, bot will never use Fat Sleeper Bait baits in Garrison."
            ), Category("Garrison")]
        public bool FatSleeperBait { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
        Description(
            "If set to false, bot will never use Blind Lake Sturgeon baits in Garrison."
            ), Category("Garrison")]
        public bool BlindLakeSturgeonBait { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
        Description(
            "If set to false, bot will never use Fire Ammonite Baits in Garrison."
            ), Category("Garrison")]
        public bool FireAmmoniteBait { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
        Description(
            "If set to false, bot will never use Sea Scorpion baits in Garrison."
            ), Category("Garrison")]
        public bool SeaScorpionBait { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
        Description(
            "If set to false, bot will never use Abyssal Gulper Eel Baits in Garrison."
            ), Category("Garrison")]
        public bool AbyssalGulperEelBaits { get; set; }

        [Setting, Styx.Helpers.DefaultValue(true),
        Description(
            "If set to false, bot will never use Blackwater Whiptail baits in Garrison."
            ), Category("Garrison")]
        public bool BlackwaterWhiptailBait { get; set; }

        #endregion

        #region Advanced

        [Setting, Styx.Helpers.DefaultValue(5),
		Description("The maximum time in minutes to spend at a pool before it gets blacklisted"), Category("Advanced")]
		public int MaxTimeAtPool { get; set; }

		[Setting, Styx.Helpers.DefaultValue(15),
		Description("The maximum number of failed casts at a pool before moving to a new location at pool"), Category("Advanced")]
		public int MaxFailedCasts { get; set; }


		[Setting, Styx.Helpers.DefaultValue(15f),
		Description("When bot is within this distance from current hotspot then it cycles to next hotspot. flymode only"),
		Category("Advanced")]
		public float PathPrecision { get; set; }

		[Setting, Styx.Helpers.DefaultValue(40),
		Description(
			"Number of tracelines to do in a 360 deg area. the higher the more likely to find a landing spot.recomended to set at a multiple of 20"
			),
		Category("Advanced")]
		public int TraceStep { get; set; }

		[Setting, Styx.Helpers.DefaultValue(0.5f),
		Description(
			"Each time bot fails to find a landing spot it adds this number to the range and tries again until it hits MaxPoolRange. Can use decimals."
			), Category("Advanced")]
		public float PoolRangeStep { get; set; }

		#endregion
	}
}