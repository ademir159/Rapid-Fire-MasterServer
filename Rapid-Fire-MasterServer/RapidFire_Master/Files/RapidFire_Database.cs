using MySql.Data.MySqlClient;
using RapidFire_MasterServer.Files.Compartilhado;
using System;
using System.Data;
using System.Data.Common;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;

namespace RapidFire_MasterServer
{
    class RapidFire_Database
    {
        private MySqlConnection connection;
        private string Host = "localhost";
        private string Database = "game_database";
        private string Username = "root";
        private string LixPassword = "";
        public const string TBL_ACCOUNT = "tbl_account";
        public const string TBL_CHARACTER = "tbl_character";
        public const string TBL_INVENTORY = "tbl_inventory";
        public const string TBL_MATCHPLAYERS = "tbl_matchplayers";
        public const string TBL_MATCHMAKER_CFG = "tbl_matchmaker_config";

        public void StartDatabase()
        {
            connection = new MySqlConnection($"Server={Host};Database={Database};Uid={Username};Pwd={LixPassword};Pooling=True");
            Program.Logger.WriteLine("Rapid Fire Database Started.");
        }

        public void Initialize()
        {

        }

        public void ProcessFrame()
        {
            //Update...
        }

        public void LoadServersConfigXML()
        {
            string path = "RapidFire_MasterConfig.xml";
            XmlReader reader = XmlReader.Create(path);
            if (reader != null)
            {
                XmlRootAttribute xRoot = new XmlRootAttribute();
                xRoot.ElementName = "MasterParams";
                xRoot.IsNullable = true;
                XmlSerializer serializer = new XmlSerializer(typeof(MasterParams), xRoot);
                MasterParams param = (MasterParams)serializer.Deserialize(reader);

                if (param != null)
                {
                    bool dif1 = Program.MatchMaking.createMatchBotNum == param.createMatchBotNum;
                    bool dif2 = Program.MatchMaking.createMathUserQueue_DM == param.createMathUserQueue_DM;
                    bool dif3 = Program.MatchMaking.createMathUserQueue_5v5 == param.createMathUserQueue_5v5;
                    bool dif4 = Program.MatchMaking.createMathUserQueue_Defuse == param.createMathUserQueue_Defuse;

                    bool dif5 = Program.MatchMaking.port_Start == param.port_Start;
                    bool dif6 = Program.MatchMaking.port_End == param.port_End;

                    if (!dif1 || !dif2 || !dif3 || !dif4 || !dif5 || !dif6)
                    {
                        Program.MatchMaking.createMatchBotNum = param.createMatchBotNum;
                        Program.MatchMaking.createMathUserQueue_DM = param.createMathUserQueue_DM;
                        Program.MatchMaking.createMathUserQueue_5v5 = param.createMathUserQueue_5v5;
                        Program.MatchMaking.createMathUserQueue_Defuse = param.createMathUserQueue_Defuse;

                        Program.MatchMaking.port_Start = param.port_Start;
                        Program.MatchMaking.port_End = param.port_End;

                        Program.Logger.WriteLine($"MasterParams was updated.");
                    }
                }
                else
                {
                    Program.Logger.WriteLine("MasterParams Deserialize error");
                }
                param = null;
                xRoot = null;
                serializer = null;
            }
            else
            {
                Program.Logger.WriteLine("MasterParams XML Reader not found.");
            }
            reader.Dispose();
            reader.Close();
        }

