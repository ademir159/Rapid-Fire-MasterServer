using System;
using System.Collections.Generic;
using System.Text;

[System.Serializable]
public enum UserTeamInfo
{
    defense = 54,
    attack = 98,
    spectator = 976,
    all = 1045,
};

namespace RapidFire_MasterServer.Files.Compartilhado
{
    public enum RESULT_OPTIONS
    {
        NULL = 0,
        ACCOUNT_INVALID,
        ACCOUNT_VALID,
        ACCOUNT_OK,
        CHARACTER_INVALID,
        CHARACTER_VALID,
        CHARACTER_OK,
        NAME_REGISTERED,
        NAME_EXIST,
        NAME_INVALID,
        PROFILE_LOAD_FAIL,
        PROFILE_LOAD_OK,
        PROFILE_SAVE_FAIL,
        PROFILE_SAVE_OK,
        PROFILE_REGISTER_FAIL,
        PROFILE_REGISTER_OK,
        PLAYER_MATCH_REGISTER_OK,
        PLAYER_MATCH_REGISTER_FAIL,
        PLAYER_MATCH_LOAD_OK,
        PLAYER_MATCH_LOAD_FAIL,
        UPDATE_INVENTORY_OK,
        UPDATE_INVENTORY_FAIL
    }
    public static class Receive_Packets
    {
        public enum CLIENT_RECEIVE_PACKET
        {
            PACKET_CLIENT_NONE = 0x00F00100,//Define o pacote inicial, criado com variável em DWORD

            PACKET_CLIENT_JOIN,
            PACKET_CLIENT_REFUSE,
            PACKET_CLIENT_KEEP_ALIVE,

            PACKET_CLIENT_ENTER_MATCH,

            PACKET_CLIENT_PROFILE_LOADED,
            PACKET_CLIENT_PROFILE_CHANGENAME,

            PACKET_CLIENT_PROFILE_UPDATE,
            PACKET_CLIENT_PROFILE_INVENTORY_UPDATE,

            PACKET_ROOM_REGISTER_USERS,
        }
        public enum SERVER_RECEIVE_PACKET
        {
            PACKET_SERVER_NONE = 0x00FFF700,//Define o pacote inicial, criado com variável em DWORD

            PACKET_SERVER_CONNECT,
            PACKET_SERVER_HOST_CONNECT,
            PACKET_SERVER_DISCONNECT,
            PACKET_SERVER_KEEP_ALIVE,

            PACKET_SERVER_USER_QUEUE_IN,
            PACKET_SERVER_USER_QUEUE_OUT,

            PACKET_SERVER_HOST_REGISTER,
            PACKET_SERVER_HOST_UNREGISTER,
            PACKET_SERVER_HOST_ACCEPT_QUEUE,

            PACKET_SERVER_PROFILE_LOAD,
            PACKET_SERVER_PROFILE_CHANGENAME,

            PACKET_SERVER_PROFILE_UPDATE,
            PACKET_SERVER_PROFILE_INVENTORY_UPDATE,
        }
    }

}
