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
        private System.Windows.Forms.Label _lblChromaKey = null!;
        private System.Windows.Forms.ComboBox _cboChromaKey = null!;

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
            ((System.ComponentModel.ISupportInitialize)_tbKeyAlpha).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_tbWindowOpacity).BeginInit();
            SuspendLayout();
            // 
            // _btnUpColor
            // 
            _btnUpColor.Location = new Point(8, 8);
            _btnUpColor.Name = "_btnUpColor";
            _btnUpColor.Size = new Size(140, 23);
            _btnUpColor.TabIndex = 0;
            _btnUpColor.Text = "All Keys Up Color...";
            _btnUpColor.Click += BtnUpColor_Click;
            // 
            // _previewUp
            // 
            _previewUp.BackColor = Color.Gray;
            _previewUp.BorderStyle = BorderStyle.FixedSingle;
            _previewUp.Location = new Point(160, 8);
            _previewUp.Name = "_previewUp";
            _previewUp.Size = new Size(48, 24);
            _previewUp.TabIndex = 1;
            // 
            // _btnDownColor
            // 
            _btnDownColor.Location = new Point(8, 44);
            _btnDownColor.Name = "_btnDownColor";
            _btnDownColor.Size = new Size(140, 23);
            _btnDownColor.TabIndex = 2;
            _btnDownColor.Text = "All Keys Down Color...";
            _btnDownColor.Click += BtnDownColor_Click;
            // 
            // _previewDown
            // 
            _previewDown.BackColor = Color.Red;
            _previewDown.BorderStyle = BorderStyle.FixedSingle;
            _previewDown.Location = new Point(160, 44);
            _previewDown.Name = "_previewDown";
            _previewDown.Size = new Size(48, 23);
            _previewDown.TabIndex = 3;
            // 
            // _btnBgColor
            // 
            _btnBgColor.Location = new Point(8, 84);
            _btnBgColor.Name = "_btnBgColor";
            _btnBgColor.Size = new Size(140, 23);
            _btnBgColor.TabIndex = 4;
            _btnBgColor.Text = "Window Background Color...";
            _btnBgColor.Click += BtnBgColor_Click;
            // 
            // _previewBg
            // 
            _previewBg.BackColor = SystemColors.ControlDark;
            _previewBg.BorderStyle = BorderStyle.FixedSingle;
            _previewBg.Location = new Point(160, 84);
            _previewBg.Name = "_previewBg";
            _previewBg.Size = new Size(48, 24);
            _previewBg.TabIndex = 5;
            // 
            // _btnBgImage
            // 
            _btnBgImage.Location = new Point(8, 124);
            _btnBgImage.Name = "_btnBgImage";
            _btnBgImage.Size = new Size(140, 23);
            _btnBgImage.TabIndex = 6;
            _btnBgImage.Text = "Background Image...";
            _btnBgImage.Click += BtnBgImage_Click;
            // 
            // _bgImageLabel
            // 
            _bgImageLabel.Location = new Point(160, 128);
            _bgImageLabel.Name = "_bgImageLabel";
            _bgImageLabel.Size = new Size(300, 23);
            _bgImageLabel.TabIndex = 7;
            _bgImageLabel.Text = "(없음)";
            // 
            // _tbKeyAlpha
            // 
            _tbKeyAlpha.Location = new Point(8, 156);
            _tbKeyAlpha.Maximum = 255;
            _tbKeyAlpha.Name = "_tbKeyAlpha";
            _tbKeyAlpha.Size = new Size(360, 45);
            _tbKeyAlpha.TabIndex = 8;
            _tbKeyAlpha.TickFrequency = 5;
            _tbKeyAlpha.Value = 255;
            _tbKeyAlpha.Scroll += TbKeyAlpha_Scroll;
            // 
            // _lblKeyAlpha
            // 
            _lblKeyAlpha.Location = new Point(376, 156);
            _lblKeyAlpha.Name = "_lblKeyAlpha";
            _lblKeyAlpha.Size = new Size(120, 23);
            _lblKeyAlpha.TabIndex = 9;
            _lblKeyAlpha.Text = "Key Alpha: 255";
            // 
            // _tbWindowOpacity
            // 
            _tbWindowOpacity.Location = new Point(8, 196);
            _tbWindowOpacity.Maximum = 255;
            _tbWindowOpacity.Name = "_tbWindowOpacity";
            _tbWindowOpacity.Size = new Size(360, 45);
            _tbWindowOpacity.TabIndex = 10;
            _tbWindowOpacity.TickFrequency = 5;
            _tbWindowOpacity.Value = 255;
            _tbWindowOpacity.Scroll += TbWindowOpacity_Scroll;
            // 
            // _lblWindowOpacity
            // 
            _lblWindowOpacity.Location = new Point(376, 196);
            _lblWindowOpacity.Name = "_lblWindowOpacity";
            _lblWindowOpacity.Size = new Size(150, 23);
            _lblWindowOpacity.TabIndex = 11;
            _lblWindowOpacity.Text = "Window Opacity: 100%"; // 변경
            // 
            // _chkTransparentBg
            // 
            _chkTransparentBg.Location = new Point(8, 236);
            _chkTransparentBg.Name = "_chkTransparentBg";
            _chkTransparentBg.Size = new Size(200, 24);
            _chkTransparentBg.TabIndex = 12;
            _chkTransparentBg.Text = "배경 완전 투명화";
            _chkTransparentBg.CheckedChanged += ChkTransparentBg_CheckedChanged;
            // 
            // 크로마키 색상 레이블
            // 
            _lblChromaKey = new System.Windows.Forms.Label();
            _lblChromaKey.AutoSize = true;
            _lblChromaKey.Location = new System.Drawing.Point(220, 240);
            _lblChromaKey.Name = "_lblChromaKey";
            _lblChromaKey.Size = new System.Drawing.Size(120, 15);
            _lblChromaKey.Text = "Chroma Key:";
            // 
            // 크로마키 색상 선택 콤보박스
            // 
            _cboChromaKey = new System.Windows.Forms.ComboBox();
            _cboChromaKey.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            _cboChromaKey.FormattingEnabled = true;
            _cboChromaKey.Items.AddRange(new object[] {
                "Magenta",
                "Green",
                "Blue",
                "Black"
            });
            _cboChromaKey.Location = new System.Drawing.Point(310, 237);
            _cboChromaKey.Name = "_cboChromaKey";
            _cboChromaKey.Size = new System.Drawing.Size(110, 23);
            _cboChromaKey.SelectedIndex = 0;
            // 
            // GlobalEditorControl
            // 
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
            Name = "GlobalEditorControl";
            Size = new Size(520, 280);
            ((System.ComponentModel.ISupportInitialize)_tbKeyAlpha).EndInit();
            ((System.ComponentModel.ISupportInitialize)_tbWindowOpacity).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}