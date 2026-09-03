namespace LastBreath.Descriptors.Sandbox
{
    using System.Collections.Generic;
    using System.Linq;
    using Core.Narrative;
    using Core.Narrative.Actions;
    using Core.Narrative.Conditions;
    using static Tooling.Text.Format;

    /// <summary>
    /// What a dry run does to the game's vocabulary before it speaks it: keeps the book of every action
    /// it runs, and holds the list it was handed against the words the game names, so a run says so when
    /// the two have drifted instead of reading a dialogue in a language the game does not speak.
    /// </summary>
    public static class SandboxVocabulary
    {
        private const string DriftFormat = "the sandbox builds {0} {1} at position {2}, where the game's vocabulary names '{3}'";

        private const string CountFormat = "the sandbox builds {0} {1}(s), where the game's vocabulary names {2}";

        private const string ConditionWord = "condition";

        private const string ActionWord = "action";

        /// <summary>The actions with the run's book kept over them: what the sandbox can carry out is
        /// carried out and written down, what only the running game can do is written down alone.</summary>
        public static List<INarrativeActionFactory> Watched(SandboxLog log, IEnumerable<INarrativeActionFactory> factories) =>
            [.. factories.Select(factory =>
                new SandboxActionFactory(factory, log, !SandboxUnavailable.Actions.Contains(factory.Type)))];

        /// <summary>Where the list this run was built with and the game's vocabulary disagree — a word
        /// out of place, a word missing, a word too many.</summary>
        public static IReadOnlyList<string> Notes(
            IReadOnlyList<INarrativeConditionFactory> conditions, IReadOnlyList<INarrativeActionFactory> actions) =>
        [
            .. Drift(ConditionWord, NarrativeVocabulary.Conditions, [.. conditions.Select(factory => factory.Type)]),
            .. Drift(ActionWord, NarrativeVocabulary.Actions, [.. actions.Select(factory => factory.Type)]),
        ];

        /// <summary>Position by position: the order is the game's registration order, and a word built in
        /// the wrong place is a vocabulary that no longer mirrors the one the parsers are given.</summary>
        private static IEnumerable<string> Drift(string word, IReadOnlyList<NarrativeRecordSpec> named, IReadOnlyList<string> built)
        {
            for (int index = 0; index < named.Count && index < built.Count; index++)
                if (built[index] != named[index].TypeName)
                    yield return Text(DriftFormat, word, built[index], index, named[index].TypeName);

            if (built.Count != named.Count) yield return Text(CountFormat, built.Count, word, named.Count);
        }
    }
}
