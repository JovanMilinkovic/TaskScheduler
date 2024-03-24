using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Text.Json.Serialization;

namespace TaskScheduler.Models
{
    public enum Status
    {
        Comming,
        Unsure,
        NotComming,
    }


    [Table("Meetings")]
    public class MeetingTicket : Ticket
    {
        [Column("StartTime")]
        [Required]
        public DateTime StartTime { get; set; }

        [Column("EndTime")]
        [Required]
        public DateTime EndTime { get; set; }

        [JsonIgnore]
        public List<User> Invited { get; set; }

        public string? StatusesString { get; set; }

        [NotMapped]
        public List<Status> Statuses
        {
            get { return StatusesString?.Split(',').Select(int.Parse).Select(status => (Status)status).ToList() ?? new List<Status>(); }
            set { StatusesString = string.Join(',', value.Select(status => (int)status)); }
        }

    }

}
