#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

// Explicit maintenance command. Source animation frames and clips are never modified.
public static class TakeShopAtlasBuilder
{
    const string Folder = "Assets/UI/TakeShop/OwnerAtlases";

    [MenuItem("MIS/テスト/ショップ画像の読み込みを軽量化")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("再生を停止してください。");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/UI/TakeShop", "OwnerAtlases");
        EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2;
        var atlases = new List<SpriteAtlas>();
        foreach (string layer in new[] { "HairL", "Hat", "Owner", "OwnerB", "OwnerF" })
        {
            string path = Folder + "/" + layer + ".spriteatlasv2";
            if (!File.Exists(path))
            {
                var asset = new SpriteAtlasAsset();
                var source = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/UI/TakeShop/Owner/" + layer);
                if (source == null) throw new InvalidOperationException("Missing shop animation layer: " + layer);
                asset.Add(new UnityEngine.Object[] { source });
                SpriteAtlasAsset.Save(asset, path);
                AssetDatabase.ImportAsset(path);
            }
            var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(path);
            var packing = importer.packingSettings;
            // UI.Image does not support rotated/tight atlas entries.
            packing.enableRotation = false;
            packing.enableTightPacking = false;
            packing.padding = 4;
            importer.packingSettings = packing;
            var texture = importer.textureSettings;
            texture.readable = false;
            texture.generateMipMaps = false;
            texture.filterMode = FilterMode.Bilinear;
            importer.textureSettings = texture;
            importer.includeInBuild = true;
            var defaults = importer.GetPlatformSettings("DefaultTexturePlatform");
            defaults.maxTextureSize = 4096;
            defaults.format = TextureImporterFormat.Automatic;
            defaults.textureCompression = TextureImporterCompression.CompressedHQ;
            defaults.compressionQuality = 100;
            importer.SetPlatformSettings(defaults);
            var desktop = importer.GetPlatformSettings("Standalone");
            desktop.overridden = true;
            desktop.maxTextureSize = 4096;
            desktop.format = TextureImporterFormat.BC7;
            desktop.compressionQuality = 100;
            importer.SetPlatformSettings(desktop);
            importer.SaveAndReimport();
            atlases.Add(AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path));
        }
        SpriteAtlasUtility.PackAtlases(atlases.ToArray(), EditorUserBuildSettings.activeBuildTarget);
        AssetDatabase.SaveAssets();
        Debug.Log("[ShopAtlas] Packed all five animation layers. Run the Take shop validation to measure load time and verify animation.");
    }
}
#endif
