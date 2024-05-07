using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniTracks.Services.ApplicationModel.DataTransfer;

public interface IShare
{
    Task ShareFile(string title, string filePath);

    Task ShareUri(string title, Uri uri);

    Task ShareText(string text);

    Task ShareFiles(string title, string[] filePaths);
}
