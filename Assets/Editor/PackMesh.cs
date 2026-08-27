using System.Collections.Generic;
using Game.Presentation;
using UnityEngine;

namespace Game.EditorTooling
{
    public static class PackMesh
    {
        public static Mesh On(Renderer renderer)
        {
            var skinned = renderer as SkinnedMeshRenderer;
            if (skinned != null)
            {
                return skinned.sharedMesh;
            }

            var filter = renderer.GetComponent<MeshFilter>();

            return filter == null ? null : filter.sharedMesh;
        }

        public static ISet<Mesh> Of(GameObject prefab)
        {
            var meshes = new HashSet<Mesh>();

            if (prefab == null)
            {
                return meshes;
            }

            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                var mesh = On(renderer);
                if (mesh != null)
                {
                    meshes.Add(mesh);
                }
            }

            return meshes;
        }

        public static Bounds Bare(GameObject prefab)
        {
            var instance = Object.Instantiate(prefab);
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            CharacterDress.Bare(instance);

            var box = Around(instance);

            WorldObjects.Destroy(instance);

            return box;
        }

        public static Bounds Around(GameObject raised)
        {
            var box = new Bounds();
            var first = true;

            foreach (var renderer in raised.GetComponentsInChildren<Renderer>(true))
            {
                var mesh = On(renderer);
                if (mesh == null)
                {
                    continue;
                }

                var here = renderer is SkinnedMeshRenderer
                    ? renderer.bounds
                    : new Bounds(
                        raised.transform.InverseTransformPoint(
                            renderer.transform.TransformPoint(mesh.bounds.center)),
                        Vector3.Scale(mesh.bounds.size, renderer.transform.lossyScale));

                if (first)
                {
                    box = here;
                    first = false;
                }
                else
                {
                    box.Encapsulate(here);
                }
            }

            return box;
        }

        public static Bounds Wearing(Transform instance, ICollection<Mesh> pack)
        {
            var box = new Bounds();
            var first = true;

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var mesh = On(renderer);
                if (mesh == null || !pack.Contains(mesh))
                {
                    continue;
                }

                if (first)
                {
                    box = renderer.bounds;
                    first = false;
                }
                else
                {
                    box.Encapsulate(renderer.bounds);
                }
            }

            return box;
        }
    }
}