        public RESULT_OPTIONS RegisterName(string account, string password, string newName)
        {
            RESULT_OPTIONS resultOptions = RESULT_OPTIONS.NULL;

            try
            {
                using (connection)
                {
                    this.OpenMySQLConnection(connection);

                    if (this.HaveLogin(account, password, connection) == false)
                    {
                        this.CloseMySQLConnection(connection);
                        return RESULT_OPTIONS.NULL;
                    }
                    if (this.HaveCharacter(account, connection) == false)
                    {
                        this.CloseMySQLConnection(connection);
                        return RESULT_OPTIONS.NULL;
                    }
                    if (this.HaveName(newName, connection) == true)
                    {
                        this.CloseMySQLConnection(connection);
                        return RESULT_OPTIONS.NAME_EXIST;
                    }
                    if (RapidFire_Database.IsNameAllow(newName) == false)
                        return RESULT_OPTIONS.NAME_INVALID;


                    string query = $"UPDATE `{TBL_CHARACTER}` SET uName='{newName}' where account='{account}'";

                    int result = RapidFire_Database.GetResultNonQuery(query, connection);

                    if (result > 0)
                        resultOptions = RESULT_OPTIONS.NAME_REGISTERED;
                    else
                        resultOptions = RESULT_OPTIONS.NAME_EXIST;

                }
                this.CloseMySQLConnection(connection);
            }
            catch (MySqlException ex)
            {
                Program.Logger.WriteException(ex.ToString());
                resultOptions = RESULT_OPTIONS.NULL;
                //Close
                this.CloseMySQLConnection(connection);
            }
            return resultOptions;
        }


        public RESULT_OPTIONS LoadProfile(ref string userID, string account, string password, ref string profileString)
        {
            RESULT_OPTIONS resultOptions = RESULT_OPTIONS.PROFILE_LOAD_OK;

            try
            {
                this.OpenMySQLConnection(connection);

                using (connection)
                {
                    if (this.HaveLogin(account, password, connection) == false)
                    {
                        profileString = "NULL";
                        resultOptions = RESULT_OPTIONS.ACCOUNT_INVALID;
                    }

                    if (this.HaveCharacter(account, connection) == false)
                    {
                        RESULT_OPTIONS result = this.RegisterProfile(account);
                        if (result != RESULT_OPTIONS.PROFILE_REGISTER_OK)
                        {
                            profileString = "NULL";
                            resultOptions = result;
                        }
                    }
                    if (resultOptions == RESULT_OPTIONS.PROFILE_LOAD_OK)
                    {
                        string query = $"SELECT {TBL_CHARACTER}.uID,{TBL_CHARACTER}.uName,{TBL_CHARACTER}.uAuth,{TBL_CHARACTER}.uKills,{TBL_CHARACTER}.uDeaths,{TBL_CHARACTER}.uExperience,{TBL_CHARACTER}.uAssistences,{TBL_CHARACTER}.uMatchsWins,{TBL_CHARACTER}.uMatchsLoses, {TBL_INVENTORY}.uSpray as uSpray, {TBL_INVENTORY}.uEquipSpray as uEquipSpray,  {TBL_INVENTORY}.uCharSkin as uCharSkin, {TBL_INVENTORY}.uEquipCharSkin as uEquipCharSkin from {TBL_INVENTORY} join {TBL_CHARACTER} on {TBL_CHARACTER}.account={TBL_INVENTORY}.account where {TBL_CHARACTER}.account='{account}'";//$"SELECT * from tbl_character where account='{account}'";
                        int queryNumber = 0;
                        using (MySqlDataReader dbReader = RapidFire_Database.GetResultReader(query, connection))
                        {
                            while (dbReader.Read())
                            {
                                string result = "";

                                userID = dbReader.GetInt32("uID").ToString();
                                string userName = dbReader.GetString("uName");
                                string userAuth = dbReader.GetString("uAuth");
                                int userKills = dbReader.GetInt32("uKills");
                                int userDeaths = dbReader.GetInt32("uDeaths");
                                int userExperience = dbReader.GetInt32("uExperience");
                                int userAssistences = dbReader.GetInt32("uAssistences");
                                int userMatchsWins = dbReader.GetInt32("uMatchsWins");
                                int userMatchsLoses = dbReader.GetInt32("uMatchsLoses");

                                //Inventory
                                string userSprays = dbReader.GetString("uSpray");
                                int userEquipSpray = dbReader.GetInt32("uEquipSpray");
                                string userCharacterSkins = dbReader.GetString("uCharSkin");
                                int userEquipCharacterSkin = dbReader.GetInt32("uEquipCharSkin");

                                result += "{";
                                result += $"\"userName\":\"{userName}\",";
                                result += $"\"userID\":{userID},";
                                result += $"\"userAuth\":\"{userAuth}\",";
                                result += $"\"userKills\":{userKills},";
                                result += $"\"userDeaths\":{userDeaths},";
                                result += $"\"userExperience\":{userExperience},";
                                result += $"\"userAssistences\":{userAssistences},";
                                result += $"\"userMatchsWins\":{userMatchsWins},";
                                result += $"\"userMatchsLoses\":{userMatchsLoses},";
                                //Inventory
                                result += $"\"userSprays\":\"{userSprays}\",";
                                result += $"\"userEquipSpray\":{userEquipSpray},";
                                result += $"\"userCharacterSkins\":\"{userCharacterSkins}\",";
                                result += $"\"userEquipCharacterSkin\":{userEquipCharacterSkin}";

                                result += "}";

                                profileString = result;
                                queryNumber++;
                            }
                            dbReader.Close();
                        }
                        if (queryNumber <= 0)
                        {
                            profileString = "NULL";
                            resultOptions = RESULT_OPTIONS.PROFILE_LOAD_FAIL;
                        }
                        else
                            resultOptions = RESULT_OPTIONS.PROFILE_LOAD_OK;
                    }
                }
                this.CloseMySQLConnection(connection);
            }
            catch (MySqlException ex)
            {
                Program.Logger.WriteException(ex.ToString());
                resultOptions = RESULT_OPTIONS.PROFILE_LOAD_FAIL;
                //Close
                this.CloseMySQLConnection(connection);
            }
            return resultOptions;
        }

