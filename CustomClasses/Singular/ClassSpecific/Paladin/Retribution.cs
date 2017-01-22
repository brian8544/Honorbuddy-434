using System.Linq;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;
using Styx;
using Styx.Combat.CombatRoutine;
using Styx.Logic.Combat;

using TreeSharp;

namespace Singular.ClassSpecific.Paladin
{
    public class Retribution
    {
        #region Normal Rotation

        [Class(WoWClass.Paladin)]
        [Spec(TalentSpec.RetributionPaladin)]
        [Behavior(BehaviorType.Pull)]
        [Behavior(BehaviorType.Combat)]
        [Context(WoWContext.Normal)]
        public static Composite CreateRetributionPaladinNormalPullAndCombat()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Helpers.Common.CreateAutoAttack(true),
                Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),

                // Heals
                Spell.Heal("Holy Light", ret => StyxWoW.Me, ret => !SpellManager.HasSpell("Flash of Light") && StyxWoW.Me.HealthPercent < 30),
                Spell.Heal("Flash of Light", ret => StyxWoW.Me, ret => StyxWoW.Me.HealthPercent < 30),
                Spell.Heal("Word of Glory", ret => StyxWoW.Me, ret => StyxWoW.Me.HealthPercent < 50 && StyxWoW.Me.CurrentHolyPower == 3),

                // Defensive
                Spell.BuffSelf("Hand of Freedom",
                    ret => StyxWoW.Me.HasAuraWithMechanic(WoWSpellMechanic.Dazed,
                                                          WoWSpellMechanic.Disoriented,
                                                          WoWSpellMechanic.Frozen,
                                                          WoWSpellMechanic.Incapacitated,
                                                          WoWSpellMechanic.Rooted,
                                                          WoWSpellMechanic.Slowed,
                                                          WoWSpellMechanic.Snared)),

                // AoE Rotation
                new Decorator(
                    ret => Unit.UnfriendlyUnitsNearTarget(8f).Count() >= SingularSettings.Instance.Paladin.ConsecrationCount,
                    new PrioritySelector(
                        // Cooldowns
                        Spell.BuffSelf("Zealotry"),
                        Spell.BuffSelf("Avenging Wrath"),
                        Spell.BuffSelf("Guardian of Ancient Kings"),
                        Spell.BuffSelf("Divine Storm"),
                        Spell.BuffSelf("Consecration"),
                        Spell.BuffSelf("Holy Wrath")
                        )),

                // Rotation
                Spell.BuffSelf("Inquisition", ret => StyxWoW.Me.CurrentHolyPower == 3),
                Spell.Cast("Hammer of Justice", ret => StyxWoW.Me.HealthPercent <= 40),
                Spell.Cast("Crusader Strike"),
                Spell.Cast("Hammer of Wrath"),
                Spell.Cast("Templar's Verdict",
                    ret => StyxWoW.Me.CurrentHolyPower == 3 &&
                           (StyxWoW.Me.HasAura("Inquisition") || !SpellManager.HasSpell("Inquisition"))),
                Spell.Cast("Exorcism", ret => StyxWoW.Me.ActiveAuras.ContainsKey("The Art of War")),
                Spell.Cast("Judgement"),

                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        #endregion

        #region Battleground Rotation

