using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace RaceDayAPI
{
    public enum GroupRoleEnum
    {
        [Description("Not in Group")]
        empty = -1,

        [Description("Denied")]
        denied = 1,

        [Description("Member")]
        member = 5,

        [Description("Admin")]
        admin = 10
    }
}