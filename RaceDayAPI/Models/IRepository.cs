using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaceDayAPI.Models
{
	public interface IRepository
	{
		void SaveChanges();
		void DiscardAndReset();

		MFUser GetUserById(string userId);
        MFUser GetUserByEmail(string email);
        MFUser GetUserByAuthentication(string email, string password);
		MFUser CreateUser(/*Facebook.FacebookUser*/ MFUser user);
        List<MFUser> GetAllUsers(Int32 groupId);
        bool DeleteUser(string email);

		void DefaultGroup(MFUser user, Group group, GroupRoleEnum role);
		GroupRoleEnum IsUserInGroup(MFUser user, Group group);
		List<GroupMember> UserMembership(MFUser user);

		Group FindGroupByCode(string code);
		Group FindGroupByCodeAndKey(string code, string key);

		Event GetEventById(Int32 eventId);
		Event AddEvent(Int32 groupId, String name, DateTime date, String url, String location, String description, String creator);
        Event AddEvent(Event eventInfo);
        bool DeleteEvent(int eventId);
		void AddUserToEvent(MFUser user, Event newEvent, AttendingEnum isAttending);
		void RemoveUserFromEvent(MFUser user, Event eEvent);
		List<Event> GetUserEvents(MFUser user);
		List<MFUser> GetUsersForEvent(int eventId);

		List<viewEventUserAttendance> GetUpcomingEventsForUser(DateTime dtStart, string idUser);
	}
}
