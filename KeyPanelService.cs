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

        public List<KeyPanel> KeyPanels { get; } = new List<KeyPanel>();

        public KeyPanelService(Form form, Color defaultColor,
            MouseEventHandler? mouseDown = null,
            MouseEventHandler? mouseMove = null,
            MouseEventHandler? mouseUp = null,
            ContextMenuStrip? contextMenu = null)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _defaultColor = defaultColor;
            _mouseDown = mouseDown;
            _mouseMove = mouseMove;
            _mouseUp = mouseUp;
            _contextMenu = contextMenu;
        }

        public KeyPanel WrapExistingPanel(Panel panel, Keys key, Color downColor, Color upColor)
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));

            ApplyContextAndHandlersRecursive(panel);

            var kp = new KeyPanel(panel, key, downColor, upColor);
            KeyPanels.Add(kp);
            AttachLayeredWindowHandlers(kp); // 레이어드 윈도우 핸들러 연결
            return kp;
        }

        public KeyPanel AddKeyPanel(Keys key, Color downColor, Color upColor, Point location, Size? size = null)
        {
            Size panelSize = size ?? new Size(85, 85); // 기본 크기를 85x85로 변경
            string nameBase = "panel";
            int idx = 1;
            while (_form.Controls.Find(nameBase + idx, false).Length > 0) idx++;
            string name = nameBase + idx;

            // 더미 패널(위치/크기 저장용, 숨김)
            var panel = new Panel
            {
                BackColor = _defaultColor,
                Location = location,
                Name = name,
                Size = panelSize,
                TabIndex = _form.Controls.Count,
                Visible = false // 레이어드 윈도우가 대신 표시됨
            };

            ApplyContextAndHandlersRecursive(panel);
            _form.Controls.Add(panel);

            var kp = new KeyPanel(panel, key, downColor, upColor);
            KeyPanels.Add(kp);
            
            // 레이어드 윈도우에 마우스 핸들러 연결
            AttachLayeredWindowHandlers(kp);
            
            return kp;
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
    }
}