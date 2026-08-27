using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public static class CharacterCast
    {
        static readonly PartStyle[] roles = { PartStyle.Start, PartStyle.Enemy, PartStyle.Boss };

        public static IReadOnlyList<PartStyle> Roles
        {
            get { return roles; }
        }

        public static bool IsRole(PartStyle style)
        {
            for (var slot = 0; slot < roles.Length; slot++)
            {
                if (roles[slot] == style)
                {
                    return true;
                }
            }

            return false;
        }

        public static PartModel MeshOf(PartStyle role)
        {
            switch (role)
            {
                case PartStyle.Start:
                    return PartModel.Knight;
                case PartStyle.Enemy:
                case PartStyle.Boss:
                    return PartModel.None;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(role), role, "That part style is not a member of the cast.");
            }
        }

        public static PartModel MeshOfPlayer(PlayerLook look)
        {
            if (look.Tier < 0 || look.Tier >= VisualTier.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(look), look.Tier, "A player look always names a visual tier.");
            }

            return MeshOf(PartStyle.Start);
        }

        public static bool Wears(PartStyle role)
        {
            return MeshOf(role) != PartModel.None;
        }
    }
}
