using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TaskScheduler.Models
{ 
    public enum TicketType 
    {
        TaskTicket = 1,
        DelegatedTaskTicket = 2,
        MeetingTicket = 3
    }

    public abstract class Ticket
    {
        [Column("TicketId")]
        [Required]
        [Key]
        public int TicketId { get; set; }

        [Column("TicketType")]
        [Required]
        public TicketType TicketType { get; set; }

        [Column("Title")]
        [MaxLength(20)]
        [Required]
        public string Title { get; set; }

        [Column("Description")]
        [MaxLength(255)]
        public string? Description { get; set; }

        [Column("CreatedDate")]
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [JsonIgnore]
        public User? CreatedBy { get; set; }
    }
}
