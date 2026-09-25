namespace Core.Items
{
    using Entity;

    /// <summary>A secondary capability an item confers on its wearer (modifiers, passive skill, proc, ability).
    /// Attached on equip, detached on unequip.</summary>
    public interface IItemGrant
    {
        string Id { get; }
        string Description { get; }

        void Attach(IFightable owner);
        void Detach(IFightable owner);
        IItemGrant Copy();

        /// <summary>A copy with every numeric value scaled by <paramref name="factor"/> — the ascension
        /// "+15% to everything" applied to grants. One-time by design: the scaled numbers become the new
        /// base and round-trip through save as such (grants deliberately do not scale with sharpening).</summary>
        IItemGrant WithScaledValues(float factor);
    }
}
