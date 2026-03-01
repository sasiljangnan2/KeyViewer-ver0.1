using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace keyviewer
{
    public partial class GlobalEditorControl : UserControl
    {
        public GlobalEditorControl()
        {
            InitializeComponent();
        }

        // 이벤트 핸들러: 디자이너에서 안전하게 참조되는 명명된 메서드로 구현
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

        private void BtnBgColor_Click(object? sender, EventArgs e)
        {
            if (_colorDialog == null || _previewBg == null) return;
            _colorDialog.Color = _previewBg.BackColor;
            if (_colorDialog.ShowDialog(this) == DialogResult.OK)
                _previewBg.BackColor = _colorDialog.Color;
        }

        private void BtnBgImage_Click(object? sender, EventArgs e)
        {
            if (_openFileDialog == null || _bgImageLabel == null) return;
            if (_openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                SelectedBgImagePath = _openFileDialog.FileName;
                _bgImageLabel.Text = Path.GetFileName(SelectedBgImagePath);
            }
        }

        private void TbKeyAlpha_Scroll(object? sender, EventArgs e)
        {
            if (_lblKeyAlpha != null && _tbKeyAlpha != null)
                _lblKeyAlpha.Text = $"Key Alpha: {_tbKeyAlpha.Value}";
        }

        private void TbWindowOpacity_Scroll(object? sender, EventArgs e)
        {
            if (_lblWindowOpacity != null && _tbWindowOpacity != null)
                _lblWindowOpacity.Text = $"Background Alpha: {_tbWindowOpacity.Value}";
        }

        // 기존 속성들 (안전한 null 처리 포함)
        private int SafeKeyAlpha => _tbKeyAlpha?.Value ?? 255;
        private Color SafePreviewUpColor => _previewUp?.BackColor ?? Color.Gray;
        private Color SafePreviewDownColor => _previewDown?.BackColor ?? Color.Red;
        private Color SafePreviewBgColor => _previewBg?.BackColor ?? SystemColors.ControlDark;

        [Browsable(true)]
        [Category("Appearance")]
        [Description("All keys up color (includes alpha via Key Alpha).")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SelectedUpColor
        {
            get
            {
                try
                {
                    return Color.FromArgb(SafeKeyAlpha, SafePreviewUpColor);
                }
                catch
                {
                    return SafePreviewUpColor;
                }
            }
            set
            {
                if (value == Color.Empty) return;
                if (_previewUp != null)
                    _previewUp.BackColor = Color.FromArgb(255, value.R, value.G, value.B);
                if (_tbKeyAlpha != null)
                    _tbKeyAlpha.Value = Math.Clamp(value.A, _tbKeyAlpha.Minimum, _tbKeyAlpha.Maximum);
                if (_lblKeyAlpha != null)
                    _lblKeyAlpha.Text = $"Key Alpha: {(_tbKeyAlpha?.Value ?? value.A)}";
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("All keys down color (includes alpha via Key Alpha).")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SelectedDownColor
        {
            get
            {
                try
                {
                    return Color.FromArgb(SafeKeyAlpha, SafePreviewDownColor);
                }
                catch
                {
                    return SafePreviewDownColor;
                }
            }
            set
            {
                if (value == Color.Empty) return;
                if (_previewDown != null)
                    _previewDown.BackColor = Color.FromArgb(255, value.R, value.G, value.B);
                if (_tbKeyAlpha != null)
                    _tbKeyAlpha.Value = Math.Clamp(value.A, _tbKeyAlpha.Minimum, _tbKeyAlpha.Maximum);
                if (_lblKeyAlpha != null)
                    _lblKeyAlpha.Text = $"Key Alpha: {(_tbKeyAlpha?.Value ?? value.A)}";
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Alpha value for keys (0-255).")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(255)]
        public int SelectedKeyAlpha
        {
            get => _tbKeyAlpha?.Value ?? 255;
            set
            {
                if (_tbKeyAlpha != null)
                    _tbKeyAlpha.Value = Math.Clamp(value, _tbKeyAlpha.Minimum, _tbKeyAlpha.Maximum);
                if (_lblKeyAlpha != null)
                    _lblKeyAlpha.Text = $"Key Alpha: {_tbKeyAlpha?.Value ?? value}";
            }
        }

        // 새: 배경 알파 (0..255)
        [Browsable(true)]
        [Category("Appearance")]
        [Description("Background alpha (0-255).")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(255)]
        public int SelectedBgAlpha
        {
            get => _tbWindowOpacity?.Value ?? 255;
            set
            {
                if (_tbWindowOpacity != null)
                    _tbWindowOpacity.Value = Math.Clamp(value, _tbWindowOpacity.Minimum, _tbWindowOpacity.Maximum);
                if (_lblWindowOpacity != null)
                    _lblWindowOpacity.Text = $"Background Alpha: {_tbWindowOpacity?.Value ?? value}";
            }
        }

        // 새: 배경 불투명도(백분율 0..100) — GlobalEditorForm에서 사용
        [Browsable(true)]
        [Category("Appearance")]
        [Description("Background opacity percent (0-100).")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(100)]
        public int SelectedOpacityPercent
        {
            get
            {
                int alpha = _tbWindowOpacity?.Value ?? 255;
                return (int)Math.Round(alpha * 100.0 / 255.0);
            }
            set
            {
                int percent = Math.Clamp(value, 0, 100);
                int alpha = (int)Math.Round(percent * 255.0 / 100.0);
                if (_tbWindowOpacity != null)
                    _tbWindowOpacity.Value = Math.Clamp(alpha, _tbWindowOpacity.Minimum, _tbWindowOpacity.Maximum);
                if (_lblWindowOpacity != null)
                    _lblWindowOpacity.Text = $"Background Alpha: {_tbWindowOpacity?.Value ?? alpha}";
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Window background color used when applying global settings.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [TypeConverter(typeof(System.Drawing.ColorConverter))]
        [DefaultValue(typeof(Color), "ControlDark")]
        public Color SelectedBgColor
        {
            get => _previewBg?.BackColor ?? SystemColors.ControlDark;
            set
            {
                if (_previewBg != null)
                    _previewBg.BackColor = value;
            }
        }

        public string? SelectedBgImagePath { get; private set; }
    }
}