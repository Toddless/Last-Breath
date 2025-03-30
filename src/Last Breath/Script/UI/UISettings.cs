namespace Playground.Script.UI
{
    using System;
    using Godot;
    using Godot.Collections;
    using Playground.Script.Helpers;

    public class UISettings : ISettings
    {
        private readonly OptionButton _languageOption;
        private readonly Array<string> _languages = ["ru", "en"];

        public UISettings(OptionButton? language)
        {
            ArgumentNullException.ThrowIfNull(language);
            _languageOption = language;
            _languageOption.ItemSelected += LanguageSelected;
        }

        private void LanguageSelected(long index) => TranslationServer.SetLocale(_languages[(int)index]);

        public void SetLanguages()
        {
            foreach (var lang in _languages)
            {
                _languageOption.AddItem(lang);
            }
        }

        public void LoadSettings(ConfigFileHandler config) => LanguageSelected(_languageOption.Selected = (int)config.LoadSetting(SettingsSection.UI, Settings.Language));

        public void SaveSettings(ConfigFileHandler config) => config.SaveSettings(SettingsSection.UI, Settings.Language, _languageOption.Selected);
    }
}
