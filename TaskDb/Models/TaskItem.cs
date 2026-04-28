using System.ComponentModel.DataAnnotations;
namespace TaskDb.Models;

public class TaskItem {
    public int Id { get; set; }
    public bool IsCompleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    [MaxLength(20)]
    public string Priority { get; set; } = "Normal";
}