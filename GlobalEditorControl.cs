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
            {
                int percent = (int)Math.Round(_tbWindowOpacity.Value * 100.0 / 255.0);
                _lblWindowOpacity.Text = $"Window Opacity: {percent}%"; // 변경
            }
        }

        private void ChkTransparentBg_CheckedChanged(object? sender, EventArgs e)
        {
            // 체크박스 상태가 변경될 때 아무것도 안 함 (Form1에서 읽기만 함)
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

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Window opacity percent (0-100).")]
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
                    _lblWindowOpacity.Text = $"Window Opacity: {percent}%"; // 변경
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

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Enable background transparency (using TransparencyKey).")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [DefaultValue(false)]
        public bool BackgroundTransparent
        {
            get => _chkTransparentBg?.Checked ?? false;
            set
            {
                if (_chkTransparentBg != null)
                    _chkTransparentBg.Checked = value;
            }
        }

        [Browsable(true)]
        [Category("Appearance")]
        [Description("Chroma key color for OBS transparency.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // 추가
        public Color ChromaKeyColor
        {
            get
            {
                if (_cboChromaKey == null) return Color.Magenta;

                return _cboChromaKey.SelectedIndex switch
                {
                    0 => Color.FromArgb(255, 0, 255),   // Magenta
                    1 => Color.FromArgb(0, 255, 0),     // Green
                    2 => Color.FromArgb(0, 0, 255),     // Blue
                    3 => Color.FromArgb(0, 0, 0),       // Black
                    _ => Color.Magenta
                };
            }
            set
            {
                if (_cboChromaKey == null) return;

                if (value.R == 255 && value.G == 0 && value.B == 255)
                    _cboChromaKey.SelectedIndex = 0; // Magenta
                else if (value.R == 0 && value.G == 255 && value.B == 0)
                    _cboChromaKey.SelectedIndex = 1; // Green
                else if (value.R == 0 && value.G == 0 && value.B == 255)
                    _cboChromaKey.SelectedIndex = 2; // Blue
                else if (value.R == 0 && value.G == 0 && value.B == 0)
                    _cboChromaKey.SelectedIndex = 3; // Black
                else
                    _cboChromaKey.SelectedIndex = 0; // 기본값
            }
        }
    }
}