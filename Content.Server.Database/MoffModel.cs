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

    public class MoffLibraryEntry
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required] public string Name { get; set; } = null!;

        [Required] public string Description { get; set; } = null!;

        [Required] public string Author { get; set; } = null!;

        [Required] public string Content { get; set; } = null!;

        [Required] public string Type { get; set; } = null!;

        [Required] public Guid AuthorUserId { get; set; }
    }
}
