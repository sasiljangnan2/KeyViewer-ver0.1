using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;

namespace keyviewer
{
    public static class LayoutManager
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

        public static void SaveLayout(string path, KeyLayout layout)
        {
            string json = JsonSerializer.Serialize(layout, _jsonOptions);
            File.WriteAllText(path, json);
        }

        public static KeyLayout? LoadLayout(string path)
        {
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<KeyLayout>(json, _jsonOptions);
        }

        // 레이아웃을 KeyPanelService로 적용하고 생성된 KeyPanel 리스트를 반환
        public static List<KeyPanel> ApplyLayout(KeyLayout layout, KeyPanelService panelService)
        {
            var created = new List<KeyPanel>();
            foreach (var cfg in layout.Panels)
            {
                var down = Color.FromArgb(cfg.DownArgb);
                var up = Color.FromArgb(cfg.UpArgb);
                var loc = new System.Drawing.Point(cfg.X, cfg.Y);
                var size = new System.Drawing.Size(cfg.Width > 0 ? cfg.Width : 85, cfg.Height > 0 ? cfg.Height : 85);
                var kp = panelService.AddKeyPanel(cfg.Key, down, up, loc, size);
                
                // 🆕 DisplayName 복원
                if (!string.IsNullOrEmpty(cfg.DisplayName))
                {
                    kp.DisplayName = cfg.DisplayName;
                    kp.UpdateVisual(); // 텍스트 갱신
                }
                
                created.Add(kp);
            }
            return created;
        }

        // 헬퍼: 간단한 샘플 레이아웃 생성
        public static KeyLayout CreateSampleLayout(string name)
        {
            var layout = new KeyLayout { Name = name };
            layout.Panels.Add(new KeyPanelConfig
            {
                Name = "pA",
                Key = System.Windows.Forms.Keys.A,
                DownArgb = Color.Red.ToArgb(),
                UpArgb = SystemColors.ControlDark.ToArgb(),
                X = 20, Y = 20, Width = 85, Height = 85
            });
            layout.Panels.Add(new KeyPanelConfig
            {
                Name = "pS",
                Key = System.Windows.Forms.Keys.S,
                DownArgb = Color.Green.ToArgb(),
                UpArgb = SystemColors.ControlDark.ToArgb(),
                X = 110, Y = 20, Width = 85, Height = 85
            });
            return layout;
        }
    }
}