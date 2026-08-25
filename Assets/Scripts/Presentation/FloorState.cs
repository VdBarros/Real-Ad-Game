using System;
using System.Collections.Generic;
using Game.Domain;
using Game.Presentation.Pure;
using UnityEngine;

namespace Game.Presentation
{
    public sealed class FloorState : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        static readonly int ColorId = Shader.PropertyToID("_Color");

        readonly Dictionary<TilePosition, Renderer> ground = new Dictionary<TilePosition, Renderer>();

        readonly List<TilePosition> flipping = new List<TilePosition>();

        MaterialPropertyBlock block;

        bool begun;

        Material cursedFloor;

        Material clearedFloor;

        FloorReading reading = FloorReading.Nothing;

        IReadOnlyList<int> ranks;

        int deepestRank;

        float elapsed;

        public bool IsSettled
        {
            get { return flipping.Count == 0; }
        }

        public int ClearedCount
        {
            get { return reading.Cleared.Count; }
        }

        public bool IsCleared(TilePosition position)
        {
            return reading.IsCleared(position);
        }

        public void Dress(Material cursed, Material cleared)
        {
            if (cursed == null)
            {
                throw new ArgumentNullException(nameof(cursed));
            }

            if (cleared == null)
            {
                throw new ArgumentNullException(nameof(cleared));
            }

            cursedFloor = cursed;
            clearedFloor = cleared;
        }

        public void Adopt(TilePosition position, Renderer tile)
        {
            if (tile == null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            ground[position] = tile;
        }

        public void Begin(RunState state)
        {
            RequireMaterials();

            flipping.Clear();
            ranks = null;
            elapsed = 0f;
            reading = FloorReading.Of(state);
            begun = true;

            foreach (var pair in ground)
            {
                Settle(pair.Key);
            }

            enabled = false;
        }

        public void Show(RunState state)
        {
            RequireMaterials();
            RequireABeginning();

            var opened = FloorReading.Of(state);
            var flippingNow = opened.Since(reading);

            foreach (var position in flipping)
            {
                Settle(position);
            }

            flipping.Clear();
            flipping.AddRange(flippingNow);
            ranks = FloorSweep.Ranks(state.Level.Tiles, flipping, reading);
            deepestRank = 0;
            foreach (var rank in ranks)
            {
                if (rank > deepestRank)
                {
                    deepestRank = rank;
                }
            }

            reading = opened;
            elapsed = 0f;
            enabled = !IsSettled;
            Paint();
        }

        public void Advance(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaSeconds), deltaSeconds, "The floor only ever flips forwards.");
            }

            if (IsSettled)
            {
                enabled = false;
                return;
            }

            elapsed += deltaSeconds;
            Paint();
        }

        void Update()
        {
            Advance(Time.deltaTime);
        }

        void Paint()
        {
            if (elapsed >= FloorSweep.Seconds)
            {
                foreach (var position in flipping)
                {
                    Settle(position);
                }

                flipping.Clear();
                ranks = null;
                enabled = false;
                return;
            }

            for (var index = 0; index < flipping.Count; index++)
            {
                Blend(flipping[index], FloorSweep.Blend(ranks[index], deepestRank, elapsed));
            }
        }

        void Settle(TilePosition position)
        {
            Renderer tile;
            if (!ground.TryGetValue(position, out tile) || tile == null)
            {
                return;
            }

            tile.sharedMaterial = reading.IsCleared(position) ? clearedFloor : cursedFloor;
            tile.SetPropertyBlock(null);
        }

        void Blend(TilePosition position, float amount)
        {
            Renderer tile;
            if (!ground.TryGetValue(position, out tile) || tile == null)
            {
                return;
            }

            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }

            tile.sharedMaterial = clearedFloor;

            var colour = Color.Lerp(
                WorldPalette.Of(PartStyle.Floor), WorldPalette.Of(PartStyle.Cleared), amount);

            block.Clear();
            block.SetColor(BaseColorId, colour);
            block.SetColor(ColorId, colour);
            tile.SetPropertyBlock(block);
        }

        void RequireMaterials()
        {
            if (cursedFloor == null || clearedFloor == null)
            {
                throw new InvalidOperationException(
                    "The floor wears two materials it has neither been dressed with nor outlived. "
                    + "Call Dress, and keep whoever owns them alive as long as the level.");
            }
        }

        void RequireABeginning()
        {
            if (!begun)
            {
                throw new InvalidOperationException(
                    "The floor flips what a reading newly covers, so it has to be given the opening one. Call Begin.");
            }
        }
    }
}
