using System;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class WorldModels : IDisposable
    {
        public const string ResourcesFolder = "Dungeon";

        public const string AtlasAsset = "dungeon_texture";

        readonly GameObject[] byModel;

        readonly bool[] looked;

        Texture2D atlas;

        bool lookedForTheAtlas;

        bool disposed;

        public WorldModels()
        {
            var models = Enum.GetValues(typeof(PartModel)).Length;
            byModel = new GameObject[models];
            looked = new bool[models];
        }

        public Texture2D Atlas
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException(nameof(WorldModels));
                }

                if (!lookedForTheAtlas)
                {
                    lookedForTheAtlas = true;
                    atlas = Resources.Load<Texture2D>(AtlasPath);

                    if (atlas == null)
                    {
                        UnityEngine.Debug.LogWarning(
                            "The dungeon atlas resolves to nothing loadable at Resources/" + AtlasPath
                            + ", so every mesh that resolves wears a flat tint instead of the pack's texture.");
                    }
                }

                return atlas;
            }
        }

        public static string AtlasPath
        {
            get { return ResourcesFolder + "/" + AtlasAsset; }
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
                        "Part model " + model + " resolves to nothing loadable under Resources/" + ResourcesFolder
                        + ", so every part that wants it falls back to the primitive its part shape names.");
                }
            }

            return byModel[slot];
        }

        public bool Dresses(PartStyle style)
        {
            return Of(PartModels.Of(style)) != null;
        }

        public static string AssetPathOf(PartModel model)
        {
            var asset = AssetNameOf(model);

            return string.IsNullOrEmpty(asset) ? null : ResourcesFolder + "/" + asset;
        }

        static GameObject Load(PartModel model)
        {
            var path = AssetPathOf(model);

            return path == null ? null : Resources.Load<GameObject>(path);
        }

        static string AssetNameOf(PartModel model)
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
            }

            atlas = null;
            lookedForTheAtlas = false;
            disposed = true;
        }
    }
}
