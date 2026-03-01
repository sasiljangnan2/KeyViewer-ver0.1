using System;
using System.Drawing;
using System.Windows.Forms;

namespace keyviewer
{
    public partial class PanelEditorControl
    {
        private ComboBox _cbKeys;
        private Button _btnUpColor;
        private Button _btnDownColor;
        private Panel _previewUp;
        private Panel _previewDown;
        private TrackBar _tbAlpha;
        private Label _lblAlpha;
        private NumericUpDown _numWidth;
        private NumericUpDown _numHeight;
        private Label _lblWidth;
        private Label _lblHeight;
        private ColorDialog _colorDialog;

        private void InitializeComponent()
        {
            _cbKeys = new ComboBox();
            _btnUpColor = new Button();
            _previewUp = new Panel();
            _btnDownColor = new Button();
            _previewDown = new Panel();
            _tbAlpha = new TrackBar();
            _lblAlpha = new Label();
            _numWidth = new NumericUpDown();
            _numHeight = new NumericUpDown();
            _lblWidth = new Label();
            _lblHeight = new Label();
            _colorDialog = new ColorDialog();

            // cbKeys
            _cbKeys.Left = 8;
            _cbKeys.Top = 8;
            _cbKeys.Width = 320;
            _cbKeys.DropDownStyle = ComboBoxStyle.DropDownList;

            // btnUpColor
            _btnUpColor.Left = 8;
            _btnUpColor.Top = 44;
            _btnUpColor.Width = 120;
            _btnUpColor.Text = "Up Color...";
            _btnUpColor.Click += BtnUpColor_Click;

            // previewUp
            _previewUp.Left = 140;
            _previewUp.Top = 44;
            _previewUp.Size = new Size(40, 24);
            _previewUp.BackColor = Color.Gray;
            _previewUp.BorderStyle = BorderStyle.FixedSingle;

            // btnDownColor
            _btnDownColor.Left = 8;
            _btnDownColor.Top = 84;
            _btnDownColor.Width = 120;
            _btnDownColor.Text = "Down Color...";
            _btnDownColor.Click += BtnDownColor_Click;

            // previewDown
            _previewDown.Left = 140;
            _previewDown.Top = 84;
            _previewDown.Size = new Size(40, 24);
            _previewDown.BackColor = Color.Red;
            _previewDown.BorderStyle = BorderStyle.FixedSingle;

            // tbAlpha
            _tbAlpha.Left = 8;
            _tbAlpha.Top = 124;
            _tbAlpha.Width = 320;
            _tbAlpha.Minimum = 0;
            _tbAlpha.Maximum = 255;
            _tbAlpha.TickFrequency = 5;
            _tbAlpha.Value = 255;
            _tbAlpha.Scroll += TbAlpha_Scroll;

            // lblAlpha
            _lblAlpha.Left = 336;
            _lblAlpha.Top = 124;
            _lblAlpha.Width = 120;
            _lblAlpha.Text = "Alpha: 255";

            // lblWidth
            _lblWidth.Left = 8;
            _lblWidth.Top = 164;
            _lblWidth.Width = 60;
            _lblWidth.Text = "Width:";
            _lblWidth.TextAlign = ContentAlignment.MiddleLeft;

            // numWidth
            _numWidth.Left = 70;
            _numWidth.Top = 164;
            _numWidth.Width = 80;
            _numWidth.Minimum = 20;
            _numWidth.Maximum = 500;
            _numWidth.Value = 85; // 기본값 85

            // lblHeight
            _lblHeight.Left = 200;
            _lblHeight.Top = 164;
            _lblHeight.Width = 60;
            _lblHeight.Text = "Height:";
            _lblHeight.TextAlign = ContentAlignment.MiddleLeft;

            // numHeight
            _numHeight.Left = 262;
            _numHeight.Top = 164;
            _numHeight.Width = 80;
            _numHeight.Minimum = 20;
            _numHeight.Maximum = 500;
            _numHeight.Value = 85; // 기본값 85

            // Load 이벤트
            this.Load += PanelEditorControl_Load;

            // Control collection
            Controls.AddRange(new Control[] { 
                _cbKeys, 
                _btnUpColor, _previewUp, 
                _btnDownColor, _previewDown, 
                _tbAlpha, _lblAlpha,
                _lblWidth, _numWidth,
                _lblHeight, _numHeight
            });

            Width = 480;
            Height = 204; // 높이 증가 (크기 입력 필드 추가)
        }
    }
}