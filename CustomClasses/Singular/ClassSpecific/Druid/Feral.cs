using System;
using System.Linq;

using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;
using Styx;
using Styx.Combat.CombatRoutine;
using Styx.Logic;
using Styx.Logic.Combat;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using TreeSharp;
using CommonBehaviors.Actions;
using Action = TreeSharp.Action;


namespace Singular.ClassSpecific.Druid
{
    public class Feral
    {
        #region Properties & Fields

        private static DruidSettings Settings { get { return SingularSettings.Instance.Druid; } }

        private const int FERAL_T11_ITEM_SET_ID = 928;
        private const int FERAL_T13_ITEM_SET_ID = 1058;

        private static int NumTier11Pieces
        {
            get
            {
                return StyxWoW.Me.CarriedItems.Count(i => i.ItemInfo.ItemSetId == FERAL_T11_ITEM_SET_ID);
            }
        }

        private static bool Has4PieceTier11Bonus { get { return NumTier11Pieces >= 4; } }

        private static int NumTier13Pieces
        {
            get
            {
                return StyxWoW.Me.CarriedItems.Count(i => i.ItemInfo.ItemSetId == FERAL_T13_ITEM_SET_ID);
            }
        }

        private static bool Has2PieceTier13Bonus { get { return NumTier13Pieces >= 2; } }

        #endregion

        #region Normal Rotation

        [Spec(TalentSpec.FeralDruid)]
        [Behavior(BehaviorType.Pull)]
        [Class(WoWClass.Druid)]
        [Context(WoWContext.Normal)]
        public static Composite CreateFeralNormalPull()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                
                new Decorator(
                    ret => StyxWoW.Me.Level < 20,
                    CreateFeralLevel1020Pull()),

                new Decorator(
                    ret => StyxWoW.Me.Level < 46,
                    CreateFeralLevel2046Pull()),

                Spell.BuffSelf("Cat Form"),
                Spell.BuffSelf("Prowl", ret => StyxWoW.Me.CurrentTarget.DistanceSqr < 30 * 30),
                Spell.Cast("Feral Charge (Cat)"),
                Spell.Cast("Dash",
                    ret => StyxWoW.Me.CurrentTarget.Distance > Spell.MeleeRange + 2f &&
                           !StyxWoW.Me.HasAura("Stampeding Roar") && 
                           (!SpellManager.HasSpell("Feral Charge (Cat)") || 
                           SpellManager.Spells["Feral Charge (Cat)"].CooldownTimeLeft.TotalSeconds >= 3)),
                Spell.BuffSelf("Stampeding Roar (Cat)",
                    ret => StyxWoW.Me.CurrentTarget.Distance > Spell.MeleeRange + 2f &&
                           !StyxWoW.Me.HasAura("Dash") &&
                           (!SpellManager.HasSpell("Feral Charge (Cat)") ||
                           SpellManager.Spells["Feral Charge (Cat)"].CooldownTimeLeft.TotalSeconds >= 3)),
                Spell.Cast("Pounce"),
                Spell.Cast("Shred", ret => StyxWoW.Me.CurrentTarget.MeIsBehind),
                Spell.Cast("Mangle (Cat)"),
                Spell.Cast("Moonfire", ret => StyxWoW.Me.CurrentTarget.Distance2DSqr < 10*10 && Math.Abs(StyxWoW.Me.CurrentTarget.Z - StyxWoW.Me.Z) > 5),
                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        [Spec(TalentSpec.FeralDruid)]
        [Behavior(BehaviorType.Combat)]
        [Class(WoWClass.Druid)]
        [Context(WoWContext.Normal)]
        public static Composite CreateFeralNormalCombat()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Common.CreateNonRestoHeals(),
                Spell.BuffSelf("Cat Form"),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Helpers.Common.CreateAutoAttack(true),
                Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),

                // Defensive spells
                Spell.BuffSelf("Barkskin", ret => StyxWoW.Me.HealthPercent < Settings.FeralBarkskin),
                Spell.BuffSelf("Survival Instincts", ret => StyxWoW.Me.HealthPercent < Settings.SurvivalInstinctsHealth),

