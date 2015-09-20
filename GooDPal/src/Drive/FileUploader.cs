using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Google.Apis.Drive.v2.Data;
using DriveFile = Google.Apis.Drive.v2.Data.File;

namespace GooDPal.Drive
{
    class FileUploader
    {
        private DriveManager mMgr;
        private string mFilepath;
        private string mDescr;
        private string mParent;

        private string mUploadBaseMsg;

        private long mFileLength;

        public FileUploader(DriveManager mgr)
        {
            this.mMgr = mgr;
            mgr.SetProgressCallback(UploadProgressCallback);
        }

        public void SetupFile(string filepath, string descr, string parent)
        {
            this.mFilepath = filepath;
            this.mDescr = descr;
            this.mParent = parent;
        }

        public async Task Upload()
        {
            // Find file length
            FileInfo info = new FileInfo(mFilepath);
            mFileLength = info.Length;

            // Set base message
            mUploadBaseMsg = "Uploading";

            await mMgr.UploadFile(mFilepath, mDescr, mParent);
        }

        public async Task Update()
        {
            // Find file length
            FileInfo info = new FileInfo(mFilepath);
            mFileLength = info.Length;

            // Set base message
            mUploadBaseMsg = "Updating";

            DriveFile file = DriveManager.FindFileByName(mMgr.FetchNonTrashed(), Path.GetFileName(mFilepath));

            if (file != null)
                await mMgr.UpdateFile(mFilepath, mDescr, mParent, file.Id);
            else
                throw new Exception("Could not find this file on Drive.");
        }

        private void UploadProgressCallback(Google.Apis.Upload.IUploadProgress prog)
        {
            string uploadMsg = mUploadBaseMsg + " -- " + Path.GetFileName(mFilepath) + " ->";

            switch (prog.Status)
            {
                case Google.Apis.Upload.UploadStatus.Starting:
                {
                    Console.Write(uploadMsg + "  0%");
                    break;
                }

                case Google.Apis.Upload.UploadStatus.Uploading:
                {
                    long percentage = 100 * prog.BytesSent / mFileLength;

                    // Remove one extra character for percentages over 10%
                    if (percentage >= 10)
                        Console.Write("\b");

                    Console.Write("\b\b" + (100 * prog.BytesSent / mFileLength) + "%");
                    break;
                }

                case Google.Apis.Upload.UploadStatus.Completed:
                {
                    for (int i = 0; i < uploadMsg.Length + 4; i++)
                    {
                        Console.Write("\b");
                    }

                    Console.WriteLine(uploadMsg + " 100%");

                    break;
                }
            }
        }
    }
}