        public RESULT_OPTIONS UpdateProfile(string userID, int _userKills, int _userDeaths, int _userExperience, int _userMatchsWins, int _userMatchsLoses)
        {
            RESULT_OPTIONS resultOptions = RESULT_OPTIONS.PROFILE_LOAD_FAIL;

            try
            {
                this.OpenMySQLConnection(connection);

                using (connection)
                {
                    int userKills = 0, userDeaths = 0, userExperience = 0, userAssistences = 0, userMatchsWins = 0, userMatchsLoses = 0;

                    string query = $"SELECT * from {TBL_CHARACTER} where uID='{userID}'";

                    using (MySqlDataReader dbReader = RapidFire_Database.GetResultReader(query, connection))
                    {
                        while (dbReader.Read())
                        {
                            userKills = dbReader.GetInt32("uKills");
                            userDeaths = dbReader.GetInt32("uDeaths");
                            userExperience = dbReader.GetInt32("uExperience");
                            userAssistences = dbReader.GetInt32("uAssistences");
                            userMatchsWins = dbReader.GetInt32("uMatchsWins");
                            userMatchsLoses = dbReader.GetInt32("uMatchsLoses");
                        }
                        dbReader.Close();
                    }
                    userKills += _userKills;
                    userDeaths += _userDeaths;
                    userExperience += _userExperience;
                    userMatchsWins += _userMatchsWins;
                    userMatchsLoses += _userMatchsLoses;

                    query = $"UPDATE {TBL_CHARACTER} set uKills = '{userKills}',  uDeaths = '{userDeaths}', uExperience = '{userExperience}', uAssistences = '{userAssistences}',  uMatchsWins = '{userMatchsWins}', uMatchsLoses = '{userMatchsLoses}' where uID = '{userID}'";

                    int result = RapidFire_Database.GetResultNonQuery(query, connection);

                    if (result > 0)
                        resultOptions = RESULT_OPTIONS.PROFILE_SAVE_OK;
                    else
                        resultOptions = RESULT_OPTIONS.PROFILE_SAVE_FAIL;
                }
                this.CloseMySQLConnection(connection);
            }
            catch (MySqlException ex)
            {
                Program.Logger.WriteException(ex.ToString());
                resultOptions = RESULT_OPTIONS.PROFILE_SAVE_FAIL;

                //Close
                this.CloseMySQLConnection(connection);
            }
            return resultOptions;
        }

