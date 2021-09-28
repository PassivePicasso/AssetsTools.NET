using AssetsExporter.Collection;
using AssetsExporter.YAML;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssetsExporter
{
    public class AssetExportManager
    {
        private static readonly Dictionary<UnityClass, string> projectSettingAssetToFileName = new Dictionary<UnityClass, string>()
        {
            [UnityClass.PhysicsManager] = "DynamicsManager",
            [UnityClass.NavMeshProjectSettings] = "NavMeshAreas",
            [UnityClass.PlayerSettings] = "ProjectSettings",
            [UnityClass.BuildSettings] = null,
            [UnityClass.DelayedCallManager] = null,
            [UnityClass.MonoManager] = null,
            [UnityClass.ResourceManager] = null,
            [UnityClass.RuntimeInitializeOnLoadManager] = null,
            [UnityClass.ScriptMapper] = null,
            [UnityClass.StreamingManager] = null,
        };

        public static void ExportGame(string pathToExecutable, string outputDirectory)
        {
            var gameName = Path.GetFileNameWithoutExtension(pathToExecutable);
            var dataFolder = Path.Combine(Path.GetDirectoryName(pathToExecutable), $"{gameName}_Data");
            var exportManager = YAMLExportManager.CreateDefault();
            var assetsManager = new AssetsManager();
            assetsManager.LoadClassPackage("classdata.tpk");
            var globalgamemanagersFile = assetsManager.LoadAssetsFile(Path.Combine(dataFolder, "globalgamemanagers"), true);
            assetsManager.LoadClassDatabaseFromPackage(globalgamemanagersFile.file.typeTree.unityVersion);
            var outputProjectSettingsDirectory = Path.Combine(outputDirectory, "ProjectSettings");
            Directory.CreateDirectory(outputProjectSettingsDirectory);

            foreach (var info in globalgamemanagersFile.table.assetFileInfo)
            {
                if (projectSettingAssetToFileName.TryGetValue((UnityClass)info.curFileType, out var fileName) && fileName == null)
                {
                    continue;
                }
                fileName = fileName ?? Enum.GetName(typeof(UnityClass), info.curFileType);
                var ext = assetsManager.GetExtAsset(globalgamemanagersFile, 0, info.index);
                using (var file = File.Create(Path.Combine(outputProjectSettingsDirectory, $"{fileName}.asset")))
                using (var streamWriter = new InvariantStreamWriter(file))
                {
                    var yamlWriter = new YAMLWriter();
                    foreach (var doc in exportManager.Export(new AssetCollection { Assets = { ext } }, assetsManager))
                    {
                        yamlWriter.AddDocument(doc);
                    }
                    yamlWriter.Write(streamWriter);
                }
            }

            var buildSettings = assetsManager.GetExtAsset(globalgamemanagersFile, 0, globalgamemanagersFile.table.GetAssetsOfType((int)UnityClass.BuildSettings).First().index).instance.GetBaseField();
            File.WriteAllText(Path.Combine(outputProjectSettingsDirectory, "ProjectVersion.txt"), $"m_EditorVersion: {buildSettings.Get("m_Version").GetValue().value.asString}");

            var scenes = buildSettings.Get("scenes")[0];
            for (var i = 0; i < scenes.childrenCount; i++)
            {
                var shaderAssetsFile = assetsManager.LoadAssetsFile(Path.Combine(dataFolder, $"sharedassets{i}.assets"), true);
                var levelFile = assetsManager.LoadAssetsFile(Path.Combine(dataFolder, $"level{i}"), true);

                var occlusionSettings = levelFile.table.GetAssetsOfType((int)UnityClass.OcclusionCullingSettings).FirstOrDefault();
                
            }

            var resourceManager = assetsManager.GetExtAsset(globalgamemanagersFile, 0, globalgamemanagersFile.table.GetAssetsOfType((int)UnityClass.ResourceManager).First().index);

            assetsManager.UnloadAll();
        }
    }
}
