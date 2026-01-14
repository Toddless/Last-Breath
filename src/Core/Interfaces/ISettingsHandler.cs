namespace Core.Interfaces
{
    using Enums;
    using Godot;

    public interface ISettingsHandler
    {
        void ApplySavedSettings();
        string[] GetLanguages();
        string[] GetWindowMods();
        string[] GetWindowResolutions();
        Variant GetSettingValue(string section, string setting);
        void SetLanguage(long index);
        void SetResolution(long index);
        void SetSoundBus(SoundBus bus, double value);
        void SetWindowMode(long index, bool borderless = false);
    }
}
