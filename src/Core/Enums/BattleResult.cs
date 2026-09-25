namespace Core.Enums
{
    public enum BattleResults : byte
    {
        /// <summary>
        /// Player will get all experience for battle.
        /// </summary>
        PlayerWon,
        /// <summary>
        /// Player will get only part of the experience for battle.
        /// </summary>
        PlayerLost,
        /// <summary>
        /// Player will get nothing.
        /// </summary>
        BattleAbandoned,
        /// <summary>
        /// Player escaped alive but earned nothing.
        /// </summary>
        PlayerFled
    }
}
