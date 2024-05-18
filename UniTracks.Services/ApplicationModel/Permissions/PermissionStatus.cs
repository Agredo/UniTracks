using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniTracks.Services.ApplicationModel;

public enum PermissionStatus
{
    Unknown,
    Denied,
    Disabled,
    Granted,
    Restricted,
    Limited
}
