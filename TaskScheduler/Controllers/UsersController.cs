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

namespace TaskScheduler.Controllers
{

    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly User _user;

        public UsersController(ApplicationDbContext context, User user)
        {
            _context = context;
            _user = user;
        }

        #region GET

        // GET: Get users
        [Route("GetUsers")]
        [HttpGet]
        public async Task<ActionResult> GetUsers()
        {
            var Users = await _context.Users.Where(u => u.Deleted == false)
                .Include(t => t.TaskTickets)
                .Include(m => m.MeetingTickets)
                .Include(d => d.DelegatedTaskTickets)
                .ToListAsync();

            return Ok(Users);
        }

        // GET: Get Deleted Users
        [Route("GetDeletedUsers")]
        [HttpGet]
        public async Task<ActionResult> GetDeletedUsers()
        {
            var DeletedUsers = await _context.Users.Where(u => u.Deleted == true).ToListAsync();

            return Ok(DeletedUsers);
        }

        // GET: Used to verify new user
        [Route("UserVerification/{Email}")]
        [HttpGet]
        public async Task<ActionResult> UserVerification(string email)
        {
            var user = _context.Users.Where(u => u.Email == email).FirstOrDefault();

            if (user == null) return BadRequest("This user does not exist!");

            if (user.VerifiedEmail == true) return BadRequest("This user is already verified!");

            user.VerifiedEmail = true;

            _context.Users.Update(user);

            await _context.SaveChangesAsync();

            return Ok("Your account has been successfully verified, you can now log in into your account.");
        }

        #endregion

        #region POST

