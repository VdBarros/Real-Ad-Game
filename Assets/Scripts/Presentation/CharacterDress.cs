using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public static class CharacterDress
    {
        public static int Bare(GameObject instance)
        {
            if (instance == null)
            {
                return 0;
            }

            var carried = new List<GameObject>();

            foreach (var node in instance.GetComponentsInChildren<Transform>(true))
            {
                if (!node.name.StartsWith(ArtPacks.CastSlotNode, StringComparison.Ordinal))
                {
                    continue;
                }

                for (var slot = 0; slot < node.childCount; slot++)
                {
                    var accessory = node.GetChild(slot).gameObject;

                    if (!PartNames.IsWorn(accessory.name))
                    {
                        carried.Add(accessory);
                    }
                }
            }

            foreach (var accessory in carried)
            {
                accessory.SetActive(false);
                WorldObjects.Destroy(accessory);
            }

            return carried.Count;
        }

        public static Transform Cloak(GameObject instance)
        {
            return Under(instance, AdventurerPack.CloakNode);
        }

        public static Transform Hand(GameObject instance)
        {
            return Under(instance, ArtPacks.CastSlotNode);
        }

        static Transform Under(GameObject instance, string node)
        {
            if (instance == null)
            {
                return null;
            }

            foreach (var found in instance.GetComponentsInChildren<Transform>(true))
            {
                if (found.name.StartsWith(node, StringComparison.Ordinal))
                {
                    return found;
                }
            }

            return null;
        }
    }
}
