using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoAccepter.Properties;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RandomGen;
using Flurl;
using Flurl.Http;
using WebSocketSharp;

namespace AutoAccepter
{
    public class ClientFunctions
    {
        private readonly string _username;
        public string clientPassword = "";
        public string clientPort = "";
        private string _baseURL;
        private string _token;
        public Account account;
        public string QueueID;

        public ClientFunctions()
        {

        }

        public ClientFunctions(string username, Account account)
        {
            _username = username;
            this.account = account;

            if (Settings.Default.GameMode == "5v5 Blind Pick")
                QueueID = "430";
            else if (Settings.Default.GameMode == "5v5 ARAM")
                QueueID = "450";
            else if (Settings.Default.GameMode == "3v3 Blind Pick")
                QueueID = "460";
            else if (Settings.Default.GameMode == "SR Co-op vs. AI Intro")
                QueueID = "830";
            else if (Settings.Default.GameMode == "SR Co-op vs. AI Beginner")
                QueueID = "840";
            else if (Settings.Default.GameMode == "SR Co-op vs. AI Intermediate")
                QueueID = "850";
            else if (Settings.Default.GameMode == "TT Co-op vs. AI Intro")
                QueueID = "810";
            else if (Settings.Default.GameMode == "TT Co-op vs. AI Intermediate")
                QueueID = "800";
        }


        public void StartClient()
        {
            try
            {
                Directory.SetCurrentDirectory(@"C:\Riot Games\League of Legends");
                var myProcess = new Process();
                myProcess.StartInfo.FileName = "LeagueClient.exe";
                myProcess.StartInfo.Arguments = "--headless --disable-gpu --disable-patching --disable-self-update";
                myProcess.StartInfo.Verb = "runas";
                myProcess.Start();
                Form1.Log("Client successfully started..");
            }
            catch (Win32Exception)
            {
                Form1.Log("Unable to start League of Legends. Please make sure your path is correct...");
            }
        }

        public void SetConnection()
        {
            var line = "";
            var aggregate = "";
            var toEncode = "";
            var pid = "";
            while (!File.Exists(Config.LockFile)) Thread.Sleep(1000);

            var driveLetter = Path.GetPathRoot(Config.LeagueFolder).Replace("\\", "/");
            var path = driveLetter + "Windows/PrintDrivers";
            var random = RandomString(14);
            FileSystem.MoveFile(Config.LockFile, path + "/Lockfiles/" + random,
                true);

            using (var stream = File.Open(path + "/Lockfiles/" + random, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite))
            {
                using (var streamReader = new StreamReader(stream, Encoding.UTF8, true, 4096))
                {
                    while (!streamReader.EndOfStream) line = streamReader.ReadLine();
                }
            }

            pid = line.Split(':', ':')[1];
            clientPort = line.Split(':', ':')[2];
            clientPassword = line.Split(':', ':')[3];

            aggregate = "https://riot:" + clientPassword + "@127.0.0.1:" + clientPort;
            _baseURL = aggregate;
            toEncode = "riot:" + clientPassword;
            _token = EncodeTo64(toEncode);
        }


        public string EncodeTo64(string toEncode)
        {
            var e = Encoding.GetEncoding("iso-8859-1");
            var toEncodeAsBytes = e.GetBytes(toEncode);
            var returnValue = Convert.ToBase64String(toEncodeAsBytes);
            return returnValue;
        }

