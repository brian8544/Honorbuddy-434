using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Styx;
using Styx.Plugins;
using Styx.Plugins.PluginClass;
using Styx.Logic.BehaviorTree;
using TreeSharp;
using Styx.Logic.Questing;
using Styx.Logic.Profiles.Quest;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Action = TreeSharp.Action;
using Styx.Helpers;
using Styx.Logic.Pathing;
using Styx.Logic.Combat;

namespace Blastranaar
{
    public class Blastranaar:CustomForcedBehavior
    {
        public Blastranaar(Dictionary<string, string> args)
            : base(args)
        {
            try
            {
                QuestId = GetAttributeAsQuestId("QuestId", true, null) ?? 0;
            }
            catch
            {
                Logging.Write("Problem parsing a QuestId in behavior: Blastranaar");
            }
        }
        public int QuestId { get; set; }
        private bool IsBehaviorDone = false;
        public int MobIdThraka = 34429;
        public int MobIdSentinel = 34494;
        public int MobIdThrower = 34492;
        private Composite _root;
        public WoWPoint Location = new WoWPoint(3048.918, -497.9261, 205.6379);
        public QuestCompleteRequirement questCompleteRequirement = QuestCompleteRequirement.NotComplete;
        public QuestInLogRequirement questInLogRequirement = QuestInLogRequirement.InLog;
        public override bool IsDone
        {
            get
            {
                return IsBehaviorDone;
            }
        }
        public override void OnStart()
        {
            OnStart_HandleAttributeProblem();
            if (!IsDone)
            {
                PlayerQuest Quest = StyxWoW.Me.QuestLog.GetQuestById((uint)QuestId);
                TreeRoot.GoalText = ((Quest != null) ? ("\"" + Quest.Name + "\"") : "In Progress");
            }
        }
        public List<WoWUnit> Thraka
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == MobIdThraka && !u.Dead && !ObjectManager.Me.Dead && !ObjectManager.Me.Combat).OrderBy(u => u.Distance).ToList();
            }
        }
        public List<WoWUnit> Sentinels
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == MobIdSentinel && !u.Dead && u.Distance < 100).OrderBy(u => u.Distance).ToList();
            }
        }
        public List<WoWUnit> Throwers
        {
            get
            {
                return ObjectManager.GetObjectsOfType<WoWUnit>().Where(u => u.Entry == MobIdThrower && !u.Dead && u.Distance < 100).OrderBy(u => u.Distance).ToList();
            }
        }
        internal static bool InVehicle { get { return Lua.GetReturnVal<int>("if IsPossessBarVisible() or UnitInVehicle('player') then return 1 else return 0 end", 0) == 1; } }
        internal static bool SentinelsDone { get { return Lua.GetReturnVal<int>("a,b,c=GetQuestLogLeaderBoard(1,GetQuestLogIndexByID(13947));if c==1 then return 1 else return 0 end", 0) == 1; } }
        internal static bool ThrowersDone { get { return Lua.GetReturnVal<int>("a,b,c=GetQuestLogLeaderBoard(2,GetQuestLogIndexByID(13947));if c==1 then return 1 else return 0 end", 0) == 1; } }

        protected override Composite CreateBehavior()
        {
            return _root ?? (_root =
                new PrioritySelector(
                    new Decorator(ret => !UtilIsProgressRequirementsMet(QuestId, questInLogRequirement, questCompleteRequirement),
                        new Sequence(
                            new Action(ret => TreeRoot.StatusText = "Finished!"),
                            new Action(ret => Lua.DoString("RunMacroText('/click VehicleMenuBarActionButton3','0')")),
                            new WaitContinue(120,
                            new Action(delegate
                            {
                                IsBehaviorDone = true;
                                return RunStatus.Success;
                            }))
                            )),
                    new Decorator(ret =>
                        !InVehicle && Thraka.Count < 1 || !InVehicle && Thraka[0].Distance > 5,
                        new Sequence(
                            new Action(ret => Navigator.MoveTo(Location)),
                            new Action(ret => Thread.Sleep(100))
                            )),
                    new Decorator(ret =>
                        !InVehicle && Thraka.Count > 0 && Thraka[0].Distance < 6,
                        new Sequence(
                            new Action(ret => Navigator.PlayerMover.MoveStop()),
                            new Action(ret => Thraka[0].Interact()),
                            new Action(ret => Thread.Sleep(500)),
                            new Action(ret => Lua.DoString("SelectGossipOption(2)"))
                            )),
                    new Decorator(ret =>
                        InVehicle && !SentinelsDone && Sentinels.Count > 0,
                        new Sequence(
                            new Action(ret => Sentinels[0].Target()),
                            new Action(ret => Lua.DoString("RunMacroText('/click VehicleMenuBarActionButton1','0')")),
                            new Action(ret => LegacySpellManager.ClickRemoteLocation(Sentinels[0].Location)),
                            new Action(ret => Thread.Sleep(3000))
                            )),
                    new Decorator(ret =>
                        InVehicle && !ThrowersDone && Throwers.Count > 0,
                        new Sequence(
                            new Action(ret => Throwers[0].Target()),
                            new Action(ret => Lua.DoString("RunMacroText('/click VehicleMenuBarActionButton1','0')")),
                            new Action(ret => LegacySpellManager.ClickRemoteLocation(Throwers[0].Location)),
                            new Action(ret => Thread.Sleep(3000))
                            ))
                   ));
        }
    }
}
