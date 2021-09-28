using AssetsExporter.YAML;
using AssetsTools.NET;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssetsExporter.YAMLExporters
{
    public class GUIDExporter : IYAMLExporter
    {
        public YAMLNode Export(ExportContext context, AssetTypeValueField parentField, AssetTypeValueField field, bool raw = false)
        {
            var node = new YAMLSequenceNode(SequenceStyle.Raw);
            foreach (var child in field.children)
            {
                node.Add(child.GetValue().value.asUInt32);
            }
            return node;
        }
    }
}
