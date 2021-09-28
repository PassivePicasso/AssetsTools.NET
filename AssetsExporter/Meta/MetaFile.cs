using AssetsExporter.Collection;
using AssetsExporter.YAML;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssetsExporter.Meta
{
    public class MetaFile
    {
        public int FileFormatVersion { get; } = 2;
        public Guid Guid { get; private set; }
        public BaseImporter Importer { get; private set; }
        public bool FolderAsset { get; private set; }

        public MetaFile(string relativeFolderPath)
        {
            FolderAsset = true;
            Guid = HashUtils.GetMD5HashGuid(relativeFolderPath);
            Importer = new DefaultImporter();
        }

        public MetaFile(BaseAssetCollection collection) : this(collection, CreateCollectionGuid(collection)) { }

        public MetaFile(BaseAssetCollection collection, Guid guid)
        {
            FolderAsset = false;
            Guid = guid;
            Importer = Activator.CreateInstance(collection.ImporterType) as BaseImporter;
            Importer.AssignCollection(collection);
        }

        public YAMLDocument ExportYAML()
        {
            var doc = new YAMLDocument();
            var root = doc.CreateMappingRoot();

            root.Add("fileFormatVersion", FileFormatVersion);
            root.Add("guid", Guid.ToString("N"));
            if (FolderAsset)
            {
                root.Add("folderAsset", true);
            }
            root.Add(Importer.Name, Importer.ExportYAML());
            return doc;
        }

        private static Guid CreateCollectionGuid(BaseAssetCollection collection)
        {
            var mainAsset = collection.MainAsset;
            if (!mainAsset.HasValue)
            {
                return Guid.Empty;
            }
            return HashUtils.GetMD5HashGuid($"{mainAsset.Value.info.index}{mainAsset.Value.file.name}");
        }
    }
}
