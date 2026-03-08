using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace keyviewer
{
    public partial class PanelEditorControl : UserControl
    {
        // 저수준 키보드 훅
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc? _hookProc;
        private IntPtr _hookID = IntPtr.Zero;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private bool _isRecording = false;
        private Keys _lastRecordedKey = Keys.None;

        public bool IsRecording => _isRecording;

        public void RecordKey(Keys rawKey)
        {
            _lastRecordedKey = rawKey;
            _cbKeys.Text = GetRawKeyDisplayName(rawKey);
            _lblCurrentKeyInfo.Text = $"입력된 키: {GetRawKeyDisplayName(rawKey)}";
        }

        private void StartHook()
        {
            if (_hookID != IntPtr.Zero) return;
            _hookProc = HookCallback;
            using var proc = Process.GetCurrentProcess();
            using var module = proc.MainModule!;
            _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc,
                GetModuleHandle(module.ModuleName!), 0);
        }

        private void StopHook()
        {
            if (_hookID == IntPtr.Zero) return;
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
            _hookProc = null;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN && _isRecording)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Keys rawKey = (Keys)vkCode;

                // UI 스레드에서 업데이트
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke(new Action(() => RecordKey(rawKey)));
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void BtnRecord_Click(object sender, EventArgs e)
        {
            if (!_isRecording)
            {
                _isRecording = true;
                _btnRecord.Text = "설정 중지하기";
                _btnRecord.BackColor = Color.Red;
                _btnRecord.ForeColor = Color.White;
                _cbKeys.BackColor = Color.LemonChiffon;
                _lblCurrentKeyInfo.Text = "키보드 키를 누르세요...";
                StartHook(); // 🔥 저수준 훅 설치
            }
            else
            {
                StopHook(); // 🔥 훅 제거
                StopRecording();
            }
        }

        private void StopRecording()
        {
            _isRecording = false;
            _btnRecord.Text = "단축키 설정";
            _btnRecord.BackColor = SystemColors.Control;
            _btnRecord.ForeColor = SystemColors.ControlText;
            _cbKeys.BackColor = SystemColors.Control;

            if (_cbKeys != null) _cbKeys.Enabled = false;

            if (_lastRecordedKey != Keys.None)
            {
                this.SelectedKey = _lastRecordedKey;
                _lblCurrentKeyInfo.Text = $"설정됨: {GetRawKeyDisplayName(_lastRecordedKey)}";
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            StopHook();
            _isRecording = false;
            _btnRecord.Text = "단축키 설정";
            _cbKeys.BackColor = SystemColors.Window;
            _btnRecord.BackColor = SystemColors.Control;
            _btnRecord.ForeColor = SystemColors.ControlText;

            if (_cbKeys != null) _cbKeys.Enabled = true;
            if (_lastRecordedKey != Keys.None)
                this.SelectedKey = _lastRecordedKey;
        }

        // ProcessCmdKey는 훅으로 대체되었으므로 제거
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (_isRecording)
            {
                if (e.Button == MouseButtons.Left) _lastRecordedKey = Keys.LButton;
                else if (e.Button == MouseButtons.Right) _lastRecordedKey = Keys.RButton;

                _lblCurrentKeyInfo.Text = $"입력된 키: {GetRawKeyDisplayName(_lastRecordedKey)}";
                _cbKeys.Text = GetRawKeyDisplayName(_lastRecordedKey);
            }
            base.OnMouseDown(e);
        }

        // 컨트롤 dispose 시 훅 해제
        protected override void Dispose(bool disposing)
        {
            if (disposing) StopHook();
            base.Dispose(disposing);
        }

        private static string GetRawKeyDisplayName(Keys key)
        {
            return key switch
            {
                Keys.LShiftKey   => "Left Shift",
                Keys.RShiftKey   => "Right Shift",
                Keys.LControlKey => "Left Ctrl",
                Keys.RControlKey => "Right Ctrl",
                Keys.LMenu       => "Left Alt",
                Keys.RMenu       => "Right Alt",
                Keys.LButton     => "Mouse Left",
                Keys.RButton     => "Mouse Right",
                _                => key.ToString()
            };
        }

        public PanelEditorControl()
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

        private int SafeAlpha => _tbAlpha?.Value ?? 255;
        private Color SafePreviewUp => _previewUp?.BackColor ?? Color.Gray;
        private Color SafePreviewDown => _previewDown?.BackColor ?? Color.Red;

        [Browsable(true), Category("Behavior"), DefaultValue(Keys.None)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Keys SelectedKey
        {
            get => _lastRecordedKey;
            set
            {
                if (_cbKeys == null) return;
                _lastRecordedKey = value;
                _cbKeys.Text = GetRawKeyDisplayName(value);
            }
        }

        [Browsable(true), Category("Appearance"), DefaultValue(typeof(Color), "Gray")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [TypeConverter(typeof(ColorConverter))]
        public Color SelectedUpColor
        {
            get => Color.FromArgb(SafeAlpha, SafePreviewUp);
            set
            {
                if (value == Color.Empty) return;
                if (_previewUp != null) _previewUp.BackColor = Color.FromArgb(255, value.R, value.G, value.B);
                if (_tbAlpha != null) _tbAlpha.Value = Math.Clamp(value.A, _tbAlpha.Minimum, _tbAlpha.Maximum);
                if (_lblAlpha != null) _lblAlpha.Text = $"Alpha: {_tbAlpha?.Value ?? value.A}";
            }
        }

        [Browsable(true), Category("Appearance"), DefaultValue(typeof(Color), "Red")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [TypeConverter(typeof(ColorConverter))]
        public Color SelectedDownColor
        {
            get => Color.FromArgb(SafeAlpha, SafePreviewDown);
            set
            {
                if (value == Color.Empty) return;
                if (_previewDown != null) _previewDown.BackColor = Color.FromArgb(255, value.R, value.G, value.B);
                if (_tbAlpha != null) _tbAlpha.Value = Math.Clamp(value.A, _tbAlpha.Minimum, _tbAlpha.Maximum);
                if (_lblAlpha != null) _lblAlpha.Text = $"Alpha: {_tbAlpha?.Value ?? value.A}";
            }
        }

        [Browsable(true), Category("Appearance"), DefaultValue(255)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int SelectedAlpha
        {
            get => _tbAlpha?.Value ?? 255;
            set
            {
                if (_tbAlpha != null) _tbAlpha.Value = Math.Clamp(value, _tbAlpha.Minimum, _tbAlpha.Maximum);
                if (_lblAlpha != null) _lblAlpha.Text = $"Alpha: {_tbAlpha?.Value ?? value}";
            }
        }

        [Browsable(true), Category("Layout"), DefaultValue(85)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int SelectedWidth
        {
            get => _numWidth?.Value != null ? (int)_numWidth.Value : 85;
            set { if (_numWidth != null) _numWidth.Value = Math.Clamp(value, (int)_numWidth.Minimum, (int)_numWidth.Maximum); }
        }

        [Browsable(true), Category("Layout"), DefaultValue(85)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int SelectedHeight
        {
            get => _numHeight?.Value != null ? (int)_numHeight.Value : 85;
            set { if (_numHeight != null) _numHeight.Value = Math.Clamp(value, (int)_numHeight.Minimum, (int)_numHeight.Maximum); }
        }

        [Browsable(false)]
        public Size SelectedSize => new Size(SelectedWidth, SelectedHeight);

        [Browsable(true), Category("Behavior"), DefaultValue("")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string SelectedDisplayName
        {
            get => _txtDisplayName?.Text ?? "";
            set { if (_txtDisplayName != null) _txtDisplayName.Text = value ?? ""; }
        }

        private void BtnBorderColor_Click(object? sender, EventArgs e)
        {
            if (_colorDialog == null || _previewBorder == null) return;
            _colorDialog.Color = _previewBorder.BackColor;
            if (_colorDialog.ShowDialog(this) == DialogResult.OK)
                _previewBorder.BackColor = _colorDialog.Color;
        }

        private void ChkBorder_CheckedChanged(object? sender, EventArgs e) { }

        [Browsable(true), Category("Appearance"), DefaultValue(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool BorderEnabled
        {
            get => _chkBorder?.Checked ?? false;
            set { if (_chkBorder != null) _chkBorder.Checked = value; }
        }

        [Browsable(true), Category("Appearance"), DefaultValue(typeof(Color), "Black")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [TypeConverter(typeof(System.Drawing.ColorConverter))]
        public Color BorderColor
        {
            get => _previewBorder?.BackColor ?? Color.Black;
            set { if (_previewBorder != null) _previewBorder.BackColor = value; }
        }

        [Browsable(true), Category("Appearance"), DefaultValue(2)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int BorderWidth
        {
            get => _numBorderWidth?.Value != null ? (int)_numBorderWidth.Value : 2;
            set { if (_numBorderWidth != null) _numBorderWidth.Value = Math.Clamp(value, (int)_numBorderWidth.Minimum, (int)_numBorderWidth.Maximum); }
        }

        [Browsable(true), Category("Appearance"), DefaultValue(0)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int CornerRadius
        {
            get => _numCornerRadius?.Value != null ? (int)_numCornerRadius.Value : 0;
            set { if (_numCornerRadius != null) _numCornerRadius.Value = Math.Clamp(value, (int)_numCornerRadius.Minimum, (int)_numCornerRadius.Maximum); }
        }
    }
}