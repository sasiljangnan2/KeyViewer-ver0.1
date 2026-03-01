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

        public KeyPanel(Panel panel, Keys key, Color downColor, Color upColor)
        {
            Panel = panel ?? throw new System.ArgumentNullException(nameof(panel));
            Key = key;
            DownColor = downColor;
            UpColor = upColor;

            // 패널을 숨기고 대신 레이어드 윈도우 생성
            Panel.Visible = false;
            CreateLayeredWindow();
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

            _layeredWindow = new LayeredForm // 커스텀 Form 사용
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Location = Panel.Parent?.PointToScreen(Panel.Location) ?? Panel.Location,
                MinimumSize = new Size(0, 0),
                MaximumSize = new Size(0, 0),
                AutoSize = false, // AutoSize 비활성화
                Owner = Panel.Parent as Form
            };

            _layeredWindow.Load += (_, __) =>
            {
                int ex = GetWindowLong(_layeredWindow.Handle, GWL_EXSTYLE);
                ex |= WS_EX_LAYERED | WS_EX_TOOLWINDOW;
                SetWindowLong(_layeredWindow.Handle, GWL_EXSTYLE, ex);
                
                // SetWindowPos로 크기 강제 설정
                SetWindowPos(_layeredWindow.Handle, IntPtr.Zero, 
                    _layeredWindow.Left, _layeredWindow.Top, 
                    Panel.Size.Width, Panel.Size.Height, 
                    SWP_NOZORDER | SWP_NOACTIVATE);
            };

            _layeredWindow.Show();
            
            // Show 후 한 번 더 크기 강제 설정 (확실하게)
            _layeredWindow.Size = Panel.Size;
            SetWindowPos(_layeredWindow.Handle, IntPtr.Zero, 
                _layeredWindow.Left, _layeredWindow.Top, 
                Panel.Size.Width, Panel.Size.Height, 
                SWP_NOZORDER | SWP_NOACTIVATE);
            
            UpdateVisual();
        }

        public void UpdatePosition(Point screenLocation)
        {
            if (_layeredWindow != null && !_layeredWindow.IsDisposed)
            {
                _layeredWindow.Location = screenLocation;
            }
        }

        public void UpdateSize(Size size)
        {
            if (_layeredWindow != null && !_layeredWindow.IsDisposed)
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
            if (_layeredWindow == null || _layeredWindow.IsDisposed) return;

            Color currentColor = _isKeyDown ? DownColor : UpColor;
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

                // 텍스트 그리기
                var rect = new Rectangle(0, 0, w, h);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                // 대비 색 (불투명)
                Color textColor = GetContrastColor(Color.FromArgb(255, currentColor));
                using var brush = new SolidBrush(textColor);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                // 폰트는 패널 부모(Form)의 폰트 사용
                var font = Panel.Parent?.Font ?? SystemFonts.DefaultFont;
                g.DrawString(Key.ToString(), font, brush, rect, sf);
            }

            UpdateLayeredWindowFromBitmap(bmp);
        }

        private Color GetContrastColor(Color bg)
        {
            int yiq = ((bg.R * 299) + (bg.G * 587) + (bg.B * 114)) / 1000;
            return yiq >= 128 ? Color.Black : Color.White;
        }

        private void UpdateLayeredWindowFromBitmap(Bitmap bmp)
        {
            if (_layeredWindow == null || _layeredWindow.IsDisposed || bmp == null) return;

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
            if (_layeredWindow != null && !_layeredWindow.IsDisposed)
                _layeredWindow.Show();
        }

        public void Hide()
        {
            if (_layeredWindow != null && !_layeredWindow.IsDisposed)
                _layeredWindow.Hide();
        }

        public void BringToFront()
        {
            if (_layeredWindow != null && !_layeredWindow.IsDisposed)
                _layeredWindow.BringToFront();
        }
    }
}