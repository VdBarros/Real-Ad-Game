using System;
using System.Collections.Generic;
using Game.Domain;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Interaction
{
    public sealed class TapInput : MonoBehaviour
    {
        CameraRig rig;

        TargetBoard board;

        RunState run;

        public event Action<TargetPreview> Aimed;

        public event Action<TargetPreview> Tapped;

        public TargetPreview Preview { get; private set; } = TargetPreview.None;

        public float Reach
        {
            get { return TouchTargets.ReachOn(TouchTargets.DotsPerInchOr(Screen.dpi)); }
        }

        public CameraFraming Framing
        {
            get
            {
                RequireARun();
                return rig.Framing;
            }
        }

        public int FrameWidth
        {
            get { return Screen.width > 0 ? Screen.width : ScreenFrame.Width; }
        }

        public int FrameHeight
        {
            get { return Screen.height > 0 ? Screen.height : ScreenFrame.Height; }
        }

        public static TapInput Raise(CameraRig framing, TargetBoard targets, RunState state)
        {
            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }

            var input = targets.gameObject.AddComponent<TapInput>();
            input.Begin(framing, targets, state);
            return input;
        }

        public void Begin(CameraRig framing, TargetBoard targets, RunState state)
        {
            if (framing == null)
            {
                throw new ArgumentNullException(nameof(framing));
            }

            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }

            rig = framing;
            board = targets;
            Preview = TargetPreview.None;
            Show(state);
        }

        public void Show(RunState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (board == null)
            {
                throw new InvalidOperationException(
                    "The tap marks a board it has not been given. Call Begin.");
            }

            run = state;
            Preview = TargetPreview.None;
            board.Show(run, Preview);
        }

        public IReadOnlyList<TapCandidate> Candidates()
        {
            RequireARun();
            return TapAim.Candidates(run, rig.Framing, FrameWidth, FrameHeight);
        }

        public void AimAt(ScreenPoint finger)
        {
            RequireARun();

            if (rig.IsBusy)
            {
                Cancel();
                return;
            }

            var aimed = TapAim.Of(Candidates(), finger, Reach);

            if (aimed == Preview.NodeId)
            {
                return;
            }

            Preview = TargetPreview.Of(run, aimed);
            board.Show(run, Preview);

            var announced = Aimed;
            if (announced != null)
            {
                announced(Preview);
            }
        }

        public void ReleaseAt(ScreenPoint finger)
        {
            AimAt(finger);

            var committed = Preview;
            Cancel();

            if (!committed.IsLegal)
            {
                return;
            }

            var tapped = Tapped;
            if (tapped != null)
            {
                tapped(committed);
            }
        }

        public void Cancel()
        {
            if (!Preview.IsAimed)
            {
                return;
            }

            Preview = TargetPreview.None;
            board.Show(run, Preview);

            var announced = Aimed;
            if (announced != null)
            {
                announced(Preview);
            }
        }

        void Update()
        {
            var pointer = Pointer.current;
            if (pointer == null || run == null)
            {
                return;
            }

            var position = pointer.position.ReadValue();
            var finger = new ScreenPoint(position.x, position.y);

            if (pointer.press.wasReleasedThisFrame)
            {
                ReleaseAt(finger);
            }
            else if (pointer.press.isPressed)
            {
                AimAt(finger);
            }
        }

        void RequireARun()
        {
            if (run == null)
            {
                throw new InvalidOperationException(
                    "The tap aims at the nodes of a run it has not been given. Call Begin.");
            }
        }
    }
}
