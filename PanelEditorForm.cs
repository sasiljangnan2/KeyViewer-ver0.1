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
        public Size SelectedSize => _editor.SelectedSize; // 새로 추가
        public string SelectedDisplayName => _editor.SelectedDisplayName; // 커스텀 이름

        public PanelEditorForm()
        {
            InitializeComponent();

            this.KeyPreview = true; // 키 입력 가로채지 못하게

            // UI 컨트롤 생성 로직
            _editor = new PanelEditorControl { Dock = DockStyle.Top };
            _btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom, Height = 40 };
            _btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Dock = DockStyle.Bottom, Height = 40 };

            _editor.Height = 260;

            Controls.Add(_editor);
            Controls.Add(_btnCancel);
            Controls.Add(_btnOk);


            ClientSize = new Size(480, 340);
            CancelButton = _btnCancel;

            this.FormClosing += (s, e) =>
            {
                // 녹화 플래그가 있는 컨트롤의 상태를 해제
            };
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // 녹화 중일 때 창을 닫으면 플래그를 꺼줌
            base.OnFormClosing(e);
        }

        public PanelEditorForm(Keys initialKey, Color upColor, Color downColor) : this()
        {
            _editor.SelectedKey = initialKey;
            _editor.SelectedUpColor = upColor;
            _editor.SelectedDownColor = downColor;
            _editor.SelectedAlpha = upColor.A;
            // 크기는 기존 패널 편집 시 변경하지 않도록 초기값 그대로 둠 (85x85)
        }

        public PanelEditorForm(Keys initialKey, Color upColor, Color downColor, string displayName) : this()
        {
            _editor.SelectedKey = initialKey;
            _editor.SelectedUpColor = upColor;
            _editor.SelectedDownColor = downColor;
            _editor.SelectedAlpha = upColor.A;
            _editor.SelectedDisplayName = displayName;
        }

        // 간단한 no-op InitializeComponent 구현: 디자이너 미사용 호스트 폼에서 에러 방지
        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // PanelEditorForm
            // 
            ClientSize = new Size(284, 261);
            Name = "PanelEditorForm";
            Load += PanelEditorForm_Load;
            ResumeLayout(false);
            // 디자이너가 생성한 초기화 코드가 없을 때를 대비한 빈 구현입니다.
            // 필요하면 여기나 별도의 PanelEditorForm.Designer.cs에 초기화 코드를 추가하세요.
        }

        private void PanelEditorForm_Load(object sender, EventArgs e)
        {

        }
    }
}