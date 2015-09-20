﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using DriveFile = Google.Apis.Drive.v2.Data.File;


namespace GooDPal.Drive
{
    class DriveManager
    {
        private static string AppName = "GooDPal";

        private static string CredentialSavePath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            AppName + "\\.credentials");

        private DriveService mDriveService;

        public delegate void UploadProgressCallback(Google.Apis.Upload.IUploadProgress prog);
        private UploadProgressCallback mProgressCallback;

        public DriveManager()
        {
            mProgressCallback = null;
        }

        public void InitService()
        {
            // Auth id and secret from google developers console
            string clientId = "354424347588-6bb4q1o8ufba3fu32tknk8elep233s4g.apps.googleusercontent.com";
            string clientSecret = "NOSdJs3FPwfyoI62A6sCrfjg";

            //Scopes for use with the Google Drive API
            string[] scopes = new string[]
            {
                DriveService.Scope.Drive
            //  ,DriveService.Scope.DriveFile
            };

            // here is where we Request the user to give us access, or use the Refresh Token that was previously stored in %AppData%
            UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                scopes,
                Environment.UserName,
                CancellationToken.None,
                new FileDataStore(CredentialSavePath, true)).Result;

            mDriveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = AppName,
            });

            Console.WriteLine("Credential file saved to: " + CredentialSavePath);
        }

        public IList<DriveFile> FetchWithSearch(string search)
        {
            IList<DriveFile> files = new List<DriveFile>();

            //List all of the files and directories for the current user.  
            FilesResource.ListRequest request = mDriveService.Files.List();
            request.MaxResults = 1000;

            if (search != null)
                request.Q = search;

            FileList filesFeed = request.Execute();

            // Loop through until we arrive at an empty page
            while (filesFeed.Items != null)
            {
                // Adding each item  to the list.
                foreach (DriveFile item in filesFeed.Items)
                    files.Add(item);

                // We will know we are on the last page when the next page token is
                // null.
                // If this is the case, break.
                if (filesFeed.NextPageToken == null)
                    break;

                // Prepare the next page of results
                request.PageToken = filesFeed.NextPageToken;

                // Execute and process the next page request
                filesFeed = request.Execute();
            }

            return files;
        }

        public IList<DriveFile> FetchNonTrashed()
        {
            return FetchWithSearch("trashed=false");
        }

        public IList<DriveFile> FetchFiles()
        {
            return FetchWithSearch("mimeType!='application/vnd.google-apps.folder' and trashed=false");
        }

        public IList<DriveFile> FetchDirectories()
        {
            return FetchWithSearch("mimeType='application/vnd.google-apps.folder' and trashed=false");
        }

        public IList<DriveFile> FetchChildren(string parentId)
        {
            return FetchWithSearch("trashed=false and '" + parentId + "' in parents");
        }

        public IList<DriveFile> FetchChildrenFiles(string parentId)
        {
            return FetchWithSearch("mimeType!='application/vnd.google-apps.folder' and trashed=false and '" + parentId + "' in parents");
        }

        public IList<DriveFile> FetchChildrenDirectories(string parentId)
        {
            return FetchWithSearch("mimeType='application/vnd.google-apps.folder' and trashed=false and '" + parentId + "' in parents");
        }

        public DriveFile CreateDirectory(string title, string descr, string parent)
        {

            DriveFile NewDirectory = null;

            // Create metaData for a new Directory
            DriveFile body = new DriveFile();
            body.Title = title;
            body.Description = descr;
            body.MimeType = "application/vnd.google-apps.folder";
            body.Parents = new List<ParentReference>() { new ParentReference() { Id = parent} };

            FilesResource.InsertRequest request = mDriveService.Files.Insert(body);
            NewDirectory = request.Execute();

            return NewDirectory;
        }

        public async Task<DriveFile> UploadFile(string filepath, string descr, string parent)
        {

            if (System.IO.File.Exists(filepath))
            {
                DriveFile body = new DriveFile();
                body.Title = System.IO.Path.GetFileName(filepath);
                body.Description = descr;
                body.MimeType = FindMimeType(filepath);
                body.Parents = new List<ParentReference>() { new ParentReference() { Id = parent } };

                // File's content.
                byte[] byteArray = System.IO.File.ReadAllBytes(filepath);
                System.IO.MemoryStream stream = new System.IO.MemoryStream(byteArray);

                FilesResource.InsertMediaUpload request = mDriveService.Files.Insert(body, stream, FindMimeType(filepath));

                // Add upload progress callback if there is one
                if(mProgressCallback != null)
                    request.ProgressChanged += new Action<Google.Apis.Upload.IUploadProgress>(mProgressCallback);

                request.ChunkSize = 256 * 1024;
                await request.UploadAsync(CancellationToken.None);
                
                return request.ResponseBody;
            }
            else
            {
                throw new FileNotFoundException("File: " + filepath + " was not found");
            }

        }

        public async Task<DriveFile> UpdateFile(string filepath, string descr, string parent, string fileId)
        {

            if (System.IO.File.Exists(filepath))
            {
                DriveFile body = new DriveFile();
                body.Title = System.IO.Path.GetFileName(filepath);
                body.Description = descr;
                body.MimeType = FindMimeType(filepath);
                body.Parents = new List<ParentReference>() { new ParentReference() { Id = parent } };

                // File's content.
                byte[] byteArray = System.IO.File.ReadAllBytes(filepath);
                System.IO.MemoryStream stream = new System.IO.MemoryStream(byteArray);

                FilesResource.UpdateMediaUpload request = mDriveService.Files.Update(body, fileId, stream, FindMimeType(filepath));

                // Add upload progress callback if there is one
                if (mProgressCallback != null)
                    request.ProgressChanged += new Action<Google.Apis.Upload.IUploadProgress>(mProgressCallback);

                request.ChunkSize = 256 * 1024;
                await request.UploadAsync(CancellationToken.None);

                return request.ResponseBody;
            }
            else
            {
                throw new FileNotFoundException("File: " + filepath + " was not found");
            }

        }

        public void DeleteFile(string fileId)
        {
            FilesResource.DeleteRequest DeleteRequest = mDriveService.Files.Delete(fileId);
            DeleteRequest.Execute();
        }

        public DriveService GetDriveService()
        {
            return mDriveService;
        }

        public void SetProgressCallback(UploadProgressCallback callback)
        {
            this.mProgressCallback = callback;
        }

        private string FindMimeType(string fileName)
        {
            string mimeType = "application/unknown";
            string ext = System.IO.Path.GetExtension(fileName).ToLower();
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
            if (regKey != null && regKey.GetValue("Content Type") != null)
                mimeType = regKey.GetValue("Content Type").ToString();
            return mimeType;
        }

        public static DriveFile FindFileByName(IList<DriveFile> files, string title)
        {
            foreach (Google.Apis.Drive.v2.Data.File file in files)
                if (file.Title.Equals(title))
                    return file;

            return null;
        }

        public static bool IsDirectory(DriveFile file)
        {
            return file.MimeType.Equals("application/vnd.google-apps.folder");
        }
    }
}
