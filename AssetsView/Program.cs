using AssetsView.Winforms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.XPath;

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
            var outputPath = @"D:\test\MoreRipTest";
            var gamePath = @"D:\Steam\steamapps\common\Risk of Rain 2\Risk of Rain 2.exe";
            var editorPath = @"C:\Program Files\Unity Editors\2018.4.16f1\Editor";
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, true);
            }
            AssetsExporter.GameExporter.ExportGame(gamePath, outputPath, editorPath);

            /*var exportManager = AssetsExporter.YAMLExportManager.CreateDefault();
            var assetsManager = new AssetsTools.NET.Extra.AssetsManager();
            assetsManager.LoadClassPackage("classdata.tpk");
            var resourcesFile = assetsManager.LoadAssetsFile(@"D:\Steam\steamapps\common\Risk of Rain 2\Risk of Rain 2_Data\level0.assets", false);
            assetsManager.LoadClassDatabaseFromPackage(resourcesFile.file.typeTree.unityVersion);

            var collection = AssetsExporter.Collection.AssetCollection.CreateAssetCollection(assetsManager, assetsManager.GetExtAsset(resourcesFile, 0, 5841));

            using (var file = System.IO.File.Create(@"D:\test\font.asset"))
            using (var streamWriter = new AssetsExporter.InvariantStreamWriter(file))
            {
                var yamlWriter = new AssetsExporter.YAML.YAMLWriter();
                foreach (var doc in exportManager.Export(collection, assetsManager))
                {
                    yamlWriter.AddDocument(doc);
                }
                yamlWriter.Write(streamWriter);
            }*/

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new StartScreen());
        }
    }
}