        public async Task<bool> Login(string Username, string Password)
        {
            var url = new Url(_baseURL + "/lol-login/v1/session");
            var request = await url.WithHeader("Authorization", "Basic " + _token).PostJsonAsync(new { username = Username, password = Password }).ReceiveJson();
            var state = request.state;

            while (state.ToString() == "IN_PROGRESS")
            {
                request = await url.WithHeader("Authorization", "Basic " + _token).GetAsync().ReceiveJson();
                state = request.state;
                Thread.Sleep(1000);
            }

            if (state.ToString() == "SUCCEEDED")
            {
                Form1.Log($"{_username}: Auto Login: Successfully signed in, loading client...");
                await Task.Delay(15000);
                var summonerID = request.summonerId;

                if (summonerID == null)
                {
                    if (Properties.Settings.Default.UsernameCreatorToggle)
                    {
                        Form1.Log("New player detected, creating an username for you..");
                        await CreateAccount();
                    }
                    else
                    {
                        Form1.Log("New player detected. Auto username creation is not enabled...");
                        return false;
                    }
                }

                return true;
            }
            else
            {
                var errorString = request.error.ToString();
                var JSONObj2 = JsonConvert.DeserializeObject<Dictionary<string, object>>(errorString);
                var messageID = JSONObj2["messageId"].ToString();
                if (messageID == "ACCOUNT_BANNED")
                    Form1.Log($"{_username}: Auto Login: Account is banned...");
                else if (messageID == "INVALID_CREDENTIALS")
                    Form1.Log($"{_username}: Auto Login: Invalid username or password entered...");
                return false;
            }
        }

        public async Task<bool> IsInGame()
        {
            var url = new Url(_baseURL + "/lol-login/v1/login-in-game-creds");
            var request = await url.WithHeader("Authorization", "Basic " + _token).GetAsync();

            if (request.IsSuccessStatusCode)
                return true;
            else
                return false;
        }

        public  String RandomString(int length)
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public async Task CreateAccount()
        {
            var newUsername = "";
            bool available = false;

            while (available == false)
            {
                var word = Gen.Random.Text.Words();
                var number = Gen.Random.Numbers.Integers();
                newUsername = word() + number();

                var url = new Url(_baseURL + "/lol-summoner/v1/check-name-availability/" + newUsername);
                var request = await url.WithHeader("Authorization", "Basic " + _token).GetStringAsync();

                if (request == "true")
                {
                    available = true;
                    Form1.Log($"{newUsername} is an available _username");
                }
            }
            var url2 = new Url(_baseURL + "/lol-summoner/v1/summoners");
            var request2 = await url2.WithHeader("Authorization", "Basic " + _token)
                .PostJsonAsync(new { name = newUsername });

            await Task.Delay(15000);

            Form1.Log($"Player successfully created, your new summoner name is {newUsername}");
        }

        public async Task CheckExistingParty()
        {

            var url = new Url(_baseURL + "/lol-matchmaking/v1/search");
            var request = await url.WithHeader("Authorization", "Basic " + _token).GetAsync();

            if (request.StatusCode == HttpStatusCode.OK)
                request = await url.WithHeader("Authorization", "Basic " + _token).DeleteAsync();

            url = new Url(_baseURL + "/lol-lobby/v2/lobby");
            request = await url.WithHeader("Authorization", "Basic " + _token).GetAsync();

            if (request.StatusCode == HttpStatusCode.OK)
                request = await url.WithHeader("Authorization", "Basic " + _token).DeleteAsync();
        }

        public async Task UpdateData()
        {
            int level = 0;
            int percentToNextLevel = 0;
            var summonerName = "";

            var url = new Url(_baseURL + "/lol-summoner/v1/current-summoner/xpInfo");
            var request = await url.WithHeader("Authorization", "Basic " + _token).PostAsync(null).ReceiveJson();

            if (((IDictionary<string, object>)request).ContainsKey("summonerLevel"))
                level = Convert.ToInt32(request.summonerLevel);
            else
                level = 1;

            if (((IDictionary<string, object>)request).ContainsKey("percentCompleteForNextLevel"))
                percentToNextLevel = Convert.ToInt32(request.percentCompleteForNextLevel);
            else
                percentToNextLevel = 0;

            if (((IDictionary<string, object>)request).ContainsKey("displayName"))
                summonerName = Convert.ToString(request.displayName);
            else
                summonerName = "N/A";

            Form1.displayData(account.username, level, percentToNextLevel, summonerName);
        }

