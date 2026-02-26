using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace keyviewer
{
    public partial class PanelEditorControl : UserControl
    {
        public PanelEditorControl()
        {
            InitializeComponent();
        }

        // -----------------------
        // 디자이너 안전 이벤트 핸들러
        // -----------------------
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

        // 런타임에 콤보박스 항목을 채움(디자이너 모드면 생략)
        private void PanelEditorControl_Load(object? sender, EventArgs e)
        {
            // DesignMode 체크: 디자이너에서 InitializeComponent가 호출될 때는 true여야 함.
            bool inDesigner = DesignMode || (LicenseManager.UsageMode == LicenseUsageMode.Designtime);
            if (inDesigner) return;

            if (_cbKeys == null) return;

            _cbKeys.Items.Clear();
            foreach (Keys k in Enum.GetValues(typeof(Keys)))
            {
                _cbKeys.Items.Add(k);
            }

            // 기존 SelectedKey 값이 있으면 선택, 없으면 0
            if (_cbKeys.Items.Count > 0)
            {
                if (SelectedKey != Keys.None && _cbKeys.Items.Contains(SelectedKey))
                    _cbKeys.SelectedItem = SelectedKey;
                else
                    _cbKeys.SelectedIndex = 0;
            }
        }

        // 안전한 접근 헬퍼
        private int SafeAlpha => _tbAlpha?.Value ?? 255;
        private Color SafePreviewUp => _previewUp?.BackColor ?? Color.Gray;
        private Color SafePreviewDown => _previewDown?.BackColor ?? Color.Red;

        // -----------------------
        // 공개 속성 (디자이너 친화적)
        // -----------------------
        [Browsable(true)]
        [Category("Behavior")]
        [Description("Selected key for the panel.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(Keys.None)]
        public Keys SelectedKey
        {
            get => _cbKeys?.SelectedItem is Keys k ? k : Keys.None;
            set
            {
                if (_cbKeys == null) return;
                if (_cbKeys.Items.Contains(value))
                    _cbKeys.SelectedItem = value;
                else if (_cbKeys.Items.Count > 0)
                    _cbKeys.SelectedIndex = 0;
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
    }
}