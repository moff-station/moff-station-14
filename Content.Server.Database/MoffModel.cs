using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public static class MoffModel
{
    /// <summary>
    /// Multi-character selection model config, kept here so upstream's OnModelCreating needs one line.
    /// </summary>
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MoffPreference>()
            .HasOne(mp => mp.Preference)
            .WithOne(p => p.MoffPreference)
            .HasForeignKey<MoffPreference>(mp => mp.PreferenceId)
            .IsRequired();

        modelBuilder.Entity<MoffJobPriority>()
            .HasOne(jp => jp.MoffPreference)
            .WithMany(mp => mp.JobPriorities)
            .HasForeignKey(jp => jp.MoffPreferenceId)
            .IsRequired();

        modelBuilder.Entity<MoffJobPriority>()
            .HasIndex(jp => new { jp.MoffPreferenceId, jp.JobName })
            .IsUnique();

        modelBuilder.Entity<MoffProfile>()
            .HasOne(mp => mp.Profile)
            .WithOne(p => p.MoffProfile)
            .HasForeignKey<MoffProfile>(mp => mp.ProfileId)
            .IsRequired();

        modelBuilder.Entity<MoffPlayer>()
            .HasOne(mp => mp.Player)
            .WithOne(p => p.MoffPlayer)
            .HasForeignKey<MoffPlayer>(mp => mp.PlayerUserId)
            .HasPrincipalKey<Player>(p => p.UserId);
    }

    public class MoffPlayer
    {
        public int Id { get; set; }

        [Required, ForeignKey("Player")]
        public Guid PlayerUserId { get; set; }

        public Player Player { get; set; } = null!;

        public int AntagWeight { get; set; } = 1;

        public string? DiscordId { get; set; }
    }

    /// <summary>
    /// Per-player state for multi-character selection. Job priorities are a property of the
    /// <b>player</b>, not of an individual character: a character only records <i>which</i> jobs it
    /// is willing to take (via the upstream <see cref="Job"/> table), while the priority applied to
    /// each of those jobs lives here and is shared across every character.
    /// </summary>
    public class MoffPreference
    {
        public int Id { get; set; }

        [Required, ForeignKey("Preference")]
        public int PreferenceId { get; set; }

        public Preference Preference { get; set; } = null!;

        public List<MoffJobPriority> JobPriorities { get; } = new();
    }

    /// <summary>
    /// A single player-global job priority. See <see cref="MoffPreference"/>.
    /// </summary>
    public class MoffJobPriority
    {
        public int Id { get; set; }

        public int MoffPreferenceId { get; set; }

        public MoffPreference MoffPreference { get; set; } = null!;

        public string JobName { get; set; } = null!;

        public DbJobPriority Priority { get; set; }
    }

    /// <summary>
    /// Per-character state for multi-character selection. Only characters marked
    /// <see cref="Enabled"/> are considered when picking who a player spawns as at round start.
    /// </summary>
    public class MoffProfile
    {
        public int Id { get; set; }

        [Required, ForeignKey("Profile")]
        public int ProfileId { get; set; }

        public Profile Profile { get; set; } = null!;

        /// <summary>
        /// Whether this character is eligible to be spawned at round start. Defaults to true so
        /// that characters which predate multi-character selection keep working unchanged.
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