        public async Task CreateParty()
        {
            var url = new Url(_baseURL + "/lol-lobby/v2/lobby");
            var request = await url.WithHeader("Authorization", "Basic " + _token)
                .PostJsonAsync(new { queueId = QueueID });

            var createLobby = await url.WithHeader("Authorization", "Basic " + _token)
            .GetJsonAsync();

            account.PartyID = createLobby.partyId;
        }


        public async Task JoinParty(string party)
        {
            var url = new Url(_baseURL + "/lol-lobby/v2/party/" + party + "/join");
            var request = await url.WithHeader("Authorization", "Basic " + _token)
                .PostAsync(null);
        }

        public async Task CallPosition()
        {
            var url = new Url(_baseURL + "/lol-chat/v1/conversations");
            dynamic request = await url.WithHeader("Authorization", "Basic " + _token)
                .GetJsonListAsync();

            var id = request[0].id;

            url = new Url(_baseURL + "/lol-summoner/v1/current-summoner");
            request = await url.WithHeader("Authorization", "Basic " + _token)
                .GetJsonAsync();

            var summonerId = Convert.ToInt32(request.summonerId);


            url = new Url(_baseURL + "/lol-chat/v1/conversations/" + id + "/messages");
            var request2 = await url.WithHeader("Authorization", "Basic " + _token)
                .PostJsonAsync(new
                { type = "chat", fromId = summonerId, isHistorical = false, timestamp = "", body = Settings.Default.Position });
        }

        public async Task SearchGame()        
        {
            var url = new Url(_baseURL + "/lol-lobby/v2/lobby/matchmaking/search");
            var request = await url.WithHeader("Authorization", "Basic " + _token)
                .PostAsync(null);
        }

        public async Task AcceptMatch()
        {
            var url = new Url(_baseURL + "/lol-matchmaking/v1/ready-check/accept");
            var request = await url.WithHeader("Authorization", "Basic " + _token)
                .PostAsync(null);
        }

        public async Task ChooseChamp()
        {
                int lockedin = 0;
                int champId = 0;
                string champ = "";

                var url = new Url(_baseURL + "/lol-champ-select/v1/session");
                var request = await url.WithHeader("Authorization", "Basic " + _token)
                    .GetJsonAsync();

                var cellId = request.localPlayerCellId;

            champId = Enums.GetChampionByName(Settings.Default.Champion);
            champ = Convert.ToString(champId);

            while (lockedin == 0)
                {
                    url = new Url(_baseURL + "/lol-champ-select/v1/pickable-champions");
                    request = await url.WithHeader("Authorization", "Basic " + _token)
                        .GetJsonAsync();

                    var pickableChamps = request.championIds;

                if (Settings.Default.RandomChampionToggle)
                    {
                        Form1.Log($"Choosing a random champion since we do not own {Settings.Default.Champion}...");
                        var rnd = new Random();
                        int r = rnd.Next(request.championIds.Count);
                        champId = Convert.ToInt32(request.championIds[r]);
                        champ = champId.ToString();
                    }

                    else
                {

                    bool found = false;
                    foreach (var items in pickableChamps)
                    {
                        if (items == champId)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        Form1.Log($"We cannot lock in {Settings.Default.Champion}, choosing a random champion...");
                        var rnd = new Random();
                        int r = rnd.Next(request.championIds.Count);
                        champId = Convert.ToInt32(request.championIds[r]);
                        champ = champId.ToString();
                    }
                }

                    url = new Url(_baseURL + "/lol-champ-select/v1/session/actions/" + cellId);
                    request = await url.WithHeader("Authorization", "Basic " + _token)
                        .PatchJsonAsync(new {completed = true, championId = champ});

                    url = new Url(_baseURL + "/lol-champ-select/v1/current-champion");
                    request = await url.WithHeader("Authorization", "Basic " + _token)
                        .GetStringAsync();

                    try
                    {
                        lockedin = int.Parse(request);
                    }
                    catch (Exception e)
                    {
                        Form1.Log("You are the only exception...");
                    }

                    await Task.Delay(1000);
                }
                 Form1.Log($"{_username}: Champion locked in: " + Enums.GetChampionById(lockedin));
        }

