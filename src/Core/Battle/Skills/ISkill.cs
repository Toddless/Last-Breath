namespace Core.Battle.Skills
{
    using Entity;
    using Interfaces;
    using Localization;

    public interface ISkill : IIdentifiable, IDisplayable
    {
        void Attach(IFightable owner);
        void Detach(IFightable owner);

        ISkill Copy();
        bool IsStronger(ISkill skill);

        /// <summary>The same rule text as <see cref="IDisplayable.Description"/>, in the format the reader
        /// can show. A card in a window takes the tinted, clickable one; a line printed into plain text —
        /// a tooltip in a tool, a test asserting on a sentence — takes the words alone. The default answers
        /// with the rich text for an implementation that renders only one way.</summary>
        string Describe(TextFormat format) => Description;
    }
}
