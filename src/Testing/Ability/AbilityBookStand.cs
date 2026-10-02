namespace LastBreathTest.Ability
{
    using Core.Entity;
    using Core.Entity.Components;
    using Core.Services;
    using Moq;

    /// <summary>
    /// A player holding an ability book, for walks that need one to exist rather than to fight. The
    /// save sections that speak of the book read it off the accessor, so a case about the sockets or
    /// the bag still has to stand a player up before the file can be written at all.
    /// </summary>
    internal static class AbilityBookStand
    {
        internal static AbilityBookComponent NewBook() => new(new Mock<IFightable>().Object);

        internal static IPlayerAccessor AccessorFor(IAbilityBookComponent book)
        {
            var player = new Mock<IPlayer>();
            player.SetupGet(fighter => fighter.AbilityBook).Returns(book);
            var accessor = new PlayerAccessor();
            accessor.Set(player.Object);
            return accessor;
        }
    }
}
