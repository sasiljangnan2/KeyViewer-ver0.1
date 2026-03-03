using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace keyviewer
{
    public class LayoutPickerForm : Form
    {
        private ListBox _lbLayouts;
        private Button _btnOk;
        private Button _btnCancel;
        private readonly string _layoutsDir;

        public string? SelectedLayoutFileName { get; private set; }

        public LayoutPickerForm(string layoutsDir)
        {
            _layoutsDir = layoutsDir ?? throw new ArgumentNullException(nameof(layoutsDir));
            Text = "Select Layout";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new System.Drawing.Size(360, 320);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            _lbLayouts = new ListBox
            {
                Left = 12,
                Top = 12,
                Width = 336,
                Height = 240
            };

            _btnOk = new Button
            {
                Text = "Apply",
                Left = 192,
                Top = 264,
                Width = 75,
                DialogResult = DialogResult.OK
            };

            _btnCancel = new Button
            {
                Text = "Cancel",
                Left = 279,
                Top = 264,
                Width = 75,
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(_lbLayouts);
            Controls.Add(_btnOk);
            Controls.Add(_btnCancel);

            Load += LayoutPickerForm_Load;
            _btnOk.Click += BtnOk_Click;
        }

        private void LayoutPickerForm_Load(object? sender, EventArgs e)
        {
            _lbLayouts.Items.Clear();
            if (!Directory.Exists(_layoutsDir)) return;
            var files = Directory.GetFiles(_layoutsDir, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(n => n);
            _lbLayouts.Items.AddRange(files.ToArray());
            if (_lbLayouts.Items.Count > 0) _lbLayouts.SelectedIndex = 0;
        }

        private void BtnOk_Click(object? sender, EventArgs e)
        {
            if (_lbLayouts.SelectedItem == null)
            {
                MessageBox.Show(this, "레이아웃을 선택하세요.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.None;
                return;
            }

            SelectedLayoutFileName = _lbLayouts.SelectedItem.ToString();
            // DialogResult already set by button
        }
    }
}