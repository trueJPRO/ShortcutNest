using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ShortcutNest.Models;

namespace ShortcutNest.Config
{
    public class LauncherConfig
    {
        public List<LauncherSlot?> Slots { get; set; } = new();

        public static string ConfigPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "launcher-config.json");

        public static LauncherConfig Load()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    var cfg = CreateDefault();
                    Save(cfg);
                    return cfg;
                }

                var text = File.ReadAllText(ConfigPath);
                var loaded = JsonSerializer.Deserialize<LauncherConfig>(text);

                if (loaded == null)
                    return CreateDefault();

                NormalizeSlots(loaded);
                return loaded;
            }
            catch
            {
                return CreateDefault();
            }
        }

        public static void Save(LauncherConfig config)
        {
            NormalizeSlots(config);
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(ConfigPath, json);
        }

        private static void NormalizeSlots(LauncherConfig config)
        {
            while (config.Slots.Count < 9) config.Slots.Add(null);
            if (config.Slots.Count > 9) config.Slots = config.Slots.GetRange(0, 9);
        }

        private static LauncherConfig CreateDefault()
        {
            return new LauncherConfig
            {
                Slots =
                [
                    new() { Title = "Terminal", Type = "app", Target = "wt.exe", IconPath = "icons\\terminal.png" },
                    new() { Title = "Explorer", Type = "app", Target = "explorer.exe", IconPath = "icons\\explorer.png" },
                    new() { Title = "Browser", Type = "url", Target = "https://google.com", IconPath = "icons\\browser.png" },
                    new() { Title = "Notes", Type = "app", Target = "notepad.exe", IconPath = "icons\\notes.png" },

                    null,
                    null,
                    null,
                    null,
                    null
                ]
            };
        }
    }
}