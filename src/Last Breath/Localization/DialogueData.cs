namespace Playground.Localization
{
    using Godot;
    using Godot.Collections;

    [GlobalClass]
    public partial class DialogueData : Resource
    {
        [Export]
        public Dictionary<string, DialogueNode>? Dialogs { get; set; }


        public void _Validate()
        {
            foreach (var item in Dialogs.Values)
            {
                if (item.Texts.Count == 0)
                {
                    // log node has no text
                }
            }
        }
    }
}
