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
        public Color ChromaKeyColor => _editor.ChromaKeyColor; // 추가

        public GlobalEditorForm(Color initialUp, Color initialDown, Color initialBg, 
            string? initialBgImagePath, int initialKeyAlpha, int initialOpacityPercent, 
            bool initialTransparent = false, Color? chromaKeyColor = null)
        {
            InitializeComponent();

            _editor = new GlobalEditorControl { Dock = DockStyle.Fill };
            
            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 35 };
            _btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 75, Height = 28, Left = 10, Top = 4 };
            _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 75, Height = 28, Left = 95, Top = 4 };
            
            buttonPanel.Controls.Add(_btnOk);
            buttonPanel.Controls.Add(_btnCancel);

            Controls.Add(buttonPanel);
            Controls.Add(_editor);

            // 초기값 전달
            _editor.SelectedUpColor = initialUp;
            _editor.SelectedDownColor = initialDown;
            _editor.SelectedBgColor = initialBg;
            _editor.SelectedKeyAlpha = initialKeyAlpha;
            _editor.SelectedOpacityPercent = initialOpacityPercent;
            _editor.BackgroundTransparent = initialTransparent;
            if (chromaKeyColor.HasValue)
                _editor.ChromaKeyColor = chromaKeyColor.Value;

            ClientSize = new Size(Math.Max(520, _editor.Width), 315 + 35); // 높이 증가
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
            ClientSize = new Size(520, 350);
            Name = "GlobalEditorForm";
            ResumeLayout(false);
        }
    }
}