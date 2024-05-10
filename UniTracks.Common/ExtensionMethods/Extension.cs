using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniTracks.Common.ExtensionMethods;

public static class Extension
{
    public static void Await(this Task task)
    {
        task.Wait();
    }
}
