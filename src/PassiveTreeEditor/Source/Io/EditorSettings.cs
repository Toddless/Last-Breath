namespace PassiveTreeEditor.Source.Io
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Core.Data;
    using Core.Enums;
    using Navigation;
    using Newtonsoft.Json;
    using Simulation;

    /// <summary>
    /// Tool-only preferences: which file was open, what character the totals are measured on, and how
    /// far apart the layout is being read. Deliberately separate from the tree file — the tree is game
    /// data and must not carry editor state into the repository.
    /// </summary>
    public static class EditorSettings
    {
        public static EditorSettingsState Load()
        {
            var state = new EditorSettingsState();

            try
            {
                if (!File.Exists(ToolPaths.SettingsPath)) return state;

                EditorSettingsDto? dto = JsonConvert.DeserializeObject<EditorSettingsDto>(File.ReadAllText(ToolPaths.SettingsPath));
                if (dto is null) return state;

                if (!string.IsNullOrWhiteSpace(dto.TreePath)) state.TreePath = dto.TreePath;

                // A file written before the handle existed, or one edited by hand into nonsense, reads
                // as the authored layout; the bounds themselves are the transform's to enforce.
                if (dto.LayoutSpread is > 0f) state.LayoutSpread = dto.LayoutSpread.Value;

                foreach (KeyValuePair<string, float> pair in dto.BaseStats ?? new Dictionary<string, float>())
                    if (EnumParser.TryParseEnum(pair.Key, out EntityParameter parameter))
                        state.BaseStats[parameter] = pair.Value;
            }
            catch (Exception)
            {
                // A damaged settings file must never stop the tool from opening: defaults win.
                return new EditorSettingsState();
            }

            return state;
        }

        public static void Save(EditorSettingsState state)
        {
            var dto = new EditorSettingsDto
            {
                TreePath = state.TreePath,
                LayoutSpread = state.LayoutSpread,
                BaseStats = new Dictionary<string, float>()
            };

            foreach (KeyValuePair<EntityParameter, float> pair in state.BaseStats.Values)
                dto.BaseStats[pair.Key.ToString()] = pair.Value;

            try
            {
                string json = JsonConvert.SerializeObject(dto, Formatting.Indented).Replace("\r\n", "\n");
                File.WriteAllText(ToolPaths.SettingsPath, json + "\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
            catch (Exception)
            {
                // Preferences are a convenience; failing to persist them is not worth an error popup.
            }
        }
    }

    public sealed class EditorSettingsState
    {
        public string TreePath { get; set; } = string.Empty;

        /// <summary>How far apart the layout was being read. A view setting, so it lives here and never
        /// in the tree file — the same 178 coordinates are on disk whatever this says.</summary>
        public float LayoutSpread { get; set; } = CanvasTransform.DefaultSpread;

        /// <summary>Saved overrides only, empty when nothing was saved. The baseline lives in the
        /// PlayerStats catalog, which is not loaded yet when settings are read.</summary>
        public BaseStatProfile BaseStats { get; set; } = new();
    }

    public sealed class EditorSettingsDto
    {
        [JsonProperty("treePath")] public string? TreePath { get; set; }

        [JsonProperty("layoutSpread")] public float? LayoutSpread { get; set; }

        [JsonProperty("baseStats")] public Dictionary<string, float>? BaseStats { get; set; }
    }
}
