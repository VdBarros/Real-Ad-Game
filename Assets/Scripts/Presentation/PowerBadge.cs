using System;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    [RequireComponent(typeof(NumberBadge))]
    public sealed class PowerBadge : MonoBehaviour
    {
        NumberBadge badge;

        CountUp countUp;

        public event Action Settled;

        public int Power
        {
            get { return countUp.Target; }
        }

        public int Shown
        {
            get { return countUp.Display; }
        }

        public bool IsSettled
        {
            get { return countUp.IsSettled; }
        }

        internal void Begin(NumberBadge composed, int power)
        {
            badge = composed;
            countUp = CountUp.Settled(power);
            badge.Show(power);
            enabled = false;
        }

        public void Show(int power)
        {
            var retargeted = countUp.Toward(power);
            if (retargeted.Equals(countUp))
            {
                return;
            }

            countUp = retargeted;
            badge.Show(countUp.Display);
            enabled = true;
        }

        void Update()
        {
            countUp = countUp.Advanced(Time.deltaTime);

            if (countUp.Display != badge.Value)
            {
                badge.Show(countUp.Display);
            }

            if (!countUp.IsSettled)
            {
                return;
            }

            enabled = false;
            var settled = Settled;
            if (settled != null)
            {
                settled();
            }
        }
    }
}
