namespace LastBreath
{
    using System.Threading.Tasks;
    using Core;
    using Core.Events;
    using Core.Narrative.Dialogues;
    using Core.Views.UI;
    using UI;

    /// <summary>Starts the conversation first, opens the window second — the window renders
    /// whatever the service already holds. A failed start (no dialogue, no matching entry
    /// rule) opens nothing: the NPC simply has nothing to say.</summary>
    public class OpenDialogueMessageHandler(IDialogueService dialogue, IUiElementsManager manager) : IMessageHandler<OpenDialogueMessage>
    {
        public Task HandleMessageAsync(OpenDialogueMessage message)
        {
            if (dialogue.Start(message.NpcId, message.NpcInstanceId, message.Faction))
            {
                manager.OpenWindow(typeof(DialogueWindow));
                return Task.CompletedTask;
            }

            Tracker.TrackInfo($"Dialogue start rejected for '{message.NpcId}': no dialogue in the catalog or no matching entry rule");
            Godot.GD.Print($"Dialogue start rejected for '{message.NpcId}'");
            return Task.CompletedTask;
        }
    }
}
