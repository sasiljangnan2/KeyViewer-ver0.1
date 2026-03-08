using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace keyviewer.UI.Controls;

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
    private const int WS_EX_NOACTIVATE = 0x08000000; // 🔥 있는지 확인
    private const byte AC_SRC_OVER = 0x00;
    private const byte AC_SRC_ALPHA = 0x01;
    private const int ULW_ALPHA = 0x00000002;
    private const int WM_GETMINMAXINFO = 0x0024;
    private const int SWP_NOMOVE = 0x0002;
    private const int SWP_NOSIZE = 0x0001;  // 🔥 추가
    private const int SWP_NOZORDER = 0x0004;
    private const int SWP_NOACTIVATE = 0x0010;
    private const int SW_SHOWNOACTIVATE = 4; // 🔥 있는지 확인
    private const int SW_HIDE = 0;            // 🔥 있는지 확인
    private const int GWLP_HWNDPARENT = -8; // 🔥 Win32 owner 제어
    
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
            : SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
    }

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
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow); // 🔥 추가

    public Panel Panel { get; }
    private LayeredForm? _layeredWindow;
    
    // PUBLIC 속성으로 노출 - KeyPanelService에서 접근 가능하도록
    public Form? LayeredWindow => _layeredWindow;
    
    public Keys Key { get; set; }
    public Color DownColor { get; set; }
    public Color UpColor { get; set; }
    public string? DisplayName { get; set; } // 커스텀 이름 (nil이면 기본 키 이름 사용)

    private bool _isKeyDown = false;
    private bool _obsCompatibilityMode = false; // OBS 호환 모드

    public bool BorderEnabled { get; set; }
    public Color BorderColor { get; set; } = Color.Black;
    public int BorderWidth { get; set; } = 2;
    public int CornerRadius { get; set; } = 0; // 기본값: 사각형

    public KeyPanel(Panel panel, Keys key, Color downColor, Color upColor, bool obsCompatibilityMode = false)
    {
        Panel = panel ?? throw new System.ArgumentNullException(nameof(panel));
        Key = key;
        DownColor = downColor;
        UpColor = upColor;
        _obsCompatibilityMode = obsCompatibilityMode;

        // DisplayName을 기본 키 이름으로 초기화
        DisplayName = GetKeyDisplayName(key);

        if (_obsCompatibilityMode)
        {
            // OBS 호환 모드: 일반 패널 사용
            Panel.Visible = true;
            Panel.BackColor = upColor;

            // OBS 모드에서 텍스트를 그리기 위해 Paint 이벤트 처리
            Panel.Paint += (s, e) =>
            {
                // 고품질 렌더링 설정
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                
                string keyText = !string.IsNullOrEmpty(DisplayName) 
                    ? DisplayName 
                    : GetKeyDisplayName(Key);
                
                e.Graphics.Clear(Panel.BackColor);
                
                var rect = Panel.ClientRectangle;
                
                // 🆕 테두리를 고려한 정확한 사각형 계산
                float halfBorder = BorderEnabled ? BorderWidth / 2.0f : 0f;
                RectangleF drawRect = new RectangleF(
                    halfBorder, 
                    halfBorder, 
                    Panel.Width - BorderWidth, 
                    Panel.Height - BorderWidth);
                
                // 둥글은 모서리 처리
                if (CornerRadius > 0)
                {
                    using var path = GetRoundedRectPathF(drawRect, CornerRadius);
                    
                    using (var bgBrush = new SolidBrush(Panel.BackColor))
                    {
                        e.Graphics.FillPath(bgBrush, path);
                    }
                    
                    if (BorderEnabled)
                    {
                        using var pen = new Pen(BorderColor, BorderWidth);
                        e.Graphics.DrawPath(pen, path);
                    }
                    
                    Panel.Region = new Region(path);
                }
                else
                {
                    if (BorderEnabled)
                    {
                        using var pen = new Pen(BorderColor, BorderWidth);
                        e.Graphics.DrawRectangle(pen, halfBorder, halfBorder, 
                            Panel.Width - BorderWidth, Panel.Height - BorderWidth);
                    }
                    
                    Panel.Region = null;
                }
                
                // 텍스트 그리기
                using var sf = new StringFormat 
                { 
                    Alignment = StringAlignment.Center, 
                    LineAlignment = StringAlignment.Center,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                
                int fontSize = Math.Max(8, Math.Min(Panel.Width, Panel.Height) / 3);
                using var font = new Font("Arial", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
                Color textColor = GetContrastColor(Color.FromArgb(255, Panel.BackColor));
                using var brush = new SolidBrush(textColor);
                
                e.Graphics.DrawString(keyText, font, brush, Panel.ClientRectangle, sf);
            };
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
        protected override bool ShowWithoutActivation => true; // 🔥 포커스 없이 Show 가능

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_GETMINMAXINFO)
            {
                MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(m.LParam, typeof(MINMAXINFO))!;
                mmi.ptMinTrackSize = new System.Drawing.Point(1, 1);
                mmi.ptMaxTrackSize = new System.Drawing.Point(10000, 10000);
                Marshal.StructureToPtr(mmi, m.LParam, true);
            }
            base.WndProc(ref m);
        }
    }

    private void CreateLayeredWindow()
    {
        if (_layeredWindow != null && !_layeredWindow.IsDisposed) return;

        Point initialLocation = Panel.Parent != null 
            ? Panel.Parent.PointToScreen(Panel.Location) 
            : new Point(100, 100);

        _layeredWindow = new LayeredForm
        {
            FormBorderStyle = FormBorderStyle.None,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = initialLocation,
            Size = Panel.Size,
            MinimumSize = new Size(0, 0),
            MaximumSize = new Size(0, 0),
            AutoSize = false,
            Owner = Panel.FindForm(),  // 🔥 Owner를 Form1으로 설정 → Z-order 자동 관리
            TopMost = false
        };

        _layeredWindow.Load += (_, __) =>
        {
            int ex = GetWindowLong(_layeredWindow.Handle, GWL_EXSTYLE);
            ex |= WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
            SetWindowLong(_layeredWindow.Handle, GWL_EXSTYLE, ex);
            
            SetWindowPos(_layeredWindow.Handle, IntPtr.Zero, 
                _layeredWindow.Left, _layeredWindow.Top, 
                Panel.Size.Width, Panel.Size.Height, 
                SWP_NOZORDER | SWP_NOACTIVATE);
        };

        _layeredWindow.Show();
        
        _layeredWindow.Size = Panel.Size;
        SetWindowPos(_layeredWindow.Handle, IntPtr.Zero, 
            _layeredWindow.Left, _layeredWindow.Top, 
            Panel.Size.Width, Panel.Size.Height, 
            SWP_NOZORDER | SWP_NOACTIVATE);
        
        UpdateVisual();
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

    // 둥글은 사각형 경로 생성 헬퍼 메서드
    private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        var path = new System.Drawing.Drawing2D.GraphicsPath();

        if (radius == 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }

    // UpdateVisual() 메서드 수정
    public void UpdateVisual()
    {
        Color currentColor = _isKeyDown ? DownColor : UpColor;

        if (_obsCompatibilityMode)
        {
            Panel.BackColor = currentColor;
            Panel.Invalidate(true);
            Panel.Update();
            return;
        }

        if (_layeredWindow == null || _layeredWindow.IsDisposed) return;

        int w = Math.Max(1, _layeredWindow.Width);
        int h = Math.Max(1, _layeredWindow.Height);

        using var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            // 고품질 렌더링 설정
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            
            g.Clear(Color.Transparent);

            // 🆕 테두리를 고려한 정확한 사각형 계산
            float halfBorder = BorderEnabled ? BorderWidth / 2.0f : 0f;
            RectangleF drawRect = new RectangleF(halfBorder, halfBorder, w - BorderWidth, h - BorderWidth);

            // 둥글 모서리 처리
            if (CornerRadius > 0)
            {
                using var path = GetRoundedRectPathF(drawRect, CornerRadius);
                
                using (var bgBrush = new SolidBrush(currentColor))
                {
                    g.FillPath(bgBrush, path);
                }

                if (BorderEnabled)
                {
                    using var pen = new Pen(BorderColor, BorderWidth);
                    g.DrawPath(pen, path);
                }
            }
            else
            {
                using (var bgBrush = new SolidBrush(currentColor))
                {
                    g.FillRectangle(bgBrush, 0, 0, w, h);
                }

                if (BorderEnabled)
                {
                    using var pen = new Pen(BorderColor, BorderWidth);
                    g.DrawRectangle(pen, halfBorder, halfBorder, w - BorderWidth, h - BorderWidth);
                }
            }

            // 텍스트 그리기
            string keyText = !string.IsNullOrEmpty(DisplayName) 
                ? DisplayName 
                : GetKeyDisplayName(Key);
            
            using var sf = new StringFormat 
            { 
                Alignment = StringAlignment.Center, 
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoWrap
            };
            
            int fontSize = Math.Max(8, Math.Min(w, h) / 3);
            using var font = new Font("Arial", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
            Color textColor = GetContrastColor(Color.FromArgb(255, currentColor));
            using var brush = new SolidBrush(textColor);
            
            g.DrawString(keyText, font, brush, new RectangleF(0, 0, w, h), sf);
        }

        UpdateLayeredWindowFromBitmap(bmp);
    }

    // 🆕 RectangleF 버전의 둥글 사각형 경로 (더 정밀함)
    private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPathF(RectangleF bounds, float radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();

        if (radius <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        float diameter = radius * 2f;
        
        // 반경이 너무 크면 제한
        float maxRadius = Math.Min(bounds.Width, bounds.Height) / 2f;
        if (radius > maxRadius)
        {
            radius = maxRadius;
            diameter = radius * 2f;
        }

        var arc = new RectangleF(bounds.X, bounds.Y, diameter, diameter);
        
        // 왼쪽 위
        path.AddArc(arc, 180, 90);
        
        // 오른쪽 위
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        
        // 오른쪽 아래
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        
        // 왼쪽 아래
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        
        path.CloseFigure();

        return path;
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
        else if (_layeredWindow != null && !_layeredWindow.IsDisposed)
            ShowWindow(_layeredWindow.Handle, SW_SHOWNOACTIVATE); // 🔥 포커스 없이 표시
    }

    public void Hide()
    {
        if (_obsCompatibilityMode)
            Panel.Visible = false;
        else if (_layeredWindow != null && !_layeredWindow.IsDisposed)
            ShowWindow(_layeredWindow.Handle, SW_HIDE); // 🔥 명시적 숨기기
    }

    public void BringToFront()
    {
        if (_obsCompatibilityMode)
            Panel.BringToFront();
        else if (_layeredWindow != null && !_layeredWindow.IsDisposed)
            SetWindowPos(_layeredWindow.Handle, IntPtr.Zero,
                0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE); // 🔥 포커스 없이 Z-order만 변경
    }

    public void SetTopMost(bool topMost)
    {
        if (_layeredWindow == null || _layeredWindow.IsDisposed) return;

        if (topMost)
        {
            // 🔥 Win32로 Owner 해제 → 독립 윈도우로 전환
            SetWindowLongPtr(_layeredWindow.Handle, GWLP_HWNDPARENT, IntPtr.Zero);
            // 🔥 HWND_TOPMOST로 항상 위에
            SetWindowPos(_layeredWindow.Handle, HWND_TOPMOST,
                0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        }
        else
        {
            // 🔥 TOPMOST 해제
            SetWindowPos(_layeredWindow.Handle, HWND_NOTOPMOST,
                0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            // 🔥 Win32로 Owner 복원
            var ownerForm = Panel.FindForm();
            if (ownerForm != null)
                SetWindowLongPtr(_layeredWindow.Handle, GWLP_HWNDPARENT, ownerForm.Handle);
        }
    }

    private string GetKeyDisplayName(Keys key)
    {
        if (!string.IsNullOrWhiteSpace(DisplayName))
            return DisplayName;

        return key switch
        {
            Keys.LShiftKey => "LShift",
            Keys.RShiftKey => "RShift",
            Keys.LControlKey => "LCtrl",
            Keys.RControlKey => "RCtrl",
            Keys.LMenu => "LAlt",
            Keys.RMenu => "RAlt",
            Keys.ShiftKey => "Shift",
            Keys.ControlKey => "Ctrl",
            Keys.Menu => "Alt",
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