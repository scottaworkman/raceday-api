using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Web;
using System.Web.Http;
using System.Web.Security;
using RaceDayAPI.Models;

namespace RaceDayAPI.Controllers
{
    public class LoginController : ApiController
    {

        // GET api/Login
        //
        // Password request for member with matching emil.  Requires GroupId=JYMF, Email, and ApiKey
        //
        public HttpResponseMessage Get([FromUri]LoginAuth auth)
        {
            if ((auth == null) ||
                (string.IsNullOrEmpty(auth.groupid)) ||
                (string.IsNullOrEmpty(auth.email)) ||
                (string.IsNullOrEmpty(auth.apikey)))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Missing authentication information");
            }

            Repository repository = new Repository();
            var fbGroup = repository.FindGroupByCodeAndKey(auth.groupid, auth.apikey);
            var mfUser = repository.GetUserByEmail(auth.email);
            var groupMember = repository.UserGroupMembership(mfUser ?? new MFUser(), fbGroup ?? new Group());

            if (groupMember == null)
            {
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "User not found");
            }

            // Send password.  If no password, then generate one and send
            //
            if (string.IsNullOrEmpty(mfUser.Password))
            {
                mfUser.Password = Membership.GeneratePassword(8, 1);
                repository.SaveChanges();
            }

            // Send the email containing the password
            //
            var smtp = new SmtpClient();            // Settings in config file
            var message = new MailMessage("no-reply@workmanfamily.com", mfUser.Email);
            message.Subject = "Your JYMF RaceDay password";
            message.IsBodyHtml = true;
            message.Body = File.ReadAllText(HttpContext.Current.Server.MapPath("~/App_Data/ForgotPassword.txt")).Replace("@EMAIL@", mfUser.Email).Replace("@PASSWORD@", mfUser.Password);

            smtp.Send(message);
            
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        // POST api/Login
        //
        // Authenticate the user.  Requires groupid=JYMF and ApiKey.  Authentication may be by userid or email/password (v2)
        //
        public HttpResponseMessage Post([FromBody]LoginAuth auth)
        {
            if ((auth == null) || 
                (string.IsNullOrEmpty(auth.groupid)) || 
                (string.IsNullOrEmpty(auth.userid) && (string.IsNullOrEmpty(auth.email))) ||
                (string.IsNullOrEmpty(auth.apikey)))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Missing authentication information");
            }
            else
            {
                Repository repository = new Repository();
                var fbGroup = repository.FindGroupByCodeAndKey(auth.groupid, auth.apikey);
                MFUser mfUser = null;
                if (string.IsNullOrEmpty(auth.password))
                {
                    mfUser = repository.GetUserByEmail(auth.email);
                }
                else
                {
                    mfUser = repository.GetUserByAuthentication(auth.email, auth.password);
                }
                if (mfUser == null && auth.userid != null && !string.IsNullOrEmpty(auth.userid))
                    mfUser = repository.GetUserById(auth.userid);
                var groupMember = repository.UserGroupMembership(mfUser ?? new MFUser(), fbGroup ?? new Group());

                if (groupMember == null)
                {
                    if (mfUser == null)
                        return Request.CreateErrorResponse(HttpStatusCode.NotFound, "User not found");

                    if (fbGroup == null)
                        return Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Invalid credentials");

                    repository.DefaultGroup(mfUser, fbGroup, GroupRoleEnum.member);
                    repository.SaveChanges();

                    groupMember = repository.UserGroupMembership(mfUser, fbGroup);
                    if (groupMember == null)
                        return Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Invalid credentials");
                }

                if ((string.IsNullOrEmpty(groupMember.AccessToken)) ||
                    ((groupMember.AccessExpiration.HasValue == false) || (groupMember.AccessExpiration.Value < DateTime.Now.ToUniversalTime())))
                {
                    groupMember.AccessToken = Guid.NewGuid().ToString();
                    groupMember.AccessExpiration = DateTime.Now.AddDays(1).ToUniversalTime();
                    repository.SaveChanges();
                }
                else
                {
                    groupMember.AccessExpiration = DateTime.Now.AddDays(1).ToUniversalTime();
                    repository.SaveChanges();
                }

                var result = new AuthResult
                {
                    token = groupMember.AccessToken,
                    expiration = groupMember.AccessExpiration.Value,
                    role = groupMember.Role,
                    name = mfUser.Name,
                    userid = mfUser.UserId,
                    firstname = mfUser.FirstName,
                    lastname = mfUser.LastName,
                    email = mfUser.Email
                };

                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
        }

        // PUT api/Login
        //
        // Change user password for user with matching email.  Requires groupid=JYMF, email, apikey, and new password
        //
        public HttpResponseMessage Put([FromBody]LoginAuth auth)
        {
            if ((auth == null) ||
                (string.IsNullOrEmpty(auth.groupid)) ||
                (string.IsNullOrEmpty(auth.email)) || 
                (string.IsNullOrEmpty(auth.password)) ||
                (string.IsNullOrEmpty(auth.apikey)))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Missing authentication information");
            }
            else
            {
                Repository repository = new Repository();
                var fbGroup = repository.FindGroupByCodeAndKey(auth.groupid, auth.apikey);
                var mfUser = repository.GetUserByEmail(auth.email);
                var groupMember = repository.UserGroupMembership(mfUser ?? new MFUser(), fbGroup ?? new Group());

                if (mfUser == null)
                    return Request.CreateErrorResponse(HttpStatusCode.NotFound, "User not found");

                if (fbGroup == null)
                    return Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Invalid credentials");

                if (groupMember == null)
                    return Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Invalid credentials");

                if ((string.IsNullOrEmpty(groupMember.AccessToken)) ||
                    ((groupMember.AccessExpiration.HasValue == false) || (groupMember.AccessExpiration.Value < DateTime.Now.ToUniversalTime())))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.Forbidden, "Invalid login");
                }

                // Update password
                //
                mfUser.Password = auth.password;
                repository.SaveChanges();

                return Request.CreateResponse(HttpStatusCode.OK);
            }
        }

        public class LoginAuth
        {
            public string groupid { get; set; }
            public string userid { get; set; }
            public string email { get; set; }
            public string password { get; set; }
            public string apikey { get; set; }
        }

        public class AuthResult
        {
            public string token { get; set; }
            public DateTime expiration { get; set; }
            public int role { get; set; }
            public string name { get; set; }
            public string userid { get; set; }
            public string firstname { get; set; }
            public string lastname { get; set; }
            public string email { get; set; }
        }
    }
}