                new Decorator(
                    ret => Unit.UnfriendlyUnitsNearTarget(8f).Count() >= 3,
                    new PrioritySelector(
                        Spell.Cast("Ferocious Bite",
                            ret => StyxWoW.Me.ComboPoints == 5 ||
                                   StyxWoW.Me.ComboPoints >= 2 && StyxWoW.Me.CurrentTarget.HealthPercent < 20),
                        Spell.Cast("Swipe (Cat)"),
                        Spell.Cast("Mangle (Cat)")
                        )),

                new Decorator(
                    ret => StyxWoW.Me.Level < 20,
                    CreateFeralLevel1020Combat()),

                new Decorator(
                    ret => StyxWoW.Me.Level < 46,
                    CreateFeralLevel2046Combat()),

                Movement.CreateMoveBehindTargetBehavior(),
                Spell.BuffSelf("Tiger's Fury"),
                Spell.Cast("Ferocious Bite", 
                    ret => StyxWoW.Me.ComboPoints == 5 ||
                           StyxWoW.Me.ComboPoints > 1 && StyxWoW.Me.CurrentTarget.HealthPercent < 20),
                Spell.Cast("Shred", ret => StyxWoW.Me.CurrentTarget.MeIsBehind),
                Spell.Cast("Ravage!", ret => StyxWoW.Me.HasAura("Stampede")),
                Spell.Buff("Rake", true, ret => StyxWoW.Me.CurrentTarget.Elite),
                Spell.Cast("Mangle (Cat)", ret => !StyxWoW.Me.CurrentTarget.MeIsBehind),
                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        private static Composite CreateFeralLevel1020Pull()
        {
            return new PrioritySelector(
                Spell.Buff("Moonfire"),
                Movement.CreateMoveToTargetBehavior(true, 30f)
                );
        }

