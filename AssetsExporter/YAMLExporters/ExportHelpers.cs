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
            var cRaw = arrayField.templateField.children[1].valueType == EnumValueTypes.UInt8;
            var sequenceStyle = cRaw ? SequenceStyle.Raw : SequenceStyle.Block;
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
