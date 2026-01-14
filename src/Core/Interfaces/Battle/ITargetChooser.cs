namespace Core.Interfaces.Battle
{
    using System.Collections.Generic;
    using Entity;

    public interface ITargetChooser
    {
        IEntity Choose(List<IEntity> targets);
    }
}
