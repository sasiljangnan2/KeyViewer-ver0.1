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

        public GlobalEditorForm(Color initialUp, Color initialDown, Color initialBg, string? initialBgImagePath, int initialKeyAlpha, int initialOpacityPercent)
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

            ClientSize = new Size(Math.Max(520, _editor.Width), _editor.Height + 56);
            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }
        // 간단한 no-op InitializeComponent: 디자이너가 없는 호스트 폼에서 빈 구현으로 에러 방지
        private void InitializeComponent()
        {
            // 필요하면 여기에 디자이너 초기화 코드를 추가하거나
            // Partial class로 .Designer.cs 파일을 생성하여 UI 초기화를 분리하세요.
        }
    }
}