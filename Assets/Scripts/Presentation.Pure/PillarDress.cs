using System;
using System.Collections.Generic;

namespace Game.Presentation.Pure
{
    public static class PillarDress
    {
        public const float PillarWidth = 1.1f;

        public const float GroundReach = 26f;

        public const float GroundDepth = 0.2f;

        static readonly PillarRole[] roles = { PillarRole.Player, PillarRole.Girl, PillarRole.Rival };

        static readonly CastLook[] playerLooks = { CastLook.Peasant, CastLook.Skeleton };

        static readonly CastLook[] girlLooks = { CastLook.Peasant, CastLook.Queen };

        static readonly CastLook[] rivalLooks = { CastLook.Champion };

        public static IReadOnlyList<PillarRole> Roles
        {
            get { return roles; }
        }

        public static PartModel StageModel
        {
            get { return PartModel.FloorTile; }
        }

        public static IReadOnlyList<CastLook> LooksOf(PillarRole role)
        {
            switch (role)
            {
                case PillarRole.Player:
                    return playerLooks;
                case PillarRole.Girl:
                    return girlLooks;
                case PillarRole.Rival:
                    return rivalLooks;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(role), role, "Nobody stands on that pillar.");
            }
        }

        public static PartModel MeshOf(CastLook look)
        {
            switch (look)
            {
                case CastLook.Peasant:
                    return PartModel.Knight;
                case CastLook.Skeleton:
                    return PartModel.SkeletonMinion;
                case CastLook.Queen:
                    return PartModel.SkeletonMage;
                case CastLook.Champion:
                    return PartModel.SkeletonWarrior;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(look), look, "The pack carries no mesh for that look.");
            }
        }

        public static PartStyle StyleOf(CastLook look)
        {
            switch (look)
            {
                case CastLook.Peasant:
                    return PartStyle.Start;
                case CastLook.Skeleton:
                case CastLook.Champion:
                    return PartStyle.Enemy;
                case CastLook.Queen:
                    return PartStyle.Boss;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(look), look, "No dressing style for that look.");
            }
        }

        public static CastMark MarkOf(PillarReel reel, PillarRole role)
        {
            switch (role)
            {
                case PillarRole.Player:
                    return reel.Player;
                case PillarRole.Girl:
                    return reel.Girl;
                case PillarRole.Rival:
                    return reel.Rival;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(role), role, "Nobody stands on that pillar.");
            }
        }

        public static FigureCue CueOf(PillarRole role, float seconds)
        {
            switch (role)
            {
                case PillarRole.Player:
                    return PlayerCueAt(seconds);
                case PillarRole.Girl:
                    return GirlCueAt(seconds);
                case PillarRole.Rival:
                    return FigureCue.Still;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(role), role, "Nobody stands on that pillar.");
            }
        }

        public static float FigureScaleOf(CastMark mark)
        {
            return mark.Scale * FigureFit.ScaleOf(MeshOf(mark.Look));
        }

        public static float LiftOf(CastLook look)
        {
            return FigureFit.LiftOf(MeshOf(look));
        }

        public static float FacingOf(CastLook look)
        {
            return ArtPacks.FacingOf(MeshOf(look));
        }

        public static float StandingHeightOf(CastMark mark)
        {
            return FigureFit.StandingHeight(MeshOf(mark.Look), mark.Scale);
        }

        public static WorldPoint GroundScale
        {
            get
            {
                var span = GroundReach / IsoProjection.TileEdge;

                return new WorldPoint(span, GroundDepth / StageHeight, span);
            }
        }

        public static WorldPoint PillarScaleOf(float height)
        {
            if (height < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height), height, "A pillar rises out of the ground, never into it.");
            }

            var span = PillarWidth / IsoProjection.TileEdge;

            return new WorldPoint(span, height / StageHeight, span);
        }

        static float StageHeight
        {
            get { return ArtPacks.HeightOf(StageModel); }
        }

        static FigureCue PlayerCueAt(float seconds)
        {
            var opened = PillarStage.Fall + PillarStage.PortalSeconds;

            if (seconds >= opened)
            {
                return FigureCue.Within(FigureAct.Recoil, PillarStage.Total - opened);
            }

            if (seconds >= PillarStage.Throw && seconds < PillarStage.Drain)
            {
                return FigureCue.Within(FigureAct.Strike, PillarStage.Drain - PillarStage.Throw);
            }

            return FigureCue.Still;
        }

        static FigureCue GirlCueAt(float seconds)
        {
            var crossed = PillarStage.Cross + PillarStage.WalkSeconds;

            if (seconds >= PillarStage.Cross && seconds < crossed)
            {
                return FigureCue.Walking;
            }

            return FigureCue.Still;
        }
    }
}
