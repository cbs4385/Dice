namespace Quintessence.Game.Clash
{
    // An intervention has been declared and Backlash already paid to Target; the
    // effect itself has NOT yet applied. It resolves only when Target responds via
    // ClashReducer.Ward, ClashReducer.DeclineWard, or (Eclipse only) an
    // EclipseCancel counter-declaration. Models docs/clash.md SS2.3's strict
    // ordering as explicit reducer state, the same way RoundPhase models "awaiting
    // a response" during drafting.
    public sealed record PendingIntervention(int Actor, int Target, InterventionKind Kind, InterventionParams Params);
}
