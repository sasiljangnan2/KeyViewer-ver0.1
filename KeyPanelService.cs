using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace keyviewer
{
    public class KeyPanelService
    {
        private readonly Form _form;
        private readonly Color _defaultColor;
        private readonly MouseEventHandler? _mouseDown;
        private readonly MouseEventHandler? _mouseMove;
        private readonly MouseEventHandler? _mouseUp;
        private ContextMenuStrip? _contextMenu;
        private bool _obsCompatibilityMode;

        public List<KeyPanel> KeyPanels { get; } = new List<KeyPanel>();

        public KeyPanelService(Form form, Color defaultColor,
            MouseEventHandler? mouseDown = null,
            MouseEventHandler? mouseMove = null,
            MouseEventHandler? mouseUp = null,
            ContextMenuStrip? contextMenu = null,
            bool obsCompatibilityMode = false)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _defaultColor = defaultColor;
            _mouseDown = mouseDown;
            _mouseMove = mouseMove;
            _mouseUp = mouseUp;
            _contextMenu = contextMenu;
            _obsCompatibilityMode = obsCompatibilityMode;
        }

        public KeyPanel WrapExistingPanel(Panel panel, Keys key, Color downColor, Color upColor)
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));

            ApplyContextAndHandlersRecursive(panel);
            
            if (_obsCompatibilityMode)
            {
                // OBS 모드: 패널에 텍스트 그리기 및 드래그 핸들러 연결
                panel.Visible = true;
                panel.Paint += (s, e) =>
                {
                    if (s is Panel p)
                    {
                        var kp = KeyPanels.Find(k => k.Panel == p);
                        if (kp != null)
                        {
                            DrawKeyText(e.Graphics, p, kp.Key, p.BackColor);
                        }
                    }
                };
                AttachPanelDragHandlers(panel);
            }
            
            var kp = new KeyPanel(panel, key, downColor, upColor, _obsCompatibilityMode);
            KeyPanels.Add(kp);
            
            if (!_obsCompatibilityMode)
                AttachLayeredWindowHandlers(kp);
            
            return kp;
        }

        public KeyPanel AddKeyPanel(Keys key, Color downColor, Color upColor, Point location, Size? size = null)
        {
            Size panelSize = size ?? new Size(85, 85);
            string nameBase = "panel";
            int idx = 1;
            while (_form.Controls.Find(nameBase + idx, false).Length > 0) idx++;
            string name = nameBase + idx;

            var panel = new Panel
            {
                BackColor = _defaultColor,
                Location = location,
                Name = name,
                Size = panelSize,
                TabIndex = _form.Controls.Count,
                Visible = _obsCompatibilityMode // OBS 모드에서는 보이도록
            };

            if (_obsCompatibilityMode)
            {
                // OBS 모드: 패널에 직접 텍스트 그리기
                panel.Paint += (s, e) =>
                {
                    if (s is Panel p)
                    {
                        // KeyPanel을 찾아서 현재 색상 가져오기
                        var kp = KeyPanels.Find(k => k.Panel == p);
                        if (kp != null)
                        {
                            DrawKeyText(e.Graphics, p, kp.Key, p.BackColor);
                        }
                    }
                };
                
                // OBS 모드: 패널 드래그 핸들러 연결
                AttachPanelDragHandlers(panel);
            }

            ApplyContextAndHandlersRecursive(panel);
            _form.Controls.Add(panel);

            var kp = new KeyPanel(panel, key, downColor, upColor, _obsCompatibilityMode);
            KeyPanels.Add(kp);
            
            if (!_obsCompatibilityMode)
                AttachLayeredWindowHandlers(kp);
            
            return kp;
        }

        private void DrawKeyText(Graphics g, Panel panel, Keys key, Color bgColor)
        {
            string keyText = GetKeyDisplayName(key);
            
            // 레이어드 윈도우 모드와 동일한 폰트 크기 계산
            int fontSize = Math.Max(8, Math.Min(panel.Width, panel.Height) / 3);
            using var font = new Font("Arial", fontSize, FontStyle.Bold);
            using var brush = new SolidBrush(GetContrastColor(bgColor));
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.DrawString(keyText, font, brush, panel.ClientRectangle, sf);
        }

        private string GetKeyDisplayName(Keys key)
        {
            return key switch
            {
                Keys.LControlKey or Keys.RControlKey or Keys.ControlKey => "Ctrl",
                Keys.LShiftKey or Keys.RShiftKey or Keys.ShiftKey => "Shift",
                Keys.LMenu or Keys.RMenu or Keys.Menu => "Alt",
                Keys.Space => "Space",
                _ => key.ToString().Replace("Key", "")
            };
        }

        private Color GetContrastColor(Color bg)
        {
            int yiq = ((bg.R * 299) + (bg.G * 587) + (bg.B * 114)) / 1000;
            return yiq >= 128 ? Color.Black : Color.White;
        }

        // 레이어드 윈도우에 드래그/우클릭 핸들러 연결
        private void AttachLayeredWindowHandlers(KeyPanel kp)
        {
            if (kp.LayeredWindow == null) return;

            // 드래그 지원: 레이어드 윈도우를 드래그하면 더미 패널 위치도 업데이트
            Point dragStart = Point.Empty;
            Point locStart = Point.Empty;
            bool dragging = false;

            kp.LayeredWindow.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    _contextMenu?.Show(kp.LayeredWindow, e.Location);
                    return;
                }
                if (e.Button == MouseButtons.Left)
                {
                    dragging = true;
                    dragStart = Control.MousePosition;
                    locStart = kp.LayeredWindow.Location;
                    kp.LayeredWindow.Capture = true;
                }
            };

            kp.LayeredWindow.MouseMove += (s, e) =>
            {
                if (!dragging) return;
                Point current = Control.MousePosition;
                int dx = current.X - dragStart.X;
                int dy = current.Y - dragStart.Y;
                Point newLoc = new Point(locStart.X + dx, locStart.Y + dy);
                
                // 그리드 스냅 (10px)
                newLoc.X -= newLoc.X % 10;
                newLoc.Y -= newLoc.Y % 10;

                // 폼 경계 내로 제한 (화면 좌표)
                if (kp.Panel.Parent != null)
                {
                    var formScreenBounds = new Rectangle(
                        _form.PointToScreen(Point.Empty),
                        _form.ClientSize
                    );

                    int maxX = formScreenBounds.Right - kp.LayeredWindow.Width;
                    int maxY = formScreenBounds.Bottom - kp.LayeredWindow.Height;
                    
                    newLoc.X = Math.Clamp(newLoc.X, formScreenBounds.Left, maxX);
                    newLoc.Y = Math.Clamp(newLoc.Y, formScreenBounds.Top, maxY);
                }

                kp.LayeredWindow.Location = newLoc;
                
                // 더미 패널 위치 업데이트 (폼 좌표)
                if (kp.Panel.Parent != null)
                {
                    kp.Panel.Location = kp.Panel.Parent.PointToClient(newLoc);
                }
            };

            kp.LayeredWindow.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    kp.LayeredWindow.Capture = false;
                    dragging = false;
                }
            };

            kp.LayeredWindow.ContextMenuStrip = _contextMenu;
        }

        public void SetContextMenu(ContextMenuStrip? contextMenu)
        {
            _contextMenu = contextMenu;
            foreach (var kp in KeyPanels)
            {
                kp.Panel.ContextMenuStrip = _contextMenu;
                if (kp.LayeredWindow != null)
                    kp.LayeredWindow.ContextMenuStrip = _contextMenu;
            }
        }

        private void ApplyContextAndHandlersRecursive(Control ctl)
        {
            if (_contextMenu != null)
                ctl.ContextMenuStrip = _contextMenu;

            AttachMouseHandlers(ctl);
            ctl.ControlAdded -= OnControlAdded;
            ctl.ControlAdded += OnControlAdded;

            foreach (Control child in ctl.Controls)
            {
                if (_contextMenu != null)
                    child.ContextMenuStrip = _contextMenu;
                AttachMouseHandlers(child);
            }
        }

        private void OnControlAdded(object? sender, ControlEventArgs e)
        {
            if (e?.Control == null) return;
            if (_contextMenu != null)
                e.Control.ContextMenuStrip = _contextMenu;
            AttachMouseHandlers(e.Control);
            e.Control.ControlAdded -= OnControlAdded;
            e.Control.ControlAdded += OnControlAdded;
        }

        private void AttachMouseHandlers(Control c)
        {
            if (_mouseDown != null)
            {
                c.MouseDown -= _mouseDown;
                c.MouseDown += _mouseDown;
            }
            if (_mouseMove != null)
            {
                c.MouseMove -= _mouseMove;
                c.MouseMove += _mouseMove;
            }
            if (_mouseUp != null)
            {
                c.MouseUp -= _mouseUp;
                c.MouseUp += _mouseUp;
            }
        }

        // OBS 모드용 패널 드래그 핸들러
        private void AttachPanelDragHandlers(Panel panel)
        {
            bool dragging = false;
            Point dragStart = Point.Empty;
            Point locStart = Point.Empty;

            panel.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    dragging = true;
                    dragStart = Control.MousePosition;
                    locStart = panel.Location;
                    panel.Capture = true;
                }
            };

            panel.MouseMove += (s, e) =>
            {
                if (!dragging) return;
                
                Point current = Control.MousePosition;
                int dx = current.X - dragStart.X;
                int dy = current.Y - dragStart.Y;
                Point newLoc = new Point(locStart.X + dx, locStart.Y + dy);

                // 그리드 스냅 (10px)
                newLoc.X -= newLoc.X % 10;
                newLoc.Y -= newLoc.Y % 10;

                // 폼 경계 내로 제한
                newLoc.X = Math.Clamp(newLoc.X, 0, Math.Max(0, _form.ClientSize.Width - panel.Width));
                newLoc.Y = Math.Clamp(newLoc.Y, 0, Math.Max(0, _form.ClientSize.Height - panel.Height));

                panel.Location = newLoc;
            };

            panel.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    panel.Capture = false;
                    dragging = false;
                }
            };
        }
    }
}