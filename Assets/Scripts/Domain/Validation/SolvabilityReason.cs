namespace Game.Domain
{
    public enum SolvabilityReason
    {
        None,
        NoStart,
        ManyStarts,
        NoBoss,
        ManyBosses,
        NodeUnassigned,
        ValueOutOfRange,
        NodeUnreachable,
        GatedBehindBoss,
        BossBeyondBound,
        BossWithinReach,
        AdversaryStalled
    }
}
