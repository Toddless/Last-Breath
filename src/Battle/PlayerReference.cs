namespace Battle
{
    using System;
    using Source;

    internal class PlayerReference
    {
        private Player? _player;

        public void SetPlayerReference(Player player) => _player = player;

        public Player GetPlayer() => _player ?? throw new ArgumentNullException(nameof(_player));
    }
}
