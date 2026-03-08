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
            
            // 🔥 첫 실행 시 샘플 레이아웃 생성
            string layoutPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "layouts", "default.json");
            if (!File.Exists(layoutPath))
            {
                var sampleLayout = LayoutManager.CreateSampleLayout("Default");
                LayoutManager.SaveLayout(layoutPath, sampleLayout);
            }
            
            Application.Run(new Form1());
        }
    }
}