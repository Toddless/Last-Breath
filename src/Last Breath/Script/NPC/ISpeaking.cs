namespace Playground.Script.NPC
{
    using System.Collections.Generic;
    using Playground.Localization;

    public interface ISpeaking
    {
        bool NpcTalking { get; set; } 
        Dictionary<string, DialogueNode> Dialogs { get; }
    }
}
