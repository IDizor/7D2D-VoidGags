using System.Collections.Generic;

namespace VoidGags
{
    public class VoidGagsCmdCompleteQuest : ConsoleCmdAbstract
    {
        public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
        {
            var player = Helper.PlayerLocal;
            if (player == null) return;
            if (!GamePrefs.GetBool(EnumGamePrefs.DebugMenuEnabled))
            {
                VoidGags.LogModWarning("Debug Menu is not activated to use this command.");
                return;
            }
            var quest = player.QuestJournal.ActiveQuest ?? player.QuestJournal.TrackedQuest;
            if (quest == null)
            {
                VoidGags.LogModWarning("No active or tracked quest.");
                return;
            }
            if (quest.CurrentState == Quest.QuestState.Completed || quest.CurrentState == Quest.QuestState.Failed)
            {
                VoidGags.LogModWarning("Cannot complete failed or already completed quest.");
                return;
            }
                    
            quest.Objectives.ForEach(o =>
            {
                //VoidGags.LogModWarning($"Objective: {o.GetType().Name} - {o.Description}");
                if (o.ObjectiveState != BaseObjective.ObjectiveStates.Complete &&
                    o is not ObjectiveReturnToNPC && o is not ObjectiveInteractWithNPC)
                {
                    o.ChangeStatus(isSuccess: true);
                }
            });
            VoidGags.LogModWarning("Forced completion of the quest: " + quest.questClass?.Name);
        }

        public override string[] getCommands()
        {
            return ["completequest"];
        }

        public override string getDescription()
        {
            return "Complete current active or tracked quest. (VoidGags)";
        }
    }
}
