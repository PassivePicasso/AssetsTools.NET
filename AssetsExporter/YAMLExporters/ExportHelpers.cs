using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.Text;
using AssetsExporter.YAML;

namespace AssetsExporter.YAMLExporters
{
    internal class ExportHelpers
    {
        public static YAMLNode ExportArray(ExportContext context, AssetTypeValueField arrayField)
        {
            var childType = arrayField.templateField.children[1].type.ToLower();
            var sequenceStyle = SequenceStyle.Block;
            var cRaw = false;

            if (childType == "uint8")
            {
                sequenceStyle = SequenceStyle.Raw;
                cRaw = true;
            }

            var node = new YAMLSequenceNode(sequenceStyle);

            if (arrayField.childrenCount > 0)
            {
                for (var i = 0; i < arrayField.childrenCount; i++)
                {
                    node.Add(context.Export(arrayField, arrayField.children[i], cRaw));
                }
            }

            return node;
        }
    }
}
