using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using DriveFile = Google.Apis.Drive.v2.Data.File;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using GooDPal.Drive;

namespace GooDPal
{
    class Program
    {
        static void Main(string[] args)
        {
            DriveManager dMgr = new DriveManager();

            DriveService service = null;
            try
            {
                dMgr.InitService();
                service = dMgr.GetDriveService();
            }
            catch (System.AggregateException e)
            {
                Console.WriteLine("Could not gain access to drive...");
                Console.WriteLine(e.Message);
            }

            // Exit app if failed to create drive service
            if (service == null)
                System.Environment.Exit(0);

            FileUploader f = new FileUploader(dMgr);

            try
            {
                Task.Run(async () =>
                    {
                        f.Init("C:\\Users\\TheCrafter\\Desktop\\asd.txt", "asdasd", "root");
                        await f.Upload();
                        f.Init("C:\\Users\\TheCrafter\\Desktop\\asd.txt", "asdasd2", "root");
                        await f.Upload();
                    });
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occured: " + e.Message);
            }

            Console.WriteLine("Press enter to continue . . .");
            Console.Read();
        }

        static DriveFile FindFileByName(IList<DriveFile> files, string title)
        {
            foreach (Google.Apis.Drive.v2.Data.File file in files)
                if (file.Title.Equals(title))
                    return file;

            return null;
        }
    }
}