        private static Composite CreateFeralLevel1020Combat()
        {
            return new PrioritySelector(
                Spell.Cast("Ferocious Bite",
                    ret => StyxWoW.Me.ComboPoints == 5 ||
                           StyxWoW.Me.ComboPoints > 1 && StyxWoW.Me.CurrentTarget.HealthPercent < 20),
                Spell.Buff("Rake", true, ret => StyxWoW.Me.CurrentTarget.Elite),
                Spell.Cast("Mangle (Cat)"),
                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        private static Composite CreateFeralLevel2046Pull()
        {
            return new PrioritySelector(
                Spell.BuffSelf("Cat Form"),
                Spell.BuffSelf("Prowl"),
                Spell.Cast("Feral Charge (Cat)"),
                Spell.Cast("Ravage", ret => StyxWoW.Me.CurrentTarget.MeIsBehind),
                Spell.Cast("Pounce"),
                Spell.Cast("Mangle (Cat)"),
                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        private static Composite CreateFeralLevel2046Combat()
        {
            return new PrioritySelector(
                Spell.Cast("Ferocious Bite",
                    ret => StyxWoW.Me.ComboPoints == 5 ||
                           StyxWoW.Me.ComboPoints > 1 && StyxWoW.Me.CurrentTarget.HealthPercent < 20),
                Spell.Cast("Ravage!", ret => StyxWoW.Me.HasAura("Stampede")),
                Spell.Buff("Rake", true, ret => StyxWoW.Me.CurrentTarget.Elite),
                Spell.Cast("Mangle (Cat)"),
                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        #endregion

        #region Battleground Rotation

        [Spec(TalentSpec.FeralDruid)]
        [Behavior(BehaviorType.Pull)]
        [Class(WoWClass.Druid)]
        [Context(WoWContext.Battlegrounds)]
        public static Composite CreateFeralPvPPull()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Spell.BuffSelf("Cat Form"),
                Spell.BuffSelf("Prowl"),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Spell.WaitForCast(),
                Spell.Cast("Feral Charge (Cat)"),
                Spell.Cast("Dash",
                    ret => StyxWoW.Me.CurrentTarget.Distance > Spell.MeleeRange + 2f &&
                           !StyxWoW.Me.HasAura("Stampeding Roar") &&
                           (!SpellManager.HasSpell("Feral Charge (Cat)") ||
                           SpellManager.Spells["Feral Charge (Cat)"].CooldownTimeLeft.TotalSeconds >= 3)),
                Spell.BuffSelf("Stampeding Roar (Cat)",
                    ret => StyxWoW.Me.CurrentTarget.Distance > Spell.MeleeRange + 2f &&
                           !StyxWoW.Me.HasAura("Dash") &&
                           (!SpellManager.HasSpell("Feral Charge (Cat)") ||
                           SpellManager.Spells["Feral Charge (Cat)"].CooldownTimeLeft.TotalSeconds >= 3)),
                //Spell.Cast("Ravage", ret => StyxWoW.Me.CurrentTarget.IsSitting),
                Spell.Cast("Pounce", ret => !StyxWoW.Me.CurrentTarget.IsStunned()),
                Spell.Cast("Ravage"),
                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        [Spec(TalentSpec.FeralDruid)]
        [Behavior(BehaviorType.Combat)]
        [Class(WoWClass.Druid)]
        [Context(WoWContext.Battlegrounds)]
        public static Composite CreateFeralPvPCombat()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Spell.BuffSelf("Cat Form"),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Spell.WaitForCast(),
                Helpers.Common.CreateAutoAttack(true),
                Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),

                // Defensive spells
                Spell.BuffSelf("Barkskin", ret => StyxWoW.Me.HealthPercent < Settings.FeralBarkskin),
                Spell.BuffSelf("Survival Instincts", ret => StyxWoW.Me.HealthPercent < Settings.SurvivalInstinctsHealth),

                // Run Forest Run!
                Spell.Cast("Feral Charge (Cat)"),
                Spell.Cast("Dash", 
                    ret => StyxWoW.Me.CurrentTarget.Distance > Spell.MeleeRange + 2f && 
                           !StyxWoW.Me.HasAura("Stampeding Roar")),
                Spell.BuffSelf("Stampeding Roar (Cat)",
                    ret => StyxWoW.Me.CurrentTarget.Distance > Spell.MeleeRange + 2f &&
                           !StyxWoW.Me.HasAura("Dash")),

                // Rotation
                Spell.Cast("Ferocious Bite", ret => StyxWoW.Me.ComboPoints >= 3 && StyxWoW.Me.CurrentTarget.HealthPercent < 20),
                Spell.Buff("Rip", true, ret => StyxWoW.Me.ComboPoints == 5),
                Spell.Cast("Maim", ret => StyxWoW.Me.ComboPoints == 5),
                Spell.BuffSelf("Savage Roar", 
                    ret => StyxWoW.Me.ComboPoints > 0 && StyxWoW.Me.CurrentTarget.Distance > Spell.MeleeRange + 5f),
                Spell.Buff("Mangle (Cat)", "Mangle", "Trauma", "Stampede"),
                Spell.BuffSelf("Tiger's Fury", 
                    ret => StyxWoW.Me.EnergyPercent <= 25 || 
                           !StyxWoW.Me.CurrentTarget.HasMyAura("Rip") && StyxWoW.Me.ComboPoints == 4 &&
                           StyxWoW.Me.CurrentTarget.HealthPercent > 20),
                Spell.BuffSelf("Berserk", ret => StyxWoW.Me.HasAura("Tiger's Fury")),
                Spell.Buff("Rake", true),
                Spell.Cast("Ravage!"),
                Spell.Cast("Shred", ret => StyxWoW.Me.CurrentTarget.MeIsBehind),
                Spell.Cast("Mangle (Cat)"),
                Spell.Buff("Faerie Fire (Feral)", "Faerie Fire"),
                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        #endregion

        #region Instance Rotation

        [Spec(TalentSpec.FeralDruid)]
        [Behavior(BehaviorType.Pull)]
        [Behavior(BehaviorType.Combat)]
        [Class(WoWClass.Druid)]
        [Context(WoWContext.Instances)]
        public static Composite CreateFeralInstanceCombat()
        {
            return new PrioritySelector(
                Safers.EnsureTarget(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                Spell.WaitForCast(),
                Helpers.Common.CreateAutoAttack(true),
                Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),
                new Decorator(
                    ret => !Group.Tanks.Any() && !Group.Healers.Any(),
                    new PrioritySelector(
                        new Decorator(
                            ret => SingularSettings.Instance.Druid.ManualFeralForm == FeralForm.None && StyxWoW.Me.CurrentMap.IsDungeon,
                            new Action(ret => Logger.Write("Singular can't decide which form to use since there is no roles set in your raid. Please set ManualFeralForm setting to your desired form from Class Config"))),
                        new Decorator(
                            ret => SingularSettings.Instance.Druid.ManualFeralForm == FeralForm.Cat || !StyxWoW.Me.CurrentMap.IsDungeon,
                            CreateFeralCatInstanceCombat()),
                        CreateFeralBearInstanceCombat()
                        )),
                new Decorator(
                    ret => !Group.MeIsTank && Group.Tanks.Any(t => t.IsAlive),
                    CreateFeralCatInstanceCombat()),
                CreateFeralBearInstanceCombat()       
                );
        }

        private static Composite CreateFeralBearInstanceCombat()
        {
            return new PrioritySelector(
                Spell.BuffSelf("Bear Form"),
                Spell.Cast("Feral Charge (Bear)"),
                // Defensive CDs are hard to 'roll' from this type of logic, so we'll simply use them more as 'oh shit' buttons, than anything.
                // Barkskin should be kept on CD, regardless of what we're tanking
                Spell.BuffSelf("Barkskin", ret => StyxWoW.Me.HealthPercent < Settings.FeralBarkskin),
                // Since Enrage no longer makes us take additional damage, just keep it on CD. Its a rage boost, and coupled with King of the Jungle, a DPS boost for more threat.
                Spell.BuffSelf("Enrage"),
                // Only pop SI if we're taking a bunch of damage.
                Spell.BuffSelf("Survival Instincts", ret => StyxWoW.Me.HealthPercent < Settings.SurvivalInstinctsHealth),
                // We only want to pop FR < 30%. Users should not be able to change this value, as FR automatically pushes us to 30% hp.
                Spell.BuffSelf("Frenzied Regeneration", ret => StyxWoW.Me.HealthPercent < Settings.FrenziedRegenerationHealth),
                // Make sure we deal with interrupts...
                //Spell.Cast(80964 /*"Skull Bash (Bear)"*/, ret => (WoWUnit)ret, ret => ((WoWUnit)ret).IsCasting),
                Helpers.Common.CreateInterruptSpellCast(ret => StyxWoW.Me.CurrentTarget),

                new Decorator(
                    ret => Unit.NearbyUnfriendlyUnits.Count(u => u.DistanceSqr < 8 * 8) >= 3,
                    new PrioritySelector(
                        Spell.Cast("Berserk"),
                        Spell.Cast("Demoralizing Roar", 
                            ret => Unit.NearbyUnfriendlyUnits.Any(u => u.DistanceSqr < 10*10 && !u.HasDemoralizing())),
                        Spell.Cast("Maul", ret => TalentManager.HasGlyph("Maul")),
                        Spell.Cast("Thrash"),
                        Spell.Cast("Swipe (Bear)"),
                        Spell.Cast("Mangle (Bear)"),
                        Movement.CreateMoveToMeleeBehavior(true)
                        )),
                // If we have 3+ units not targeting us, and are within 10yds, then pop our AOE taunt. (These are ones we have 'no' threat on, or don't hold solid threat on)
                Spell.Cast(
                    "Challenging Roar", ret => TankManager.Instance.NeedToTaunt.FirstOrDefault(),
                    ret => SingularSettings.Instance.EnableTaunting && TankManager.Instance.NeedToTaunt.Count(u => u.Distance <= 10) >= 3),
                // If there's a unit that needs taunting, do it.
                Spell.Cast(
                    "Growl", ret => TankManager.Instance.NeedToTaunt.FirstOrDefault(),
                    ret => SingularSettings.Instance.EnableTaunting),
                Spell.Cast("Pulverize", ret => ((WoWUnit)ret).HasAura("Lacerate", 3) && !StyxWoW.Me.HasAura("Pulverize")),

                Spell.Buff("Demoralizing Roar", ret => !StyxWoW.Me.CurrentTarget.HasDemoralizing()),

                Spell.Cast("Faerie Fire (Feral)", ret => !((WoWUnit)ret).HasSunders()),
                Spell.Cast("Mangle (Bear)"),
                // Maul is our rage dump... don't pop it unless we have to, or we still have > 2 targets.
                Spell.Cast("Maul", ret => StyxWoW.Me.RagePercent > 60),
                Spell.Cast("Lacerate"),
                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        private static Composite CreateFeralCatInstanceCombat()
        {
            return new PrioritySelector(
                Spell.BuffSelf("Cat Form"),
                Spell.Cast("Feral Charge (Cat)"),

                Spell.Cast("Dash",
                    ret => StyxWoW.Me.CurrentTarget.Distance > Spell.MeleeRange + 2f &&
                           !StyxWoW.Me.HasAura("Stampeding Roar")),
                Spell.BuffSelf("Stampeding Roar (Cat)",
                    ret => StyxWoW.Me.CurrentTarget.Distance > Spell.MeleeRange + 2f &&
                           !StyxWoW.Me.HasAura("Dash")),

                Spell.BuffSelf("Barkskin", ret => StyxWoW.Me.HealthPercent < Settings.FeralBarkskin),
                Spell.BuffSelf("Survival Instincts", ret => StyxWoW.Me.HealthPercent < Settings.SurvivalInstinctsHealth),

                new Decorator(
                    ret => Unit.NearbyUnfriendlyUnits.Count(u => u.DistanceSqr < 5*5) >= 3,
                    new PrioritySelector(
                        Spell.BuffSelf("Tiger's Fury"),
                        Spell.BuffSelf("Berserk"),
                        Spell.Cast("Swipe (Cat)"),
                        Movement.CreateMoveToMeleeBehavior(true)
                        )),

                Movement.CreateMoveBehindTargetBehavior(),

                Spell.BuffSelf("Tiger's Fury", ret => StyxWoW.Me.EnergyPercent < 35 && !StyxWoW.Me.HasAura("Stampede")),
                Spell.BuffSelf("Berserk", ret => StyxWoW.Me.HasAura("Tiger's Fury")),
                Spell.Cast("Mangle (Cat)", 
                    ret => Has4PieceTier11Bonus && StyxWoW.Me.GetAuraTimeLeft("Strength of the Panther", false).TotalSeconds < 3),
                Spell.Buff("Faerie Fire (Feral)", ret => !StyxWoW.Me.CurrentTarget.HasSunders()),
                Spell.Buff("Mangle (Cat)", "Mangle", "Trauma", "Stampede"),
                Spell.Cast("Ravage!", ret => StyxWoW.Me.GetAuraTimeLeft("Stampede", true).TotalSeconds < 3),
                Spell.Cast("Ferocious Bite", 
                    ret => TalentManager.GetCount(2,19) == 2 && StyxWoW.Me.CurrentTarget.HealthPercent < (Has2PieceTier13Bonus ? 60 : 25) &&
                           (StyxWoW.Me.ComboPoints == 5 ||
                           StyxWoW.Me.CurrentTarget.HasMyAura("Rip") && StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rip", true).TotalSeconds < 3)),
                Spell.Cast("Shred", 
                    ret => TalentManager.HasGlyph("Bloodletting") && StyxWoW.Me.CurrentTarget.HasMyAura("Rip") &&
                           StyxWoW.Me.CurrentTarget.MeIsBehind &&
                           StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rip", true).TotalSeconds < 14),
                Spell.Cast("Rip", 
                    ret => StyxWoW.Me.ComboPoints == 5 && StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rip", true).TotalSeconds < 2),
                Spell.Cast("Ferocious Bite", 
                    ret => StyxWoW.Me.HasAura("Berserk") && StyxWoW.Me.ComboPoints == 5 &&
                           StyxWoW.Me.CurrentTarget.HasMyAura("Rip") && StyxWoW.Me.CurrentTarget.HasMyAura("Savage Roar") &&
                           StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rip", true).TotalSeconds >= 5 &&
                           StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Savage Roar", true).TotalSeconds >= 3),
                Spell.Cast("Rake", 
                    ret => StyxWoW.Me.HasAura("Tiger's Fury") && StyxWoW.Me.CurrentTarget.HasMyAura("Rake") &&
                           StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rake", true).TotalSeconds < 9),
                Spell.Cast("Rake", ret => StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rake", true).TotalSeconds < 3),
                Spell.Cast("Shred", ret => StyxWoW.Me.HasAura("Omen of Clarity") && StyxWoW.Me.CurrentTarget.MeIsBehind),
                Spell.Cast("Mangle (Cat)", ret => StyxWoW.Me.HasAura("Omen of Clarity") && !StyxWoW.Me.CurrentTarget.MeIsBehind),
                Spell.Cast("Savage Roar", ret => StyxWoW.Me.GetAuraTimeLeft("Savage Roar", true).TotalSeconds < 2),
                Spell.Cast("Ferocious Bite", 
                    ret => StyxWoW.Me.ComboPoints == 5 && StyxWoW.Me.CurrentTarget.HasMyAura("Rip") && 
                           StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Rip", true).TotalSeconds >= 14 &&
                           (TalentManager.GetCount(2, 17) < 2 || 
                           StyxWoW.Me.CurrentTarget.HasMyAura("Savage Roar") &&
                           StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Savage Roar", true).TotalSeconds >= 10)),
                Spell.Cast("Ravage!", 
                    ret => StyxWoW.Me.HasAura("Stampede") && !StyxWoW.Me.HasAura("Omen of Clarity") &&
                           (StyxWoW.Me.HasAura("Tiger's Fury") || 
                           SpellManager.HasSpell("Tiger's Fury") && !SpellManager.GlobalCooldown &&
                           SpellManager.Spells["Tiger's Fury"].CooldownTimeLeft.TotalSeconds <= 3)),
                Spell.Cast("Mangle (Cat)", ret => Has4PieceTier11Bonus && !StyxWoW.Me.HasAura("Strength of the Panther", 3)),
                new Decorator(
                    ret => StyxWoW.Me.CurrentTarget.MeIsBehind,
                    new PrioritySelector(
                        Spell.Cast("Shred", ret => StyxWoW.Me.HasAura("Tiger's Fury") && StyxWoW.Me.HasAura("Berserk")),
                        Spell.Cast("Shred", 
                            ret => SpellManager.HasSpell("Tiger's Fury") && !SpellManager.GlobalCooldown &&
                                   SpellManager.Spells["Tiger's Fury"].CooldownTimeLeft.TotalSeconds <= 3),
                        Spell.Cast("Shred", ret => StyxWoW.Me.ComboPoints == 4),
                        Spell.Cast("Shred", ret => StyxWoW.Me.EnergyPercent >= 85))),
                new Decorator(
                    ret => !StyxWoW.Me.CurrentTarget.MeIsBehind,
                    new PrioritySelector(
                        Spell.Cast("Mangle (Cat)", ret => StyxWoW.Me.HasAura("Tiger's Fury") && StyxWoW.Me.HasAura("Berserk")),
                        Spell.Cast("Mangle (Cat)", 
                            ret => SpellManager.HasSpell("Tiger's Fury") && !SpellManager.GlobalCooldown &&
                                   SpellManager.Spells["Tiger's Fury"].CooldownTimeLeft.TotalSeconds <= 3),
                        Spell.Cast("Mangle (Cat)", ret => StyxWoW.Me.ComboPoints == 4),
                        Spell.Cast("Mangle (Cat)", ret => StyxWoW.Me.EnergyPercent >= 85))),
                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        #endregion
    }
}