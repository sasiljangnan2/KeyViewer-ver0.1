using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;

namespace keyviewer
{
    public partial class Form1 : Form
    {
        // 컨텍스트 메뉴
        private ContextMenuStrip _contextMenuStrip = null!;
        private readonly Color[] _colors = { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Purple, Color.Orange };
        private int _colorIndex = 0;
        private readonly Color _defaultColor = SystemColors.ControlDark;

        // 레이아웃 UI
        private ComboBox _cbLayouts = null!;
        private Button _btnApplyLayout = null!;
        private Button _btnSaveLayout = null!;
        private Button _btnDeleteLayout = null!;
        private readonly string _layoutsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "layouts");

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

        // 디자이너에서 래핑한 초기 패널 수 제거: 이제 디자이너 패널 사용 안 함
        private int _initialWrappedCount = 0;

        // 현재 배경 이미지 경로 (선택적으로 저장) 및 배경 색
        private string? _currentBgImagePath = null;
        private Color _currentBgColor;

        // P/Invoke: 키보드 훅 관련 (필요)
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public Form1()
        {
            InitializeComponent();

            // 초기 배경 상태 저장
            _currentBgColor = this.BackColor;

            // 레이아웃 폴더 보장
            Directory.CreateDirectory(_layoutsDir);

            // 컨텍스트 메뉴 초기화 (먼저 생성)
            InitializeContextMenu();

            // 서비스 생성: Form의 마우스 드래그 핸들러를 전달하고 컨텍스트 메뉴 자동 할당 위임
            _panel_service_create:
            _panelService = new KeyPanelService(this, _defaultColor, Panel_MouseDown, Panel_MouseMove, Panel_MouseUp, _contextMenuStrip);

            // 초기 래핑된 개수 저장 (디자이너 패널을 더 이상 래핑하지 않음)
            _initialWrappedCount = _keyPanels.Count;

            // 전역 후크 설치
            _proc = HookCallback;
            _hookID = InstallHook(_proc);

            // 폼 자체의 우클릭도 컨텍스트 메뉴가 뜨도록 연결
            this.ContextMenuStrip = _contextMenuStrip;
        }

        // 레이아웃 목록 로드
        private void RefreshLayoutList()
        {
            _cbLayouts?.Items.Clear();
            foreach (var file in Directory.GetFiles(_layoutsDir, "*.json"))
            {
                _cbLayouts.Items.Add(Path.GetFileNameWithoutExtension(file));
            }
            if (_cbLayouts.Items.Count > 0)
                _cbLayouts.SelectedIndex = 0;
        }

