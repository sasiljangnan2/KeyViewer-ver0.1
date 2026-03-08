using System;
using System.Drawing;
using System.Windows.Forms;

namespace keyviewer
{
    public partial class GlobalEditorControl
    {
        private Button _btnUpColor;
        private Button _btnDownColor;
        private Panel _previewUp;
        private Panel _previewDown;
        private Button _btnBgColor;
        private Panel _previewBg;
        private Button _btnBgImage;
        private Label _bgImageLabel;
        private TrackBar _tbKeyAlpha;
        private Label _lblKeyAlpha;
        private TrackBar _tbWindowOpacity;
        private Label _lblWindowOpacity;
        private CheckBox _chkTransparentBg;
        private ColorDialog _colorDialog;
        private OpenFileDialog _openFileDialog;
        private Label _lblChromaKey;
        private ComboBox _cboChromaKey;
        private CheckBox _chkBorder;
        private Button _btnBorderColor;
        private Panel _previewBorder;
        private NumericUpDown _numBorderWidth;
        private Label _lblBorderWidth;
        private NumericUpDown _numCornerRadius;
        private Label _lblCornerRadius;

        private void InitializeComponent()
        {
            _btnUpColor = new Button();
            _previewUp = new Panel();
            _btnDownColor = new Button();
            _previewDown = new Panel();
            _btnBgColor = new Button();
            _previewBg = new Panel();
            _btnBgImage = new Button();
            _bgImageLabel = new Label();
            _tbKeyAlpha = new TrackBar();
            _lblKeyAlpha = new Label();
            _tbWindowOpacity = new TrackBar();
            _lblWindowOpacity = new Label();
            _chkTransparentBg = new CheckBox();
            _colorDialog = new ColorDialog();
            _openFileDialog = new OpenFileDialog();
            _lblChromaKey = new Label();
            _cboChromaKey = new ComboBox();
            _chkBorder = new CheckBox();
            _btnBorderColor = new Button();
            _previewBorder = new Panel();
            _numBorderWidth = new NumericUpDown();
            _lblBorderWidth = new Label();
            _numCornerRadius = new NumericUpDown();
            _lblCornerRadius = new Label();
            
            ((System.ComponentModel.ISupportInitialize)_tbKeyAlpha).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_tbWindowOpacity).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numBorderWidth).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numCornerRadius).BeginInit();
            SuspendLayout();
            
            // === Row 1 (Y=8): 키 색상 ===
            _btnUpColor.Location = new Point(8, 8);
            _btnUpColor.Name = "_btnUpColor";
            _btnUpColor.Size = new Size(130, 23);
            _btnUpColor.TabIndex = 0;
            _btnUpColor.Text = "Keys Up Color...";
            _btnUpColor.Click += BtnUpColor_Click;
            
            _previewUp.BackColor = Color.Gray;
            _previewUp.BorderStyle = BorderStyle.FixedSingle;
            _previewUp.Location = new Point(145, 8);
            _previewUp.Name = "_previewUp";
            _previewUp.Size = new Size(45, 24);
            _previewUp.TabIndex = 1;
            
            _btnDownColor.Location = new Point(210, 8);
            _btnDownColor.Name = "_btnDownColor";
            _btnDownColor.Size = new Size(130, 23);
            _btnDownColor.TabIndex = 2;
            _btnDownColor.Text = "Keys Down Color...";
            _btnDownColor.Click += BtnDownColor_Click;
            
            _previewDown.BackColor = Color.Red;
            _previewDown.BorderStyle = BorderStyle.FixedSingle;
            _previewDown.Location = new Point(347, 8);
            _previewDown.Name = "_previewDown";
            _previewDown.Size = new Size(45, 24);
            _previewDown.TabIndex = 3;
            
            // === Row 2 (Y=40): 배경 설정 ===
            _btnBgColor.Location = new Point(8, 40);
            _btnBgColor.Name = "_btnBgColor";
            _btnBgColor.Size = new Size(130, 23);
            _btnBgColor.TabIndex = 4;
            _btnBgColor.Text = "Background Color...";
            _btnBgColor.Click += BtnBgColor_Click;
            
            _previewBg.BackColor = SystemColors.ControlDark;
            _previewBg.BorderStyle = BorderStyle.FixedSingle;
            _previewBg.Location = new Point(145, 40);
            _previewBg.Name = "_previewBg";
            _previewBg.Size = new Size(45, 24);
            _previewBg.TabIndex = 5;
            
            _btnBgImage.Location = new Point(210, 40);
            _btnBgImage.Name = "_btnBgImage";
            _btnBgImage.Size = new Size(130, 23);
            _btnBgImage.TabIndex = 6;
            _btnBgImage.Text = "BG Image...";
            _btnBgImage.Click += BtnBgImage_Click;
            
            _bgImageLabel.Location = new Point(347, 43);
            _bgImageLabel.Name = "_bgImageLabel";
            _bgImageLabel.Size = new Size(200, 20);
            _bgImageLabel.TabIndex = 7;
            _bgImageLabel.Text = "(없음)";
            
            // === Row 3 (Y=75): Key Alpha ===
            _tbKeyAlpha.Location = new Point(8, 75);
            _tbKeyAlpha.Maximum = 255;
            _tbKeyAlpha.Name = "_tbKeyAlpha";
            _tbKeyAlpha.Size = new Size(400, 45);
            _tbKeyAlpha.TabIndex = 8;
            _tbKeyAlpha.TickFrequency = 5;
            _tbKeyAlpha.Value = 255;
            _tbKeyAlpha.Scroll += TbKeyAlpha_Scroll;
            
            _lblKeyAlpha.Location = new Point(420, 75);
            _lblKeyAlpha.Name = "_lblKeyAlpha";
            _lblKeyAlpha.Size = new Size(130, 23);
            _lblKeyAlpha.TabIndex = 9;
            _lblKeyAlpha.Text = "Key Alpha: 255";
            
            // === Row 4 (Y=125): Window Opacity ===
            _tbWindowOpacity.Location = new Point(8, 125);
            _tbWindowOpacity.Maximum = 255;
            _tbWindowOpacity.Name = "_tbWindowOpacity";
            _tbWindowOpacity.Size = new Size(400, 45);
            _tbWindowOpacity.TabIndex = 10;
            _tbWindowOpacity.TickFrequency = 5;
            _tbWindowOpacity.Value = 255;
            _tbWindowOpacity.Scroll += TbWindowOpacity_Scroll;
            
            _lblWindowOpacity.Location = new Point(420, 125);
            _lblWindowOpacity.Name = "_lblWindowOpacity";
            _lblWindowOpacity.Size = new Size(130, 23);
            _lblWindowOpacity.TabIndex = 11;
            _lblWindowOpacity.Text = "Window Opacity: 100%";
            
            // === Row 5 (Y=175): 투명화 및 크로마키 ===
            _chkTransparentBg.Location = new Point(8, 175);
            _chkTransparentBg.Name = "_chkTransparentBg";
            _chkTransparentBg.Size = new Size(140, 24);
            _chkTransparentBg.TabIndex = 12;
            _chkTransparentBg.Text = "배경 완전 투명화";
            _chkTransparentBg.CheckedChanged += ChkTransparentBg_CheckedChanged;
            
            _lblChromaKey.Location = new Point(160, 178);
            _lblChromaKey.Name = "_lblChromaKey";
            _lblChromaKey.AutoSize = true;
            _lblChromaKey.Text = "Chroma:";
            
            _cboChromaKey.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboChromaKey.FormattingEnabled = true;
            _cboChromaKey.Items.AddRange(new object[] { "Magenta", "Green", "Blue", "Black" });
            _cboChromaKey.Location = new Point(220, 175);
            _cboChromaKey.Name = "_cboChromaKey";
            _cboChromaKey.Size = new Size(100, 23);
            _cboChromaKey.SelectedIndex = 0;
            
            // === Row 6 (Y=210): 테두리 설정 ===
            _chkBorder.Location = new Point(8, 210);
            _chkBorder.Name = "_chkBorder";
            _chkBorder.Size = new Size(85, 24);
            _chkBorder.TabIndex = 13;
            _chkBorder.Text = "테두리";
            _chkBorder.CheckedChanged += ChkBorder_CheckedChanged;
            
            _btnBorderColor.Location = new Point(100, 210);
            _btnBorderColor.Name = "_btnBorderColor";
            _btnBorderColor.Size = new Size(70, 23);
            _btnBorderColor.TabIndex = 14;
            _btnBorderColor.Text = "색상...";
            _btnBorderColor.Click += BtnBorderColor_Click;
            
            _previewBorder.BackColor = Color.Black;
            _previewBorder.BorderStyle = BorderStyle.FixedSingle;
            _previewBorder.Location = new Point(177, 210);
            _previewBorder.Name = "_previewBorder";
            _previewBorder.Size = new Size(40, 24);
            _previewBorder.TabIndex = 15;
            
            _lblBorderWidth.Location = new Point(227, 213);
            _lblBorderWidth.Name = "_lblBorderWidth";
            _lblBorderWidth.Size = new Size(40, 20);
            _lblBorderWidth.TabIndex = 16;
            _lblBorderWidth.Text = "두께:";
            _lblBorderWidth.TextAlign = ContentAlignment.MiddleRight;
            
            _numBorderWidth.Location = new Point(272, 211);
            _numBorderWidth.Minimum = 1;
            _numBorderWidth.Maximum = 10;
            _numBorderWidth.Name = "_numBorderWidth";
            _numBorderWidth.Size = new Size(60, 23);
            _numBorderWidth.TabIndex = 17;
            _numBorderWidth.Value = 2;
            
            _lblCornerRadius.Location = new Point(342, 213);
            _lblCornerRadius.Name = "_lblCornerRadius";
            _lblCornerRadius.Size = new Size(50, 20);
            _lblCornerRadius.TabIndex = 18;
            _lblCornerRadius.Text = "모서리:";
            _lblCornerRadius.TextAlign = ContentAlignment.MiddleRight;
            
            _numCornerRadius.Location = new Point(397, 211);
            _numCornerRadius.Minimum = 0;
            _numCornerRadius.Maximum = 50;
            _numCornerRadius.Name = "_numCornerRadius";
            _numCornerRadius.Size = new Size(60, 23);
            _numCornerRadius.TabIndex = 19;
            _numCornerRadius.Value = 0;
            
            // 컨트롤 추가
            Controls.Add(_btnUpColor);
            Controls.Add(_previewUp);
            Controls.Add(_btnDownColor);
            Controls.Add(_previewDown);
            Controls.Add(_btnBgColor);
            Controls.Add(_previewBg);
            Controls.Add(_btnBgImage);
            Controls.Add(_bgImageLabel);
            Controls.Add(_tbKeyAlpha);
            Controls.Add(_lblKeyAlpha);
            Controls.Add(_tbWindowOpacity);
            Controls.Add(_lblWindowOpacity);
            Controls.Add(_chkTransparentBg);
            Controls.Add(_lblChromaKey);
            Controls.Add(_cboChromaKey);
            Controls.Add(_chkBorder);
            Controls.Add(_btnBorderColor);
            Controls.Add(_previewBorder);
            Controls.Add(_lblBorderWidth);
            Controls.Add(_numBorderWidth);
            Controls.Add(_lblCornerRadius);
            Controls.Add(_numCornerRadius);
            
            Name = "GlobalEditorControl";
            Size = new Size(560, 245);
            
            ((System.ComponentModel.ISupportInitialize)_tbKeyAlpha).EndInit();
            ((System.ComponentModel.ISupportInitialize)_tbWindowOpacity).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numBorderWidth).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numCornerRadius).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}