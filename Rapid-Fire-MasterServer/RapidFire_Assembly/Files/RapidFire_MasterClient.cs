using System;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading;
using RapidFire_MasterServer.Files.Compartilhado;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;

namespace RapidFire_MasterServer
{
    [System.Serializable]
    public class QueueUserRegister
    {
        public string userID = "-1";
        public string userName = "NULL";
        public int TeamID = 54;
    }

    public class MasterConnection
    {
        #region Variables

        public TcpClient socketConnection;
        private Thread clientReceiveThread;

        public event OnMessageLog MessageLog;

        //Received Events
        public event OnReceive_Packet_Join Packet_Join;
        public event OnReceive_Packet_Refuse Packet_Refuse;
        public event OnReceive_Packet_KeepAlive Packet_KeepAlive;
        public event OnReceive_Packet_EnterMatch Packet_EnterMatch;
        public event OnReceive_Packet_RoomRegisterUsers Packet_RoomRegisterUsers;
        public event OnReceive_Packet_Disconnected Packet_Disconnected;

        //Profile
        public event OnReceive_Packet_ChangeName Packet_ChangeName;
        public event OnReceive_Packet_UpdateProfile Packet_UpdateProfile;
        public event OnReceive_Packet_UpdateInventory Packet_UpdateInventory;

        public string serverIP = "0";
        public int serverPORT = 0;

        private string hostID = "0";
        private string userID = "0";
        private string userAccount = "NULL";
        private string userPassword = "NULL";
        private bool dedicatedServer = false;

        public bool connected;
        #endregion

        #region Networking

        public void CreateServerSession(string _hostID)
        {
            this.hostID = _hostID;
            this.dedicatedServer = true;
            this.StartListener();
        }
        public void CreateLobbySession(string _account, string _password)
        {
            this.userAccount = _account;
            this.userPassword = _password;
            this.dedicatedServer = false;

            this.StartListener();
        }

