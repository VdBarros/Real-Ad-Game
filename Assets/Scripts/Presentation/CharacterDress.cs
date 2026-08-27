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
                    carried.Add(node.GetChild(slot).gameObject);
                }
            }

            foreach (var accessory in carried)
            {
                accessory.SetActive(false);
                WorldObjects.Destroy(accessory);
            }

            return carried.Count;
        }
    }
}
