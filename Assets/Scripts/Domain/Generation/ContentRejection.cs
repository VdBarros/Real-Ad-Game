namespace Game.Domain
{
    public enum ContentRejection
    {
        None,
        RecipeSlotMismatch,
        RolesUnfilled,
        AdversaryStalled,
        UnaffordableEnemy,
        ValueNeverMinted,
        RegionFloorUnmet,
        EnvelopeInverted,
        GatedBehindBoss,
        BossBeyondBound,
        BossWithinReach,
        PanelStalled,
        RegionSpreadTooThin,
        OpeningWithoutAChoice,
        MultiplierProductBeyondCap,
        DeadWalkBeyondBudget
    }
}
