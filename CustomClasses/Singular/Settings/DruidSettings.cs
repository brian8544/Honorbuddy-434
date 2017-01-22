#region Revision Info

// This file is part of Singular - A community driven Honorbuddy CC
// $Author: raphus $
// $Date: 2012-02-28 19:21:20 +0200 (Sal, 28 Şub 2012) $
// $HeadURL: http://svn.apocdev.com/singular/trunk/Singular/Settings/DruidSettings.cs $
// $LastChangedBy: raphus $
// $LastChangedDate: 2012-02-28 19:21:20 +0200 (Sal, 28 Şub 2012) $
// $LastChangedRevision: 604 $
// $Revision: 604 $

#endregion

using System;
using System.ComponentModel;

using Styx.Helpers;

using DefaultValue = Styx.Helpers.DefaultValueAttribute;

namespace Singular.Settings
{
    public enum FeralForm
    {
        None,
        Bear,
        Cat
    }

    internal class DruidSettings : Styx.Helpers.Settings
    {
        public DruidSettings()
            : base(SingularSettings.SettingsPath + "_Druid.xml")
        {
        }

        #region Common

        [Setting]
        [DefaultValue(40)]
        [Category("Common")]
        [DisplayName("Innervate Mana")]
        [Description("Innervate will be used when your mana drops below this value")]
        public int InnervateMana { get; set; }

        [Setting]
        [DefaultValue(false)]
        [Category("Common")]
        [DisplayName("Disable Healing for Balance and Feral")]
        public bool NoHealBalanceAndFeral { get; set; }

        [Setting]
        [DefaultValue(20)]
        [Category("Common")]
        [DisplayName("Healing Touch Health (Balance and Feral)")]
        [Description("Healing Touch will be used at this value.")]
        public int NonRestoHealingTouch { get; set; }

        [Setting]
        [DefaultValue(40)]
        [Category("Common")]
        [DisplayName("Rejuvenation Health (Balance and Feral)")]
        [Description("Rejuvenation will be used at this value")]
        public int NonRestoRejuvenation { get; set; }

        [Setting]
        [DefaultValue(40)]
        [Category("Common")]
        [DisplayName("Regrowth Health (Balance and Feral)")]
        [Description("Regrowth will be used at this value")]
        public int NonRestoRegrowth { get; set; }

        #endregion

        #region Balance

        [Setting]
        [DefaultValue(false)]
        [Category("Balance")]
        [DisplayName("Starfall")]
        [Description("Use Starfall.")]
        public bool UseStarfall { get; set; }

        #endregion

        #region Resto

        [Setting]
        [DefaultValue(60)]
        [Category("Restoration")]
        [DisplayName("Tranquility Health")]
        [Description("Tranquility will be used at this value")]
        public int TranquilityHealth { get; set; }

        [Setting]
        [DefaultValue(3)]
        [Category("Restoration")]
        [DisplayName("Tranquility Count")]
        [Description("Tranquility will be used when count of party members whom health is below Tranquility health mets this value ")]
        public int TranquilityCount { get; set; }

        [Setting]
        [DefaultValue(65)]
        [Category("Restoration")]
        [DisplayName("Swiftmend Health")]
        [Description("Swiftmend will be used at this value")]
        public int Swiftmend { get; set; }

        [Setting]
        [DefaultValue(80)]
        [Category("Restoration")]
        [DisplayName("Wild Growth Health")]
        [Description("Wild Growth will be used at this value")]
        public int WildGrowthHealth { get; set; }

        [Setting]
        [DefaultValue(2)]
        [Category("Restoration")]
        [DisplayName("Wild Growth Count")]
        [Description("Wild Growth will be used when count of party members whom health is below Wild Growth health mets this value ")]
        public int WildGrowthCount { get; set; }

        [Setting]
        [DefaultValue(70)]
        [Category("Restoration")]
        [DisplayName("Regrowth Health")]
        [Description("Regrowth will be used at this value")]
        public int Regrowth { get; set; }

        [Setting]
        [DefaultValue(60)]
        [Category("Restoration")]
        [DisplayName("Healing Touch Health")]
        [Description("Healing Touch will be used at this value")]
        public int HealingTouch { get; set; }

        [Setting]
        [DefaultValue(75)]
        [Category("Restoration")]
        [DisplayName("Nourish Health")]
        [Description("Nourish will be used at this value")]
        public int Nourish { get; set; }

        [Setting]
        [DefaultValue(90)]
        [Category("Restoration")]
        [DisplayName("Rejuvenation Health")]
        [Description("Rejuvenation will be used at this value")]
        public int Rejuvenation { get; set; }

        [Setting]
        [DefaultValue(80)]
        [Category("Restoration")]
        [DisplayName("Tree of Life Health")]
        [Description("Tree of Life will be used at this value")]
        public int TreeOfLifeHealth { get; set; }

        [Setting]
        [DefaultValue(3)]
        [Category("Restoration")]
        [DisplayName("Tree of Life Count")]
        [Description("Tree of Life will be used when count of party members whom health is below Tree of Life health mets this value ")]
        public int TreeOfLifeCount { get; set; }

        [Setting]
        [DefaultValue(70)]
        [Category("Restoration")]
        [DisplayName("Barkskin Health")]
        [Description("Barkskin will be used at this value")]
        public int Barkskin { get; set; }

        #endregion

        #region Feral
        
        [Setting]
        [DefaultValue(50)]
        [Category("Feral")]
        [DisplayName("Barkskin Health")]
        [Description("Barkskin will be used at this value. Set this to 100 to enable on cooldown usage. (Recommended: 100)")]
        public int FeralBarkskin { get; set; }

        [Setting]
        [DefaultValue(55)]
        [Category("Feral")]
        [DisplayName("Survival Instincts Health")]
        [Description("SI will be used at this value. Set this to 100 to enable on cooldown usage. (Recommended: 55)")]
        public int SurvivalInstinctsHealth { get; set; }

        [Setting]
        [DefaultValue(30)]
        [Category("Feral Tanking")]
        [DisplayName("Frenzied Regeneration Health")]
        [Description("FR will be used at this value. Set this to 100 to enable on cooldown usage. (Recommended: 30 if glyphed. 15 if not.)")]
        public int FrenziedRegenerationHealth { get; set; }

        [Setting]
        [DefaultValue(FeralForm.None)]
        [Category("Feral")]
        [DisplayName("Manual Feral Form")]
        [Description("This setting will be used when Singular can't decide the roles in a raid")]
        public FeralForm ManualFeralForm { get; set; }

        #endregion
    }
}