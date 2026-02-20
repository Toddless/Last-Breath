namespace Battle.Internal.NPC
{
    using Core.Enums;
    using Core.Interfaces.Entity;
    using Core.Interfaces.UI;
    using Godot;

    [GlobalClass]
    internal partial class Bat : Enemy, INpc, IInitializable
    {
        private const string UID = "uid://bssmtdwwycbpt";
        public int Level { get; }
        public Rarity Rarity { get; }
        [Export] public EntityType EntityType { get; private set; }
        [Export] public Fractions Fraction { get; private set; }
        public INpcModifiersComponent NpcModifiers { get; }
        public static PackedScene Initialize() =>ResourceLoader.Load<PackedScene>(UID);
    }
}
