namespace Playground.Script.UI
{
    using Godot;
    using Playground.Script.Enums;
    using Playground.Script.Helpers;

    public class SoundSettings : ISettings
    {
        private HSlider? _musicSlider, _sfxSlider, _masterSlider;

        public SoundSettings(HSlider musicSlider, HSlider sfxSlider, HSlider masterSlider)
        {
            _masterSlider = masterSlider;
            _musicSlider = musicSlider;
            _sfxSlider = sfxSlider;
            _masterSlider!.ValueChanged += MasterSliderValueChanged;
            _musicSlider!.ValueChanged += MusicSliderValueChanged;
            _sfxSlider!.ValueChanged += SfxSliderValueChanged;
        }

        private void MasterSliderValueChanged(double value) => SliderValueChanged(value, SoundBus.Master);

        private void MusicSliderValueChanged(double value) => SliderValueChanged(value, SoundBus.Music);

        private void SfxSliderValueChanged(double value) => SliderValueChanged(value, SoundBus.Sfx);

        private void SliderValueChanged(double value, SoundBus soundBus) => AudioServer.SetBusVolumeDb((int)soundBus, (float)Mathf.LinearToDb(value));


        public void LoadSettings(ConfigFileHandler config)
        {
            _masterSlider!.Value = (float)config.LoadSetting(SettingsSection.Sound, KeyBindings.Master);
            _musicSlider!.Value = (float)config.LoadSetting(SettingsSection.Sound, KeyBindings.Music);
            _sfxSlider!.Value = (float)config.LoadSetting(SettingsSection.Sound, KeyBindings.Sfx);
        }

        public void SaveSettings(ConfigFileHandler config)
        {
            config.SaveSettings(SettingsSection.Sound, KeyBindings.Master, _masterSlider!.Value);
            config.SaveSettings(SettingsSection.Sound, KeyBindings.Music, _musicSlider!.Value);
            config.SaveSettings(SettingsSection.Sound, KeyBindings.Sfx, _sfxSlider!.Value);
        }
    }
}
