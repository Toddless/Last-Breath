namespace LastBreathTest.BattleSystemTests
{
    using Core.Battle;
    using Core.Entity.Components;
    using Core.Enums;
    using Core.Modifiers.Context;
    using Moq;

    /// <summary>
    /// The semantic priority scale (2026-07-16): ModifierHandlerComponent applies a pipeline's
    /// context modifiers ordered Early → Normal → Late → Absolute regardless of insertion order.
    /// Damage conversions, when they arrive, take Absolute — nothing may run after them.
    /// </summary>
    [TestClass]
    public class ContextModifierPriorityTests
    {
        [TestMethod]
        public void Apply_RunsTheScaleInOrder_WhateverTheInsertionOrder()
        {
            var component = new ModifierHandlerComponent();
            var trace = new List<string>();

            // Deliberately scrambled insertion: the sort must own the order, not Add() call order.
            component.Add(new RecordingAttackModifier(ContextModifierPriority.Late, "late", trace));
            component.Add(new RecordingAttackModifier(ContextModifierPriority.Absolute, "absolute", trace));
            component.Add(new RecordingAttackModifier(ContextModifierPriority.Early, "early", trace));
            component.Add(new RecordingAttackModifier(ContextModifierPriority.Normal, "normal", trace));

            component.Apply(Mock.Of<IAttackContext>());

            CollectionAssert.AreEqual(new[] { "early", "normal", "late", "absolute" }, trace);
        }

        [TestMethod]
        public void Apply_SamePriority_KeepsAllModifiers()
        {
            var component = new ModifierHandlerComponent();
            var trace = new List<string>();
            component.Add(new RecordingAttackModifier(ContextModifierPriority.Normal, "first", trace));
            component.Add(new RecordingAttackModifier(ContextModifierPriority.Normal, "second", trace));

            component.Apply(Mock.Of<IAttackContext>());

            Assert.AreEqual(2, trace.Count); // equal priorities coexist — nothing is dropped
        }

        private sealed class RecordingAttackModifier(ContextModifierPriority priority, string name, List<string> trace)
            : ContextModifier(priority, $"Test_{name}"), IAttackModifier
        {
            public void Apply(IAttackContext context) => trace.Add(name);
        }
    }
}
