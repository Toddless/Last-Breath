namespace LastBreath.Script
{
    using Godot;
    using System;
    using Core.Enums;
    using Core.Interfaces;
    using Core.Interfaces.UI;
    using Core.Interfaces.Data;
    using LastBreath.Script.UI;
    using LastBreath.Script.Helpers;
    using Core.Constants;

    public partial class OptionsWindow : UI.Window, IInitializable, IRequireServices
    {
        private const string UID = "uid://crpiglshqam38";

        [Export] private HSlider? _music, _sfx, _master;
        [Export] private OptionButton? _languange, _windowMode, _windowResolution;
        [Export] private LocalizableButton? _returnButton;
        [Export] private LocalizableLabel[] _labels = [];

        private ISettingsHandler? _settings;
        private IUiMediator? _uiMediator;

        public override void _Ready()
        {
            if (_returnButton != null) _returnButton.Pressed += RaiseClose;

            AddWindowMods();
            AddWindowResolutions();
            AddLanguages();
            SetSavedSettingsValues();

            _music.ValueChanged += OnMusicSliderValueChanges;
            _sfx.ValueChanged += OnSfxSliderValueChanges;
            _master.ValueChanged += OnMasterValueChanges;
            _languange.ItemSelected += OnLanguageSelected;
            _windowMode.ItemSelected += OnWindowModeSelected;
            _windowResolution.ItemSelected += OnWindowResolurionSelected;
            UpdateUI();
        }


        public override void InjectServices(IGameServiceProvider provider)
        {
            _settings = provider.GetService<ISettingsHandler>();
            _uiMediator = provider.GetService<IUiMediator>();
            _uiMediator.UpdateUi += UpdateUI;
        }

        public override void _ExitTree()
        {
            if (_uiMediator != null) _uiMediator.UpdateUi -= UpdateUI;
        }

        public static PackedScene Initialize() => ResourceLoader.Load<PackedScene>(UID);

        private void UpdateUI()
        {
            _returnButton?.UpdateButtonText();
            foreach (var label in _labels)
                label.UpdateLabelText();
        }


        private void SetSavedSettingsValues()
        {
            ArgumentNullException.ThrowIfNull(_settings);
            _music.Value = (double)_settings.GetSettingValue(SettingsSection.Sound, Settings.Music);
            _sfx.Value = (double)_settings.GetSettingValue(SettingsSection.Sound, Settings.Sfx);
            _master.Value = (double)_settings.GetSettingValue(SettingsSection.Sound, Settings.Master);
            _languange.Selected = (int)_settings.GetSettingValue(SettingsSection.UI, Settings.Language);
            _windowMode.Selected = (int)_settings.GetSettingValue(SettingsSection.Video, Settings.WindowMode);
            _windowResolution.Selected = (int)_settings.GetSettingValue(SettingsSection.Video, Settings.Resolution);
        }

        private void AddLanguages()
        {
            ArgumentNullException.ThrowIfNull(_settings);
            foreach (var lang in _settings.GetLanguages())
                _languange?.AddItem(lang);
        }

        private void AddWindowResolutions()
        {
            ArgumentNullException.ThrowIfNull(_settings);
            foreach (var resolution in _settings.GetWindowResolutions())
                _windowResolution?.AddItem(resolution);
        }

        private void AddWindowMods()
        {
            ArgumentNullException.ThrowIfNull(_settings);
            foreach (var mode in _settings.GetWindowMods())
                _windowMode?.AddItem(mode);
        }

        private void OnWindowResolurionSelected(long index) => _settings?.SetResolution(index);
        private void OnWindowModeSelected(long index) => _settings?.SetWindowMode(index);
        private void OnLanguageSelected(long index) => _settings?.SetLanguage(index);
        private void SlideValueChanges(double value, SoundBus soundBus) => _settings?.SetSoundBus(soundBus, value);
        private void OnMusicSliderValueChanges(double value) => SlideValueChanges(value, SoundBus.Music);
        private void OnSfxSliderValueChanges(double value) => SlideValueChanges(value, SoundBus.Sfx);
        private void OnMasterValueChanges(double value) => SlideValueChanges(value, SoundBus.Master);
    }
}