        private void SaveCurrentLayout()
        {
            using var dlg = new SaveFileDialog
            {
                InitialDirectory = _layoutsDir,
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = "json",
                FileName = "layout"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            var layout = new KeyLayout { Name = Path.GetFileNameWithoutExtension(dlg.FileName) };
            foreach (var kp in _keyPanels)
            {
                var cfg = new KeyPanelConfig
                {
                    Key = kp.Key,
                    DownArgb = kp.DownColor.ToArgb(),
                    UpArgb = kp.UpColor.ToArgb(),
                    X = kp.Panel.Location.X,
                    Y = kp.Panel.Location.Y,
                    Width = kp.Panel.Size.Width,
                    Height = kp.Panel.Size.Height,
                    Name = kp.Panel.Name
                };
                layout.Panels.Add(cfg);
            }

            LayoutManager.SaveLayout(dlg.FileName, layout);
            RefreshLayoutList();
            _cbLayouts.SelectedItem = layout.Name;
        }

        private void DeleteSelectedLayout()
        {
            if (_cbLayouts.SelectedItem == null) return;
            string name = _cbLayouts.SelectedItem.ToString()!;
            string path = Path.Combine(_layoutsDir, name + ".json");
            if (File.Exists(path))
            {
                if (MessageBox.Show(this, $"Delete layout '{name}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    File.Delete(path);
                    RefreshLayoutList();
                }
            }
        }

        // 런타임으로 추가된 패널을 제거(디자이너 패널은 유지)
        private void RemoveRuntimePanels()
        {
            var runtime = new List<KeyPanel>();
            for (int i = _initialWrappedCount; i < _keyPanels.Count; i++)
            {
                runtime.Add(_keyPanels[i]);
            }

            foreach (var kp in runtime)
            {
                if (kp.Panel.Parent != null)
                    Controls.Remove(kp.Panel);
                _keyPanels.Remove(kp);
            }
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

        // 간단한 대비 색 반환(전경색 자동 선택)
        private Color GetContrastColor(Color bg)
        {
            // YIQ 밝기 공식
            int yiq = ((bg.R * 299) + (bg.G * 587) + (bg.B * 114)) / 1000;
            return yiq >= 128 ? Color.Black : Color.White;
        }

        // 컨택스트 매뉴 및 Global Settings 등 기존 코드 유지
        private void InitializeContextMenu()
        {
            // 클릭 시점의 마우스 위치를 폼 좌표로 계산 — 이전에는 초기화 시점에만 계산되어 문제가 있었습니다.
           

            _contextMenuStrip = new ContextMenuStrip();

            var toggleTopMost = new ToolStripMenuItem("Always on Top");
            toggleTopMost.CheckOnClick = true;
            toggleTopMost.Checked = this.TopMost;
            toggleTopMost.Click += (s, e) => this.TopMost = toggleTopMost.Checked;

            var layoutsItem = new ToolStripMenuItem("Layouts...");
            layoutsItem.Click += (s, e) =>
            {
                string layoutsDir = _layoutsDir;
                if (!Directory.Exists(layoutsDir)) Directory.CreateDirectory(layoutsDir);
                using var dlg = new LayoutPickerForm(layoutsDir);
                if (dlg.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(dlg.SelectedLayoutFileName))
                {
                    string path = Path.Combine(layoutsDir, dlg.SelectedLayoutFileName + ".json");
                    var layout = LayoutManager.LoadLayout(path);
                    if (layout == null) { MessageBox.Show(this, "레이아웃을 불러올 수 없습니다."); return; }
                    RemoveRuntimePanels();
                    var created = LayoutManager.ApplyLayout(layout, _panelService);
                    foreach (var kp in created)
                    {
                        // 패널에 paint 기반 키 그리기 핸들러를 등록
                        AttachKeyPaint(kp);
                        kp.Panel.ContextMenuStrip = _contextMenuStrip;
                    }
                }
            };

            var saveLayoutItem = new ToolStripMenuItem("Save Layout...");
            saveLayoutItem.Click += (s, e) => SaveCurrentLayout();

            var addPanelItem = new ToolStripMenuItem("Add Key Panel");
            addPanelItem.Click += (s, e) =>
            {
                Point loc = PointToClient(Cursor.Position);
                loc.X -= loc.X % 10;
                loc.Y -= loc.Y % 10;
                int w = 85, h = 85;
                loc.X = Math.Clamp(loc.X, 0, Math.Max(0, ClientSize.Width - w));
                loc.Y = Math.Clamp(loc.Y, 0, Math.Max(0, ClientSize.Height - h));

                using var editor = new PanelEditorForm();
                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    var up = editor.SelectedUpColor;
                    var down = editor.SelectedDownColor;
                    var kp = _panelService.AddKeyPanel(editor.SelectedKey, down, up, loc, new Size(w, h));
                    kp.Panel.ContextMenuStrip = _contextMenuStrip;
                    kp.Panel.BackColor = kp.UpColor;

                    // 패널 내부에 별도 라벨을 추가하지 않고, Paint로 키를 그리도록 등록
                    AttachKeyPaint(kp);
                    kp.Panel.BringToFront();
                }
            };

            var setEditorItem = new ToolStripMenuItem("Edit Panel...");
            setEditorItem.Click += (s, e) =>
            {
                var kp = GetKeyPanelFromContext();
                if (kp == null) return;
                OpenPanelEditor(kp);
            };

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => this.Close();

            var globalSettingsItem = new ToolStripMenuItem("Global Settings...");
            globalSettingsItem.Click += (s, e) =>
            {
                Color upInit = _keyPanels.Count > 0 ? _keyPanels[0].UpColor : _defaultColor;
                Color downInit = _keyPanels.Count > 0 ? _keyPanels[0].DownColor : Color.Red;
                Color bgInit = this.BackColor;
                string? currentBgPath = _currentBgImagePath;
                int keyAlphaInit = _keyPanels.Count > 0 ? _keyPanels[0].UpColor.A : 255;
                int opacityPercent = (int)(this.Opacity * 100);

                using var dlg = new GlobalEditorForm(upInit, downInit, bgInit, currentBgPath, keyAlphaInit, opacityPercent);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    foreach (var kp in _keyPanels)
                    {
                        kp.UpColor = dlg.SelectedUpColor;
                        kp.DownColor = dlg.SelectedDownColor;
                        kp.Panel.BackColor = kp.UpColor;
                        kp.Panel.Invalidate(); // paint 기반 텍스트 갱신
                    }

                    this.BackColor = dlg.SelectedBgColor;
                    _currentBgColor = dlg.SelectedBgColor;
                    _currentBgImagePath = dlg.SelectedBgImagePath;

                    try
                    {
                        this.BackgroundImage?.Dispose();
                        if (!string.IsNullOrEmpty(_currentBgImagePath) && File.Exists(_currentBgImagePath))
                        {
                            var img = Image.FromFile(_currentBgImagePath);
                            this.BackgroundImage = new Bitmap(img);
                            this.BackgroundImageLayout = ImageLayout.Stretch;
                        }
                        else
                        {
                            this.BackgroundImage = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"이미지 적용 실패: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            _contextMenuStrip.Items.Add(toggleTopMost);
            _contextMenuStrip.Items.Add(new ToolStripSeparator());
            _contextMenuStrip.Items.Add(layoutsItem);
            _contextMenuStrip.Items.Add(new ToolStripSeparator());
            _contextMenuStrip.Items.Add(saveLayoutItem);
            _contextMenuStrip.Items.Add(new ToolStripSeparator());
            _contextMenuStrip.Items.Add(addPanelItem);
            _contextMenuStrip.Items.Add(new ToolStripSeparator());
            _contextMenuStrip.Items.Add(setEditorItem);
            _contextMenuStrip.Items.Add(new ToolStripSeparator());
            _contextMenuStrip.Items.Add(globalSettingsItem);
            _contextMenuStrip.Items.Add(new ToolStripSeparator());
            _contextMenuStrip.Items.Add(exitItem);
        }

        // 패널에 키 텍스트를 직접 그리도록 Paint 핸들러 연결
        private void AttachKeyPaint(KeyPanel kp)
        {
            kp.Panel.Paint -= Panel_DrawKey; // 중복 등록 방지
            kp.Panel.Paint += Panel_DrawKey;
            kp.Panel.Resize -= Panel_Invalidate;
            kp.Panel.Resize += Panel_Invalidate;
            kp.Panel.Invalidate();
        }

        private void Panel_Invalidate(object? sender, EventArgs e)
        {
            if (sender is Control c) c.Invalidate();
        }

        private void Panel_DrawKey(object? sender, PaintEventArgs e)
        {
            if (sender is not Panel p) return;
            // KeyPanel 찾기
            var kp = _keyPanels.FirstOrDefault(x => x.Panel == p);
            if (kp == null) return;

            var rect = p.ClientRectangle;
            using var sf = new System.Drawing.StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var brush = new SolidBrush(GetContrastColor(p.BackColor));
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            e.Graphics.DrawString(kp.Key.ToString(), this.Font, brush, rect, sf);
        }

        private KeyPanel? GetKeyPanelFromContext()
        {
            Control? src = _contextMenuStrip?.SourceControl;
            if (src == null) return null;

            Control? cur = src;
            while (cur != null && !(cur is Panel))
                cur = cur.Parent;
            if (cur == null) return null;

            var panel = cur as Panel;
            foreach (var kp in _keyPanels)
            {
                if (kp.Panel == panel) return kp;
            }
            return null;
        }

        private void OpenPanelEditor(KeyPanel kp)
        {
            if (kp == null) return;
            using var editor = new PanelEditorForm(kp.Key, kp.UpColor, kp.DownColor);
            if (editor.ShowDialog(this) == DialogResult.OK)
            {
                kp.Key = editor.SelectedKey;
                kp.UpColor = editor.SelectedUpColor;
                kp.DownColor = editor.SelectedDownColor;
                kp.Panel.BackColor = kp.UpColor;
                kp.Panel.Invalidate();
            }
        }
    }
}