        [Class(WoWClass.Paladin)]
        [Spec(TalentSpec.RetributionPaladin)]
        [Behavior(BehaviorType.Pull)]
        [Behavior(BehaviorType.Combat)]
        [Context(WoWContext.Battlegrounds)]
        public static Composite CreateRetributionPaladinPvPPullAndCombat()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Helpers.Common.CreateAutoAttack(true),
                Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),

                // Defensive
                Spell.BuffSelf("Hand of Freedom",
                    ret => !StyxWoW.Me.Auras.Values.Any(a => a.Name.Contains("Hand of") && a.CreatorGuid == StyxWoW.Me.Guid) &&
                           StyxWoW.Me.HasAuraWithMechanic(WoWSpellMechanic.Dazed,
                                                          WoWSpellMechanic.Disoriented,
                                                          WoWSpellMechanic.Frozen,
                                                          WoWSpellMechanic.Incapacitated,
                                                          WoWSpellMechanic.Rooted,
                                                          WoWSpellMechanic.Slowed,
                                                          WoWSpellMechanic.Snared)),
                Spell.BuffSelf("Divine Shield", ret => StyxWoW.Me.HealthPercent <= 20 && !StyxWoW.Me.HasAura("Forbearance")),

                // Cooldowns
                Spell.BuffSelf("Zealotry"),
                Spell.BuffSelf("Avenging Wrath"),
                Spell.BuffSelf("Guardian of Ancient Kings"),

                // AoE Rotation
                new Decorator(
                    ret => Unit.UnfriendlyUnitsNearTarget(8f).Count() >= 3,
                    new PrioritySelector(
                        Spell.BuffSelf("Divine Storm"),
                        Spell.BuffSelf("Consecration"),
                        Spell.BuffSelf("Holy Wrath")
                        )),

                // Rotation
                Spell.BuffSelf("Inquisition", ret => StyxWoW.Me.CurrentHolyPower == 3),
                Spell.Cast("Hammer of Justice", ret => StyxWoW.Me.CurrentTarget.HealthPercent <= 40),
                Spell.Cast("Crusader Strike"),
                Spell.Cast("Hammer of Wrath"),
                Spell.Cast("Templar's Verdict",
                    ret => StyxWoW.Me.CurrentHolyPower == 3 &&
                           (StyxWoW.Me.HasAura("Inquisition") || !SpellManager.HasSpell("Inquisition"))),
                Spell.Cast("Exorcism", ret => StyxWoW.Me.ActiveAuras.ContainsKey("The Art of War")),
                Spell.Cast("Judgement"),
                Spell.BuffSelf("Holy Wrath"),
                Spell.BuffSelf("Consecration"),

                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        #endregion

        #region Instance Rotation

        [Class(WoWClass.Paladin)]
        [Spec(TalentSpec.RetributionPaladin)]
        [Behavior(BehaviorType.Pull)]
        [Behavior(BehaviorType.Combat)]
        [Context(WoWContext.Instances)]
        public static Composite CreateRetributionPaladinInstancePullAndCombat()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Helpers.Common.CreateAutoAttack(true),
                Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),
                Movement.CreateMoveBehindTargetBehavior(),

                // Defensive
                Spell.BuffSelf("Hand of Freedom",
                    ret => !StyxWoW.Me.Auras.Values.Any(a => a.Name.Contains("Hand of") && a.CreatorGuid == StyxWoW.Me.Guid) &&
                           StyxWoW.Me.HasAuraWithMechanic(WoWSpellMechanic.Dazed,
                                                          WoWSpellMechanic.Disoriented,
                                                          WoWSpellMechanic.Frozen,
                                                          WoWSpellMechanic.Incapacitated,
                                                          WoWSpellMechanic.Rooted,
                                                          WoWSpellMechanic.Slowed,
                                                          WoWSpellMechanic.Snared)),
                Spell.BuffSelf("Divine Shield", ret => StyxWoW.Me.HealthPercent <= 20 && !StyxWoW.Me.HasAura("Forbearance")),

                // Cooldowns
                new Decorator(
                    ret => StyxWoW.Me.CurrentTarget.IsBoss(),
                    new PrioritySelector(
                    Spell.BuffSelf("Zealotry"),
                    Spell.BuffSelf("Avenging Wrath"),
                    Spell.BuffSelf("Guardian of Ancient Kings"))),

                // AoE Rotation
                new Decorator(
                    ret => Unit.UnfriendlyUnitsNearTarget(8f).Count() >= SingularSettings.Instance.Paladin.ConsecrationCount,
                    new PrioritySelector(
                        Spell.BuffSelf("Divine Storm"),
                        Spell.BuffSelf("Consecration"),
                        Spell.BuffSelf("Holy Wrath")
                        )),

                // Rotation
                Spell.BuffSelf("Inquisition", ret => StyxWoW.Me.CurrentHolyPower == 3),
                Spell.Cast("Crusader Strike"),
                Spell.Cast("Hammer of Wrath"),
                Spell.Cast("Templar's Verdict", 
                    ret => StyxWoW.Me.CurrentHolyPower == 3 && 
                           (StyxWoW.Me.HasAura("Inquisition") || !SpellManager.HasSpell("Inquisition"))),
                Spell.Cast("Exorcism", ret => StyxWoW.Me.ActiveAuras.ContainsKey("The Art of War")),
                Spell.Cast("Judgement"),
                Spell.BuffSelf("Holy Wrath"),
                Spell.BuffSelf("Consecration"),

                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        #endregion
    }
}
