using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace keyviewer
{
    public class KeyPanel
    {
        // 레이어드 윈도우 관련 P/Invoke
        [StructLayout(LayoutKind.Sequential)]
        private struct Point32 { public int x; public int y; public Point32(int x, int y) { this.x = x; this.y = y; } }

        [StructLayout(LayoutKind.Sequential)]
        private struct Size32 { public int cx; public int cy; public Size32(int cx, int cy) { this.cx = cx; this.cy = cy; } }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BLENDFUNCTION
        {
            public byte BlendOp, BlendFlags, SourceConstantAlpha, AlphaFormat;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public System.Drawing.Point ptReserved;
            public System.Drawing.Point ptMaxSize;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Point ptMinTrackSize;
            public System.Drawing.Point ptMaxTrackSize;
        }

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TOOLWINDOW = 0x80;
        private const byte AC_SRC_OVER = 0x00;
        private const byte AC_SRC_ALPHA = 0x01;
        private const int ULW_ALPHA = 0x00000002;
        private const int WM_GETMINMAXINFO = 0x0024;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOZORDER = 0x0004;
        private const int SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref Point32 pptDst, ref Size32 psize,
            IntPtr hdcSrc, ref Point32 pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);
        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        public Panel Panel { get; }
        private LayeredForm? _layeredWindow;
        
        // PUBLIC 속성으로 노출 - KeyPanelService에서 접근 가능하도록
        public Form? LayeredWindow => _layeredWindow;
        
        public Keys Key { get; set; }
        public Color DownColor { get; set; }
        public Color UpColor { get; set; }

        private bool _isKeyDown = false;
        private bool _obsCompatibilityMode = false; // OBS 호환 모드

        public KeyPanel(Panel panel, Keys key, Color downColor, Color upColor, bool obsCompatibilityMode = false)
        {
            Panel = panel ?? throw new System.ArgumentNullException(nameof(panel));
            Key = key;
            DownColor = downColor;
            UpColor = upColor;
            _obsCompatibilityMode = obsCompatibilityMode;

            if (_obsCompatibilityMode)
            {
                // OBS 호환 모드: 일반 패널 사용
                Panel.Visible = true;
                Panel.BackColor = upColor;
            }
            else
            {
                // 기본 모드: 레이어드 윈도우 사용
                Panel.Visible = false;
                CreateLayeredWindow();
            }
        }

        // 최소 크기 제약을 우회하는 커스텀 Form 클래스
        private class LayeredForm : Form
        {
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_GETMINMAXINFO)
                {
                    // 최소/최대 크기 제약 제거
                    MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(m.LParam, typeof(MINMAXINFO))!;
                    mmi.ptMinTrackSize = new System.Drawing.Point(1, 1); // 최소 1x1
                    mmi.ptMaxTrackSize = new System.Drawing.Point(10000, 10000); // 최대 크기
                    Marshal.StructureToPtr(mmi, m.LParam, true);
                }
                base.WndProc(ref m);
            }
        }

        private void CreateLayeredWindow()
        {
            if (_layeredWindow != null && !_layeredWindow.IsDisposed) return;

            // 초기 위치 계산 (화면 좌표)
            Point initialLocation = Panel.Parent != null 
                ? Panel.Parent.PointToScreen(Panel.Location) 
                : new Point(100, 100); // 기본 위치

            _layeredWindow = new LayeredForm
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Location = initialLocation,
                Size = Panel.Size, // 크기 먼저 설정
                MinimumSize = new Size(0, 0),
                MaximumSize = new Size(0, 0),
                AutoSize = false,
                Owner = Panel.Parent as Form,
                TopMost = true // 🆕 기본적으로 최상위 표시
            };

            _layeredWindow.Load += (_, __) =>
            {
                int ex = GetWindowLong(_layeredWindow.Handle, GWL_EXSTYLE);
                ex |= WS_EX_LAYERED | WS_EX_TOOLWINDOW;
                SetWindowLong(_layeredWindow.Handle, GWL_EXSTYLE, ex);
                
                SetWindowPos(_layeredWindow.Handle, IntPtr.Zero, 
                    _layeredWindow.Left, _layeredWindow.Top, 
                    Panel.Size.Width, Panel.Size.Height, 
                    SWP_NOZORDER | SWP_NOACTIVATE);
            };

            _layeredWindow.Show();
            
            // Show 후 크기 재설정
            _layeredWindow.Size = Panel.Size;
            SetWindowPos(_layeredWindow.Handle, IntPtr.Zero, 
                _layeredWindow.Left, _layeredWindow.Top, 
                Panel.Size.Width, Panel.Size.Height, 
                SWP_NOZORDER | SWP_NOACTIVATE);
            
            UpdateVisual();
            
            // 🆕 디버깅 로그
            System.Diagnostics.Debug.WriteLine(
                $"LayeredWindow created: Pos={_layeredWindow.Location}, Size={_layeredWindow.Size}");
        }

        public void UpdatePosition(Point screenLocation)
        {
            if (_obsCompatibilityMode)
            {
                if (Panel.Parent != null)
                    Panel.Location = Panel.Parent.PointToClient(screenLocation);
            }
            else if (_layeredWindow != null && !_layeredWindow.IsDisposed)
            {
                _layeredWindow.Location = screenLocation;
            }
        }

        public void UpdateSize(Size size)
        {
            Panel.Size = size;
            
            if (!_obsCompatibilityMode && _layeredWindow != null && !_layeredWindow.IsDisposed)
            {
                // SetWindowPos로 크기 강제 설정
                SetWindowPos(_layeredWindow.Handle, IntPtr.Zero, 
                    _layeredWindow.Left, _layeredWindow.Top, 
                    size.Width, size.Height, 
                    SWP_NOZORDER | SWP_NOACTIVATE);
                _layeredWindow.Size = size;
                UpdateVisual();
            }
        }

        public void HandleKeyDown(Keys key)
        {
            if (key == Key && !_isKeyDown)
            {
                _isKeyDown = true;
                Panel.BackColor = DownColor;
                UpdateVisual();
            }
        }

        public void HandleKeyUp(Keys key)
        {
            if (key == Key && _isKeyDown)
            {
                _isKeyDown = false;
                Panel.BackColor = UpColor;
                UpdateVisual();
            }
        }

        // 레이어드 윈도우를 현재 상태(색상, 키 텍스트)로 업데이트
        public void UpdateVisual()
        {
            // 변수를 메서드 최상단에서 한 번만 선언
            Color currentColor = _isKeyDown ? DownColor : UpColor;
            
            if (_obsCompatibilityMode)
            {
                // OBS 모드: 패널 직접 업데이트
                Panel.BackColor = currentColor;
                Panel.Invalidate();
                return;
            }

            // 레이어드 윈도우 모드: 기존 로직
            if (_layeredWindow == null || _layeredWindow.IsDisposed) return;

            int w = Math.Max(1, _layeredWindow.Width);
            int h = Math.Max(1, _layeredWindow.Height);

            using var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);

                // 배경 색 (알파 포함)
                using (var bgBrush = new SolidBrush(currentColor))
                {
                    g.FillRectangle(bgBrush, 0, 0, w, h);
                }

                // 텍스트 그리기 - OBS 모드와 동일한 로직 사용
                string keyText = GetKeyDisplayName(Key);
                var rect = new Rectangle(0, 0, w, h);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                
                // OBS 모드와 동일한 폰트 크기 계산
                int fontSize = Math.Max(8, Math.Min(w, h) / 3);
                using var font = new Font("Arial", fontSize, FontStyle.Bold);
                
                // 대비 색 (불투명)
                Color textColor = GetContrastColor(Color.FromArgb(255, currentColor));
                using var brush = new SolidBrush(textColor);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                g.DrawString(keyText, font, brush, rect, sf);
            }

            UpdateLayeredWindowFromBitmap(bmp);
        }

        private void UpdateLayeredWindowFromBitmap(Bitmap bmp)
        {
            if (_layeredWindow == null || _layeredWindow.IsDisposed) return;

            IntPtr screenDC = IntPtr.Zero;
            IntPtr memDC = IntPtr.Zero;
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr oldBitmap = IntPtr.Zero;

            try
            {
                screenDC = GetDC(IntPtr.Zero);
                memDC = CreateCompatibleDC(screenDC);
                hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
                oldBitmap = SelectObject(memDC, hBitmap);

                var size = new Size32(bmp.Width, bmp.Height);
                var pointSource = new Point32(0, 0);
                var topPos = new Point32(_layeredWindow.Left, _layeredWindow.Top);

                var blend = new BLENDFUNCTION
                {
                    BlendOp = AC_SRC_OVER,
                    BlendFlags = 0,
                    SourceConstantAlpha = 255, // 비트맵의 알파 채널 사용
                    AlphaFormat = AC_SRC_ALPHA
                };

                UpdateLayeredWindow(_layeredWindow.Handle, IntPtr.Zero, ref topPos, ref size, memDC, ref pointSource, 0, ref blend, ULW_ALPHA);
            }
            finally
            {
                if (oldBitmap != IntPtr.Zero) SelectObject(memDC, oldBitmap);
                if (hBitmap != IntPtr.Zero) DeleteObject(hBitmap);
                if (memDC != IntPtr.Zero) DeleteDC(memDC);
                if (screenDC != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screenDC);
            }
        }

        public void Dispose()
        {
            if (_layeredWindow != null && !_layeredWindow.IsDisposed)
            {
                _layeredWindow.Close();
                _layeredWindow.Dispose();
            }
            _layeredWindow = null;
        }

        public void Show()
        {
            if (_obsCompatibilityMode)
                Panel.Visible = true;
            else
                _layeredWindow?.Show();
        }

        public void Hide()
        {
            if (_obsCompatibilityMode)
                Panel.Visible = false;
            else
                _layeredWindow?.Hide();
        }

        public void BringToFront()
        {
            if (_obsCompatibilityMode)
                Panel.BringToFront();
            else
                _layeredWindow?.BringToFront();
        }

        private string GetKeyDisplayName(Keys key)
        {
            return key switch
            {
                Keys.LControlKey or Keys.RControlKey or Keys.ControlKey => "Ctrl",
                Keys.LShiftKey or Keys.RShiftKey or Keys.ShiftKey => "Shift",
                Keys.LMenu or Keys.RMenu or Keys.Menu => "Alt",
                Keys.Space => "Space",
                _ => key.ToString().Replace("Key", "")
            };
        }

        private Color GetContrastColor(Color bg)
        {
            int yiq = ((bg.R * 299) + (bg.G * 587) + (bg.B * 114)) / 1000;
            return yiq >= 128 ? Color.Black : Color.White;
        }
    }
}