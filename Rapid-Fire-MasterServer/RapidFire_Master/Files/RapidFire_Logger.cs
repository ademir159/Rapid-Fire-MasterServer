using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace RapidFire_MasterServer
{

    class RapidFire_Logger
    {
        public void StartLogger()
        {
        }
        public void ProcessFrame()
        {
            //Update...
        }

        public void WriteLine(string statusMessage)
        {
            Console.WriteLine(statusMessage);
            RapidFire_Logger.Create_Log(System.DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss") + ": " + statusMessage);
        }
        public void WriteException(string statusMessage)
        {
            Console.WriteLine(statusMessage);
            RapidFire_Logger.Create_Exception(System.DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss") + ": " + statusMessage);
        }

        public static void Create_Log(string strLog)
        {
            StreamWriter log;
            FileStream fileStream = null;
            DirectoryInfo logDirInfo = null;
            FileInfo logFileInfo;

            string logFilePath = System.IO.Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar;// "C:\\Logs\\";
            logFilePath = logFilePath + "Log-" + System.DateTime.Today.ToString("MM-dd-yyyy") + "." + "txt";
            logFileInfo = new FileInfo(logFilePath);
            logDirInfo = new DirectoryInfo(logFileInfo.DirectoryName);
            if (!logDirInfo.Exists) logDirInfo.Create();
            if (!logFileInfo.Exists)
            {
                fileStream = logFileInfo.Create();
            }
            else
            {
                fileStream = new FileStream(logFilePath, FileMode.Append);
            }
            log = new StreamWriter(fileStream);
            log.WriteLine(strLog);
            log.Close();
        }
        public static void Create_Exception(string strLog)
        {
            StreamWriter log;
            FileStream fileStream = null;
            DirectoryInfo logDirInfo = null;
            FileInfo logFileInfo;

            string logFilePath = System.IO.Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar;// "C:\\Logs\\";
            logFilePath = logFilePath + "Exc-" + System.DateTime.Today.ToString("MM-dd-yyyy") + "." + "txt";
            logFileInfo = new FileInfo(logFilePath);
            logDirInfo = new DirectoryInfo(logFileInfo.DirectoryName);
            if (!logDirInfo.Exists) logDirInfo.Create();
            if (!logFileInfo.Exists)
            {
                fileStream = logFileInfo.Create();
            }
            else
            {
                fileStream = new FileStream(logFilePath, FileMode.Append);
            }
            log = new StreamWriter(fileStream);
            log.WriteLine(strLog);
            log.Close();
        }
    }
}
