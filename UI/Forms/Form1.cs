using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using keyviewer.UI.Controls;
using keyviewer.UI.Editors;      // 🔥 추가 (PanelEditorForm, GlobalEditorForm, LayoutPickerForm)
using keyviewer.Services;
using keyviewer.Models;

namespace keyviewer.UI.Forms
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
        private bool _dragging = true;
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
        private const int WM_SYSCOMMAND = 0x0112;  // 🔥 추가
        private const int SC_MINIMIZE = 0xF020;     // 🔥 추가
        private const int SC_RESTORE = 0xF120;      // 🔥 추가
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

        // 필드 추가
        private Color _chromaKeyColor = Color.FromArgb(255, 0, 255); // 기본 마젠타

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

        private bool _obsCompatibilityMode = false; // OBS 호환 모드
        private bool _isMinimizing = false; // 🔥 최소화 중 플래그
        private bool _alwaysOnTop = false; // 🔥 Always on Top 상태 추적

        public Form1()
        {
            InitializeComponent();

            // OBS 호환 모드 활성화 여부 설정
            _obsCompatibilityMode = false; // 기본값

            _currentBgColor = this.BackColor;
            Directory.CreateDirectory(_layoutsDir);
            InitializeContextMenu();

            _panelService = new KeyPanelService(this, _defaultColor,
                Panel_MouseDown, Panel_MouseMove, Panel_MouseUp,
                _contextMenuStrip, _obsCompatibilityMode);

            _initialWrappedCount = _keyPanels.Count;

            _proc = HookCallback;
            _hookID = InstallHook(_proc);

            this.ContextMenuStrip = _contextMenuStrip;

            // 🆕 이벤트 핸들러를 메서드로 분리하여 관리
            SetupEventHandlers();

            if (_keyPanels.Count == 0)  // 패널이 없으면
            {
                var defaultLayout = LayoutManager.CreateSampleLayout("Default");
                var created = LayoutManager.ApplyLayout(defaultLayout, _panelService);
                
                foreach (var kp in created)
                {
                    kp.UpdateVisual();
                    kp.Show();
                }
            }

            this.Shown += OnFormShown;
        }

        // 🆕 이벤트 핸들러 설정 메서드
        private void SetupEventHandlers()
        {
            if (!_obsCompatibilityMode)
            {
                this.LocationChanged += SyncLayeredWindows;
                this.SizeChanged += SyncLayeredWindows;
                this.VisibleChanged += SyncLayeredWindowsVisibility;

                this.Load += (s, e) =>
                {
                    SyncLayeredWindows();
                    SyncLayeredWindowsVisibility();
                };

                // 🔥 Owner 관계에 의해 Z-order 자동 관리 → Activated 핸들러 불필요
            }
        }

        // 🆕 Shown 이벤트 핸들러
        private void OnFormShown(object? sender, EventArgs e)
        {
            if (_obsCompatibilityMode) return;
            
            System.Threading.Tasks.Task.Delay(500).ContinueWith(_ =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    foreach (var kp in _keyPanels)
                    {
                        kp.Show();        // 🔥 _layeredWindow.Show() 대신
                        kp.BringToFront();
                        kp.UpdateVisual();
                    }
                }));
            });
        }

        // 레이어드 윈도우 위치 동기화 (이벤트 핸들러 시그니처로 변경)
        private void SyncLayeredWindows(object? sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized || _isMinimizing) return;
            if (this.Left < -10000 || this.Top < -10000) return; // 🔥 최소화 좌표(-32000) 필터링
    
            foreach (var kp in _keyPanels)
            {
                var screenLoc = this.PointToScreen(kp.Panel.Location);
                kp.UpdatePosition(screenLoc);
            }
        }

        // 오버로드 추가 (파라미터 없는 버전)
        private void SyncLayeredWindows()
        {
            SyncLayeredWindows(null, EventArgs.Empty);
        }

        private void SyncLayeredWindowsVisibility(object? sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized || _isMinimizing) return; // 🔥 플래그 체크 추가
    
            foreach (var kp in _keyPanels)
            {
                if (this.Visible)
                    kp.Show();
                else
                    kp.Hide();
            }
        }

        // 오버로드 추가
        private void SyncLayeredWindowsVisibility()
        {
            SyncLayeredWindowsVisibility(null, EventArgs.Empty);
        }

        // 레이아웃 목록 로드
        private void RefreshLayoutList()
        {
            // 🔥 _cbLayouts가 null이면 아무것도 안 함
            if (_cbLayouts == null) return;
            
            _cbLayouts.Items.Clear();
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

            try
            {
                var layout = new KeyLayout 
                { 
                    Name = Path.GetFileNameWithoutExtension(dlg.FileName),
                    FormWidth = this.ClientSize.Width,
                    FormHeight = this.ClientSize.Height,
                    BackgroundColorArgb = _currentBgColor.ToArgb(),
                    BackgroundImagePath = _currentBgImagePath,
                    BackgroundTransparent = _backgroundTransparent,
                    ChromaKeyColorArgb = _chromaKeyColor.ToArgb(),
                    WindowOpacityPercent = (int)(this.Opacity * 100),
                    OBSCompatibilityMode = _obsCompatibilityMode // 🆕
                };
        
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
                        Name = kp.Panel.Name ?? $"panel_{kp.Key}",
                        DisplayName = kp.DisplayName,
                        BorderEnabled = kp.BorderEnabled,
                        BorderColorArgb = kp.BorderColor.ToArgb(),
                        BorderWidth = kp.BorderWidth,
                        CornerRadius = kp.CornerRadius
                    };
                    layout.Panels.Add(cfg);
                }

                LayoutManager.SaveLayout(dlg.FileName, layout);
                RefreshLayoutList();
        
                MessageBox.Show(this, $"레이아웃이 저장되었습니다.\n{dlg.FileName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, 
                    $"레이아웃 저장 실패:\n\n{ex.Message}", 
                    "Error", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
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
            toggleTopMost.Checked = false;
            toggleTopMost.Click += (s, e) =>
            {
                _alwaysOnTop = toggleTopMost.Checked;
                foreach (var kp in _keyPanels)
                {
                    kp.SetTopMost(_alwaysOnTop);
                }
            };

            // 🆕 OBS 호환 모드 토글
            var toggleOBSMode = new ToolStripMenuItem("OBS Compatibility Mode");
            toggleOBSMode.CheckOnClick = true;
            toggleOBSMode.Checked = _obsCompatibilityMode;
            toggleOBSMode.Click += (s, e) =>
            {
                bool newMode = toggleOBSMode.Checked;
                if (newMode == _obsCompatibilityMode) return; // 변경 없음
                
                var result = MessageBox.Show(this,
                    $"모드를 {(newMode ? "OBS 호환" : "레이어드 윈도우")}로 변경하시겠습니까?\n\n" +
                    "모든 키 패널이 다시 생성됩니다.",
                    "모드 변경",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    SwitchMode(newMode);
                }
                else
                {
                    toggleOBSMode.Checked = _obsCompatibilityMode; // 원래대로
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
                    
                    // 🔥 OBS 모드가 다르면 먼저 모드 전환 (패널 상태는 유지 안 함)
                    if (layout.OBSCompatibilityMode != _obsCompatibilityMode)
                    {
                        // 모든 패널 제거
                        foreach (var kp in _keyPanels.ToList())
                        {
                            kp.Dispose();
                            if (kp.Panel.Parent != null)
                                Controls.Remove(kp.Panel);
                        }
                        _keyPanels.Clear();
                        
                        // 이벤트 핸들러 제거
                        if (!_obsCompatibilityMode)
                        {
                            this.LocationChanged -= SyncLayeredWindows;
                            this.SizeChanged -= SyncLayeredWindows;
                            this.VisibleChanged -= SyncLayeredWindowsVisibility;
                        }
                        
                        // 모드 변경
                        _obsCompatibilityMode = layout.OBSCompatibilityMode;
                        
                        // KeyPanelService 재생성
                        _panelService = new KeyPanelService(this, _defaultColor,
                            Panel_MouseDown, Panel_MouseMove, Panel_MouseUp,
                            _contextMenuStrip, _obsCompatibilityMode);
                        
                        // 이벤트 핸들러 추가
                        if (!_obsCompatibilityMode)
                        {
                            this.LocationChanged += SyncLayeredWindows;
                            this.SizeChanged += SyncLayeredWindows;
                            this.VisibleChanged += SyncLayeredWindowsVisibility;
                        }
                    }
                    else
                    {
                        // 같은 모드면 기존 패널만 제거
                        RemoveRuntimePanels();
                    }
        
                    // 창 크기 복원
                    if (layout.FormWidth > 0 && layout.FormHeight > 0)
                    {
                        this.ClientSize = new Size(layout.FormWidth, layout.FormHeight);
                    }
        
                    // 배경 설정 복원
                    if (layout.BackgroundColorArgb != 0)
                    {
                        _currentBgColor = Color.FromArgb(layout.BackgroundColorArgb);
                    }
                    _currentBgImagePath = layout.BackgroundImagePath;
                    _backgroundTransparent = layout.BackgroundTransparent;
                    if (layout.ChromaKeyColorArgb != 0)
                    {
                        _chromaKeyColor = Color.FromArgb(layout.ChromaKeyColorArgb);
                    }
        
                    // 배경 적용
                    if (_backgroundTransparent)
                    {
                        this.BackgroundImage?.Dispose();
                        this.BackgroundImage = null;
                        this.BackColor = _chromaKeyColor;
                        this.TransparencyKey = _chromaKeyColor;
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
                        catch
                        {
                            this.BackgroundImage = null;
                        }
                    }
                    
                    // 투명도 복원
                    if (layout.WindowOpacityPercent > 0)
                    {
                        this.Opacity = Math.Clamp(layout.WindowOpacityPercent, 1, 100) / 100.0;
                    }
                    
                    // 키 패널 로드
                    var created = LayoutManager.ApplyLayout(layout, _panelService);
                    foreach (var kp in created)
                    {
                        kp.Panel.ContextMenuStrip = _contextMenuStrip;
                        kp.UpdateVisual();

                        // 🔥 모드에 따라 다르게 처리
                        if (!_obsCompatibilityMode)
                        {
                            var screenLoc = this.PointToScreen(kp.Panel.Location);
                            kp.UpdatePosition(screenLoc);
                            kp.Show();
                        }
                        else
                        {
                            // OBS 모드: 패널 표시
                            kp.Panel.Visible = true;
                            kp.Panel.BringToFront();
                        }
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
                    var size = editor.SelectedSize;
                    var displayName = editor.SelectedDisplayName;

                    loc.X = Math.Clamp(loc.X, 0, Math.Max(0, ClientSize.Width - size.Width));
                    loc.Y = Math.Clamp(loc.Y, 0, Math.Max(0, ClientSize.Height - size.Height));

                    var kp = _panelService.AddKeyPanel(editor.SelectedKey, down, up, loc, size);
                    
                    // 🆕 DisplayName 설정 및 UpdateVisual 호출
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        kp.DisplayName = displayName;
                        kp.UpdateVisual(); // 텍스트 갱신
                    }
                    
                    kp.BringToFront();
                }
            };

            // 🆕 조건부로 표시되는 메뉴 항목들
            var editPanelItem = new ToolStripMenuItem("Edit Panel...");
            editPanelItem.Click += (s, e) =>
            {
                var kp = GetKeyPanelFromContext();
                if (kp == null) return;
                OpenPanelEditor(kp);
            };

            var deletePanelItem = new ToolStripMenuItem("Delete Panel");
            deletePanelItem.Click += (s, e) =>
            {
                var kp = GetKeyPanelFromContext();
                if (kp == null) return;

                var result = MessageBox.Show(this,
                    $"Delete key panel '{kp.Key}'?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    kp.Dispose();
                    if (kp.Panel.Parent != null)
                        Controls.Remove(kp.Panel);
                    _keyPanels.Remove(kp);
                }
            };

            var globalSettingsItem = new ToolStripMenuItem("Global Settings...");
            globalSettingsItem.Click += (s, e) =>
            {
                Color upInit = _keyPanels.Count > 0 ? _keyPanels[0].UpColor : _defaultColor;
                Color downInit = _keyPanels.Count > 0 ? _keyPanels[0].DownColor : Color.Red;
                Color bgInit = _currentBgColor;  // 🔥 this.BackColor 대신 _currentBgColor 사용!
                string? currentBgPath = _currentBgImagePath;
                int keyAlphaInit = _keyPanels.Count > 0 ? _keyPanels[0].UpColor.A : 255;
                int opacityPercent = (int)(this.Opacity * 100);
                
                bool borderEnabledInit = _keyPanels.Count > 0 ? _keyPanels[0].BorderEnabled : false;
                Color borderColorInit = _keyPanels.Count > 0 ? _keyPanels[0].BorderColor : Color.Black;
                int borderWidthInit = _keyPanels.Count > 0 ? _keyPanels[0].BorderWidth : 2;
                int cornerRadiusInit = _keyPanels.Count > 0 ? _keyPanels[0].CornerRadius : 0;

                using var dlg = new GlobalEditorForm(upInit, downInit, bgInit, currentBgPath,
                    keyAlphaInit, opacityPercent, _backgroundTransparent, _chromaKeyColor,
                    borderEnabledInit, borderColorInit, borderWidthInit, cornerRadiusInit);

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _chromaKeyColor = dlg.ChromaKeyColor;
                    int finalAlpha = dlg.SelectedKeyAlpha;

                    foreach (var kp in _keyPanels)
                    {
                        kp.UpColor = Color.FromArgb(finalAlpha, dlg.SelectedUpColor);
                        kp.DownColor = Color.FromArgb(finalAlpha, dlg.SelectedDownColor);
                        kp.Panel.BackColor = kp.UpColor;
                        
                        kp.BorderEnabled = dlg.BorderEnabled;
                        kp.BorderColor = dlg.BorderColor;
                        kp.BorderWidth = dlg.BorderWidth;
                        kp.CornerRadius = dlg.CornerRadius;
                        
                        kp.UpdateVisual();
                    }

                    this.BackColor = dlg.SelectedBgColor;
                    _currentBgColor = dlg.SelectedBgColor;  // 🔥 항상 현재 색상 저장
                    _currentBgImagePath = dlg.SelectedBgImagePath;
                    
                    // 🔥 투명화 상태 이전값 저장 (토글 전)
                    bool wasPreviouslyTransparent = _backgroundTransparent;
                    _backgroundTransparent = dlg.BackgroundTransparent;

                    if (_backgroundTransparent)
                    {
                        // 투명화 활성화
                        this.BackgroundImage?.Dispose();
                        this.BackgroundImage = null;
                        this.BackColor = _chromaKeyColor;
                        this.TransparencyKey = _chromaKeyColor;

                        if (_obsCompatibilityMode)
                        {
                            MessageBox.Show(this,
                                $"OBS Setup Instructions:\n\n" +
                                $"1. Add 'Window Capture' source\n" +
                                $"2. Capture Method: 'Windows 10 (1903+)'\n" +
                                $"3. Enable 'Capture Client Area'\n" +
                                $"4. (Optional) Add 'Chroma Key' filter\n" +
                                $"5. Select color: {GetChromaKeyName(_chromaKeyColor)}\n\n" +
                                $"OR use 'Game Capture' with 'Allow Transparency' enabled",
                                "OBS Transparency Setup",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        // 🔥 투명화 해제 → 이전 배경 복원
                        this.TransparencyKey = Color.Empty;
                        this.BackColor = _currentBgColor;  // 저장된 색상 복원

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

                    this.Opacity = dlg.SelectedOpacityPercent / 100.0;

                    if (dlg.SelectedOpacityPercent < 100 || dlg.SelectedKeyAlpha < 255)
                    {
                        if (_obsCompatibilityMode && !_backgroundTransparent)
                        {
                            MessageBox.Show(this,
                                "OBS에서 반투명을 캡처하려면:\n\n" +
                                "1. Window Capture → Capture Method를\n" +
                                "   'Windows 10 (1903+)'로 변경\n\n" +
                                "   (Allow Transparency 체크)",
                                "OBS Transparency Tip",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    }
                }
            };
             
            var debugItem = new ToolStripMenuItem("Debug: Show Window Info");
            debugItem.Click += (s, e) =>
            {
                var info = $"Mode: {(_obsCompatibilityMode ? "OBS" : "Layered")}\n" +
                           $"Key Panels: {_keyPanels.Count}\n" +
                           $"Form: {this.Location}, Size: {this.Size}\n\n";

                foreach (var kp in _keyPanels)
                {
                    info += $"Panel {kp.Key}:\n";
                    info += $"  Panel Pos: {kp.Panel.Location}\n";
                    if (kp.LayeredWindow != null && !kp.LayeredWindow.IsDisposed)
                    {
                        info += $"  Layered Pos: {kp.LayeredWindow.Location}\n";
                        info += $"  Layered Size: {kp.LayeredWindow.Size}\n";
                        info += $"  Visible: {kp.LayeredWindow.Visible}\n";
                    }
                    else
                    {
                        info += "  Layered Window: NULL or Disposed\n";
                    }
                    info += "\n";
                }

                MessageBox.Show(this, info, "Debug Info");
            };

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => this.Close();

            // 기본 메뉴 항목 추가
            _contextMenuStrip.Items.Add(toggleTopMost);
            _contextMenuStrip.Items.Add(toggleOBSMode); // 🆕 OBS 모드 토글
            _contextMenuStrip.Items.Add(new ToolStripSeparator());
            _contextMenuStrip.Items.Add(layoutsItem);
            _contextMenuStrip.Items.Add(saveLayoutItem);
            _contextMenuStrip.Items.Add(new ToolStripSeparator());
            _contextMenuStrip.Items.Add(addPanelItem);
            _contextMenuStrip.Items.Add(new ToolStripSeparator());
            _contextMenuStrip.Items.Add(globalSettingsItem);
            _contextMenuStrip.Items.Add(debugItem);
            _contextMenuStrip.Items.Add(new ToolStripSeparator());
            _contextMenuStrip.Items.Add(exitItem);

            // 🆕 Opening 이벤트: 메뉴가 열릴 때 동적으로 항목 추가/제거
            _contextMenuStrip.Opening += (s, e) =>
            {
                // 기존 Edit/Delete 항목 제거
                _contextMenuStrip.Items.Remove(editPanelItem);
                _contextMenuStrip.Items.Remove(deletePanelItem);

                // 키 패널에서 우클릭한 경우에만 Edit/Delete 추가
                var kp = GetKeyPanelFromContext();
                if (kp != null)
                {
                    // "Add Key Panel" 다음에 삽입 (인덱스 5 위치)
                    int insertIndex = _contextMenuStrip.Items.IndexOf(addPanelItem) + 1;
                    
                    _contextMenuStrip.Items.Insert(insertIndex, editPanelItem);
                    _contextMenuStrip.Items.Insert(insertIndex + 1, deletePanelItem);
                }
            };
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
            
            using var editor = new PanelEditorForm(
                kp.Key, 
                kp.UpColor, 
                kp.DownColor, 
                kp.DisplayName ?? "", 
                kp.Panel.Size,
                kp.BorderEnabled,
                kp.BorderColor,
                kp.BorderWidth,
                kp.CornerRadius); // 🆕 모서리 반경 전달
    
            if (editor.ShowDialog(this) == DialogResult.OK)
            {
                kp.Key = editor.SelectedKey;
                kp.UpColor = editor.SelectedUpColor;
                kp.DownColor = editor.SelectedDownColor;
                kp.DisplayName = editor.SelectedDisplayName;
                kp.Panel.BackColor = kp.UpColor;

                // 테두리 설정
                kp.BorderEnabled = editor.BorderEnabled;
                kp.BorderColor = editor.BorderColor;
                kp.BorderWidth = editor.BorderWidth;
                
                // 🆕 모서리 반경
                kp.CornerRadius = editor.CornerRadius;

                var newSize = editor.SelectedSize;
                if (kp.Panel.Size != newSize)
                {
                    kp.Panel.Size = newSize;
                    kp.UpdateSize(newSize);
                }

                kp.UpdateVisual();
            }
        }

        // OBS 모드 전환
        private void SwitchMode(bool obsMode)
        {
            // 현재 패널 상태 저장
            var panelStates = new List<(Keys Key, Color Up, Color Down, Point Loc, Size Size, 
                string? DisplayName, bool BorderEnabled, Color BorderColor, int BorderWidth, int CornerRadius)>();
            
            foreach (var kp in _keyPanels.ToList())
            {
                panelStates.Add((
                    kp.Key,
                    kp.UpColor,
                    kp.DownColor,
                    kp.Panel.Location,
                    kp.Panel.Size,
                    kp.DisplayName,
                    kp.BorderEnabled,
                    kp.BorderColor,
                    kp.BorderWidth,
                    kp.CornerRadius
                ));
            }

            // 모든 패널 제거
            foreach (var kp in _keyPanels.ToList())
            {
                kp.Dispose();
                if (kp.Panel.Parent != null)
                    Controls.Remove(kp.Panel);
            }
            _keyPanels.Clear();

            // 🔥 이벤트 핸들러 제거 (레이어드 윈도우 모드였던 경우)
            if (!_obsCompatibilityMode)
            {
                this.LocationChanged -= SyncLayeredWindows;
                this.SizeChanged -= SyncLayeredWindows;
                this.VisibleChanged -= SyncLayeredWindowsVisibility;
            }

            // 모드 변경
            _obsCompatibilityMode = obsMode;
            
            // KeyPanelService 재생성
            _panelService = new KeyPanelService(this, _defaultColor,
                Panel_MouseDown, Panel_MouseMove, Panel_MouseUp,
                _contextMenuStrip, _obsCompatibilityMode);

            // 🔥 이벤트 리스너 재설정
            if (!_obsCompatibilityMode)
            {
                this.LocationChanged += SyncLayeredWindows;
                this.SizeChanged += SyncLayeredWindows;
                this.VisibleChanged += SyncLayeredWindowsVisibility;
            }

            // 패널 복원
            foreach (var state in panelStates)
            {
                var kp = _panelService.AddKeyPanel(state.Key, state.Down, state.Up, state.Loc, state.Size);
                kp.DisplayName = state.DisplayName;
                kp.BorderEnabled = state.BorderEnabled;
                kp.BorderColor = state.BorderColor;
                kp.BorderWidth = state.BorderWidth;
                kp.CornerRadius = state.CornerRadius;
                kp.Panel.ContextMenuStrip = _contextMenuStrip;
                kp.UpdateVisual();

                if (!_obsCompatibilityMode)
                {
                    var screenLoc = this.PointToScreen(kp.Panel.Location);
                    kp.UpdatePosition(screenLoc);
                    kp.Show();
                }
            }

            MessageBox.Show(this, 
                $"모드가 {(obsMode ? "OBS 호환" : "레이어드 윈도우")}로 변경되었습니다.", 
                "모드 변경 완료", 
                MessageBoxButtons.OK, 
                MessageBoxIcon.Information);
        }

        // 헬퍼 메서드 추가
        private string GetChromaKeyName(Color color)
        {
            if (color.R == 255 && color.G == 0 && color.B == 255) return "Magenta/Pink";
            if (color.R == 0 && color.G == 255 && color.B == 0) return "Green";
            if (color.R == 0 && color.G == 0 && color.B == 255) return "Blue";
            if (color.R == 0 && color.G == 0 && color.B == 0) return "Black";
            return "Custom";
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SYSCOMMAND && !_obsCompatibilityMode)
            {
                int command = m.WParam.ToInt32() & 0xFFF0;
                
                if (command == SC_MINIMIZE)
                {
                    _isMinimizing = true;
                    
                    if (_alwaysOnTop)
                    {
                        // 🔥 Always on Top: 버튼은 그대로 유지
                        base.WndProc(ref m);
                    }
                    else
                    {
                        // 일반 모드: 버튼도 숨김
                        foreach (var kp in _keyPanels)
                            kp.Hide();
                        base.WndProc(ref m);
                    }
                    return;
                }
                
                if (command == SC_RESTORE)
                {
                    base.WndProc(ref m);
                    _isMinimizing = false;
                    foreach (var kp in _keyPanels)
                    {
                        kp.Show();
                        kp.BringToFront();
                    }
                    SyncLayeredWindows();
                    return;
                }
            }
            
            base.WndProc(ref m);
        }
    }
}
