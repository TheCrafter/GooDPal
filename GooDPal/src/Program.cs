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

            // Create directory structure from path
            Directory dir = new Directory("C:\\Users\\TheCrafter\\Desktop\\Test");
            dir.Init();

            try
            {
                Task.Run(async () =>
                {
                    Drive.DirectorySynchronizer ds = new DirectorySynchronizer(dMgr);
                    await ds.SyncDirectory(dir, "root");
                }).Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occured: " + e.Message);
            }

            Console.WriteLine("Press enter to continue . . .");
            Console.Read();
        }
    }
}
