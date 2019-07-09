using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoAccepter.Properties;
using WebSocketSharp;

namespace AutoAccepter
{
    public class Account
    {
        public readonly string username;
        public readonly string password;
        public Process LeagueClient;
        public bool isLeader;
        public string PartyID;
        private static WebSocket socket;
        public ClientFunctions API;
        public bool dodgeNotified = false;

        public Account(string Username, string Password)
        {
            username = Username;
            password = Password;
            API = new ClientFunctions(Username, this);
        }

        public void Subscribe()
        {
                socket = new WebSocket("wss://127.0.0.1:" + API.clientPort + "/", "wamp");
                socket.SetCredentials("riot", API.clientPassword, true);
                socket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
                socket.SslConfiguration.ServerCertificateValidationCallback +=
                    (sender, certificate, chain, sslPolicyErrors) => true;
                socket.OnMessage += OnMessage;
                socket.OnClose += OnClose;
                socket.Connect();
                if (socket.IsAlive)
                    Form1.Log("Socket connection successfully established with the LCU...");
                socket.Send("[5,\"OnJsonApiEvent\"]");
        }

        private async void OnMessage(object sender, MessageEventArgs originalMessage)
        {
            if (!originalMessage.IsText) return;

            var body = SimpleJson.DeserializeObject<JsonArray>(originalMessage.Data);

            if (body == null) return;

            dynamic typeID = body[0]; //subscription type, ie. 8

            if (typeID != 8) return;

            dynamic
                message = body[
                    2]; //the actual event from the body variable, 0 and 1 are the subscription type 8, and 1 is "OnJSONApiEvent"

            var uri = message["uri"];
            var eventType = message["eventType"];
            var messageData = message["data"];

            String finalMessage = "[" + uri + "\", " + eventType + ", " + messageData + "]";

            if (isLeader)
                Console.WriteLine(finalMessage);

            if (uri.Equals("/lol-gameflow/v1/gameflow-phase") && eventType.Equals("Update"))
            {
                if (messageData.Equals("Lobby"))
                {
                    if (isLeader)
                    {
                        Form1.Log($"{username}: We are in the lobby...");
                    }
                }
                else if (messageData.Equals("Matchmaking"))
                {
                    if (isLeader)
                    {
                        dodgeNotified = false;
                        Form1.Log($"{username}: Successfully placed in a queue, searching for a match...");
                    }
                }
                else if (messageData.Equals("ReadyCheck"))
                {
                    if (isLeader)
                        Form1.Log($"{username}: We have found a match, accepting...");
                    await API.AcceptMatch();
                }
                else if (messageData.Equals("ChampSelect"))
                {
                    if (isLeader)
                    {
                        Form1.Log($"{username}: We are in champ select");
                        if(Settings.Default.RoleCallerToggle && !Settings.Default.HideClientToggle)
                        {
                            await Task.Delay(2000);
                            await API.CallPosition();
                        }
                    }
                }
                else if (messageData.Equals("GameStart"))
                {
                    Form1.Log($"{username}: Game has successfully started...");
                }
                else if (messageData.Equals("Reconnect"))
                {
                    Form1.Log($"{username}: Disconnected from the game, reconnecting...");
                    await API.Reconnect();
                }
                else if (messageData.Equals("WaitingForStats"))
                {
                    CloseGame();
                }
            }

            //leaverbuster penalty
            else if (isLeader && uri.Equals("/lol-matchmaking/v1/search") && eventType.Equals("Create") && !dodgeNotified)
            {
                    var lowPriorityData = messageData["lowPriorityData"];
                    var penaltyTimeRemaining = lowPriorityData["penaltyTimeRemaining"];
                    var errors = messageData["errors"];

                    foreach (var error in errors)
                    {
                        var errorType = error["errorType"];
                        var penaltyTime = (int)error["penaltyTimeRemaining"] + 5;
                        var penaltyTimeMS = penaltyTime * 1000;

                        if (errorType == "QUEUE_DODGER")
                        {
                            dodgeNotified = true;
                            Form1.Log($"{username}: We have a {penaltyTime} seconds queue dodge penalty, we are waiting it out...");
                            await Task.Delay(penaltyTimeMS);
                            await API.SearchGame();
                        }
                    }

                   if (penaltyTimeRemaining > 0)
                    {
                        Form1.Log($"{username}: We have a {penaltyTimeRemaining} seconds leaverbuster penalty...");
                    }
            }

            else if (uri.Equals("/lol-matchmaking/v1/search") && eventType.Equals("Update"))
            {
                if (messageData.ContainsKey("dodgeData"))
                {
                    var dodgeData = messageData["dodgeData"];
                    var state = dodgeData["state"];
                    if (state.Equals("StrangerDodged"))
                    {
                        if (isLeader)
                            Form1.Log($"{username}: Someone dodged in champ select, we are waiting for a new match ...");
                    }
                }
            }

            else if (uri.Equals("/lol-lobby-team-builder/v1/matchmaking") && eventType.Equals("Update"))
            {
                if (messageData.ContainsKey("dodgeData"))
                {
                    var dodgeData = messageData["dodgeData"];
                    var state = dodgeData["state"];
                    if (state.Equals("PartyDodged"))
                    {
                        if (isLeader)
                            Form1.Log($"{username}: Someone in our party dodged this match, queuing again...");
                        await API.SearchGame();
                    }
                }
            }

            else if (uri.Equals("/lol-matchmaking/v1/ready-check") && eventType.Equals("Update"))
                {
                    var state = messageData["state"];

                    if (state.Equals("PartyNotReady"))
                    {
                        if (isLeader)
                            Form1.Log($"{username}: Match declined by a member of this party..");
                    }
                    else if (state.Equals("StrangerNotReady"))
                    {
                        if (isLeader)
                            Form1.Log($"{username}: A stranger declined a popped match, waiting for another...");
                    }
                }

                else if (uri.Equals("/lol-champ-select/v1/session") && eventType.Equals("Create"))
            {
                await API.ChooseChamp();
            }

                else if (uri.Equals("/lol-lobby/v2/comms/members") && eventType.Equals("Update"))
                {
                    var players = messageData["players"];
                    var setting = Settings.Default.PartySizeSlider;

                    if (players.Count == Settings.Default.PartySizeSlider)
                    {
                        if (isLeader)
                        {
                        await API.SearchGame();
                        }
                    }
                }

                else if (uri.Equals("/lol-honor-v2/v1/ballot") && eventType.Equals("Create"))
                {
                    if (isLeader)
                        Form1.Log($"{username}: Honor screen is here, time to honor someone my deer");

                    long gameId = messageData["gameId"];
                    await API.HonorPlayer(gameId);

                    if (isLeader)
                    {
                        Form1.Log($"{username}: We have skipped the honor screen, queuing again...");
                    }
                    await API.PlayAgain();
                    await API.DismissStats();
                    await API.UpdateData();
            }

                else if (isLeader && uri.Equals("/lol-lobby/v2/party/eog-status") && eventType.Equals("Update"))
                {
                var players = messageData["readyPlayers"];
                var count = players.Count;
                Console.WriteLine(count);
                if (count >= 1)
                {
                    Form1.Log($"{username}: EoG, playing again...");
                    await Task.Delay(15000);
                    await API.SearchGame();
                }
                }
            else if (Settings.Default.PartySizeSlider == 1 && uri.Equals("/lol-lobby/v2/party-active") && eventType.Equals("Update"))
            {
                await API.SearchGame();
            }

        }

        public void Unsubscribe()
        {
            if (socket != null)
            {
                socket.OnMessage -= OnMessage;
                if(socket.IsAlive)
                    socket.Close();
                socket.OnClose -= OnClose;
                socket = null;
            }
        }

        public void OnClose(object o, CloseEventArgs e)
        {
            Form1.Log("LCU has been closed, terminating socket connection...");
            Unsubscribe();
            Form1.CheckStopped();
        }

        public void CloseGame()
        {
            try
            {

                Process p = MyProcesses.GetChildLeagueProcess(LeagueClient.Id);
                if (p != null)
                {
                    if(!p.WaitForExit(5000))
                        p.Kill();
                }
            }
            catch (Exception e)
            {
                Form1.Log(e.Message);
            }
        }

    }
}
