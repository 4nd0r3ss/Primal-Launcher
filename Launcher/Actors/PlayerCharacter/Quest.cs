﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Sockets;

namespace Launcher
{
    [Serializable]
    public class Quest
    {
        public uint Id { get; set; } = 0x0001ADB1;
        public uint Region { get; set; }
        public OrderedDictionary Phases { get; set; } = new OrderedDictionary();
        public byte PhaseIndex { get; set; } = 0;

        public QuestPhase CurrentPhase
        {
            get
            {
                return ((QuestPhase)Phases[PhaseIndex]);
            }
        }

        public Quest()
        {
            Phases.Add(0, new QuestPhase
            {
                FinishCondition = "allStepsDone",
                Steps = new List<QuestPhaseStep>
                {
                    new QuestPhaseStep{ActorClassId = 1001652, Type = "questIcon", Value = "2"},
                    new QuestPhaseStep{ActorClassId = 1000442, Type = "questIcon", Value = "2"},
                    new QuestPhaseStep{ActorClassId = 1000447, Type = "questIcon", Value = "2"},                   
                    new QuestPhaseStep{ActorClassId = 1001652, Type = "pushDefault", Value = "processTtrNomal002", OnExecute = "Enabled:0"},
                    new QuestPhaseStep{ActorClassId = 1001652, Type = "talkDefault", Value = "processTtrNomal003"}
                }
            });

            Phases.Add(1, new QuestPhase
            {
                FinishCondition = "allStepsDone",
                Steps = new List<QuestPhaseStep>
                {
                    new QuestPhaseStep{ActorClassId = 1001652, Type = "talkDefault", Value = "processTtrMini001", OnExecute = "QuestIcon:0"},
                    new QuestPhaseStep{ActorClassId = 1000442, Type = "talkDefault", Value = "processTtrMini002", OnExecute = "QuestIcon:0"},
                    new QuestPhaseStep{ActorClassId = 1000447, Type = "talkDefault", Value = "processTtrMini003", OnExecute = "QuestIcon:0"}
                }
            });

            Phases.Add(2, new QuestPhase
            {
                FinishCondition = "allStepsDone",
                Steps = new List<QuestPhaseStep>
                {
                     new QuestPhaseStep{ActorClassId = 1090025, Type = "questIcon", Value = "3"},
                     new QuestPhaseStep{ActorClassId = 1090025, Type = "pushDefault", Value = "processEventNewRectAsk", Repeatable = true}
                }
            });

        }

        public void StartPhase(Socket sender) => CurrentPhase.Start(sender);
       

        public QuestPhaseStep SearchActorStep(uint actorClassId, string type)
        {
            QuestPhaseStep stepForActor = null;           

            foreach (QuestPhaseStep step in CurrentPhase.Steps)
            {
                if (step.ActorClassId == actorClassId && step.Type == type && step.Done == false)
                {
                    stepForActor = step;
                    break;
                }               
            }

            return stepForActor;
        }

        public void CheckPhase(Socket sender)
        {
            if(CurrentPhase.Steps.TrueForAll(x => x.Done == true) && PhaseIndex < Phases.Count-1)
            {
                PhaseIndex++;
                CurrentPhase.Start(sender);
            }
        }

        public string ActorStepComplete(Socket sender, uint classId, string eventName)
        {            
             QuestPhaseStep step = SearchActorStep(classId, eventName);

             if (step != null)
             {
                step.Execute(sender);
                CheckPhase(sender);
                return step.Value;
            }
            else
            {
                return null;
            }     
        }
    }

    [Serializable]
    public class QuestPhase
    {
        public List<QuestPhaseStep> Steps { get; set; } = new List<QuestPhaseStep>();
        public object FinishCondition { get; set; }

        public void Start(Socket sender)
        {
            foreach(var step in Steps)
            {
                Populace actorRef = (Populace)World.Instance.Zones.Find(x => x.Id == User.Instance.Character.Position.ZoneId).Actors.Find(x => x.ClassId == step.ActorClassId);

                switch (step.Type)
                {
                    case "questIcon":
                        actorRef.QuestIcon = Convert.ToInt32(step.Value);
                        if(actorRef.Spawned) actorRef.SetQuestIcon(sender);
                        step.Done = true;
                        break;
                    case "pushDefault":
                    case "talkDefault":
                        actorRef.Events.Find(x => x.EventName == step.Type).Enabled = 1;
                        if (actorRef.Spawned) actorRef.SetEventStatus(sender);
                        break;
                    

                }                
            }            
        }
    }

    [Serializable]
    public class QuestPhaseStep
    {
        public uint ActorClassId { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public string OnExecute { get; set; }
        public bool Done { get; set; }
        public bool Repeatable { get; set; }

        public void Execute(Socket sender)
        {
            if (!string.IsNullOrEmpty(OnExecute))
            {
                Populace actorRef = (Populace)World.Instance.Zones.Find(x => x.Id == User.Instance.Character.Position.ZoneId).Actors.Find(x => x.ClassId == ActorClassId);

                if (OnExecute.IndexOf(":") > 0)
                {
                    string[] command = OnExecute.Split(new[] { ':' });                    

                    switch (command[0])
                    {
                        case "Enabled":
                            typeof(Event).GetProperty(command[0]).SetValue(actorRef.Events.Find(x => x.EventName == Type), Convert.ToByte(command[1]));
                            actorRef.SetEventStatus(sender);
                            break;
                        case "QuestIcon":
                            actorRef.QuestIcon = Convert.ToInt32(command[1]);
                            actorRef.SetQuestIcon(sender);
                            break;
                    }
                    
                }
            }

            if(!Repeatable) Done = true;
        }
    }
}