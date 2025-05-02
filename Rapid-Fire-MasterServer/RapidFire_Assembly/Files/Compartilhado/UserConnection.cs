using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using System.IO;

namespace RapidFire_MasterServer
{
    public class UserConnection
    {
        private string strUserID;

        private TcpClient tcpClient;

        public HandlePacket handlePacket;
        public HandleDisconnect handleDisconnect;

        private Byte[] bytesData = new Byte[1024];

        public UserConnection(TcpClient client)
        {
            strUserID = "0";
            this.tcpClient = client;
        }

        public TcpClient GetTcpClient()
        {
            return this.tcpClient;
        }
        public string userID
        {
            get
            {
                return strUserID;
            }
            set
            {
                strUserID = value;
            }
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
                }
            }
            catch (Exception)
            {
                this.handleDisconnect(this);
                this.GetTcpClient().GetStream().Close();
                this.GetTcpClient().Close();
            }
        }
        public bool IsConnected()
        {
            return this.GetTcpClient().Connected;
        }
    }
}