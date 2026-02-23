using System;
using System.Drawing;
using System.Windows.Forms;

namespace keyviewer
{
    public class KeyPickerForm : Form
    {
        private ComboBox _cbKeys;
        private Button _btnColor;
        private Panel _colorPreview;
        private Button _btnOk;
        private Button _btnCancel;
        private ColorDialog _colorDialog;

        public Keys SelectedKey => _cbKeys.SelectedItem is Keys k ? k : Keys.None;
        public Color SelectedColor => _colorPreview.BackColor;

        public KeyPickerForm()
        {
            Text = "Select Key and Color";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(360, 140);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            _cbKeys = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Left = 12,
                Top = 12,
                Width = 336
            };
            foreach (Keys k in Enum.GetValues(typeof(Keys)))
            {
                _cbKeys.Items.Add(k);
            }
            if (_cbKeys.Items.Count > 0)
                _cbKeys.SelectedIndex = 0;

            _btnColor = new Button
            {
                Text = "Choose Color...",
                Left = 12,
                Top = 48,
                Width = 120
            };

            _colorPreview = new Panel
            {
                Left = 144,
                Top = 48,
                Size = new Size(40, 24),
                BackColor = Color.Red,
                BorderStyle = BorderStyle.FixedSingle
            };

            _colorDialog = new ColorDialog { AllowFullOpen = true, AnyColor = true };

            _btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Left = 192,
                Top = 92,
                Width = 75
            };

            _btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Left = 279,
                Top = 92,
                Width = 75
            };

            _btnColor.Click += (s, e) =>
            {
                if (_colorDialog.ShowDialog(this) == DialogResult.OK)
                {
                    _colorPreview.BackColor = _colorDialog.Color;
                }
            };

            Controls.Add(_cbKeys);
            Controls.Add(_btnColor);
            Controls.Add(_colorPreview);
            Controls.Add(_btnOk);
            Controls.Add(_btnCancel);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }
    }
}