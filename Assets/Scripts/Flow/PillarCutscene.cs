using System;
using System.Collections.Generic;
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

        public const string MeshSuffix = "_Mesh";

        const float HeartSize = 0.3f;

        const float PortalWidth = 1.9f;

        const float PortalLip = 0.05f;

        static readonly Tint HeartTint = new Tint(0.96f, 0.42f, 0.62f);

        sealed class Costume
        {
            public CastLook Look;

            public PartModel Mesh;

            public Transform Figure;

            public FigureAnimator Acting;

            public Renderer[] Skins;
        }

        sealed class Actor
        {
            public PillarRole Role;

            public Transform Pillar;

            public Costume[] Costumes;

            public NumberBadge Badge;
        }

        readonly WorldModels models = new WorldModels();

        readonly WorldMaterials materials;

        readonly BadgeAssets badges = new BadgeAssets();

        PillarReel reel = PillarReel.Opening;

        GameObject root;

        CameraRig rig;

        Actor[] actors;

        Transform ground;

        Transform heart;

        Transform portal;

        bool playing;

        bool spent;

        public PillarCutscene()
        {
            materials = new WorldMaterials(models);
        }

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

        public Transform Ground
        {
            get { return ground; }
        }

        public Transform PillarOf(PillarRole role)
        {
            var actor = Cast(role);

            return actor == null ? null : actor.Pillar;
        }

        public Transform FigureOf(PillarRole role)
        {
            var worn = Wearing(role);

            return worn == null ? null : worn.Figure;
        }

        public FigureAnimator ActingOf(PillarRole role)
        {
            var worn = Wearing(role);

            return worn == null ? null : worn.Acting;
        }

        public PartModel WornBy(PillarRole role)
        {
            var worn = Wearing(role);

            return worn == null ? PartModel.None : worn.Mesh;
        }

        public NumberBadge BadgeOf(PillarRole role)
        {
            var actor = Cast(role);

            return actor == null ? null : actor.Badge;
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

            ground = Slab(GroundName, root.transform, PartStyle.Floor);
            ground.localScale = Vector(PillarDress.GroundScale);
            ground.localPosition = new Vector3(0f, -PillarDress.GroundDepth, 0f);

            var plan = PillarStage.Plan;
            var raised = new List<Actor>();

            foreach (var role in PillarDress.Roles)
            {
                raised.Add(Cast(role, PillarDress.MarkOf(reel, role), plan));
            }

            actors = raised.ToArray();

            heart = Raise(PrimitiveType.Sphere, HeartName, root.transform, PartStyle.Spark);
            heart.localScale = new Vector3(HeartSize, HeartSize, HeartSize);
            Tints.Wash(heart.GetComponent<Renderer>(), HeartTint);
            heart.gameObject.SetActive(false);

            portal = Raise(PrimitiveType.Cylinder, PortalName, root.transform, PartStyle.Start);
            portal.gameObject.SetActive(false);
        }

        static string NameOf(PillarRole role)
        {
            switch (role)
            {
                case PillarRole.Player:
                    return PlayerName;
                case PillarRole.Girl:
                    return GirlName;
                case PillarRole.Rival:
                    return RivalName;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, "Nobody stands on that pillar.");
            }
        }

        Actor Cast(PillarRole role, CastMark mark, BadgePlan plan)
        {
            var name = NameOf(role);
            var group = new GameObject(name);
            group.transform.SetParent(root.transform, worldPositionStays: false);

            var pillar = Slab(name + PillarSuffix, group.transform, PartStyle.Pillar);
            var looks = PillarDress.LooksOf(role);
            var costumes = new Costume[looks.Count];

            for (var slot = 0; slot < looks.Count; slot++)
            {
                costumes[slot] = Dressed(looks[slot], name, group.transform);
            }

            var part = new BadgePart(
                name + BadgeSuffix,
                0,
                0,
                mark.Badge,
                mark.Number,
                plan.Capacity,
                new WorldPoint(0f, 0f, 0f),
                IsoProjection.CameraRotation);

            return new Actor
            {
                Role = role,
                Pillar = pillar,
                Costumes = costumes,
                Badge = BadgeFactory.Raise(part, plan, badges, group.transform)
            };
        }

        Costume Dressed(CastLook look, string name, Transform parent)
        {
            var mesh = PillarDress.MeshOf(look);
            var frame = new GameObject(name + FigureSuffix + "_" + look);
            frame.transform.SetParent(parent, worldPositionStays: false);

            var costume = new Costume
            {
                Look = look,
                Mesh = PartModel.None,
                Figure = frame.transform,
                Skins = new Renderer[0]
            };

            var instance = Wear(mesh, frame.transform, name + FigureSuffix + MeshSuffix, PillarDress.StyleOf(look));
            if (instance == null)
            {
                return costume;
            }

            instance.transform.localEulerAngles = new Vector3(0f, PillarDress.FacingOf(look), 0f);
            CharacterDress.Bare(instance);

            costume.Mesh = mesh;
            costume.Skins = instance.GetComponentsInChildren<Renderer>(true);
            costume.Acting = FigureAnimator.Raise(frame, mesh, models);

            return costume;
        }

        Transform Slab(string name, Transform parent, PartStyle style)
        {
            var frame = new GameObject(name);
            frame.transform.SetParent(parent, worldPositionStays: false);

            var instance = Wear(PillarDress.StageModel, frame.transform, name + MeshSuffix, style);
            if (instance != null)
            {
                Seat(instance.transform);
            }

            return frame.transform;
        }

        GameObject Wear(PartModel mesh, Transform parent, string name, PartStyle style)
        {
            var model = models.Of(mesh);
            if (model == null)
            {
                Debug.LogWarning(
                    "The cutscene wanted the " + mesh + " mesh for " + name
                    + " and the pack resolved to nothing loadable, so that part of the stage stands empty.");
                return null;
            }

            var instance = UnityEngine.Object.Instantiate(model);
            instance.name = name;
            instance.transform.SetParent(parent, worldPositionStays: false);
            Coat(instance, materials.Of(style));
            WorldObjects.Destroy(instance.GetComponent<Collider>());

            return instance;
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

        static void Coat(GameObject instance, Material material)
        {
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterial = material;
            }
        }

        static void Seat(Transform instance)
        {
            var seated = false;
            var box = new Bounds();

            foreach (var filter in instance.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                var local = filter.sharedMesh.bounds;
                var here = new Bounds(
                    instance.InverseTransformPoint(filter.transform.TransformPoint(local.center)),
                    Vector3.Scale(local.size, filter.transform.lossyScale));

                if (seated)
                {
                    box.Encapsulate(here);
                }
                else
                {
                    box = here;
                    seated = true;
                }
            }

            if (!seated)
            {
                return;
            }

            instance.localPosition = new Vector3(-box.center.x, -box.min.y, -box.center.z);
        }

        void Draw()
        {
            if (rig != null)
            {
                rig.Hold(reel.Framing);
            }

            foreach (var actor in actors)
            {
                Dress(actor, PillarDress.MarkOf(reel, actor.Role));
            }

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

        void Dress(Actor actor, CastMark mark)
        {
            var seat = mark.PillarBase;

            actor.Pillar.localScale = Vector(PillarDress.PillarScaleOf(mark.PillarHeight));
            actor.Pillar.localPosition = new Vector3(seat.X, 0f, seat.Z);

            var stand = mark.Position;
            var scale = PillarDress.FigureScaleOf(mark);
            var lift = mark.Scale * PillarDress.LiftOf(mark.Look);

            foreach (var costume in actor.Costumes)
            {
                var shown = costume.Look == mark.Look;
                costume.Figure.gameObject.SetActive(shown);

                if (!shown)
                {
                    continue;
                }

                costume.Figure.localScale = new Vector3(scale, scale, scale);
                costume.Figure.localPosition = new Vector3(stand.X, stand.Y + lift, stand.Z);

                foreach (var skin in costume.Skins)
                {
                    Tints.Wash(skin, mark.Tint);
                }

                if (costume.Acting != null)
                {
                    costume.Acting.Cue(PillarDress.CueOf(actor.Role, reel.Elapsed));
                }
            }

            actor.Badge.Show(mark.Number);
            actor.Badge.transform.localPosition = new Vector3(
                stand.X,
                BadgeMetrics.AnchorAbove(stand.Y + PillarDress.StandingHeightOf(mark)),
                stand.Z);
        }

        Actor Cast(PillarRole role)
        {
            if (actors == null)
            {
                return null;
            }

            foreach (var actor in actors)
            {
                if (actor.Role == role)
                {
                    return actor;
                }
            }

            return null;
        }

        Costume Wearing(PillarRole role)
        {
            var actor = Cast(role);
            if (actor == null)
            {
                return null;
            }

            var look = PillarDress.MarkOf(reel, role).Look;

            foreach (var costume in actor.Costumes)
            {
                if (costume.Look == look)
                {
                    return costume;
                }
            }

            return null;
        }

        static Vector3 Vector(WorldPoint point)
        {
            return new Vector3(point.X, point.Y, point.Z);
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

            actors = null;
            ground = null;
            heart = null;
            portal = null;
            rig = null;

            materials.Dispose();
            badges.Dispose();
            models.Dispose();
        }
    }
}
