using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text; // if you want text formatting helpers (recommended)

namespace GooDPal.Cmd
{
    class CmdOptions
    {
        [Option('l', "localPath", Required = true, HelpText = "Local path to synchronize with drive.")]
        public string mLocalDirPath { get; set; }

        [Option('r', "remotePath", Required = false, DefaultValue = "", HelpText = "Remote path to the base remote directory of synchronization.")]
        public string mRemoteDirPath { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var help = new HelpText
            {
                Heading = new HeadingInfo("GooDPal", "v0.0.0.1"),
                Copyright = new CopyrightInfo("<Vlachakis Dimitris>", 2015),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine(" ");
            help.AddPreOptionsLine("Usage: GooDPal -l Path/To/Dir/For/Sync");
            help.AddPreOptionsLine("       GooDPal -l Path/To/Dir/For/Sync -r Remote/Root/Path");
            help.AddOptions(this);
            return help;
        }
    }
}
