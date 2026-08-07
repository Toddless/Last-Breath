namespace Battle.Source.Abilities
{
    using System;
    using System.Collections.Generic;
    using Core.Battle.Abilities;
    using Core.Entity.Components;
    using Core.Services;
    using Core.Session;

    /// <inheritdoc cref="IAbilityAugmentBinder"/>
    /// <param name="players">Whose book is dressed. No player, nothing to dress — the board keeps its
    /// arrangement and the first fighter to appear is bound by the pass that announces him.</param>
    /// <param name="sockets">The arrangement itself: the only thing that decides what an ability wears.</param>
    /// <param name="abilities">Turns the seated copy into the upgrade it installs, with that copy's
    /// own numbers rather than the record's averages.</param>
    public sealed class AbilityAugmentBinder(
        IPlayerAccessor players,
        IAbilitySocketBoard sockets,
        IAbilityProvider abilities)
        : IAbilityAugmentBinder, ISessionResettable
    {
        public void Bind()
        {
            IAbilityBookComponent? book = players.Player?.AbilityBook;
            if (book == null) return;

            foreach (IAbility ability in book.AllAbilities)
                ability.InstallUpgrades(SeatedOn(ability.Id));
        }

        /// <summary>A new playthrough empties the board, and the abilities of the one being left behind
        /// must not keep wearing what it held. Registered after the board itself, so the pass reads a
        /// board that has already been cleared (see <c>SessionResetDependencies</c>).</summary>
        public void ResetSession() => Bind();

        /// <summary>The upgrades one ability wears, read off its own slots. A socket standing empty
        /// contributes nothing, and so does one holding a copy this build can make no upgrade of — the
        /// silence is reported where the upgrade is built, and an ability is never left wearing half an
        /// arrangement because of it.</summary>
        private Dictionary<string, IAbilityUpgrade> SeatedOn(string abilityId)
        {
            Dictionary<string, IAbilityUpgrade> seated = new(StringComparer.Ordinal);

            foreach (AbilitySocket socket in sockets.SocketsOf(abilityId))
                if (socket.Augment is { } augment && abilities.CreateUpgrade(augment) is { } upgrade)
                    seated[socket.SocketId] = upgrade;

            return seated;
        }
    }
}
