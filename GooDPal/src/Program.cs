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
        static string LocalDirPath = "C:\\Users\\TheCrafter\\Desktop\\Test";
        static string RemoteDirPath = "";

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
            Directory dir = new Directory(LocalDirPath);
            dir.Init();

            // Find root sync folder id
            string syncRootId = FindRemoteFolderIdByPath(dMgr, "root", ParseSyncRootString(RemoteDirPath));
            
            if(syncRootId == null)
            {
                Console.WriteLine("The path you requested is invalid.");
                Environment.Exit(1);
            }

            try
            {
                Task.Run(async () =>
                {
                    Drive.DirectorySynchronizer ds = new DirectorySynchronizer(dMgr);
                    await ds.SyncDirectory(dir, syncRootId);
                }).Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occured while synchronization process:\n" + e.Message);
            }

            Console.WriteLine("Press enter to continue . . .");
            Console.Read();
        }

        static List<string> ParseSyncRootString(string str)
        {
            string[] separators = new string[] { "/" };
            return new List<string>(str.Split(separators, StringSplitOptions.RemoveEmptyEntries));
        }

        static string FindRemoteFolderIdByPath(DriveManager mgr, string id, List<string> foldersList)
        {
            IList<DriveFile> dirs = mgr.FetchChildrenDirectories(id);

            // If at this point the list is empty it only means one thing.
            // We're trying to find an empty path (""). The empty path
            // is a special case and it means the root directory. So
            // just return the root directory id.
            if (foldersList.Count == 0)
                return "root";

            string curDir = foldersList[0];
            foldersList.RemoveAt(0);

            foreach (DriveFile dir in dirs)
            {
                if (dir.Title.Equals(curDir))
                {
                    // We dont have any more directories to search. This is it!!
                    if (foldersList.Count == 0)
                        return dir.Id;
                    else
                        // Continue the search to the next directory
                        return FindRemoteFolderIdByPath(mgr, dir.Id, foldersList);
                }
            }

            return null;
        }
    }
}
