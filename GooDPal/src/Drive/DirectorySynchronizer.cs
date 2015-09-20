﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DriveFile = Google.Apis.Drive.v2.Data.File;

namespace GooDPal.Drive
{
    class DirectorySynchronizer
    {
        private DriveManager mDMgr;
        private FileUploader mUploader;

        public DirectorySynchronizer(DriveManager mgr)
        {
            this.mDMgr = mgr;
            this.mUploader = new FileUploader(mDMgr);
        }

        public async Task SyncDirectory(Directory dir, string parentId)
        {
            // Get directories of parent and find this folder's id in drive
            IList<DriveFile> parentChildren = mDMgr.FetchChildren(parentId);
            string dirId = "";
            foreach (DriveFile file in parentChildren)
                if (file.Title.Equals(dir.GetName()))
                    dirId = file.Id;

            // Folder does not exist so create it
            if (dirId.Equals(""))
                dirId = mDMgr.CreateDirectory(dir.GetName(), dir.GetName(), parentId).Id;

            // Get files of this directory in drive
            IList<DriveFile> childrenFiles = mDMgr.FetchChildrenFiles(dirId);

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
                    mDMgr.DeleteFile(dFile.Id);
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
                        mUploader.SetupFile(file, file, dirId);
                        await mUploader.Update();
                    }
                }
                else
                {
                    mUploader.SetupFile(file, file, dirId);
                    await mUploader.Upload();
                }
            }

            // Now get subdirectories and sync them
            foreach (Directory subDir in dir.getSubDirectories())
                await SyncDirectory(subDir, dirId);
        }
    }
}
