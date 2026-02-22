namespace ShortcutNest.Models
{
    public class LauncherSlot
    {
        public string? Title { get; set; }
        public string? Type { get; set; }     // app | folder | url | command
        public string? Target { get; set; }
        public string? IconPath { get; set; } // relative or absolute path
    }
}