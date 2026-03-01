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

        // 디자이너에서 만든 기존 Panel을 KeyPanel로 래핑하고 이벤트/컨텍스트 연결
        public KeyPanel WrapExistingPanel(Panel panel, Keys key, Color downColor, Color upColor)
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));

            ApplyContextAndHandlersRecursive(panel);

            var kp = new KeyPanel(panel, key, downColor, upColor);
            KeyPanels.Add(kp);
            return kp;
        }

        // 런타임에서 새 Panel을 생성하고 KeyPanel로 추가
        public KeyPanel AddKeyPanel(Keys key, Color downColor, Color upColor, Point location, Size? size = null)
        {
            Size panelSize = size ?? new Size(104, 96);
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
                TabIndex = _form.Controls.Count
            };

            // 일관된 설정 적용(컨텍스트, 마우스 핸들러, ControlAdded 구독)
            ApplyContextAndHandlersRecursive(panel);

            _form.Controls.Add(panel);

            var kp = new KeyPanel(panel, key, downColor, upColor);
            KeyPanels.Add(kp);
            return kp;
        }

        // 컨텍스트 메뉴를 나중에 변경할 때 기존 패널들에 적용
        public void SetContextMenu(ContextMenuStrip? contextMenu)
        {
            _contextMenu = contextMenu;
            foreach (var kp in KeyPanels)
            {
                kp.Panel.ContextMenuStrip = _contextMenu;
                foreach (Control child in kp.Panel.Controls)
                {
                    child.ContextMenuStrip = _contextMenu;
                }
            }
        }

        // 패널 및 자식 컨트롤에 일관된 설정을 적용하고 ControlAdded를 통해
        // 런타임에 추가되는 자식에도 동일 동작 적용
        private void ApplyContextAndHandlersRecursive(Control ctl)
        {
            if (_contextMenu != null)
                ctl.ContextMenuStrip = _contextMenu;

            AttachMouseHandlers(ctl);

            // ControlAdded 구독: 자식이 추가되면 자동으로 동일 설정 적용
            ctl.ControlAdded -= OnControlAdded;
            ctl.ControlAdded += OnControlAdded;

            // 이미 있는 자식들에도 적용
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

            // 재귀 구독 (자식의 자식에도 적용되도록)
            e.Control.ControlAdded -= OnControlAdded;
            e.Control.ControlAdded += OnControlAdded;
        }

        private void AttachMouseHandlers(Control c)
        {
            if (_mouseDown != null)
            {
                c.MouseDown -= _mouseDown; // 중복 구독 방지
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