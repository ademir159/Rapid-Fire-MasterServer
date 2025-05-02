using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.IO;
using System.Timers;
using RapidFire_MasterServer.Files.Compartilhado;

namespace RapidFire_MasterServer
{
    //defense = 54,
    //attack = 98,
    //spectator = 976,
    //all = 1045,

    [System.Serializable]
    public class QueueUserRegister
    {
        public string userID = "-1";
        public string userName = "NULL";
        public int TeamID = (int)UserTeamInfo.spectator;
    }


    public class UserConnection
    {
        public string userID;

        private TcpClient tcpClient;

        public HandlePacket handlePacket;
        public HandleDisconnect handleDisconnect;

        private Byte[] bytesData = new Byte[1024];

    //    private System.Timers.Timer aTimer;

        //private long sendKeepAliveID = 0;
        //private long recvKeepAliveID = 0;

        bool IsBound = true;

        public const int PING_INTERVAL = 10000;

        public Task task = null;
        CancellationTokenSource tokenSource = new CancellationTokenSource();


        public void CreatePingMesage()
        {
            var token = tokenSource.Token;

            task = Task.Run(() =>
            {
                while (IsBound)
                {
                    Program.Logger.WriteLine($"Task thread ID: {Thread.CurrentThread.ManagedThreadId}");

                    Program.Networking.SendPacket(this, Receive_Packets.CLIENT_RECEIVE_PACKET.PACKET_CLIENT_KEEP_ALIVE, Program.MatchMaking.clients.Count.ToString());
                    Thread.Sleep(PING_INTERVAL);
                }
            }, token);
        }
        public void UpdateKeepAlive()
        {
        }

        public UserConnection(TcpClient client)
        {
            userID = "0";
            this.tcpClient = client;
        }

        /*
        public void BeginKeepAlive()
        {
            aTimer = new System.Timers.Timer();
            aTimer.Interval = 10 * 1000;
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;

            aTimer.Start();
        }
        public void UpdateKeepAlive()
        {
            //Program.Logger.WriteLine($"Receive KeepAlive from {userID}");
            recvKeepAliveID += 1;
        }
        private void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            if (recvKeepAliveID != sendKeepAliveID)
            {
                Program.Logger.WriteLine($"KEEP ALIVE >> FAIL >> {userID}");
                Program.Networking.DisconnectUser(this);
            }
            else
            {
                sendKeepAliveID += 1;
                Program.Networking.SendPacket(this, Receive_Packets.CLIENT_RECEIVE_PACKET.PACKET_CLIENT_KEEP_ALIVE, Program.MatchMaking.clients.Count.ToString());
                //Program.Logger.WriteLine($"SendKeepAlive to {userID}");

            }
        }
        */
        public TcpClient GetTcpClient()
        {
            return this.tcpClient;
        }
        public void SetTcpClient(TcpClient _tcpClient)
        {
            /*
            if (aTimer != null)
            {
                this.aTimer.Close();
                this.aTimer.Dispose();
                this.aTimer = null;
            }
            */

            this.IsBound = false;
            tokenSource.Cancel();
            /*
            if (task != null)
                task.Dispose();
            */


            this.tcpClient = _tcpClient;


        }

        public void BeginRead()
        {
            try
            {
                this.GetTcpClient().GetStream().BeginRead(bytesData, 0, bytesData.Length, new AsyncCallback(OnReadComplete), null);
            }
            catch (Exception)
            {
                this.handleDisconnect(this);
                this.GetTcpClient().GetStream().Close();
                this.GetTcpClient().Close();

                this.IsBound = false;
                tokenSource.Cancel();
                //     this.aTimer.Close();
                //   this.aTimer.Dispose();
                // this.aTimer = null;
                //if (task != null)
                //    task.Dispose();
            }
        }

        private void OnReadComplete(IAsyncResult ar)
        {
            try
            {
                int bytesRead = this.GetTcpClient().GetStream().EndRead(ar);

                if (bytesRead > 0)
                {
                    string s = System.Text.Encoding.UTF8.GetString(bytesData, 0, bytesRead);
                    this.handlePacket(this, s);
                    this.BeginRead();
                }
                else
                {
                    this.handleDisconnect(this);
                    this.GetTcpClient().GetStream().Close();
                    this.GetTcpClient().Close();
                    this.IsBound = false;
                }
            }
            catch (Exception)
            {
                this.handleDisconnect(this);
                /*
                if (task != null)
                    task.Dispose();
                */
                this.IsBound = false;
                tokenSource.Cancel();
            }
        }
        public bool IsConnected()
        {
            return this.GetTcpClient().Connected;
        }
    }
}