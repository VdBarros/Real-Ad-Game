using System;
using Game.Domain;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraRig : MonoBehaviour
    {
        const string MainCameraTag = "MainCamera";

        Camera lens;

        WorldBackdrop backdrop;

        CameraStaging staging;

        CameraFraming applied;

        float jolt;

        bool staged;

        public float Jolted
        {
            get { return jolt; }
        }

        public bool IsBusy
        {
            get { return staged && staging.IsBusy; }
        }

        public bool IsAway
        {
            get { return staged && staging.IsAway; }
        }

        public CameraFraming Framing
        {
            get { return applied; }
        }

        public CameraFraming Following
        {
            get { return staged ? staging.Following : applied; }
        }

        public WorldBackdrop Backdrop
        {
            get { return backdrop; }
        }

        public static CameraRig Raise()
        {
            var carrier = new GameObject(PartNames.Rig) { tag = MainCameraTag };
            carrier.transform.rotation = Quaternion.Euler(
                IsoProjection.CameraPitch, IsoProjection.CameraYaw, IsoProjection.CameraRoll);

            var raised = carrier.AddComponent<CameraRig>();
            raised.backdrop = WorldBackdrop.Hang(raised.Lens());

            return raised;
        }

        public void Begin(LevelGraph graph)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            staging = CameraStaging.Over(graph);
            staged = true;
            Apply();
        }

        public void Hold(CameraFraming framing)
        {
            staged = false;
            applied = framing;
            Place();
            Lens().orthographicSize = framing.OrthographicSize;
            Refit();
            enabled = false;
        }

        public void Jolt(float impulse)
        {
            var kicked = CameraJolt.Clamped(impulse);
            if (kicked.Equals(jolt))
            {
                return;
            }

            jolt = kicked;
            Place();
        }

        public void Follow(WorldPoint subject)
        {
            RequireALevel();
            Stage(staging.Follows(subject));
        }

        public void Look(WorldPoint offset)
        {
            RequireALevel();
            Stage(staging.Looks(offset));
        }

        public void LookHeld()
        {
            RequireALevel();
            Stage(staging.LookHeld());
        }

        public void LookBack()
        {
            RequireALevel();
            Stage(staging.LooksBack());
        }

        public void CutTo(TilePosition position)
        {
            RequireALevel();
            staging = staging.CutTo(position);
            Apply();
        }

        public void Release()
        {
            RequireALevel();
            staging = staging.Released();
            Apply();
        }

        public void Skip()
        {
            RequireALevel();
            staging = staging.Skipped();
            Apply();
        }

        public void Advance(float deltaSeconds)
        {
            if (!staged)
            {
                enabled = false;
                return;
            }

            var advanced = staging.Advanced(deltaSeconds);
            if (advanced.Equals(staging))
            {
                enabled = !staging.IsSettled;
                return;
            }

            staging = advanced;
            Apply();
        }

        void Update()
        {
            Advance(Time.deltaTime);
        }

        void Stage(CameraStaging wanted)
        {
            if (wanted.Equals(staging))
            {
                return;
            }

            staging = wanted;
            Apply();
        }

        void RequireALevel()
        {
            if (!staged)
            {
                throw new InvalidOperationException("The rig frames a level it has been given. Call Begin first.");
            }
        }

        void Apply()
        {
            var framing = staging.Framing;
            enabled = !staging.IsSettled;

            if (framing.Equals(applied))
            {
                return;
            }

            applied = framing;
            Place();
            Lens().orthographicSize = framing.OrthographicSize;
            Refit();
        }

        void Place()
        {
            var at = CameraJolt.Jolted(applied.Position, jolt);

            transform.position = new Vector3(at.X, at.Y, at.Z);
        }

        void Refit()
        {
            if (backdrop != null)
            {
                backdrop.Fit(Lens().orthographicSize);
            }
        }

        Camera Lens()
        {
            if (lens != null)
            {
                return lens;
            }

            lens = GetComponent<Camera>();
            lens.orthographic = true;
            lens.nearClipPlane = IsoProjection.NearPlane;
            lens.farClipPlane = IsoProjection.FarPlane;
            return lens;
        }
    }
}
