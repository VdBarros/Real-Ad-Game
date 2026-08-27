using System;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor;

namespace Game.EditorTooling
{
    public sealed class DungeonArtPostprocessor : AssetPostprocessor
    {
        public const string ModelFolder = "Assets/Resources/" + WorldModels.ResourcesFolder + "/";

        public const ModelImporterMeshCompression Compression = ModelImporterMeshCompression.Medium;

        public const int AtlasMaxSize = 1024;

        public override uint GetVersion()
        {
            return 3;
        }

        void OnPreprocessModel()
        {
            var importer = assetImporter as ModelImporter;
            if (importer == null || !Ours(importer.assetPath))
            {
                return;
            }

            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.importVisibility = false;
            importer.importConstraints = false;
            importer.isReadable = false;
            importer.optimizeMeshVertices = true;
            importer.optimizeMeshPolygons = true;
            importer.weldVertices = true;
            importer.meshCompression = Compression;
            importer.useFileScale = true;
            importer.globalScale = DungeonPack.ImportScale;
        }

        void OnPreprocessTexture()
        {
            var importer = assetImporter as TextureImporter;
            if (importer == null || !Ours(importer.assetPath))
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = true;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.anisoLevel = 1;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.maxTextureSize = AtlasMaxSize;
            importer.textureCompression = TextureImporterCompression.Compressed;

            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = AtlasMaxSize;
            android.format = TextureImporterFormat.ASTC_6x6;
            importer.SetPlatformTextureSettings(android);
        }

        static bool Ours(string path)
        {
            return path != null && path.StartsWith(ModelFolder, StringComparison.Ordinal);
        }
    }
}
