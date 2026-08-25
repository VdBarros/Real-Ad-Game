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

        IReadOnlyList<TapCandidate> candidates;

        CameraFraming projected;

        TapHold hold = TapHold.Idle;

        int projectedWidth;

        int projectedHeight;

        float reach;

        public event Action<TargetPreview> Aimed;

        public event Action<TargetPreview> Tapped;

        public event Action Released;

        public bool IsLocked { get; set; }

        public TargetPreview Preview { get; private set; } = TargetPreview.None;

        public float Reach
        {
            get
            {
                Project();
                return reach;
            }
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
            candidates = null;
            Preview = TargetPreview.None;
            board.Show(run, Preview);
        }

        public IReadOnlyList<TapCandidate> Candidates()
        {
            Project();
            return candidates;
        }

        void Project()
        {
            RequireARun();

            if (candidates != null
                && projected.Equals(rig.Framing)
                && projectedWidth == FrameWidth
                && projectedHeight == FrameHeight)
            {
                return;
            }

            projected = rig.Framing;
            projectedWidth = FrameWidth;
            projectedHeight = FrameHeight;
            reach = TouchTargets.ReachOn(TouchTargets.DotsPerInchOr(Screen.dpi));
            candidates = TapAim.Candidates(run, projected, projectedWidth, projectedHeight);
        }

        public void AimAt(ScreenPoint finger)
        {
            RequireARun();

            if (rig.IsBusy || IsLocked)
            {
                Cancel();
                return;
            }

            Project();
            var aimed = TapAim.Of(candidates, finger, reach);

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

            var released = Released;
            if (released != null)
            {
                released();
            }

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

            hold = hold.Reading(
                pointer.press.wasPressedThisFrame,
                pointer.press.wasReleasedThisFrame,
                pointer.press.isPressed,
                pointer is Mouse);

            if (hold.Gesture == TapGesture.Ignore)
            {
                return;
            }

            var position = pointer.position.ReadValue();
            var finger = new ScreenPoint(position.x, position.y);

            if (hold.Gesture == TapGesture.Release)
            {
                ReleaseAt(finger);
                return;
            }

            AimAt(finger);
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