        public RESULT_OPTIONS RegisterProfile(string account)
        {
            RESULT_OPTIONS resultOptions = RESULT_OPTIONS.PROFILE_REGISTER_FAIL;

            string userName = "NULL";
            string userAuth = "U";
            int userKills = 0;
            int userDeaths = 0;
            int userExperience = 0;
            int userAssistences = 0;
            int userMatchsWins = 0;
            int userMatchsLoses = 0;

            //Insert to character Table
            string query = $"INSERT INTO `{TBL_CHARACTER}`(`account`, `uName`, `uAuth`, `uKills`, `uDeaths`, `uExperience`, `uAssistences`, `uMatchsWins`, `uMatchsLoses`) VALUES('{account}', '{userName}', '{userAuth}', '{userKills}', '{userDeaths}', '{userExperience}', '{userAssistences}', '{userMatchsWins}', '{userMatchsLoses}')";

            int result = RapidFire_Database.GetResultNonQuery(query, connection);

            if (result > 0)
                resultOptions = RESULT_OPTIONS.PROFILE_REGISTER_OK;
            else
                resultOptions = RESULT_OPTIONS.PROFILE_REGISTER_FAIL;

            //Add player to Inventory Table
            if (resultOptions == RESULT_OPTIONS.PROFILE_REGISTER_OK)
            {
                query = $"INSERT `{TBL_INVENTORY}`(`account`, `uSpray`, `uEquipSpray`, `uCharSkin`, `uEquipCharSkin` ) VALUES('{account}', '0,', '0', '0,', 0)";

                result = RapidFire_Database.GetResultNonQuery(query, connection);

                if (result > 0)
                    resultOptions = RESULT_OPTIONS.PROFILE_REGISTER_OK;
                else
                    resultOptions = RESULT_OPTIONS.PROFILE_REGISTER_FAIL;
            }

            //Add player to match Table
            if (resultOptions == RESULT_OPTIONS.PROFILE_REGISTER_OK)
            {
                query = $"INSERT `{TBL_MATCHPLAYERS}`(`account`, `uMatchID` ) VALUES('{account}', '0')";

                result = RapidFire_Database.GetResultNonQuery(query, connection);

                if (result > 0)
                    resultOptions = RESULT_OPTIONS.PROFILE_REGISTER_OK;
                else
                    resultOptions = RESULT_OPTIONS.PROFILE_REGISTER_FAIL;
            }
            return resultOptions;
        }

        //Match
        public RESULT_OPTIONS UpdatePlayerMatch(string account, string matchID)
        {
            RESULT_OPTIONS resultOptions = RESULT_OPTIONS.PLAYER_MATCH_REGISTER_FAIL;

            try
            {
                this.OpenMySQLConnection(connection);

                using (connection)
                {
                    string query = $"UPDATE `{TBL_MATCHPLAYERS}` SET uMatchID='{matchID}' where account='{account}'";

                    int result = RapidFire_Database.GetResultNonQuery(query, connection);

                    if (result > 0)
                        resultOptions = RESULT_OPTIONS.PLAYER_MATCH_REGISTER_OK;
                    else
                        resultOptions = RESULT_OPTIONS.PLAYER_MATCH_REGISTER_FAIL;
                }
                this.CloseMySQLConnection(connection);
            }
            catch (MySqlException ex)
            {
                Program.Logger.WriteException(ex.ToString());
                resultOptions = RESULT_OPTIONS.PLAYER_MATCH_REGISTER_FAIL;

                //Close
                this.CloseMySQLConnection(connection);
            }
            return resultOptions;
        }

        public RESULT_OPTIONS LoadPlayerMatch(string account, ref string matchID)
        {
            RESULT_OPTIONS resultOptions = RESULT_OPTIONS.PLAYER_MATCH_LOAD_FAIL;

            try
            {
                this.OpenMySQLConnection(connection);

                using (connection)
                {
                    string query = $"SELECT * from {TBL_MATCHPLAYERS} where account='{account}'";

                    using (MySqlDataReader dbReader = RapidFire_Database.GetResultReader(query, connection))
                    {
                        while (dbReader.Read())
                        {
                            matchID = dbReader.GetString("uMatchID");

                            resultOptions = RESULT_OPTIONS.PLAYER_MATCH_LOAD_OK;
                        }
                        dbReader.Close();
                    }
                }
                this.CloseMySQLConnection(connection);
            }
            catch (MySqlException ex)
            {
                Program.Logger.WriteException(ex.ToString());
                matchID = "0";
                resultOptions = RESULT_OPTIONS.PLAYER_MATCH_LOAD_FAIL;
                //Close
                this.CloseMySQLConnection(connection);
            }
            return resultOptions;
        }

