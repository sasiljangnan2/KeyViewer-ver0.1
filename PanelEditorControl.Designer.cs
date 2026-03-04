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
        private Button _btnRecord; // 녹화 시작 버튼
        private Label _lblCurrentKeyInfo; // 현재 입력 상태 표시 라벨
        private TextBox _txtDisplayName; // 커스텀 이름 입력 필드
        private Label _lblDisplayName; // 커스텀 이름 라벨

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
            _btnRecord = new Button();
            _lblCurrentKeyInfo = new Label();
            _txtDisplayName = new TextBox();
            _lblDisplayName = new Label();

            // 녹화 시작 버튼 설정
            _btnRecord.Location = new Point(235, 23);
            _btnRecord.Size = new Size(130, 28);
            _btnRecord.Text = "단축키 설정";
            _btnRecord.Click += BtnRecord_Click;

            // 입력 정보 라벨 설정
            _lblCurrentKeyInfo.Location = new Point(10, 5);
            _lblCurrentKeyInfo.Size = new Size(300, 15);
            _lblCurrentKeyInfo.Text = "단축키를 설정하려면 버튼을 누르세요";

            // 전체 컨트롤 크기 확장 (버튼이 보일 수 있게)
            this.Size = new Size(460, 240);
            this.Height = 280;
            this.Controls.Add(_btnRecord);
            this.Controls.Add(_lblCurrentKeyInfo);

            // cbKeys(ComboBox)
            _cbKeys.Location = new Point(10, 25);
            _cbKeys.Width = 220;
            _cbKeys.Enabled = false; // 직접 입력 방지
            _cbKeys.DropDownStyle = ComboBoxStyle.DropDown;

            // btnUpColor
            _btnUpColor.Location = new Point(10, 70);
            _btnUpColor.Size = new Size(120, 27);
            _btnUpColor.Text = "Up Color...";
            _btnUpColor.Click += BtnUpColor_Click;

            // previewUp
            _previewUp.Location = new Point(160, 73);
            _previewUp.Size = new Size(40, 24);
            _previewUp.BackColor = Color.Gray;
            _previewUp.BorderStyle = BorderStyle.FixedSingle;

            // btnDownColor
            _btnDownColor.Location = new Point(10, 105);
            _btnDownColor.Size = new Size(120, 27);
            _btnDownColor.Text = "Down Color...";
            _btnDownColor.Click += BtnDownColor_Click;

            // previewDown
            _previewDown.Location = new Point(160, 105);
            _previewDown.Size = new Size(40, 24);
            _previewDown.BackColor = Color.Red;
            _previewDown.BorderStyle = BorderStyle.FixedSingle;

            // tbAlpha
            _tbAlpha.Location = new Point(10, 145);
            _tbAlpha.Width = 320;
            _tbAlpha.Minimum = 0;
            _tbAlpha.Maximum = 255;
            _tbAlpha.TickFrequency = 5;
            _tbAlpha.Value = 255;
            _tbAlpha.Scroll += TbAlpha_Scroll;

            // lblAlpha
            _lblAlpha.Location = new Point(340, 145);
            _lblAlpha.Width = 120;
            _lblAlpha.Text = "Alpha: 255";

            // lblWidth
            _lblWidth.Location = new Point(10, 195);
            _lblWidth.Width = 60;
            _lblWidth.Text = "Width:";
            _lblWidth.TextAlign = ContentAlignment.MiddleLeft;

            // numWidth
            _numWidth.Location = new Point(80, 195);
            _numWidth.Width = 80;
            _numWidth.Minimum = 20;
            _numWidth.Maximum = 500;
            _numWidth.Value = 85; // 기본값 85

            // lblHeight
            _lblHeight.Location = new Point(180, 195);
            _lblHeight.Width = 60;
            _lblHeight.Text = "Height:";
            _lblHeight.TextAlign = ContentAlignment.MiddleLeft;

            // numHeight
            _numHeight.Location = new Point(250, 195);
            _numHeight.Width = 80;
            _numHeight.Minimum = 20;
            _numHeight.Maximum = 500;
            _numHeight.Value = 85; // 기본값 85

            // lblDisplayName
            _lblDisplayName.Location = new Point(10, 220);
            _lblDisplayName.Width = 100;
            _lblDisplayName.Text = "Display Name:";
            _lblDisplayName.TextAlign = ContentAlignment.MiddleLeft;

            // txtDisplayName
            _txtDisplayName.Location = new Point(120, 220);
            _txtDisplayName.Width = 310;
            _txtDisplayName.Text = "";

            // Load 이벤트
            this.Load += PanelEditorControl_Load;

            // Control collection
            Controls.AddRange(new Control[] { 
                _cbKeys, 
                _btnUpColor, _previewUp, 
                _btnDownColor, _previewDown, 
                _tbAlpha, _lblAlpha,
                _lblWidth, _numWidth,
                _lblHeight, _numHeight,
                _lblDisplayName, _txtDisplayName,
                _btnRecord,
                _lblCurrentKeyInfo
            });


        }
    }
}