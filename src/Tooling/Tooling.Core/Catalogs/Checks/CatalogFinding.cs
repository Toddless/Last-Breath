namespace Tooling.Catalogs.Checks
{
    /// <summary>What one cross-check over the catalogs has to say. Every kind is a fact of a different
    /// sort, because what an author does about one is different: an id nothing answers is a rename that
    /// got away, a second record under one name is a record nothing can address, and a key no locale
    /// words is a name the player reads as its own internal spelling.</summary>
    public enum CatalogFindingKind
    {
        /// <summary>An id is written where a record is meant, and no catalog the field may point into
        /// holds one under it.</summary>
        UnknownReference,

        /// <summary>A catalog a reference points into is one this run cannot read at all. A fact about the
        /// run and not about the data: every id pointing there is left unanswered rather than called
        /// broken, and the finding is said once for the target rather than once per word.</summary>
        UndescribedTarget,

        /// <summary>Two records of one section answer to the same name. A catalog states the field its
        /// records are FOUND by, so whichever of the two the reader keeps, the other is out of reach —
        /// and where the id is a weight in a roll, both are kept and the roll is loaded.</summary>
        DuplicateId,

        /// <summary>A key a record's catalog words its text under is in the reference locale of the run
        /// under nothing: the name reaches the player as the id itself.</summary>
        MissingText,

        /// <summary>The key is written in the reference locale and missing from another one.</summary>
        UntranslatedText,

        /// <summary>One locale writes a key twice. gettext keeps the first, so everything written under
        /// the second is out of the game with nothing said about it.</summary>
        DuplicateKey,

        /// <summary>A field the schema reads as a pair of bounds is written as something else — a plain
        /// number, or an object one of whose ends is not a number. The converters read a bare number as a
        /// fixed range, so the file grows a second spelling of one field that every reader has to know
        /// about; a bound they cannot read is a bound that is not there.</summary>
        MalformedRange,

        /// <summary>A record whose shapes are told apart by the PRESENCE of a key wears none of them or
        /// more than one. Naming both leaves what the record means undecided and naming neither describes
        /// nothing, so the reader refuses the record either way.</summary>
        AmbiguousShape,

        /// <summary>A word is written where one of a named set of members is meant, and it is none of
        /// them. Its own sort and not an id nothing answers: the members are written down in the schema
        /// rather than in another catalog, and what an author does about it is pick from a list. The
        /// readers of such a field are strict wherever they are — the word is refused, and the record
        /// carrying it goes out of the game without either being named.</summary>
        UnknownChoice,

        /// <summary>A rule of the GAME over the meaning of its own data, run beside these and reported in
        /// one list. The shape of a record says nothing about such a fact — a knob written two ways, a
        /// filter nothing answers, an artefact handed out again on every turn-in — so the rules live with
        /// the game and this is the one sort the checks carry them under. Which rule spoke stands beside
        /// the finding.</summary>
        Rule
    }

    /// <summary>
    /// One thing the checks found: what sort it is, the catalog and the record it belongs to, the word it
    /// is about, the address it stands at, and what is wrong there.
    /// </summary>
    /// <param name="Catalog">The catalog the finding is in; empty for one about the run itself.</param>
    /// <param name="Record">The id of the record it stands in; empty where it belongs to no record.</param>
    /// <param name="Named">The word the finding is about — the id nothing answers, the key no locale
    /// words, the target nobody described; empty where the finding is about a shape rather than a word.</param>
    /// <param name="Where">The address: the file and the pointer inside it, or the locale and the line
    /// for a finding about the wording. Empty for a finding that stands at no one place.</param>
    /// <param name="Rule">Which rule spoke, for a finding of the game's own rules over the meaning of its
    /// data; empty for every rule over a shape, whose sort already names it. Four different facts share
    /// one sort, and a row saying only <see cref="CatalogFindingKind.Rule"/> would call them one thing.</param>
    /// <remarks>The first four are written the same way every run, so a finding can be pinned as known
    /// without pinning either the wording of a message or a record's place in its file.</remarks>
    public sealed record CatalogFinding(
        CatalogFindingKind Kind,
        string Catalog,
        string Record,
        string Named,
        string Where,
        string Message,
        string Rule = "");
}
