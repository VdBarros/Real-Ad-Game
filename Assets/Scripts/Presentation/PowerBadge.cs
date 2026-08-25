using System;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    [RequireComponent(typeof(NumberBadge))]
    public sealed class PowerBadge : MonoBehaviour
    {
        NumberBadge badge;

        PlayerFigure player;

        PowerBeat beat;

        public event Action Settled;

        public event Action<int> Changed;

        public int Power
        {
            get { return beat.Power; }
        }

        public int Shown
        {
            get { return beat.Shown; }
        }

        public bool IsSettled
        {
            get { return beat.IsSettled; }
        }

        public bool HasLanded
        {
            get { return beat.HasLanded; }
        }

        public PlayerLook Look
        {
            get { return beat.Look; }
        }

        internal void Begin(NumberBadge composed, PlayerFigure figure, int power)
        {
            badge = composed;
            player = figure;
            beat = PowerBeat.Begin(power);
            badge.Show(power);

            if (player != null)
            {
                player.Begin(beat);
            }

            enabled = false;
        }

        public void Show(int power)
        {
            var retargeted = beat.Toward(power);
            if (retargeted.Equals(beat))
            {
                return;
            }

            beat = retargeted;
            badge.Show(beat.Shown);
            enabled = true;

            var changed = Changed;
            if (changed != null)
            {
                changed(power);
            }
        }

        public void DropWeaponFrom(WorldPoint deathSite)
        {
            if (player != null)
            {
                player.AwaitWeaponFrom(deathSite);
            }
        }

        void Update()
        {
            Advance(Time.deltaTime);
        }

        public void Advance(float deltaSeconds)
        {
            var landed = beat.HasLanded;
            beat = beat.Advanced(deltaSeconds);

            if (beat.Shown != badge.Value)
            {
                badge.Show(beat.Shown);
            }

            if (player != null)
            {
                player.Follow(beat, deltaSeconds);
            }

            if (!beat.HasLanded)
            {
                return;
            }

            if (!landed)
            {
                var settled = Settled;
                if (settled != null)
                {
                    settled();
                }
            }

            if (beat.IsSettled && (player == null || !player.IsFlying))
            {
                enabled = false;
            }
        }
    }
}
