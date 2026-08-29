using System;
using Game.Domain;
using Game.Interaction;
using Game.Presentation;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Flow
{
    public sealed class GameLoop : MonoBehaviour
    {
        public const string LoopName = "GameLoop";

        WorldBuilder world;

        CameraRig rig;

        ResultScreen screen;

        RecentreButton button;

        LevelSupply supply;

        ICutscene cutscene;

        GameObject levelRoot;

        TapInput input;

        Walker walker;

        PlacedLevel level;

        GameCycle cycle = GameCycle.Booting;

        bool bossHasFallen;

        bool closed;

        public event Action<GameCycle> Turned;

        public static GameLoop Raise(long openingSeed, MazePreset preset, ICutscene scene)
        {
            var loop = new GameObject(LoopName).AddComponent<GameLoop>();
            loop.Begin(openingSeed, preset, scene);
            return loop;
        }

        public void Begin(long openingSeed, MazePreset preset, ICutscene scene)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            if (supply != null)
            {
                throw new InvalidOperationException("A loop opens once, and this one is already running.");
            }

            supply = new LevelSupply(openingSeed, preset);
            cutscene = scene;
            world = new WorldBuilder();
            rig = CameraRig.Raise();
            screen = ResultScreen.Raise(Next);
            button = RecentreButton.Raise(Recentre);
            cycle = GameCycle.Booting;
        }

        public GameCycle Cycle
        {
            get { return cycle; }
        }

        public GamePhase Phase
        {
            get { return cycle.Phase; }
        }

        public int LevelNumber
        {
            get { return cycle.LevelNumber; }
        }

        public GameObject LevelRoot
        {
            get { return levelRoot; }
        }

        public LevelSupply Supply
        {
            get { return supply; }
        }

        public WorldBuilder World
        {
            get { return world; }
        }

        public CameraRig Rig
        {
            get { return rig; }
        }

        public ResultScreen Screen
        {
            get { return screen; }
        }

        public RecentreButton Button
        {
            get { return button; }
        }

        public TapInput Input
        {
            get { return input; }
        }

        public Walker Walker
        {
            get { return walker; }
        }

        public PlacedLevel Level
        {
            get { return level; }
        }

        public RunState Run
        {
            get { return walker == null ? null : walker.Run; }
        }

        public void Advance(float deltaSeconds)
        {
            RequireARun();

            for (var turning = true; turning; )
            {
                var before = cycle;
                Step(deltaSeconds);
                turning = !cycle.Equals(before);

                if (turning)
                {
                    Announce();
                }
            }

            Offer();
        }

        public void Recentre()
        {
            RequireARun();

            if (input == null)
            {
                return;
            }

            input.LookBack();
        }

        public void Next()
        {
            RequireARun();

            if (cycle.Phase != GamePhase.Result)
            {
                throw new InvalidOperationException(
                    "Next follows a result, and the cycle sits in " + cycle.Phase + ".");
            }

            var drawn = supply.Draw();

            screen.Hide();
            Tear();
            Raise(drawn);
            Announce();
            Offer();
        }

        public void Skip()
        {
            RequireARun();

            if (cycle.Phase == GamePhase.Cutscene)
            {
                if (cutscene != null)
                {
                    cutscene.Skip();
                }

                return;
            }

            if ((cycle.Phase == GamePhase.Preview || cycle.Phase == GamePhase.Play) && rig != null)
            {
                rig.Skip();
            }
        }

        public void Tear()
        {
            if (levelRoot == null)
            {
                return;
            }

            if (input != null)
            {
                input.Released -= Skip;
            }

            if (walker != null)
            {
                walker.Finished -= Land;
            }

            levelRoot.SetActive(false);
            WorldObjects.Destroy(levelRoot);

            levelRoot = null;
            input = null;
            walker = null;
            level = null;
            bossHasFallen = false;

            Offer();
        }

        void Offer()
        {
            if (button == null)
            {
                return;
            }

            var showing = rig != null
                && input != null
                && levelRoot != null
                && RecentreCall.Showing(cycle.Phase, rig.IsAway, rig.IsBusy);

            button.Offer(showing);

            if (input != null)
            {
                input.CallShowing = showing;
            }
        }

        void Step(float deltaSeconds)
        {
            switch (cycle.Phase)
            {
                case GamePhase.Boot:
                    cycle = cycle.Watching();
                    if (cutscene != null)
                    {
                        cutscene.Play();
                    }

                    return;

                case GamePhase.Cutscene:
                    if (cutscene != null)
                    {
                        cutscene.Advance(deltaSeconds);
                        if (cutscene.IsPlaying)
                        {
                            return;
                        }
                    }

                    Raise(supply.Draw());
                    return;

                case GamePhase.Preview:
                    if (input == null || rig == null || rig.IsBusy)
                    {
                        return;
                    }

                    cycle = cycle.Playing();
                    input.IsLocked = false;
                    return;

                case GamePhase.Play:
                    if (input == null || walker == null || !bossHasFallen || !PowerHasSettled())
                    {
                        return;
                    }

                    input.IsLocked = true;
                    cycle = cycle.Finished(walker.Run.Power);
                    screen.Show(
                        cycle.FinalPower,
                        Stars.For(level.Par, cycle.FinalPower, supply.LastLevelNumber));
                    return;

                default:
                    return;
            }
        }

        void Raise(PlacedLevel drawn)
        {
            var generating = cycle.Generating();
            var graph = drawn.Graph;
            var root = world.Build(graph, drawn.StartingPower);

            rig.Begin(graph);

            var opening = RunState.Begin(graph, drawn.StartingPower);
            var taps = TapInput.Raise(rig, world.Targets, opening);
            var walk = Walker.Raise(rig, world, taps, opening);

            level = drawn;
            levelRoot = root;
            input = taps;
            walker = walk;
            input.IsLocked = true;
            input.Released += Skip;
            walker.Finished += Land;
            bossHasFallen = false;

            cycle = generating.Previewing();
        }

        void Land(RunState run)
        {
            bossHasFallen |= run.IsLevelComplete;
        }

        bool PowerHasSettled()
        {
            if (world.Orbs != null && !world.Orbs.IsSettled)
            {
                return false;
            }

            return world.PlayerBadge == null || world.PlayerBadge.IsSettled;
        }

        void Announce()
        {
            var turned = Turned;
            if (turned != null)
            {
                turned(cycle);
            }
        }

        void Update()
        {
            if (closed || supply == null)
            {
                return;
            }

            Advance(Time.deltaTime);
        }

        public void Close()
        {
            if (closed)
            {
                return;
            }

            closed = true;

            if (cutscene != null && cutscene.IsPlaying)
            {
                cutscene.Skip();
            }

            Tear();

            if (button != null)
            {
                button.Dispose();
                button = null;
            }

            if (screen != null)
            {
                screen.Dispose();
                screen = null;
            }

            if (rig != null)
            {
                WorldObjects.Destroy(rig.gameObject);
                rig = null;
            }

            if (world != null)
            {
                world.Dispose();
                world = null;
            }
        }

        void OnDestroy()
        {
            Close();
        }

        void RequireARun()
        {
            if (closed)
            {
                throw new InvalidOperationException("The loop has been closed, and a closed loop does not turn again.");
            }

            if (supply == null)
            {
                throw new InvalidOperationException(
                    "The loop cycles through levels it has not been given a seed for. Call Begin.");
            }
        }
    }
}
