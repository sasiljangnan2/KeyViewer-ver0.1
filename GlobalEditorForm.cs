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
        public bool BackgroundTransparent => _editor.BackgroundTransparent;
        public Color ChromaKeyColor => _editor.ChromaKeyColor;

        public GlobalEditorForm(Color initialUp, Color initialDown, Color initialBg, 
            string? initialBgImagePath, int initialKeyAlpha, int initialOpacityPercent, 
            bool initialTransparent = false, Color? chromaKeyColor = null)
        {
            InitializeComponent();

            _editor = new GlobalEditorControl { Dock = DockStyle.Top };
            
            // 🆕 PanelEditorForm과 동일한 버튼 스타일
            _btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Size = new Size(380, 35),
                Location = new Point(50, 295)
            };
            _btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Size = new Size(380, 35),
                Location = new Point(50, 335)
            };

            _editor.Height = 290;

            Controls.Add(_editor);
            Controls.Add(_btnCancel);
            Controls.Add(_btnOk);

            // 초기값 전달
            _editor.SelectedUpColor = initialUp;
            _editor.SelectedDownColor = initialDown;
            _editor.SelectedBgColor = initialBg;
            _editor.SelectedKeyAlpha = initialKeyAlpha;
            _editor.SelectedOpacityPercent = initialOpacityPercent;
            _editor.BackgroundTransparent = initialTransparent;
            if (chromaKeyColor.HasValue)
                _editor.ChromaKeyColor = chromaKeyColor.Value;

            ClientSize = new Size(480, 380);
            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Global Settings";
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            ClientSize = new Size(480, 380);
            Name = "GlobalEditorForm";
            ResumeLayout(false);
        }
    }
}