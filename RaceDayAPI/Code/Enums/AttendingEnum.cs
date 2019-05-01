using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace RaceDayAPI
{
    public enum AttendingEnum
    {
        [Description("Not Attending")]
        NotAttending,

        [Description("Attending")]
        Attending,

        [Description("Tentative")]
        Tentative
    }
}