using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RaceDayAPI.Models
{
	public class Repository : BaseRepository, IRepository
	{
		const int STALE_USER = 7;

		#region Users

		/// <summary>
		/// Get user record by user id
		/// </summary>
		/// <param name="userId"></param>
		/// <returns></returns>
		public MFUser GetUserById(string userId)
		{
			return context.MFUsers.Where(r => r.UserId == userId).FirstOrDefault();
		}

        /// <summary>
        /// Get user record by email address
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public MFUser GetUserByEmail(string email)
        {
            return context.MFUsers.Where(r => r.Email == email).FirstOrDefault();
        }

        /// <summary>
        /// Get user record by email/password
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public MFUser GetUserByAuthentication(string email, string password)
        {
            var user = context.MFUsers.Where(r => (r.Email == email) && (!string.IsNullOrEmpty(r.Password)) && (r.Password == password)).FirstOrDefault();
            if (user != null)
            {
                // Make sure password is case-sensitive
                //
                if (password.Equals(user.Password, StringComparison.Ordinal) == false)
                    user = null;
            }

            return user;
        }

		/// <summary>
		/// Make sure user exists in the database.
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		/// 
		public MFUser CreateUser(/*Facebook.FacebookUser*/ MFUser fbUser)
		{
			MFUser user = GetUserById(fbUser.UserId);
			if (user == null)
			{
				user = new MFUser()
				{
					UserId = fbUser.UserId,
					Name = fbUser.Name,
					FirstName = fbUser.FirstName,
					LastName = fbUser.LastName,
					Email = fbUser.Email,
                    Password = fbUser.Password,
					LastUpdate = DateTime.Now
				};
				context.MFUsers.Add(user);
			}
			else if (user.LastUpdate.AddDays(STALE_USER) < DateTime.Now)
			{
				user.Name = (!string.IsNullOrEmpty(fbUser.Name) ? fbUser.Name : user.Name);
				user.FirstName = (!string.IsNullOrEmpty(fbUser.FirstName) ? fbUser.FirstName : user.FirstName);
				user.LastName = (!string.IsNullOrEmpty(fbUser.LastName) ? fbUser.LastName : user.LastName);
				user.Email = (!string.IsNullOrEmpty(fbUser.Email) ? fbUser.Email : user.Email);
                user.Password = (!string.IsNullOrEmpty(fbUser.Password) ? fbUser.Password : user.Password);
				user.LastUpdate = DateTime.Now;
			}

			return user;
		}

        public List<MFUser> GetAllUsers(Int32 groupId)
        {
            var memberList = context.GroupMembers.Where(r => r.GroupId == groupId).ToList();
            string[] members = new string[memberList.Count];
            int i = 0;
            foreach (GroupMember member in memberList)
            {
                members[i] = member.UserId;
                i++;
            }

            return context.MFUsers.Where(r => members.Contains(r.UserId)).OrderBy(o => o.FirstName).ThenBy(o => o.LastName).ToList();
        }

        public bool DeleteUser(string email)
        {
            var user = context.MFUsers.Where(r => r.Email == email).FirstOrDefault();
            if (user != null)
            {
                foreach(var group in user.GroupMembers.ToList())
                {
                    context.GroupMembers.Remove(group);
                }

                foreach(var attend in user.Attendings.ToList())
                {
                    context.Attendings.Remove(attend);
                }

                context.MFUsers.Remove(user);
                context.SaveChanges();

                return true;
            }

            return false;
        }

        #endregion

        #region Events

        /// <summary>
        /// Get a specific event
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        /// 
        public Event GetEventById(Int32 eventId)
		{
			return context.Events.Where(r => r.EventId == eventId).FirstOrDefault();
		}

		/// <summary>
		/// Add a new event to the database.  Check only for duplicate name and date
		/// </summary>
		/// <param name="groupId"></param>
		/// <param name="name"></param>
		/// <param name="date"></param>
		/// <param name="url"></param>
		/// <param name="location"></param>
		/// <param name="description"></param>
		/// <param name="creator"></param>
		/// <returns></returns>
		/// 
		public Event AddEvent(Int32 groupId, String name, DateTime date, String url, String location, String description, String creator)
		{
			Event newEvent = new Event()
			{
				GroupId = groupId,
				Name = name,
				Date = date,
				Url = url,
				Location = location,
				Description = description,
				CreatorId = creator
			};
			context.Events.Add(newEvent);

			return newEvent;
		}

        public Event AddEvent(Event eventInfo)
        {
            return AddEvent(eventInfo.GroupId, eventInfo.Name, eventInfo.Date, eventInfo.Url, eventInfo.Location, eventInfo.Description, eventInfo.CreatorId);
        }
        
        /// <summary>
        /// Delete event and all attendees
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns></returns>
        /// 
        public bool DeleteEvent(int eventId)
        {
            var eventInfo = context.Events.Where(r => r.EventId == eventId).FirstOrDefault();
            if (eventInfo != null)
            {
                foreach(var user in eventInfo.Attendings.ToList())
                {
                    context.Attendings.Remove(user);
                }
                context.Events.Remove(eventInfo);
                context.SaveChanges();

                return true;
            }

            return false;
        }

		/// <summary>
		/// Adds user as participating in the event
		/// </summary>
		/// <param name="user"></param>
		/// <param name="newEvent"></param>
		/// 
		public void AddUserToEvent(MFUser user, Event newEvent, AttendingEnum isAttending)
		{
			Attending attending = context.Attendings.Where(r => (r.UserId == user.UserId) && (r.EventId == newEvent.EventId)).FirstOrDefault();
			if (attending == null)
			{
				attending = new Attending()
				{
					UserId = user.UserId,
					EventId = newEvent.EventId,
					Attending1 = (int)isAttending
				};
				context.Attendings.Add(attending);
			}
			attending.Attending1 = (int)isAttending;
		}

		/// <summary>
		/// Removes an user from a specific event
		/// </summary>
		/// <param name="user"></param>
		/// <param name="eEvent"></param>
		/// 
		public void RemoveUserFromEvent(MFUser user, Event eEvent)
		{
			Attending attending = context.Attendings.Where(r => (r.UserId == user.UserId) && (r.EventId == eEvent.EventId)).FirstOrDefault();
			if (attending != null)
				context.Attendings.Remove(attending);
		}

		/// <summary>
		/// Get events that the user has access to through assigned groups
		/// </summary>
		/// <param name="userId"></param>
		/// <returns></returns>
		/// 
		public List<Event> GetUserEvents(MFUser user)
		{
			// Get an array of groups the user belongs to
			//
			int[] groups = new int[user.GroupMembers.Count];
			int i = 0;
			foreach(GroupMember member in user.GroupMembers)
			{
				groups[i] = member.GroupId;
				i++;
			}

			// Return events in the matching groups
			//
			DateTime start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
			return context.Events.Where(r => groups.Contains(r.GroupId) && (r.Date >= start)).OrderBy(o => o.Date).ThenBy(o => o.Name).ToList();
		}

		/// <summary>
		/// Retrieve all users for a given event
		/// </summary>
		/// <param name="eEvent"></param>
		/// <returns></returns>
		/// 
		public List<MFUser> GetUsersForEvent(int eventId)
		{
			var participants = context.Attendings.Where(r => r.EventId == eventId).OrderBy(o => o.MFUser.LastName).ThenBy(o => o.MFUser.FirstName);

			List<MFUser> users = new List<MFUser>();
			foreach(Attending p in participants)
			{
				users.Add(GetUserById(p.UserId));
			}

			return users;
		}

		#endregion

		#region GroupMembers

		/// <summary>
		/// Determines if user is a member in the group
		/// </summary>
		/// <param name="user"></param>
		/// <param name="group"></param>
		/// <returns></returns>
		/// 
		public GroupRoleEnum IsUserInGroup(MFUser user, Group group)
		{
			GroupMember member = context.GroupMembers.Where(r => (r.UserId == user.UserId) && (r.GroupId == group.GroupId)).FirstOrDefault();
			if (member == null)
				return GroupRoleEnum.empty;

			return (GroupRoleEnum)member.Role;
		}

		/// <summary>
		/// Assigns a default group and role to the user
		/// </summary>
		/// <param name="user"></param>
		/// <param name="group"></param>
		/// <param name="role"></param>
		/// 
		public void DefaultGroup(MFUser user, Group group, GroupRoleEnum role)
		{
			if (IsUserInGroup(user, group) == GroupRoleEnum.empty)
			{
				GroupMember member = new GroupMember()
				{
					GroupId = group.GroupId,
					UserId = user.UserId,
					Role = (int)role
				};
				context.GroupMembers.Add(member);
			}
		}

		/// <summary>
		/// Returns all groups the user has access to
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		/// 
		public List<GroupMember> UserMembership(MFUser user)
		{
			return context.GroupMembers.Where(r => (r.UserId == user.UserId) && (r.Role >= (int)GroupRoleEnum.member)).ToList();
		}

		/// <summary>
		/// Returns the user group membership record
		/// </summary>
		/// <param name="user"></param>
		/// <param name="group"></param>
		/// <returns></returns>
		public GroupMember UserGroupMembership(MFUser user, Group group)
		{
			return context.GroupMembers.Where(r => (r.UserId == user.UserId) && (r.GroupId == group.GroupId) && (r.Role >= (int)GroupRoleEnum.member)).FirstOrDefault();
		}

		public GroupMember UserGroupMembership(string access_token)
		{
			return context.GroupMembers.Where(r => r.AccessToken == access_token).FirstOrDefault();
		}

		#endregion

		#region Groups

		/// <summary>
		/// Return group matching a code
		/// </summary>
		/// <param name="code"></param>
		/// <returns></returns>
		/// 
		public Group FindGroupByCode(string code)
		{
			return context.Groups.Where(r => r.Code.ToLower() == code.ToLower()).FirstOrDefault();
		}

		public Group FindGroupByCodeAndKey(string code, string key)
		{
			return context.Groups.Where(r => (r.Code.ToLower() == code.ToLower()) && (r.ApiKey.ToLower() == key.ToLower())).FirstOrDefault();
		}

		#endregion

		#region Mobile API

		/// <summary>
		/// Returns a list of upcoming events and includes a field as to whether or not the specified user is attending
		/// </summary>
		/// <param name="dtStart"></param>
		/// <param name="idUser"></param>
		/// <returns></returns>
		/// 
		public List<viewEventUserAttendance> GetUpcomingEventsForUser(DateTime dtStart, string idUser)
		{
			return context.viewEventUserAttendances.Where(r => (r.UserId == idUser) && (r.Date >= dtStart)).OrderBy(o => o.Date).ToList();
		}

        public viewEventUserAttendance GetEventViewById(int eventId, string idUser)
        {
            return context.viewEventUserAttendances.Where(r => (r.EventId == eventId) && (r.UserId == idUser)).FirstOrDefault();
        }

		#endregion
	}
}