using System.Linq;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;
using Styx;
using Styx.Combat.CombatRoutine;
using Styx.WoWInternals.WoWObjects;
using TreeSharp;

namespace Singular.ClassSpecific.Druid
{
    public class Common
    {
        public static ShapeshiftForm WantedDruidForm { get; set; }

        #region PreCombat Buffs

        [Class(WoWClass.Druid)]
        [Behavior(BehaviorType.PreCombatBuffs)]
        [Spec(TalentSpec.BalanceDruid)]
        [Spec(TalentSpec.FeralDruid)]
        [Spec(TalentSpec.RestorationDruid)]
        [Spec(TalentSpec.Lowbie)]
        [Context(WoWContext.All)]
        public static Composite CreateDruidPreCombatBuff()
        {
            return new PrioritySelector(
               Spell.Cast(
                   "Mark of the Wild",
                   ret => StyxWoW.Me,
                   ret => !StyxWoW.Me.HasAura("Prowl") &&
                          (Unit.NearbyFriendlyPlayers.Any(unit =>
                               !unit.Dead && !unit.IsGhost && unit.IsInMyPartyOrRaid &&
                               !unit.HasAnyAura("Mark of the Wild", "Embrace of the Shale Spider", "Blessing of Kings")) || 
                          !StyxWoW.Me.HasAnyAura("Mark of the Wild", "Embrace of the Shale Spider", "Blessing of Kings")))
                );
        }

        #endregion

        #region Combat Buffs

        [Class(WoWClass.Druid)]
        [Behavior(BehaviorType.CombatBuffs)]
        [Spec(TalentSpec.BalanceDruid)]
        [Spec(TalentSpec.FeralDruid)]
        [Spec(TalentSpec.RestorationDruid)]
        [Context(WoWContext.Instances)]
        public static Composite CreateDruidInstanceCombatBuffs()
        {
            const uint mapleSeedId = 17034;

            return new PrioritySelector(
                ctx => Group.Tanks.FirstOrDefault(t => !t.IsMe && t.Dead) ?? Group.Healers.FirstOrDefault(h => !h.IsMe && h.Dead),
                new Decorator(
                    ret => ret != null && Item.HasItem(mapleSeedId),
                    new PrioritySelector(
                        Spell.WaitForCast(true),
                        Movement.CreateMoveToLosBehavior(ret => (WoWPlayer)ret),
                        Spell.Cast("Rebirth", ret => (WoWPlayer)ret),
                        Movement.CreateMoveToTargetBehavior(true, 35f)))
                );
        }

        #endregion

        #region Rest

        [Class(WoWClass.Druid)]
        [Behavior(BehaviorType.Rest)]
        [Spec(TalentSpec.BalanceDruid)]
        [Spec(TalentSpec.FeralDruid)]
        [Context(WoWContext.All)]
        public static Composite CreateBalanceAndFeralDruidRest()
        {
            return new PrioritySelector(
                CreateNonRestoHeals(),
                Rest.CreateDefaultRestBehaviour(),
                Spell.Resurrect("Revive")
                );
        }

        #endregion

        #region Non Resto Healing

        public static Composite CreateNonRestoHeals()
        {
            return
                new Decorator(
                    ret => !SingularSettings.Instance.Druid.NoHealBalanceAndFeral && !StyxWoW.Me.HasAura("Drink"),
                    new PrioritySelector(
                        Spell.WaitForCast(false,false),
                        Spell.Heal("Rejuvenation",
                            ret => StyxWoW.Me.HealthPercent <= SingularSettings.Instance.Druid.NonRestoRejuvenation &&
                                    !StyxWoW.Me.HasAura("Rejuvenation")),
                        Spell.Heal("Regrowth",
                            ret => StyxWoW.Me.HealthPercent <= SingularSettings.Instance.Druid.NonRestoRegrowth &&
                                    !StyxWoW.Me.HasAura("Regrowth")),
                        Spell.Heal("Healing Touch",
                            ret => StyxWoW.Me.HealthPercent <= SingularSettings.Instance.Druid.NonRestoHealingTouch)));
        }

        #endregion
    }
}
