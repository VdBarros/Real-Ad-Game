using System;
using Game.Domain;
using UnityEngine;

namespace Game.Flow
{
    public static class GameBoot
    {
        public const string SunName = "GameSun";

        static readonly Quaternion SunAngle = Quaternion.Euler(50f, 200f, 0f);

        const float SunStrength = 1.6f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Open()
        {
            if (UnityEngine.Object.FindAnyObjectByType<GameLoop>() != null)
            {
                return;
            }

            RaiseTheSun();
            GameLoop.Raise(SeedOfThisSession(), MazePreset.Ship, null);
        }

        public static long SeedOfThisSession()
        {
            return DateTime.UtcNow.Ticks;
        }

        static void RaiseTheSun()
        {
            foreach (var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    return;
                }
            }

            var sun = new GameObject(SunName).AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = SunAngle;
            sun.intensity = SunStrength;
        }
    }
}
