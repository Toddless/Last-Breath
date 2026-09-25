namespace LastBreath.UI
{
    using System;
    using Core.Constants;
    using Core.Data;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Views.UI;
    using Godot;
    using Helpers;

    public partial class OptionsWindow : Control, IWindow
    {
        private const string UID = "uid://crpiglshqam38";

        [Export] private HSlider? _music, _sfx, _master;
        [Export] private OptionButton? _language, _windowMode, _windowResolution;
        [Export] private Button? _returnButton;

        private ISettingsHandler? _settings;


        public override void _Ready()
        {
            _returnButton?.Pressed += Close;
            _music?.ValueChanged += OnMusicSliderValueChanges;
            _sfx?.ValueChanged += OnSfxSliderValueChanges;
            _master?.ValueChanged += OnMasterValueChanges;
            _language?.ItemSelected += OnLanguageSelected;
            _windowMode?.ItemSelected += OnWindowModeSelected;
            _windowResolution?.ItemSelected += OnWindowResolutionSelected;

            AddWindowMods();
            AddWindowResolutions();
            AddLanguages();
            SetSavedSettingsValues();
        }


        public void InjectServices(IGameServiceProvider provider)
        {
            _settings = provider.GetService<ISettingsHandler>();
        }

        // Closing means dying (IWindow contract) — see SaveLoadWindow.Close for the reopen NRE.
        public void Close() => QueueFree();

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);


        private void SetSavedSettingsValues()
        {
            ArgumentNullException.ThrowIfNull(_settings);
            _music?.Value = (double)_settings.GetSettingValue(SettingsSection.Sound, Settings.Music);
            _sfx?.Value = (double)_settings.GetSettingValue(SettingsSection.Sound, Settings.Sfx);
            _master?.Value = (double)_settings.GetSettingValue(SettingsSection.Sound, Settings.Master);
            _language?.Selected = (int)_settings.GetSettingValue(SettingsSection.UI, Settings.Language);
            _windowMode?.Selected = (int)_settings.GetSettingValue(SettingsSection.Video, Settings.WindowMode);
            _windowResolution?.Selected = (int)_settings.GetSettingValue(SettingsSection.Video, Settings.Resolution);
        }

        private void AddLanguages()
        {
            ArgumentNullException.ThrowIfNull(_settings);
            foreach (string lang in _settings.GetLanguages())
                _language?.AddItem(lang);
        }

        private void AddWindowResolutions()
        {
            ArgumentNullException.ThrowIfNull(_settings);
            foreach (string resolution in _settings.GetWindowResolutions())
                _windowResolution?.AddItem(resolution);
        }

        private void AddWindowMods()
        {
            ArgumentNullException.ThrowIfNull(_settings);
            foreach (string mode in _settings.GetWindowMods())
                _windowMode?.AddItem(mode);
        }

        private void OnWindowResolutionSelected(long index) => _settings?.SetResolution(index);
        private void OnWindowModeSelected(long index) => _settings?.SetWindowMode(index);
        private void OnLanguageSelected(long index) => _settings?.SetLanguage(index);
        private void SlideValueChanges(double value, SoundBus soundBus) => _settings?.SetSoundBus(soundBus, value);
        private void OnMusicSliderValueChanges(double value) => SlideValueChanges(value, SoundBus.Music);
        private void OnSfxSliderValueChanges(double value) => SlideValueChanges(value, SoundBus.Sfx);
        private void OnMasterValueChanges(double value) => SlideValueChanges(value, SoundBus.Master);
    }
}
