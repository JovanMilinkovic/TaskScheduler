using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskScheduler.Models
{
    [Table("Tasks")]
    public class TaskTicket : Ticket
    {
        [Column("Finished")]
        [Required]
        public bool Finished { get; set; }

        [Column("DueDate")]
        [Required]
        public DateTime DueDate { get; set; }

        [Column("Expired")]
        [Required]
        public bool Expired { get; set; }

    }
}
