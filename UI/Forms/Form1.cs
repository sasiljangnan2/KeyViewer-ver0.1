using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using keyviewer.UI.Controls;
using keyviewer.UI.Editors;
using keyviewer.Services;
using keyviewer.Models;

namespace keyviewer.UI.Forms
{
    /// <summary>
    /// 메인 폼 - 키 패널 관리 및 글로벌 단축키 후킹을 담당합니다.
    /// </summary>
    public partial class Form1 : Form
    {
        #region UI 컨트롤 및 메뉴

        /// <summary>우클릭 컨텍스트 메뉴</summary>
        private ContextMenuStrip _contextMenuStrip = null!;
        
        /// <summary>미리 정의된 색상 배열</summary>
        private readonly Color[] _colors = { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Purple, Color.Orange };
        private int _colorIndex = 0;
        
        /// <summary>기본 색상 (어두운 회색)</summary>
        private readonly Color _defaultColor = SystemColors.ControlDark;

        #endregion

        #region 레이아웃 관련 필드

        /// <summary>레이아웃 선택 콤보박스</summary>
        private ComboBox _cbLayouts = null!;
        private Button _btnApplyLayout = null!;
        private Button _btnSaveLayout = null!;
        private Button _btnDeleteLayout = null!;
        
        /// <summary>레이아웃 저장 디렉토리 경로</summary>
        private readonly string _layoutsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "layouts");

        #endregion

        #region 드래그 관련 필드

        /// <summary>드래그 상태 플래그</summary>
        private bool _dragging = true;
        private Point _dragStartMouse;
        private Point _dragStartLocation;
        private Control? _draggedControl;

        #endregion

        #region 글로벌 키보드 후킹 관련

        /// <summary>저수준 키보드 훅 콜백 대리자</summary>
        private LowLevelKeyboardProc _proc;
        
        /// <summary>설치된 훅의 ID</summary>
        private IntPtr _hookID = IntPtr.Zero;

        // Windows 메시지 상수들
        private const int WH_KEYBOARD_LL = 13;      // 저수준 키보드 훅
        private const int WM_KEYDOWN = 0x0100;      // 키 눌림
        private const int WM_KEYUP = 0x0101;        // 키 떨어짐
        private const int WM_SYSKEYDOWN = 0x0104;   // 시스템 키 눌림 (Alt 등)
        private const int WM_SYSKEYUP = 0x0105;     // 시스템 키 떨어짐
        private const int WM_SYSCOMMAND = 0x0112;   // 시스템 명령
        private const int SC_MINIMIZE = 0xF020;     // 최소화 명령
        private const int SC_RESTORE = 0xF120;      // 복원 명령
        
        /// <summary>저수준 키보드 훅 콜백 함수 시그니처</summary>
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        #endregion

        #region 키 패널 관리 서비스

        /// <summary>키 패널을 생성하고 관리하는 서비스</summary>
        private KeyPanelService _panelService = null!;
        
        /// <summary>현재 관리 중인 모든 키 패널 목록</summary>
        private List<KeyPanel> _keyPanels => _panelService.KeyPanels;

        /// <summary>디자이너에서 생성한 초기 패널의 개수</summary>
        private int _initialWrappedCount = 0;

        #endregion

        #region 배경 및 투명화 관련

        /// <summary>현재 배경 이미지 경로</summary>
        private string? _currentBgImagePath = null;
        
        /// <summary>현재 배경 색상 (투명화 OFF 시 사용)</summary>
        private Color _currentBgColor;
        
        /// <summary>배경 투명화 활성화 여부</summary>
        private bool _backgroundTransparent = false;
        
        /// <summary>투명화 키로 사용할 색상 (현재 미사용)</summary>
        private readonly Color _transparencyKeyColor = Color.Magenta;

        /// <summary>크로마키 (투명화) 색상 - OBS에서 제거할 색상</summary>
        private Color _chromaKeyColor = Color.FromArgb(255, 0, 255);

        #endregion

        #region P/Invoke - Windows API 호출

        /// <summary>저수준 키보드 훅을 설치합니다.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        /// <summary>설치된 훅을 제거합니다.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        /// <summary>다음 훅 프로시저로 메시지를 전달합니다.</summary>
        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>현재 프로세스의 모듈 핸들을 가져옵니다.</summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion

        #region 모드 및 상태

