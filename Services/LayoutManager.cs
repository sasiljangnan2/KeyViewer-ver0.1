using keyviewer.UI.Controls;
using keyviewer.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.Json;

namespace keyviewer.Services
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
                
                if (!string.IsNullOrEmpty(cfg.DisplayName))
                {
                    kp.DisplayName = cfg.DisplayName;
                }
                
                // 테두리 복원
                kp.BorderEnabled = cfg.BorderEnabled;
                if (cfg.BorderColorArgb != 0)
                {
                    kp.BorderColor = Color.FromArgb(cfg.BorderColorArgb);
                }
                kp.BorderWidth = cfg.BorderWidth > 0 ? cfg.BorderWidth : 2;
                
                // 🆕 모서리 반경 복원
                kp.CornerRadius = cfg.CornerRadius;
                
                kp.UpdateVisual();
                
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

        // 🆕 전체 키보드 레이아웃 생성 (모든 키 포함)
        public static KeyLayout CreateFullKeyboardLayout(string name)
        {
            var layout = new KeyLayout { Name = name };
            int x = 10, y = 10;
            const int keySize = 40;
            const int spacing = 5;
            const int rowHeight = keySize + spacing;

            // 🔥 F1-F9를 맨 위에 배치
            x = 10; y = 10;
            var functionKeys = new[] { Keys.F1, Keys.F2, Keys.F3, Keys.F4, Keys.F5, 
                                      Keys.F6, Keys.F7, Keys.F8, Keys.F9 };
            foreach (var key in functionKeys)
            {
                AddKeyPanel(layout, key, x, y, keySize - 5, keySize);
                x += keySize - 5 + spacing;
            }

            // 숫자 행 (1-0)
            x = 10; y += rowHeight + 10;
            var numberRow = new[] { Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.D0 };
            foreach (var key in numberRow)
            {
                AddKeyPanel(layout, key, x, y, keySize, keySize);
                x += keySize + spacing;
            }

            // 첫 번째 글자 행 (Q-P)
            x = 10; y += rowHeight;
            var qwertyRow = new[] { Keys.Q, Keys.W, Keys.E, Keys.R, Keys.T, Keys.Y, Keys.U, Keys.I, Keys.O, Keys.P };
            foreach (var key in qwertyRow)
            {
                AddKeyPanel(layout, key, x, y, keySize, keySize);
                x += keySize + spacing;
            }

            // 두 번째 글자 행 (A-L)
            x = 20; y += rowHeight;
            var asdfRow = new[] { Keys.A, Keys.S, Keys.D, Keys.F, Keys.G, Keys.H, Keys.J, Keys.K, Keys.L };
            foreach (var key in asdfRow)
            {
                AddKeyPanel(layout, key, x, y, keySize, keySize);
                x += keySize + spacing;
            }

            // 세 번째 글자 행 (Z-M)
            x = 30; y += rowHeight;
            var zxcvRow = new[] { Keys.Z, Keys.X, Keys.C, Keys.V, Keys.B, Keys.N, Keys.M };
            foreach (var key in zxcvRow)
            {
                AddKeyPanel(layout, key, x, y, keySize, keySize);
                x += keySize + spacing;
            }

            // 수식어 키 (Ctrl, Shift, Alt, Space)
            y += rowHeight;
            x = 10;
            AddKeyPanel(layout, Keys.LControlKey, x, y, (int)(keySize * 1.2), keySize);
            x += (int)(keySize * 1.2) + spacing;
            AddKeyPanel(layout, Keys.LShiftKey, x, y, (int)(keySize * 1.2), keySize);
            x += (int)(keySize * 1.2) + spacing;
            AddKeyPanel(layout, Keys.LMenu, x, y, keySize, keySize);
            x += keySize + spacing;
            AddKeyPanel(layout, Keys.Space, x, y, (int)(keySize * 3), keySize);
            x += (int)(keySize * 3) + spacing;
            AddKeyPanel(layout, Keys.Menu, x, y, keySize, keySize);
            x += keySize + spacing;
            AddKeyPanel(layout, Keys.RControlKey, x, y, (int)(keySize * 1.2), keySize);

            // 🔥 창 크기 저장
            layout.FormWidth = 550;
            layout.FormHeight = 350;

            return layout;
        }

        // 🆕 레이아웃에 키 패널 추가 (헬퍼 메서드)
        private static void AddKeyPanel(KeyLayout layout, Keys key, int x, int y, int width, int height)
        {
            layout.Panels.Add(new KeyPanelConfig
            {
                Name = $"p{key}",
                Key = key,
                DownArgb = Color.Red.ToArgb(),
                UpArgb = SystemColors.ControlDark.ToArgb(),
                X = x,
                Y = y,
                Width = width,  // 🔥 이미 int이므로 캐스트 불필요
                Height = height,
                DisplayName = GetKeyDisplayName(key)
            });
        }

        // 🆕 키 표시 이름 생성
        private static string GetKeyDisplayName(Keys key)
        {
            return key switch
            {
                Keys.D1 => "1", Keys.D2 => "2", Keys.D3 => "3", Keys.D4 => "4", Keys.D5 => "5",
                Keys.D6 => "6", Keys.D7 => "7", Keys.D8 => "8", Keys.D9 => "9", Keys.D0 => "0",
                Keys.Q => "Q", Keys.W => "W", Keys.E => "E", Keys.R => "R", Keys.T => "T",
                Keys.Y => "Y", Keys.U => "U", Keys.I => "I", Keys.O => "O", Keys.P => "P",
                Keys.A => "A", Keys.S => "S", Keys.D => "D", Keys.F => "F", Keys.G => "G",
                Keys.H => "H", Keys.J => "J", Keys.K => "K", Keys.L => "L",
                Keys.Z => "Z", Keys.X => "X", Keys.C => "C", Keys.V => "V", Keys.B => "B", Keys.N => "N", Keys.M => "M",
                Keys.Space => "SPACE", Keys.LShiftKey => "SHIFT", Keys.RShiftKey => "SHIFT",
                Keys.LControlKey => "CTRL", Keys.RControlKey => "CTRL", Keys.LMenu => "ALT", Keys.RMenu => "ALT",
                Keys.Up => "↑", Keys.Down => "↓", Keys.Left => "←", Keys.Right => "→",
                _ => key.ToString()
            };
        }

        // 🆕 layout1: 방향키 + 수식어 레이아웃 생성
        public static KeyLayout CreateLayout1Layout(string name)
        {
            var layout = new KeyLayout { Name = name };

            // 방향키 (위)
            layout.Panels.Add(new KeyPanelConfig
            {
                Name = "panel1",
                Key = System.Windows.Forms.Keys.Up,
                DownArgb = -65536,  // 빨강
                UpArgb = -32640,    // 어두운 회색
                X = 430,
                Y = 20,
                Width = 85,
                Height = 85,
                DisplayName = "↑",
                BorderEnabled = true,
                BorderColorArgb = -1,   // 검정
                BorderWidth = 3,
                CornerRadius = 15
            });

            // 방향키 (아래)
            layout.Panels.Add(new KeyPanelConfig
            {
                Name = "panel2",
                Key = System.Windows.Forms.Keys.Down,
                DownArgb = -65536,
                UpArgb = -32640,
                X = 430,
                Y = 120,
                Width = 85,
                Height = 85,
                DisplayName = "↓",
                BorderEnabled = true,
                BorderColorArgb = -1,
                BorderWidth = 3,
                CornerRadius = 15
            });

            // 방향키 (왼쪽)
            layout.Panels.Add(new KeyPanelConfig
            {
                Name = "panel3",
                Key = System.Windows.Forms.Keys.Left,
                DownArgb = -65536,
                UpArgb = -32640,
                X = 330,
                Y = 120,
                Width = 85,
                Height = 85,
                DisplayName = "←",
                BorderEnabled = true,
                BorderColorArgb = -1,
                BorderWidth = 3,
                CornerRadius = 15
            });

            // 방향키 (오른쪽)
            layout.Panels.Add(new KeyPanelConfig
            {
                Name = "panel4",
                Key = System.Windows.Forms.Keys.Right,
                DownArgb = -65536,
                UpArgb = -32640,
                X = 530,
                Y = 120,
                Width = 85,
                Height = 85,
                DisplayName = "→",
                BorderEnabled = true,
                BorderColorArgb = -1,
                BorderWidth = 3,
                CornerRadius = 15
            });

            // Shift
            layout.Panels.Add(new KeyPanelConfig
            {
                Name = "panel5",
                Key = System.Windows.Forms.Keys.LShiftKey,
                DownArgb = -65536,
                UpArgb = -32640,
                X = 30,
                Y = 20,
                Width = 120,
                Height = 85,
                DisplayName = "Shift",
                BorderEnabled = true,
                BorderColorArgb = -1,
                BorderWidth = 3,
                CornerRadius = 15
            });

            // Ctrl
            layout.Panels.Add(new KeyPanelConfig
            {
                Name = "panel6",
                Key = System.Windows.Forms.Keys.LControlKey,
                DownArgb = -65536,
                UpArgb = -32640,
                X = 30,
                Y = 120,
                Width = 85,
                Height = 85,
                DisplayName = "Ctrl",
                BorderEnabled = true,
                BorderColorArgb = -1,
                BorderWidth = 3,
                CornerRadius = 15
            });

            // Space
            layout.Panels.Add(new KeyPanelConfig
            {
                Name = "panel7",
                Key = System.Windows.Forms.Keys.Space,
                DownArgb = -65536,
                UpArgb = -32640,
                X = 130,
                Y = 120,
                Width = 180,
                Height = 85,
                DisplayName = "Space",
                BorderEnabled = true,
                BorderColorArgb = -1,
                BorderWidth = 3,
                CornerRadius = 15
            });

            // 창 크기 저장
            layout.FormWidth = 663;
            layout.FormHeight = 250;

            return layout;
        }

        // 🆕 layout2: ASDF + Shift + Space 레이아웃 생성
        public static KeyLayout CreateLayout2Layout(string name)
        {
            var layout = new KeyLayout { Name = name };

            // A 키
            layout.Panels.Add(new KeyPanelConfig
            {
                Name = "panel3",
                Key = System.Windows.Forms.Keys.A,
                DownArgb = -16721166,
                UpArgb = -8323073,
                X = 34,
                Y = 21,
                Width = 85,
                Height = 85,
                DisplayName = "A",
                BorderEnabled = true,
                BorderColorArgb = -2949123,
                BorderWidth = 5,
                CornerRadius = 3
            });

            // S 키
            layout.Panels.Add(new KeyPanelConfig
            {
                Name = "panel4",
                Key = System.Windows.Forms.Keys.S,
                DownArgb = -16721166,
                UpArgb = -8323073,
                X = 134,
                Y = 21,
                Width = 85,
                Height = 85,
                DisplayName = "S",
                BorderEnabled = true,
                BorderColorArgb = -2949123,
                BorderWidth = 5,
                CornerRadius = 3
            });

            // D 키
            layout.Panels.Add(new KeyPanelConfig
            {
                Name = "panel5",
                Key = System.Windows.Forms.Keys.D,
                DownArgb = -16721166,
                UpArgb = -8323073,
                X = 234,
                Y = 21,
                Width = 85,
                Height = 85,
                DisplayName = "D",
                BorderEnabled = true,
                BorderColorArgb = -2949123,
                BorderWidth = 5,
                CornerRadius = 3
            });

            // L 키
            layout.Panels.Add(new KeyPanelConfig
            {
                Name = "panel6",
                Key = System.Windows.Forms.Keys.L,
                DownArgb = -16721166,
                UpArgb = -8323073,
                X = 334,
                Y = 21,
                Width = 85,
                Height = 85,
                DisplayName = "L",
                BorderEnabled = true,
                BorderColorArgb = -2949123,
                BorderWidth = 5,
                CornerRadius = 3
            });

            // ; (Oem1)
            layout.Panels.Add(new KeyPanelConfig
            {
                Name = "panel7",
                Key = System.Windows.Forms.Keys.Oem1,
                DownArgb = -16721166,
                UpArgb = -8323073,
                X = 434,
                Y = 21,
                Width = 85,
                Height = 85,
                DisplayName = ";",
                BorderEnabled = true,
                BorderColorArgb = -2949123,
                BorderWidth = 5,
                CornerRadius = 3
            });

            // ' (Oem7)
            layout.Panels.Add(new KeyPanelConfig
            {
                Name = "panel8",
                Key = System.Windows.Forms.Keys.Oem7,
                DownArgb = -16721166,
                UpArgb = -8323073,
                X = 534,
                Y = 21,
                Width = 85,
                Height = 85,
                DisplayName = "'",
                BorderEnabled = true,
                BorderColorArgb = -2949123,
                BorderWidth = 5,
                CornerRadius = 3
            });

            // Left Shift
            layout.Panels.Add(new KeyPanelConfig
            {
                Name = "panel1",
                Key = System.Windows.Forms.Keys.LShiftKey,
                DownArgb = -16721166,
                UpArgb = -8323073,
                X = 34,
                Y = 121,
                Width = 120,
                Height = 85,
                DisplayName = "LShift",
                BorderEnabled = true,
                BorderColorArgb = -2949123,
                BorderWidth = 5,
                CornerRadius = 3
            });

            // Right Shift
            layout.Panels.Add(new KeyPanelConfig
            {
                Name = "panel2",
                Key = System.Windows.Forms.Keys.RShiftKey,
                DownArgb = -16721166,
                UpArgb = -8323073,
                X = 494,
                Y = 121,
                Width = 120,
                Height = 85,
                DisplayName = "RShift",
                BorderEnabled = true,
                BorderColorArgb = -2949123,
                BorderWidth = 5,
                CornerRadius = 3
            });

            // Space
            layout.Panels.Add(new KeyPanelConfig
            {
                Name = "panel9",
                Key = System.Windows.Forms.Keys.Space,
                DownArgb = -16721166,
                UpArgb = -8323073,
                X = 234,
                Y = 121,
                Width = 85,
                Height = 85,
                DisplayName = "Space",
                BorderEnabled = true,
                BorderColorArgb = -2949123,
                BorderWidth = 5,
                CornerRadius = 3
            });

            // ALT (HangulMode 키 사용)
            layout.Panels.Add(new KeyPanelConfig
            {
                Name = "panel10",
                Key = System.Windows.Forms.Keys.LMenu,
                DownArgb = -16721166,
                UpArgb = -8323073,
                X = 334,
                Y = 121,
                Width = 85,
                Height = 85,
                DisplayName = "ALT",
                BorderEnabled = true,
                BorderColorArgb = -2949123,
                BorderWidth = 5,
                CornerRadius = 3
            });

            // 창 크기 저장
            layout.FormWidth = 650;
            layout.FormHeight = 241;

            return layout;
        }
    }
}
