using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;

using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    public class MFUserController : BaseApiController
    {
        // GET api/MFUser
        //
        // Return list of all users
        //
        public HttpResponseMessage Get()
        {
            Repository repository = new Repository();
            List<MFUser> users = repository.GetAllUsers(GroupId);

            return Request.CreateResponse(HttpStatusCode.OK, JsonUser.FromDatabase(users));
        }

        public HttpResponseMessage Get(string id)
        {
            // Make sure the request is valid
            //
            if (string.IsNullOrEmpty(UserId))
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Not Authorized");
            }

            Repository repository = new Repository();
            var mfUser = repository.GetUserByEmail(id);
            if (mfUser == null)
            {
                mfUser = repository.GetUserById(id);
            }

            if (mfUser == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "User not found");
            }

            return Request.CreateResponse(HttpStatusCode.OK, JsonUser.FromDatabase(mfUser));
        }

        // POST api/<controller>
        //
        // Add user to the database
        //
        public HttpResponseMessage Post([FromBody]JsonUser value)
        {
            if (value == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid user information");
            }

            Models.Repository repository = new Models.Repository();
            var user = repository.GetUserByEmail(value.Email);
            if (user != null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.Conflict, "User with same email already exists");
            }

            string groupCode = HttpContext.Current.Request.QueryString["code"];
            Models.Group defaultGroup = repository.FindGroupByCode((string.IsNullOrEmpty(groupCode) ? "JYMF" : groupCode));
            if (defaultGroup == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Group code not found");
            }

            string userid;
            do
            {
                Random random = new Random();
                userid = (!string.IsNullOrEmpty(value.UserId) ? value.UserId : System.Web.Security.Membership.GeneratePassword(16, 0));
                userid = Regex.Replace(userid, @"[^a-zA-Z0-9]", m => random.Next(0, 9).ToString());

                user = repository.GetUserById(userid);
            } while (user != null);

            MFUser fbUser = new MFUser
            {
                UserId = userid,
                Email = value.Email,
                Password = value.Password,
                FirstName = value.FirstName,
                LastName = value.LastName,
                Name = value.Name
            };
            var mfUser = repository.CreateUser(fbUser);
            repository.SaveChanges();

            if (repository.IsUserInGroup(mfUser, defaultGroup) == GroupRoleEnum.empty)
            {
                repository.DefaultGroup(mfUser, defaultGroup, GroupRoleEnum.member);
                repository.SaveChanges();
            }

            // Send an email notification
            //
            var smtp = new SmtpClient();            // Settings in config file
            var message = new MailMessage("no-reply@workmanfamily.com", ConfigurationManager.AppSettings["AdminEmail"]);
            message.Subject = "JYMF RaceDay New User";
            message.IsBodyHtml = true;
            message.Priority = MailPriority.High;
            message.Body = File.ReadAllText(HttpContext.Current.Server.MapPath("~/App_Data/NewUser.txt"));
            message.Body = message.Body.Replace("@FIRSTNAME@", fbUser.FirstName)
                                        .Replace("@LASTNAME@", fbUser.LastName)
                                        .Replace("@EMAIL@", fbUser.Email);

            smtp.Send(message);

            return Request.CreateResponse(HttpStatusCode.Created, "User added to application");
        }

        // PUT api/<controller>
        //
        // Update user in the database
        //
        public HttpResponseMessage Put(string id, [FromBody]JsonUser value)
        {
            // Make sure the request is valid
            //
            if (string.IsNullOrEmpty(UserId))
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Not Authorized");
            }

            Repository repository = new Repository();
            var mfUser = repository.GetUserByEmail(id);
            if (mfUser == null)
            {
                mfUser = repository.GetUserById(id);
            }

            if (mfUser == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "User not found");
            }

            if (String.Compare(mfUser.Email, value.Email, true) != 0)
            {
                var dup = repository.GetUserByEmail(value.Email);
                if (dup != null)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.Conflict, "Email already in use");
                }
            }

            // Update the user properties (password, names)
            //
            mfUser.Email = (!string.IsNullOrEmpty(value.Email) ? value.Email : mfUser.Email);
            mfUser.FirstName = (!string.IsNullOrEmpty(value.FirstName) ? value.FirstName : mfUser.FirstName);
            mfUser.LastName = (!string.IsNullOrEmpty(value.LastName) ? value.LastName : mfUser.LastName);
            mfUser.Name = (!string.IsNullOrEmpty(value.Name) ? value.Name : mfUser.Name);
            mfUser.Password = (!string.IsNullOrEmpty(value.Password) ? value.Password : mfUser.Password);
            mfUser.UserId = (!string.IsNullOrEmpty(value.UserId) ? value.UserId : mfUser.UserId);

            repository.SaveChanges();

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        // DELETE api/mfuser/email
        //
        // Delete the user with the email
        //
        public HttpResponseMessage Delete(string id)
        {
            // Make sure the request is valid
            //
            if (string.IsNullOrEmpty(UserId))
            {
                return Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "Not Authorized");
            }

            Repository repository = new Repository();
            var mfUser = repository.GetUserByEmail(id);
            if (mfUser == null)
            {
                mfUser = repository.GetUserById(id);
            }

            if (mfUser == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "User not found");
            }

            bool bResult = repository.DeleteUser(mfUser.Email);

            if (bResult == false)
                return Request.CreateErrorResponse(HttpStatusCode.ExpectationFailed, "Unable to Delete User");

            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}