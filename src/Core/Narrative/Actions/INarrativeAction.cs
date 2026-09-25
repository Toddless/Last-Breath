namespace Core.Narrative.Actions
{
    /// <summary>
    /// A data-driven world mutation executed by dialogues and quest transitions the moment
    /// their entry fires (dialogue is atomic per choice — nothing here is ever rolled back).
    /// </summary>
    public interface INarrativeAction
    {
        void Execute(NarrativeContext context);
    }
}
