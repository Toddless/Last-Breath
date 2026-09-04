namespace Core.Narrative.Validation
{
    /// <summary>What one cross-check has to say about the shipped narrative. Every kind is a fact of a
    /// different sort, because what an author does about one is different: a dropped record is out of the
    /// game entirely, a dangling route is why, and a word nothing answers is a rename that got away.</summary>
    public enum NarrativeFindingKind
    {
        /// <summary>The loader kept nothing under this id: the whole dialogue or quest is out of the game.
        /// The findings beside it usually say why; one standing alone means the loader refused it over
        /// something these rules do not read, and the run's own report names that.</summary>
        Dropped,

        /// <summary>A record, a node or a stage is missing a part the game requires of it, or is written in
        /// a shape that contradicts itself: an objective that is both a predicate and a counter or neither,
        /// a stage that both ends and routes on, an ending that buries a quest written unable to fail.</summary>
        Incomplete,

        /// <summary>Two records of a catalog, or two nodes, stages or endings of one record, answer to the
        /// same name. The loader keeps one of them and everything written under the other is out of the
        /// game without a word.</summary>
        DuplicateId,

        /// <summary>A route out of a dialogue leads to a node nobody wrote.</summary>
        DanglingNode,

        /// <summary>No route of the dialogue reaches the node: it is written and unreadable.</summary>
        UnreachableNode,

        /// <summary>A route out of a stage leads to a stage nobody wrote.</summary>
        DanglingStage,

        /// <summary>A quest names an npc to take it from or hand it in to who has no dialogue to do it in.</summary>
        MissingDialogue,

        /// <summary>A condition or an action is written under a word no factory of the vocabulary reads,
        /// or is not written as an entry at all — a list put down as an object, an entry as a bare value —
        /// so the parsers read nothing out of it and the clause gates nothing.</summary>
        UnknownEntry,

        /// <summary>A key the vocabulary requires is not written, or is written empty.</summary>
        MissingParameter,

        /// <summary>An id is written where a record is meant, and no catalog it may point into holds one.</summary>
        UnknownReference,

        /// <summary>A word is written where one of a named set of members is meant, and it is none of
        /// them. Its own kind and not an unknown entry: the entry is one the vocabulary reads, and the
        /// author picked a member that does not exist rather than a factory that does not.</summary>
        UnknownChoice,

        /// <summary>A stage of a quest is reachable from itself: the routes close a ring and the quest
        /// would never end.</summary>
        LoopingStage,

        /// <summary>A catalog a reference points into is one this run cannot read at all. A fact about the
        /// run and not about the data: every id pointing there is left unanswered rather than called broken.</summary>
        UndescribedTarget,

        /// <summary>A localization key of a line or an option is in no locale the run read.</summary>
        MissingText,

        /// <summary>The key is written in the reference locale and missing from another one.</summary>
        UntranslatedText,

        /// <summary>Somebody asks about a fact key and nothing — no document, no code — ever raises it.
        /// The clause gated on it can never be met, and the data gives no sign of it until played.</summary>
        FactNeverWritten,

        /// <summary>A fact key is raised and nobody ever asks about it: the write is a step nothing hangs
        /// on. Its own kind because it is not necessarily wrong — a fact may be written today for a quest
        /// written next week — which is why the run reports it rather than refusing it.</summary>
        FactNeverRead,
    }

    /// <summary>One thing the checks found: what sort it is, the place in the data it belongs to, and what
    /// is wrong there. The place is written the same way every run, so a finding can be pinned as known.</summary>
    public sealed record NarrativeFinding(NarrativeFindingKind Kind, string Where, string Message);
}
