using AssetsView.Winforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AssetsView
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var outputPath = @"D:\TestRip";
            var gamePath = @"D:\SteamLibrary\steamapps\common\Risk of Rain 2\Risk of Rain 2.exe";
            if (System.IO.Directory.Exists(outputPath))
            {
                System.IO.Directory.Delete(outputPath, true);
            }
            AssetsExporter.AssetExportManager.ExportGame(gamePath, outputPath);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new StartScreen());
        }
    }
}
