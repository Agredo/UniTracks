using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniTracks.Common.Contants;

public static class ApplicationConstants
{
    public static string SQliteDatabaseName { get; set; } = "sqliteDatabase.db";
    public static string LiteDBName { get; set; } = "UniTracks.db";
    public static object RawIconBasePath { get; set; } = "Icons/SVG/";
}

public static class ApplicationIconConstants
{
    public static string PlayIcon { get; set; } = "play.svg";
    public static string StopIcon { get; set; } = "stop.svg";
    public static string RecordIcon { get; set; } = "record.svg";

}
