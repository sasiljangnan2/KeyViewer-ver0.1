using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace keyviewer
{
    public partial class PanelEditorControl : UserControl
    {

        private bool _isRecording = false; // 녹화 상태 플래그
        private Keys _lastRecordedKey = Keys.None; // 마지막으로 입력된 키를 녹화하기 위한 변수

        private void BtnRecord_Click(object sender, EventArgs e) // 단축키설정 녹화시작
        {
            if (!_isRecording) // 녹화가 시작됨
            {

                _isRecording = true;
                _btnRecord.Text = "설정 중지하기";
                _btnRecord.BackColor = Color.Red;
                _btnRecord.ForeColor = Color.White;

                _cbKeys.BackColor = Color.LemonChiffon;
                _lblCurrentKeyInfo.Text = "키보드 키를 누르세요...";

                // 포커스를 이 컨트롤로 가져와야 키 입력을 직접 받음
                this.Focus();
            }
            else // 녹화가 중지됨
             {
                StopRecording();
            }
        }
        //단축키 녹화 중지
        private void StopRecording()
        {
            _isRecording = false;
            _btnRecord.Text = "단축키 설정";
            _btnRecord.BackColor = SystemColors.Control;
            _btnRecord.ForeColor = SystemColors.ControlText;
            _cbKeys.BackColor = SystemColors.Window;

            //콤보박스 막혔을 경우 다시 활성화
            if (_cbKeys != null) _cbKeys.Enabled = true;
            
            if (_lastRecordedKey != Keys.None)
            {
                this.SelectedKey = _lastRecordedKey;
                _lblCurrentKeyInfo.Text = $"설정됨: {_lastRecordedKey}";
            }
        }

        private void BtnStop_Click(object sender, EventArgs e) // 녹화 시작 시 작동하며 녹화를 중지하는 버튼 클릭 핸들러
        {
            _isRecording = false;
            _btnRecord.Text = "단축키 설정";
            _cbKeys.BackColor = SystemColors.Window;
            _btnRecord.BackColor = SystemColors.Control;
            _btnRecord.ForeColor = SystemColors.ControlText;

            if (_cbKeys != null) _cbKeys.Enabled = true;
            // 녹화된 마지막 키를 ComboBox에 반영
            if (_lastRecordedKey != Keys.None)
            {
                this.SelectedKey = _lastRecordedKey;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_isRecording)
            {
                // 가장 마지막으로 누른 키만 저장(그 앞에 누른 키들은 제외)
                _lastRecordedKey = keyData;
                _cbKeys.Text = keyData.ToString();
                _lblCurrentKeyInfo.Text = $"입력된 키: {keyData}"; // 키 이름을 띄움

                return true; // 이 키 입력을 시스템에 넘기지 않고 여기서 소모함
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnMouseDown(MouseEventArgs e) // 마우스 버튼을 입력할 때 녹화기능 작동
        {
            if (_isRecording)
            {
                if (e.Button == MouseButtons.Left) _lastRecordedKey = Keys.LButton;
                else if (e.Button == MouseButtons.Right) _lastRecordedKey = Keys.RButton;

                _lblCurrentKeyInfo.Text = $"입력된 키: {_lastRecordedKey}";
                _cbKeys.Text = _lastRecordedKey.ToString(); // 화면에 즉시 표시
            }
            base.OnMouseDown(e);
        }

        public PanelEditorControl()
        {
            InitializeComponent();
        }

        // 디자이너 안전 이벤트 핸들러
        private void BtnUpColor_Click(object? sender, EventArgs e)
        {
            if (_colorDialog == null || _previewUp == null) return;
            _colorDialog.Color = _previewUp.BackColor;
            if (_colorDialog.ShowDialog(this) == DialogResult.OK)
                _previewUp.BackColor = _colorDialog.Color;
        }

        private void BtnDownColor_Click(object? sender, EventArgs e)
        {
            if (_colorDialog == null || _previewDown == null) return;
            _colorDialog.Color = _previewDown.BackColor;
            if (_colorDialog.ShowDialog(this) == DialogResult.OK)
                _previewDown.BackColor = _colorDialog.Color;
        }

        private void TbAlpha_Scroll(object? sender, EventArgs e)
        {
            if (_lblAlpha != null && _tbAlpha != null)
                _lblAlpha.Text = $"Alpha: {_tbAlpha.Value}";
        }

        private void PanelEditorControl_Load(object? sender, EventArgs e)
        {
            bool inDesigner = DesignMode || (LicenseManager.UsageMode == LicenseUsageMode.Designtime);
            if (inDesigner) return;

            if (_cbKeys == null) return;


        }

        // 안전한 접근 헬퍼
        private int SafeAlpha => _tbAlpha?.Value ?? 255;
        private Color SafePreviewUp => _previewUp?.BackColor ?? Color.Gray;
        private Color SafePreviewDown => _previewDown?.BackColor ?? Color.Red;

        // 공개 속성
        [Browsable(true)]
        [Category("Behavior")]
        [Description("Selected key for the panel.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(Keys.None)]
        public Keys SelectedKey
        {
            get => _lastRecordedKey;
            set
            {
                if (_cbKeys == null) return;

                _lastRecordedKey = value; // 녹화 변수에 값 저장
                _cbKeys.Text = value.ToString(); // 콤보박스 화면에 글자로만 표시
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Panel Up color (RGB). Alpha is controlled by SelectedAlpha).")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [TypeConverter(typeof(ColorConverter))]
        [DefaultValue(typeof(Color), "Gray")]
        public Color SelectedUpColor
        {
            get => Color.FromArgb(SafeAlpha, SafePreviewUp);
            set
            {
                if (value == Color.Empty) return;
                if (_previewUp != null)
                    _previewUp.BackColor = Color.FromArgb(255, value.R, value.G, value.B);
                if (_tbAlpha != null)
                    _tbAlpha.Value = Math.Clamp(value.A, _tbAlpha.Minimum, _tbAlpha.Maximum);
                if (_lblAlpha != null)
                    _lblAlpha.Text = $"Alpha: {_tbAlpha?.Value ?? value.A}";
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Panel Down color (RGB). Alpha is controlled by SelectedAlpha).")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [TypeConverter(typeof(ColorConverter))]
        [DefaultValue(typeof(Color), "Red")]
        public Color SelectedDownColor
        {
            get => Color.FromArgb(SafeAlpha, SafePreviewDown);
            set
            {
                if (value == Color.Empty) return;
                if (_previewDown != null)
                    _previewDown.BackColor = Color.FromArgb(255, value.R, value.G, value.B);
                if (_tbAlpha != null)
                    _tbAlpha.Value = Math.Clamp(value.A, _tbAlpha.Minimum, _tbAlpha.Maximum);
                if (_lblAlpha != null)
                    _lblAlpha.Text = $"Alpha: {_tbAlpha?.Value ?? value.A}";
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Alpha value for the key colors (0-255).")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(255)]
        public int SelectedAlpha
        {
            get => _tbAlpha?.Value ?? 255;
            set
            {
                if (_tbAlpha != null)
                    _tbAlpha.Value = Math.Clamp(value, _tbAlpha.Minimum, _tbAlpha.Maximum);
                if (_lblAlpha != null)
                    _lblAlpha.Text = $"Alpha: {_tbAlpha?.Value ?? value}";
            }
        }

        // 새로 추가: 패널 너비
        [Browsable(true)]
        [Category("Layout")]
        [Description("Panel width in pixels.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(85)]
        public int SelectedWidth
        {
            get => _numWidth?.Value != null ? (int)_numWidth.Value : 85;
            set
            {
                if (_numWidth != null)
                    _numWidth.Value = Math.Clamp(value, (int)_numWidth.Minimum, (int)_numWidth.Maximum);
            }
        }

        // 새로 추가: 패널 높이
        [Browsable(true)]
        [Category("Layout")]
        [Description("Panel height in pixels.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(85)]
        public int SelectedHeight
        {
            get => _numHeight?.Value != null ? (int)_numHeight.Value : 85;
            set
            {
                if (_numHeight != null)
                    _numHeight.Value = Math.Clamp(value, (int)_numHeight.Minimum, (int)_numHeight.Maximum);
            }
        }

        // 크기를 Size 타입으로 반환
        [Browsable(false)]
        public Size SelectedSize => new Size(SelectedWidth, SelectedHeight);

        // 커스텀 이름 필드
        [Browsable(true)]
        [Category("Behavior")]
        [Description("Custom display name for the key panel. Leave empty to use default key name.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue("")]
        public string SelectedDisplayName
        {
            get => _txtDisplayName?.Text ?? "";
            set
            {
                if (_txtDisplayName != null)
                    _txtDisplayName.Text = value ?? "";
            }
        }
    }
}