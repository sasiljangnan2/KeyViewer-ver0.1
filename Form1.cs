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
        private readonly Color _defaultColor = SystemColors.Control;

        // 전역 후크 관련 필드
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookId = IntPtr.Zero;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public Form1()
        {
            InitializeComponent();

            // 후크 콜백을 가비지 컬렉션에서 보호하기 위해 필드에 보관
            _proc = HookCallback;
            _hookId = SetHook(_proc);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // 폼 종료 시 후크 해제
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
            base.OnFormClosed(e);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using Process curProcess = Process.GetCurrentProcess();
            using ProcessModule curModule = curProcess.MainModule!;
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }

        // 후크 콜백: 네이티브 스레드에서 호출되므로 UI 변경은 BeginInvoke로 마샬링
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    Keys key = (Keys)vkCode;
                    BeginInvoke(new Action(() => HandleGlobalKeyDown(key)));
                }
                else if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    Keys key = (Keys)vkCode;
                    BeginInvoke(new Action(() => HandleGlobalKeyUp(key)));
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        // 전역 키 다운 처리: 누르고 있는 동안 색 변경
        private void HandleGlobalKeyDown(Keys key)
        {
            if (key == Keys.A)
            {
                panel1.BackColor = Color.Red;
            }
            else if (key == Keys.S)
            {
                panel2.BackColor = Color.Red;
            }
            else if (key == Keys.D)
            {
                panel3.BackColor = Color.Red;
            }
            else if (key == Keys.L)
            {
                panel4.BackColor = Color.Red;
            }
            else if (key == Keys.Oem1) // 대부분의 레이아웃에서 ';'
            {
                panel5.BackColor = Color.Red;
            }
            else if (key == Keys.Oem7) // 대부분의 레이아웃에서 '''
            {
                panel6.BackColor = Color.Red;
            }
        }

        // 전역 키 업 처리: 떼면 기본색으로 복원
        private void HandleGlobalKeyUp(Keys key)
        {
            if (key == Keys.A)
            {
                panel1.BackColor = _defaultColor;
            }
            else if (key == Keys.S)
            {
                panel2.BackColor = _defaultColor;
            }
            else if (key == Keys.D)
            {
                panel3.BackColor = _defaultColor;
            }
            else if (key == Keys.L)
            {
                panel4.BackColor = _defaultColor;
            }
            else if (key == Keys.Oem1)
            {
                panel5.BackColor = _defaultColor;
            }
            else if (key == Keys.Oem7)
            {
                panel6.BackColor = _defaultColor;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {
        }

        private void fontDialog1_Apply(object sender, EventArgs e)
        {
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {
        }
    }
}
