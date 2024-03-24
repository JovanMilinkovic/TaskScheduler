using Microsoft.AspNetCore.Authentication;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace TaskScheduler.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("UserId")]
        [Required]
        public int UserId { get; set; }

        [Column("Name")]
        [MaxLength(20)]
        [Required]
        public string Name { get; set; }

        [Column("Surname")]
        [MaxLength(20)]
        [Required]
        public string Surname { get; set; }

        [Column("Email")]
        [MaxLength(50)]
        [Required]
        public string Email { get; set; }

        [Column("VerifiedEmail")]
        [Required]
        public virtual bool VerifiedEmail { get; set; }

        [Column("Password")]
        [Required]
        public byte[] Password { get; set; }

        [Required]
        public byte[] Salt { get; set; }

        public virtual List<Ticket> Tickets { get; set; }

        public virtual List<DelegatedTaskTicket> DelegatedTaskTickets { get; set; }

        public virtual List<MeetingTicket> MeetingTickets { get; set; }

        public virtual List<TaskTicket> TaskTickets { get; set; }

        [Column("Admin")]
        [Required]
        public bool Admin { get; set; }

        [Column("Deleted")]
        [Required]
        public bool Deleted {  get; set; }


    }
}
