using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GooDPal
{
    class Directory
    {
        private List<Directory> mSubDirectories;
        private string[] mFiles;
        private string mPath;
        private string mName;

        public Directory(string path)
        {
            this.mPath = path;
            this.mName = new DirectoryInfo(path).Name;
            this.mSubDirectories = new List<Directory>();
        }

        public void Init()
        {
            mFiles = System.IO.Directory.GetFiles(mPath);
            string[] dirs = System.IO.Directory.GetDirectories(mPath);

            foreach(string d in dirs)
            {
                Directory newDir = new Directory(d);
                newDir.Init();
                mSubDirectories.Add(newDir);
            }
        }

        public List<Directory> getSubDirectories()
        {
            return mSubDirectories;
        }

        public string[] GetFiles()
        {
            return mFiles;
        }

        public string GetName()
        {
            return mName;
        }
    }
}
