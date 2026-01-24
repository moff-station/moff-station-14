// File to store as much CD related database things outside of Model.cs

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
    /// Stores CD Character data separately from the main Profile. This is done to work around a bug
    /// in EFCore migrations.
    /// <p />
    /// There is no way of forcing a dependent table to exist in EFCore (according to MS).
    /// You must always account for the possibility of this table not existing.
    /// </summary>
    public class MoffPlayer
    {
        public int Id { get; set; }

        [Required, ForeignKey("Player")]
        public Guid PlayerUserId { get; set; }

        public Player Player { get; set; } = null!;

        public int AntagWeight { get; set; } = 1;
    }
}
