using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Game.Domain;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTooling
{
    public static class FloorMeshCheckCommand
    {
        const long Seed = 20250824L;

        const string FloorMeshPath =
            "Assets/Resources/" + WorldModels.ResourcesFolder + "/floor_tile_large.fbx";

        const string BaseMap = "_BaseMap";

        const string BaseColour = "_BaseColor";

        public static void Check()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var report = new StringBuilder("t-21 floor mesh, ship seed ")
                .Append(Seed.ToString(CultureInfo.InvariantCulture))
                .Append(':');
            var failures = 0;

            using (var models = new WorldModels())
            {
                failures += Resolves(models, report);
                failures += Imported(report);
                failures += Bounds(models, report);
                failures += Atlas(models, report);
                failures += Built(models, report);
            }

            report.Append("\n  t-21: ")
                .Append(failures == 0
                    ? "every assertion above held"
                    : failures + (failures == 1 ? " assertion" : " assertions") + " above failed");

            Debug.Log(report.ToString());

            if (failures > 0)
            {
                Debug.LogError("The floor mesh check failed " + failures + " assertions. Read the report above.");
            }
        }

        static int Resolves(WorldModels models, StringBuilder report)
        {
            var path = WorldModels.AssetPathOf(PartModel.FloorTile);
            var prefab = models.Of(PartModel.FloorTile);
            var failures = 0;

            failures += Assert(
                report,
                prefab != null,
                "the floor part model resolves",
                PartModel.FloorTile + " wants Resources/" + (path ?? "nothing") + " and got "
                + (prefab == null ? "nothing" : prefab.name));

            failures += Assert(
                report,
                models.Dresses(PartStyle.Floor) && models.Dresses(PartStyle.Cleared),
                "cursed and cleared floors resolve to that one mesh",
                "Floor wants " + PartModels.Of(PartStyle.Floor) + ", Cleared wants "
                + PartModels.Of(PartStyle.Cleared));

            var falling = new List<string>();
            foreach (PartStyle style in Enum.GetValues(typeof(PartStyle)))
            {
                if (!models.Dresses(style))
                {
                    falling.Add(style.ToString());
                }
            }

            var styles = Enum.GetValues(typeof(PartStyle)).Length;
            var modelled = 0;

            foreach (PartStyle style in Enum.GetValues(typeof(PartStyle)))
            {
                if (PartModels.Of(style) != PartModel.None)
                {
                    modelled++;
                }
            }

            failures += Assert(
                report,
                falling.Count == styles - modelled,
                "every part style the table names no mesh for still falls back to its primitive",
                falling.Count + " of " + styles + " fall back: " + string.Join(", ", falling.ToArray()));

            return failures;
        }

        static int Imported(StringBuilder report)
        {
            var importer = AssetImporter.GetAtPath(FloorMeshPath) as ModelImporter;

            if (importer == null)
            {
                return Assert(report, false, "the floor mesh has a model importer", FloorMeshPath + " has none");
            }

            var failures = 0;

            failures += Assert(
                report,
                importer.materialImportMode == ModelImporterMaterialImportMode.None,
                "the postprocessor turned material import off",
                "the importer reports " + importer.materialImportMode);
            failures += Assert(
                report,
                !importer.importAnimation && importer.animationType == ModelImporterAnimationType.None,
                "the postprocessor turned animation import off",
                "importAnimation " + importer.importAnimation + ", animationType " + importer.animationType);
            failures += Assert(
                report,
                !importer.importCameras && !importer.importLights,
                "the postprocessor turned camera and light import off",
                "importCameras " + importer.importCameras + ", importLights " + importer.importLights);
            failures += Assert(
                report,
                !importer.importBlendShapes,
                "the postprocessor turned blend shape import off",
                "the importer reports " + importer.importBlendShapes);
            failures += Assert(
                report,
                !importer.isReadable,
                "the postprocessor left the mesh unreadable at runtime",
                "isReadable is " + importer.isReadable);
            failures += Assert(
                report,
                importer.optimizeMeshVertices,
                "the postprocessor optimised the vertices",
                "optimizeMeshVertices is " + importer.optimizeMeshVertices);
            failures += Assert(
                report,
                Math.Abs(importer.globalScale - DungeonPack.ImportScale) < 1e-6f,
                "the postprocessor pinned the global import scale to "
                + DungeonPack.ImportScale.ToString("0.####", CultureInfo.InvariantCulture),
                "the importer reports "
                + importer.globalScale.ToString("0.####", CultureInfo.InvariantCulture)
                + " with useFileScale " + importer.useFileScale);

            report.Append("\n  mesh compression is ").Append(importer.meshCompression);

            return failures;
        }

        static int Bounds(WorldModels models, StringBuilder report)
        {
            var mesh = MeshOf(models);
            if (mesh == null)
            {
                return Assert(
                    report, false, "the floor mesh bounds match the tile edge", "there is no mesh to measure");
            }

            var size = mesh.bounds.size;
            var edge = IsoProjection.TileEdge;
            var failures = 0;

            failures += Assert(
                report,
                Math.Abs(size.x - edge) <= DungeonPack.BoundsEpsilon
                && Math.Abs(size.z - edge) <= DungeonPack.BoundsEpsilon,
                "the imported floor mesh spans one tile edge within "
                + DungeonPack.BoundsEpsilon.ToString("0.####", CultureInfo.InvariantCulture),
                string.Format(
                    CultureInfo.InvariantCulture,
                    "bounds are {0:0.#####} wide by {1:0.#####} deep against a tile edge of {2:0.#####}, "
                    + "from {3:0.###} pack units at an import scale of {4:0.####}",
                    size.x,
                    size.z,
                    edge,
                    DungeonPack.GridUnits,
                    DungeonPack.ImportScale));

            failures += Assert(
                report,
                size.y < edge * 0.25f,
                "the imported floor mesh is a slab rather than a standing panel",
                string.Format(CultureInfo.InvariantCulture, "it is {0:0.#####} tall", size.y));

            return failures;
        }

        static int Atlas(WorldModels models, StringBuilder report)
        {
            var atlas = models.Atlas;

            return Assert(
                report,
                atlas != null,
                "the pack atlas resolves",
                "Resources/" + WorldModels.AtlasPath + " got "
                + (atlas == null ? "nothing" : atlas.name + " at " + atlas.width + " by " + atlas.height));
        }

        static int Built(WorldModels models, StringBuilder report)
        {
            var expected = MeshOf(models);
            var graph = LevelGenerator.Generate(Seed, MazePreset.Ship).Graph;
            var builder = new WorldBuilder();
            var root = builder.Build(graph);
            var failures = 0;

            var world = new List<Material>();
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var material = renderer.sharedMaterial;
                if (material != null
                    && material.name.StartsWith(WorldMaterials.NamePrefix, StringComparison.Ordinal)
                    && !world.Contains(material))
                {
                    world.Add(material);
                }
            }

            var byName = new Dictionary<string, Transform>();
            foreach (var node in root.GetComponentsInChildren<Transform>(true))
            {
                byName[node.name] = node;
            }

            var floors = 0;
            var meshed = 0;
            var collided = 0;
            var flat = 0;
            var quadded = 0;
            var absent = new List<string>();

            foreach (var tile in graph.Tiles.Tiles)
            {
                var name = PartNames.Tile(tile.Position);
                var walksOnItsQuad =
                    LevelBlueprintBuilder.WalkingSurfaceOf(graph.Tiles, tile.Position) == name;

                if (walksOnItsQuad)
                {
                    quadded++;
                }

                Transform node;
                if (!byName.TryGetValue(name, out node))
                {
                    if (walksOnItsQuad)
                    {
                        absent.Add(name);
                    }

                    continue;
                }

                floors++;

                var filter = node.GetComponentInChildren<MeshFilter>();
                if (filter != null && filter.sharedMesh == expected)
                {
                    meshed++;
                }

                if (node.GetComponentInChildren<Collider>() != null)
                {
                    collided++;
                }

                if (Math.Abs(node.localEulerAngles.x) < 0.001f && Math.Abs(node.localEulerAngles.z) < 0.001f)
                {
                    flat++;
                }
            }

            failures += Assert(
                report,
                quadded > 0 && floors == quadded && absent.Count == 0,
                "every tile that walks on its own floor quad has one in the built world, and the tiles "
                + "without one are exactly those footed with a flight",
                floors + " of " + quadded + " quad-walking tiles of " + graph.Tiles.Tiles.Count
                + " carry a quad, " + (graph.Tiles.Tiles.Count - quadded) + " walk on a flight"
                + (absent.Count == 0 ? "" : "; missing " + string.Join(", ", absent.ToArray())));
            failures += Assert(
                report,
                floors > 0 && meshed == floors,
                "every floor quad in the built world carries the pack mesh",
                meshed + " of " + floors + " quads do");
            failures += Assert(
                report,
                floors > 0 && flat == floors,
                "every floor quad lies flat rather than wearing the quad's tilt",
                flat + " of " + floors + " quads do");
            failures += Assert(
                report,
                floors > 0 && collided == floors,
                "every floor quad keeps a collider",
                collided + " of " + floors + " quads do");

            var ceiling = Enum.GetValues(typeof(PartStyle)).Length;

            failures += Assert(
                report,
                world.Count <= ceiling,
                "the built world's material count is within its ceiling of " + ceiling,
                world.Count + " distinct world materials");

            var atlassed = 0;
            var dressed = 0;
            var wrong = new List<string>();

            foreach (PartStyle style in Enum.GetValues(typeof(PartStyle)))
            {
                var material = Named(world, style);
                if (material == null)
                {
                    continue;
                }

                var wants = models.Dresses(style);
                var wears = material.HasProperty(BaseMap) && material.GetTexture(BaseMap) != null;

                if (wants)
                {
                    dressed++;
                }

                if (wears)
                {
                    atlassed++;
                }

                if (wants != wears)
                {
                    wrong.Add(style.ToString());
                }
            }

            failures += Assert(
                report,
                wrong.Count == 0,
                "exactly the world materials whose part style has a mesh bind the atlas as their base map",
                atlassed + " of " + world.Count + " world materials do against " + dressed
                + " dressed styles" + (wrong.Count == 0 ? "" : ", wrong on " + string.Join(", ", wrong.ToArray())));

            var cursed = Named(world, PartStyle.Floor);
            var cleared = Named(world, PartStyle.Cleared);
            var shared = cursed != null
                && cleared != null
                && cursed.GetTexture(BaseMap) != null
                && cursed.GetTexture(BaseMap) == cleared.GetTexture(BaseMap);
            var tinted = cursed != null
                && cleared != null
                && cursed.GetColor(BaseColour) != cleared.GetColor(BaseColour);

            failures += Assert(
                report,
                shared && tinted,
                "cursed and cleared share the atlas and stay distinct by base colour alone",
                "same atlas " + shared + ", different tint " + tinted);

            WorldObjects.Destroy(root);
            builder.Dispose();

            return failures;
        }

        static Mesh MeshOf(WorldModels models)
        {
            var prefab = models.Of(PartModel.FloorTile);
            if (prefab == null)
            {
                return null;
            }

            var filter = prefab.GetComponentInChildren<MeshFilter>();

            return filter == null ? null : filter.sharedMesh;
        }

        static Material Named(IEnumerable<Material> materials, PartStyle style)
        {
            var wanted = WorldMaterials.NamePrefix + style;

            foreach (var material in materials)
            {
                if (material.name == wanted)
                {
                    return material;
                }
            }

            return null;
        }

        static int Assert(StringBuilder report, bool held, string claim, string detail)
        {
            report.Append("\n  ").Append(held ? "ok   " : "FAIL ").Append(claim).Append(" - ").Append(detail);

            return held ? 0 : 1;
        }
    }
}
