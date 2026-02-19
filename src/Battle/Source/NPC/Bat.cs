namespace Battle.Source.NPC
{
    using Godot;
    using Core.Enums;
    using Core.Interfaces.UI;
    using Core.Interfaces.Entity;

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
