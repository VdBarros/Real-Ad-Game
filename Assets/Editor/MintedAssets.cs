using System;
using System.Collections.Generic;
using System.Reflection;
using Game.Flow;
using Game.Interaction;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.EditorTooling
{
    public static class MintedAssets
    {
        static readonly Assembly[] GameAssemblies =
        {
            typeof(WorldBuilder).Assembly,
            typeof(Walker).Assembly,
            typeof(GameLoop).Assembly
        };

        public static List<string> ReleasesTheEditorNeverReaches()
        {
            var unreached = new List<string>();

            foreach (var assembly in GameAssemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!typeof(MonoBehaviour).IsAssignableFrom(type) || type.IsAbstract)
                    {
                        continue;
                    }

                    var release = type.GetMethod(
                        "OnDestroy",
                        BindingFlags.Instance
                            | BindingFlags.Public
                            | BindingFlags.NonPublic
                            | BindingFlags.DeclaredOnly);

                    if (release == null || type.IsDefined(typeof(ExecuteAlways), inherit: false))
                    {
                        continue;
                    }

                    unreached.Add(type.FullName);
                }
            }

            unreached.Sort(StringComparer.Ordinal);

            return unreached;
        }

        public static Dictionary<string, int> Census()
        {
            var tally = new Dictionary<string, int>(StringComparer.Ordinal);

            Gather<Material>(tally);
            Gather<Texture>(tally);
            Gather<Mesh>(tally);

            return tally;
        }

        public static List<string> StrandedSince(Dictionary<string, int> census)
        {
            if (census == null)
            {
                throw new ArgumentNullException(nameof(census));
            }

            var stranded = new List<string>();

            foreach (var entry in Census())
            {
                int held;
                if (!census.TryGetValue(entry.Key, out held))
                {
                    held = 0;
                }

                if (entry.Value > held)
                {
                    stranded.Add(entry.Key + " x" + (entry.Value - held).ToString());
                }
            }

            stranded.Sort(StringComparer.Ordinal);

            return stranded;
        }

        public static List<string> WorldMaterialNames()
        {
            var named = new List<string>();

            foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
            {
                if (PartNames.IsWorldPrefixed(material.name))
                {
                    named.Add(material.name);
                }
            }

            named.Sort(StringComparer.Ordinal);

            return named;
        }

        public static string StraysAmong(IEnumerable<string> named)
        {
            if (named == null)
            {
                throw new ArgumentNullException(nameof(named));
            }

            var styled = new HashSet<string>(StringComparer.Ordinal);

            foreach (PartStyle style in Enum.GetValues(typeof(PartStyle)))
            {
                styled.Add(WorldMaterials.NamePrefix + style);
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var strays = new List<string>();

            foreach (var name in named)
            {
                if (!styled.Contains(name))
                {
                    strays.Add(name + ", which colours no part style");
                    continue;
                }

                if (!seen.Add(name))
                {
                    strays.Add(name + ", which is coloured more than once");
                }
            }

            return strays.Count == 0 ? string.Empty : string.Join("; ", strays.ToArray());
        }

        static void Gather<T>(Dictionary<string, int> tally) where T : UnityEngine.Object
        {
            foreach (var asset in Resources.FindObjectsOfTypeAll<T>())
            {
                if (asset.hideFlags != HideFlags.HideAndDontSave)
                {
                    continue;
                }

                var key = typeof(T).Name + " " + asset.name;

                int held;
                tally.TryGetValue(key, out held);
                tally[key] = held + 1;
            }
        }
    }
}
