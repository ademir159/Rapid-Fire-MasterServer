using System;
using System.Threading;
using System.Timers;

namespace RapidFire_MasterServer
{
    class Program
    {
        public const int TIME_SEC = 1000;
        public const int TIME_MIN = TIME_SEC * 60;
        public const int TIME_HOUR = (TIME_SEC * 60) * 60;

        public static RapidFire_Logger Logger;
        public static RapidFire_Database Database;
        public static RapidFire_MatchMaking MatchMaking;
        public static RapidFire_Networking Networking;

        static void Main(string[] args)
        {
            //INIT
            Logger = new RapidFire_Logger();
            Database = new RapidFire_Database();
            MatchMaking = new RapidFire_MatchMaking();
            Networking = new RapidFire_Networking();

            //CONSOLE LOG
            Program.Logger.WriteLine("--------------------------------------");
            Program.Logger.WriteLine("Rapid Fire Authenticator");
            Program.Logger.WriteLine("--------------------------------------");

            //CREATE LISTENER
            Database.StartDatabase();
            MatchMaking.StartMatchMaking();
            Networking.StartConnection();

            Program.Logger.WriteLine("--------------------------------------");


            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                Program.Logger.WriteLine("This process is running in WINDOWS");
            else
                Program.Logger.WriteLine("This process is running in LINUX");

            Program.Logger.WriteLine("--------------------------------------");
            Thread.Sleep(1000);//speed 1 sec for start server listener.

            //Initialize function
            Database.Initialize();
            MatchMaking.Initialize();
            Networking.Initialize();

            //Auto Config Update
            //   System.Timers.Timer newTimer = new System.Timers.Timer();
            //   newTimer.Elapsed += new System.Timers.ElapsedEventHandler(DisplayTimeEvent);
            //   newTimer.Interval = TIME_MIN;
            //   newTimer.AutoReset = true;
            //   newTimer.Enabled = true;

          //  int i = 0;
            while (true)
            {
                Database.ProcessFrame();
                MatchMaking.ProcessFrame();
                Networking.ProcessFrame();

               // Program.Logger.WriteLine("" + i);

                //i++;
                Thread.Sleep(100);//Update every 0.1 sec
            }
        }
        /*
        public static void DisplayTimeEvent(object source, ElapsedEventArgs e)
        {
            MatchMaking.UpdateConfig();
        }
        */
    }
}
