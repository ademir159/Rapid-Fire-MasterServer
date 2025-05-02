using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using System.Text;
//sing RapidFire_Master.Files;
using RapidFire_MasterServer.Files.Compartilhado;

using System.Timers;
using Newtonsoft.Json;


namespace RapidFire_MasterServer
{
    public class RapidFire_Networking
    {
        const int PORT_NUM = 23466;

        private TcpListener tcpListener;
        private Thread tcpListenerThread;

        public void Initialize()
        {

        }

        public RapidFire_Networking()
        {

        }

        public void StartConnection()
        {
            tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequests));
            tcpListenerThread.IsBackground = true;
            tcpListenerThread.Start();

            Program.Logger.WriteLine("Rapid Fire MasterServer Started.");
        }
        private void ListenForIncommingRequests()
        {
            try
            {
                tcpListener = new TcpListener(System.Net.IPAddress.Any, PORT_NUM);

                tcpListener.Start();
                Program.Logger.WriteLine("Authenticator is listening...\n\n\n");

                while (true)
                {
                    UserConnection user = new UserConnection(tcpListener.AcceptTcpClient());
                    user.handlePacket += OnHandlePacket;
                    user.handleDisconnect += DisconnectUser;
                    user.BeginRead();
                }
            }
            catch (SocketException socketException)
            {
                Program.Logger.WriteException("SocketException " + socketException.ToString());
                throw socketException;
            }
        }

        private void ConnectUser(string account, string password, UserConnection sender)
        {
            string userID = "0";
            string profileString = "NULL";


            RESULT_OPTIONS resultOptions = Program.Database.LoadProfile(ref userID, account, password, ref profileString);

            UserInfo userInfo = null;
            if (Program.MatchMaking.clients.TryGetValue(userID, out userInfo) || resultOptions != RESULT_OPTIONS.PROFILE_LOAD_OK)
            {
                if (userInfo != null)
                    userInfo.userConnection = sender;

                Program.Logger.WriteLine("SERVER >> REFUSED CONNECTION >> " + userID);
                SendPacket(sender, Receive_Packets.CLIENT_RECEIVE_PACKET.PACKET_CLIENT_REFUSE, ((int)resultOptions).ToString(), "NULL");
                return;
            }
            else
            {
                sender.userID = userID;
                Program.Logger.WriteLine("SERVER >> " + userID + " CONNECTED");

                userInfo = new UserInfo();
                userInfo.inQueue = false;
                userInfo.gameMode = 0;
                userInfo.userID = userID;
                userInfo.account = account;
                userInfo.password = password;
                userInfo.userConnection = sender;
                Program.MatchMaking.clients.Add(userID, userInfo);

                SendPacket(sender, Receive_Packets.CLIENT_RECEIVE_PACKET.PACKET_CLIENT_JOIN, ((int)resultOptions).ToString(), profileString);

                userInfo.userConnection.CreatePingMesage();//
                                                           // userInfo.userConnection.BeginKeepAlive();
            }

            //Rejoin after quit.
            string matchID = "0";
            if (Program.Database.LoadPlayerMatch(account, ref matchID) == RESULT_OPTIONS.PLAYER_MATCH_LOAD_OK)
            {
                ServerInfo serverInfo = null;
                if (Program.MatchMaking.currentServer.TryGetValue(matchID, out serverInfo))
                    SendPacket(userInfo.userConnection, Receive_Packets.CLIENT_RECEIVE_PACKET.PACKET_CLIENT_ENTER_MATCH, serverInfo.Port.ToString());
            }
        }

        public void DisconnectUser(UserConnection sender)
        {
            if (sender == null)
                return;

            try
            {
                if (sender.GetTcpClient() == null)
                    return;

                sender.GetTcpClient().Dispose();
                sender.GetTcpClient().Close();
                sender.SetTcpClient(null);

                Program.MatchMaking.clients.Remove(sender.userID);

                for (int i = 0; i < Program.MatchMaking.queueList.Count; i++)
                {
                    int key = Program.MatchMaking.queueList.ElementAt(i).Key;

                    UserInfo infoUser = null;
                    if (Program.MatchMaking.queueList[key].usersQueue.TryGetValue(sender.userID, out infoUser))
                    {
                        Program.MatchMaking.queueList[key].usersQueue.Remove(sender.userID);
                    }
                }
                if (int.Parse(sender.userID) > 0)
                    Program.Logger.WriteLine("SERVER >> " + sender.userID + " HAS DISCONNECTED.");
            }
            catch (System.Exception ex)
            {
                Program.Logger.WriteException(ex.ToString());
            }
        }

        //SEND MESSAGE 
        public void SendMessage(UserConnection target, string _serverMessage)
        {
            try
            {
                if (target.GetTcpClient() == null)
                    return;

                NetworkStream stream = target.GetTcpClient().GetStream();
                if (stream.CanWrite)
                {
                    byte[] serverMessageAsByteArray = Encoding.UTF8.GetBytes(_serverMessage);
                    stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);
                }
            }
            catch (SocketException socketException)
            {
                Program.Logger.WriteException("Socket exception: " + socketException.ToString());
                this.DisconnectUser(target);
                throw socketException;
            }
        }

        public void SendPacket(UserConnection target, Receive_Packets.CLIENT_RECEIVE_PACKET packet, params string[] args)
        {
            string argsData = "";
            for (int i = 0; i < args.Length; i++)
                argsData += args[i] + "#";

            string convertPacket = ((int)packet).ToString() + "^" + argsData;//^ separator

            string cryptoPacket = AesEncryption.Encrypt(convertPacket, "9f04d346ac0e135e1571041dd8e8e4fb");
            this.SendMessage(target, cryptoPacket);
            Program.Logger.WriteLine(target.userID + " >> SEND >> " + cryptoPacket);
        }

        //RECV MESSAGE 
        private async void OnHandlePacket(UserConnection sender, string criptoData)
        {
            try
            {
                string argsData = AesEncryption.Decrypt(criptoData, "9f04d346ac0e135e1571041dd8e8e4fb");

                //IGNORE LOSS DATA!!!!!
                if (argsData.Contains('^') == false)
                    return;

                //Program.Logger.WriteLine(sender.userID + " >> RECV >> " + argsData);

                string[] dataArray;
                dataArray = argsData.Split('^');//^ = separator

                if (IsNumber(dataArray[0]) == false)//check packet if is ID.
                    return;

                string[] args = dataArray[1].Split("#");//split args

                Receive_Packets.SERVER_RECEIVE_PACKET packet = (Receive_Packets.SERVER_RECEIVE_PACKET)int.Parse(dataArray[0]);

                switch (packet)
                {
                    case Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_CONNECT:
                        {
                            string account = args[0];
                            string password = args[1];
                            this.ConnectUser(account, password, sender);
                            break;
                        }
                    case Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_HOST_CONNECT:
                        {
                            string hostID = args[0];

                            ServerInfo serverInfo = null;
                            if (Program.MatchMaking.currentServer.TryGetValue(hostID, out serverInfo))
                                Program.Logger.WriteLine($"SERVER >> NEW MATCH CONNECTED >> {serverInfo.MatchID} ({serverInfo.matchPID})");
                            else
                                Program.Logger.WriteLine($"SERVER >> NEW MATCH CONNECTED >> {hostID}");
                            break;
                        }
                    case Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_DISCONNECT:
                        {
                            this.DisconnectUser(sender);
                            break;
                        }
                    case Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_KEEP_ALIVE:
                        {
                            //string userID = args[0];//Unused.
                            // SendPacket(sender, Receive_Packets.CLIENT_RECEIVE_PACKET.PACKET_CLIENT_KEEP_ALIVE, Program.MatchMaking.clients.Count.ToString());
                            sender.UpdateKeepAlive();
                            break;
                        }
                    case Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_USER_QUEUE_IN:
                        {
                            string userID = args[0];
                            int gameMode = int.Parse(args[1]);

                            UserInfo userInfo = null;
                            if (Program.MatchMaking.clients.TryGetValue(args[0], out userInfo))
                            {
                                //Rejoin after quit.
                                string matchID = "0";
                                if (Program.Database.LoadPlayerMatch(userInfo.account, ref matchID) == RESULT_OPTIONS.PLAYER_MATCH_LOAD_OK)
                                {
                                    ServerInfo serverInfo = null;
                                    if (Program.MatchMaking.currentServer.TryGetValue(matchID, out serverInfo))
                                    {

                                        Program.Logger.WriteLine($"{ userID } >> RELOGIN AT MATCH: {matchID} >> PORT: {serverInfo.Port}");
                                        Program.MatchMaking.SendSinglePlayerToMatch(userInfo, matchID);
                                        return;
                                    }
                                }
                                //
                                QueueInfo infoQueue = null;
                                if (Program.MatchMaking.queueList.TryGetValue(gameMode, out infoQueue))
                                {
                                    UserInfo infoUser = null;
                                    if (infoQueue.usersQueue.TryGetValue(userID, out infoUser))
                                    {
                                        sender.userID = userID;
                                        infoUser.inQueue = true;
                                        infoUser.gameMode = int.Parse(args[1]);
                                        infoUser.account = userInfo.account;
                                        infoUser.userConnection = sender;
                                    }
                                    else
                                    {
                                        sender.userID = userID;

                                        infoUser = new UserInfo();
                                        infoUser.inQueue = true;
                                        infoUser.gameMode = int.Parse(args[1]);
                                        infoUser.userID = args[0];
                                        infoUser.userConnection = sender;
                                        infoUser.account = userInfo.account;
                                        infoQueue.usersQueue.Add(args[0], infoUser);
                                    }
                                }
                                Program.MatchMaking.clients[args[0]].inQueue = true;
                                Program.Logger.WriteLine($"{ userID } >> FINDING MATCH: {gameMode} {Program.MatchMaking.createMathUserQueue_DM}");
                            }
                            else
                            {
                                Program.Logger.WriteLine($"{ userID } not founded.");
                            }
                            break;
                        }
                    case Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_USER_QUEUE_OUT:
                        {
                            string userID = args[0];
                            int gameMode = int.Parse(args[1]);
                            Program.Logger.WriteLine($"{ userID } FINDING MATCH HAS STOPPED: {gameMode}");

                            QueueInfo infoQueue = null;
                            if (Program.MatchMaking.queueList.TryGetValue(gameMode, out infoQueue))
                            {
                                UserInfo infoUser = null;
                                if (infoQueue.usersQueue.TryGetValue(userID, out infoUser))
                                    infoQueue.usersQueue.Remove(userID);
                            }
                            //REMOVE FROM QUEUE
                            UserInfo userInfo = null;
                            if (Program.MatchMaking.clients.TryGetValue(args[0], out userInfo))
                            {
                                Program.MatchMaking.clients[args[0]].inQueue = false;
                            }
                            break;
                        }

                    case Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_PROFILE_LOAD:
                        {
                            string version = args[0];
                            //this.ConnectUser(userID, sender);
                            break;
                        }
                    case Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_PROFILE_CHANGENAME:
                        {
                            string userID = args[0];
                            string account = args[1];
                            string password = args[2];
                            string newName = args[3];

                            RESULT_OPTIONS resultOptions = Program.Database.RegisterName(account, password, newName);
                            SendPacket(sender, Receive_Packets.CLIENT_RECEIVE_PACKET.PACKET_CLIENT_PROFILE_CHANGENAME, ((int)resultOptions).ToString(), newName);
                            break;
                        }
                    //SERVER
                    case Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_HOST_REGISTER:
                        {
                            string matchID = args[0];
                            int matchPort = int.Parse(args[1]);

                            Program.Logger.WriteLine(matchID);

                            ServerInfo serverInfo = null;
                            if (Program.MatchMaking.currentServer.TryGetValue(matchID, out serverInfo) == false)
                                return;

                            string machIP = "212.56.35.157";

                            Program.MatchMaking.currentServer[matchID].IP = machIP;
                            Program.MatchMaking.currentServer[matchID].Port = matchPort;
                            Program.MatchMaking.currentServer[matchID].listToRegister.Clear();

                            //
                            Program.Logger.WriteLine($"Rapid Fire Server ({matchID}) registered: IP:{Program.MatchMaking.currentServer[matchID].IP} PORT: {Program.MatchMaking.currentServer[matchID].Port}");

                            //Auto team select
                            int defenseCount = 0;
                            int attackCount = 0;
                            for (int i = 0; i < Program.MatchMaking.currentServer[matchID].users.Count; i++)
                            {
                                UserInfo info = Program.MatchMaking.currentServer[matchID].users[i];

                                if (info != null)
                                {
                                    QueueUserRegister user = new QueueUserRegister();
                                    user.userID = info.userID;
                                    user.userName = Program.Database.GetNameByAccount(info.account);

                                    if (defenseCount > attackCount)
                                    {
                                        user.TeamID = (int)UserTeamInfo.attack;
                                        attackCount++;
                                    }
                                    else
                                    {
                                        user.TeamID = (int)UserTeamInfo.defense;
                                        defenseCount++;
                                    }
                                    Program.MatchMaking.currentServer[matchID].listToRegister.Add(user);
                                }
                            }

                            SendPacket(sender, Receive_Packets.CLIENT_RECEIVE_PACKET.PACKET_ROOM_REGISTER_USERS, JsonConvert.SerializeObject(Program.MatchMaking.currentServer[matchID].listToRegister));
                            break;
                        }

                    case Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_HOST_ACCEPT_QUEUE:
                        {
                            string matchID = args[0];

                            Program.Logger.WriteLine($"MATCH CREATED >> QUEUENNING PLAYERS TO MATCH: {matchID}");

                            List<UserInfo> list = Program.MatchMaking.currentServer[matchID].users;
                            if (Program.MatchMaking.currentServer[matchID].listToRegister.Count > 0)
                            {
                                for (int i = 0; i < list.Count; i++)
                                    Program.MatchMaking.SendSinglePlayerToMatch(list[i], matchID);
                            }
                            else
                            {
                                Program.Logger.WriteLine($"MATCH CREATED >> SendPlayersToMatch, playerNames empty");
                            }

                            break;
                        }
                    //SERVER
                    case Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_HOST_UNREGISTER:
                        {
                            string matchID = args[0];
                            string matchPort = args[1];

                            try
                            {
                                //Kill Process!!!
                                //if (Program.MatchMaking.KillMatch(matchID))
                                //    Program.Logger.WriteLine($"Process closed: {matchID}");

                                //Remove Users from Queue and clear matchID;
                              //  List<UserInfo> list = Program.MatchMaking.currentServer[matchID].users;

                                ServerInfo serverInfo = null;
                                if (Program.MatchMaking.currentServer.TryGetValue(matchID, out serverInfo))
                                {
                                    for (int i = 0; i < serverInfo.users.Count; i++)
                                    {
                                        UserInfo userInfo = null;
                                        if (Program.MatchMaking.clients.TryGetValue(serverInfo.users[i].userID, out userInfo))
                                        {
                                            if (Program.Database.UpdatePlayerMatch(serverInfo.users[i].account, "0") == RESULT_OPTIONS.PLAYER_MATCH_REGISTER_OK)
                                                userInfo.inQueue = false;
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Program.Logger.WriteException($"Failed to unregister {matchID}, it's running??");
                                throw ex;
                            }

                            Program.MatchMaking.currentServer[matchID].users.Clear();
                            Program.MatchMaking.currentServer[matchID] = null;

                            Program.MatchMaking.currentServer.Remove(matchID);

                            Program.Logger.WriteLine("SERVER >> MATCH CLOSED AT PORT: " + matchPort);
                            break;
                        }
                    case Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_PROFILE_UPDATE:
                        {
                            string userID = args[0];
                            int userKills = int.Parse(args[1]);
                            int userDeaths = int.Parse(args[2]);
                            int userExperience = int.Parse(args[3]);
                            int userAlphaWins = int.Parse(args[4]);
                            int userDeltaWins = int.Parse(args[5]);

                            UserInfo userInfo = null;
                            if (Program.MatchMaking.clients.TryGetValue(userID, out userInfo))
                                Program.Database.UpdatePlayerMatch(userInfo.account, "0");

                            RESULT_OPTIONS resultOptions = Program.Database.UpdateProfile(userID, userKills, userDeaths, userExperience, userAlphaWins, userDeltaWins);
                            SendPacket(sender, Receive_Packets.CLIENT_RECEIVE_PACKET.PACKET_CLIENT_PROFILE_UPDATE, ((int)resultOptions).ToString());

                            break;
                        }
                    case Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_PROFILE_INVENTORY_UPDATE:
                        {
                            string userID = args[0];
                            int userEquipSpray = int.Parse(args[1]);
                            int unlockSprayID = int.Parse(args[2]);

                            int userEquipCharacterSkin = int.Parse(args[3]);
                            int unlockCharacterSkinID = int.Parse(args[4]);

                            string outString = "NULL";
                            RESULT_OPTIONS resultOptions = Program.Database.UpdateInventory(userID, userEquipSpray, unlockSprayID, userEquipCharacterSkin, unlockCharacterSkinID, ref outString);
                            SendPacket(sender, Receive_Packets.CLIENT_RECEIVE_PACKET.PACKET_CLIENT_PROFILE_INVENTORY_UPDATE, ((int)resultOptions).ToString(), outString);
                            break;
                        }
                    default:
                        Program.Logger.WriteLine("INVALID PACKET!!!!!!!!!!");
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Program.Logger.WriteException(ex.ToString());
            }
        }
        public void ProcessFrame()
        {
            //Update...
        }

        public bool IsNumber(string stringNumber)
        {
            int numericValue;
            return int.TryParse(stringNumber, out numericValue);
        }
    }
}