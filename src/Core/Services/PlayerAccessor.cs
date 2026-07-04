namespace Core.Services
{
    using System;
    using Interfaces;

    public class PlayerAccessor : IPlayerAccessor
    {
        public IPlayer? Player { get; private set; }

        public event Action<IPlayer>? PlayerChanged;

        public void Set(IPlayer player)
        {
            if (ReferenceEquals(Player, player)) return;
            Player = player;
            PlayerChanged?.Invoke(player);
        }
    }
}
