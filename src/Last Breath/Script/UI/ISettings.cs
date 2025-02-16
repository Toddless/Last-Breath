namespace Playground.Script.UI
{
    public interface ISettings
    {
        void LoadSettings(ConfigFileHandler config);
        void SaveSettings(ConfigFileHandler config);
    }
}
