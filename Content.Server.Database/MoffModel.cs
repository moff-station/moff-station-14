using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public static class MoffModel
{
    public class MoffPlayer
    {
        public int Id { get; set; }

        [Required, ForeignKey("Player")]
        public Guid PlayerUserId { get; set; }

        public Player Player { get; set; } = null!;

        public int AntagWeight { get; set; } = 1;
    }
}
