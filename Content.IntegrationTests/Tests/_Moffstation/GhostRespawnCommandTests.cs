#nullable enable

using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server._Moffstation.RespawnButton.Commands;
using Content.Server.Mind;
using Content.Shared._Moffstation.CCVar;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Tests._Moffstation;

[TestFixture, TestOf(typeof(GhostRespawnCommand))]
public sealed class GhostRespawnCommandTests : GameTest
{
    public override PoolSettings PoolSettings => new()
    {
        DummyTicker = false,
        Connected = true,
    };

    [SidedDependency(Side.Server)] private readonly IConfigurationManager _sConfigMan = default!;
    [SidedDependency(Side.Server)] private readonly IConsoleHost _sConsoleHost = default!;
    [SidedDependency(Side.Server)] private readonly SharedGhostSystem _sGhost = default!;
    [SidedDependency(Side.Server)] private readonly MindSystem _sMind = default!;
    [SidedDependency(Side.Server)] private readonly ISharedPlayerManager _sPlayerMan = default!;

    [Test]
    public async Task TestGhostRespawnWhenDisabled()
    {
        await Server.WaitPost(() => _sConfigMan.SetCVar(MoffCCVars.RespawningEnabled, false));

        var (ghost, session) = await SetupClientGhost();
        await Server.WaitPost(() =>
        {
            _sConsoleHost.GetSessionShell(session).ExecuteCommand(GhostRespawnCommand.CommandName);
        });

        Assert.That(
            session.AttachedEntity,
            Is.EqualTo(ghost.Owner),
            "Client's attached entity should still be ghost after a failed respawn command"
        );
    }

    [Test]
    public async Task TestGhostRespawnWhenNotGhost()
    {
        var clientSession = GetClientSessionOnServer();
        Assume.That(clientSession.AttachedEntity, Is.Not.Null);
        var clientEntBefore = clientSession.AttachedEntity;

        await Server.WaitPost(() =>
        {
            _sConsoleHost.GetSessionShell(clientSession).ExecuteCommand(GhostRespawnCommand.CommandName);
        });

        Assert.That(
            clientSession.AttachedEntity,
            Is.EqualTo(clientEntBefore),
            "Client attached ent should not have changed after a failed respawn command"
        );
    }

    [Test]
    public async Task TestGhostRespawnWhenTooSoon()
    {
        var (ghost, session) = await SetupClientGhost();

        await Server.WaitPost(() =>
        {
            // Spoof the death time so that we've definition not been waiting long enough.
            SEntMan.System<SharedGhostSystem>()
                .SetTimeOfDeath(ghost.AsNullable(), SGameTiming.ServerTime + TimeSpan.FromHours(1));
            _sConsoleHost.GetSessionShell(session).ExecuteCommand(GhostRespawnCommand.CommandName);
        });

        Assert.That(
            session.AttachedEntity,
            Is.Not.Null.And.EqualTo(ghost.Owner),
            "Client's attached entity should still be ghost after a failed respawn command"
        );
    }

    [Test]
    public async Task TestGhostRespawnSuccess()
    {
        var (ghost, session) = await SetupClientGhost();

        await Server.WaitPost(() =>
        {
            // Spoof the death time so that we've definitely been waiting long enough.
            _sGhost.SetTimeOfDeath(ghost.AsNullable(), SGameTiming.ServerTime - TimeSpan.FromHours(24));
            _sConsoleHost.GetSessionShell(session).ExecuteCommand(GhostRespawnCommand.CommandName);
        });

        Assert.That(
            session.AttachedEntity,
            Is.Not.EqualTo(ghost.Owner),
            "Client's attached entity should have changed after a successful respawn command"
        );

        await Server.WaitAssertion(() =>
        {
            Assert.That(
                SEntMan.HasComponent<GhostComponent>(session.AttachedEntity),
                Is.False,
                "Client's attached entity should no longer be a ghost after a successful respawn command"
            );
        });
    }

    private ICommonSession GetClientSessionOnServer()
    {
        Assume.That(Client.Session, Is.Not.Null);
        return _sPlayerMan.GetSessionById(Client.Session.UserId);
    }

    /// Ensures the starting state of the client is a ghost, by suiciding. Returns the client's attached entity and
    /// session from the server's point of view..
    private async Task<(Entity<GhostComponent> ClientGhost, ICommonSession ClientSession)> SetupClientGhost()
    {
        var clientSession = GetClientSessionOnServer();
        var clientEnt = clientSession.AttachedEntity;
        Assume.That(clientEnt, Is.Not.Null);

        Entity<GhostComponent> clientGhost = default;
        await Server.WaitAssertion(() =>
        {
            var mind = _sMind.GetMind(clientEnt.Value);
            Assume.That(mind, Is.Not.Null);
            var mindComponent = SEntMan.GetComponent<MindComponent>(mind.Value);
            _sConsoleHost.GetSessionShell(clientSession).ExecuteCommand("suicide");
            Assume.That(SEntMan.TryGetComponent<GhostComponent>(mindComponent.CurrentEntity, out var ghost), Is.True);
            clientGhost = (mindComponent.CurrentEntity!.Value, ghost!);
        });

        return (clientGhost, clientSession);
    }
}
