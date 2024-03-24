using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace TaskScheduler.Models
{
    [Table("DelegatedTaskTickets")]
    public class DelegatedTaskTicket : TaskTicket
    {
        [JsonIgnore]
        public List<User> DelegatedTo { get; set; }

        [Required]
        public bool Urgent { get; set; }    
        
    }
}
