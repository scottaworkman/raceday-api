# raceday
REST API for RaceDay Web Application and mobile app for the Jordan YMCA group.

REST API now separated into separate application to support the <a href="https://github.com/scottaworkman/raceday-mobile">RaceDay Mobile</a> client application and <a href="https://github.com/scottaworkman/raceday-web">Web Application</a>. API methods include:
<ul>
<li>/LOGIN (GET) - Password request.  groupid, email, apikey query string parameters</li>
<li>/LOGIN (POST) - Login user with email/password credentials</li>
<li>/LOGIN (PUT) - Change password</li>
<li>/EVENT (GET) - retrieve list of upcoming events with user participation indicated</li>
<li>/EVENT/{ID} (GET) - retrieve specific event details and participants</li>
<li>/EVENT (POST) - add new event</li>
<li>/EVENT/{ID} (PUT) - update existing event</li>
<li>/EVENT/{ID} (DELETE) - remove existing event</li>
<li>/ATTEND (GET) - retrieve upcoming events authenticated user is attending</li>
<li>/ATTEND/{ID} (PUT) - add user as attending the specified event</li>
<li>/ATTEND/{ID} (DELETE) - remove user as attending specified event</li>
<li>/MFUSER (GET) - Return list of all users</li>
<li>/MFUSER/{ID} (GET) - retrieve specific user</li>
<li>/MFUSER (POST) - add new user</li>
<li>/MFUSER/{ID} (PUT) - update existing user</li>
<li>/MFUSER/{ID} (DELETE) - remove existing user</li>
</ul>

#Authentication
User must login using email/password to receive token used for all API calls

POST /API/LOGIN<br />
{ groupid: [JYMF Facebook group Id], userid: [user email], password: [user password] apikey: [Application API Key] }<br />
Returns:<br />
{ token: [access token], expiration: [date/time token expires], role: [(-1=empty | 1=denied | 5=member | 10=admin)] }<br />

All REST calls should include the following header:<br />
Authorization:  Bearer [access token]

