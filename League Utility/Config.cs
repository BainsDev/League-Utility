using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpConfig;
namespace AutoAccepter
{
    public static class Config
    {
        public static string LeagueFolder { get; set; }
        public static string LockFile { get; set; }

        public static Boolean Load()
        {
            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    String fullpath = Path.Combine(drive.Name, "Riot Games/League of Legends");
                    if (Directory.Exists(fullpath))
                    {
                        LeagueFolder = fullpath;
                        break;
                    }
                }

                if (String.IsNullOrEmpty(LeagueFolder))
                {
                    MessageBox.Show("League folder not found. Please make sure League is installed on your computer.", "Game Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                LockFile = LeagueFolder + "/lockfile";
            }
            catch (Exception)
            {
                MessageBox.Show("An error occurred while reading your settings.");
                return false;
            }
            return true;
        }
    }
}
