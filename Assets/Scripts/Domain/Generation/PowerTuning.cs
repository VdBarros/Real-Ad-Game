using System;
using System.Collections.Generic;

namespace Game.Domain
{
    public sealed class PowerTuning
    {
        public const int FloorRepairPasses = 6;

        public const double EliteShare = 0.15;

        static readonly int[] Ladder = { 2, 3, 4 };

        public static readonly PowerTuning Tiny =
            new PowerTuning(2, 200, 0.6, 0.2, 0.8, 0.8, 0.7, 0.0, 1.0, 1);

        public static readonly PowerTuning Ship =
            new PowerTuning(2, 600, 0.6, 0.2, 0.8, 0.8, 0.7, 0.0, 1.0, 1);

        public static readonly PowerTuning Stress =
            new PowerTuning(2, 2000, 0.6, 0.2, 0.8, 0.8, 0.7, 0.0, 1.0, 1);

        public PowerTuning(
            int startingPower,
            int stripTarget,
            double enemyCap,
            double jitter,
            double bossFactor,
            double gatePreference,
            double pocketTreasure,
            double eliteFraction,
            double spreadFloor,
            int openingChoices)
        {
            if (startingPower < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(startingPower), startingPower, "A run begins holding power.");
            }

            if (stripTarget <= startingPower)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(stripTarget), stripTarget, "Stripping the level has to be worth more than starting it.");
            }

            RequireShare(enemyCap, nameof(enemyCap));
            RequireShare(jitter, nameof(jitter));
            RequireShare(gatePreference, nameof(gatePreference));
            RequireShare(pocketTreasure, nameof(pocketTreasure));
            RequireShare(eliteFraction, nameof(eliteFraction));

            if (openingChoices < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(openingChoices), openingChoices, "A level opens on at least one fight.");
            }

            if (spreadFloor < 1.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(spreadFloor), spreadFloor, "A region is never entered poorer than it can be unlocked.");
            }

            if (bossFactor <= 0.0 || bossFactor >= 1.0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bossFactor), bossFactor, "The boss is a share of a bound it must stay under.");
            }

            StartingPower = startingPower;
            StripTarget = stripTarget;
            EnemyCap = enemyCap;
            Jitter = jitter;
            BossFactor = bossFactor;
            GatePreference = gatePreference;
            PocketTreasure = pocketTreasure;
            EliteFraction = eliteFraction;
            SpreadFloor = spreadFloor;
            OpeningChoices = openingChoices;
        }

        public static IReadOnlyList<int> MultiplierLadder
        {
            get { return Ladder; }
        }

        public int StartingPower { get; }

        public int StripTarget { get; }

        public double EnemyCap { get; }

        public double Jitter { get; }

        public double BossFactor { get; }

        public double GatePreference { get; }

        public double PocketTreasure { get; }

        public double EliteFraction { get; }

        public double SpreadFloor { get; }

        public int OpeningChoices { get; }

        public PowerTuning Rebased(int startingPower)
        {
            if (startingPower == StartingPower)
            {
                return this;
            }

            return new PowerTuning(
                startingPower,
                (int)((long)StripTarget * startingPower / StartingPower),
                EnemyCap,
                Jitter,
                BossFactor,
                GatePreference,
                PocketTreasure,
                EliteFraction,
                SpreadFloor,
                OpeningChoices);
        }

        public PowerTuning Locking(double eliteFraction)
        {
            if (eliteFraction == EliteFraction)
            {
                return this;
            }

            return new PowerTuning(
                StartingPower,
                StripTarget,
                EnemyCap,
                Jitter,
                BossFactor,
                GatePreference,
                PocketTreasure,
                eliteFraction,
                SpreadFloor,
                OpeningChoices);
        }

        public PowerTuning Routing(double spreadFloor)
        {
            if (spreadFloor == SpreadFloor)
            {
                return this;
            }

            return new PowerTuning(
                StartingPower,
                StripTarget,
                EnemyCap,
                Jitter,
                BossFactor,
                GatePreference,
                PocketTreasure,
                EliteFraction,
                spreadFloor,
                OpeningChoices);
        }

        public PowerTuning Opening(int openingChoices)
        {
            if (openingChoices == OpeningChoices)
            {
                return this;
            }

            return new PowerTuning(
                StartingPower,
                StripTarget,
                EnemyCap,
                Jitter,
                BossFactor,
                GatePreference,
                PocketTreasure,
                EliteFraction,
                SpreadFloor,
                openingChoices);
        }

        public static PowerTuning For(MazePreset preset)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            if (preset.Name == MazePreset.Tiny.Name)
            {
                return Tiny;
            }

            if (preset.Name == MazePreset.Ship.Name)
            {
                return Ship;
            }

            if (preset.Name == MazePreset.Stress.Name)
            {
                return Stress;
            }

            throw new ArgumentException(
                "No power tuning is filed for the preset named " + preset.Name + ".", nameof(preset));
        }

        static void RequireShare(double value, string parameter)
        {
            if (value < 0.0 || value > 1.0)
            {
                throw new ArgumentOutOfRangeException(parameter, value, "A share runs from none to all.");
            }
        }
    }
}
