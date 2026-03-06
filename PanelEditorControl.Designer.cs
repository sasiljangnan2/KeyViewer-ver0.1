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
        private Button _btnRecord;
        private Label _lblCurrentKeyInfo;
        private TextBox _txtDisplayName;
        private Label _lblDisplayName;
        private CheckBox _chkBorder;
        private Button _btnBorderColor;
        private Panel _previewBorder;
        private NumericUpDown _numBorderWidth;
        private Label _lblBorderWidth;
        private NumericUpDown _numCornerRadius;
        private Label _lblCornerRadius;

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
            _chkBorder = new CheckBox();
            _btnBorderColor = new Button();
            _previewBorder = new Panel();
            _numBorderWidth = new NumericUpDown();
            _lblBorderWidth = new Label();
            _numCornerRadius = new NumericUpDown();
            _lblCornerRadius = new Label();

            ((System.ComponentModel.ISupportInitialize)_numWidth).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numHeight).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_tbAlpha).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numBorderWidth).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_numCornerRadius).BeginInit();
            SuspendLayout();

            // === Row 1 (Y=5): 키 정보 라벨 ===
            _lblCurrentKeyInfo.Location = new Point(10, 5);
            _lblCurrentKeyInfo.Name = "_lblCurrentKeyInfo";
            _lblCurrentKeyInfo.Size = new Size(300, 20);
            _lblCurrentKeyInfo.TabIndex = 0;
            _lblCurrentKeyInfo.Text = "단축키를 설정하려면 버튼을 누르세요";
            
            // === Row 2 (Y=28): 키 선택 및 녹화 버튼 ===
            _cbKeys.Location = new Point(10, 28);
            _cbKeys.Name = "_cbKeys";
            _cbKeys.Size = new Size(220, 23);
            _cbKeys.TabIndex = 1;
            _cbKeys.Enabled = false;
            _cbKeys.DropDownStyle = ComboBoxStyle.DropDown;

            _btnRecord.Location = new Point(240, 28);
            _btnRecord.Name = "_btnRecord";
            _btnRecord.Size = new Size(120, 23);
            _btnRecord.TabIndex = 2;
            _btnRecord.Text = "단축키 설정";
            _btnRecord.Click += BtnRecord_Click;

            // === Row 3 (Y=60): 색상 선택 ===
            _btnUpColor.Location = new Point(10, 60);
            _btnUpColor.Name = "_btnUpColor";
            _btnUpColor.Size = new Size(100, 23);
            _btnUpColor.TabIndex = 3;
            _btnUpColor.Text = "Up Color...";
            _btnUpColor.Click += BtnUpColor_Click;

            _previewUp.Location = new Point(115, 60);
            _previewUp.Name = "_previewUp";
            _previewUp.Size = new Size(40, 24);
            _previewUp.TabIndex = 4;
            _previewUp.BackColor = Color.Gray;
            _previewUp.BorderStyle = BorderStyle.FixedSingle;

            _btnDownColor.Location = new Point(170, 60);
            _btnDownColor.Name = "_btnDownColor";
            _btnDownColor.Size = new Size(100, 23);
            _btnDownColor.TabIndex = 5;
            _btnDownColor.Text = "Down Color...";
            _btnDownColor.Click += BtnDownColor_Click;

            _previewDown.Location = new Point(275, 60);
            _previewDown.Name = "_previewDown";
            _previewDown.Size = new Size(40, 24);
            _previewDown.TabIndex = 6;
            _previewDown.BackColor = Color.Red;
            _previewDown.BorderStyle = BorderStyle.FixedSingle;

            // === Row 4 (Y=95): Alpha 슬라이더 ===
            _tbAlpha.Location = new Point(10, 95);
            _tbAlpha.Name = "_tbAlpha";
            _tbAlpha.Size = new Size(350, 45);
            _tbAlpha.TabIndex = 7;
            _tbAlpha.Minimum = 0;
            _tbAlpha.Maximum = 255;
            _tbAlpha.TickFrequency = 5;
            _tbAlpha.Value = 255;
            _tbAlpha.Scroll += TbAlpha_Scroll;

            _lblAlpha.Location = new Point(370, 95);
            _lblAlpha.Name = "_lblAlpha";
            _lblAlpha.Size = new Size(120, 23);
            _lblAlpha.TabIndex = 8;
            _lblAlpha.Text = "Alpha: 255";

            // === Row 5 (Y=145): 크기 설정 ===
            _lblWidth.Location = new Point(10, 148);
            _lblWidth.Name = "_lblWidth";
            _lblWidth.Size = new Size(50, 20);
            _lblWidth.TabIndex = 9;
            _lblWidth.Text = "Width:";
            _lblWidth.TextAlign = ContentAlignment.MiddleLeft;

            _numWidth.Location = new Point(65, 146);
            _numWidth.Name = "_numWidth";
            _numWidth.Size = new Size(80, 23);
            _numWidth.TabIndex = 10;
            _numWidth.Minimum = 20;
            _numWidth.Maximum = 500;
            _numWidth.Value = 85;

            _lblHeight.Location = new Point(160, 148);
            _lblHeight.Name = "_lblHeight";
            _lblHeight.Size = new Size(50, 20);
            _lblHeight.TabIndex = 11;
            _lblHeight.Text = "Height:";
            _lblHeight.TextAlign = ContentAlignment.MiddleLeft;

            _numHeight.Location = new Point(215, 146);
            _numHeight.Name = "_numHeight";
            _numHeight.Size = new Size(80, 23);
            _numHeight.TabIndex = 12;
            _numHeight.Minimum = 20;
            _numHeight.Maximum = 500;
            _numHeight.Value = 85;

            // === Row 6 (Y=180): 표시 이름 ===
            _lblDisplayName.Location = new Point(10, 183);
            _lblDisplayName.Name = "_lblDisplayName";
            _lblDisplayName.Size = new Size(90, 20);
            _lblDisplayName.TabIndex = 13;
            _lblDisplayName.Text = "Display Name:";
            _lblDisplayName.TextAlign = ContentAlignment.MiddleLeft;

            _txtDisplayName.Location = new Point(105, 180);
            _txtDisplayName.Name = "_txtDisplayName";
            _txtDisplayName.Size = new Size(190, 23);
            _txtDisplayName.TabIndex = 14;
            _txtDisplayName.Text = "";

            // === Row 7 (Y=215): 테두리 설정 ===
            _chkBorder.Location = new Point(8, 215);
            _chkBorder.Name = "_chkBorder";
            _chkBorder.Size = new Size(80, 24);
            _chkBorder.TabIndex = 15;
            _chkBorder.Text = "테두리";
            _chkBorder.CheckedChanged += ChkBorder_CheckedChanged;

            _btnBorderColor.Location = new Point(95, 215);
            _btnBorderColor.Name = "_btnBorderColor";
            _btnBorderColor.Size = new Size(65, 23);
            _btnBorderColor.TabIndex = 16;
            _btnBorderColor.Text = "색상...";
            _btnBorderColor.Click += BtnBorderColor_Click;

            _previewBorder.BackColor = Color.Black;
            _previewBorder.BorderStyle = BorderStyle.FixedSingle;
            _previewBorder.Location = new Point(167, 215);
            _previewBorder.Name = "_previewBorder";
            _previewBorder.Size = new Size(35, 24);
            _previewBorder.TabIndex = 17;

            _lblBorderWidth.Location = new Point(210, 218);
            _lblBorderWidth.Name = "_lblBorderWidth";
            _lblBorderWidth.Size = new Size(40, 20);
            _lblBorderWidth.TabIndex = 18;
            _lblBorderWidth.Text = "두께:";
            _lblBorderWidth.TextAlign = ContentAlignment.MiddleRight;

            _numBorderWidth.Location = new Point(255, 216);
            _numBorderWidth.Minimum = 1;
            _numBorderWidth.Maximum = 10;
            _numBorderWidth.Name = "_numBorderWidth";
            _numBorderWidth.Size = new Size(55, 23);
            _numBorderWidth.TabIndex = 19;
            _numBorderWidth.Value = 2;

            _lblCornerRadius.Location = new Point(320, 218);
            _lblCornerRadius.Name = "_lblCornerRadius";
            _lblCornerRadius.Size = new Size(50, 20);
            _lblCornerRadius.TabIndex = 20;
            _lblCornerRadius.Text = "모서리:";
            _lblCornerRadius.TextAlign = ContentAlignment.MiddleRight;

            _numCornerRadius.Location = new Point(375, 216);
            _numCornerRadius.Minimum = 0;
            _numCornerRadius.Maximum = 50;
            _numCornerRadius.Name = "_numCornerRadius";
            _numCornerRadius.Size = new Size(55, 23);
            _numCornerRadius.TabIndex = 21;
            _numCornerRadius.Value = 0;

            // Load 이벤트
            this.Load += PanelEditorControl_Load;

            // Control collection
            Controls.AddRange(new Control[] { 
                _lblCurrentKeyInfo,
                _cbKeys,
                _btnRecord,
                _btnUpColor, _previewUp, 
                _btnDownColor, _previewDown, 
                _tbAlpha, _lblAlpha,
                _lblWidth, _numWidth,
                _lblHeight, _numHeight,
                _lblDisplayName, _txtDisplayName,
                _chkBorder, _btnBorderColor, _previewBorder,
                _lblBorderWidth, _numBorderWidth,
                _lblCornerRadius, _numCornerRadius
            });

            Name = "PanelEditorControl";
            Size = new Size(500, 250);

            ((System.ComponentModel.ISupportInitialize)_numWidth).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numHeight).EndInit();
            ((System.ComponentModel.ISupportInitialize)_tbAlpha).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numBorderWidth).EndInit();
            ((System.ComponentModel.ISupportInitialize)_numCornerRadius).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
    }
}