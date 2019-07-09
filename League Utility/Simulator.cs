using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoAccepter.Properties;

namespace AutoAccepter
{
    public class Simulator
    {
        public bool finished;
        private List<Account> accountList;

        public Simulator(List<Account> accountList)
        {
            tokenSource =
                new CancellationTokenSource(); //creates cancellation token and sends cancel requests to all copies of the token
            token = tokenSource.Token; // the actual token
            this.accountList = accountList;
                accountList[0].isLeader = true;
        }

        private CancellationTokenSource tokenSource { get; }
        private CancellationToken token { get; }
      
        public void Play()
        {
            // one-time startup routine
            Task.Run(() =>
            {
                if (Settings.Default.AutoLoginToggle)
                {
                    var boolTaskList = new List<Task<bool>>();
                    var TaskList = new List<Task>();

                    accountList.ForEach(account => { account.API.StartClient(); });

                    accountList.ForEach(account => { account.API.SetConnection(); });

                    Form1.Log($"{accountList[0].username}: Webserver credentials obtained from LCU...");

                    if (token.IsCancellationRequested)
                    {
                        finished = true;
                        return;
                    }

                    accountList.ForEach(account =>
                    {
                        boolTaskList.Add(account.API.Login(account.username, account.password));
                    });
                    Task.WaitAll(boolTaskList.ToArray());

                    if (boolTaskList.Any(x => x.Result == false))
                    {
                        Form1.Log("Account group execution stopping...");
                        finished = true;
                        Form1.CheckStopped();
                        return;
                    }
                    else
                        Form1.Log("All accounts successfully signed in...");

                    if (token.IsCancellationRequested)
                    {
                        finished = true;
                        return;
                    }

                    accountList.ForEach(account => { TaskList.Add(account.API.UpdateData()); });
                    Task.WaitAll(TaskList.ToArray());

                    if (token.IsCancellationRequested)
                    {
                        finished = true;
                        return;
                    }

                    boolTaskList.Clear();

                    accountList.ForEach(account => { boolTaskList.Add(account.API.IsInGame()); });
                    Task.WaitAll(boolTaskList.ToArray());

                    if (token.IsCancellationRequested)
                    {
                        finished = true;
                        return;
                    }

                    if (boolTaskList.All(x => x.Result == false))
                    {
                        TaskList.Add(accountList[0].API.CheckExistingParty());
                        Task.WaitAll(TaskList.ToArray());

                        if (token.IsCancellationRequested)
                        {
                            finished = true;
                            return;
                        }

                        accountList.ForEach(account => { account.Subscribe(); });

                        Form1.Log($"{accountList[0].username}: Creating a new party..");

                        TaskList.Add(accountList[0].API.CreateParty());
                        Task.WaitAll(TaskList.ToArray());

                        if (token.IsCancellationRequested)
                        {
                            finished = true;
                            return;
                        }

                        Form1.Log("Leader's Party ID is " + accountList[0].PartyID);

                        Form1.Log($"{accountList[0].username}: Leader is waiting for all members to join the party...");

                        foreach (var account in accountList.Skip(1))
                            TaskList.Add(account.API.JoinParty(accountList[0].PartyID));
                        Task.WaitAll(TaskList.ToArray());

                        Form1.Log($"{accountList[0].username}: All members have joined...");


                        if (token.IsCancellationRequested)
                            return;

                    }
                    else
                    {
                        Form1.Log($"{accountList[0].username}: We are already in a game...");
                    }

                    finished = true;
                }
                else
                {
                    Form1.Log("We are executing the user initiated mode...");
                    accountList.ForEach(account => { account.API.StartClient(); });
                    accountList.ForEach(account => { account.API.SetConnection(); });
                    accountList.ForEach(account => { account.Subscribe(); });
                    finished = true;
                }

            }, token);
        }

        public void Stop()
        {
            tokenSource.Cancel();
            accountList.ForEach(account => { account.Unsubscribe(); });
        }
    }
}