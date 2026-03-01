using System;
using System.Drawing;
using System.Windows.Forms;

namespace keyviewer
{
    public partial class GlobalEditorForm : Form
    {
        private readonly GlobalEditorControl _editor;
        private readonly Button _btnOk;
        private readonly Button _btnCancel;

        public Color SelectedUpColor => _editor.SelectedUpColor;
        public Color SelectedDownColor => _editor.SelectedDownColor;
        public int SelectedKeyAlpha => _editor.SelectedKeyAlpha;
        public Color SelectedBgColor => _editor.SelectedBgColor;
        public string? SelectedBgImagePath => _editor.SelectedBgImagePath;
        public int SelectedOpacityPercent => _editor.SelectedOpacityPercent;
        public bool BackgroundTransparent => _editor.BackgroundTransparent; // 새 속성

        public GlobalEditorForm(Color initialUp, Color initialDown, Color initialBg, string? initialBgImagePath, int initialKeyAlpha, int initialOpacityPercent, bool initialTransparent = false)
        {
            InitializeComponent();

            _editor = new GlobalEditorControl { Dock = DockStyle.Top };
            _btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom };
            _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Dock = DockStyle.Bottom };

            Controls.Add(_editor);
            Controls.Add(_btnCancel);
            Controls.Add(_btnOk);

            // 초기값 전달
            _editor.SelectedUpColor = initialUp;
            _editor.SelectedDownColor = initialDown;
            _editor.SelectedBgColor = initialBg;
            _editor.SelectedKeyAlpha = initialKeyAlpha;
            _editor.SelectedOpacityPercent = initialOpacityPercent;
            _editor.BackgroundTransparent = initialTransparent; // 새 초기값

            ClientSize = new Size(Math.Max(520, _editor.Width), _editor.Height + 56);
            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        private void InitializeComponent()
        {
            // 필요하면 여기에 디자이너 초기화 코드를 추가하거나
            // Partial class로 .Designer.cs 파일을 생성하여 UI 초기화를 분리하세요.
        }
    }
}