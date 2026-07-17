namespace Core.Enums
{
    public enum EquipmentPiece : byte
    {
        Body,
        Belt,
        Gloves,
        Boots,
        Helmet,
        Amulet,
        Cloak,
        Weapon,
        Ring,

        /// <summary>The second ring SLOT of the paperdoll. Items never carry this piece — a ring
        /// item is always <see cref="Ring"/>; the equipment component resolves which slot it lands in.</summary>
        Ring2,
    }
}
