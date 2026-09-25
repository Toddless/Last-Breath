namespace Core.MessageBus.Messages
{
    using Enums;

    /// <summary>Something in the world (a talk interaction, later a zone trigger) wants a
    /// conversation. The handler starts the dialogue service and opens the window.</summary>
    public record OpenDialogueMessage(string NpcId, string? NpcInstanceId, Fractions Faction) : IMessage;
}
