using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Model.Entities;

[Table("HIGH_SCORES")]
public class HighScore {
    
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    [Column("PLAYER_NAME")]
    public string PlayerName { get; set; } = null!;
    
    [Column("SCORE")]
    public int Score { get; set; }
}