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
                    await SyncDirectory(dir, dMgr);
                }).Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occured: " + e.Message);
            }

            Console.WriteLine("Press enter to continue . . .");
            Console.Read();
        }

        static async Task SyncDirectory(Directory dir, DriveManager mgr)
        {
            IList<DriveFile> files = mgr.FetchDirectories();

            // Find root id
            string rootId = "";
            foreach (DriveFile file in files)
            {
                IList<ParentReference> parents = file.Parents;
                foreach (ParentReference p in parents)
                    if (p.IsRoot.HasValue)
                        if ((bool)p.IsRoot)
                            rootId = p.Id;
            }

            // Start recursive sync process from root
            await SyncDirectoryR(dir, mgr, rootId);
        }

        static async Task SyncDirectoryR(Directory dir, DriveManager mgr, string parentId)
        {
            FileUploader uploader = new FileUploader(mgr);

            // Get directories of parent and find this folder's id in drive
            IList<DriveFile> parentChildren = mgr.FetchChildren(parentId);
            string dirId = "";
            foreach (DriveFile file in parentChildren)
                if (file.Title.Equals(dir.GetName()))
                    dirId = file.Id;

            // Folder does not exist so create it
            if (dirId.Equals(""))
                dirId = mgr.CreateDirectory(dir.GetName(), dir.GetName(), parentId).Id;

            // Get files of this directory in drive
            IList<DriveFile> childrenFiles = mgr.FetchChildrenFiles(dirId);

            // Start syncing this directory
            // Check the drive for any files that do not exist locally and delete them
            foreach (DriveFile dFile in childrenFiles)
            {
                bool existsLocally = false;
                foreach (string file in dir.GetFiles())
                {
                    if (dFile.Title.Equals(Path.GetFileName(file)))
                    {
                        existsLocally = true;
                        break;
                    }
                }

                // Delete if it doesn't exist
                if (!existsLocally)
                {
                    Console.WriteLine("Deleting " + dFile.Title);
                    mgr.DeleteFile(dFile.Id);
                }
            }

            // Upload or update files
            foreach (string file in dir.GetFiles())
            {
                // Get simple filename
                string filename = Path.GetFileName(file);

                // Try to find it on drive
                DriveFile fileInDrive = null;
                foreach (DriveFile dFile in childrenFiles)
                {
                    if (dFile.Title.Equals(filename))
                    {
                        fileInDrive = dFile;
                        break;
                    }
                }

                // If didn't found it, upload it, else update it
                if (fileInDrive != null)
                {
                    // If the local file is modified since the last time it was uploaded on drive, update it.
                    // If not, just leave it as it is. In case of error (eg. the fileInDrive.ModifiedDate is null)
                    // just upload it anyway. Better be safe than sorry :)
                    bool shouldUpload = true;
                    if (fileInDrive.ModifiedDate.HasValue)
                    {
                        DateTime localDate = System.IO.File.GetLastWriteTime(file);
                        DateTime driveDate = (DateTime)fileInDrive.ModifiedDate;

                        if (DateTime.Compare(localDate, driveDate) <= 0)
                            shouldUpload = false;
                    }

                    if (shouldUpload)
                    {
                        uploader.SetupFile(file, file, dirId);
                        Console.WriteLine("Updating " + Path.GetFileName(file));
                        await uploader.Update();
                    }
                }
                else
                {
                    uploader.SetupFile(file, file, dirId);
                    await uploader.Upload();
                }
            }

            // Now get subdirectories and sync them
            foreach (Directory subDir in dir.getSubDirectories())
                await SyncDirectoryR(subDir, mgr, dirId);
        }
    }
}
