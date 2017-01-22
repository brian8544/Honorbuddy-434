// Behavior originally contributed by Nesox.
//
// DOCUMENTATION:
//     http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Custom_Behavior:_UseItemOn
//
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

using Styx.Helpers;
using Styx.Logic.BehaviorTree;
using Styx.Logic.Combat;
using Styx.Logic.Pathing;
using Styx.Logic.Questing;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;

using TreeSharp;
using Action = TreeSharp.Action;


namespace Styx.Bot.Quest_Behaviors
{
    /// <summary>
    /// Allows you to use items on nearby gameobjects/npc's
    /// ##Syntax##
    /// QuestId: The id of the quest.
    /// MobId1, MobId2, ...MobIdN: The ids of the mobs.
    /// ItemId: The id of the item to use.
    /// [Optional]NumOfTimes: Number of times to use said item.
    /// [Optional]WaitTime: Time to wait after using an item. DefaultValue: 1500 ms
    /// [Optional]CollectionDistance: The distance it will use to collect objects. DefaultValue:100 yards
    /// [Optional]HasAura: If a unit has a certian aura to check before using item. (By: j0achim)
    /// [Optional]Range: The range to object that it will use the item
    /// [Optional]MobState: The state of the npc -> Dead, Alive, BelowHp. None is default
    /// [Optional]MobHpPercentLeft: Will only be used when NpcState is BelowHp
    /// ObjectType: the type of object to interact with, expected value: Npc/Gameobject
    /// [Optional]X,Y,Z: The general location where theese objects can be found
    /// </summary>
    public class UseItemOn : CustomForcedBehavior
    {
        public enum ObjectType
        {
            Npc,
            GameObject,
        }

        public enum NpcStateType
        {
            Alive,
            BelowHp,
            Dead,
            DontCare,
        }

        public enum NavigationType
        {
            Mesh,
            CTM,
            None,
        }

        public UseItemOn(Dictionary<string, string> args)
            : base(args)
        {
            try
            {
                int tmpMobHasAuraId;
                int tmpMobHasAuraMissingId;

                // QuestRequirement* attributes are explained here...
                //    http://www.thebuddyforum.com/mediawiki/index.php?title=Honorbuddy_Programming_Cookbook:_QuestId_for_Custom_Behaviors
                // ...and also used for IsDone processing.
                CollectionDistance = GetAttributeAsNullable<double>("CollectionDistance", false, ConstrainAs.Range, null) ?? 100.0;
                tmpMobHasAuraId = GetAttributeAsNullable<int>("HasAuraId", false, ConstrainAs.AuraId, new[] { "HasAura" }) ?? 0;
                tmpMobHasAuraMissingId = GetAttributeAsNullable<int>("IsMissingAuraId", false, ConstrainAs.AuraId, null) ?? 0;
                MobHpPercentLeft = GetAttributeAsNullable<double>("MobHpPercentLeft", false, ConstrainAs.Percent, new[] { "HpLeftAmount" }) ?? 100.0;
                ItemId = GetAttributeAsNullable<int>("ItemId", true, ConstrainAs.ItemId, null) ?? 0;
                Location = GetAttributeAsNullable<WoWPoint>("", false, ConstrainAs.WoWPointNonEmpty, null) ?? Me.Location;
                MobIds = GetNumberedAttributesAsArray<int>("MobId", 1, ConstrainAs.MobId, new[] { "NpcId" });
                MobType = GetAttributeAsNullable<ObjectType>("MobType", false, null, new[] { "ObjectType" }) ?? ObjectType.Npc;
                NumOfTimes = GetAttributeAsNullable<int>("NumOfTimes", false, ConstrainAs.RepeatCount, null) ?? 1;
                NpcState = GetAttributeAsNullable<NpcStateType>("MobState", false, null, new[] { "NpcState" }) ?? NpcStateType.DontCare;
                NavigationState = GetAttributeAsNullable<NavigationType>("Nav", false, null, new[] { "Navigation" }) ?? NavigationType.Mesh;
                WaitForNpcs = GetAttributeAsNullable<bool>("WaitForNpcs", false, null, null) ?? false;
                Range = GetAttributeAsNullable<double>("Range", false, ConstrainAs.Range, null) ?? 4;
                QuestId = GetAttributeAsNullable<int>("QuestId", false, ConstrainAs.QuestId(this), null) ?? 0;
                QuestRequirementComplete = GetAttributeAsNullable<QuestCompleteRequirement>("QuestCompleteRequirement", false, null, null) ?? QuestCompleteRequirement.NotComplete;
                QuestRequirementInLog = GetAttributeAsNullable<QuestInLogRequirement>("QuestInLogRequirement", false, null, null) ?? QuestInLogRequirement.InLog;
                WaitTime = GetAttributeAsNullable<int>("WaitTime", false, ConstrainAs.Milliseconds, null) ?? 1500;

                MobAuraName = (tmpMobHasAuraId != 0) ? AuraNameFromId("HasAuraId", tmpMobHasAuraId) : null;
                MobAuraMissingName = (tmpMobHasAuraMissingId != 0) ? AuraNameFromId("HasAuraId", tmpMobHasAuraMissingId) : null;
            }

            catch (Exception except)
            {
                // Maintenance problems occur for a number of reasons.  The primary two are...
                // * Changes were made to the behavior, and boundary conditions weren't properly tested.
                // * The Honorbuddy core was changed, and the behavior wasn't adjusted for the new changes.
                // In any case, we pinpoint the source of the problem area here, and hopefully it
                // can be quickly resolved.
                LogMessage("error", "BEHAVIOR MAINTENANCE PROBLEM: " + except.Message
                                    + "\nFROM HERE:\n"
                                    + except.StackTrace + "\n");
                IsAttributeProblem = true;
            }
        }


