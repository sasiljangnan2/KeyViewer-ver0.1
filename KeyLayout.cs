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
        public string? DisplayName { get; set; } // 🆕 커스텀 표시 이름
    }

    public class KeyLayout
    {
        public string? Name { get; set; }
        public List<KeyPanelConfig> Panels { get; set; } = new List<KeyPanelConfig>();
        
        // 창 설정
        public int FormWidth { get; set; }
        public int FormHeight { get; set; }
        
        // 배경 설정
        public int BackgroundColorArgb { get; set; }
        public string? BackgroundImagePath { get; set; }
        public bool BackgroundTransparent { get; set; }
        public int ChromaKeyColorArgb { get; set; }
        
        // 투명도 설정
        public int WindowOpacityPercent { get; set; }
    }
}