namespace Core.Interfaces.Events
{
    public record MartialArtMasteryUpgradedMessage(int Level) : IMessage;
}
