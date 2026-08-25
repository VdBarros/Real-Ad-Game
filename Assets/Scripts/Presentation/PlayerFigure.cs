using System.Collections.Generic;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class PlayerFigure : Figure
    {
        readonly List<GameObject> trophies = new List<GameObject>();

        GameObject weapon;

        WeaponFlight flight;

        WorldPoint dropSite;

        bool dropPending;

        bool landed;

        public bool IsFlying
        {
            get { return weapon != null; }
        }

        public int Carrying
        {
            get { return trophies.Count; }
        }

        public void AwaitWeaponFrom(WorldPoint deathSite)
        {
            dropSite = deathSite;
            dropPending = true;
        }

        internal void Begin(PowerBeat beat)
        {
            landed = beat.HasLanded;
            Wear(beat.Scale, beat.Tint);
            Wield(beat.Look.Trophies);
        }

        internal void Follow(PowerBeat beat, float deltaSeconds)
        {
            if (beat.HasLanded && !landed)
            {
                Launch(beat.Look);
            }

            landed = beat.HasLanded;
            Fly(deltaSeconds);
            Wear(beat.Scale, beat.Tint);

            if (beat.IsSettled)
            {
                Wield(beat.Look.Trophies);
            }
        }

        void Wield(int count)
        {
            while (trophies.Count > count)
            {
                var last = trophies.Count - 1;
                WorldObjects.Destroy(trophies[last]);
                trophies.RemoveAt(last);
            }

            while (trophies.Count < count)
            {
                trophies.Add(Plant(trophies.Count));
            }
        }

        void Launch(PlayerLook look)
        {
            if (!dropPending)
            {
                return;
            }

            dropPending = false;
            WorldObjects.Destroy(weapon);

            flight = WeaponFlight.From(dropSite, MergePoint(look));
            weapon = Forge(PartNames.Weapon, transform.parent);
            weapon.transform.localScale = Vector(Trophy.Size) * look.Scale;
        }

        void Fly(float deltaSeconds)
        {
            if (weapon == null)
            {
                return;
            }

            flight = flight.Advanced(deltaSeconds);

            if (flight.IsSettled)
            {
                WorldObjects.Destroy(weapon);
                weapon = null;
                return;
            }

            var position = flight.Position;
            weapon.transform.localPosition = new Vector3(position.X, position.Y, position.Z);
            weapon.transform.localEulerAngles = new Vector3(0f, flight.Spin, flight.Spin * 0.5f);
        }

        void OnDestroy()
        {
            WorldObjects.Destroy(weapon);
            weapon = null;
        }

        WorldPoint MergePoint(PlayerLook look)
        {
            var standing = transform.localPosition;
            var waist = TileHeight + look.Scale;

            if (look.Trophies < 1)
            {
                return new WorldPoint(standing.x, waist + look.Scale * Trophy.Shoulder, standing.z);
            }

            var slot = Trophy.PositionOf(look.Trophies - 1);
            return new WorldPoint(
                standing.x + slot.X * look.Scale,
                waist + slot.Y * look.Scale,
                standing.z + slot.Z * look.Scale);
        }

        GameObject Plant(int slot)
        {
            var trophy = Forge(PartNames.Trophy(slot), transform);
            trophy.transform.localPosition = Vector(Trophy.PositionOf(slot));
            trophy.transform.localEulerAngles = Vector(Trophy.RotationOf(slot));
            return trophy;
        }

        static GameObject Forge(string name, Transform parent)
        {
            var instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.name = name;
            instance.transform.SetParent(parent, worldPositionStays: false);
            instance.transform.localScale = Vector(Trophy.Size);
            WorldObjects.Destroy(instance.GetComponent<Collider>());
            Tints.Wash(instance.GetComponent<Renderer>(), Trophy.Steel);
            return instance;
        }

        static Vector3 Vector(WorldPoint point)
        {
            return new Vector3(point.X, point.Y, point.Z);
        }
    }
}
