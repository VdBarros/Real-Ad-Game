namespace Game.Domain
{
    public enum LayoutRejection
    {
        None,
        TilesDisconnected,
        PocketOverflow,
        SlotShortfall,
        BossTooShallow,
        TooFewOffPathSlots
    }
}
