using RapidFire_MasterServer.Files.Compartilhado;
using System.Collections.Generic;

namespace RapidFire_MasterServer
{
    //Client
    public delegate void OnMessageLog(string data);

    public delegate void OnReceive_Packet_Join(RESULT_OPTIONS result, string profileData);
    public delegate void OnReceive_Packet_Refuse(RESULT_OPTIONS result);
    public delegate void OnReceive_Packet_KeepAlive(int ccu);
    public delegate void OnReceive_Packet_EnterMatch(string ip, int port, List<QueueUserRegister> users);
    public delegate void OnReceive_Packet_RoomRegisterUsers(List<QueueUserRegister> users);

    public delegate void OnReceive_Packet_Disconnected();
    public delegate void OnReceive_Packet_ChangeName(RESULT_OPTIONS result, string name);
    public delegate void OnReceive_Packet_UpdateProfile(RESULT_OPTIONS result);
    public delegate void OnReceive_Packet_UpdateInventory(RESULT_OPTIONS result, string inventoryData);

    //Server
    public delegate void HandlePacket(UserConnection sender, string criptoData);
    public delegate void HandleDisconnect(UserConnection sender);
}
