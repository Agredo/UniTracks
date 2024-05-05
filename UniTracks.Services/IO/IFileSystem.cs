using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniTracks.Services.IO;

public interface IFileSystem
{
    public string AppDataDirectory { get; }
}
