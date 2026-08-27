using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public static class CharacterCast
    {
        static readonly PartStyle[] roles = { PartStyle.Start, PartStyle.Enemy, PartStyle.Boss };

        static readonly PartModel[] enemyByTier =
        {
            PartModel.SkeletonMinion,
            PartModel.SkeletonRogue,
            PartModel.SkeletonRogue,
            PartModel.SkeletonWarrior,
            PartModel.SkeletonWarrior
        };

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
                    return TierMeshOf(0);
                case PartStyle.Boss:
                    return PartModel.SkeletonMage;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(role), role, "That part style is not a member of the cast.");
            }
        }

        public static PartModel MeshOf(PartStyle role, int power)
        {
            return role == PartStyle.Enemy ? TierMeshOf(VisualTier.Of(power)) : MeshOf(role);
        }

        public static PartModel TierMeshOf(int tier)
        {
            if (tier < 0 || tier >= enemyByTier.Length)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tier), tier, "No enemy silhouette for a tier the power ramp does not reach.");
            }

            return enemyByTier[tier];
        }

        public static IReadOnlyList<PartModel> MeshesOf(PartStyle role)
        {
            if (role != PartStyle.Enemy)
            {
                return new[] { MeshOf(role) };
            }

            var worn = new List<PartModel>();

            for (var tier = 0; tier < enemyByTier.Length; tier++)
            {
                if (!worn.Contains(enemyByTier[tier]))
                {
                    worn.Add(enemyByTier[tier]);
                }
            }

            return worn;
        }
    }
}
