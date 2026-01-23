namespace keyviewer
{
    public partial class Form1 : Form
    {
        private readonly Color[] _colors = { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Purple, Color.Orange };
        private int _colorIndex = 0;
        private readonly Color _defaultColor = SystemColors.Control;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        // 폴더/폰트 대화상자 이벤트(사용하지 않으면 그대로 둬도 됩니다)
        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }

        private void fontDialog1_Apply(object sender, EventArgs e)
        {

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        // 키 누름: 누른 동안 색 변경
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // 알파벳 키 매핑 예시
            if (e.KeyCode == Keys.A)
            {
                panel1.BackColor = Color.Red;
            }
            else if (e.KeyCode == Keys.S)
            {
                panel2.BackColor = Color.Red;
            }
            else if (e.KeyCode == Keys.D)
            {
                panel3.BackColor = Color.Red;
            }
            else if (e.KeyCode == Keys.L)
            {
                panel4.BackColor = Color.Red;
            }

            // 세미콜론(;) — 대부분의 레이아웃에서 Keys.Oem1
            else if (e.KeyCode == Keys.Oem1)
            {
                panel5.BackColor = Color.Red;
            }

            // 아포스트로피(') — 대부분의 레이아웃에서 Keys.Oem7
            else if (e.KeyCode == Keys.Oem7)
            {
                panel6.BackColor = Color.Red;
            }
        }

        // 키 뗌: 누르고 있던 키가 떼어지면 기본색으로 복원
        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.A)
            {
                panel1.BackColor = _defaultColor;
            }
            else if (e.KeyCode == Keys.S)
            {
                panel2.BackColor = _defaultColor;
            }
            else if (e.KeyCode == Keys.D)
            {
                panel3.BackColor = _defaultColor;
            }
            else if (e.KeyCode == Keys.L)
            {
                panel4.BackColor = _defaultColor;
            }
            else if (e.KeyCode == Keys.Oem1)
            {
                panel5.BackColor = _defaultColor;
            }
            else if (e.KeyCode == Keys.Oem7)
            {
                panel6.BackColor = _defaultColor;
            }
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
