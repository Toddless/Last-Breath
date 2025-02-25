namespace Playground.Script.NPC
{
    using System.Collections.Generic;
    using Playground.Localization;

    public interface ISpeaking
    {
        Dictionary<string, DialogueNode> Dialogs { get; }
    }
}
