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
        private ColorDialog _colorDialog;
        private OpenFileDialog _openFileDialog;

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
            _colorDialog = new ColorDialog();
            _openFileDialog = new OpenFileDialog { Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All Files|*.*" };

            // Up color
            _btnUpColor.Left = 8; _btnUpColor.Top = 8; _btnUpColor.Width = 140; _btnUpColor.Text = "All Keys Up Color...";
            _previewUp.Left = 160; _previewUp.Top = 8; _previewUp.Size = new Size(48, 24); _previewUp.BackColor = Color.Gray; _previewUp.BorderStyle = BorderStyle.FixedSingle;
            _btnUpColor.Click += BtnUpColor_Click;

            // Down color
            _btnDownColor.Left = 8; _btnDownColor.Top = 44; _btnDownColor.Width = 140; _btnDownColor.Text = "All Keys Down Color...";
            _previewDown.Left = 160; _previewDown.Top = 44; _previewDown.Size = new Size(48, 24); _previewDown.BackColor = Color.Red; _previewDown.BorderStyle = BorderStyle.FixedSingle;
            _btnDownColor.Click += BtnDownColor_Click;

            // Background color/image
            _btnBgColor.Left = 8; _btnBgColor.Top = 84; _btnBgColor.Width = 140; _btnBgColor.Text = "Window Background Color...";
            _previewBg.Left = 160; _previewBg.Top = 84; _previewBg.Size = new Size(48, 24); _previewBg.BackColor = SystemColors.ControlDark; _previewBg.BorderStyle = BorderStyle.FixedSingle;
            _btnBgColor.Click += BtnBgColor_Click;

            _btnBgImage.Left = 8; _btnBgImage.Top = 124; _btnBgImage.Width = 140; _btnBgImage.Text = "Background Image...";
            _bgImageLabel.Left = 160; _bgImageLabel.Top = 128; _bgImageLabel.Width = 300; _bgImageLabel.Text = "(없음)";
            _btnBgImage.Click += BtnBgImage_Click;

            // Key alpha
            _tbKeyAlpha.Left = 8; _tbKeyAlpha.Top = 156; _tbKeyAlpha.Width = 360; _tbKeyAlpha.Minimum = 0; _tbKeyAlpha.Maximum = 255; _tbKeyAlpha.TickFrequency = 5; _tbKeyAlpha.Value = 255;
            _lblKeyAlpha.Left = 376; _lblKeyAlpha.Top = 156; _lblKeyAlpha.Width = 120; _lblKeyAlpha.Text = "Key Alpha: 255";
            _tbKeyAlpha.Scroll += TbKeyAlpha_Scroll;

            // Background alpha (디자이너 초기값은 리터럴로 설정)
            // 범위 0..255 — 배경 색의 알파(Background Alpha)
            _tbWindowOpacity.Left = 8; _tbWindowOpacity.Top = 196; _tbWindowOpacity.Width = 360; _tbWindowOpacity.Minimum = 0; _tbWindowOpacity.Maximum = 255; _tbWindowOpacity.TickFrequency = 5; _tbWindowOpacity.Value = 255;
            _lblWindowOpacity.Left = 376; _lblWindowOpacity.Top = 196; _lblWindowOpacity.Width = 140; _lblWindowOpacity.Text = "Background Alpha: 255";
            _tbWindowOpacity.Scroll += TbWindowOpacity_Scroll;

            Controls.AddRange(new Control[] {
                _btnUpColor, _previewUp,
                _btnDownColor, _previewDown,
                _btnBgColor, _previewBg,
                _btnBgImage, _bgImageLabel,
                _tbKeyAlpha, _lblKeyAlpha,
                _tbWindowOpacity, _lblWindowOpacity
            });

            Width = 520;
            Height = 260;
        }
    }
}