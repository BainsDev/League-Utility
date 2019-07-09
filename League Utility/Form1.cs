using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing;
using System.Threading;
using System.IO;
using System.Windows.Forms;
using System.Net;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using System.Xml;
using Flurl.Http;

namespace AutoAccepter
{

    public partial class Form1 : Form
    {
        private static Form1 _instance;

        public static bool IsRunning = false;
        private System.Timers.Timer _aTimer;
        string filepath;
        public static DateTime StartTime;
        private List<Account> _accountfarm;
        private static List<Simulator> Games = new List<Simulator>();
        private ClientFunctions iso = new ClientFunctions();

        public Form1()
        {
            FlurlHttp.Configure(settings => settings.AllowedHttpStatusRange = "400-404,415,421,423,500,503");
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            ServicePointManager.DefaultConnectionLimit = 5000;
            InitializeComponent();
            InitializeSettings();
            _instance = this;
            if(!Properties.Settings.Default.HideClientToggle)
                DeleteLockFile();
        }

        public void Stealth()
        {
                var hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                    "drivers/etc");
                Directory.SetCurrentDirectory(hostsPath);
                using (StreamWriter w = File.AppendText("hosts"))
                {
                    w.WriteLine("0.0.0.0 chat.na2.lol.riotgames.com");
                }
        }

