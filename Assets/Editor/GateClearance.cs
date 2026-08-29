using System.Collections.Generic;
using UnityEngine;

namespace Game.EditorTooling
{
    public static class GateClearance
    {
        public static void Gather(Transform instance, Transform frame, List<Vector3[]> triangles)
        {
            foreach (var filter in instance.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                var vertices = mesh.vertices;
                var indices = mesh.triangles;

                for (var index = 0; index + 2 < indices.Length; index += 3)
                {
                    triangles.Add(new[]
                    {
                        Seen(filter.transform, frame, vertices[indices[index]]),
                        Seen(filter.transform, frame, vertices[indices[index + 1]]),
                        Seen(filter.transform, frame, vertices[indices[index + 2]])
                    });
                }
            }
        }

        static Vector3 Seen(Transform mesh, Transform frame, Vector3 vertex)
        {
            var world = mesh.TransformPoint(vertex);

            return frame == null ? world : frame.InverseTransformPoint(world);
        }

        public static bool Blocked(List<Vector3[]> triangles, Vector3 centre, Vector3 half)
        {
            foreach (var triangle in triangles)
            {
                if (Touches(triangle, centre, half))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool Touches(Vector3[] triangle, Vector3 centre, Vector3 half)
        {
            var a = triangle[0] - centre;
            var b = triangle[1] - centre;
            var c = triangle[2] - centre;

            var edges = new[] { b - a, c - b, a - c };
            var units = new[] { Vector3.right, Vector3.up, Vector3.forward };

            foreach (var edge in edges)
            {
                foreach (var unit in units)
                {
                    var axis = Vector3.Cross(unit, edge);

                    if (axis.sqrMagnitude > 0f && Apart(a, b, c, half, axis))
                    {
                        return false;
                    }
                }
            }

            foreach (var unit in units)
            {
                if (Apart(a, b, c, half, unit))
                {
                    return false;
                }
            }

            var normal = Vector3.Cross(edges[0], edges[1]);

            return normal.sqrMagnitude <= 0f || !Apart(a, a, a, half, normal);
        }

        static bool Apart(Vector3 a, Vector3 b, Vector3 c, Vector3 half, Vector3 axis)
        {
            var one = Vector3.Dot(axis, a);
            var other = Vector3.Dot(axis, b);
            var third = Vector3.Dot(axis, c);
            var reach = Mathf.Abs(axis.x) * half.x + Mathf.Abs(axis.y) * half.y + Mathf.Abs(axis.z) * half.z;

            return Mathf.Min(one, Mathf.Min(other, third)) > reach
                || Mathf.Max(one, Mathf.Max(other, third)) < -reach;
        }
    }
}
