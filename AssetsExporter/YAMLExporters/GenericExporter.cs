using AssetsExporter.YAML.Utils.Extensions;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.Text;
using AssetsExporter.YAML;

namespace AssetsExporter.YAMLExporters
{
    public class GenericExporter : IYAMLExporter
    {
        public YAMLNode Export(ExportContext context, AssetTypeValueField parentField, AssetTypeValueField field, bool raw = false)
        {
            if (field.templateField.valueType != EnumValueTypes.None)
            {
                throw new NotSupportedException("Value types are not supported for this exporter");
            }

            if (field.templateField.isArray)
            {
                return ExportHelpers.ExportArray(context, field);
            }

            if (field.childrenCount == 1 && field.children[0].templateField.isArray)
            {
                if (field.templateField.type == "map")
                {
                    var node = new YAMLMappingNode();
                    var arrayChild = field.children[0];

                    if (arrayChild.childrenCount > 0)
                    {
                        for (var i = 0; i < arrayChild.childrenCount; i++)
                        {
                            var elem = arrayChild.children[i];
                            node.Add(context.Export(arrayChild, elem.children[0]), context.Export(arrayChild, elem.children[1]));
                        }
                    }

                    return node;
                }
                return ExportHelpers.ExportArray(context, field.children[0]);
            }

            if (field.childrenCount > 0)
            {
                var node = new YAMLMappingNode();
                node.AddSerializedVersion(field.templateField.version);
                for (var i = 0; i < field.childrenCount; i++)
                {
                    var child = field.children[i];
                    node.Add(child.templateField.name, context.Export(field, child));
                }
                return node;
            }

            return new YAMLMappingNode();
        }
    }
}
