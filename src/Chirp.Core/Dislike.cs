using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Chirp.Core;

public class Dislike
{
    [Key]
    public int DislikeId { get; set; } // Primary key for the relationship entity

    [ForeignKey("Cheep")]
    public int CheepId { get; set; } // Foreign key to Cheep
    public Cheep Cheep { get; set; } = null!; // Navigation property for Cheep

    [ForeignKey("Author")]
    public int AuthorId { get; set; } // Foreign key to Author
    public Author Author { get; set; } = null!; // Navigation property for Author
}