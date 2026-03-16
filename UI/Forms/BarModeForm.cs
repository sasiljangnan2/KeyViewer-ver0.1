using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using keyviewer.UI.Controls;

namespace keyviewer.UI.Forms
{
    /// <summary>
    /// 바 모드 윈도우 - 키 입력을 바 형식으로 표시합니다.
    /// </summary>
    public partial class BarModeForm : Form
    {
        private List<KeyPanel> _keyPanels;
        private Dictionary<Keys, BarVisualizer> _barVisualizers = new Dictionary<Keys, BarVisualizer>();
        private Dictionary<Keys, Label> _keyLabels = new Dictionary<Keys, Label>();
        private System.Windows.Forms.Timer _updateTimer;
        private const int BAR_HEIGHT = 30;
        private const int SPACING = 10;
        private const int BAR_WIDTH = 300;
        private const int LABEL_HEIGHT = 25;

        public BarModeForm(List<KeyPanel> keyPanels)
        {
            _keyPanels = keyPanels;
            InitializeComponent();
            SetupBars();
            SetupTimer();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ClientSize = new Size(100, _keyPanels.Count * (BAR_WIDTH + LABEL_HEIGHT + SPACING) + 20);
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.Name = "BarModeForm";
            this.Text = "Key Visualizer - Bar Mode";
            this.TopMost = true;
            this.AutoScroll = true;
            this.DoubleBuffered = true;

            this.ResumeLayout(false);
        }

        private void SetupBars()
        {
            int yPos = 10;
            int xPos = 10;

            foreach (var kp in _keyPanels)
            {
                // 바 시각화 패널 (먼저 배치)
                var barPanel = CreateBarVisualizer(kp, xPos, yPos, BAR_HEIGHT, BAR_WIDTH);
                this.Controls.Add(barPanel.Panel);
                _barVisualizers[kp.Key] = barPanel;

                // 키 이름 라벨 (바 아래에 배치)
                Label keyLabel = new Label
                {
                    Text = string.IsNullOrEmpty(kp.DisplayName) ? kp.Key.ToString() : kp.DisplayName,
                    Location = new Point(xPos, yPos + BAR_WIDTH),
                    Size = new Size(BAR_HEIGHT, LABEL_HEIGHT),
                    ForeColor = Color.White,
                    BackColor = Color.FromArgb(32, 32, 32),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 9, FontStyle.Bold)
                };
                this.Controls.Add(keyLabel);
                _keyLabels[kp.Key] = keyLabel;

                xPos += BAR_HEIGHT + SPACING;
            }

            // 전체 크기 조정 (가로: 키 개수만큼, 세로: 바 높이 + 라벨 높이)
            this.ClientSize = new Size(xPos + 10, BAR_WIDTH + LABEL_HEIGHT + 20);
        }

