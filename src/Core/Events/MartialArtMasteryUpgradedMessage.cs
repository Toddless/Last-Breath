namespace Core.Events
{
    public record MartialArtMasteryUpgradedMessage(int Level) : IMessage;
}