        /// <summary>OBS 호환 모드 활성화 여부</summary>
        private bool _obsCompatibilityMode = false;
        
        /// <summary>창 최소화 중 플래그</summary>
        private bool _isMinimizing = false;
        
        /// <summary>항상 위에 유지 모드</summary>
        private bool _alwaysOnTop = false;

        /// <summary>바 모드 윈도우</summary>
        private BarModeForm? _barModeForm = null;

        #endregion

        /// <summary>
        /// 폼 초기화 및 구성 요소 설정
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            _obsCompatibilityMode = false;
            _currentBgColor = this.BackColor;
            
            // 아이콘 설정 - Resources 폴더에서 icon.ico 로드
            try
            {
                string iconPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Resources", "icon_dot.ico"
                );
                
                if (File.Exists(iconPath))
                {
                    this.Icon = new System.Drawing.Icon(iconPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"아이콘 로드 실패: {ex.Message}");
            }

            // 레이아웃 디렉토리 생성
            Directory.CreateDirectory(_layoutsDir);
            
            // 컨텍스트 메뉴 초기화
            InitializeContextMenu();

            // 키 패널 서비스 생성
            _panelService = new KeyPanelService(this, _defaultColor,
                Panel_MouseDown, Panel_MouseMove, Panel_MouseUp,
                _contextMenuStrip, _obsCompatibilityMode);

            // 초기 패널 수 저장 (런타임 추가 패널과 구분)
            _initialWrappedCount = _keyPanels.Count;

            // 글로벌 키보드 훅 설치
            _proc = HookCallback;
            _hookID = InstallHook(_proc);

            this.ContextMenuStrip = _contextMenuStrip;

            // 이벤트 핸들러 설정
            SetupEventHandlers();

            // 초기 패널이 없으면 샘플 레이아웃 로드
            if (_keyPanels.Count == 0)
            {
                var defaultLayout = LayoutManager.CreateSampleLayout("기본값");
                var created = LayoutManager.ApplyLayout(defaultLayout, _panelService);
                
                foreach (var kp in created)
                {
                    kp.UpdateVisual();
                    kp.Show();
                }
            }

