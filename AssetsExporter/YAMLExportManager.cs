using AssetsExporter.YAMLExporters;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.Text;
using AssetsExporter.YAML;
using System.Text.RegularExpressions;
using AssetsExporter.Collection;

namespace AssetsExporter
{
    public sealed class YAMLExportManager
    {
        private readonly SortedSet<IRegistrationContext> exporters = new SortedSet<IRegistrationContext>(new RegistrationContextComparer());

        internal YAMLExportManager() { }

        public YAMLNode Export(ExportContext context, AssetTypeValueField parentField, AssetTypeValueField field, bool raw = false, Type ignoreExporterType = null)
        {
            var exporter = PickExporter(context, parentField, field, ignoreExporterType);

            return exporter.Export(context, parentField, field, raw);
        }

        public IEnumerable<YAMLDocument> Export(AssetCollection collection, AssetsManager manager)
        {
            foreach (var asset in collection.Assets)
            {
                var baseField = asset.instance.GetBaseField();
                if (baseField.IsDummy())
                {
                    yield return new YAMLDocument();
                }

                var context = new ExportContext(this, manager, collection, asset);
                var doc = new YAMLDocument();
                var root = doc.CreateMappingRoot();
                root.Tag = asset.info.curFileType.ToString();
                //Maybe use something instead of an index?
                //Though it's definitely unique per asset file which is enough in this case
                root.Anchor = asset.info.index.ToString();

                root.Add(baseField.templateField.type, context.Export(null, baseField));
                yield return doc;
            }
        }

        private IYAMLExporter PickExporter(ExportContext context, AssetTypeValueField parentField, AssetTypeValueField field, Type ignoreExporterType)
        {
            var template = field.templateField;
            foreach (var exporter in exporters)
            {
                if (exporter.ExporterType == ignoreExporterType) continue;
                if (template.hasValue && ((1u << (int)template.valueType - 1) & exporter.ValueType) == 0) continue;
                if (!template.hasValue && exporter.ValueType != 0) continue;
                if (parentField != null)
                {
                    if (!TypeMatch(exporter.ParentTypeNames, parentField.templateField.type)) continue;
                    if (!TypeMatchRegex(exporter.RegexParentTypeNames, parentField.templateField.type)) continue;
                }
                if (!TypeMatch(exporter.TypeNames, template.type)) continue;
                if (!TypeMatchRegex(exporter.RegexTypeNames, template.type)) continue;

                return exporter.ExporterInstance;
            }
            throw new NotSupportedException("Not found suitable exporter");

            bool TypeMatch(HashSet<string> collection, string value)
            {
                if (collection.Count == 0)
                {
                    return true;
                }
                var match = false;
                foreach (var item in collection)
                {
                    if (item == value)
                    {
                        match = true;
                        break;
                    }
                }
                return match;
            }

            bool TypeMatchRegex(HashSet<Regex> collection, string value)
            {
                if (collection.Count == 0)
                {
                    return true;
                }
                var match = false;
                foreach (var item in collection)
                {
                    if (item.IsMatch(value))
                    {
                        match = true;
                        break;
                    }
                }
                return match;
            }
        }

        public static YAMLExportManager CreateDefault()
        {
            return new YAMLExportManager()
                .RegisterExporter<ValueTypeExporter>(x => x
                    .WithPriority(int.MaxValue)
                    .WhenAnyValueType())
                .RegisterExporter<MonoBehaviourExporter>(x => x
                    .WhenTypeName("MonoBehaviour"))
                .RegisterExporter<PPtrExporter>(x => x
                    .WhenTypeNameRegex(/* language=regex */ @"\APPtr<(.*)>\z"))
                .RegisterExporter<ComponentPairExporter>(x => x
                    .WhenTypeName("ComponentPair"))
                .RegisterExporter<TypelessDataExporter>(x => x
                    .WhenTypeName("TypelessData"))
                .RegisterExporter<StreamingInfoExporter>(x => x
                    .WhenTypeName("StreamingInfo"))
                .RegisterExporter<GUIDExporter>(x => x
                    .WhenTypeName("GUID"))
                .RegisterExporter<GenericExporter>(x => x
                    .WithPriority(int.MinValue));
        }

        public YAMLExportManager RegisterExporter<T>(Action<RegistrationContext> action) where T : IYAMLExporter, new()
        {
            var registration = new RegistrationContext() as IRegistrationContext;
            registration.ExporterInstance = new T();
            registration.ExporterType = typeof(T);
            action?.Invoke(registration as RegistrationContext);
            exporters.Add(registration);
            return this;
        }

        private interface IRegistrationContext
        {
            IYAMLExporter ExporterInstance { get; set; }
            Type ExporterType { get; set; }
            int Priority { get; set; }
            HashSet<string> TypeNames { get; }
            HashSet<Regex> RegexTypeNames { get; }
            HashSet<string> ParentTypeNames { get; }
            HashSet<Regex> RegexParentTypeNames { get; }
            uint ValueType { get; set; }
        }

        //Sort by descending
        private class RegistrationContextComparer : IComparer<IRegistrationContext>
        {
            public int Compare(IRegistrationContext x, IRegistrationContext y)
            {
                return x.Priority > y.Priority ? -1 : 1;
            }
        }

        public class RegistrationContext : IRegistrationContext
        {
            Type IRegistrationContext.ExporterType { get; set; }
            IYAMLExporter IRegistrationContext.ExporterInstance { get; set; }
            int IRegistrationContext.Priority { get; set; }
            HashSet<string> IRegistrationContext.TypeNames { get; } = new HashSet<string>();
            HashSet<Regex> IRegistrationContext.RegexTypeNames { get; } = new HashSet<Regex>();
            HashSet<string> IRegistrationContext.ParentTypeNames { get; } = new HashSet<string>();
            HashSet<Regex> IRegistrationContext.RegexParentTypeNames { get; } = new HashSet<Regex>();
            uint IRegistrationContext.ValueType { get; set; }

            private IRegistrationContext ThisIRC => this;

            public RegistrationContext WhenAnyValueType()
            {
                ThisIRC.ValueType = uint.MaxValue;
                return this;
            }

            public RegistrationContext WhenValueType(EnumValueTypes valueType)
            {
                if (valueType != EnumValueTypes.None)
                {
                    ThisIRC.ValueType |= 1u << (int)valueType - 1;
                }
                return this;
            }

            public RegistrationContext WhenTypeName(string typeName)
            {
                ThisIRC.TypeNames.Add(typeName);
                return this;
            }

            public RegistrationContext WhenTypeNameRegex(string typeNameRegex)
            {
                ThisIRC.RegexTypeNames.Add(new Regex(typeNameRegex, RegexOptions.Compiled));
                return this;
            }

            public RegistrationContext WhenParentTypeName(string typeName)
            {
                ThisIRC.ParentTypeNames.Add(typeName);
                return this;
            }

            public RegistrationContext WhenParentTypeNameRegex(string typeNameRegex)
            {
                ThisIRC.RegexParentTypeNames.Add(new Regex(typeNameRegex, RegexOptions.Compiled));
                return this;
            }

            public RegistrationContext WithPriority(int priority)
            {
                ThisIRC.Priority = priority;
                return this;
            }
        }
    }
}
