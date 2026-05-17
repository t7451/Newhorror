namespace HorrorCoopGame.Game
{
    /// <summary>
    /// High-level networked game phase. Replicated by <see cref="GameManager"/>
    /// and consumed by the HUD and other gameplay systems.
    /// </summary>
    public enum GamePhase : byte
    {
        Lobby = 0,
        Playing = 1,
        Victory = 2,
        Defeat = 3
    }
}
