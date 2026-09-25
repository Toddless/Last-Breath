namespace Core.Battle
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>One rolled stage of the intelligence stance.</summary>
    /// <param name="Stage">The stage this row decides; stage 1 always fires and has no row.</param>
    /// <param name="BaseChance">The chance before the caster's MulticastChance multiplies it.</param>
    /// <param name="Cap">Ceiling of the final chance: a stage becomes guaranteed only where its row allows it.</param>
    public record MulticastStage(int Stage, float BaseChance, float Cap);

    /// <summary>
    /// Parsed multicast rules (see CombatRules.json, "multicast" section): what the intelligence stance
    /// rolls on. The rows are held top-down, the order the roll walks them in — the highest stage that
    /// comes up is the one the cast lands on.
    /// </summary>
    public record MulticastRules
    {
        /// <summary>What the stance rolls on where no rules can be reached: casts are rolled by hosts
        /// that compose no services at all (tests, tools), and a stance left without figures there would
        /// never multicast at all. The numbers are a balance placeholder and live in the shipped file;
        /// these only have to be a working ladder.</summary>
        public static readonly MulticastRules Default = new(
            [new MulticastStage(2, 0.5f, 1f), new MulticastStage(3, 0.25f, 0.65f), new MulticastStage(4, 0.05f, 0.4f)]);

        /// <summary>The rolled stages, highest first.</summary>
        public IReadOnlyList<MulticastStage> Stages { get; }

        public MulticastRules(IEnumerable<MulticastStage> stages) =>
            Stages = [.. stages.OrderByDescending(stage => stage.Stage)];
    }
}
