using System;

using Singular.Managers;
using Styx;
using Styx.Combat.CombatRoutine;
using Styx.Helpers;
using Styx.Logic.Combat;
using TreeSharp;
using Action = TreeSharp.Action;

namespace Singular.Helpers
{
    internal static class Common
    {
        /// <summary>
        ///  Creates a behavior to start auto attacking to current target.
        /// </summary>
        /// <remarks>
        ///  Created 23/05/2011
        /// </remarks>
        /// <param name="includePet"> This will also toggle pet auto attack. </param>
        /// <returns></returns>
        public static Composite CreateAutoAttack(bool includePet)
        {
            const int spellIdAutoShot = 75;

            return new PrioritySelector(
                new Decorator(
                    ret => !StyxWoW.Me.IsAutoAttacking && StyxWoW.Me.AutoRepeatingSpellId != spellIdAutoShot,
                    new Action(ret =>
                        {
                            StyxWoW.Me.ToggleAttack();
                            return RunStatus.Failure;
                        })),
                new Decorator(
                    ret => includePet && StyxWoW.Me.GotAlivePet && (StyxWoW.Me.Pet.CurrentTarget == null || StyxWoW.Me.Pet.CurrentTarget != StyxWoW.Me.CurrentTarget),
                    new Action(
                        delegate
                        {
                            PetManager.CastPetAction("Attack");
                            return RunStatus.Failure;
                        }))
                );
        }

        /// <summary>
        ///  Creates a behavior to start shooting current target with the wand.
        /// </summary>
        /// <remarks>
        ///  Created 23/05/2011
        /// </remarks>
        /// <returns></returns>
        public static Composite CreateUseWand()
        {
            return CreateUseWand(ret => true);
        }

        /// <summary>
        ///  Creates a behavior to start shooting current target with the wand if extra conditions are met.
        /// </summary>
        /// <param name="extra"> Extra conditions to check to start shooting. </param>
        /// <returns></returns>
        public static Composite CreateUseWand(SimpleBooleanDelegate extra)
        {
            return new PrioritySelector(
                new Decorator(
                    ret => Item.HasWand && !StyxWoW.Me.IsWanding() && extra(ret),
                    new Action(ret => SpellManager.Cast("Shoot")))
                );
        }

        /// <summary>Creates an interrupt spell cast composite. This will attempt to use racials before any class/spec abilities. It will attempt to stun if possible!</summary>
        /// <remarks>Created 9/7/2011.</remarks>
        /// <param name="onUnit">The on unit.</param>
        /// <returns>.</returns>
        public static Composite CreateInterruptSpellCast(UnitSelectionDelegate onUnit)
        {
            return
                new Decorator(
                    // If the target is casting, and can actually be interrupted, AND we've waited out the double-interrupt timer, then find something to interrupt with.
                    ret => onUnit != null && onUnit(ret) != null && onUnit(ret).IsCasting && onUnit(ret).CanInterruptCurrentSpellCast
                    /* && PreventDoubleInterrupt*/,
                    new PrioritySelector(
                        Spell.Cast("Rebuke", onUnit),
                        Spell.Cast("Avenger's Shield", onUnit),
                        Spell.Cast("Hammer of Justice", onUnit),
                        Spell.Cast("Repentance", onUnit, 
                            ret =>  onUnit(ret).IsPlayer || onUnit(ret).IsDemon || onUnit(ret).IsHumanoid || 
                                    onUnit(ret).IsDragon || onUnit(ret).IsGiant || onUnit(ret).IsUndead),

                        Spell.Cast("Kick", onUnit),
                        Spell.Cast("Gouge", onUnit, ret => !onUnit(ret).IsBoss() && !onUnit(ret).MeIsSafelyBehind), // Can't gouge bosses.

                        Spell.Cast("Counterspell", onUnit),

                        Spell.Cast("Wind Shear", onUnit),

                        Spell.Cast("Pummel", onUnit),
                        // Gag Order only works on non-bosses due to it being a silence, not an interrupt!
                        Spell.Cast("Heroic Throw", onUnit, ret => TalentManager.GetCount(3, 7) == 2 && !onUnit(ret).IsBoss()),

                        Spell.Cast("Silence", onUnit),

                        Spell.Cast("Silencing Shot", onUnit),

                        // Can't stun most bosses. So use it on trash, etc.
                        Spell.Cast("Bash", onUnit, ret => !onUnit(ret).IsBoss()),
                        Spell.Cast("Skull Bash (Cat)", onUnit, ret => StyxWoW.Me.Shapeshift == ShapeshiftForm.Cat),
                        Spell.Cast("Skull Bash (Bear)", onUnit, ret => StyxWoW.Me.Shapeshift == ShapeshiftForm.Bear),
                        Spell.Cast("Solar Beam", onUnit, ret => StyxWoW.Me.Shapeshift == ShapeshiftForm.Moonkin),

                        Spell.Cast("Strangulate", onUnit),
                        Spell.Cast("Mind Freeze", onUnit),


                        // Racials last.
                        Spell.Cast("Arcane Torrent", onUnit),
                        // Don't waste stomp on bosses. They can't be stunned 99% of the time!
                        Spell.Cast("War Stomp", onUnit, ret => !onUnit(ret).IsBoss() && onUnit(ret).Distance < 8)
                        ));
        }

        private static readonly WaitTimer InterruptTimer = new WaitTimer(TimeSpan.FromMilliseconds(500));

        private static bool PreventDoubleInterrupt
        {
            get
            {
                var tmp = InterruptTimer.IsFinished;
                if (tmp)
                    InterruptTimer.Reset();
                return tmp;
            }
        }
    }
}
