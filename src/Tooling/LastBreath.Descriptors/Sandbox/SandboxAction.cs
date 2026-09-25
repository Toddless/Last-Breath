namespace LastBreath.Descriptors.Sandbox
{
    using System;
    using System.Collections.Generic;
    using Core.Narrative;
    using Core.Narrative.Actions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using static Tooling.Text.Format;

    /// <summary>
    /// Every action of a dry run, written down as it goes by. The ones the sandbox can carry out are
    /// carried out against it — a fact rises, an item moves, a quest advances, and the panel shows the
    /// world after; the ones that need a running game are named and left undone.
    /// </summary>
    /// <remarks>The refusal is a decorator over the built action rather than a stub service under it:
    /// the entry is still parsed by the game's own factory, so the panel reads the same "broken entry"
    /// the game would, and only the doing is held back.</remarks>
    public sealed class SandboxAction(INarrativeAction inner, SandboxLog log, string entry, bool available) : INarrativeAction
    {
        private const string RanFormat = "did {0}";

        private const string OutsideFormat = "would do {0} — only the running game can";

        public void Execute(NarrativeContext context)
        {
            log.Write(Text(available ? RanFormat : OutsideFormat, entry));

            // Written down before it runs: an action that throws is one the author most needs to see named.
            if (available) inner.Execute(context);
        }
    }

    /// <summary>The game's own action factory with the dry run's book kept over it.</summary>
    public sealed class SandboxActionFactory(INarrativeActionFactory inner, SandboxLog log, bool available) : INarrativeActionFactory
    {
        public string Type => inner.Type;

        public NarrativeRecordSpec Parameters => inner.Parameters;

        public INarrativeAction? Create(JObject json, INarrativeActionParser parser) =>
            inner.Create(json, parser) is { } action
                ? new SandboxAction(action, log, json.ToString(Formatting.None), available)
                : null;
    }

    /// <summary>
    /// The actions no tool can carry out: they reach for a window, a scene, a character sheet or a
    /// reputation pipeline that only the running game has. Named one by one — a list of what a sandbox
    /// cannot do is a decision about each entry, and a rule guessing at it would quietly grow.
    /// </summary>
    /// <remarks>An action whose sandbox service does nothing belongs here rather than among the ones
    /// that ran: a line reading "did this" beside a world that did not move is worse than no line.
    /// A standing shifted by a dialogue and a deed put through the reputation pipeline are both of
    /// that kind — the sandbox states a relation LEVEL, and neither has anywhere to land in it.</remarks>
    public static class SandboxUnavailable
    {
        public static IReadOnlyCollection<string> Actions { get; } = new HashSet<string>(StringComparer.Ordinal)
        {
            PublishDeedActionFactory.Spec.TypeName,
            AddReputationActionFactory.Spec.TypeName,
            StartTradeActionFactory.Spec.TypeName,
            SpawnNpcActionFactory.Spec.TypeName,
            GrantTreePointsActionFactory.Spec.TypeName,
        };
    }
}