        public async Task Reconnect()
        {
            var url = new Url(_baseURL + "/lol-gameflow/v1/reconnect");
            var request = await url.WithHeader("Authorization", "Basic " + _token)
                .PostAsync(null);
        }

        public async Task HonorPlayer(long gameID)
        {
            var url = new Url(_baseURL + "/lol-honor-v2/v1/honor-player");
            var request = await url.WithHeader("Authorization", "Basic " + _token)
                .PostJsonAsync(new { gameId = gameID, honorCategory = "OPT_OUT", summonerId = 0 });
        }

        public async Task DismissStats()
        {
            var url = new Url(_baseURL + "/lol-end-of-game/v1/state/dismiss-stats");
            var request = await url.WithHeader("Authorization", "Basic " + _token)
                .PostAsync(null);
        }

        public async Task PlayAgain()
        {
            var url = new Url(_baseURL + "/lol-lobby/v2/play-again");
            var request = await url.WithHeader("Authorization", "Basic " + _token)
                .PostAsync(null);
        }

        public async Task ClearFriends()
        {
            try
            {
                RunningSet();

                var url = new Url(_baseURL + "/lol-chat/v1/friends");
                var request = await url.WithHeader("Authorization", "Basic " + _token)
                    .GetJsonListAsync();

                foreach (var expando in request)
                {
                    var data = expando.id;
                    var name = expando.name;

                    url = new Url(_baseURL + "/lol-chat/v1/friends/" + data.ToString());
                    var request2 = await url.WithHeader("Authorization", "Basic " + _token)
                        .DeleteAsync();
                    await Task.Delay(1000);
                }

                Form1.Log("Friends list has been wiped...");
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "IndexOutOfRangeException")
                    Form1.Log("LeagueClient is not running. Please start the client before trying to perform an operation.");
            }
        }

