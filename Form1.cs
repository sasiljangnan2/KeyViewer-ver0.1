using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;

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

        // KeyPanel 리스트 (객체지향 매핑)
        private List<KeyPanel> _keyPanels = new List<KeyPanel>();

        public Form1()
        {
            InitializeComponent();

            // 모든 Panel에 마우스 이벤트 등록 (디자이너/팩토리로 만든 패널도 포함)
            foreach (Control c in Controls)
            {
                if (c is Panel p)
                {
                    p.MouseDown += Panel_MouseDown;
                    p.MouseMove += Panel_MouseMove;
                    p.MouseUp += Panel_MouseUp;
                }
            }

            // KeyPanel 객체로 매핑: (패널, 키, DownColor, UpColor)
            _keyPanels = new List<KeyPanel>
            {
                new KeyPanel(panel1, Keys.A, Color.Red, _defaultColor),
                new KeyPanel(panel2, Keys.S, Color.Red, _defaultColor),
                new KeyPanel(panel3, Keys.D, Color.Red, _defaultColor),
                new KeyPanel(panel4, Keys.L, Color.Red, _defaultColor),
                new KeyPanel(panel5, Keys.Oem1, Color.Red, _defaultColor),
                new KeyPanel(panel6, Keys.Oem7, Color.Red, _defaultColor)
            };

            // 전역 후크 설치
            _proc = HookCallback;
            _hookID = InstallHook(_proc);
        }

        // 런타임/디자이너 공용 패널 팩토리
        private Panel CreateButtonPanel(string name, Point location, Size size, int tabIndex)
        {
            var p = new Panel
            {
                BackColor = _defaultColor,
                Location = location,
                Name = name,
                Size = size,
                TabIndex = tabIndex
            };

            // 기본 Mouse 이벤트 바인딩은 생성 후 Form1 생성자에서 일괄 등록하므로 여기서는 생략.
            return p;
        }

        // 런타임에서 패널+키 매핑을 추가하는 편의 메서드
        public KeyPanel AddKeyPanel(Keys key, Color downColor, Color upColor, Point location, Size? size = null)
        {
            Size panelSize = size ?? new Size(104, 96);
            // 이름 충돌 가능성 있으므로 안전하게 고유 이름 생성
            string nameBase = "panel";
            int idx = 1;
            while (Controls.Find(nameBase + idx, false).Length > 0) idx++;
            string name = nameBase + idx;

            var panel = CreateButtonPanel(name, location, panelSize, Controls.Count);
            Controls.Add(panel);

            // 마우스 드래그 이벤트 연결
            panel.MouseDown += Panel_MouseDown;
            panel.MouseMove += Panel_MouseMove;
            panel.MouseUp += Panel_MouseUp;

            var kp = new KeyPanel(panel, key, downColor, upColor);
            _keyPanels.Add(kp);
            return kp;
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

        // 전역 키 핸들러: KeyPanel에 위임
        private void HandleGlobalKeyDown(Keys key)
        {
            foreach (var kp in _keyPanels)
            {
                kp.HandleKeyDown(key);
            }
        }

        private void HandleGlobalKeyUp(Keys key)
        {
            foreach (var kp in _keyPanels)
            {
                kp.HandleKeyUp(key);
            }
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

        private void panel4_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
