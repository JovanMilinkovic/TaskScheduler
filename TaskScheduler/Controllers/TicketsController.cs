using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskScheduler.Data;
using TaskScheduler.Models;
using Microsoft.AspNetCore.Authorization;
using System.Net.Mail;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;

namespace TaskScheduler.Controllers
{
    public struct FreeTimeSlot
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public List<string> UserEmails { get; set; }
    }

    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TicketsController(ApplicationDbContext context)
        {
            _context = context;
        }
        #region GET

        // GET: AllTickets
        [HttpGet]
        [Route("GetAllTickets")]
        public async Task<ActionResult> GetAllTickets()
        {
            var Tickets = await _context.Tickets.ToListAsync();

            return Ok(Tickets);
        }

        // GET: TaskTickets
        [HttpGet]
        [Route("GetTaskTickets")]
        public async Task<ActionResult> GetTaskTickets()
        {
            var Tickets = await _context.TaskTickets.Where(t => t.TicketType == TicketType.TaskTicket).ToListAsync();

            foreach (TaskTicket ticket in Tickets)
            {
                if(DateTime.Compare(ticket.DueDate, DateTime.Now) <= 0)
                {
                    ticket.Expired = true;
                    _context.TaskTickets.Update(ticket);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(Tickets);
        }

        // GET: MeetingTickets
        [HttpGet]
        [Route("GetMeetingTickets")]
        public async Task<ActionResult> GetMeetingTickets()
        {
            var Tickets = await _context.MeetingTickets.ToListAsync();

            return Ok(Tickets);
        }

        // GET: DelegatedTaskTickets
        [HttpGet]
        [Route("GetDelegatedTaskTickets")]
        public async Task<ActionResult> GetDelegatedTaskTickets()
        {
            var Tickets = await _context.DelegatedTaskTickets.ToListAsync();

            return Ok(Tickets);
        }

        #endregion

        #region POST

        // POST: CreateTaskTicket
        [Route("CreateTaskTicket/{Title}/{Description}/{Email}/{DueDate}")]
        [HttpPost]
        public async Task<IActionResult> CreateTaskTicket(string Title, string Description, string Email, DateTime DueDate)
        {
            if (Title == "") return BadRequest("Enter a title!");
            if (Title.Length > 20) return BadRequest("Max length of a title is 20!");

            if (Description == "") return BadRequest("Enter a description!");
            if (Description.Length > 255) return BadRequest("Max length of a description is 255!");

            if (DueDate <= DateTime.Now) return BadRequest("Change Due Date!");

            var User = _context.Users
                .Include(u => u.TaskTickets)
                .FirstOrDefault(u => u.Email == Email);

            if (User != null && User.Deleted == false && User.VerifiedEmail == true)
            {
                TaskTicket ticket = new TaskTicket();
                ticket.Title = Title;
                ticket.Description = Description;
                ticket.CreatedBy = _context.Users.Where(u => u.Email == Email).FirstOrDefault();
                ticket.DueDate = DueDate;
                ticket.TicketType = TicketType.TaskTicket;
                ticket.Finished = false;
                ticket.Expired = false;
                
                try
                {
                    if (User.TaskTickets == null) User.TaskTickets = new List<TaskTicket>();
                    User.TaskTickets.Add(ticket);
                    _context.Users.Update(User);
                    _context.TaskTickets.Add(ticket);
                    await _context.SaveChangesAsync();

                    var TaskUser = await _context.Users.Where(u => u.Email == Email).FirstOrDefaultAsync();
                    TaskEmail(TaskUser, ticket);

                    return Ok(User.TaskTickets);
                }
                catch (Exception e)
                {
                    return BadRequest(e.InnerException.Message);
                }
            }
            else return BadRequest("User does not exist or user is not verified!");
        }

        // POST: CreateMeetingTicket
        [Route("CreateMeetingTicket/{Title}/{Description}/{Email}/{StartTime}/{EndTime}")]
        [HttpPost]
        public async Task<IActionResult> CreateMeetingTicket(string Title, string Description, string Email, DateTime StartTime, DateTime EndTime, [FromBody] List<string> UserEmails)
        {
            if (Title == "") return BadRequest("Enter a title!");
            if (Title.Length > 20) return BadRequest("Max length of a title is 20!");

            if (Description == "") return BadRequest("Enter a description!");
            if (Description.Length > 255) return BadRequest("Max length of a description is 255!");

            if (DateTime.Compare(StartTime, EndTime) >= 0) return BadRequest("Change Start and End time!");

            var user = _context.Users.FirstOrDefault(u => u.Email == Email);
            if (user != null && user.Deleted == false)
            {
                MeetingTicket ticket = new MeetingTicket();
                ticket.Title = Title;
                ticket.Description = Description;
                ticket.CreatedBy = _context.Users.Where(u => u.Email == Email).FirstOrDefault();
                ticket.StartTime = StartTime;
                ticket.EndTime = EndTime;
                ticket.TicketType = TicketType.MeetingTicket;
                ticket.Invited = new List<User>();
                ticket.StatusesString = null;
                
                foreach (string mail in UserEmails)
                {
                    User invitedUser = _context.Users.FirstOrDefault(u => u.Email == mail);
                    if (invitedUser == null) return BadRequest("Invited user does not exist!");

                    if (invitedUser.MeetingTickets == null) invitedUser.MeetingTickets = new List<MeetingTicket>();

                    invitedUser.MeetingTickets.Add(ticket);
                    ticket.Invited.Add(invitedUser);

                    var statuses = ticket.Statuses.ToList();
                    statuses.Add(Status.Unsure);
                    ticket.Statuses = statuses;
                    
                    //ticket.Statuses.Add(Status.Unsure);
                    TaskEmail(invitedUser, ticket);
                }

                try
                {
                    if (user.MeetingTickets == null) user.MeetingTickets = new List<MeetingTicket>();
                    user.MeetingTickets.Add(ticket);
                    _context.Users.Update(user);
                    _context.MeetingTickets.Add(ticket);
                    await _context.SaveChangesAsync();

                    var TaskUser = await _context.Users.Where(u => u.Email == Email).FirstOrDefaultAsync();
                    TaskEmail(TaskUser, ticket);

                    return Ok(null);
                }
                catch (Exception e)
                {
                    return BadRequest(e.InnerException.Message);
                }
            }
            else return BadRequest("User does not exist!");
        }

        // POST: CreateDelegatedTaskTicket
        [Route("CreateDelegatedTaskTicket/{Title}/{Description}/{Email}/{DueDate}/{Urgent}")]
        [HttpPost]
        public async Task<IActionResult> CreateDelegatedTaskTicket(string Title, string Description, string Email, DateTime DueDate, bool Urgent, [FromBody] List<string> UserEmails)
        {
            if (Title == "") return BadRequest("Enter a title!");
            if (Title.Length > 20) return BadRequest("Max length of a title is 20!");

            if (Description == "") return BadRequest("Enter a description!");
            if (Description.Length > 255) return BadRequest("Max length of a description is 255!");

            var user = _context.Users.FirstOrDefault(u => u.Email == Email);
            if (user != null && user.Deleted == false)
            {
                DelegatedTaskTicket ticket = new DelegatedTaskTicket();
                ticket.Title = Title;
                ticket.Description = Description;
                ticket.CreatedBy = _context.Users.Where(u => u.Email == Email).FirstOrDefault();
                ticket.DueDate = DueDate;
                ticket.Urgent = Urgent;
                ticket.TicketType = TicketType.DelegatedTaskTicket;
                ticket.DelegatedTo = new List<User>();

                foreach (string mail in UserEmails)
                {
                    User invitedUser = _context.Users.FirstOrDefault(u => u.Email == mail);
                    if (invitedUser == null) return BadRequest("Invited user does not exist!");

                    if (invitedUser.DelegatedTaskTickets == null) invitedUser.DelegatedTaskTickets = new List<DelegatedTaskTicket>();
                    invitedUser.DelegatedTaskTickets.Add(ticket);
                    ticket.DelegatedTo.Add(invitedUser);
                    TaskEmail(invitedUser, ticket);
                }

                try
                {
                    if (user.DelegatedTaskTickets == null) user.DelegatedTaskTickets = new List<DelegatedTaskTicket>();
                    user.DelegatedTaskTickets.Add(ticket);
                    _context.Users.Update(user);
                    _context.DelegatedTaskTickets.Add(ticket);
                    await _context.SaveChangesAsync();

                    var TaskUser = await _context.Users.Where(u => u.Email == Email).FirstOrDefaultAsync();
                    TaskEmail(TaskUser, ticket);

                    return Ok(null);
                }
                catch (Exception e)
                {
                    return BadRequest(e.InnerException.Message);
                }
            }
            else return BadRequest("User does not exist!");
        }

        private static async void TaskEmail(User user, Ticket ticket)
        {
            String emailText = $"Dear {user.Name}\n\nA task: {ticket.Title} has been created." ;

            SmtpClient Client = new SmtpClient()
            {
                Host = "smtp.outlook.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential()
                {
                    UserName = "taskscheduler123@outlook.com",
                    Password = "TaskScheduler1109"
                }
            };
            MailAddress fromMail = new MailAddress("taskscheduler123@outlook.com", "TaskScheduler");
            MailAddress toMail = new MailAddress(user.Email, user.Name);
            MailMessage message = new MailMessage()
            {
                From = fromMail,
                Subject = "Created ticket",
                Body = emailText
            };

            message.To.Add(toMail);
            await Client.SendMailAsync(message);

        }

        #endregion

        #region UPDATE

        // PUT: ChangeTaskTicket
        [Route("ChangeTaskTicket/{Email}/{Title}/{Finished}/{DeleteTask}")]
        [HttpPut]
        public async Task<ActionResult> ChangeTaskTicket(string Email, string Title, bool Finished, bool DeleteTask)
        {
            if (Email == null) return BadRequest("Email is not valid!");

            var ticket = _context.TaskTickets
                .Where(t => t.Title == Title)
                .FirstOrDefault();

            var user = _context.Users
                .Where(u => u.Email == Email)
                .FirstOrDefault();


            if (ticket != null && ticket.CreatedBy == user)
            {
                try
                {
                    if (DeleteTask != true && Finished == true)
                    {
                        ticket.Finished = Finished;
                        _context.TaskTickets.Update(ticket);
                        await _context.SaveChangesAsync();
                        return Ok("Your task is finished!");
                    }
                    else
                    {
                        _context.TaskTickets.Remove(ticket);
                        await _context.SaveChangesAsync();
                        return Ok("Your task is finished and deleted!");
                    }
                }
                catch (Exception e)
                {
                    return BadRequest(e.Message);
                }
            }
            else return BadRequest("User does not exist!");
        }


        #endregion

        #region DELETE

        // DELETE: DeleteAllTaskTickets
        [Route("DeleteAllTaskTickets")]
        [HttpDelete]
        public async Task<IActionResult> DeleteAllTaskTickets()
        {
            try
            {
                List<TaskTicket> TaskTickets = await _context.Tickets
                    .OfType<TaskTicket>()
                    .ToListAsync();

                _context.Tickets.RemoveRange(TaskTickets);

                await _context.SaveChangesAsync();

                return Ok("All task tickets deleted successfully.");
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, $"An error occurred while deleting task tickets: {ex.Message}");
            }
        }

        // DELETE: DeleteAllMeetingTickets
        [Route("DeleteAllMeetingTickets")]
        [HttpDelete]
        public async Task<IActionResult> DeleteAllMeetingTickets()
        {
            try
            {
                List<MeetingTicket> MeetingTickets = await _context.Tickets
                    .OfType<MeetingTicket>()
                    .ToListAsync();

                _context.Tickets.RemoveRange(MeetingTickets);

                await _context.SaveChangesAsync();

                return Ok("All meeting tickets deleted successfully.");
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, $"An error occurred while deleting meeting tickets: {ex.Message}");
            }
        }

        // DELETE: DeleteAllDelegatedTaskTickets
        [Route("DeleteAllDelegatedTaskTickets")]
        [HttpDelete]
        public async Task<IActionResult> DeleteAllDelegatedTaskTickets()
        {
            try
            {
                List<DelegatedTaskTicket> DelegatedTaskTickets = await _context.Tickets
                    .OfType<DelegatedTaskTicket>()
                    .ToListAsync();

                _context.Tickets.RemoveRange(DelegatedTaskTickets);

                await _context.SaveChangesAsync();

                return Ok("All delegated task tickets deleted successfully.");
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, $"An error occurred while deleting meeting tickets: {ex.Message}");
            }
        }

        #endregion


        [Route("FindFreeTimeSlots/{UserEmails}/{StartTime}/{EndTime}")]
        [HttpGet]
        public async Task<IActionResult> FindFreeTimeSlots(List<string> UserEmails, DateTime StartTime, DateTime EndTime)
        {
            List<MeetingTicket> tickets = new List<MeetingTicket>();

            foreach (string UserEmail in UserEmails)
            {
                var user = _context.Users
                    .Include(u => u.MeetingTickets)
                    .Where(u => u.Email == UserEmail)
                    .FirstOrDefault();

                if (user == null)
                {
                    return BadRequest("The given user does not exist!");
                }

                var userTickets = _context.MeetingTickets
                    .Where(ticket => ticket.Invited.Contains(user) && ticket.StartTime >= StartTime && ticket.EndTime <= EndTime)
                    .OrderBy(ticket => ticket.StartTime)
                    .ToList();

                tickets.Concat(userTickets);
            }

            tickets.OrderBy(ticket => ticket.StartTime);

            var freeSlots = new List<FreeTimeSlot>();

            DateTime currentStart = StartTime;
            foreach (var ticket in tickets)
            {
                if (ticket.StartTime > currentStart)
                {
                    FreeTimeSlot slot = new FreeTimeSlot { Start = currentStart, End = ticket.StartTime, UserEmails = UserEmails };
                    freeSlots.Add(slot);
                }

                currentStart = ticket.EndTime;
            }

            if (EndTime > currentStart)
            {
                FreeTimeSlot slot = new FreeTimeSlot { Start = currentStart, End = EndTime, UserEmails = UserEmails };
                freeSlots.Add(slot);
            }

            return Ok(freeSlots);
        }

    }
}
