using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Web;
using System.Web.Http;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    public class EventController : BaseApiController
    {
        // GET api/<controller>
        //
        // Return upcoming events marking those that the authorized user is attending.  Authorization header must
        // contain the access_token identifying the user group membership
        //
        public HttpResponseMessage Get()
        {
            if (string.IsNullOrEmpty(UserId))
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Not Authorized");
            }

            Repository repository = new Repository();
            List<viewEventUserAttendance> events = repository.GetUpcomingEventsForUser(DateTime.Now.AddDays(-1), UserId);

            return Request.CreateResponse(HttpStatusCode.OK, events);
        }

        // GET api/<controller>/5
        //
        // Return detailed event information along with all users attending the event
        //
        public HttpResponseMessage Get(int id)
        {
            if (string.IsNullOrEmpty(UserId))
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Not Authorized");
            }

            Repository repository = new Repository();
            var eventInfo = repository.GetEventViewById(id, UserId);
            if (eventInfo != null)
            {
                var eventAttendees = repository.GetUsersForEvent(eventInfo.EventId);
                return Request.CreateResponse(HttpStatusCode.OK, new { eventinfo = eventInfo, attendees = JsonUser.FromDatabase(eventAttendees) });
            }
            else
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format("Event {0} not found", id));
        }

        // POST api/<controller>
        //
        // Create a new event.  Json eventInfo is passed in through the body of the POST
        //
        public HttpResponseMessage Post([FromBody]JsonEvent eventInfo)
        {
            // Make sure the request is valid
            //
            if (string.IsNullOrEmpty(UserId))
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Not Authorized");
            }

            if (eventInfo == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotAcceptable, "Invalid Event Information");
            }

            // Create new Model Event
            //
            RaceDayAPI.Models.Event iEvent = eventInfo.ToDatabase();
            iEvent.GroupId = GroupId;
            iEvent.CreatorId = UserId;

            // Add to the database and then add this user as a participant
            //
            Repository repository = new Repository();
            Event newEvent = repository.AddEvent(iEvent);
            repository.SaveChanges();

            if ((newEvent != null) && (newEvent.EventId > 0))
            {
                var user = repository.GetUserById(UserId);
                repository.AddUserToEvent(user, newEvent, AttendingEnum.Attending);
                repository.SaveChanges();

                var addedEvent = repository.GetEventViewById(newEvent.EventId, UserId);
                var eventAttendees = repository.GetUsersForEvent(newEvent.EventId);

                // Send an email notification
                //
                var smtp = new SmtpClient();            // Settings in config file
                var message = new MailMessage("no-reply@workmanfamily.com", ConfigurationManager.AppSettings["AdminEmail"]);
                message.Subject = "JYMF RaceDay New Event";
                message.IsBodyHtml = true;
                message.Body = File.ReadAllText(HttpContext.Current.Server.MapPath("~/App_Data/NewEvent.txt"));
                message.Body = message.Body.Replace("@NAME@", newEvent.Name)
                                            .Replace("@DATE@", newEvent.Date.ToShortDateString())
                                            .Replace("@URL@", newEvent.Url)
                                            .Replace("@LOCATION@", newEvent.Location)
                                            .Replace("@DESCRIPTION@", newEvent.Description)
                                            .Replace("@MFUSER@", this.UserId);

                smtp.Send(message);
                return Request.CreateResponse(HttpStatusCode.Created, new { eventinfo = addedEvent, attendees = JsonUser.FromDatabase(eventAttendees) });
            }

            return Request.CreateResponse(HttpStatusCode.NotAcceptable, "Unable to create event");
        }

        // PUT api/<controller>/5
        //
        // Modify existing event
        //
        public HttpResponseMessage Put(int id, [FromBody]JsonEvent value)
        {
            // Make sure the request is valid
            //
            if (string.IsNullOrEmpty(UserId))
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Not Authorized");
            }

            // Update the event
            //
            Repository repository = new Repository();
            Event iEvent = repository.GetEventById(id);
            if (iEvent != null)
            {
                iEvent.Name = value.Name;
                iEvent.Date = value.Date;
                iEvent.Url = value.Url;
                iEvent.Location = value.Location;
                iEvent.Description = value.Description;

                repository.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, JsonEvent.FromDatabase(iEvent));
            }

            return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Event not found");
        }

        // DELETE api/<controller>/5
        //
        // Delete the event and all attendees
        //
        public HttpResponseMessage Delete(int id)
        {
            // Make sure the request is valid
            //
            if (string.IsNullOrEmpty(UserId))
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Not Authorized");
            }

            Repository repository = new Repository();
            bool bResult = repository.DeleteEvent(id);

            if (bResult == false)
                return Request.CreateErrorResponse(HttpStatusCode.ExpectationFailed, "Unable to Delete Event");

            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }

    #region JSON Structures

    public class JsonEvent
    {
        public int EventId { get; set; }
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public string Url { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public string CreatorId { get; set; }

        public static JsonEvent FromDatabase(RaceDayAPI.Models.Event iEvent)
        {
            return new JsonEvent
            {
                EventId = iEvent.EventId,
                Name = iEvent.Name,
                Date = iEvent.Date,
                Url = iEvent.Url,
                Location = iEvent.Location,
                Description = iEvent.Description,
                CreatorId = iEvent.CreatorId,
            };
        }

        public RaceDayAPI.Models.Event ToDatabase()
        {
            RaceDayAPI.Models.Event iEvent = new RaceDayAPI.Models.Event
            {
                EventId = EventId,
                Name = Name,
                Date = Date,
                Url = Url,
                Location = Location,
                Description = Description,
                CreatorId = CreatorId
            };

            return iEvent;
        }
    }

    public class JsonUser
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public DateTime LastUpdate { get; set; }

        public static JsonUser FromDatabase(RaceDayAPI.Models.MFUser user)
        {
            return new JsonUser
            {
                UserId = user.UserId,
                Name = user.Name,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Password = user.Password,
                LastUpdate = user.LastUpdate
            };
        }

        public static List<JsonUser> FromDatabase(List<RaceDayAPI.Models.MFUser> userList)
        {
            List<JsonUser> newList = new List<JsonUser>();
            foreach (var user in userList)
            {
                newList.Add(FromDatabase(user));
            }

            return newList;
        }

        public RaceDayAPI.Models.MFUser ToDatabase()
        {
            RaceDayAPI.Models.MFUser user = new RaceDayAPI.Models.MFUser
            {
                UserId = UserId,
                Email = Email,
                Password = Password,
                Name = Name,
                FirstName = FirstName,
                LastName = LastName,
                LastUpdate = DateTime.Now
            };

            return user;
        }
    }

    #endregion
}