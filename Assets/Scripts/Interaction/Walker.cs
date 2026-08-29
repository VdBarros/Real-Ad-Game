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

        OrbBoard orbs;

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

        public bool IsDraining
        {
            get { return journey.IsDraining; }
        }

        public Drain Drain
        {
            get { return journey.Drain; }
        }

        public ActionResult Arrival
        {
            get { return journey.Arrival; }
        }

        public Walk Walk
        {
            get { return journey.Walk; }
        }

        public Fight Fight
        {
            get { return journey.Fight; }
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
            orbs = world.Orbs;

            if (orbs != null)
            {
                orbs.Landed += Deliver;
            }

            if (pickups != null)
            {
                pickups.Settle(opening);
            }

            journey = Journey.Nowhere;
            landed = false;
            holding = false;
            afoot = false;
            Run = opening;

            input.Aimed += Sketch;
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
                BreakOff(journey.DivertedTo(nodeId));
                return;
            }

            SetOut(Journey.Toward(Run, nodeId));
        }

        public void Cancel()
        {
            if (!IsWalking)
            {
                return;
            }

            BreakOff(journey.Cancelled());
        }

        void SetOut(Journey setOut)
        {
            if (setOut.IsOver)
            {
                return;
            }

            journey = setOut;
            landed = false;
            holding = false;
            afoot = true;
            trail.Show(journey.Walk.Route);
            Follow();
            enabled = true;
        }

        void BreakOff(Journey broken)
        {
            journey = broken;

            if (journey.IsOver)
            {
                Settle();
                return;
            }

            Follow();
        }

        public void Advance(float deltaSeconds)
        {
            Turn(deltaSeconds);

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
            else
            {
                Bleed();
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
            var carried = Run.Power;

            landed = true;
            Run = journey.State;

            if (power != null)
            {
                if (journey.Arrival.Outcome == ActionOutcome.Win && fights != null)
                {
                    power.DropWeaponFrom(fights.SiteOf(journey.Walk.ArrivedNodeId));
                }

                if (Reaped(carried))
                {
                    orbs.Launch(fights.SiteOf(journey.Walk.ArrivedNodeId), Run.Power - carried);
                }
                else
                {
                    power.Show(Run.Power);
                }
            }

            floor.Show(Run);

            if (pickups != null)
            {
                pickups.Show(Run);
            }

            input.Show(Run);

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

        bool Reaped(int carried)
        {
            return journey.Arrival.Outcome == ActionOutcome.Win
                && orbs != null
                && fights != null
                && Run.Power > carried;
        }

        void Deliver(int gain)
        {
            if (power != null && Run != null)
            {
                power.Show(Run.Power);
            }
        }

        void Bleed()
        {
            if (journey.State == null || ReferenceEquals(journey.State, Run))
            {
                return;
            }

            Run = journey.State;

            if (power != null)
            {
                power.Show(Run.Power);
            }
        }

        void Act()
        {
            if (acting != null)
            {
                acting.Cue(FigureCues.Of(journey, Gripped));
            }
        }

        PlayerWeapon Gripped
        {
            get { return figure == null ? PlayerWeapon.None : figure.Gripping; }
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

                if (!journey.Walk.IsRetreating)
                {
                    figure.Face(journey.Walk.Facing);
                }
            }

            Act();
            rig.Jolt(journey.Fight.Impact);

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

        void Turn(float deltaSeconds)
        {
            if (figure != null)
            {
                figure.Turn(deltaSeconds);
            }

            if (fights != null)
            {
                fights.Turn(deltaSeconds);
            }
        }

        bool IsTheCastStillTurning()
        {
            if (figure != null && figure.IsTurning)
            {
                return true;
            }

            return fights != null && fights.IsTurning;
        }

        void Settle()
        {
            enabled = IsTheCastStillTurning();
            rig.Jolt(0f);

            if (acting != null)
            {
                acting.Cue(FigureCue.Still);
            }

            if (!afoot)
            {
                return;
            }

            var onward = journey.Onward();

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

            SetOut(onward);
        }

        void Sketch(TargetPreview preview)
        {
            if (IsWalking || trail == null)
            {
                return;
            }

            if (!preview.IsAimed || !preview.IsLegal || preview.Route.Count < 2)
            {
                trail.Clear();
                return;
            }

            trail.Preview(TileRoute.Of(Run.Level, preview.Route), Trail.MoodOf(preview));
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
            if (orbs != null)
            {
                orbs.Landed -= Deliver;
            }

            if (input == null)
            {
                return;
            }

            input.Aimed -= Sketch;
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
