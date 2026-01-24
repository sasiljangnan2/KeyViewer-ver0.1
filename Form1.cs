using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace keyviewer
{
    public partial class Form1 : Form
    {
        private readonly Color[] _colors = { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Purple, Color.Orange };
        private int _colorIndex = 0;
        private readonly Color _defaultColor = SystemColors.ControlDark;

        // 드래그 상태 필드
        private bool _dragging = false;
        private Point _dragStartMouse;
        private Point _dragStartLocation;
        private Control? _draggedControl;

        // 전역 후크 관련
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public Form1()
        {
            InitializeComponent();

            // 모든 Panel에 마우스 이벤트 등록
            foreach (Control c in Controls)
            {
                if (c is Panel p)
                {
                    p.MouseDown += Panel_MouseDown;
                    p.MouseMove += Panel_MouseMove;
                    p.MouseUp += Panel_MouseUp;
                }
            }

            // 전역 후크 설치
            _proc = HookCallback;
            _hookID = InstallHook(_proc);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // 폼 종료 시 후크 해제
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
            base.OnFormClosed(e);
        }

        private IntPtr InstallHook(LowLevelKeyboardProc proc)
        {
            using Process curProcess = Process.GetCurrentProcess();
            using ProcessModule? curModule = curProcess.MainModule;
            IntPtr moduleHandle = curModule != null ? GetModuleHandle(curModule.ModuleName) : IntPtr.Zero;
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, moduleHandle, 0);
        }

        // 폴더/폰트 대화상자 이벤트(사용하지 않으면 그대로 둬도 됩니다)
        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e) { }

        private void fontDialog1_Apply(object sender, EventArgs e) { }

        private void panel1_Paint(object sender, PaintEventArgs e) { }

        // --- 전역 후크 콜백 ---
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    int vk = Marshal.ReadInt32(lParam);
                    Keys key = (Keys)vk;
                    BeginInvoke(new Action(() => HandleGlobalKeyDown(key)));
                }
                else if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
                {
                    int vk = Marshal.ReadInt32(lParam);
                    Keys key = (Keys)vk;
                    BeginInvoke(new Action(() => HandleGlobalKeyUp(key)));
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        // 전역 키 핸들러 (UI 변경은 UI 스레드에서 실행됨)
        private void HandleGlobalKeyDown(Keys key)
        {
            if (key == Keys.A) panel1.BackColor = Color.Red;
            else if (key == Keys.S) panel2.BackColor = Color.Red;
            else if (key == Keys.D) panel3.BackColor = Color.Red;
            else if (key == Keys.L) panel4.BackColor = Color.Red;
            else if (key == Keys.Oem1) panel5.BackColor = Color.Red; // ';' 레이아웃에 따라 다름
            else if (key == Keys.Oem7) panel6.BackColor = Color.Red; // ''' 레이아웃에 따라 다름
        }

        private void HandleGlobalKeyUp(Keys key)
        {
            if (key == Keys.A) panel1.BackColor = _defaultColor;
            else if (key == Keys.S) panel2.BackColor = _defaultColor;
            else if (key == Keys.D) panel3.BackColor = _defaultColor;
            else if (key == Keys.L) panel4.BackColor = _defaultColor;
            else if (key == Keys.Oem1) panel5.BackColor = _defaultColor;
            else if (key == Keys.Oem7) panel6.BackColor = _defaultColor;
        }

        // 키 폼 이벤트(로컬 포커스용, 필요하면 사용)
        private void Form1_KeyDown(object sender, KeyEventArgs e) { /* 기존 로컬 처리 있으면 유지 */ }
        private void Form1_KeyUp(object sender, KeyEventArgs e) { /* 기존 로컬 처리 있으면 유지 */ }

        private void panel2_Paint(object sender, PaintEventArgs e) { }

        // --- 마우스 드래그 핸들러 ---
        private void Panel_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (sender is Control c)
            {
                _draggedControl = c;
                _dragging = true;
                _dragStartMouse = Control.MousePosition;
                _dragStartLocation = c.Location;
                c.Capture = true;
            }
        }

        private void Panel_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_dragging || _draggedControl == null) return;

            Point currentMouse = Control.MousePosition;
            int dx = currentMouse.X - _dragStartMouse.X;
            int dy = currentMouse.Y - _dragStartMouse.Y;
            Point desired = new Point(_dragStartLocation.X + dx, _dragStartLocation.Y + dy);

            int maxX = ClientSize.Width - _draggedControl.Width;
            int maxY = ClientSize.Height - _draggedControl.Height;
            int clampedX = Math.Max(0, Math.Min(desired.X, maxX));
            int clampedY = Math.Max(0, Math.Min(desired.Y, maxY));

            _draggedControl.Location = new Point(clampedX, clampedY);
        }

        private void Panel_MouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (_draggedControl != null)
            {
                _draggedControl.Capture = false;
                _draggedControl = null;
            }
            _dragging = false;
        }

        // P/Invoke
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
