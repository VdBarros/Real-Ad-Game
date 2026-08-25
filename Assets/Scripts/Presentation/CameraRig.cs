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

        public CameraFraming Framing
        {
            get { return applied; }
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
                return;
            }

            staging = advanced;
            Apply();
        }

        void Update()
        {
            Advance(Time.deltaTime);
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
            enabled = staging.IsBusy;

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
