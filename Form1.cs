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
        private bool _backgroundTransparent = false; // 배경 투명화 상태
        private readonly Color _transparencyKeyColor = Color.Magenta; // TransparencyKey 색

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

            // 폼 이동/크기 변경 시 레이어드 윈도우 동기화
            this.LocationChanged += (s, e) => SyncLayeredWindows();
            this.SizeChanged += (s, e) => SyncLayeredWindows();
            this.VisibleChanged += (s, e) => SyncLayeredWindowsVisibility();
        }

        // 레이어드 윈도우 위치 동기화
        private void SyncLayeredWindows()
        {
            foreach (var kp in _keyPanels)
            {
                var screenLoc = this.PointToScreen(kp.Panel.Location);
                kp.UpdatePosition(screenLoc);
            }
        }

        private void SyncLayeredWindowsVisibility()
        {
            foreach (var kp in _keyPanels)
            {
                if (this.Visible)
                    kp.Show();
                else
                    kp.Hide();
            }
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
                kp.Dispose(); // 레이어드 윈도우 해제
                if (kp.Panel.Parent != null)
                    Controls.Remove(kp.Panel);
                _keyPanels.Remove(kp);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }

            // 모든 레이어드 윈도우 해제
            foreach (var kp in _keyPanels)
            {
                kp.Dispose();
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

        // 더미 패널용 드래그 핸들러 (레이어드 윈도우가 대신 처리하므로 사용 안 함)
        private void Panel_MouseDown(object? sender, MouseEventArgs e) { }
        private void Panel_MouseMove(object? sender, MouseEventArgs e) { }
        private void Panel_MouseUp(object? sender, MouseEventArgs e) { }

        private Color GetContrastColor(Color bg)
        {
            int yiq = ((bg.R * 299) + (bg.G * 587) + (bg.B * 114)) / 1000;
            return yiq >= 128 ? Color.Black : Color.White;
        }

        private void InitializeContextMenu()
        {
            _contextMenuStrip = new ContextMenuStrip();

            var toggleTopMost = new ToolStripMenuItem("Always on Top");
            toggleTopMost.CheckOnClick = true;
            toggleTopMost.Checked = this.TopMost;
            toggleTopMost.Click += (s, e) =>
            {
                this.TopMost = toggleTopMost.Checked;
                // 레이어드 윈도우도 TopMost 동기화
                foreach (var kp in _keyPanels)
                {
                    if (kp.LayeredWindow != null && !kp.LayeredWindow.IsDisposed)
                        kp.LayeredWindow.TopMost = this.TopMost;
                }
            };

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
                        kp.Panel.ContextMenuStrip = _contextMenuStrip;
                        kp.UpdateVisual();
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

                using var editor = new PanelEditorForm();
                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    var up = editor.SelectedUpColor;
                    var down = editor.SelectedDownColor;
                    var size = editor.SelectedSize; // 사용자가 입력한 크기 사용
                    
                    // 폼 내부로 클램프
                    loc.X = Math.Clamp(loc.X, 0, Math.Max(0, ClientSize.Width - size.Width));
                    loc.Y = Math.Clamp(loc.Y, 0, Math.Max(0, ClientSize.Height - size.Height));

                    var kp = _panelService.AddKeyPanel(editor.SelectedKey, down, up, loc, size);
                    kp.BringToFront();
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

                using var dlg = new GlobalEditorForm(upInit, downInit, bgInit, currentBgPath, keyAlphaInit, opacityPercent, _backgroundTransparent);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    // 배경 투명화 모드: 키 알파를 강제로 255로 설정 (마젠타가 비치지 않도록)
                    int finalAlpha = dlg.BackgroundTransparent ? 255 : dlg.SelectedUpColor.A;

                    foreach (var kp in _keyPanels)
                    {
                        kp.UpColor = Color.FromArgb(finalAlpha, dlg.SelectedUpColor);
                        kp.DownColor = Color.FromArgb(finalAlpha, dlg.SelectedDownColor);
                        kp.Panel.BackColor = kp.UpColor;
                        kp.UpdateVisual(); // 레이어드 윈도우 갱신
                    }

                    this.BackColor = dlg.SelectedBgColor;
                    _currentBgColor = dlg.SelectedBgColor;
                    _currentBgImagePath = dlg.SelectedBgImagePath;
                    _backgroundTransparent = dlg.BackgroundTransparent;

                    // 배경 투명화 적용
                    if (_backgroundTransparent)
                    {
                        this.BackgroundImage?.Dispose();
                        this.BackgroundImage = null;
                        this.BackColor = _transparencyKeyColor;
                        this.TransparencyKey = _transparencyKeyColor;
                    }
                    else
                    {
                        this.TransparencyKey = Color.Empty;
                        this.BackColor = _currentBgColor;

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

        private KeyPanel? GetKeyPanelFromContext()
        {
            Control? src = _contextMenuStrip?.SourceControl;
            
            // 레이어드 윈도우에서 우클릭한 경우
            if (src is Form layered)
            {
                return _keyPanels.FirstOrDefault(kp => kp.LayeredWindow == layered);
            }

            // 더미 패널에서 우클릭한 경우
            if (src == null) return null;
            Control? cur = src;
            while (cur != null && !(cur is Panel))
                cur = cur.Parent;
            if (cur == null) return null;

            var panel = cur as Panel;
            return _keyPanels.FirstOrDefault(kp => kp.Panel == panel);
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
                
                // 크기 변경 지원 (사용자가 수정한 경우)
                var newSize = editor.SelectedSize;
                if (kp.Panel.Size != newSize)
                {
                    kp.Panel.Size = newSize;
                    kp.UpdateSize(newSize);
                }
                
                kp.UpdateVisual(); // 레이어드 윈도우 갱신
            }
        }
    }
}