        // Attributes provided by caller
        public double CollectionDistance { get; private set; }
        public int ItemId { get; private set; }
        public WoWPoint Location { get; private set; }
        public string MobAuraName { get; private set; }
        public string MobAuraMissingName { get; private set; }
        public double MobHpPercentLeft { get; private set; }
        public int[] MobIds { get; private set; }
        public ObjectType MobType { get; private set; }
        public NpcStateType NpcState { get; private set; }
        public NavigationType NavigationState { get; private set; }
        public int NumOfTimes { get; private set; }
        public int QuestId { get; private set; }
        public QuestCompleteRequirement QuestRequirementComplete { get; private set; }
        public QuestInLogRequirement QuestRequirementInLog { get; private set; }
        public double Range { get; private set; }
        public bool WaitForNpcs { get; private set; }
        public int WaitTime { get; private set; }

        // Private variables for internal state
        private bool _isBehaviorDone;
        private bool _isDisposed;
        private readonly List<ulong> _npcAuraWait = new List<ulong>();
        private readonly List<ulong> _npcBlacklist = new List<ulong>();
        private Composite _root;

        // Private properties
        private int Counter { get; set; }
        private LocalPlayer Me { get { return (ObjectManager.Me); } }

        // DON'T EDIT THESE--they are auto-populated by Subversion
        public override string SubversionId { get { return ("$Id: UseItemOn.cs 217 2012-02-11 16:52:02Z Nesox $"); } }
        public override string SubversionRevision { get { return ("$Revision: 217 $"); } }


        ~UseItemOn()
        {
            Dispose(false);
        }


        public void Dispose(bool isExplicitlyInitiatedDispose)
        {
            if (!_isDisposed)
            {
                // NOTE: we should call any Dispose() method for any managed or unmanaged
                // resource, if that resource provides a Dispose() method.

                // Clean up managed resources, if explicit disposal...
                if (isExplicitlyInitiatedDispose)
                {
                    // empty, for now
                }

                // Clean up unmanaged resources (if any) here...
                TreeRoot.GoalText = string.Empty;
                TreeRoot.StatusText = string.Empty;

                // Call parent Dispose() (if it exists) here ...
                base.Dispose();
            }

            _isDisposed = true;
        }


        // May return 'null' if auraId is not valid.
        private string AuraNameFromId(string attributeName,
                                           int auraId)
        {
            string tmpString = null;

            try
            {
                tmpString = WoWSpell.FromId(auraId).Name;
            }
            catch
            {
                LogMessage("fatal", "Could not find {0}({0}).", attributeName, auraId);
                IsAttributeProblem = true;
            }

            return (tmpString);
        }


        /// <summary> Current object we should interact with.</summary>
        /// <value> The object.</value>
        private WoWObject CurrentObject
        {
            get
            {
                WoWObject @object = null;

                switch (MobType)
                {
                    case ObjectType.GameObject:
                        @object = ObjectManager.GetObjectsOfType<WoWGameObject>()
                                                .OrderBy(ret => ret.Distance)
                                                .FirstOrDefault(obj => !_npcBlacklist.Contains(obj.Guid)
                                                                        && obj.Distance < CollectionDistance
                                                                        && MobIds.Contains((int)obj.Entry));
                        break;

                    case ObjectType.Npc:
                        var baseTargets = ObjectManager.GetObjectsOfType<WoWUnit>()
                                                               .OrderBy(target => target.Distance)
                                                               .Where(target => !_npcBlacklist.Contains(target.Guid)
                                                                                && (target.Distance < CollectionDistance)
                                                                                && MobIds.Contains((int)target.Entry));

                        var auraQualifiedTargets = baseTargets
                                                            .Where(target => (((MobAuraName == null) && (MobAuraMissingName == null))
                                                                              || ((MobAuraName != null) && target.HasAura(MobAuraName))
                                                                              || ((MobAuraMissingName != null) && !target.HasAura(MobAuraMissingName))));

                        var npcStateQualifiedTargets = auraQualifiedTargets
                                                            .Where(target => ((NpcState == NpcStateType.DontCare)
                                                                              || ((NpcState == NpcStateType.Dead) && target.Dead)
                                                                              || ((NpcState == NpcStateType.Alive) && target.IsAlive)
                                                                              || ((NpcState == NpcStateType.BelowHp) && target.IsAlive && (target.HealthPercent < MobHpPercentLeft))));

                        @object = npcStateQualifiedTargets.FirstOrDefault();
                        break;
                }

                if (@object != null)
                { LogMessage("debug", @object.Name); }

                return @object;
            }
        }

