using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IShare = UniTracks.Services.ApplicationModel.DataTransfer.IShare;
using MauiShare = Microsoft.Maui.ApplicationModel.DataTransfer.Share;

namespace UniTracks.Maui.Services.ApplicationModel.DataTransfer
{
    public class Share : IShare
    {
        public async Task ShareFile(string title, string filePath)
        {
            await MauiShare.RequestAsync(new ShareFileRequest()
            {
                Title = title,
                File = new ShareFile(filePath)
            });
        }

        public async Task ShareFiles(string title, string[] filePaths)
        {
            await MauiShare.RequestAsync(new ShareMultipleFilesRequest()
            {
                Files = filePaths.Select(f => new ShareFile(f)).ToList(),
                Title = title
            });
        }

        public async Task ShareText(string text)
        {
            await MauiShare.RequestAsync(new ShareTextRequest()
            {
                Text = text
            });
        }

        public async Task ShareUri(string title, Uri uri)
        {
            await MauiShare.RequestAsync(new ShareTextRequest()
            {
                Uri = uri.OriginalString,
                Title = title
            });
        }
    }
}
