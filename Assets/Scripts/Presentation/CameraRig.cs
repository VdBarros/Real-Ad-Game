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

        CameraStaging staging;

        CameraFraming applied;

        bool staged;

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

        public static CameraRig Raise()
        {
            var carrier = new GameObject(PartNames.Rig) { tag = MainCameraTag };
            carrier.transform.rotation = Quaternion.Euler(
                IsoProjection.CameraPitch, IsoProjection.CameraYaw, IsoProjection.CameraRoll);

            return carrier.AddComponent<CameraRig>();
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
            transform.position = new Vector3(framing.Position.X, framing.Position.Y, framing.Position.Z);
            Lens().orthographicSize = framing.OrthographicSize;
            enabled = false;
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
            transform.position = new Vector3(framing.Position.X, framing.Position.Y, framing.Position.Z);
            Lens().orthographicSize = framing.OrthographicSize;
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
