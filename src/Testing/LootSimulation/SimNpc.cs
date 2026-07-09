namespace LastBreathTest.LootSimulation
{
    using Core.Components.NpcModifiers;
    using Core.Entity;
    using Moq;

    /// <summary>Builds a minimal <see cref="IFightableNpc"/> for the drop pipeline: the loot code
    /// only reads identity, level, rarity, type, fraction and the (real) modifiers component.</summary>
    internal static class SimNpc
    {
        public static IFightableNpc Create(NpcArchetype archetype, IEnumerable<INpcModifier> modifiers)
        {
            var mock = new Mock<IFightableNpc>();
            mock.SetupGet(npc => npc.Id).Returns(archetype.Name);
            mock.SetupGet(npc => npc.InstanceId).Returns(Guid.NewGuid().ToString());
            mock.SetupGet(npc => npc.Level).Returns(archetype.Level);
            mock.SetupGet(npc => npc.Rarity).Returns(archetype.Rarity);
            mock.SetupGet(npc => npc.EntityType).Returns(archetype.EntityType);
            mock.SetupGet(npc => npc.Fraction).Returns(archetype.Fraction);

            var component = new NpcModifiersComponent(mock.Object);
            mock.SetupGet(npc => npc.NpcModifiers).Returns(component);
            component.AddModifiers(modifiers.Select(modifier => modifier.Copy()).ToList());

            return mock.Object;
        }
    }
}
