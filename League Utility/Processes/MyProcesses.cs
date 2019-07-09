using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Caching;

namespace AutoAccepter
{

    public static class MyProcesses
    {
        private static  ManagementEventWatcher _gameWatch;
        private static ManagementEventWatcher _clientWatch;

        public static void MonitorClientStart()
        {
            _clientWatch = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace WHERE ProcessName = 'LeagueClient.exe'"));
            _clientWatch.EventArrived
                += new EventArrivedEventHandler(Form1.clientWatch_EventArrived);
            _clientWatch.Start();
        }

        public static void StopMonitorClientStart()
        {
            _clientWatch.EventArrived
                -= new EventArrivedEventHandler(Form1.clientWatch_EventArrived);
            _clientWatch.Stop();
            _clientWatch.Dispose();
        }

        public static void MonitorLeagueStart()
        {
            _gameWatch = new ManagementEventWatcher(
              new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace WHERE ProcessName = 'League of Legends.exe'"));
            _gameWatch.EventArrived
                                += new EventArrivedEventHandler(Form1.gameWatch_EventArrived);
            _gameWatch.Start();
        }

        public static void StopMonitorLeagueStart()
        {
            _gameWatch.EventArrived
                -= new EventArrivedEventHandler(Form1.gameWatch_EventArrived);
            _gameWatch.Stop();
            _gameWatch.Dispose();
        }

        public static void KillExistingClients()
        {
            Task.Run(() =>
            {
                Process[] league = Process.GetProcessesByName("LeagueClient");
                if (league.Any())
                {
                    foreach (Process p in league)
                    {
                    p.Kill();
                    }
                    Thread.Sleep(5000);
                }
            });
        }

        public static void EmptyWorkingSet(Process p)
        {
            Task.Run(() =>
            {
                while (!p.HasExited)
                {
                    try
                    {
                        NativeMethods.EmptyWorkingSet(p.Handle);
                        Thread.Sleep(1000);
                    }
                    catch (Exception e)
                    {

                    }
                }
            });
        }

        public static Process GetChildLeagueProcess(int parentId)
        {
            try
            {
                var query = "Select * From Win32_Process Where ParentProcessId = "
                            + parentId;
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                foreach (ManagementObject mo in searcher.Get())
                {
                    var s = Process.GetProcessById(Convert.ToInt32(mo["ProcessID"]));
                    if (s.ProcessName == "League of Legends")
                        return s;
                }

                return null;
            }

            catch (Exception)
            {
                Console.WriteLine("Exception in get child");
                return null; //
            }
        }
    }
}