        /// <summary>
        /// 바 시각화 컴포넌트를 생성합니다 (팩토리 패턴)
        /// </summary>
        private BarVisualizer CreateBarVisualizer(KeyPanel kp, int x, int y, int width, int height)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = Color.FromArgb(50, 50, 50),
                BorderStyle = BorderStyle.FixedSingle
            };

            var visualizer = new BarVisualizer(panel, kp.DownColor, kp.UpColor);
            
            panel.Paint += (s, e) =>
            {
                visualizer.Draw(e.Graphics, panel.ClientRectangle);
            };

            return visualizer;
        }

        private void SetupTimer()
        {
            _updateTimer = new System.Windows.Forms.Timer();
            _updateTimer.Interval = 8; // 약 125 FPS (더 부드러움)
            _updateTimer.Tick += UpdateBars;
            _updateTimer.Start();
        }

        private void UpdateBars(object? sender, EventArgs e)
        {
            foreach (var kp in _keyPanels)
            {
                if (_barVisualizers.ContainsKey(kp.Key))
                {
                    var visualizer = _barVisualizers[kp.Key];
                    visualizer.Update(kp.IsKeyPressed);
                    visualizer.Panel.Invalidate();
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer.Dispose();
            }
            base.OnFormClosed(e);
        }

        /// <summary>
        /// 바 시각화를 담당하는 클래스
        /// </summary>
        private class BarVisualizer
        {
            public Panel Panel { get; }
            private Color _activeColor;
            private Color _inactiveColor;
            private List<RectangleData> _rectangles = new List<RectangleData>();
            private bool _wasKeyPressedLastFrame = false;
            private const float RISE_SPEED = 5f;        // 높이 증가 속도
            private const float RISE_OFFSET_SPEED = 5f; // 키를 누르는 동안의 올라가는 속도
            private const float FALL_SPEED = 5f;        // 키를 뗀 후의 올라가는 속도

            /// <summary>
            /// 각 사각형의 데이터를 관리하는 구조체
            /// </summary>
            private class RectangleData
            {
                public float Height { get; set; }     // 사각형의 높이
                public float OffsetY { get; set; }    // 사각형의 Y 위치 (위로 올라가는 오프셋)
                public bool IsMoving { get; set; }    // 올라가는 중 여부
            }

            public BarVisualizer(Panel panel, Color activeColor, Color inactiveColor)
            {
                Panel = panel;
                _activeColor = activeColor;
                _inactiveColor = inactiveColor;
            }

            public void Update(bool isKeyPressed)
            {
                // 키가 눌린 순간 감지 - 새로운 사각형 생성
                if (isKeyPressed && !_wasKeyPressedLastFrame)
                {
                    _rectangles.Add(new RectangleData 
                    { 
                        Height = 0, 
                        OffsetY = 0, 
                        IsMoving = false 
                    });
                }

                // 각 사각형 업데이트
                for (int i = _rectangles.Count - 1; i >= 0; i--)
                {
                    var rect = _rectangles[i];
                    bool isNewestRectangle = (i == _rectangles.Count - 1);

                    if (!rect.IsMoving && isKeyPressed && isNewestRectangle)
                    {
                        // 높이만 증가 (바의 아래쪽에서 위로 자라남)
                        // 패널 높이를 초과하지 않도록 제한
                        float maxHeight = Panel.Height - 4;
                        float heightIncrease = Math.Min(RISE_SPEED, maxHeight - rect.Height);
                        rect.Height += heightIncrease;
                    }
                    else if (!isKeyPressed && isNewestRectangle && !rect.IsMoving)
                    {
                        // 키를 떼면 계속 올라가기만 함 (높이는 유지)
                        rect.IsMoving = true;
                        rect.OffsetY = -5f; // 첫 프레임부터 약간 올라가 있도록 초기화
                    }
                    else if (rect.IsMoving)
                    {
                        // 올라가는 중: 위로 이동
                        rect.OffsetY -= FALL_SPEED;
                        
                        // 패널 범위를 완전히 벗어나면 제거
                        if (rect.OffsetY <= -Panel.Height)
                        {
                            _rectangles.RemoveAt(i);
                        }
                    }
                }   

                _wasKeyPressedLastFrame = isKeyPressed;
            }

            public void Draw(Graphics g, Rectangle bounds)
            {
                // 배경 그리기
                using (var bgBrush = new SolidBrush(Color.FromArgb(50, 50, 50)))
                {
                    g.FillRectangle(bgBrush, bounds);
                }

                // 테두리 그리기
                using (var borderPen = new Pen(Color.FromArgb(100, 100, 100), 1))
                {
                    g.DrawRectangle(borderPen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
                }

                // 모든 사각형 그리기
                foreach (var rect in _rectangles)
                {
                    if (rect.Height <= 0) continue;

                    // 패널 안쪽 여백을 한 번 정의하고 그 안에서만 그림
                    float innerLeft = bounds.Left + 2f;
                    float innerTop = bounds.Top + 2f;
                    float innerRight = bounds.Right - 2f;
                    float innerBottom = bounds.Bottom - 2f;
                    float innerWidth = Math.Max(0f, innerRight - innerLeft);
                    float innerHeight = Math.Max(0f, innerBottom - innerTop);

                    // Growing 상태와 Floating 상태에 따라 다르게 계산
                    float barTop, barBottom;
                    
                    if (!rect.IsMoving)
                    {
                        // Growing 상태: 바의 아래쪽에서 위로 자라남
                        // barTop이 innerTop 아래로 유지되도록 보장
                        barBottom = innerBottom;
                        barTop = Math.Max(innerTop, innerBottom - rect.Height);
                    }
                    else
                    {
                        // Floating 상태: 전체 사각형이 위로 이동
                        barBottom = innerBottom + rect.OffsetY;
                        barTop = barBottom - rect.Height;
                    }
                    
                    // 클리핑: 패널 범위 내에서만 그리기
                    float clipTop = Math.Max(innerTop, barTop);
                    float clipBottom = Math.Min(innerBottom, barBottom);
                    float drawHeight = Math.Max(0, clipBottom - clipTop);
                    
                    // 사각형이 완전히 범위 밖이면 스킵
                    if (drawHeight <= 0 || clipBottom <= innerTop || clipTop >= innerBottom) continue;

                    var barRect = new RectangleF(
                        innerLeft,
                        clipTop,
                        innerWidth,
                        drawHeight
                    );

                    using (var barBrush = new SolidBrush(_activeColor))
                    {
                        g.FillRectangle(barBrush, barRect);
                    }

                    // 강조 테두리
                    using (var barBorderPen = new Pen(Color.White, 1.5f))
                    {
                        float borderWidth = Math.Max(0f, barRect.Width - 1f);
                        float borderHeight = Math.Max(0f, barRect.Height - 1f);
                        if (borderWidth > 0 && borderHeight > 0)
                        {
                            g.DrawRectangle(barBorderPen,
                                barRect.X + 0.5f,
                                barRect.Y + 0.5f,
                                borderWidth,
                                borderHeight);
                        }
                    }
                }
            }
        }
    }
}