        //Inventory
        public RESULT_OPTIONS UpdateInventory(string userID, int _userEquipSpray, int _unlockSprayID, int _userEquipCharacterSkin, int _unlockCharacterSkinID, ref string inventoryString)
        {
            RESULT_OPTIONS resultOptions = RESULT_OPTIONS.UPDATE_INVENTORY_OK;

            try
            {
                this.OpenMySQLConnection(connection);

                using (connection)
                {
                    string userSprays = "0,";
                    int userEquipCharSpray = -1;
                    string userCharacterSkins = "0,";
                    int userEquipCharSkin = -1;
                    string account = "";



                    string query = $"SELECT {TBL_INVENTORY}.uSpray, {TBL_INVENTORY}.uEquipSpray, {TBL_INVENTORY}.uCharSkin, {TBL_INVENTORY}.uEquipCharSkin, {TBL_INVENTORY}.account, {TBL_CHARACTER}.account as account from {TBL_CHARACTER} join {TBL_INVENTORY} on {TBL_INVENTORY}.account = {TBL_CHARACTER}.account where {TBL_CHARACTER}.uID='{userID}'";

                    using (MySqlDataReader dbReader = RapidFire_Database.GetResultReader(query, connection))
                    {
                        while (dbReader.Read())
                        {
                            userSprays = dbReader.GetString("uSpray");
                            userEquipCharSpray = dbReader.GetInt32("uEquipSpray");
                            userCharacterSkins = dbReader.GetString("uCharSkin");
                            userEquipCharSkin = dbReader.GetInt32("uEquipCharSkin");
                            account = dbReader.GetString("account");
                        }
                        dbReader.Close();
                    }
                    if (_unlockSprayID != -1)
                        userSprays += _unlockSprayID.ToString() + ",";

                    if (_unlockCharacterSkinID != -1)
                        userCharacterSkins += _unlockCharacterSkinID.ToString() + ",";


                    query = $"UPDATE {TBL_INVENTORY} set";
                    query += $" uSpray = '{userSprays}'";

                    if (_userEquipSpray != -1)
                    {
                        query += ",";
                        query += $" uEquipSpray = '{_userEquipSpray}'";
                    }
                    query += ",";
                    query += $" uCharSkin = '{userCharacterSkins}'";

                    if (_userEquipCharacterSkin != -1)
                    {
                        query += ",";
                        query += $" uEquipCharSkin = '{_userEquipCharacterSkin}'";
                    }

                    query += $" where account = '{account}'";

                    int result = RapidFire_Database.GetResultNonQuery(query, connection);

                    if (_userEquipSpray == -1)
                        _userEquipSpray = userEquipCharSpray;
                    if (_userEquipCharacterSkin == -1)
                        _userEquipCharacterSkin = userEquipCharSkin;

                    string resultSTR = "";
                    if (result > 0)
                    {
                        resultOptions = RESULT_OPTIONS.UPDATE_INVENTORY_OK;

                        resultSTR += "{";
                        resultSTR += $"\"userSprays\":\"{userSprays}\",";
                        resultSTR += $"\"userEquipSpray\":{_userEquipSpray},";
                        resultSTR += $"\"userCharacterSkins\":\"{userCharacterSkins}\",";
                        resultSTR += $"\"userEquipCharacterSkin\":{_userEquipCharacterSkin}";
                        resultSTR += "}";

                        inventoryString = resultSTR;
                    }
                    else
                        resultOptions = RESULT_OPTIONS.UPDATE_INVENTORY_FAIL;
                }
                this.CloseMySQLConnection(connection);
            }
            catch (MySqlException ex)
            {
                Program.Logger.WriteException(ex.ToString());
                resultOptions = RESULT_OPTIONS.UPDATE_INVENTORY_FAIL;
                //Close
                this.CloseMySQLConnection(connection);
            }
            return resultOptions;
        }

