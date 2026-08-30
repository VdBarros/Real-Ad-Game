using System;
using System.Collections.Generic;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    [ExecuteAlways]
    public sealed class OrbBoard : MonoBehaviour
    {
        const float BurstEdge = 0.9f;

        readonly List<OrbStream> flying = new List<OrbStream>();

        readonly List<GameObject> beads = new List<GameObject>();

        readonly List<int> delivering = new List<int>();

        PlayerFigure player;

        Material paint;

        GameObject burst;

        public event Action<int> Landed;

        public int InFlight
        {
            get { return flying.Count; }
        }

        public bool IsSettled
        {
            get { return flying.Count == 0; }
        }

        public int Landings { get; private set; }

        public int Delivered { get; private set; }

        public int Showing
        {
            get
            {
                var showing = 0;

                foreach (var bead in beads)
                {
                    if (bead != null && bead.activeSelf)
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

        internal void Begin(PlayerFigure figure)
        {
            player = figure;
            flying.Clear();
            Landings = 0;
            Delivered = 0;
            Douse();
            enabled = false;
        }

        public void Launch(WorldPoint deathSite, int gain)
        {
            var stream = OrbStream.From(deathSite, gain);
            if (!stream.IsCarried)
            {
                return;
            }

            flying.Add(stream);
            enabled = true;
        }

        public void Advance(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds), deltaSeconds, "An orb only ever flies forwards.");
            }

            delivering.Clear();

            for (var index = flying.Count - 1; index >= 0; index--)
            {
                var landed = flying[index].HasLanded;
                var moved = flying[index].Advanced(deltaSeconds);
                flying[index] = moved;

                if (!landed && moved.HasLanded)
                {
                    Landings++;
                    Delivered += moved.Gain;
                    delivering.Add(moved.Gain);
                }

                if (moved.IsSpent)
                {
                    flying.RemoveAt(index);
                }
            }

            Draw();
            enabled = !IsSettled;

            foreach (var gain in delivering)
            {
                var landing = Landed;
                if (landing != null)
                {
                    landing(gain);
                }
            }
        }

        void Draw()
        {
            var post = Homing();
            var lit = 0;
            var flare = 0f;

            foreach (var stream in flying)
            {
                flare = stream.Flare > flare ? stream.Flare : flare;

                if (!stream.IsFlying)
                {
                    continue;
                }

                for (var orb = 0; orb < stream.Orbs; orb++)
                {
                    for (var dot = 0; dot <= OrbStream.TrailDots; dot++)
                    {
                        var size = stream.SizeOf(orb, dot);
                        if (size <= 0f)
                        {
                            continue;
                        }

                        Place(lit++, stream.TrailOf(orb, dot, post), size);
                    }
                }
            }

            for (var index = lit; index < beads.Count; index++)
            {
                if (beads[index].activeSelf)
                {
                    beads[index].SetActive(false);
                }
            }

            Flash(post, flare);
        }

        void Place(int index, WorldPoint at, float size)
        {
            while (beads.Count <= index)
            {
                beads.Add(Bead());
            }

            var bead = beads[index];
            bead.transform.localPosition = new Vector3(at.X, at.Y, at.Z);
            bead.transform.localScale = new Vector3(size, size, size);
            bead.SetActive(true);
            Tints.Wash(bead.GetComponent<Renderer>(), OrbStream.Glow);
        }

        void Flash(WorldPoint post, float flare)
        {
            if (flare <= 0f)
            {
                Douse();
                return;
            }

            var lit = Burst();
            var edge = BurstEdge * flare;

            lit.transform.localPosition = new Vector3(post.X, post.Y, post.Z);
            lit.transform.localScale = new Vector3(edge, edge, edge);
            lit.SetActive(true);
            Tints.Wash(lit.GetComponent<Renderer>(), OrbStream.Glow);
        }

        void Douse()
        {
            if (burst != null)
            {
                burst.SetActive(false);
            }
        }

        WorldPoint Homing()
        {
            if (player == null)
            {
                return default(WorldPoint);
            }

            var ground = player.Ground;
            return new WorldPoint(ground.X, ground.Y + OrbStream.Lift, ground.Z);
        }

        GameObject Bead()
        {
            var bead = Raise(PartNames.Orb(beads.Count));
            bead.SetActive(false);
            return bead;
        }

        GameObject Burst()
        {
            if (burst == null)
            {
                burst = Raise(PartNames.OrbBurst);
                burst.SetActive(false);
            }

            return burst;
        }

        GameObject Raise(string name)
        {
            if (paint == null)
            {
                throw new InvalidOperationException(
                    "The orbs wear a material they have neither been dressed with nor outlived. Call Dress.");
            }

            var raised = GameObject.CreatePrimitive(PrimitiveType.Cube);
            raised.name = name;
            raised.transform.SetParent(transform, worldPositionStays: false);
            raised.GetComponent<Renderer>().sharedMaterial = paint;
            WorldObjects.Destroy(raised.GetComponent<Collider>());
            return raised;
        }

        void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Advance(Time.deltaTime);
        }

        void OnDestroy()
        {
            foreach (var bead in beads)
            {
                WorldObjects.Destroy(bead);
            }

            beads.Clear();
            flying.Clear();
            WorldObjects.Destroy(burst);
            burst = null;
            player = null;
            Landed = null;
        }
    }
}