        public async Task ChangeIcon(int icon)
        {
            try
            {

            RunningSet();

                var url = new Url(_baseURL + "/lol-summoner/v1/current-summoner/icon");
                var request = await url.WithHeader("Authorization", "Basic " + _token)
                    .PutJsonAsync(new {profileIconId = icon});
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "IndexOutOfRangeException")
                    Form1.Log("LeagueClient is not running. Please start the client before trying to perform an operation.");
            }
        }



        public async Task OpenLoot()
        {
            try
            {
                RunningSet();

                var capsules = new Dictionary<string, int>();
                var keys = new Dictionary<string, int>();
                var champions = new Dictionary<string, int>();

                var url = new Url(_baseURL + "/lol-loot/v1/player-loot-map");
                var request = await url.WithHeader("Authorization", "Basic " + _token)
                    .GetJsonAsync();

                foreach (var item in request)
                {
                    var lootName = item.Key;
                    var count = Convert.ToInt32(item.Value.count);
                    if (lootName.Contains("CHEST"))
                        capsules.Add(lootName, count);
                    else if (lootName.Contains("MATERIAL"))
                        keys.Add(lootName, count);
                    else if (lootName.Contains("CHAMPION"))
                    {
                        champions.Add(lootName, count);
                    }
                }


                //opens all capsules
                if (capsules.Any())
                {
                    foreach (var capsule in capsules)
                    {
                        var chestName = capsule.Key;
                        var totalChests = capsule.Value;

                        string capsuleBody = "[\"" + capsule.Key + "\"]";

                        url = new Url(_baseURL + "/lol-loot/v1/recipes/" + chestName + "_OPEN/craft?repeat=" +
                                      totalChests);
                        var request2 = await url.WithHeader("Authorization", "Basic " + _token)
                            .WithHeader("Content-Length", capsuleBody.Length)
                            .WithHeader("Content-Type", "application/json")
                            .WithHeader("Accept", "application/json")
                            .PostStringAsync(capsuleBody);
                    }
                }

                //get all loot again after opening all the capsules

                url = new Url(_baseURL + "/lol-loot/v1/player-loot-map");
                request = await url.WithHeader("Authorization", "Basic " + _token)
                    .GetJsonAsync();

                capsules.Clear();
                keys.Clear();
                champions.Clear();

                foreach (var item in request)
                {
                    var lootName = item.Key;
                    var count = Convert.ToInt32(item.Value.count);
                    if (lootName.Contains("MATERIAL"))
                        keys.Add(lootName, count);
                    else if (lootName.Contains("CHAMPION"))
                    {
                        champions.Add(lootName, count);
                    }
                }


                // crafts each group of 3 key fragments into keys that can be used on chests
                if (keys.Any())
                {
                    foreach (var item in keys)
                    {
                        if (item.Key == ("MATERIAL_key_fragment"))
                        {
                            var totalKeys = (int) (item.Value / 3);
                            string keyFragmentBody = "[\"" + item.Key + "\"]";
                            url = new Url(_baseURL + "/lol-loot/v1/recipes/MATERIAL_key_fragment_forge/craft?repeat=" +
                                          totalKeys);
                            var request3 = await url.WithHeader("Authorization", "Basic " + _token)
                                .WithHeader("Content-Length", keyFragmentBody.Length)
                                .WithHeader("Content-Type", "application/json")
                                .WithHeader("Accept", "application/json")
                                .PostStringAsync(keyFragmentBody);
                            break;
                        }
                    }

                }

                //sells all champion shards
                if (champions.Any())
                {
                    foreach (var champ in champions)
                    {
                        string champBody = "[\"" + champ.Key + "\"]";
                        if (champ.Key.Contains("RENTAL"))
                            url = new Url(_baseURL + "/lol-loot/v1/recipes/CHAMPION_RENTAL_disenchant/craft?repeat=" +
                                          champ.Value);
                        else
                            url = new Url(_baseURL + "/lol-loot/v1/recipes/CHAMPION_disenchant/craft?repeat=" +
                                          champ.Value);

                        var request2 = await url.WithHeader("Authorization", "Basic " + _token)
                            .WithHeader("Content-Length", champBody.Length)
                            .WithHeader("Content-Type", "application/json")
                            .WithHeader("Accept", "application/json")
                            .PostStringAsync(champBody);
                    }
                }
            }
            catch (Exception e)
            {
                if (e.GetType().Name == "IndexOutOfRangeException")
                    Form1.Log("LeagueClient is not running. Please start the client before trying to perform an operation.");
            }
        }

        public async Task<string> SearchSummoner(string id)
        {
            try
            {
                RunningSet();

                var url = new Url(_baseURL + "/lol-summoner/v1/summoners/" + id);
                var request = await url.WithHeader("Authorization", "Basic " + _token)
                    .GetJsonAsync();
                return request.displayName;
            }
            catch (Exception e)
            {
                if(e.GetType().Name == "IndexOutOfRangeException")
                    Form1.Log("LeagueClient is not running. Please start the client before trying to perform an operation.");
                return "Error";

            }
        }

        public void RunningSet()
        {
                Process p = Process.GetProcessesByName("LeagueClientUX")[0];
                String[] arguments = CommandLineUtilities.getCommandLinesParsed(p); //2 is token, 8 is port
                clientPassword = arguments[2];
                clientPort = arguments[8];
                clientPassword = clientPassword.Substring(clientPassword.LastIndexOf('=') + 1).Replace("\"", "");
                clientPort = clientPort.Substring(clientPort.LastIndexOf('=') + 1).Replace("\"", "");
                _baseURL = "https://riot:" + clientPassword + "@127.0.0.1:" + clientPort;
                _token = EncodeTo64("riot:" + clientPassword);
        }
    }
}