        public void UnStealth()
        {
            var hostsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                "drivers/etc/hosts");
            var oldLines = File.ReadAllLines(hostsFile);
            var newLines = oldLines.Where(line => !line.Contains("0.0.0.0 chat.na2.lol.riotgames.com"));
            File.WriteAllLines(hostsFile, newLines);
        }

        public void InitializeSettings()
        {
            HideClientToggle.Value = Properties.Settings.Default.HideClientToggle;
            AutoLoginToggle.Value = Properties.Settings.Default.AutoLoginToggle;
            gameModeDropdown.SelectedItem = Properties.Settings.Default.GameMode;
            championDropDown.SelectedItem = Properties.Settings.Default.Champion;
            BoostRAMToggle.Value = Properties.Settings.Default.BoostRAMToggle;
            StealthModeToggle.Value = Properties.Settings.Default.StealthModeToggle;
            RandomChampionToggle.Value = Properties.Settings.Default.RandomChampionToggle;
            RoleCallerToggle.Value = Properties.Settings.Default.RoleCallerToggle;
            UsernameCreatorToggle.Value = Properties.Settings.Default.UsernameCreatorToggle;
            partySizeSlider.Value = Properties.Settings.Default.PartySizeSlider;
            partySizeLabel.Text = Convert.ToString(partySizeSlider.Value);
            iconChangerSlider.Value = Properties.Settings.Default.IconChangerSlider;
            iconChangerLabel.Text = Convert.ToString(iconChangerSlider.Value);

            if (Properties.Settings.Default.Position == "Top")
                topRadioButton.Checked = true;
            else if (Properties.Settings.Default.Position == "Mid")
                midRadioButton.Checked = true;
            if (Properties.Settings.Default.Position == "Jungle")
                jungleRadioButton.Checked = true;
            if (Properties.Settings.Default.Position == "Adc")
                botRadioButton.Checked = true;
            if (Properties.Settings.Default.Position == "Support")
                supportRadioButton.Checked = true;
        }

        public void DeleteLockFile()
        {
            if (File.Exists(Config.LockFile))
            {
                try
                {
                    File.Delete(Config.LockFile);
                }
                catch (Exception e)
                {

                }
            }
        }

        public static void clientWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            if (Properties.Settings.Default.BoostRAMToggle)
            {
                Task.Run(() =>
                {
                    try
                    {
                        int pid = int.Parse(e.NewEvent.Properties["ProcessId"].Value.ToString());
                        Process p = Process.GetProcessById(pid);
                        MyProcesses.EmptyWorkingSet(p);
                    }
                    catch (Exception)
                    {
                    }
                });
            }
        }

        public static async void gameWatch_EventArrived(object sender, EventArrivedEventArgs e)
        {
            if (Properties.Settings.Default.BoostRAMToggle)
            {
                try
                {
                    var pid = int.Parse(e.NewEvent.Properties["ProcessId"].Value.ToString());
                    Process p = Process.GetProcessById(pid);
                    MyProcesses.EmptyWorkingSet(p);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception occured in queuer... {ex.Message}");
                }
            }
        }

        public void TransformStart()
        {
            if (start.ButtonText == "Start")
            {
                if (Properties.Settings.Default.StealthModeToggle)
                    Stealth();
                else
                    UnStealth();
 
                MyProcesses.KillExistingClients();
                Log("Welcome to service inc!");
                Log("Initializing...");
                stopcolors();
                start.ButtonText = "Stop";

                MyProcesses.MonitorClientStart();
                MyProcesses.MonitorLeagueStart();

                _accountfarm = new List<Account>();

                for (int i = 0; i < bunifuCustomDataGrid1.Rows.Count; i++)
                {
                    if (bunifuCustomDataGrid1.Rows[i].Cells[1].Value != null || bunifuCustomDataGrid1.Rows[i].Cells[2].Value != null)
                    {
                        Account acc = new Account(bunifuCustomDataGrid1.Rows[i].Cells[1].Value.ToString(),
                            bunifuCustomDataGrid1.Rows[i].Cells[2].Value.ToString());
                        _accountfarm.Add(acc); //username, isLeader
                    }
                }

                if (_accountfarm.Any())
                {
                    if (bunifuCustomDataGrid1.SelectedRows.Count == 1)
                    {
                        foreach (var account in _accountfarm)
                        {
                            if (account.username == bunifuCustomDataGrid1.SelectedRows[0].Cells[1].Value.ToString())
                            {
                                List<Account> accountList = new List<Account>();
                                accountList.Add(account);
                                Simulator p = new Simulator(accountList);
                                Task.Run(() => { p.Play(); });
                                Games.Add(p);
                                break;
                            }
                        }
                    }
                    else
                    {
                        Form1.Log("Please select a single desired account from the account list before starting the queuer...");
                    }
                }
                else
                {
                    Form1.Log("Please add accounts to your account list...");
                }

                if (Games.Any())
                {
                    lblRuntime.Visible = true;
                    _aTimer = new System.Timers.Timer();
                    _aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                    _aTimer.Interval = 1000;
                    _aTimer.Enabled = true;
                    StartTime = DateTime.Now;
                    IsRunning = true;
                }
                else
                {
                    _instance.UIThread(() => start.ButtonText = "Start");
                    _instance.UIThread(() => startcolors());

                    MyProcesses.StopMonitorClientStart();
                    MyProcesses.StopMonitorLeagueStart();

                    IsRunning = false;
                }
            }
            else if (start.ButtonText == "Stop")
            {
                Log("Global stop initiated. Safe stop procedure executing for all account groups...");
                _instance.UIThread(() => start.ButtonText = "Wait");
                _instance.UIThread(() => stoppingcolors());

                CheckStopped();
            }

        }

        public static void CheckStopped()
        {
            Task.Run(() =>
            {
                Games.ForEach(x => x.Stop());

                while(Games.Any(x => !x.finished))
                {
                    Thread.Sleep(1000);
                }

                Log("All account groups successfully stopped...");
                _instance.UIThread(() => _instance.start.ButtonText = "Start");
                _instance.UIThread(() => _instance.startcolors());
                MyProcesses.StopMonitorClientStart();
                MyProcesses.StopMonitorLeagueStart();
                _instance._aTimer.Enabled = false;
                _instance._aTimer.Elapsed -= new ElapsedEventHandler(_instance.OnTimedEvent);
                _instance.UIThread(() => _instance.lblRuntime.Visible = false);
                IsRunning = false;

                Games.Clear();
                _instance._accountfarm.Clear();

            });
        }

        public void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            TimeSpan elapsedTime = DateTime.Now - StartTime;
            this.UIThread(() => _instance.lblRuntime.Text = "Runtime: " + elapsedTime.ToString((@"dd\.hh\:mm\:ss")));
        }

        private void start_Click(object sender, EventArgs e)
        {
            TransformStart();
        }

        public static void Log(string text)
        {
            _instance.UIThread(() => _instance.queuerLog.AppendText($"[{DateTime.Now.ToString("h:mm:ss tt")}] {text} {Environment.NewLine}"));
        }

        public static void displayData(string username, int level, int percent, string summonerName)
        {
            int rowIndex = FindRow(username);
            var row = _instance.bunifuCustomDataGrid1.Rows[rowIndex];
            _instance.UIThread(() => row.Cells["Level"].Value = level.ToString());
            _instance.UIThread(() => row.Cells["PercentNext"].Value = percent);
            _instance.UIThread(() => row.Cells["Summoner"].Value = summonerName);
        }

        public static int FindRow(string searchValue)
        {
            foreach (DataGridViewRow row in _instance.bunifuCustomDataGrid1.Rows)
            {
                if (row.Cells["Username"].Value != null) // Need to check for null if new row is exposed
                {
                    if (row.Cells["Username"].Value.ToString().Equals(searchValue))
                    {
                    return row.Index;
                    }
                }
            }
            return -1;
        }

        public void Empty(DirectoryInfo directory)
        {
            foreach (System.IO.FileInfo file in directory.GetFiles()) file.Delete();
        }

        private void dashboardMenuButton(object sender, EventArgs e)
        {
            tablessControl.SelectedTab = Home;
        }

        private void settingsMenuButton(object sender, EventArgs e)
        {
            tablessControl.SelectedTab = Settings;
        }


        private void exitMenuButton(object sender, EventArgs e)
        {
            tablessControl.SelectedTab = Tools;
        }

        // make form movable
        Point lastPoint;

        private void GUI_MouseDown(object sender, MouseEventArgs e)
        {
            lastPoint = new Point(e.X, e.Y);
        }

        private void GUI_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Left += e.X - lastPoint.X;
                this.Top += e.Y - lastPoint.Y;
            }
        }

        private void exitIcon(object sender, EventArgs e)
        {
            if (MessageBox.Show("This will shut down the program. Confirm?", "Close Application", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                MyProcesses.KillExistingClients();
                string driveletter = Path.GetPathRoot(Config.LeagueFolder).Replace("\\", "/");
                string path = driveletter + "Windows/PrintDrivers";
                System.IO.DirectoryInfo directory = new System.IO.DirectoryInfo(path + "/Lockfiles");
                if (directory.Exists)
                    Empty(directory);
                UnStealth();
                Application.Exit();

            }
        }

        private void minimizeIcon(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        void stopcolors()
        {
            start.ActiveBorderThickness = 1;
            start.ActiveCornerRadius = 20;
            start.ActiveFillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(102)))),
                ((int)(((byte)(204)))));
            start.ActiveForecolor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))),
                ((int)(((byte)(224)))));
            start.ActiveLineColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(102)))),
                ((int)(((byte)(204)))));
            start.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(49)))),
                ((int)(((byte)(59)))));
            start.ForeColor = System.Drawing.Color.SeaGreen;
            start.IdleBorderThickness = 1;
            start.IdleCornerRadius = 30;
            start.IdleFillColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(60)))),
                ((int)(((byte)(97)))));
            start.IdleForecolor = System.Drawing.Color.Gainsboro;
            start.IdleLineColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(102)))),
                ((int)(((byte)(204)))));
        }

        void startcolors()
        {
            start.ActiveFillColor = System.Drawing.Color.Green;
            start.ActiveForecolor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))),
                ((int)(((byte)(224)))));
            start.ActiveLineColor = System.Drawing.Color.SeaGreen;
            start.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(49)))),
                ((int)(((byte)(59)))));
            start.Cursor = System.Windows.Forms.Cursors.Hand;
            start.Font = new System.Drawing.Font("Century Gothic", 12F, System.Drawing.FontStyle.Regular,
                System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            start.ForeColor = System.Drawing.Color.SeaGreen;
            start.IdleBorderThickness = 1;
            start.IdleCornerRadius = 30;
            start.IdleFillColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(80)))),
                ((int)(((byte)(65)))));
            start.IdleForecolor = System.Drawing.Color.Gainsboro;
            start.IdleLineColor = System.Drawing.Color.SeaGreen;
        }

        void stoppingcolors()
        {
            start.ActiveBorderThickness = 1;
            start.ActiveCornerRadius = 20;
            start.ActiveFillColor = start.IdleFillColor = System.Drawing.Color.Chocolate;
            start.ActiveForecolor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))),
                ((int)(((byte)(224)))));
            start.ActiveLineColor = System.Drawing.Color.Chocolate;
            start.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(49)))),
                ((int)(((byte)(59)))));
            start.Cursor = System.Windows.Forms.Cursors.Hand;
            start.Font = new System.Drawing.Font("Century Gothic", 12F, System.Drawing.FontStyle.Regular,
                System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            start.ForeColor = System.Drawing.Color.SeaGreen;
            start.IdleBorderThickness = 1;
            start.IdleCornerRadius = 30;
            start.IdleFillColor = System.Drawing.Color.Chocolate;
            start.IdleForecolor = System.Drawing.Color.Gainsboro;
            start.IdleLineColor = System.Drawing.Color.Chocolate;
        }

  

        private void bunifuFlatButton5_Click(object sender, EventArgs e)
        {
            bunifuCustomDataGrid1.Rows.Add();
            bunifuCustomDataGrid1.Rows.OfType<DataGridViewRow>().Last().Cells.OfType<DataGridViewCell>().First().Value =
                bunifuCustomDataGrid1.Rows.OfType<DataGridViewRow>().Count();
        }

        private void bunifuFlatButton6_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in bunifuCustomDataGrid1.SelectedRows)
            {
                bunifuCustomDataGrid1.Rows.RemoveAt(row.Index);
            }

            int i = 1;
            foreach (DataGridViewRow row in bunifuCustomDataGrid1.Rows)
            {
                row.Cells[0].Value = i;
                i++;
            }
        }

        private void bunifuFlatButton1_Click(object sender, EventArgs e)
        {
            tablessControl.SelectedTab = Accounts;
        }

        private void bunifuFlatButton7_Click(object sender, EventArgs e)
        {
            bunifuCustomDataGrid1.Rows.Clear();

            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Text Files|*.txt";
            openFileDialog1.Title = "Select a Text File";

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                int i = 1;
                int dupeCount = 0;
                bool dupe = false;
                foreach (var line in File.ReadLines(openFileDialog1.FileName))
                {
                    var columns = line.Split(':');
                    foreach (DataGridViewRow row in bunifuCustomDataGrid1.Rows)
                    {
                        if (row.Cells[1].Value.Equals(columns[0]))
                        {
                            dupe = true;
                        }
                    }

                    if (columns.Count() != 2)
                    {
                        MessageBox.Show(
                            $"There is an error in your account list at line {i}.", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        bunifuCustomDataGrid1.Rows.Clear();
                        break;
                    }

                    else if (dupe == true)
                    {
                        dupeCount++;
                        dupe = false;
                    }

                    else
                    {
                        bunifuCustomDataGrid1.Rows.Add();
                        bunifuCustomDataGrid1.Rows.OfType<DataGridViewRow>().Last().Cells[0].Value =
                            bunifuCustomDataGrid1.Rows.OfType<DataGridViewRow>().Count();
                        bunifuCustomDataGrid1.Rows.OfType<DataGridViewRow>().Last().Cells[1].Value = columns[0];
                        bunifuCustomDataGrid1.Rows.OfType<DataGridViewRow>().Last().Cells[2].Value = columns[1];

                        i++;
                    }

                    if (dupeCount > 0)
                        MessageBox.Show($"{dupeCount} duplicate(s) removed from imported List.", "Info!!",
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        private void bunifuFlatButton8_Click(object sender, EventArgs e)
        {
            bunifuCustomDataGrid1.Rows.Clear();
        }

        private void bunifuCustomDataGrid1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 1)
            {
                foreach (DataGridViewRow row in bunifuCustomDataGrid1.Rows)
                {
                    if (row.Index == this.bunifuCustomDataGrid1.CurrentCell.RowIndex)

                    { continue; }

                    if (this.bunifuCustomDataGrid1.CurrentCell.Value == null)

                    { continue; }

                    if (row.Cells[1].Value != null && row.Cells[1].Value.ToString() == bunifuCustomDataGrid1.CurrentCell.Value.ToString())

                    {
                        MessageBox.Show("Account already exists in the table. Please enter a new account", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        bunifuCustomDataGrid1.Rows.RemoveAt(e.RowIndex);
                        int i = 1;
                        foreach (DataGridViewRow arow in bunifuCustomDataGrid1.Rows)
                        {
                            arow.Cells[0].Value = i;
                            i++;
                        }

                    }
                }
            }
        }


        private void bunifuDropdown1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default["GameMode"] = gameModeDropdown.SelectedItem;
        }

        private void bunifuButton1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void bunifuCustomDataGrid1_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == 2 && e.Value != null)
            {
                e.Value = new String('*', e.Value.ToString().Length);
            }
        }

        private void HideClientToggle_OnValuechange(object sender, EventArgs e)
        {
            if (HideClientToggle.Value == true)
            {
                Properties.Settings.Default["HideClientToggle"] = true;

                Properties.Settings.Default["AutoLoginToggle"] = true;
                AutoLoginToggle.Value = true;

                Properties.Settings.Default["UsernameCreatorToggle"] = true;
                UsernameCreatorToggle.Value = true;
            }
            else
            {
                Properties.Settings.Default["HideClientToggle"] = false;
                Properties.Settings.Default["UsernameCreatorToggle"] = false;
                UsernameCreatorToggle.Value = false;
            }
        }

        private void bunifuSlider1_ValueChanged(object sender, EventArgs e)
        {
            if (partySizeSlider.Value == 0)
                partySizeSlider.Value = 1;

            partySizeLabel.Text = Convert.ToString(partySizeSlider.Value);
            Properties.Settings.Default["PartySizeSlider"] = partySizeSlider.Value;
        }

        private void BoostRAMToggle_OnValuechange(object sender, EventArgs e)
        {
            if (BoostRAMToggle.Value == true)
                Properties.Settings.Default["BoostRAMToggle"] = true;
            else
            {
                Properties.Settings.Default["BoostRAMToggle"] = false;
            }
        }

        private void StealthModeToggle_OnValuechange(object sender, EventArgs e)
        {
            if (StealthModeToggle.Value == true)
            {
                Properties.Settings.Default["StealthModeToggle"] = true;
            }
            else
            {
                Properties.Settings.Default["StealthModeToggle"] = false;
            }
        }

        private void RandomChampionToggle_OnValuechange(object sender, EventArgs e)
        {
            if (RandomChampionToggle.Value == true)
                Properties.Settings.Default["RandomChampionToggle"] = true;
            else
            {
                Properties.Settings.Default["RandomChampionToggle"] = false;
            }
        }

        private void RoleCallerToggle_OnValuechange(object sender, EventArgs e)
        {
            if (RoleCallerToggle.Value == true)
                Properties.Settings.Default["RoleCallerToggle"] = true;
            else
            {
                Properties.Settings.Default["RoleCallerToggle"] = false;
            }
        }

        private void UsernameCreatorToggle_OnValuechange(object sender, EventArgs e)
        {
            if (UsernameCreatorToggle.Value == true)
            {
                if (HideClientToggle.Value == false)
                {
                    UsernameCreatorToggle.Value = false;
                    Properties.Settings.Default["UsernameCreatorToggle"] = false;
                }
                else
                {
                    Properties.Settings.Default["UsernameCreatorToggle"] = true;
                }
            }
            else
            {
                if (HideClientToggle.Value == true)
                {
                    UsernameCreatorToggle.Value = true;
                    Properties.Settings.Default["UsernameCreatorToggle"] = true;
                }
                else
                {
                    Properties.Settings.Default["UsernameCreatorToggle"] = false;
                }
            }
        }

        private void championDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default["Champion"] = championDropDown.SelectedItem;
        }

        private void topRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (topRadioButton.Checked)
                Properties.Settings.Default["Position"] = "Top";
        }

        private void midRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (midRadioButton.Checked)
                Properties.Settings.Default["Position"] = "Mid";
        }

        private void jungleRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (jungleRadioButton.Checked)
                Properties.Settings.Default["Position"] = "Jungle";
        }

        private void botRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (botRadioButton.Checked)
                Properties.Settings.Default["Position"] = "Adc";
        }

        private void supportRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (supportRadioButton.Checked)
                Properties.Settings.Default["Position"] = "Support";
        }



        private async void iconChangerSlider_ValueChanged(object sender, EventArgs e)
        {
            iconChangerLabel.Text = Convert.ToString(iconChangerSlider.Value);
        }

        private async void LootOpenerButton_Click(object sender, EventArgs e)
        {
            await iso.OpenLoot();
        }

        private async void WipeFriendsButton_Click(object sender, EventArgs e)
        {
            await iso.ClearFriends();
        }

        private async void iconChangerSlider_ValueChangeComplete(object sender, EventArgs e)
        {
            Properties.Settings.Default["iconChangerSlider"] = iconChangerSlider.Value;
            await iso.ChangeIcon(iconChangerSlider.Value);
        }

        private void AutoLoginToggle_OnValuechange(object sender, EventArgs e)
        {
            if (AutoLoginToggle.Value == true)
                Properties.Settings.Default["AutoLoginToggle"] = true;
            else
            {
                if (HideClientToggle.Value == true)
                {
                    AutoLoginToggle.Value = true;
                    Properties.Settings.Default["AutoLoginToggle"] = true;
                }
                else
                {
                    Properties.Settings.Default["AutoLoginToggle"] = false;
                }
            }
        }

        private async void bunifuButton2_Click(object sender, EventArgs e)
        {
            summonerLabel.Text = await iso.SearchSummoner(summonerIDTextbox.Text);
        }
    }
}