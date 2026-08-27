using System;
using System.Collections.Generic;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor;

namespace Game.EditorTooling
{
    public sealed class CharacterArtPostprocessor : AssetPostprocessor
    {
        public const string ModelFolder = "Assets/Resources/" + WorldModels.CharacterFolder + "/";

        public const ModelImporterMeshCompression Compression = ModelImporterMeshCompression.Off;

        public const ModelImporterAnimationType Rig = ModelImporterAnimationType.Generic;

        public const ModelImporterSkinWeights SkinWeights = ModelImporterSkinWeights.Standard;

        public const ModelImporterAnimationCompression AnimationCompression =
            ModelImporterAnimationCompression.Optimal;

        public const float RotationError = 0.5f;

        public const float PositionError = 0.5f;

        public const float ScaleError = 0.5f;

        public const int AtlasMaxSize = 1024;

        public override uint GetVersion()
        {
            return 4;
        }

        void OnPreprocessModel()
        {
            var importer = assetImporter as ModelImporter;
            if (importer == null || !Ours(importer.assetPath))
            {
                return;
            }

            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.animationType = Rig;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.skinWeights = SkinWeights;
            importer.importAnimation = true;
            importer.animationCompression = AnimationCompression;
            importer.animationRotationError = RotationError;
            importer.animationPositionError = PositionError;
            importer.animationScaleError = ScaleError;
            importer.resampleCurves = true;
            importer.removeConstantScaleCurves = true;
            importer.importAnimatedCustomProperties = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.importVisibility = false;
            importer.importConstraints = false;
            importer.optimizeGameObjects = false;
            importer.isReadable = false;
            importer.optimizeMeshVertices = true;
            importer.optimizeMeshPolygons = false;
            importer.weldVertices = false;
            importer.meshCompression = Compression;
            importer.useFileScale = true;
            importer.globalScale = ArtPacks.CastImportScale;
        }

        void OnPreprocessAnimation()
        {
            var importer = assetImporter as ModelImporter;
            if (importer == null || !Ours(importer.assetPath))
            {
                return;
            }

            importer.clipAnimations = Narrowed(importer);
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

        static ModelImporterClipAnimation[] Narrowed(ModelImporter importer)
        {
            var takes = importer.importedTakeInfos;

            if (takes == null || takes.Length == 0)
            {
                return importer.clipAnimations;
            }

            var kept = new List<ModelImporterClipAnimation>(AdventurerClips.Count);

            foreach (var take in takes)
            {
                if (!AdventurerClips.Wants(take.name))
                {
                    continue;
                }

                var loops = AdventurerClips.LoopsOf(take.name);

                kept.Add(new ModelImporterClipAnimation
                {
                    takeName = take.name,
                    name = take.name,
                    firstFrame = (float)Math.Round(take.startTime * take.sampleRate),
                    lastFrame = (float)Math.Round(take.stopTime * take.sampleRate),
                    loopTime = loops,
                    loopPose = loops,
                    lockRootRotation = false,
                    lockRootHeightY = false,
                    lockRootPositionXZ = false,
                    keepOriginalOrientation = true,
                    keepOriginalPositionY = true,
                    keepOriginalPositionXZ = true,
                    wrapMode = loops ? UnityEngine.WrapMode.Loop : UnityEngine.WrapMode.ClampForever
                });
            }

            return kept.ToArray();
        }

        static bool Ours(string path)
        {
            return path != null && path.StartsWith(ModelFolder, StringComparison.Ordinal);
        }
    }
}
