namespace Playground.Components
{
    using System;

    public static class UIEventBus
    {
        public static event Action? Close, Resume, SaveSettings;
        public static void PublishClose() => Close?.Invoke();
        public static void PublishResume() => Resume?.Invoke();
        public static void PublishSaveSettings() => SaveSettings?.Invoke();
    }
}