        public string GetNameByAccount(string account)
        {
            string nameByAccount = "";

            try
            {
                this.OpenMySQLConnection(connection);

                using (connection)
                {
                    string query = $"SELECT * FROM {TBL_CHARACTER} where account='{account}'";

                    using (MySqlDataReader dbReader = RapidFire_Database.GetResultReader(query, connection))
                    {
                        while (dbReader.Read())
                        {
                            nameByAccount = dbReader.GetString("uName");
                        }
                        dbReader.Close();
                    }
                }
                this.CloseMySQLConnection(connection);
            }
            catch (MySqlException ex)
            {
                Program.Logger.WriteException(ex.ToString());
                //Close
                this.CloseMySQLConnection(connection);
            }
            return nameByAccount;
        }

        public bool ContainsInQuery(string query, MySqlConnection conn)
        {
            bool checkStatus = false;

            try
            {
                using (MySqlDataReader dbReader = RapidFire_Database.GetResultReader(query, conn))
                {
                    while (dbReader.Read())
                    {
                        checkStatus = true;
                    }
                    dbReader.Close();
                }
            }
            catch (MySqlException ex)
            {
                Program.Logger.WriteException(ex.ToString());
                checkStatus = false;
            }
            return checkStatus;
        }

        public bool HaveLogin(string account, string password, MySqlConnection conn)
        {
            return this.ContainsInQuery($"SELECT * from {TBL_ACCOUNT} where account='{account}' and password='{password}'", conn);
        }
        public bool HaveCharacter(string account, MySqlConnection conn)
        {
            return this.ContainsInQuery($"SELECT * from {TBL_CHARACTER} where account='{account}'", conn);
        }
        public bool HaveName(string name, MySqlConnection conn)
        {
            return this.ContainsInQuery($"SELECT * from {TBL_CHARACTER} where uName='{name}'", conn);
        }

        void OpenMySQLConnection(MySqlConnection conn)
        {
            if (conn.State != ConnectionState.Open)
            {
                conn.Close();
                conn.Open();
            }
        }
        void CloseMySQLConnection(MySqlConnection conn)
        {
            if (conn.State != ConnectionState.Closed)
            {
                conn.Dispose();
                conn.Close();
            }
        }
        private static MySqlDataReader GetResultReader(string insertQuery, MySqlConnection conn)
        {
            MySqlCommand cmd = new MySqlCommand(insertQuery);
            cmd.CommandType = CommandType.Text;
            cmd.Connection = conn;

            MySqlDataReader reader = cmd.ExecuteReader();
            cmd.Dispose();
            return reader;
        }
        private static int GetResultNonQuery(string commandText, MySqlConnection conn)
        {
            int value = 0;
            var command = conn.CreateCommand();
            command.CommandText = commandText;

            value = command.ExecuteNonQuery();
            command.Dispose();

            return value;
        }
        static bool IsNameAllow(string newName)
        {
            string[] ignoredNamesInSolr = new string[] {
            "puta" , "Puta"      , "PuTa"      , "PUTA"      , "PutA",
            "lixo" , "Lixo"      , "LiXo"      , "LIXO"      , "LixO",
            "rapid", "Rapid"     , "RaPiD"     , "RAPID"     ,
            "GM"   , "GameMaster", "gameMaster", "Gamemaster",
            "Admin", "ADM"       , "DarO"      , "Chupa"     , "Xupa"     , "Xup4"   , "Chup4"   ,
            "Pinto", "Pint0"     , "P1nt0"     , "P1nto"     , "pinto"    , "pint0"  , "p1nt0"   , "p1nto"  ,  "PINTO",  "PINT0"    , "P1NT0"    , "P1NTO",
            "Viado", "VIADO"     , "V14D0"     , "V14do"     , "vIaDo"    , "Viad0"  , "NULL"    , "Null"   ,  "null" ,
            "POST" , "post"      , "Post"      , "WHERE"     , "where"    , "Where"  , "viado"   , " "      ,  "'"    ,   ";"
            };
            for (int i = 0; i < ignoredNamesInSolr.Length; i++)
            {
                if (newName.Contains(ignoredNamesInSolr[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}