namespace Core.Views.UI
{
    using System;

    /// <summary>
    /// The game situation the UI lives in — exactly one is active at a time. Windows declare the
    /// contexts they are available in at factory registration (flags); everything else about
    /// gating is the manager's job, not scattered ifs.
    /// </summary>
    [Flags]
    public enum UiContext
    {
        None = 0,

        /// <summary>Walking the world — the default.</summary>
        World = 1 << 0,

        /// <summary>A battle is running (from BattleInitialized to BattleEnd).</summary>
        Battle = 1 << 1,

        /// <summary>A conversation is open; the world doesn't wait, but menus do.</summary>
        Dialogue = 1 << 2,

        /// <summary>The player lies defeated in the world, waiting to revive or burn.</summary>
        Defeated = 1 << 3,

        /// <summary>Final death: only the game-over flow remains.</summary>
        GameOver = 1 << 4,

        All = World | Battle | Dialogue | Defeated | GameOver,
    }
}
