namespace Playground.Localization
{
    using System.IO;
    using Godot;
    using Newtonsoft.Json;

    public static class DialogueDataConverter
    {
        public static void LoadDialogueData(string json, string resourcePath)
        {
            string jsonText = File.ReadAllText(json);
            DialogueData data = ConvertJsonToDialogueData(jsonText);

            ResourceSaver.Save(data, resourcePath);
        }

        private static DialogueData ConvertJsonToDialogueData(string jsonText)
        {
            var jsonNodes = JsonConvert.DeserializeObject<System.Collections.Generic.List<JsonDialogueNode>>(jsonText);
            if (jsonNodes == null) return new();
            var dialogueData = new DialogueData
            {
                Dialogs = []
            };

            foreach (var jsonNode in jsonNodes)
            {
                var dialogueNode = new DialogueNode
                {
                    DialogueId = jsonNode.Id
                };

                for (int i = 0; i < jsonNode.Texts.Count; i++)
                {
                    var key = $"dialog{jsonNode.Id}Text{i + 1}";
                    dialogueNode.Texts.Add(new LocalizedString { Key = key });
                }

                for (int j = 0; j < jsonNode.Options.Count; j++)
                {
                    var option = jsonNode.Options[j];
                    var optionKey = $"dialog{jsonNode.Id}Option{j + 1}";

                    dialogueNode.Options.Add(new DialogueOption
                    {
                        OptionName = new LocalizedString { Key = optionKey },
                        TargetNode = option.TargetNode,
                        RelationEffect = option.RelationEffect
                    });
                }
                dialogueNode.ReturnToPrevious = jsonNode.ReturnToPrevious;
                dialogueData.Dialogs[jsonNode.Id] = dialogueNode;
            }

            return dialogueData;
        }

        private class JsonDialogueNode
        {
            [JsonProperty(nameof(Id))]
            public string Id { get; set; } = string.Empty;

            [JsonProperty(nameof(Texts))]
            public System.Collections.Generic.List<TextEntry> Texts { get; set; } = [];

            [JsonProperty(nameof(Options))]
            public System.Collections.Generic.List<OptionEntry> Options { get; set; } = [];

            [JsonProperty(nameof(ReturnToPrevious))]
            public bool ReturnToPrevious { get; set; }
        }

        private class TextEntry
        {
            [JsonProperty(nameof(TextRU))]
            public string TextRU { get; set; } = string.Empty;

            [JsonProperty(nameof(TextEN))]
            public string TextEN { get; set; } = string.Empty;
        }

        private class OptionEntry
        {
            [JsonProperty(nameof(OptionNameRU))]
            public string OptionNameRU { get; set; } = string.Empty;

            [JsonProperty(nameof(OptionNameEN))]
            public string OptionNameEN { get; set; } = string.Empty;

            [JsonProperty(nameof(TargetNode))]
            public string TargetNode { get; set; } = string.Empty;

            [JsonProperty(nameof(RelationEffect))]
            public int RelationEffect { get; set; } = 0;
        }
    }
}
