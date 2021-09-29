using AssetsExporter.Collection;
using AssetsExporter.Meta;
using AssetsExporter.YAML;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssetsExporter
{
    public static class AssetExportManager
    {
        private static readonly Dictionary<AssetClassID, string> projectSettingAssetToFileName = new Dictionary<AssetClassID, string>()
        {
            [AssetClassID.PhysicsManager] = "DynamicsManager",
            [AssetClassID.NavMeshProjectSettings] = "NavMeshAreas",
            [AssetClassID.PlayerSettings] = "ProjectSettings",
            [AssetClassID.BuildSettings] = null,
            [AssetClassID.DelayedCallManager] = null,
            [AssetClassID.MonoManager] = null,
            [AssetClassID.ResourceManager] = null,
            [AssetClassID.RuntimeInitializeOnLoadManager] = null,
            [AssetClassID.ScriptMapper] = null,
            [AssetClassID.StreamingManager] = null,
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
                if (projectSettingAssetToFileName.TryGetValue((AssetClassID)info.curFileType, out var fileName) && fileName == null)
                {
                    continue;
                }
                fileName = fileName ?? Enum.GetName(typeof(AssetClassID), info.curFileType);
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

            var buildSettings = assetsManager.GetExtAsset(globalgamemanagersFile, 0, globalgamemanagersFile.table.GetAssetsOfType((int)AssetClassID.BuildSettings).First().index).instance.GetBaseField();
            File.WriteAllText(Path.Combine(outputProjectSettingsDirectory, "ProjectVersion.txt"), $"m_EditorVersion: {buildSettings.Get("m_Version").GetValue().value.asString}");

            var scenes = buildSettings.Get("scenes")[0];
            for (var i = 0; i < scenes.childrenCount; i++)
            {
                var sceneAssetPath = scenes[i].value.value.asString;
                var shaderAssetsFile = assetsManager.LoadAssetsFile(Path.Combine(dataFolder, $"sharedassets{i}.assets"), true);
                var levelFile = assetsManager.LoadAssetsFile(Path.Combine(dataFolder, $"level{i}"), true);

                var occlusionSettingsInfo = levelFile.table.GetAssetsOfType((int)AssetClassID.OcclusionCullingSettings).FirstOrDefault();
                Guid guid;
                if (occlusionSettingsInfo != null)
                {
                    var occlusionSettings = assetsManager.GetTypeInstance(levelFile, occlusionSettingsInfo);
                    var baseField = occlusionSettings.GetBaseField();
                    var guidField = baseField.Get("m_SceneGUID");
                    guid = new Guid(guidField.children.Select(el => el.value.value.asUInt32).SelectMany(BitConverter.GetBytes).ToArray());
                }
                else
                {
                    guid = HashUtils.GetMD5HashGuid(sceneAssetPath);
                }

                var sceneCollection = new SceneCollection();
                sceneCollection.Assets.AddRange(levelFile.table.assetFileInfo.Select(el => assetsManager.GetExtAsset(levelFile, 0, el.index)));
                var sceneMeta = new MetaFile(sceneCollection, guid);

                var outputScenePath = Path.Combine(outputDirectory, sceneAssetPath);
                
                SaveCollection(exportManager, assetsManager, sceneCollection, sceneMeta, outputScenePath);

#warning TODO: export sharedAssets
            }

            var resourceManager = assetsManager.GetExtAsset(globalgamemanagersFile, 0, globalgamemanagersFile.table.GetAssetsOfType((int)AssetClassID.ResourceManager).First().index);
#warning TODO: export resources

            assetsManager.UnloadAll();

            CreateAssetsSubDirectoriesMeta(outputDirectory);
        }

        private static void SaveCollection(YAMLExportManager exportManager, AssetsManager assetsManager, BaseAssetCollection collection, MetaFile meta, string outputFilePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
            using (var file = File.Create(outputFilePath))
            using (var streamWriter = new InvariantStreamWriter(file))
            {
                var yamlWriter = new YAMLWriter();
                foreach (var doc in exportManager.Export(collection, assetsManager))
                {
                    yamlWriter.AddDocument(doc);
                }
                yamlWriter.Write(streamWriter);
            }
            using (var file = File.Create($"{outputFilePath}.meta"))
            using (var streamWriter = new InvariantStreamWriter(file))
            {
                var yamlWriter = new YAMLWriter
                {
                    IsWriteDefaultTag = false,
                    IsWriteVersion = false
                };
                yamlWriter.AddDocument(meta.ExportYAML());
                yamlWriter.Write(streamWriter);
            }
        }

        private static void CreateAssetsSubDirectoriesMeta(string projectRootPath)
        {
            var assetsPath = Path.Combine(projectRootPath, "Assets");
            if (!Directory.Exists(assetsPath))
            {
                return;
            }
            foreach (var dir in Directory.GetDirectories(assetsPath, "", SearchOption.AllDirectories))
            {
                var relativeDirPath = dir.Substring(0, projectRootPath.Length + 1);
                var metaPath = relativeDirPath + ".meta";
                if (File.Exists(metaPath))
                {
                    continue;
                }

                var meta = new MetaFile(relativeDirPath);
                using (var file = File.Create(metaPath))
                using (var streamWriter = new InvariantStreamWriter(file))
                {
                    var yamlWriter = new YAMLWriter
                    {
                        IsWriteDefaultTag = false,
                        IsWriteVersion = false
                    };
                    yamlWriter.AddDocument(meta.ExportYAML());
                    yamlWriter.Write(streamWriter);
                }
            }
        }
    }
}
