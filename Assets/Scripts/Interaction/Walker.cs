using System;
using Game.Domain;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Interaction
{
    public sealed class Walker : MonoBehaviour
    {
        CameraRig rig;

        TapInput input;

        PlayerFigure figure;

        FigureAnimator acting;

        PowerBadge power;

        FloorState floor;

        TrailBoard trail;

        FightBoard fights;

        PickupBoard pickups;

        Journey journey = Journey.Nowhere;

        bool landed;

        bool holding;

        bool afoot;

        public event Action<ActionResult> Arrived;

        public event Action<RunState> Finished;

        public RunState Run { get; private set; }

        public bool IsWalking
        {
            get { return !journey.IsOver; }
        }

        public bool IsHolding
        {
            get { return holding; }
        }

        public ActionResult Arrival
        {
            get { return journey.Arrival; }
        }

        public Walk Walk
        {
            get { return journey.Walk; }
        }

        public static Walker Raise(CameraRig framing, WorldBuilder world, TapInput taps, RunState opening)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var walker = world.Floor.gameObject.AddComponent<Walker>();
            walker.Begin(framing, world, taps, opening);
            return walker;
        }

        public void Begin(CameraRig framing, WorldBuilder world, TapInput taps, RunState opening)
        {
            if (framing == null)
            {
                throw new ArgumentNullException(nameof(framing));
            }

            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (taps == null)
            {
                throw new ArgumentNullException(nameof(taps));
            }

            if (opening == null)
            {
                throw new ArgumentNullException(nameof(opening));
            }

            Unhook();

            rig = framing;
            input = taps;
            figure = world.Player;
            acting = figure == null ? null : figure.GetComponent<FigureAnimator>();
            power = world.PlayerBadge;
            floor = world.Floor;
            trail = world.Trail;
            fights = world.Fights;
            pickups = world.Pickups;

            if (pickups != null)
            {
                pickups.Settle(opening);
            }

            journey = Journey.Nowhere;
            landed = false;
            holding = false;
            afoot = false;
            Run = opening;

            input.Tapped += Commit;
            input.Released += Interrupt;
            input.IsLocked = false;
            enabled = false;
        }

        public void WalkTo(int nodeId)
        {
            RequireARun();

            if (IsWalking)
            {
                return;
            }

            var setOut = Journey.Toward(Run, nodeId);
            if (setOut.IsOver)
            {
                return;
            }

            journey = setOut;
            landed = false;
            holding = false;
            afoot = true;
            input.IsLocked = true;
            trail.Show(journey.Walk.Route);
            Follow();
            enabled = true;
        }

        public void Cancel()
        {
            if (!IsWalking)
            {
                return;
            }

            journey = journey.Cancelled();

            if (journey.IsOver)
            {
                Settle();
                return;
            }

            Follow();
        }

        public void Advance(float deltaSeconds)
        {
            if (!IsWalking)
            {
                Settle();
                return;
            }

            journey = journey.Advanced(deltaSeconds);
            Follow();

            if (journey.IsOver)
            {
                Settle();
                return;
            }

            if (!journey.IsWaiting)
            {
                return;
            }

            if (!landed)
            {
                Land();
            }

            if (journey.HoldsForAFight)
            {
                return;
            }

            if (holding)
            {
                ReleaseTheBeat();

                if (rig.IsBusy)
                {
                    return;
                }

                holding = false;
            }

            journey = journey.Resumed();
            landed = false;
            Act();

            if (journey.IsOver)
            {
                Settle();
            }
        }

        void Land()
        {
            landed = true;
            Run = journey.State;

            if (power != null)
            {
                if (journey.Arrival.Outcome == ActionOutcome.Win && fights != null)
                {
                    power.DropWeaponFrom(fights.SiteOf(journey.Walk.ArrivedNodeId));
                }

                power.Show(Run.Power);
            }

            floor.Show(Run);

            if (pickups != null)
            {
                pickups.Show(Run);
            }

            input.Show(Run);
            input.IsLocked = true;

            var arrived = Arrived;
            if (arrived != null)
            {
                arrived(journey.Arrival);
            }

            if (!journey.HoldsForABeat)
            {
                return;
            }

            holding = true;
            rig.CutTo(Run.Level.Decisions.Node(journey.Walk.ArrivedNodeId).Position);
        }

        void Act()
        {
            if (acting != null)
            {
                acting.Cue(FigureCues.Of(journey));
            }
        }

        void ReleaseTheBeat()
        {
            if (power == null || power.IsSettled)
            {
                rig.Release();
            }
        }

        void Follow()
        {
            rig.Follow(journey.Walk.Position);

            if (figure != null)
            {
                figure.StandOn(journey.Walk.Position);
            }

            Act();

            if (fights != null)
            {
                fights.Show(journey);
            }

            if (journey.Walk.IsRetreating)
            {
                trail.Clear();
                return;
            }

            trail.Follow(journey.Walk.Travelled);
        }

        void Settle()
        {
            enabled = false;

            if (acting != null)
            {
                acting.Cue(FigureCue.Still);
            }

            if (!afoot)
            {
                return;
            }

            trail.Clear();
            journey = Journey.Nowhere;
            landed = false;
            holding = false;
            afoot = false;
            input.IsLocked = false;

            var finished = Finished;
            if (finished != null)
            {
                finished(Run);
            }
        }

        void Commit(TargetPreview preview)
        {
            WalkTo(preview.NodeId);
        }

        void Interrupt()
        {
            Cancel();
        }

        void Update()
        {
            Advance(Time.deltaTime);
        }

        void OnDestroy()
        {
            Unhook();
        }

        void Unhook()
        {
            if (input == null)
            {
                return;
            }

            input.Tapped -= Commit;
            input.Released -= Interrupt;
        }

        void RequireARun()
        {
            if (Run == null)
            {
                throw new InvalidOperationException(
                    "The walker moves through a run it has not been given. Call Begin.");
            }
        }
    }
}