        // POST: Users/CreateUser
        [Route("CreateUser/{Name}/{Surname}/{Email}/{Password}")]
        [HttpPost]
        public async Task<IActionResult> CreateUser(string Name, string Surname, string Email, string Password)
        {
            if (Name == "") return BadRequest("Enter a name!");
            if (Name.Length > 20) return BadRequest("Max length of a name is 20!");

            if (Surname == "") return BadRequest("Enter a surname!");
            if (Surname.Length > 20) return BadRequest("Max length of a surname is 20!");

            if (Password == "") return BadRequest("You need to enter a password!");
            if (Password.Length < 8) return BadRequest("Your password is too short!");
            if (Password.Length > 30) return BadRequest("Your password is too long!");

            if (Email == "") return BadRequest("You need to enter an email!");
            if (Email.Length > 50) return BadRequest("Your email is too long!");

            if (ModelState.IsValid) ;

            User user = new User();
            user.Name = Name;
            user.Surname = Surname;
            user.Email = Email;
            user.Admin = false;
            user.Tickets = new List<Ticket>();
            user.DelegatedTaskTickets = new List<DelegatedTaskTicket>();
            user.MeetingTickets = new List<MeetingTicket>();
            user.TaskTickets = new List<TaskTicket>();
            user.Deleted = false;

            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(Password, out passwordHash, out passwordSalt);
            user.Password = passwordHash;
            user.Salt = passwordSalt;

            foreach (var C in _context.Users.ToList())
            {
                if (C.Email.CompareTo(Email) == 0)
                    return BadRequest("Email is already in use!");
            }

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var verifyClient = await _context.Users
                    .Where(u => u.Email == Email)
                    .FirstOrDefaultAsync();

                Verification(verifyClient);

                return Ok(null);
            }
            catch (Exception e)
            {
                return BadRequest(e.InnerException.Message);
            }
        }

        // POST : CreateAdmin
        [Route("CreateAdmin/{Name}/{Surname}/{Email}/{Password}")]
        [HttpPost]
        public async Task<IActionResult> CreateAdmin(string Name, string Surname, string Email, string Password)
        {
            if (Name == "") return BadRequest("Enter a name!");
            if (Name.Length > 20) return BadRequest("Max length of a name is 20!");

            if (Surname == "") return BadRequest("Enter a surname!");
            if (Surname.Length > 20) return BadRequest("Max length of a surname is 20!");

            if (Password == "") return BadRequest("You need to enter a password!");
            if (Password.Length < 8) return BadRequest("Your password is too short!");
            if (Password.Length > 30) return BadRequest("Your password is too long!");

            if (Email == "") return BadRequest("You need to enter an email!");
            if (Email.Length > 50) return BadRequest("Your email is too long!");

            User user = new User();
            user.Name = Name;
            user.Surname = Surname;
            user.Email = Email;
            user.Admin = true;
            user.Tickets = new List<Ticket>();
            user.DelegatedTaskTickets = new List<DelegatedTaskTicket>();
            user.MeetingTickets = new List<MeetingTicket>();
            user.TaskTickets = new List<TaskTicket>();
            user.Deleted = false;

            byte[] passwordHash, passwordSalt;
            CreatePasswordHash(Password, out passwordHash, out passwordSalt);
            user.Password = passwordHash;
            user.Salt = passwordSalt;

            foreach (var C in _context.Users.ToList())
            {
                if (C.Email.CompareTo(Email) == 0)
                    return BadRequest("Email is already in use!");
            }

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var verifyClient = await _context.Users
                    .Where(u => u.Email == Email)
                    .FirstOrDefaultAsync();

                Verification(verifyClient);

                return Ok(null);
            }
            catch (Exception e)
            {
                return BadRequest(e.InnerException.Message);
            }
        }

        #endregion

        #region PUT

        // PUT: Change User Privilege
        [Route("ChangeUserPrivilege/{Email}/{Admin}")]
        [HttpPut]
        public async Task<ActionResult> ChangeUserPrivilege(string Email, bool Admin)
        {
            if (Email == null) return BadRequest("Email is not valid!");

            try
            {
                var User = _context.Users
                    .Where(u => u.Email == Email)
                    .FirstOrDefault();

                if (User == null) return BadRequest("User does not exist!");

                User.Admin = Admin;

                _context.Users.Update(User);
                await _context.SaveChangesAsync();
                return Ok("User privilege has been changed!");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        #endregion

        //Password Hash
        private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            if (password == null) throw new ArgumentNullException("password");
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Value cannot be empty or whitespace only string.", "password");

            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        //Verification
        private static async void Verification(User user)
        {
            String emailText = $"Dear {user.Name}\n\nYou have created an account, please confirm your identity by clicking on the link below.\n" +
                    "https://localhost:44330/UserVerification/" + user.Email + "\n\nWelcome to Task Scheduler!"; ;

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
                Subject = "Verification email",
                Body = emailText
            };

            message.To.Add(toMail);
            await Client.SendMailAsync(message);

        }

        #region DELETE

        [Route("DeleteUser/{Email}")]
        [HttpDelete]
        public async Task<ActionResult> DeleteUser(string Email)
        {
            if (Email == null) return BadRequest("Email is not valid!");

            try
            {
                var user = _context.Users
                    .Where(u => u.Email == Email)
                    .Include(u => u.Tickets)
                    .Include(u => u.TaskTickets)
                    .Include(u => u.DelegatedTaskTickets)
                    .Include(u => u.MeetingTickets)
                    .FirstOrDefault();

                if (user != null)
                {
                    foreach (Ticket t in user.Tickets)
                    {
                        if (t.TicketType == TicketType.TaskTicket)
                        {
                            _context.TaskTickets.Remove((TaskTicket)t);
                        }
                        else if(t.TicketType == TicketType.DelegatedTaskTicket)
                        {
                            _context.DelegatedTaskTickets.Remove((DelegatedTaskTicket)t);
                        }
                        else if(t.TicketType == TicketType.MeetingTicket)
                        {
                             int index = ((MeetingTicket)t).Invited.FindIndex(u => u.UserId == user.UserId);
                             if (index != -1)
                             {
                                ((MeetingTicket)t).Invited.RemoveAt(index);
                                ((MeetingTicket)t).Statuses.RemoveAt(index);
                                ((MeetingTicket)t).StatusesString = string.Join(',', ((MeetingTicket)t).Statuses.Select(status => (Status)(int)status));
                            }
                        }
                    }
                    user.Deleted = true;

                    NotificationAboutDeletingUser(user);
                    _context.Users.Update(user);
                    _context.Entry(user).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    return Ok(user);
                }
                else
                {
                    return Ok("User does not exist!");
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        #endregion

        public static async void NotificationAboutDeletingUser(User user)
        {
            String emailText;
            emailText = $"Dear {user.Name}\n\nYour profile is deleted.";

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
                Subject = "Account deletion",
                Body = emailText
            };

            message.To.Add(toMail);
            await Client.SendMailAsync(message);
        }
    }
}