        void StartListener()
        {
            try
            {
                clientReceiveThread = new Thread(new ThreadStart(ListenForData));
                clientReceiveThread.IsBackground = true;
                clientReceiveThread.Start();
            }
            catch (SocketException socketException)
            {
                this.MessageLog("Socket exception: " + socketException);
                this.Packet_Disconnected();
                connected = false;
            }
        }
        private void ListenForData()
        {
            try
            {
                socketConnection = new TcpClient(this.serverIP, this.serverPORT);

                Byte[] bytes = new Byte[1024];

                //NEED AUTH
                if (this.dedicatedServer)
                    this.SendPacket(Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_HOST_CONNECT, this.hostID);
                else
                    this.SendPacket(Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_CONNECT, userAccount, this.userPassword);

                connected = true;

                while (true)
                {
                    if (socketConnection != null && socketConnection.Connected == false)
                        return;

                    using (NetworkStream stream = socketConnection.GetStream())
                    {
                        if (stream != null)
                        {
                            int length;
                            while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                            {
                                var incommingData = new byte[length];
                                Array.Copy(bytes, 0, incommingData, 0, length);
                                string serverMessage = Encoding.UTF8.GetString(incommingData);

                                this.OnHandlePacket(serverMessage);
                                //LOG
                                this.MessageLog("RECV >> " + serverMessage);
                            }
                        }
                        else
                        {
                            this.MessageLog("FAILED TO RECEIVE STREAM");
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                this.Packet_Disconnected();
                this.MessageLog("Socket exception: " + ex.Message);

                if (this.socketConnection != null)
                {
                    this.socketConnection.Dispose();
                    this.socketConnection.Close();
                }
                connected = false;
            }
        }
        #endregion

        #region Packet Message
        public void SendMessage(string clientMessage)
        {
            if (socketConnection == null)
                return;

            try
            {
                if (socketConnection.Connected == false)
                    return;

                NetworkStream stream = socketConnection.GetStream();
                if (stream.CanWrite)
                {
                    byte[] clientMessageAsByteArray = Encoding.UTF8.GetBytes(clientMessage);
                    stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
                }
            }
            catch (SocketException socketException)
            {
                this.Packet_Disconnected();
                this.MessageLog("Socket exception: " + socketException);
                connected = false;
            }
        }

        private void SendPacket(Receive_Packets.SERVER_RECEIVE_PACKET packet, params string[] args)
        {
            string argsData = "";
            for (int i = 0; i < args.Length; i++)
                argsData += args[i] + "#";

            string convertPacket = ((int)packet).ToString() + "^" + argsData;//^ separator
            string cryptoPacket = AesEncryption.Encrypt(convertPacket, "9f04d346ac0e135e1571041dd8e8e4fb");
            this.SendMessage(cryptoPacket);
        }
        #endregion

        #region Default Packets
        //Connection
        public void DisconnectFromMaster() => this.SendPacket(Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_DISCONNECT, this.userID);
        public void SendKeepAlive() => this.SendPacket(Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_KEEP_ALIVE, this.userID);

        //Client Queue
        public void StartQueue(int _matchMode) => this.SendPacket(Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_USER_QUEUE_IN, this.userID, _matchMode.ToString());
        public void StopQueue(int _matchMode) => this.SendPacket(Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_USER_QUEUE_OUT, this.userID, _matchMode.ToString());

        //Host Data
        public void MasterRegister(string _matchID, int _port) => this.SendPacket(Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_HOST_REGISTER, _matchID, _port.ToString());
        public void MasterUnRegister(string _matchID, int _port) => this.SendPacket(Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_HOST_UNREGISTER, _matchID, _port.ToString());

        public void AllPlayersRegistered(string _matchID) => this.SendPacket(Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_HOST_ACCEPT_QUEUE, _matchID);
        //Profile
        public void Profile_ChangeName(string _newName) => this.SendPacket(Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_PROFILE_CHANGENAME, this.userID, this.userAccount, this.userPassword, _newName);
        public void Profile_UpdateInventory(int _userEquipSpray, int _unlockSprayID, int _userEquipCharacterSkin, int _unlockCharacterSkinID) => this.SendPacket(Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_PROFILE_INVENTORY_UPDATE, this.userID, _userEquipSpray.ToString(), _unlockSprayID.ToString(), _userEquipCharacterSkin.ToString(), _unlockCharacterSkinID.ToString());

        //Server Profile
        public void Profile_Update(string _ID, int _Kills, int _Deaths, int _Exp, int _AWin, int _DWin) => this.SendPacket(Receive_Packets.SERVER_RECEIVE_PACKET.PACKET_SERVER_PROFILE_UPDATE, _ID, _Kills.ToString(), _Deaths.ToString(), _Exp.ToString(), _AWin.ToString(), _DWin.ToString());



        #endregion

        public bool IsNumber(string stringNumber)
        {
            int numericValue;
            return int.TryParse(stringNumber, out numericValue);
        }
        //RECV MESSAGE 
        private void OnHandlePacket(string cryptoData)
        {
            try
            {
                string argsData = AesEncryption.Decrypt(cryptoData, "9f04d346ac0e135e1571041dd8e8e4fb");

                //IGNORE LOSS DATA!!!!!
                if (argsData.Contains("^") == false)
                    return;

                string[] dataArray;
                dataArray = argsData.Split('^');//^ = separator

                if (IsNumber(dataArray[0]) == false)//check packet if is ID.
                    return;

                string[] args = dataArray[1].Split('#');//split args

                Receive_Packets.CLIENT_RECEIVE_PACKET packet = (Receive_Packets.CLIENT_RECEIVE_PACKET)int.Parse(dataArray[0]);

                switch (packet)
                {
                    case Receive_Packets.CLIENT_RECEIVE_PACKET.PACKET_CLIENT_JOIN:
                        {
                            RESULT_OPTIONS result = (RESULT_OPTIONS)int.Parse(args[0]);

                            this.Packet_Join(result, args[1]);
                            break;
                        }
                    case Receive_Packets.CLIENT_RECEIVE_PACKET.PACKET_CLIENT_REFUSE:
                        {
                            RESULT_OPTIONS result = (RESULT_OPTIONS)int.Parse(args[0]);
                            this.Packet_Refuse(result);
                            break;
                        }
                    case Receive_Packets.CLIENT_RECEIVE_PACKET.PACKET_CLIENT_KEEP_ALIVE:
                        {
                            if (IsNumber(args[0]))
                            {
                                int ccu = int.Parse(args[0]);
                                this.Packet_KeepAlive(ccu);
                            }
                            break;
                        }
                    case Receive_Packets.CLIENT_RECEIVE_PACKET.PACKET_CLIENT_ENTER_MATCH:
                        {
                            if (IsNumber(args[0]))
                            {
                                int port = int.Parse(args[0]);
                                string ip = args[1];
                                List<QueueUserRegister> users = JsonConvert.DeserializeObject<List<QueueUserRegister>>(args[2]);

                                if (users != null)
                                    this.Packet_EnterMatch(ip, port, users);
                                else
                                    this.MessageLog("PACKET_ROOM_REGISTER_USERS exception: users=NULL");
                            }
                            break;
                        }

                    case Receive_Packets.CLIENT_RECEIVE_PACKET.PACKET_ROOM_REGISTER_USERS:
                        {
                            List<QueueUserRegister> users = JsonConvert.DeserializeObject<List<QueueUserRegister>>(args[0]);
                            if (users != null)
                                this.Packet_RoomRegisterUsers(users);
                            else
                                this.MessageLog("PACKET_ROOM_REGISTER_USERS exception: users=NULL");
                            break;
                        }
                    //Profile
                    case Receive_Packets.CLIENT_RECEIVE_PACKET.PACKET_CLIENT_PROFILE_CHANGENAME:
                        {
                            RESULT_OPTIONS result = (RESULT_OPTIONS)int.Parse(args[0]);

                            this.Packet_ChangeName(result, args[1]);

                            break;
                        }
                    //Update Profile
                    case Receive_Packets.CLIENT_RECEIVE_PACKET.PACKET_CLIENT_PROFILE_UPDATE:
                        {
                            RESULT_OPTIONS result = (RESULT_OPTIONS)int.Parse(args[0]);
                            this.Packet_UpdateProfile(result);
                            break;
                        }
                    //Update Inventory
                    case Receive_Packets.CLIENT_RECEIVE_PACKET.PACKET_CLIENT_PROFILE_INVENTORY_UPDATE:
                        {
                            RESULT_OPTIONS result = (RESULT_OPTIONS)int.Parse(args[0]);
                            this.Packet_UpdateInventory(result, args[1]);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                this.MessageLog("Socket exception: " + ex.Message);
            }
        }
        public void SetUserID(string _userID)
        {
            this.userID = _userID;
        }
    }
}