using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public static class PlayerGuises
    {
        static readonly PlayerGuise[] all =
            { PlayerGuise.Knight, PlayerGuise.Barbarian, PlayerGuise.Rogue };

        public static IReadOnlyList<PlayerGuise> All
        {
            get { return all; }
        }

        public static int Count
        {
            get { return all.Length; }
        }

        public static PartModel MeshOf(PlayerGuise guise)
        {
            switch (guise)
            {
                case PlayerGuise.Knight:
                    return PartModel.Knight;
                case PlayerGuise.Barbarian:
                    return PartModel.Barbarian;
                case PlayerGuise.Rogue:
                    return PartModel.Rogue;
                default:
                    throw Stranger(guise);
            }
        }

        public static string CapeOf(PlayerGuise guise)
        {
            switch (guise)
            {
                case PlayerGuise.Knight:
                    return AdventurerPack.KnightCloakNode;
                case PlayerGuise.Barbarian:
                    return AdventurerPack.BarbarianCloakNode;
                case PlayerGuise.Rogue:
                    return AdventurerPack.RogueCloakNode;
                default:
                    throw Stranger(guise);
            }
        }

        public static FigureAct FinisherOf(PlayerGuise guise)
        {
            switch (guise)
            {
                case PlayerGuise.Knight:
                    return FigureAct.Slice;
                case PlayerGuise.Barbarian:
                    return FigureAct.Cleave;
                case PlayerGuise.Rogue:
                    return FigureAct.Loose;
                default:
                    throw Stranger(guise);
            }
        }

        public static bool Drapes(PlayerGuise guise)
        {
            return WearsACape(CapeOf(guise));
        }

        public static bool WearsACape(string cape)
        {
            return !string.IsNullOrEmpty(cape);
        }

        public static bool IsGuise(PlayerGuise guise)
        {
            for (var slot = 0; slot < all.Length; slot++)
            {
                if (all[slot] == guise)
                {
                    return true;
                }
            }

            return false;
        }

        static ArgumentOutOfRangeException Stranger(PlayerGuise guise)
        {
            return new ArgumentOutOfRangeException(
                nameof(guise), guise, "No adventurer wears that guise.");
        }
    }
}
