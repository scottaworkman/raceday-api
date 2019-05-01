using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    public class AttendController : BaseApiController
    {
        // GET api/<controller>
        //
        // Return upcoming events that the authorized user is attending.
        //
        public HttpResponseMessage Get()
        {
            if (string.IsNullOrEmpty(UserId))
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Not Authorized");
            }

            Repository repository = new Repository();
            List<viewEventUserAttendance> events = repository.GetUpcomingEventsForUser(DateTime.Now, UserId).Where(r => r.Attending == 1).ToList();

            return Request.CreateResponse(HttpStatusCode.OK, events);
        }

        // PUT api/<controller>/5
        //
        // Add user as attending to the event
        //
        public HttpResponseMessage Put(int id)
        {
            // Make sure the request is valid
            //
            if (string.IsNullOrEmpty(UserId))
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Not Authorized");
            }

            Repository repository = new Repository();
            MFUser user = repository.GetUserById(UserId);
            Event iEvent = repository.GetEventById(id);

            if ((user == null) || (iEvent == null))
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Event not found");

            repository.AddUserToEvent(user, iEvent, AttendingEnum.Attending);
            repository.SaveChanges();

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        // DELETE api/<controller>/5
        //
        // Removes the user as attending the event
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
            MFUser user = repository.GetUserById(UserId);
            Event iEvent = repository.GetEventById(id);

            if ((user == null) || (iEvent == null))
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Event not found");

            repository.RemoveUserFromEvent(user, iEvent);
            repository.SaveChanges();

            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}