namespace Battle.Source
{
    using System.Threading.Tasks;
    using Core.Battle.Abilities;
    using Core.Events;
    using Core.Services;

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
