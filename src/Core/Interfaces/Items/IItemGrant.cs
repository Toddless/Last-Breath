namespace Core.Interfaces.Items
{
    using Entity;

    /// <summary>A secondary capability an item confers on its wearer (modifiers, passive skill, proc, ability).
    /// Attached on equip, detached on unequip.</summary>
    public interface IItemGrant
    {
        string Id { get; }

        void Attach(IFightable owner);
        void Detach(IFightable owner);
        IItemGrant Copy();
    }
}
