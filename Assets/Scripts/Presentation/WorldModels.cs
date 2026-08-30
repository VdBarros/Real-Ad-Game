using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class WorldModels : IDisposable
    {
        public const string ResourcesFolder = "Dungeon";

        public const string CharacterFolder = "Characters";

        public const string AtlasAsset = "dungeon_texture";

        public const string CharacterAtlasAsset = "knight_texture";

        public const string SkeletonAtlasAsset = "skeleton_texture";

        public const string WeaponAtlasAsset = "weapons_bits_texture";

        readonly GameObject[] byModel;

        readonly bool[] looked;

        readonly Texture2D[] byPack;

        readonly bool[] lookedForAnAtlas;

        readonly Dictionary<string, AnimationClip>[] clipsByModel;

        readonly ClipComplaints complaints = new ClipComplaints();

        bool disposed;

        public WorldModels()
        {
            var models = Enum.GetValues(typeof(PartModel)).Length;
            byModel = new GameObject[models];
            looked = new bool[models];
            clipsByModel = new Dictionary<string, AnimationClip>[models];

            var packs = Enum.GetValues(typeof(ArtPack)).Length;
            byPack = new Texture2D[packs];
            lookedForAnAtlas = new bool[packs];
        }

        public Texture2D Atlas
        {
            get { return AtlasOf(ArtPack.Dungeon); }
        }

        public static string AtlasPath
        {
            get { return AtlasPathOf(ArtPack.Dungeon); }
        }

        public Texture2D AtlasOf(ArtPack pack)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(WorldModels));
            }

            var slot = (int)pack;
            if (!lookedForAnAtlas[slot])
            {
                lookedForAnAtlas[slot] = true;
                byPack[slot] = Resources.Load<Texture2D>(AtlasPathOf(pack));

                if (byPack[slot] == null)
                {
                    UnityEngine.Debug.LogWarning(
                        "The " + pack + " atlas resolves to nothing loadable at Resources/" + AtlasPathOf(pack)
                        + ", so every mesh that resolves wears a flat tint instead of the pack's texture.");
                }
            }

            return byPack[slot];
        }

        public Texture2D AtlasFor(PartModel model)
        {
            return model == PartModel.None ? null : AtlasOf(ArtPacks.Of(model));
        }

        public static string AtlasPathOf(ArtPack pack)
        {
            switch (pack)
            {
                case ArtPack.Dungeon:
                    return ResourcesFolder + "/" + AtlasAsset;
                case ArtPack.Adventurers:
                    return CharacterFolder + "/" + CharacterAtlasAsset;
                case ArtPack.Skeletons:
                    return CharacterFolder + "/" + SkeletonAtlasAsset;
                case ArtPack.Weapons:
                    return CharacterFolder + "/" + WeaponAtlasAsset;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pack), pack, "No atlas for that pack.");
            }
        }

        public GameObject Of(PartModel model)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(WorldModels));
            }

            if (model == PartModel.None)
            {
                return null;
            }

            var slot = (int)model;
            if (!looked[slot])
            {
                looked[slot] = true;
                byModel[slot] = Load(model);

                if (byModel[slot] == null)
                {
                    UnityEngine.Debug.LogWarning(
                        "Part model " + model + " resolves to nothing loadable under Resources/"
                        + FolderOf(model)
                        + ", so every part that wants it falls back to the primitive its part shape names.");
                }
            }

            return byModel[slot];
        }

        public AnimationClip ClipOf(PartModel model, FigureAct act)
        {
            return ClipOf(model, CastClips.NameOf(model, act));
        }

        public AnimationClip ClipOf(PartModel model, string clip)
        {
            var table = Table(model);

            if (table == null || string.IsNullOrEmpty(clip))
            {
                return null;
            }

            AnimationClip found;
            if (table.TryGetValue(clip, out found))
            {
                return found;
            }

            if (complaints.ShouldSay(model + "/" + clip))
            {
                UnityEngine.Debug.LogWarning(
                    "Animation clip " + clip + " resolves to nothing under Resources/"
                    + string.Join(", Resources/", ClipPathsOf(model))
                    + ", where " + table.Count
                    + " clips did load, so every figure wearing that mesh holds its static pose whenever "
                    + clip + " is called for.");
            }

            return null;
        }

        public int ClipCountOf(PartModel model)
        {
            var table = Table(model);

            return table == null ? 0 : table.Count;
        }

        Dictionary<string, AnimationClip> Table(PartModel model)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(WorldModels));
            }

            if (model == PartModel.None)
            {
                return null;
            }

            var slot = (int)model;
            if (clipsByModel[slot] == null)
            {
                clipsByModel[slot] = Clips(model);
            }

            return clipsByModel[slot];
        }

        public bool Dresses(PartStyle style)
        {
            if (!CharacterCast.IsRole(style))
            {
                return Of(PartModels.Of(style)) != null;
            }

            var worn = CharacterCast.MeshesOf(style);

            for (var slot = 0; slot < worn.Count; slot++)
            {
                if (Of(worn[slot]) == null)
                {
                    return false;
                }
            }

            return worn.Count > 0;
        }

        public PartModel Worn(PartModel wanted)
        {
            return Of(wanted) == null ? PartModel.None : wanted;
        }

        public static string AssetPathOf(PartModel model)
        {
            var asset = AssetNameOf(model);

            return string.IsNullOrEmpty(asset) ? null : FolderOf(model) + "/" + asset;
        }

        public static string FolderOf(PartModel model)
        {
            return ArtPacks.ShipsWithTheCast(model) ? CharacterFolder : ResourcesFolder;
        }

        public static string[] ClipPathsOf(PartModel model)
        {
            if (!ArtPacks.IsRiggedCharacter(model))
            {
                return new string[0];
            }

            if (ArtPacks.Of(model) != ArtPack.Adventurers)
            {
                var own = AssetPathOf(model);

                return own == null ? new string[0] : new[] { own };
            }

            var sets = AnimationSets.Assets;
            var paths = new string[sets.Count];

            for (var slot = 0; slot < sets.Count; slot++)
            {
                paths[slot] = CharacterFolder + "/" + sets[slot];
            }

            return paths;
        }

        static Dictionary<string, AnimationClip> Clips(PartModel model)
        {
            var table = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);

            foreach (var path in ClipPathsOf(model))
            {
                foreach (var clip in Resources.LoadAll<AnimationClip>(path))
                {
                    if (clip != null)
                    {
                        table[clip.name] = clip;
                    }
                }
            }

            return table;
        }

        static GameObject Load(PartModel model)
        {
            var path = AssetPathOf(model);

            return path == null ? null : Resources.Load<GameObject>(path);
        }

        public static string AssetNameOf(PartModel model)
        {
            switch (model)
            {
                case PartModel.None:
                    return null;
                case PartModel.FloorTile:
                    return "floor_tile_large";
                case PartModel.WallPanel:
                    return "barrier";
                case PartModel.Chest:
                    return "chest";
                case PartModel.CoinStack:
                    return "coin_stack_large";
                case PartModel.Staircase:
                    return "stairs_narrow";
                case PartModel.Foundation:
                    return "floor_foundation_allsides";
                case PartModel.Pillar:
                    return "pillar";
                case PartModel.Candle:
                    return "candle";
                case PartModel.Column:
                    return "column";
                case PartModel.TorchLit:
                    return "torch_lit";
                case PartModel.BarrelLarge:
                    return "barrel_large";
                case PartModel.CratesStacked:
                    return "crates_stacked";
                case PartModel.SwordShield:
                    return "sword_shield";
                case PartModel.Knight:
                    return "Knight";
                case PartModel.SkeletonMinion:
                    return "Skeleton_Minion";
                case PartModel.SkeletonRogue:
                    return "Skeleton_Rogue";
                case PartModel.SkeletonWarrior:
                    return "Skeleton_Warrior";
                case PartModel.SkeletonMage:
                    return "Skeleton_Mage";
                case PartModel.Sword1Handed:
                    return "sword_1handed";
                case PartModel.Axe2Handed:
                    return "axe_2handed";
                case PartModel.Staff:
                    return "staff";
                case PartModel.Sword2Handed:
                    return "sword_2handed";
                case PartModel.SwordA:
                    return "sword_A";
                case PartModel.AxeB:
                    return "axe_B";
                case PartModel.StaffA:
                    return "staff_A";
                case PartModel.StaffB:
                    return "staff_B";
                case PartModel.BowA:
                    return "bow_A_withString";
                default:
                    throw new ArgumentOutOfRangeException(nameof(model), model, "No asset name for that part model.");
            }
        }

        public void Dispose()
        {
            for (var slot = 0; slot < byModel.Length; slot++)
            {
                byModel[slot] = null;
                looked[slot] = false;
                clipsByModel[slot] = null;
            }

            complaints.Forget();

            for (var slot = 0; slot < byPack.Length; slot++)
            {
                byPack[slot] = null;
                lookedForAnAtlas[slot] = false;
            }

            disposed = true;
        }
    }
}
