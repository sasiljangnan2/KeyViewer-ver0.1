using System.Drawing;
using System.Windows.Forms;

namespace keyviewer
{
    public class KeyPanel
    {
        public Panel Panel { get; }
        public Keys Key { get; set; }
        public Color DownColor { get; set; }
        public Color UpColor { get; set; }

        public KeyPanel(Panel panel, Keys key, Color downColor, Color upColor)
        {
            Panel = panel ?? throw new System.ArgumentNullException(nameof(panel));
            Key = key;
            DownColor = downColor;
            UpColor = upColor;
        }

        public void HandleKeyDown(Keys key)
        {
            if (key == Key)
            {
                Panel.BackColor = DownColor;
            }
        }

        public void HandleKeyUp(Keys key)
        {
            if (key == Key)
            {
                Panel.BackColor = UpColor;
            }
        }
    }
}