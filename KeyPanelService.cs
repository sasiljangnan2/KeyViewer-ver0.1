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

        public List<KeyPanel> KeyPanels { get; } = new List<KeyPanel>();

        public KeyPanelService(Form form, Color defaultColor,
            MouseEventHandler? mouseDown = null,
            MouseEventHandler? mouseMove = null,
            MouseEventHandler? mouseUp = null)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _defaultColor = defaultColor;
            _mouseDown = mouseDown;
            _mouseMove = mouseMove;
            _mouseUp = mouseUp;
        }

        // 디자이너에서 만든 기존 Panel을 KeyPanel로 래핑하고 이벤트 연결
        public KeyPanel WrapExistingPanel(Panel panel, Keys key, Color downColor, Color upColor)
        {
            if (panel == null) throw new ArgumentNullException(nameof(panel));
            AttachMouseHandlers(panel);
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

            _form.Controls.Add(panel);
            AttachMouseHandlers(panel);

            var kp = new KeyPanel(panel, key, downColor, upColor);
            KeyPanels.Add(kp);
            return kp;
        }

        // 단일 팩토리: 필요하면 외부에서 직접 패널을 만들 때 사용
        public Panel CreateButtonPanel(string name, Point location, Size size, int tabIndex)
        {
            var p = new Panel
            {
                BackColor = _defaultColor,
                Location = location,
                Name = name,
                Size = size,
                TabIndex = tabIndex
            };
            AttachMouseHandlers(p);
            return p;
        }

        private void AttachMouseHandlers(Panel p)
        {
            if (_mouseDown != null) p.MouseDown += _mouseDown;
            if (_mouseMove != null) p.MouseMove += _mouseMove;
            if (_mouseUp != null) p.MouseUp += _mouseUp;
        }
    }
}