using AssetsExporter.YAML;
using AssetsTools.NET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AssetsExporter.YAMLExporters
{
    public class TypelessDataExporter : IYAMLExporter
    {
        public YAMLNode Export(ExportContext context, AssetTypeValueField parentField, AssetTypeValueField field, bool raw = false)
        {
            var size = field.GetValue().value.asArray.size;
            YAMLNode typelessDataNode = null;
            if (size > 0)
            {
                typelessDataNode = context.Export(parentField, field, false, typeof(TypelessDataExporter));
            }
            else
            {
                var streamData = context.SourceAsset.instance.GetBaseField().GetChildrenList().FirstOrDefault(el => el.templateField.type == "StreamingInfo");
                if (streamData.IsDummy())
                {
                    goto exit;
                }

                var offset = streamData.Get("offset").GetValue().value.asUInt32;
                size = (int)streamData.Get("size").GetValue().value.asUInt32;
                if (size == 0)
                {
                    goto exit;
                }

                var relativePath = streamData.Get("path").GetValue().value.asString;
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    goto exit;
                }

                var path = Path.Combine(Path.GetDirectoryName(context.SourceAsset.file.path), relativePath);
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException($"Couldn't find {relativePath} in {path}");
                }
                using (var file = File.OpenRead(path))
                {
                    var bytes = new byte[size];
                    file.Position = offset;
                    file.Read(bytes, 0, size);
                    typelessDataNode = bytes.ExportYAML();
                }
            }
            exit:
            typelessDataNode = typelessDataNode ?? new YAMLSequenceNode();
            var node = new YAMLScalarNode(size);
            node.AddExtraNamedNodeForParent("_typelessdata", typelessDataNode);
            return node;
        }
    }
}
