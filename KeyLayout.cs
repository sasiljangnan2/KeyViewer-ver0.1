using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace keyviewer
{
    public class KeyPanelConfig
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Keys Key { get; set; }
        public int DownArgb { get; set; }
        public int UpArgb { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string? Name { get; set; }
    }

    public class KeyLayout
    {
        public string? Name { get; set; }
        public List<KeyPanelConfig> Panels { get; set; } = new List<KeyPanelConfig>();
    }
}