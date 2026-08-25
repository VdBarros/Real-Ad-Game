using System;
using System.Collections.Generic;
using Game.Domain;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class TrailBoard : MonoBehaviour
    {
        readonly List<GameObject> dots = new List<GameObject>();

        IReadOnlyList<TrailDot> plan;

        Material paint;

        public int Showing
        {
            get
            {
                var showing = 0;
                foreach (var dot in dots)
                {
                    if (dot != null && dot.activeSelf)
                    {
                        showing++;
                    }
                }

                return showing;
            }
        }

        public void Dress(Material material)
        {
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }

            paint = material;
        }

        public void Show(TileRoute route)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            if (paint == null)
            {
                throw new InvalidOperationException(
                    "The trail wears a material it has neither been dressed with nor outlived. Call Dress.");
            }

            plan = Trail.Along(route);

            while (dots.Count < plan.Count)
            {
                dots.Add(Lay());
            }

            for (var index = 0; index < dots.Count; index++)
            {
                var lit = index < plan.Count;
                dots[index].SetActive(lit);

                if (lit)
                {
                    var position = plan[index].Position;
                    dots[index].transform.localPosition = new Vector3(position.X, position.Y, position.Z);
                }
            }
        }

        public void Follow(float travelled)
        {
            if (plan == null)
            {
                return;
            }

            for (var index = 0; index < plan.Count; index++)
            {
                var lit = !Trail.IsSpent(plan[index], travelled);
                if (dots[index].activeSelf != lit)
                {
                    dots[index].SetActive(lit);
                }
            }
        }

        public void Clear()
        {
            plan = null;

            foreach (var dot in dots)
            {
                dot.SetActive(false);
            }
        }

        GameObject Lay()
        {
            var dot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dot.name = PartNames.Dot(dots.Count);
            dot.transform.SetParent(transform, worldPositionStays: false);
            dot.transform.localScale = new Vector3(Trail.Size, Trail.Size, Trail.Size);
            dot.GetComponent<Renderer>().sharedMaterial = paint;
            WorldObjects.Destroy(dot.GetComponent<Collider>());
            dot.SetActive(false);
            return dot;
        }
    }
}
