using System;

namespace Game.Presentation.Pure
{
    public static class WeaponDrop
    {
        public static Tint Iron
        {
            get { return new Tint(0.22f, 0.24f, 0.28f); }
        }

        public static bool CarriesAMesh(PlayerLook look)
        {
            return look.Weapon != PlayerWeapon.None;
        }

        public static PartModel ModelOf(PlayerLook look)
        {
            RequireAWeapon(look);

            return PlayerKit.ModelOf(look.Weapon);
        }

        public static float ScaleOf(PlayerLook look)
        {
            RequireAWeapon(look);

            return look.Scale * PlayerKit.StandingPerImportUnitOf(look.Guise);
        }

        public static float SpanOf(PlayerLook look)
        {
            RequireAWeapon(look);

            return look.Scale * PlayerKit.ReachOf(look.Guise, look.Weapon);
        }

        public static float LeastSpanThatReads
        {
            get { return FigureReadability.Height; }
        }

        public static bool IsBigEnoughToRead(PlayerLook look)
        {
            return SpanOf(look) >= LeastSpanThatReads;
        }

        public static bool TellsItselfApartFrom(Tint ground)
        {
            return Tint.Contrast(Iron, ground) >= WorldTints.LeastSeparation;
        }

        public static bool ReadsAgainst(PlayerLook look, Tint ground)
        {
            return IsBigEnoughToRead(look) && TellsItselfApartFrom(ground);
        }

        static void RequireAWeapon(PlayerLook look)
        {
            if (look.Weapon != PlayerWeapon.None)
            {
                return;
            }

            throw new ArgumentOutOfRangeException(
                nameof(look), look, "A rung that grips nothing has no weapon mesh to fly.");
        }
    }
}
