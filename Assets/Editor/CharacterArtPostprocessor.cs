using System;
using System.Collections.Generic;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor;
using UnityEngine;

namespace Game.EditorTooling
{
    public sealed class CharacterArtPostprocessor : AssetPostprocessor
    {
        public const string ModelFolder = "Assets/Resources/" + WorldModels.CharacterFolder + "/";

        public const string ModelExtension = ".fbx";

        public const ModelImporterMeshCompression Compression = ModelImporterMeshCompression.Off;

        public const ModelImporterAnimationType Rig = ModelImporterAnimationType.Generic;

        public const ModelImporterSkinWeights SkinWeights = ModelImporterSkinWeights.Standard;

        public const ModelImporterAnimationCompression AnimationCompression =
            ModelImporterAnimationCompression.Optimal;

        public const float RotationError = 0.5f;

        public const float PositionError = 0.5f;

        public const float ScaleError = 0.5f;

        public const int AtlasMaxSize = 1024;

        static readonly HashSet<string> sets = SetAssets();

        static readonly HashSet<string> skeletons = SkeletonAssets();

        public override uint GetVersion()
        {
            return 8;
        }

        public static bool IsAnimationSet(string path)
        {
            return path != null && sets.Contains(path);
        }

        public static bool KeepsItsOwnTakes(string path)
        {
            return path != null && skeletons.Contains(path);
        }

        public static bool Animated(string path)
        {
            return IsAnimationSet(path) || KeepsItsOwnTakes(path);
        }

        public static ClipTable TableFor(string path)
        {
            if (IsAnimationSet(path))
            {
                return AdventurerClips.Table;
            }

            return KeepsItsOwnTakes(path) ? SkeletonClips.Table : null;
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
            importer.importAnimation = Animated(importer.assetPath);
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
            importer.globalScale = ImportScaleOf(importer.assetPath);

            if (!importer.importAnimation)
            {
                importer.clipAnimations = new ModelImporterClipAnimation[0];
            }
        }

        void OnPreprocessAnimation()
        {
            var importer = assetImporter as ModelImporter;
            if (importer == null || !Animated(importer.assetPath))
            {
                return;
            }

            importer.clipAnimations = Narrowed(importer, TableFor(importer.assetPath));
        }

        void OnPostprocessAnimation(GameObject root, AnimationClip clip)
        {
            var importer = assetImporter as ModelImporter;
            if (importer == null || clip == null || !IsAnimationSet(importer.assetPath))
            {
                return;
            }

            Rebound(clip);
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

        public static int Rebound(AnimationClip clip)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings == null || bindings.Length == 0)
            {
                return 0;
            }

            var curves = new AnimationCurve[bindings.Length];
            var rebound = new EditorCurveBinding[bindings.Length];
            var moved = 0;

            for (var slot = 0; slot < bindings.Length; slot++)
            {
                curves[slot] = AnimationUtility.GetEditorCurve(clip, bindings[slot]);
                rebound[slot] = bindings[slot];

                var path = AnimationSets.Rebound(bindings[slot].path);
                if (string.Equals(path, bindings[slot].path, StringComparison.Ordinal))
                {
                    continue;
                }

                rebound[slot].path = path;
                moved++;
            }

            if (moved == 0)
            {
                return 0;
            }

            for (var slot = 0; slot < bindings.Length; slot++)
            {
                AnimationUtility.SetEditorCurve(clip, bindings[slot], null);
            }

            for (var slot = 0; slot < bindings.Length; slot++)
            {
                AnimationUtility.SetEditorCurve(clip, rebound[slot], curves[slot]);
            }

            return moved;
        }

        static ModelImporterClipAnimation[] Narrowed(ModelImporter importer, ClipTable table)
        {
            var takes = importer.importedTakeInfos;

            if (table == null || takes == null || takes.Length == 0)
            {
                return importer.clipAnimations;
            }

            var kept = new List<ModelImporterClipAnimation>(table.Count);

            foreach (var take in takes)
            {
                if (!table.Wants(take.name))
                {
                    continue;
                }

                var loops = table.LoopsOf(take.name);

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

        public static float ImportScaleOf(string path)
        {
            var named = ModelNamed(path);

            return named == PartModel.None ? ArtPacks.CastImportScale : ArtPacks.ImportScaleFor(named);
        }

        public static PartModel ModelNamed(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return PartModel.None;
            }

            var asset = System.IO.Path.GetFileNameWithoutExtension(path);

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (model != PartModel.None
                    && ArtPacks.ShipsWithTheCast(model)
                    && string.Equals(WorldModels.AssetNameOf(model), asset, StringComparison.Ordinal))
                {
                    return model;
                }
            }

            return PartModel.None;
        }

        static HashSet<string> SetAssets()
        {
            var named = new HashSet<string>(StringComparer.Ordinal);

            foreach (var set in AnimationSets.Assets)
            {
                named.Add(ModelFolder + set + ModelExtension);
            }

            return named;
        }

        static HashSet<string> SkeletonAssets()
        {
            var named = new HashSet<string>(StringComparer.Ordinal);

            foreach (PartModel model in Enum.GetValues(typeof(PartModel)))
            {
                if (model == PartModel.None || ArtPacks.Of(model) != ArtPack.Skeletons)
                {
                    continue;
                }

                named.Add("Assets/Resources/" + WorldModels.AssetPathOf(model) + ModelExtension);
            }

            return named;
        }

        static bool Ours(string path)
        {
            return path != null && path.StartsWith(ModelFolder, StringComparison.Ordinal);
        }
    }
}
