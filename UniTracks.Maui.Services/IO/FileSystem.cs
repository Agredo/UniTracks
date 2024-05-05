using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MauiFileSystem = Microsoft.Maui.Storage.FileSystem;

namespace UniTracks.Maui.Services.IO;

public class FileSystem : UniTracks.Services.IO.IFileSystem
{
    public string AppDataDirectory => MauiFileSystem.Current.AppDataDirectory;
}