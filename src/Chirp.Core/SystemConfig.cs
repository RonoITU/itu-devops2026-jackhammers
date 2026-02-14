using System.ComponentModel.DataAnnotations;

namespace Chirp.Core;

/// <summary>
/// Entity for storing system-level configuration values.
/// Used for values that need to persist across application restarts
/// and be shared across multiple application instances.
/// </summary>
public class SystemConfig
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public required string Key { get; set; }
    
    public int? IntValue { get; set; }
    public string? StringValue { get; set; }
    public DateTime? DateTimeValue { get; set; }
}
