using System;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Flow
{
    public sealed class PillarCutscene : ICutscene
    {
        public const string RootName = "PillarCutscene";

        public const string GroundName = "Ground";

        public const string HeartName = "Heart";

        public const string PortalName = "Portal";

        public const string PlayerName = "Player";

        public const string GirlName = "Girl";

        public const string RivalName = "Rival";

        public const string PillarSuffix = "_Pillar";

        public const string FigureSuffix = "_Figure";

        public const string BadgeSuffix = "_Badge";

        const float PillarWidth = 1.1f;

        const float GroundReach = 26f;

        const float GroundDepth = 0.2f;

        const float HeartSize = 0.3f;

        const float PortalWidth = 1.9f;

        const float PortalLip = 0.05f;

        static readonly Tint HeartTint = new Tint(0.96f, 0.42f, 0.62f);

        sealed class Mover
        {
            public Transform Pillar;

            public Transform Figure;

            public Renderer Skin;

            public NumberBadge Badge;
        }

        readonly WorldMaterials materials = new WorldMaterials();

        readonly BadgeAssets badges = new BadgeAssets();

        PillarReel reel = PillarReel.Opening;

        GameObject root;

        CameraRig rig;

        Mover player;

        Mover girl;

        Mover rival;

        Transform heart;

        Transform portal;

        bool playing;

        bool spent;

        public bool IsPlaying
        {
            get { return playing; }
        }

        public PillarReel Reel
        {
            get { return reel; }
        }

        public GameObject Root
        {
            get { return root; }
        }

        public void Play()
        {
            if (spent)
            {
                throw new InvalidOperationException("A cutscene opens the game once, and this one has already run.");
            }

            reel = PillarReel.Opening;
            playing = true;
            spent = true;
            Build();
            Draw();
        }

        public void Skip()
        {
            if (!playing)
            {
                return;
            }

            reel = reel.Skipped();
            Close();
        }

        public void Advance(float deltaSeconds)
        {
            if (!playing)
            {
                return;
            }

            if (Interrupted())
            {
                Skip();
                return;
            }

            reel = reel.Advanced(deltaSeconds);

            if (reel.IsOver)
            {
                Close();
                return;
            }

            Draw();
        }

        static bool Interrupted()
        {
            var pointer = Pointer.current;
            if (pointer != null && pointer.press.wasPressedThisFrame)
            {
                return true;
            }

            var keyboard = Keyboard.current;
            return keyboard != null && keyboard.anyKey.wasPressedThisFrame;
        }

        void Build()
        {
            root = new GameObject(RootName);
            rig = UnityEngine.Object.FindAnyObjectByType<CameraRig>();

            var ground = Raise(PrimitiveType.Cube, GroundName, root.transform, PartStyle.Floor);
            ground.localScale = new Vector3(GroundReach, GroundDepth, GroundReach);
            ground.localPosition = new Vector3(0f, -GroundDepth * 0.5f, 0f);

            var plan = PillarStage.Plan;
            player = Cast(PlayerName, reel.Player, plan);
            girl = Cast(GirlName, reel.Girl, plan);
            rival = Cast(RivalName, reel.Rival, plan);

            heart = Raise(PrimitiveType.Sphere, HeartName, root.transform, PartStyle.Spark);
            heart.localScale = new Vector3(HeartSize, HeartSize, HeartSize);
            Tints.Wash(heart.GetComponent<Renderer>(), HeartTint);
            heart.gameObject.SetActive(false);

            portal = Raise(PrimitiveType.Cylinder, PortalName, root.transform, PartStyle.Start);
            portal.gameObject.SetActive(false);
        }

        Mover Cast(string name, CastMark mark, BadgePlan plan)
        {
            var group = new GameObject(name);
            group.transform.SetParent(root.transform, worldPositionStays: false);

            var pillar = Raise(PrimitiveType.Cylinder, name + PillarSuffix, group.transform, PartStyle.Pillar);
            var figure = Raise(PrimitiveType.Capsule, name + FigureSuffix, group.transform, PartStyle.Start);

            var part = new BadgePart(
                name + BadgeSuffix,
                0,
                0,
                mark.Badge,
                mark.Number,
                plan.Capacity,
                new WorldPoint(0f, 0f, 0f),
                IsoProjection.CameraRotation);

            return new Mover
            {
                Pillar = pillar,
                Figure = figure,
                Skin = figure.GetComponent<Renderer>(),
                Badge = BadgeFactory.Raise(part, plan, badges, group.transform)
            };
        }

        Transform Raise(PrimitiveType shape, string name, Transform parent, PartStyle style)
        {
            var instance = GameObject.CreatePrimitive(shape);
            instance.name = name;
            instance.transform.SetParent(parent, worldPositionStays: false);
            instance.GetComponent<Renderer>().sharedMaterial = materials.Of(style);
            WorldObjects.Destroy(instance.GetComponent<Collider>());
            return instance.transform;
        }

        void Draw()
        {
            if (rig != null)
            {
                rig.Hold(reel.Framing);
            }

            Dress(player, reel.Player);
            Dress(girl, reel.Girl);
            Dress(rival, reel.Rival);

            var flying = reel.HeartIsFlying;
            heart.gameObject.SetActive(flying);

            if (flying)
            {
                var carried = reel.HeartPosition;
                heart.localPosition = new Vector3(carried.X, carried.Y, carried.Z);
            }

            var open = reel.PortalOpen;
            portal.gameObject.SetActive(open > 0f);

            if (open <= 0f)
            {
                return;
            }

            var mouth = reel.Player.PillarBase;
            portal.localPosition = new Vector3(mouth.X, PortalLip, mouth.Z);
            portal.localScale = new Vector3(PortalWidth * open, PortalLip, PortalWidth * open);
        }

        static void Dress(Mover mover, CastMark mark)
        {
            var height = mark.PillarHeight;
            var seat = mark.PillarBase;

            mover.Pillar.localScale = new Vector3(PillarWidth, height * 0.5f, PillarWidth);
            mover.Pillar.localPosition = new Vector3(seat.X, height * 0.5f, seat.Z);

            var scale = mark.Scale;
            var stand = mark.Position;

            mover.Figure.localScale = new Vector3(scale, scale, scale);
            mover.Figure.localPosition = new Vector3(stand.X, stand.Y + scale, stand.Z);
            Tints.Wash(mover.Skin, mark.Tint);

            mover.Badge.Show(mark.Number);
            mover.Badge.transform.localPosition = new Vector3(
                stand.X, BadgeMetrics.AnchorAbove(stand.Y + scale * 2f), stand.Z);
        }

        void Close()
        {
            playing = false;

            if (root != null)
            {
                root.SetActive(false);
                WorldObjects.Destroy(root);
                root = null;
            }

            player = null;
            girl = null;
            rival = null;
            heart = null;
            portal = null;
            rig = null;

            materials.Dispose();
            badges.Dispose();
        }
    }
}
