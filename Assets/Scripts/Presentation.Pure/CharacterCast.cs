using System;

namespace Game.Presentation.Pure
{
    public static class CharacterCast
    {
        static readonly PartStyle[] roles = { PartStyle.Start, PartStyle.Enemy, PartStyle.Boss };

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

    }
}