        public WoWItem Item
        {
            get
            {
                return StyxWoW.Me.CarriedItems.FirstOrDefault(ret => ret.Entry == ItemId);
            }
        }

        #region Overrides of CustomForcedBehavior

        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
            new PrioritySelector(

                new Decorator(ret => Counter >= NumOfTimes,
                    new Action(ret => _isBehaviorDone = true)),

                    new PrioritySelector(
                        new Decorator(ret => CurrentObject != null && CurrentObject.DistanceSqr > Range * Range,
                            new Switch<NavigationType>(ret => NavigationState,
                                new SwitchArgument<NavigationType>(
                                    NavigationType.CTM,
                                    new Sequence(
                                        new Action(ret => { TreeRoot.StatusText = "Moving to use item on - " + CurrentObject.Name; }),
                                        new Action(ret => WoWMovement.ClickToMove(CurrentObject.Location))
                                    )),
                                new SwitchArgument<NavigationType>(
                                    NavigationType.Mesh,
                                    new Sequence(
                                        new Action(delegate { TreeRoot.StatusText = "Moving to use item on \"" + CurrentObject.Name + "\""; }),
                                        new Action(ret => Navigator.MoveTo(CurrentObject.Location))
                                        )),
                                new SwitchArgument<NavigationType>(
                                    NavigationType.None,
                                    new Sequence(
                                        new Action(ret => { TreeRoot.StatusText = "Object is out of range, Skipping - " + CurrentObject.Name + " Distance: " + CurrentObject.Distance; }),
                                        new Action(ret => _isBehaviorDone = true)
                                    )))),

                        new Decorator(ret => CurrentObject != null && CurrentObject.DistanceSqr <= Range * Range && Item != null && Item.Cooldown == 0,
                            new Sequence(
                                new DecoratorContinue(ret => StyxWoW.Me.IsMoving,
                                    new Action(ret =>
                                    {
                                        WoWMovement.MoveStop();
                                        StyxWoW.SleepForLagDuration();
                                    })),

                                new Action(ret =>
                                {
                                    bool targeted = false;
                                    TreeRoot.StatusText = "Using item on \"" + CurrentObject.Name + "\"";
                                    if (CurrentObject is WoWUnit && (StyxWoW.Me.CurrentTarget == null || StyxWoW.Me.CurrentTarget != CurrentObject))
                                    {
                                        (CurrentObject as WoWUnit).Target();
                                        targeted = true;
                                        StyxWoW.SleepForLagDuration();
                                    }

                                    WoWMovement.Face(CurrentObject.Guid);

                                    Item.UseContainerItem();
                                    _npcBlacklist.Add(CurrentObject.Guid);

                                    StyxWoW.SleepForLagDuration();
                                    Counter++;

                                    if (WaitTime < 100)
                                        WaitTime = 100;

                                    if (WaitTime > 100)
                                    {
                                        if (targeted)
                                            StyxWoW.Me.ClearTarget();
                                    }

                                    Thread.Sleep(WaitTime);
                                }))
                                    ),

                            new Decorator(
                                ret => Location.DistanceSqr(Me.Location) > 2 * 2,
                                new Sequence(
                                    new Action(delegate { TreeRoot.StatusText = "Moving to location " + Location; }),
                                    new Action(ret => Navigator.MoveTo(Location)))),

                            new Decorator(
                                 ret => !WaitForNpcs && CurrentObject == null,
                                 new Action(ret => _isBehaviorDone = true)),

                            new Action(ret => TreeRoot.StatusText = "Waiting for object to spawn")
                )));
        }


        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        public override bool IsDone
        {
            get
            {
                return (_isBehaviorDone     // normal completion
                        || !UtilIsProgressRequirementsMet(QuestId, QuestRequirementInLog, QuestRequirementComplete));
            }
        }

        public override void OnStart()
        {
            // This reports problems, and stops BT processing if there was a problem with attributes...
            // We had to defer this action, as the 'profile line number' is not available during the element's
            // constructor call.
            OnStart_HandleAttributeProblem();

            // If the quest is complete, this behavior is already done...
            // So we don't want to falsely inform the user of things that will be skipped.
            if (!IsDone)
            {
                PlayerQuest quest = StyxWoW.Me.QuestLog.GetQuestById((uint)QuestId);

                TreeRoot.GoalText = this.GetType().Name + ": " + ((quest != null) ? ("\"" + quest.Name + "\"") : "In Progress");
            }
        }

        #endregion
    }
}