namespace Playground.Localization
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;

    public class LocalizationManager
    {
        public static Dictionary<string, DialogueNode> LoadDialogue(string path)
        {
            var data = JsonSerializer.Deserialize<DialogueData>(File.ReadAllText(path));
            return data?.Dialogs?.ToDictionary(d => d.Id) ?? [];
        }
    }
}
