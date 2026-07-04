namespace Battle.Source
{
    using System.Threading.Tasks;
    using Core.Interfaces;
    using Core.Interfaces.Abilities;
    using Core.Interfaces.Events;

    public class MartialArtMasteryUpgradedMessageHandler(
        IPlayerAccessor Player,
        IAbilityProvider provider)
        : IMessageHandler<MartialArtMasteryUpgradedMessage>
    {
        public Task HandleMessageAsync(MartialArtMasteryUpgradedMessage message)
        {
            int newLevel = message.Level;

            return Task.CompletedTask;
        }
    }
}
