using System.Collections.Generic;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class PlayerFigure : Figure
    {
        readonly List<GameObject> trophies = new List<GameObject>();

        GameObject weapon;

        GameObject held;

        GameObject cloak;

        PlayerWeapon gripped = PlayerWeapon.None;

        bool cloaked;

        bool armed;

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

        public PlayerWeapon Gripping
        {
            get { return gripped; }
        }

        public bool IsCloaked
        {
            get { return cloak != null; }
        }

        public void AwaitWeaponFrom(WorldPoint deathSite)
        {
            dropSite = deathSite;
            dropPending = true;
        }

        internal void Begin(PowerBeat beat)
        {
            landed = beat.HasLanded;
            Wear(beat.Scale);
            Wield(beat.Look.Trophies);
            Arm(beat.Look);
        }

        internal void Follow(PowerBeat beat, float deltaSeconds)
        {
            if (beat.HasLanded && !landed)
            {
                Launch(beat.Look);
            }

            landed = beat.HasLanded;
            Fly(deltaSeconds);
            Wear(beat.Scale);

            if (beat.IsSettled)
            {
                Wield(beat.Look.Trophies);
                Arm(beat.Look);
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

        void Arm(PlayerLook look)
        {
            if (armed && gripped == look.Weapon && cloaked == look.Cloak)
            {
                return;
            }

            if (!armed || gripped != look.Weapon)
            {
                WorldObjects.Destroy(held);
                held = null;
                gripped = look.Weapon;

                if (gripped != PlayerWeapon.None)
                {
                    held = Hang(
                        PartNames.Held(gripped), Grip(), PlayerKit.LimbsOf(gripped), PlayerKit.Steel);
                }
            }

            if (!armed || cloaked != look.Cloak)
            {
                WorldObjects.Destroy(cloak);
                cloak = null;
                cloaked = look.Cloak;

                if (cloaked)
                {
                    cloak = Hang(PartNames.Cloak, transform, PlayerKit.CloakLimbs, PlayerKit.Cloth);
                }
            }

            armed = true;
        }

        Transform Grip()
        {
            var slot = CharacterDress.Hand(gameObject);

            return slot == null ? transform : slot;
        }

        GameObject Hang(
            string name, Transform anchor, IReadOnlyList<PropLimb> limbs, Tint tint)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(anchor, worldPositionStays: false);
            holder.transform.localPosition = Vector3.zero;
            holder.transform.localScale = Vector3.one * UnitUnder(anchor);
            holder.transform.rotation = transform.rotation;

            for (var limb = 0; limb < limbs.Count; limb++)
            {
                Slab(PartNames.Limb(name, limb), holder.transform, limbs[limb], tint);
            }

            return holder;
        }

        float UnitUnder(Transform anchor)
        {
            var mine = transform.lossyScale.x;
            var theirs = anchor.lossyScale.x;

            return theirs <= 1e-6f || mine <= 1e-6f ? CapsuleUnit : mine * CapsuleUnit / theirs;
        }

        static void Slab(string name, Transform parent, PropLimb limb, Tint tint)
        {
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = name;
            slab.transform.SetParent(parent, worldPositionStays: false);
            slab.transform.localPosition = Vector(limb.Offset);
            slab.transform.localEulerAngles = Vector(limb.Rotation);
            slab.transform.localScale = Vector(limb.Size);
            WorldObjects.Destroy(slab.GetComponent<Collider>());
            Tints.Wash(slab.GetComponent<Renderer>(), tint);
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
            held = null;
            cloak = null;
            gripped = PlayerWeapon.None;
            cloaked = false;
            armed = false;
            trophies.Clear();
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
            trophy.transform.localPosition = Vector(Trophy.PositionOf(slot)) * CapsuleUnit;
            trophy.transform.localEulerAngles = Vector(Trophy.RotationOf(slot));
            trophy.transform.localScale = Vector(Trophy.Size) * CapsuleUnit;
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
