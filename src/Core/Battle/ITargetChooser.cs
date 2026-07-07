namespace Core.Battle
{
    using System.Collections.Generic;
    using Entity;

    public interface ITargetChooser
    {
        IFightable Choose(List<IFightable> targets);
    }
}
