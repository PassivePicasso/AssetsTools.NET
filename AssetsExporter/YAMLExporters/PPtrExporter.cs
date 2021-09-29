using AssetsExporter.Extensions;
using AssetsExporter.YAML;
using AssetsTools.NET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssetsExporter.YAMLExporters
{
    public class PPtrExporter : IYAMLExporter
    {
        public static string FoundExternalDependencies = nameof(FoundExternalDependencies);

        public YAMLNode Export(ExportContext context, AssetTypeValueField parentField, AssetTypeValueField field, bool raw = false)
        {
            var node = new YAMLMappingNode(MappingStyle.Flow);
            var fileID = field.Get("m_FileID").GetValue().value.asInt32;
            var pathID = field.Get("m_PathID").GetValue().value.asInt64;

            if (pathID == 0)
            {
                node.Add("fileID", 0);
                return node;
            }

            if (field.GetName() == "m_Script")
            {
                var scriptAsset = context.AssetsManager.GetExtAsset(context.SourceAsset.file, field);
                var scriptBase = scriptAsset.instance.GetBaseField();

                var className = scriptBase.Get("m_ClassName").GetValue().value.asString;
                var @namespace = scriptBase.Get("m_Namespace").GetValue().value.asString;
                var assemblyName = scriptBase.Get("m_AssemblyName").GetValue().value.asString;
#warning TODO: unity has guids for their extension assemblies in editor folder (ivy.xml files), so better use them
                node.Add("fileID", HashUtils.ComputeScriptFileID(@namespace, className));
                node.Add("guid", HashUtils.GetMD5HashGuid(Path.GetFileNameWithoutExtension(assemblyName)).ToString("N"));
                node.Add("type", 3);
                return node;
            }

            if (fileID != 0)
            {
                var dep = context.SourceAsset.file.file.dependencies.dependencies[fileID - 1];
                if (dep.guid != Guid.Empty)
                {
                    node.Add("fileID", pathID);
                    node.Add("guid", dep.guid.ToString("N"));
                    node.Add("type", 0);
                    return node;
                }
            }
            else if (context.Collection.Assets.Any(el => el.info.index == pathID))
            {
                node.Add("fileID", pathID);
                return node;
            }
            AddFoundDependency(context, fileID, pathID);
            var file = fileID == 0 ? context.SourceAsset.file : context.SourceAsset.file.GetDependency(context.AssetsManager, fileID - 1);
#warning TODO: introduce a way to supply cached map "asset => root asset" instead of calculating root asset each time beacuse it's pretty slow operation
            var rootAsset = AssetsHelpers.GetRootAsset(context.AssetsManager, context.AssetsManager.GetExtAsset(context.SourceAsset.file, fileID, pathID));
            node.Add("fileID", pathID);
            node.Add("guid", HashUtils.GetMD5HashGuid($"{rootAsset.info.index}{file.name}").ToString("N"));
            node.Add("type", 2);
            return node;
        }

        private void AddFoundDependency(ExportContext exportContext, int fileID, long pathID)
        {
            var externalDependencies = exportContext.Info.GetOrAdd<HashSet<(int, long)>>(FoundExternalDependencies);
            externalDependencies.Add((fileID, pathID));
        }
    }
}
