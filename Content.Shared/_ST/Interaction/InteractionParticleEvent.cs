// SPDX-FileCopyrightText: 2026 Janet Blackquill <uhhadd@gmail.com>
//
// SPDX-License-Identifier: MIT

using Robust.Shared.Serialization;

namespace Content.Shared._ST.Interaction;

/// <summary>
/// Data for interaction particles
/// </summary>
[Serializable, NetSerializable]
public sealed class StellarInteractionParticleEvent(NetEntity performer, NetEntity? used, NetEntity target, bool isClientEvent, StellarInteractionParticleType type) : EntityEventArgs // Moff - Stow delay
{
    public NetEntity Performer = performer;

    public NetEntity? Used = used;

    public NetEntity Target = target;

    public TimeSpan Cooldown = TimeSpan.FromSeconds(0.25);
    /// <summary>
    /// Workaround for event subscription not working w/ the session overload
    /// </summary>
    public bool IsClientEvent = isClientEvent;

    public StellarInteractionParticleType Type = type; // Moff - Stow delay
}

// Moff Start - Stow delay
[Serializable, NetSerializable]
public enum StellarInteractionParticleType
{
    Use,
    InHand,
}
// Moff end
