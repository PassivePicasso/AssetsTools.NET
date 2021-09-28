using AssetsExporter.Extensions;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetsExporter.Collection
{
    public class AssetCollection : BaseAssetCollection
    {
        public override string ExportExtension => (UnityClass)(MainAsset?.info.curFileType ?? -1u) == UnityClass.GameObject ? "prefab" : "asset";

        public static AssetCollection CreateAssetCollection(AssetsManager assetsManager, AssetExternal asset)
        {
            var collection = new AssetCollection();

            var rootAsset = AssetsHelpers.GetRootAsset(assetsManager, asset);
            collection.Assets.AddRange(AssetsHelpers.GetAssetWithSubAssets(assetsManager, rootAsset));

            return collection;
        }
    }
}
