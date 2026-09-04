namespace Core.Narrative.Dialogues
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// What a line and an option of a conversation are read under. A key is not a name somebody thinks
    /// of: it is worded from the place the text stands in — the npc who speaks it, the node it belongs
    /// to, and the line's place in that node or the option's own name — so that a node renamed carries
    /// its wording with it and a line added anywhere is already readable.
    /// <para>The exception is an option offered in more than one place under one sentence: "Leave.",
    /// "About something else...". Those are worded from no structure at all and are named here one by
    /// one — a key derived from the place would be the same sentence written out once per place it is
    /// offered in, and translated as many times.</para>
    /// <para>One rule and one list, read by the tool that writes the keys, by the checks that hold the
    /// shipped files to them, and by the migration that named the old ones again. Two readings of it
    /// would be two answers to what a line is called.</para>
    /// </summary>
    public static class DialogueKeys
    {
        /// <summary>What every key of a conversation begins with.</summary>
        public const string Prefix = "Dlg_";

        /// <summary>What the parts of a key are joined with.</summary>
        public const string Separator = "_";

        /// <summary>What an npc id begins with, and what a key worded from one leaves out: the key
        /// already says which catalog it belongs to, and saying it twice reads as a slip.</summary>
        public const string NpcPrefix = "Npc_";

        /// <summary>What an option belonging to no one conversation is read under.</summary>
        public const string SharedPrefix = Prefix + "Opt" + Separator;

        /// <summary>Ends the conversation.</summary>
        public const string Leave = SharedPrefix + "Leave";

        /// <summary>Goes back to the node the branch was entered from.</summary>
        public const string Back = SharedPrefix + "Back";

        /// <summary>Asks what the trial waiting outside is; every trial asks it in the same words.</summary>
        public const string TrialHint = SharedPrefix + "TrialHint";

        /// <summary>Reports a finished trial; every trial reports it in the same words.</summary>
        public const string TrialTurnIn = SharedPrefix + "TrialTurnIn";

        /// <summary>What the first line of a node is numbered. Lines are counted the way an author
        /// counts them and not the way an array is indexed: the number is read in a .po file.</summary>
        private const int FirstLine = 1;

        private static readonly HashSet<string> s_shared = new(StringComparer.Ordinal)
        {
            Leave,
            Back,
            TrialHint,
            TrialTurnIn,
        };

        /// <summary>The options no structure words: offered in more than one place under one sentence,
        /// and therefore read under one key wherever they stand.</summary>
        public static IReadOnlyCollection<string> SharedOptions => s_shared;

        /// <summary>The key one line of a node is read under. <paramref name="lineIndex"/> is the line's
        /// place in its node, counted from zero as the file lists it.</summary>
        public static string Expected(string npcId, string nodeId, int lineIndex)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(lineIndex);

            return Under(npcId, nodeId, (lineIndex + FirstLine).ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>The key one option of a node is read under, worded from the name the node's routes
        /// already call it by.</summary>
        public static string Expected(string npcId, string nodeId, string optionId)
        {
            ArgumentNullException.ThrowIfNull(optionId);

            return optionId.Length == 0 ? string.Empty : Under(npcId, nodeId, optionId);
        }

        /// <summary>Whether the key is one of the options every conversation shares — the one sort of key
        /// no place words and none of the rules above answer for.</summary>
        public static bool IsShared(string key)
        {
            ArgumentNullException.ThrowIfNull(key);

            return s_shared.Contains(key);
        }

        /// <summary>Whether the place carrying the key is read under it as it stands instead of under the
        /// key its own place words. Only an option ever is: a shared sentence belongs to the several
        /// places offering it, and a line is said by one npc in one node — a line carrying a shared key
        /// is reading words that answer to no place at all.</summary>
        public static bool IsShared(string key, bool offered) => offered && IsShared(key);

        /// <summary>What a conversation's keys are worded from: the npc, without the word saying he is
        /// one. An id written without that word is taken as it stands.</summary>
        public static string Speaker(string npcId)
        {
            ArgumentNullException.ThrowIfNull(npcId);

            return npcId.StartsWith(NpcPrefix, StringComparison.Ordinal) ? npcId[NpcPrefix.Length..] : npcId;
        }

        /// <summary>The three parts of every key, joined. Empty where the npc or the node carries no
        /// name: a place nobody has named words no key, and the rules say so about the place itself.</summary>
        private static string Under(string npcId, string nodeId, string last)
        {
            ArgumentNullException.ThrowIfNull(npcId);
            ArgumentNullException.ThrowIfNull(nodeId);

            if (npcId.Length == 0 || nodeId.Length == 0) return string.Empty;

            return Prefix + Speaker(npcId) + Separator + nodeId + Separator + last;
        }
    }
}
