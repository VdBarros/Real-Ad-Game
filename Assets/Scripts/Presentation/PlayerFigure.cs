using System.Collections.Generic;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class PlayerFigure : Figure
    {
        readonly List<GameObject> trophies = new List<GameObject>();

        WorldModels library;

        Material skin;

        GameObject weapon;

        GameObject held;

        PlayerWeapon gripped = PlayerWeapon.None;

        bool cloaked;

        bool armed;

        bool stowed;

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

        public bool IsStowed
        {
            get { return stowed; }
        }

        public bool IsCloaked
        {
            get
            {
                var cape = CharacterDress.Cloak(gameObject);

                return cape != null && cape.gameObject.activeSelf;
            }
        }

        public Transform Wielding
        {
            get { return held == null ? null : held.transform; }
        }

        internal void Kit(WorldModels models, Material dressing)
        {
            library = models;
            skin = dressing;
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
                    held = Mount(PartNames.Held(gripped), Grip(), PlayerKit.ModelOf(gripped));
                    Hang();
                }
            }

            if (!armed || cloaked != look.Cloak)
            {
                cloaked = look.Cloak;
                Drape(cloaked);
            }

            armed = true;
        }

        public void Sling(bool away)
        {
            if (stowed == away)
            {
                return;
            }

            stowed = away;
            Hang();

            for (var slot = 0; slot < trophies.Count; slot++)
            {
                Seat(trophies[slot], slot);
            }
        }

        void Hang()
        {
            if (held == null)
            {
                return;
            }

            if (!stowed)
            {
                held.transform.SetParent(Grip(), worldPositionStays: false);
                held.transform.localPosition = Vector3.zero;
                held.transform.localRotation = Quaternion.identity;
                held.transform.localScale = Vector3.one;
                return;
            }

            held.transform.SetParent(transform, worldPositionStays: true);
            held.transform.localPosition =
                Vector(WeaponStow.PoseOf(gripped, RestYaw)) * CapsuleUnit;
            held.transform.localEulerAngles = new Vector3(0f, WeaponStow.LocalYaw(RestYaw), 0f);
        }

        Transform Grip()
        {
            var slot = CharacterDress.Hand(gameObject);

            return slot == null ? transform : slot;
        }

        GameObject Mount(string name, Transform anchor, PartModel model)
        {
            var prefab = library == null ? null : library.Of(model);

            if (prefab == null)
            {
                return null;
            }

            var instance = Instantiate(prefab);
            instance.name = name;
            instance.transform.SetParent(anchor, worldPositionStays: false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            WorldObjects.Destroy(instance.GetComponent<Animator>());

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = skin;
            }

            foreach (var solid in instance.GetComponentsInChildren<Collider>(true))
            {
                WorldObjects.Destroy(solid);
            }

            return instance;
        }

        void Drape(bool worn)
        {
            var cape = CharacterDress.Cloak(gameObject);

            if (cape != null)
            {
                cape.gameObject.SetActive(worn);
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
            held = null;
            library = null;
            skin = null;
            gripped = PlayerWeapon.None;
            cloaked = false;
            armed = false;
            stowed = false;
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
            Seat(trophy, slot);
            trophy.transform.localScale = Vector(Trophy.Size) * CapsuleUnit;
            return trophy;
        }

        void Seat(GameObject trophy, int slot)
        {
            var seat = stowed ? WeaponStow.TrophyOf(slot) : Trophy.PositionOf(slot);

            trophy.transform.localPosition = Vector(seat) * CapsuleUnit;
            trophy.transform.localEulerAngles = Vector(Trophy.RotationOf(slot));
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
