using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace keyviewer.UI.Editors;

public partial class PanelEditorForm : Form
{
    private readonly PanelEditorControl _editor;
    private readonly Button _btnOk;
    private readonly Button _btnCancel;

    // 🔥 좌/우 modifier 구분을 위한 GetKeyState
    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    public Keys SelectedKey => _editor.SelectedKey;
    public Color SelectedUpColor => _editor.SelectedUpColor;
    public Color SelectedDownColor => _editor.SelectedDownColor;
    public int SelectedAlpha => _editor.SelectedAlpha;
    public Size SelectedSize => _editor.SelectedSize;
    public string SelectedDisplayName => _editor.SelectedDisplayName;
    public bool BorderEnabled => _editor.BorderEnabled;
    public Color BorderColor => _editor.BorderColor;
    public int BorderWidth => _editor.BorderWidth;
    public int CornerRadius => _editor.CornerRadius;

    public PanelEditorForm()
    {
        InitializeComponent();

        this.KeyPreview = true;

        _editor = new PanelEditorControl { Dock = DockStyle.Top };
        _btnCancel = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Size = new Size(400, 35),
            Location = new Point(50, 265)
        };
        _btnOk = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Size = new Size(400, 35),
            Location = new Point(50, 305)
        };

        _editor.Height = 255;

        Controls.Add(_editor);
        Controls.Add(_btnCancel);
        Controls.Add(_btnOk);
        
        ClientSize = new Size(500, 350);
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Panel Settings";

        // 🔥 ProcessCmdKey 대신 KeyDown 사용 → Shift 포함 모든 키 캡처
        this.KeyDown += OnFormKeyDown;
    }

    // 🔥 Shift/Ctrl/Alt를 포함한 모든 키를 KeyPreview로 캡처
    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (_editor?.IsRecording != true) return;

        // GetKeyState로 좌/우 modifier 구분 (0xA0=LShift, 0xA1=RShift 등)
        Keys rawKey = e.KeyCode switch
        {
            Keys.ShiftKey   => (GetKeyState(0xA0) & 0x8000) != 0 ? Keys.LShiftKey  : Keys.RShiftKey,
            Keys.ControlKey => (GetKeyState(0xA2) & 0x8000) != 0 ? Keys.LControlKey : Keys.RControlKey,
            Keys.Menu       => (GetKeyState(0xA4) & 0x8000) != 0 ? Keys.LMenu       : Keys.RMenu,
            _               => e.KeyCode
        };

        _editor.RecordKey(rawKey);
        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
    }

    public PanelEditorForm(Keys initialKey, Color upColor, Color downColor) : this()
    {
        _editor.SelectedKey = initialKey;
        _editor.SelectedUpColor = upColor;
        _editor.SelectedDownColor = downColor;
        _editor.SelectedAlpha = upColor.A;
    }

    public PanelEditorForm(Keys initialKey, Color upColor, Color downColor, string displayName, Size size) : this()
    {
        _editor.SelectedKey = initialKey;
        _editor.SelectedUpColor = upColor;
        _editor.SelectedDownColor = downColor;
        _editor.SelectedAlpha = upColor.A;
        _editor.SelectedDisplayName = displayName;
        _editor.SelectedWidth = size.Width;
        _editor.SelectedHeight = size.Height;
    }

    public PanelEditorForm(Keys initialKey, Color upColor, Color downColor, string displayName, Size size, 
        bool borderEnabled, Color borderColor, int borderWidth) : this()
    {
        _editor.SelectedKey = initialKey;
        _editor.SelectedUpColor = upColor;
        _editor.SelectedDownColor = downColor;
        _editor.SelectedAlpha = upColor.A;
        _editor.SelectedDisplayName = displayName;
        _editor.SelectedWidth = size.Width;
        _editor.SelectedHeight = size.Height;
        _editor.BorderEnabled = borderEnabled;
        _editor.BorderColor = borderColor;
        _editor.BorderWidth = borderWidth;
    }

    public PanelEditorForm(Keys initialKey, Color upColor, Color downColor, string displayName, Size size, 
        bool borderEnabled, Color borderColor, int borderWidth, int cornerRadius) : this()
    {
        _editor.SelectedKey = initialKey;
        _editor.SelectedUpColor = upColor;
        _editor.SelectedDownColor = downColor;
        _editor.SelectedAlpha = upColor.A;
        _editor.SelectedDisplayName = displayName;
        _editor.SelectedWidth = size.Width;
        _editor.SelectedHeight = size.Height;
        _editor.BorderEnabled = borderEnabled;
        _editor.BorderColor = borderColor;
        _editor.BorderWidth = borderWidth;
        _editor.CornerRadius = cornerRadius;
    }

    private void InitializeComponent()
    {
        SuspendLayout();
        ClientSize = new Size(500, 350);
        Name = "PanelEditorForm";
        Load += PanelEditorForm_Load;
        ResumeLayout(false);
    }

    private void PanelEditorForm_Load(object sender, EventArgs e) { }
}