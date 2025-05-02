using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
//using RapidFire_Master.Files;
using RapidFire_MasterServer.Files.Compartilhado;
using Newtonsoft.Json.Linq;

public enum OperationalSystemTarget
{
    Linux,
    Windows,
};
namespace RapidFire_MasterServer
{
    public class UserInfo
    {
        public string userID;
        public string account;
        public string password;
        public int gameMode;
        public bool inQueue;
        public UserTeamInfo teamInfo;


        public string conPort = "0";
        // These fields should not be public
        public UserConnection userConnection;

        public UserInfo()
        {
            userID = "0";
            account = "NULL";
            password = "NULL";
            gameMode = 0;
            inQueue = false;
            conPort = "0";
            teamInfo = UserTeamInfo.spectator;
        }
    }

    public class QueueInfo
    {
        public int gameMode;
        public Dictionary<string, UserInfo> usersQueue;

        public QueueInfo(int _gameMode)//, int _numPlayers)
        {

            // The dictionary is already initialized in the field definition
            usersQueue = new Dictionary<string, UserInfo>();
            gameMode = _gameMode;
        }
    }

    public class ServerInfo
    {
        public String MatchID = "0";
        public string matchPID = "0";
        public string IP = "0.0.0.0";
        public int Port = 0;
        public int GameMode = 0;
        public List<UserInfo> users = new List<UserInfo>();
        public List<QueueUserRegister> listToRegister = new List<QueueUserRegister>();

        public ServerInfo()
        {
            // The list is already initialized in the field definition
            users = new List<UserInfo>();
            listToRegister = new List<QueueUserRegister>();
            matchPID = "0";
            IP = "0.0.0.0";
            Port = 0;
            MatchID = "0";
            GameMode = 0;

        }

        public void Clear()
        {
            users.Clear();
            listToRegister.Clear();
            matchPID = "0";
            Port = 0;
            MatchID = "0";
            GameMode = 0;
        }
    }

    public class RapidFire_MatchMaking
    {
        private string fullPath = "\\";

        public int createMathUserQueue_DM = 10;
        public int createMathUserQueue_5v5 = 10;
        public int createMathUserQueue_Defuse = 10;

        public int port_Start = 7777;
        public int port_End = 9000;

        private int currentPort = 0;
        public int createMatchBotNum = 0;

        public Dictionary<string, UserInfo> clients;
        public Dictionary<int, QueueInfo> queueList;
        public Dictionary<string, ServerInfo> currentServer;

        public List<UserInfo> nextMatchUsers;

        public RapidFire_MatchMaking()
        {
            // The dictionaries and list should be initialized
            clients = new Dictionary<string, UserInfo>();
            queueList = new Dictionary<int, QueueInfo>();
            currentServer = new Dictionary<string, ServerInfo>();
            nextMatchUsers = new List<UserInfo>();

            // queueList.Add(0, new QueueInfo(0, createMathUserQueue_DM)); //DM
            //   queueList.Add(10, new QueueInfo(10, createMathUserQueue_5v5)); //5v5
            // queueList.Add(100, new QueueInfo(100, createMathUserQueue_Defuse)); //Defuse
        }
        public int GetStartPlayerNum(int gameMode)
        {
            int count = 10;
            if (gameMode == 10)
                count = Program.MatchMaking.createMathUserQueue_5v5;
            else if (gameMode == 100)
                count = Program.MatchMaking.createMathUserQueue_Defuse;
            else
                count = Program.MatchMaking.createMathUserQueue_DM;

            return (count <= 0 ? 1 : count);
        }

        public void StartMatchMaking()
        {
            string[] removePatch = Process.GetCurrentProcess().MainModule.FileName.Split('\\');
            string[] strr = new string[removePatch.Length - 1];

            for (int i = 0; i < strr.Length; i++)
                strr[i] = removePatch[i];

            string full = "";

            for (int i = 0; i < strr.Length; i++)
            {
                full += strr[i] + @"\";
            }
            fullPath = full;
            Program.Logger.WriteLine("Rapid Fire MatchMaking Started.");
        }

