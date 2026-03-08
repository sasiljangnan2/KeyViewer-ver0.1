using keyviewer.UI.Forms;
using keyviewer.UI.Editors;
using keyviewer.UI.Controls;
using keyviewer.Services;
using keyviewer.Models;

namespace keyviewer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            
            string layoutsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "layouts");
            Directory.CreateDirectory(layoutsDir);
            
            // 🔥 layout.json - 전체 키보드 레이아웃
            string fullKeyboardPath = Path.Combine(layoutsDir, "layout.json");
            if (!File.Exists(fullKeyboardPath))
            {
                var fullKeyboard = LayoutManager.CreateFullKeyboardLayout("Full Keyboard");
                LayoutManager.SaveLayout(fullKeyboardPath, fullKeyboard);
            }
            
            // 🔥 layout1.json - 방향키 + 수식어 레이아웃
            string layout1Path = Path.Combine(layoutsDir, "layout1.json");
            if (!File.Exists(layout1Path))
            {
                var layout1 = LayoutManager.CreateLayout1Layout("layout");
                LayoutManager.SaveLayout(layout1Path, layout1);
            }
            
            // 🔥 layout2.json - ASDF + Shift + Space 레이아웃
            string layout2Path = Path.Combine(layoutsDir, "layout2.json");
            if (!File.Exists(layout2Path))
            {
                var layout2 = LayoutManager.CreateLayout2Layout("layout2");
                LayoutManager.SaveLayout(layout2Path, layout2);
            }
            
            var form = new Form1();
            form.ClientSize = new System.Drawing.Size(550, 350);
            
            Application.Run(form);
        }
    }
}