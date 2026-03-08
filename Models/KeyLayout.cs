using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace keyviewer.Models
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
        
        // 테두리 설정
        public bool BorderEnabled { get; set; }
        public int BorderColorArgb { get; set; }
        public int BorderWidth { get; set; } = 2;
        
        // 🆕 모서리 반경
        public int CornerRadius { get; set; } = 0;
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
        
        // 🆕 OBS 모드 설정
        public bool OBSCompatibilityMode { get; set; }
    }
}