namespace Battle.Source
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Interfaces;
    using Core.Interfaces.Battle;
    using Core.Interfaces.Entity;

    public class ChoosePlayerAsTarget : ITargetChooser
    {
        public IEntity Choose(List<IEntity> targets) => targets.First(x => x is IPlayer);
    }
}
