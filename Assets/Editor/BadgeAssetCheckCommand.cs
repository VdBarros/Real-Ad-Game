using System.Collections.Generic;
using System.Globalization;
using Game.Domain;
using Game.Presentation;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTooling
{
    public static class BadgeAssetCheckCommand
    {
        const long FirstSeed = 20250824L;

        const long SecondSeed = 20250825L;

        public static void Check()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var builder = new WorldBuilder();

            var first = builder.Build(LevelGenerator.Generate(FirstSeed, MazePreset.Ship).Graph);
            var firstSprites = SpritesOn(first);
            var firstMaterials = MaterialsOn(first);
            WorldObjects.Destroy(first);

            var second = builder.Build(LevelGenerator.Generate(SecondSeed, MazePreset.Ship).Graph);
            var secondSprites = SpritesOn(second);
            var secondMaterials = MaterialsOn(second);

            firstSprites.AddRange(secondSprites);
            firstMaterials.AddRange(secondMaterials);

            Debug.Log(string.Format(
                CultureInfo.InvariantCulture,
                "badge assets: {0} badges over two levels share {1} sprites and {2} materials",
                secondSprites.Count,
                Distinct(firstSprites),
                Distinct(firstMaterials)));

            var sprite = secondSprites[0];
            var material = secondMaterials[0];

            WorldObjects.Destroy(second);
            Debug.Log("with the level destroyed the sprite is " + Fate(sprite)
                + " and the material is " + Fate(material));

            builder.Dispose();
            Debug.Log("with the builder disposed the sprite is " + Fate(sprite)
                + " and the material is " + Fate(material));
        }

        static string Fate(Object asset)
        {
            return asset == null ? "gone" : "still alive";
        }

        static List<Sprite> SpritesOn(GameObject root)
        {
            var sprites = new List<Sprite>();
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sprites.Add(renderer.sprite);
            }

            return sprites;
        }

        static List<Material> MaterialsOn(GameObject root)
        {
            var materials = new List<Material>();
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                materials.Add(renderer.sharedMaterial);
            }

            return materials;
        }

        static int Distinct<T>(List<T> assets) where T : Object
        {
            var seen = new List<T>();
            foreach (var asset in assets)
            {
                if (!seen.Contains(asset))
                {
                    seen.Add(asset);
                }
            }

            return seen.Count;
        }
    }
}
