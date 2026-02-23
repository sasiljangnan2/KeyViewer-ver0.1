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
        // 컨텍스트 메뉴
        private ContextMenuStrip _contextMenuStrip = null!;
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

        // KeyPanel 서비스(팩토리 + 관리)
        private KeyPanelService _panelService = null!;
        // 로컬 참조(간편 접근)
        private List<KeyPanel> _keyPanels => _panelService.KeyPanels;

        public Form1()
        {
            InitializeComponent();

            // 컨텍스트 메뉴 초기화
            InitializeContextMenu();

            // 서비스 생성: Form의 마우스 드래그 핸들러를 전달
            _panelService = new KeyPanelService(this, _defaultColor, Panel_MouseDown, Panel_MouseMove, Panel_MouseUp);

            // 디자이너에서 만든 기존 Panel들을 서비스로 래핑 (키 매핑 지정)
            _panelService.WrapExistingPanel(panel1, Keys.A, Color.Red, _defaultColor);
            _panelService.WrapExistingPanel(panel2, Keys.S, Color.Red, _defaultColor);
            _panelService.WrapExistingPanel(panel3, Keys.D, Color.Red, _defaultColor);
            _panelService.WrapExistingPanel(panel4, Keys.L, Color.Red, _defaultColor);
            _panelService.WrapExistingPanel(panel5, Keys.Oem1, Color.Red, _defaultColor);
            _panelService.WrapExistingPanel(panel6, Keys.Oem7, Color.Red, _defaultColor);

            // 전역 후크 설치
            _proc = HookCallback;
            _hookID = InstallHook(_proc);

            // 폼 자체의 우클릭도 컨텍스트 메뉴가 뜨도록 연결
            this.ContextMenuStrip = _contextMenuStrip;
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
            // 우클릭: 컨텍스트 메뉴 표시
            if (e.Button == MouseButtons.Right)
            {
                if (sender is Control ctrl)
                {
                    _contextMenuStrip?.Show(ctrl, e.Location);
                }
                return;
            }

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

        // 컨텍스트 메뉴 생성 및 항목 연결
        private void InitializeContextMenu()
        {
            _contextMenuStrip = new ContextMenuStrip();

            var toggleTopMost = new ToolStripMenuItem("Always on Top");
            toggleTopMost.CheckOnClick = true;
            toggleTopMost.Checked = this.TopMost;
            toggleTopMost.Click += (s, e) =>
            {
                this.TopMost = toggleTopMost.Checked;
            };

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => this.Close();
           
            var addPanelItem = new ToolStripMenuItem("Add Key Panel");
            addPanelItem.Click += (s, e) =>
            {
                // 위치: 마우스 위치를 폼 좌표로 변환, 그리드 10에 맞춤
                Point loc = PointToClient(Cursor.Position);
                loc.X -= loc.X % 10;
                loc.Y -= loc.Y % 10;
                using var picker = new KeyPickerForm();
                if (picker.ShowDialog(this) == DialogResult.OK)
                {
                    // 새 KeyPanel 생성
                    var kp = _panelService.AddKeyPanel(picker.SelectedKey, picker.SelectedColor, _defaultColor, loc, new Size(85, 85));

                    // 라벨 추가 (키 이름 표시)
                   /* var lbl = new Label
                    {
                        Text = kp.Key.ToString(),
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter,
                        BackColor = Color.Transparent,
                        ForeColor = GetContrastColor(picker.SelectedColor)
                    };
                    kp.Panel.Controls.Add(lbl);*/
                }
            };

            _contextMenuStrip.Items.Add(toggleTopMost);
            _contextMenuStrip.Items.Add(new ToolStripSeparator());
            _contextMenuStrip.Items.Add(addPanelItem);
            _contextMenuStrip.Items.Add(new ToolStripSeparator());
           _contextMenuStrip.Items.Add(exitItem);
        }

        // 간단한 대비 색 반환(전경색 자동 선택)
        private Color GetContrastColor(Color bg)
        {
            // YIQ 밝기 공식
            int yiq = ((bg.R * 299) + (bg.G * 587) + (bg.B * 114)) / 1000;
            return yiq >= 128 ? Color.Black : Color.White;
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
            clampedX -= clampedX % 10;
            clampedY -= clampedY % 10;
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

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