            this.Shown += OnFormShown;
        }

        /// <summary>
        /// 폼의 이벤트 핸들러를 설정합니다.
        /// 레이어드 윈도우 모드에서 위치 동기화가 필요합니다.
        /// </summary>
        private void SetupEventHandlers()
        {
            if (!_obsCompatibilityMode)
            {
                // 폼 위치 변경 시 키 패널 위치 업데이트
                this.LocationChanged += SyncLayeredWindows;
                // 폼 크기 변경 시 키 패널 위치 업데이트
                this.SizeChanged += SyncLayeredWindows;
                // 폼 표시/숨김 시 키 패널 표시/숨김
                this.VisibleChanged += SyncLayeredWindowsVisibility;

                this.Load += (s, e) =>
                {
                    SyncLayeredWindows();
                    SyncLayeredWindowsVisibility();
                };
            }
        }

        /// <summary>
        /// 폼이 표시된 후 키 패널을 보이게 합니다.
        /// </summary>
        private void OnFormShown(object? sender, EventArgs e)
        {
            if (_obsCompatibilityMode) return;
            
            // 500ms 지연 후 키 패널 표시 (초기화 완료 대기)
            System.Threading.Tasks.Task.Delay(500).ContinueWith(_ =>
            {
                this.BeginInvoke(new Action(() =>
                {
                    foreach (var kp in _keyPanels)
                    {
                        kp.Show();
                        kp.BringToFront();
                        kp.UpdateVisual();
                    }
                }));
            });
        }

        /// <summary>
        /// 폼의 위치가 변경될 때 레이어드 윈도우 위치를 동기화합니다.
        /// </summary>
        private void SyncLayeredWindows(object? sender, EventArgs e)
        {
            // 최소화되었거나 최소화 중인 경우 제외
            if (WindowState == FormWindowState.Minimized || _isMinimizing) return;
            // 폼이 화면 밖에 있으면 제외 (최소화 좌표 필터링)
            if (this.Left < -10000 || this.Top < -10000) return;
    
            // 각 키 패널의 위치를 화면 좌표로 변환 및 업데이트
            foreach (var kp in _keyPanels)
            {
                var screenLoc = this.PointToScreen(kp.Panel.Location);
                kp.UpdatePosition(screenLoc);
            }
        }

        /// <summary>파라미터 없는 오버로드 - 즉시 동기화</summary>
        private void SyncLayeredWindows()
        {
            SyncLayeredWindows(null, EventArgs.Empty);
        }

        /// <summary>
        /// 폼의 표시/숨김 상태가 변경될 때 키 패널의 표시/숨김을 동기화합니다.
        /// </summary>
        private void SyncLayeredWindowsVisibility(object? sender, EventArgs e)
        {
            // 최소화되었거나 최소화 중인 경우 제외
            if (WindowState == FormWindowState.Minimized || _isMinimizing) return;
    
            // 폼이 보이면 키 패널도 표시, 숨겨지면 숨김
            foreach (var kp in _keyPanels)
            {
                if (this.Visible)
                    kp.Show();
                else
                    kp.Hide();
            }
        }

        /// <summary>파라미터 없는 오버로드</summary>
        private void SyncLayeredWindowsVisibility()
        {
            SyncLayeredWindowsVisibility(null, EventArgs.Empty);
        }

        /// <summary>
        /// 레이아웃 디렉토리에서 사용 가능한 레이아웃 목록을 새로고칩니다.
        /// </summary>
        private void RefreshLayoutList()
        {
            if (_cbLayouts == null) return;
            
            _cbLayouts.Items.Clear();
            // layouts 폴더의 모든 JSON 파일을 목록에 추가
            foreach (var file in Directory.GetFiles(_layoutsDir, "*.json"))
            {
                _cbLayouts.Items.Add(Path.GetFileNameWithoutExtension(file));
            }
            if (_cbLayouts.Items.Count > 0)
                _cbLayouts.SelectedIndex = 0;
        }

        /// <summary>
        /// 현재 키 패널 구성을 레이아웃 파일로 저장합니다.
        /// </summary>
        private void SaveCurrentLayout()
        {
            using var dlg = new SaveFileDialog
            {
                InitialDirectory = _layoutsDir,
                Filter = "JSON 파일 (*.json)|*.json",
                DefaultExt = "json",
                FileName = "layout"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                // 현재 폼 상태를 KeyLayout 객체로 생성
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
                    OBSCompatibilityMode = _obsCompatibilityMode
                };
        
                // 각 키 패널의 설정을 레이아웃에 추가
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

                // JSON 파일로 저장
                LayoutManager.SaveLayout(dlg.FileName, layout);
                RefreshLayoutList();
        
                MessageBox.Show(this, $"레이아웃이 저장되었습니다.\n{dlg.FileName}", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, 
                    $"레이아웃 저장 실패:\n\n{ex.Message}", 
                    "오류", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 런타임에 추가된 패널만 제거합니다. (디자이너 패널은 유지)
        /// </summary>
        private void RemoveRuntimePanels()
        {
            var runtime = new List<KeyPanel>();
            // 초기 패널 개수 이후의 패널만 선택
            for (int i = _initialWrappedCount; i < _keyPanels.Count; i++)
            {
                runtime.Add(_keyPanels[i]);
            }

            // 선택된 패널 제거
            foreach (var kp in runtime)
            {
                kp.Dispose();
                if (kp.Panel.Parent != null)
                    Controls.Remove(kp.Panel);
                _keyPanels.Remove(kp);
            }
        }

        /// <summary>
        /// 폼이 닫힐 때 리소스를 정리합니다.
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // 바 모드 창 닫기
            if (_barModeForm != null && !_barModeForm.IsDisposed)
            {
                _barModeForm.Close();
                _barModeForm.Dispose();
            }

            // 글로벌 키보드 훅 제거
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

        /// <summary>
        /// 글로벌 키보드 훅을 설치합니다.
        /// </summary>
        private IntPtr InstallHook(LowLevelKeyboardProc proc)
        {
            using Process curProcess = Process.GetCurrentProcess();
            using ProcessModule? curModule = curProcess.MainModule;
            IntPtr moduleHandle = curModule != null ? GetModuleHandle(curModule.ModuleName) : IntPtr.Zero;
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, moduleHandle, 0);
        }

        /// <summary>
        /// 글로벌 키보드 훅 콜백 - 모든 키 입력을 감지합니다.
        /// </summary>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    int vk = Marshal.ReadInt32(lParam);
                    Keys key = (Keys)vk;
                    // UI 스레드에서 처리하기 위해 BeginInvoke 사용
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

        /// <summary>
        /// 전역 키 입력 감지 - 모든 패널에 전달합니다.
        /// </summary>
        private void HandleGlobalKeyDown(Keys key)
        {
            foreach (var kp in _keyPanels)
            {
                kp.HandleKeyDown(key);
            }
        }

        /// <summary>
        /// 전역 키 떨어짐 감지 - 모든 패널에 전달합니다.
        /// </summary>
        private void HandleGlobalKeyUp(Keys key)
        {
            foreach (var kp in _keyPanels)
            {
                kp.HandleKeyUp(key);
            }
        }

        /// <summary>더미 패널용 마우스 이벤트 핸들러 (사용 안 함)</summary>
        private void Panel_MouseDown(object? sender, MouseEventArgs e) { }
        private void Panel_MouseMove(object? sender, MouseEventArgs e) { }
        private void Panel_MouseUp(object? sender, MouseEventArgs e) { }

        /// <summary>
        /// 배경색에 따라 대비되는 텍스트 색을 반환합니다.
        /// </summary>
        private Color GetContrastColor(Color bg)
        {
            // YIQ 색상 공간 사용 - 밝은 배경은 검정, 어두운 배경은 흰색
            int yiq = ((bg.R * 299) + (bg.G * 587) + (bg.B * 114)) / 1000;
            return yiq >= 128 ? Color.Black : Color.White;
        }

        /// <summary>
        /// 우클릭 컨텍스트 메뉴를 초기화합니다.
        /// </summary>
        private void InitializeContextMenu()
        {
            _contextMenuStrip = new ContextMenuStrip();

            // [항상 위에 유지] 메뉴 항목
            var toggleTopMost = new ToolStripMenuItem("항상 위에 유지");
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

            // [OBS 호환 모드] 메뉴 항목 - OBS와 레이어드 윈도우 모드 전환
            var toggleOBSMode = new ToolStripMenuItem("OBS 호환 모드");
            toggleOBSMode.CheckOnClick = true;
            toggleOBSMode.Checked = _obsCompatibilityMode;
            toggleOBSMode.Click += (s, e) =>
            {
                bool newMode = toggleOBSMode.Checked;
                if (newMode == _obsCompatibilityMode) return;
                
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
                    toggleOBSMode.Checked = _obsCompatibilityMode;
                }
            };

            // [레이아웃...] 메뉴 항목 - 저장된 레이아웃 로드
            var layoutsItem = new ToolStripMenuItem("레이아웃...");
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
                    
                    // 로드된 레이아웃의 모드가 다르면 먼저 모드 전환
                    if (layout.OBSCompatibilityMode != _obsCompatibilityMode)
                    {
                        foreach (var kp in _keyPanels.ToList())
                        {
                            kp.Dispose();
                            if (kp.Panel.Parent != null)
                                Controls.Remove(kp.Panel);
                        }
                        _keyPanels.Clear();
                        
                        if (!_obsCompatibilityMode)
                        {
                            this.LocationChanged -= SyncLayeredWindows;
                            this.SizeChanged -= SyncLayeredWindows;
                            this.VisibleChanged -= SyncLayeredWindowsVisibility;
                        }
                        
                        _obsCompatibilityMode = layout.OBSCompatibilityMode;
                        
                        _panelService = new KeyPanelService(this, _defaultColor,
                            Panel_MouseDown, Panel_MouseMove, Panel_MouseUp,
                            _contextMenuStrip, _obsCompatibilityMode);
                        
                        if (!_obsCompatibilityMode)
                        {
                            this.LocationChanged += SyncLayeredWindows;
                            this.SizeChanged += SyncLayeredWindows;
                            this.VisibleChanged += SyncLayeredWindowsVisibility;
                        }
                    }
                    else
                    {
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
        
                    // 투명화 상태에 따라 배경 적용
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
                    
                    // 레이아웃의 키 패널 생성
                    var created = LayoutManager.ApplyLayout(layout, _panelService);
                    foreach (var kp in created)
                    {
                        kp.Panel.ContextMenuStrip = _contextMenuStrip;
                        kp.UpdateVisual();

                        if (!_obsCompatibilityMode)
                        {
                            var screenLoc = this.PointToScreen(kp.Panel.Location);
                            kp.UpdatePosition(screenLoc);
                            kp.Show();
                        }
                        else
                        {
                            kp.Panel.Visible = true;
                            kp.Panel.BringToFront();
                        }
                    }
                }
            };

            // [레이아웃 저장...] 메뉴 항목
            var saveLayoutItem = new ToolStripMenuItem("레이아웃 저장...");
            saveLayoutItem.Click += (s, e) => SaveCurrentLayout();

            // [키 패널 추가] 메뉴 항목
            var addPanelItem = new ToolStripMenuItem("키 패널 추가");
            addPanelItem.Click += (s, e) =>
            {
                // 마우스 위치를 폼 클라이언트 좌표로 변환
                Point loc = PointToClient(Cursor.Position);
                // 10px 그리드에 맞춤
                loc.X -= loc.X % 10;
                loc.Y -= loc.Y % 10;

                using var editor = new PanelEditorForm();
                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    var up = editor.SelectedUpColor;
                    var down = editor.SelectedDownColor;
                    var size = editor.SelectedSize;
                    var displayName = editor.SelectedDisplayName;

                    // 폼 경계 내로 위치 제한
                    loc.X = Math.Clamp(loc.X, 0, Math.Max(0, ClientSize.Width - size.Width));
                    loc.Y = Math.Clamp(loc.Y, 0, Math.Max(0, ClientSize.Height - size.Height));

                    var kp = _panelService.AddKeyPanel(editor.SelectedKey, down, up, loc, size);
                    
                    if (!string.IsNullOrEmpty(displayName))
                    {
                        kp.DisplayName = displayName;
                        kp.UpdateVisual();
                    }
                    
                    kp.BringToFront();
                }
            };

            // [패널 편집...] 메뉴 항목 - 우클릭된 패널 편집 (동적으로 추가)
            var editPanelItem = new ToolStripMenuItem("패널 편집...");
            editPanelItem.Click += (s, e) =>
            {
                var kp = GetKeyPanelFromContext();
                if (kp == null) return;
                OpenPanelEditor(kp);
            };

            // [패널 삭제] 메뉴 항목 - 우클릭된 패널 삭제 (동적으로 추가)
            var deletePanelItem = new ToolStripMenuItem("패널 삭제");
            deletePanelItem.Click += (s, e) =>
            {
                var kp = GetKeyPanelFromContext();
                if (kp == null) return;

                var result = MessageBox.Show(this,
                    $"패널 '{kp.Key}'을(를) 삭제하시겠습니까?",
                    "삭제 확인",
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

            // [전역 설정...] 메뉴 항목 - 모든 패널의 색상, 투명도, 배경 설정
            var globalSettingsItem = new ToolStripMenuItem("전역 설정...");
            globalSettingsItem.Click += (s, e) =>
            {
                // 첫 번째 패널의 설정을 초기값으로 사용
                Color upInit = _keyPanels.Count > 0 ? _keyPanels[0].UpColor : _defaultColor;
                Color downInit = _keyPanels.Count > 0 ? _keyPanels[0].DownColor : Color.Red;
                Color bgInit = _currentBgColor;
                string? currentBgPath = _currentBgImagePath;
                int keyAlphaInit = _keyPanels.Count > 0 ? _keyPanels[0].UpColor.A : 255;
                int opacityPercent = (int)(this.Opacity * 100);
                
                bool borderEnabledInit = _keyPanels.Count > 0 ? _keyPanels[0].BorderEnabled : false;
                Color borderColorInit = _keyPanels.Count > 0 ? _keyPanels[0].BorderColor : Color.Black;
                int borderWidthInit = _keyPanels.Count > 0 ? _keyPanels[0].BorderWidth : 2;
                int cornerRadiusInit = _keyPanels.Count > 0 ? _keyPanels[0].CornerRadius : 0;

                // 전역 설정 대화 창 열기
                using var dlg = new GlobalEditorForm(upInit, downInit, bgInit, currentBgPath,
                    keyAlphaInit, opacityPercent, _backgroundTransparent, _chromaKeyColor,
                    borderEnabledInit, borderColorInit, borderWidthInit, cornerRadiusInit);

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _chromaKeyColor = dlg.ChromaKeyColor;
                    int finalAlpha = dlg.SelectedKeyAlpha;

                    // 모든 패널에 설정 적용
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

                    // 배경 색상 저장 및 적용
                    this.BackColor = dlg.SelectedBgColor;
                    _currentBgColor = dlg.SelectedBgColor;
                    _currentBgImagePath = dlg.SelectedBgImagePath;
                    
                    // 투명화 상태 변경
                    bool wasPreviouslyTransparent = _backgroundTransparent;
                    _backgroundTransparent = dlg.BackgroundTransparent;

                    if (_backgroundTransparent)
                    {
                        // 투명화 활성화 - 크로마키 색상을 배경으로 설정
                        this.BackgroundImage?.Dispose();
                        this.BackgroundImage = null;
                        this.BackColor = _chromaKeyColor;
                        this.TransparencyKey = _chromaKeyColor;

                        if (_obsCompatibilityMode)
                        {
                            // OBS 사용자를 위한 설정 안내
                            MessageBox.Show(this,
                                $"OBS 설정 방법:\n\n" +
                                $"1. 'Window Capture' 소스 추가\n" +
                                $"2. Capture Method: 'Windows 10 (1903+)'\n" +
                                $"3. 'Capture Client Area' 체크\n" +
                                $"4. (선택) 'Chroma Key' 필터 추가\n" +
                                $"5. 색상 선택: {GetChromaKeyName(_chromaKeyColor)}\n\n" +
                                $"또는 'Game Capture'에서 'Allow Transparency' 체크",
                                "OBS 투명화 설정",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        // 투명화 해제 - 이전 배경 복원
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
                            MessageBox.Show(this, $"이미지 적용 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }

                    // 창의 투명도 설정
                    this.Opacity = dlg.SelectedOpacityPercent / 100.0;

                    // 반투명 사용자를 위한 OBS 팁
                    if (dlg.SelectedOpacityPercent < 100 || dlg.SelectedKeyAlpha < 255)
                    {
                        if (_obsCompatibilityMode && !_backgroundTransparent)
                        {
                            MessageBox.Show(this,
                                "OBS에서 반투명을 캡처하려면:\n\n" +
                                "1. Window Capture → Capture Method를\n" +
                                "   'Windows 10 (1903+)'로 변경\n\n" +
                                "   (Allow Transparency 체크)",
                                "OBS 반투명 팁",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    }
                }
            };
             
            // [디버그: 윈도우 정보 표시] 메뉴 항목
            var debugItem = new ToolStripMenuItem("디버그: 윈도우 정보 표시");
            debugItem.Click += (s, e) =>
            {
                var info = $"모드: {(_obsCompatibilityMode ? "OBS" : "레이어드")}\n" +
                           $"키 패널: {_keyPanels.Count}개\n" +
                           $"폼: {this.Location}, 크기: {this.Size}\n\n";

                foreach (var kp in _keyPanels)
                {
                    info += $"패널 {kp.Key}:\n";
                    info += $"  패널 위치: {kp.Panel.Location}\n";
                    if (kp.LayeredWindow != null && !kp.LayeredWindow.IsDisposed)
                    {
                        info += $"  레이어드 위치: {kp.LayeredWindow.Location}\n";
                        info += $"  레이어드 크기: {kp.LayeredWindow.Size}\n";
                        info += $"  표시됨: {kp.LayeredWindow.Visible}\n";
                    }
                    else
                    {
                        info += "  레이어드 윈도우: NULL 또는 해제됨\n";
                    }
                    info += "\n";
                }

                MessageBox.Show(this, info, "디버그 정보");
            };

            // [종료] 메뉴 항목
            var exitItem = new ToolStripMenuItem("종료");
            exitItem.Click += (s, e) => this.Close();

            // [바 모드] 메뉴 항목 - 별도 창에서 키 입력을 바 형식으로 표시
            var barModeItem = new ToolStripMenuItem("바 모드");
            barModeItem.Click += (s, e) =>
            {
                if (_barModeForm == null || _barModeForm.IsDisposed)
                {
                    _barModeForm = new BarModeForm(_keyPanels);
                    _barModeForm.Show();
                }
                else
                {
                    _barModeForm.Focus();
                    _barModeForm.BringToFront();
                }
            };

            // 메뉴 항목 추가
            _contextMenuStrip.Items.Add(toggleTopMost);
            _contextMenuStrip.Items.Add(toggleOBSMode);
            _contextMenuStrip.Items.Add(new ToolStripSeparator());
            _contextMenuStrip.Items.Add(barModeItem);
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

            // 메뉴가 열릴 때 패널별 메뉴 항목 동적 추가/제거
            _contextMenuStrip.Opening += (s, e) =>
            {
                _contextMenuStrip.Items.Remove(editPanelItem);
                _contextMenuStrip.Items.Remove(deletePanelItem);

                // 우클릭된 컨트롤이 패널인 경우에만 편집/삭제 메뉴 추가
                var kp = GetKeyPanelFromContext();
                if (kp != null)
                {
                    int insertIndex = _contextMenuStrip.Items.IndexOf(addPanelItem) + 1;
                    
                    _contextMenuStrip.Items.Insert(insertIndex, editPanelItem);
                    _contextMenuStrip.Items.Insert(insertIndex + 1, deletePanelItem);
                }
            };
        }

        /// <summary>
        /// 우클릭 컨텍스트로부터 대상 키 패널을 찾습니다.
        /// </summary>
        private KeyPanel? GetKeyPanelFromContext()
        {
            Control? src = _contextMenuStrip?.SourceControl;

            // 레이어드 윈도우에서 우클릭한 경우
            if (src is Form layered)
            {
                return _keyPanels.FirstOrDefault(kp => kp.LayeredWindow == layered);
            }

            // 폼의 패널에서 우클릭한 경우
            if (src == null) return null;
            Control? cur = src;
            while (cur != null && !(cur is Panel))
                cur = cur.Parent;
            if (cur == null) return null;

            var panel = cur as Panel;
            return _keyPanels.FirstOrDefault(kp => kp.Panel == panel);
        }

        /// <summary>
        /// 키 패널 편집 대화 창을 엽니다.
        /// </summary>
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
                kp.CornerRadius);
    
            if (editor.ShowDialog(this) == DialogResult.OK)
            {
                kp.Key = editor.SelectedKey;
                kp.UpColor = editor.SelectedUpColor;
                kp.DownColor = editor.SelectedDownColor;
                kp.DisplayName = editor.SelectedDisplayName;
                kp.Panel.BackColor = kp.UpColor;

                kp.BorderEnabled = editor.BorderEnabled;
                kp.BorderColor = editor.BorderColor;
                kp.BorderWidth = editor.BorderWidth;
                
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

        /// <summary>
        /// OBS 호환 모드와 레이어드 윈도우 모드 간 전환합니다.
        /// </summary>
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

            // 이전 모드의 이벤트 핸들러 제거
            if (!_obsCompatibilityMode)
            {
                this.LocationChanged -= SyncLayeredWindows;
                this.SizeChanged -= SyncLayeredWindows;
                this.VisibleChanged -= SyncLayeredWindowsVisibility;
            }

            // 모드 변경
            _obsCompatibilityMode = obsMode;
            
            // 새로운 서비스 생성
            _panelService = new KeyPanelService(this, _defaultColor,
                Panel_MouseDown, Panel_MouseMove, Panel_MouseUp,
                _contextMenuStrip, _obsCompatibilityMode);

            // 새 모드의 이벤트 핸들러 추가
            if (!_obsCompatibilityMode)
            {
                this.LocationChanged += SyncLayeredWindows;
                this.SizeChanged += SyncLayeredWindows;
                this.VisibleChanged += SyncLayeredWindowsVisibility;
            }

            // 저장된 패널 상태 복원
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

        /// <summary>
        /// 크로마키 색상의 이름을 반환합니다.
        /// </summary>
        private string GetChromaKeyName(Color color)
        {
            if (color.R == 255 && color.G == 0 && color.B == 255) return "자홍색/핑크";
            if (color.R == 0 && color.G == 255 && color.B == 0) return "초록색";
            if (color.R == 0 && color.G == 0 && color.B == 255) return "파란색";
            if (color.R == 0 && color.G == 0 && color.B == 0) return "검정색";
            return "사용자 정의";
        }

        /// <summary>
        /// Windows 메시지 처리 - 최소화/복원 명령 처리
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SYSCOMMAND && !_obsCompatibilityMode)
            {
                int command = m.WParam.ToInt32() & 0xFFF0;
                
                // 최소화 명령 처리
                if (command == SC_MINIMIZE)
                {
                    _isMinimizing = true;
                    
                    if (_alwaysOnTop)
                    {
                        // 항상 위에 유지: 패널 유지
                        base.WndProc(ref m);
                    }
                    else
                    {
                        // 일반 모드: 패널 숨김
                        foreach (var kp in _keyPanels)
                            kp.Hide();
                        base.WndProc(ref m);
                    }
                    return;
                }
                
                // 복원 명령 처리
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
