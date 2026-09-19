namespace Content.Shared._Moffstation.Chitter;

// Marks a ChitterServerComponent as a private M.P.N. server: excluded from ChitterServerSystem's normal
// grid-wide auto-discovery, only reachable once a PDA has been manually connected to it.
[RegisterComponent]
public sealed partial class ChitterMpnServerComponent : Component
{
}