        public void Initialize()
        {
            var task = Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(Program.TIME_MIN);
                    //Load configs at start
                    Program.Database.LoadServersConfigXML();
                }
            });

            Program.Database.LoadServersConfigXML();

            currentPort = port_Start;

            queueList.Add(0, new QueueInfo(0)); //DM
            queueList.Add(10, new QueueInfo(10)); //5v5
            queueList.Add(100, new QueueInfo(100)); //Defuse
        }

        public void ProcessFrame()
        {
            if (queueList.Count <= 0)
                return;

            if (clients.Count <= 0)
                return;

            if (currentPort < port_Start)
                currentPort = port_Start;

            if (currentPort >= port_End)
                currentPort = port_Start;

            for (int i = 0; i < queueList.Count; i++)
            {
                int key = queueList.ElementAt(i).Key;
                QueueInfo currentRoomInfo = queueList[key];

                if (currentRoomInfo.usersQueue.Count <= 0)
                    continue;

                if (currentRoomInfo.usersQueue.Count >= this.GetStartPlayerNum(currentRoomInfo.gameMode))
                {
                    var task = Task.Run(() =>
                    {
                        nextMatchUsers.Clear();

                        foreach (var user in currentRoomInfo.usersQueue.Take(this.GetStartPlayerNum(currentRoomInfo.gameMode)))
                        {
                            user.Value.conPort = currentPort.ToString();
                            nextMatchUsers.Add(user.Value);
                        }
                        // Remove users from queue
                        foreach (var user in nextMatchUsers)
                            currentRoomInfo.usersQueue.Remove(user.userID);

                        if (CreateMatchProcess(currentRoomInfo.gameMode, currentPort.ToString()))
                        {
                            currentPort += 1;
                        }
                    });
                }
            }
        }

        /*
        ProcessStartInfo CreateProcessInfo(string matchID, int currentPort, int gameMode)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = false;

            int playerNum = this.GetStartPlayerNum(gameMode);
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))//if (Program.operationalSystemTarget == OperationalSystemTarget.Windows)
            {
                startInfo.FileName = @"F:\Unity 2021.3.0f1\RapidFire_TPD\Build\Game\RapidFire.exe";
                startInfo.Arguments = $"-server -console -matchID {matchID} -port {currentPort} -matchMode {gameMode} -minPerTeam {playerNum} -maxPerTeam {playerNum} -bot {createMatchBotNum}";
            }
            else// if (Program.operationalSystemTarget == OperationalSystemTarget.Linux)
            {
                startInfo.FileName = "/home/opc/rapidfire/server/server.x86_64";
                startInfo.Arguments = $"-batchmode -nographics -server -matchID {matchID} -port {currentPort} -matchMode {gameMode} -minPerTeam {playerNum} -maxPerTeam {playerNum} -bot {createMatchBotNum}";
            }
            startInfo.CreateNoWindow = false;
            return startInfo;
        }
        */

        public static Process InitProcess(string commandline)
        {
            try
            {
                OperationalSystemTarget systemTarget = OperationalSystemTarget.Linux;

                ProcessStartInfo startInfo = new ProcessStartInfo();
                if (systemTarget == OperationalSystemTarget.Windows)
                    startInfo.FileName = @"C:\Unity 2021.3.0f1\RapidFire_FPS_Mirror\Build\Game\RapidFire.exe";
                else if (systemTarget == OperationalSystemTarget.Linux)
                    startInfo.FileName = "/root/rapidfire/server/server.x86_64";
                startInfo.UseShellExecute = false;
                startInfo.Arguments = commandline;

                return Process.Start(startInfo);
            }
            catch
            {
                return null;
            }
        }

        public bool CreateMatchProcess(int gameMode, string port)
        {
            bool conclusion = true;
            Task refresh = Task.Run(() =>
            {
                try
                {
                    Program.Logger.WriteLine($"Trying to create a room at Port:{port}, gamemode:{gameMode}");


                    var timestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                    String matchID = timestamp.ToString();

                    int playerNum = this.GetStartPlayerNum(gameMode);
                    string commandLine = $"-batchmode -nographics -server -matchID {matchID} -port {currentPort} -matchMode {gameMode} -minPerTeam {playerNum} -maxPerTeam {playerNum} -bot {createMatchBotNum}";

                    Process process = InitProcess(commandLine);

                  //  if (process != null && !string.IsNullOrEmpty(process.ProcessName) && conclusion == true)
                    //    conclusion = false;

                    if (process != null )//&& !string.IsNullOrEmpty(process.ProcessName) && conclusion == true)
                    {
                        ServerInfo info = new ServerInfo();
                        info.matchPID = process.Id.ToString();
                        info.GameMode = gameMode;
                        info.MatchID = matchID;
                        info.IP = "0.0.0.0";
                        info.Port = int.Parse(currentPort.ToString());
                        info.users = nextMatchUsers;
                        currentServer.Add(matchID.ToString(), info);
                        Program.Logger.WriteLine($"Rapid Fire Server ({info.MatchID}) started at internal port: {info.Port} matchPID: {info.matchPID} GameMode: {info.GameMode}");

                        conclusion = true;
                    }
                    else
                    {
                        Program.Logger.WriteLine($"Remote Server: Failed to start room at port {port}");
                        conclusion = false;
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.WriteException($"Failed to start room: " + ex.Message);
                    conclusion = false;
                }
            }
            );
            return conclusion;
        }

        public void SendSinglePlayerToMatch(UserInfo userInfo, string matchID)
        {
            if (Program.Database.UpdatePlayerMatch(userInfo.account, matchID) == RESULT_OPTIONS.PLAYER_MATCH_REGISTER_OK)
                Program.Networking.SendPacket(userInfo.userConnection, Receive_Packets.CLIENT_RECEIVE_PACKET.PACKET_CLIENT_ENTER_MATCH, Program.MatchMaking.currentServer[matchID].Port.ToString(), Program.MatchMaking.currentServer[matchID].IP.ToString(), JsonConvert.SerializeObject(Program.MatchMaking.currentServer[matchID].listToRegister)/*, profileString*/);
            else
                Program.Logger.WriteLine($"{userInfo.account}: FAILED TO REGISTER TO NEW MATCH >> {matchID}");
        }

        /*
        public bool KillMatch(string matchID)
        {
            try
            {
                //Kill Process!!!
                using (Process chosen = Process.GetProcessById(currentServer[matchID].PID))
                {
                    chosen.Kill();
                    chosen.WaitForExit();
                    return true;
                }
            }
            catch (Exception)
            {
                Program.Logger.WriteException($"Failed do kill server {matchID}, it's live??");
            }
            return false;
        }

        void OnProcessExited(ServerInfo info)
        {
            Program.Logger.WriteLine($"Rapid Fire Server ({info.MatchID}) closed with port: {info.Port} PID: {info.PID} GameMode: {info.GameMode}");
        }
        */
    }
}