using System;
using System.Drawing;
using System.Windows.Forms;

namespace keyviewer
{
    public partial class PanelEditorForm : Form
    {
        private readonly PanelEditorControl _editor;
        private readonly Button _btnOk;
        private readonly Button _btnCancel;

        public Keys SelectedKey => _editor.SelectedKey;
        public Color SelectedUpColor => _editor.SelectedUpColor;
        public Color SelectedDownColor => _editor.SelectedDownColor;
        public int SelectedAlpha => _editor.SelectedAlpha;
        public Size SelectedSize => _editor.SelectedSize;
        public string SelectedDisplayName => _editor.SelectedDisplayName;

        public PanelEditorForm()
        {
            InitializeComponent();

            this.KeyPreview = true;

            _editor = new PanelEditorControl { Dock = DockStyle.Top };
            _btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom };
            _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Dock = DockStyle.Bottom };

            _editor.Height = 240;

            Controls.Add(_editor);
            Controls.Add(_btnCancel);
            Controls.Add(_btnOk);

            ClientSize = new Size(480, 320);
            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            this.FormClosing += (s, e) =>
            {
                // 대화 플래그가 있는 컨트롤의 상태를 저장
            };
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
        }

        public PanelEditorForm(Keys initialKey, Color upColor, Color downColor) : this()
        {
            _editor.SelectedKey = initialKey;
            _editor.SelectedUpColor = upColor;
            _editor.SelectedDownColor = downColor;
            _editor.SelectedAlpha = upColor.A;
        }

        // 🆕 크기를 포함한 생성자 오버로드
        public PanelEditorForm(Keys initialKey, Color upColor, Color downColor, string displayName, Size size) : this()
        {
            _editor.SelectedKey = initialKey;
            _editor.SelectedUpColor = upColor;
            _editor.SelectedDownColor = downColor;
            _editor.SelectedAlpha = upColor.A;
            _editor.SelectedDisplayName = displayName;
            
            // ✅ SelectedSize 대신 Width와 Height 개별 설정
            _editor.SelectedWidth = size.Width;
            _editor.SelectedHeight = size.Height;
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            ClientSize = new Size(284, 261);
            Name = "PanelEditorForm";
            Load += PanelEditorForm_Load;
            ResumeLayout(false);
        }

        private void PanelEditorForm_Load(object sender, EventArgs e)
        {

        }
    }
}