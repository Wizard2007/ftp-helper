using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace FTPHelperLib
{
    public static class FTPHelperConst
    {
        public const string PATH_TO_SERVER = "ftp://{0}:{1}";
        public const int DEFAULT_PORT = 21;
        public const int DEFAULT_UP_LOAD_BUFF_SIZE = 2048;
        public const string DEFAULT_ROOT_DIR = "/";
        public const string DEFAULT_PATH_DELIMETER = "/";
        public const string DEFAULT_FILE_NAME = "";
        public const string CD_DIR_URL_COMMAND = "/%2F";
        public const Boolean DEFAULT_USE_BINARY = true;
        public const Boolean DEFAULT_USE_PASSIVE = true;
        public const Boolean DEFAULT_USE_KEEP_ALIVE = true;
        public const Char FTP_FILE_SIGNATURE = '-';
        public const string DEFAULT_KEY_FTP_PATH_TO_SERVER = "FTP_PATH_TO_SERVER";
        public const string DEFAULT_KEY_FTP_PORT = "FTP_PORT";
        public const string DEFAULT_KEY_FTP_USER_NAME = "FTP_USER_NAME";
        public const string DEFAULT_KEY_FTP_PASSWORD = "FTP_PASSWORD";
    }

    class FTPHelper
    {
        /*
         */
        public string KeyFtpPathToServer { get; set; }
        public string KeyFtpPort { get; set; }
        public string KeyFtpUserName { get; set; }
        public string KeyFtpPasword { get; set; }
        /*
         */
        public string ServerName { get; set;}
        public int Port { get; set;}
        public int UpLoadBuffSize { get; set; }
        public string UserName { get; set;}
        public string Password { get; set;}
        public Boolean UseBinary { get; set;}
        public Boolean UsePassive { get; set;}
        public Boolean KeepAlive { get; set;}
        public string RootDir { get; set; }
        private string _CurrentDir;
        public string CurrentDir {
            get { return _CurrentDir; }
            set { _CurrentDir = ProcessPath(value); }
        }
        public string CurrentFileName { get; set; }
        public Boolean Connected {get; private set;}

        public FtpWebRequest Request { get; set; }
        public FtpWebResponse Response { get; set; }
        public Boolean isStrInArraty(string AStr, string[] AArray)
        { 
            for (int i = 0; i < AArray.Length; i++)
            {
                if (AStr == AArray[i])
                {
                    return true;
                }
            }
            return false;
        }
        public string ProcessPath(string APath)
        {
            string lPath = FTPHelperConst.DEFAULT_PATH_DELIMETER + APath + FTPHelperConst.DEFAULT_PATH_DELIMETER;
            lPath = lPath.Replace(FTPHelperConst.DEFAULT_PATH_DELIMETER + FTPHelperConst.DEFAULT_PATH_DELIMETER
                , FTPHelperConst.DEFAULT_PATH_DELIMETER);
            return lPath;
        }
        public Boolean ChangeDir(string ANewDir) 
        {
            DisConnect();
            
            CurrentDir = ProcessPath(ANewDir);
            string lPathToServer = string.Format(FTPHelperConst.PATH_TO_SERVER, ServerName, Port) + CurrentDir;
            Connected = NewConnection(lPathToServer, UserName, Password);
            return Connected;
        }
        public Boolean UploadFile(string APathtoSourceFile, string APathToDestFile, string ADestFileName, int ABuffSize)
        {
            CurrentFileName = ADestFileName;
            FileStream lFileStream = new FileStream(APathtoSourceFile, FileMode.Open);
            if (!string.IsNullOrWhiteSpace(APathToDestFile))
            {
                 CurrentDir = APathToDestFile;
            }
            DisConnect();
            if (Connect())
            {
                return UploadStream(lFileStream, ABuffSize);
            }
            else
            {
                return false;
            }
        }
        public Boolean UploadStream(Stream ASourceStream, int ABuffSize)
        {
            if (Connected)
            {
                Request.Method = WebRequestMethods.Ftp.UploadFile;
                Request.ContentLength = ASourceStream.Length;

                int buffLength = ABuffSize;
                byte[] buff = new byte[ABuffSize];
                int contentLen;               
 
                Stream strm = Request.GetRequestStream();
 
                contentLen = ASourceStream.Read(buff, 0, buffLength);
 
                while (contentLen != 0)
                {
                    strm.Write(buff, 0, contentLen);
                    contentLen = ASourceStream.Read(buff, 0, buffLength);
                }
                strm.Close();
                FtpWebResponse response1 = (FtpWebResponse)Request.GetResponse();

                Console.WriteLine("Upload File Complete, status {0}", response1.StatusDescription);
                response1.Close();
                return true;
            }
            else
            {
                return false;
            }
        }

        public Boolean DownloadFile(string APathToDestFile, string APathToSourceFile, string ASourceFileName, int ABuffSize)
        {
            CurrentFileName = ASourceFileName;
            FileStream lFileStream = new FileStream(APathToDestFile, FileMode.Create);
            if (!string.IsNullOrWhiteSpace(APathToDestFile))
            {
                CurrentDir = APathToSourceFile;
            }
            DisConnect();
            if (Connect())
            {
                return DownloadStream(lFileStream, ABuffSize);                
            }
            else
            {
                return false;
            }
        }

        public Boolean DownloadStream(Stream ADestStream, int ABuffSize)
        {
            if (Connected)
            {
                Request.Method = WebRequestMethods.Ftp.DownloadFile;
                int buffLength = ABuffSize;
                byte[] buff = new byte[ABuffSize];
                int contentLen;               

                FtpWebResponse response = (FtpWebResponse)Request.GetResponse();

                Stream responseStream = response.GetResponseStream();

                contentLen = responseStream.Read(buff, 0, buffLength);

                while (contentLen != 0)
                {
                    ADestStream.Write(buff, 0, contentLen);
                    contentLen = responseStream.Read(buff, 0, buffLength);
                }

    
                StreamReader reader = new StreamReader(ADestStream);                
                Console.WriteLine(reader.ReadToEnd());
                responseStream.Close();
                ADestStream.Close();
                return true;
            }
            else
            {
                return false;
            }
        }
        public List<string> GetFileList(string AFileExtList = "")
        {
            string [] lFileExtArray = AFileExtList.Split(';');
            string lFileName = "";
            Boolean isNeedAdd;
            CurrentFileName = "";
            DisConnect();
            List<string> lList = new List<string>();
            if (Connect())
            {
                Request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
                Response = (FtpWebResponse)Request.GetResponse();
                Stream responseStream = Response.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);
                string lReaponseDataStrind = reader.ReadToEnd();
                string[] lSeparators = { " " };
                string[] lResults;
                string[] data_s = lReaponseDataStrind.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                lReaponseDataStrind = "";
                string lFileDescription = "";
                for (int i = 0; i < data_s.Length; i++)
                {
                    lResults = data_s[i].Split(lSeparators, StringSplitOptions.RemoveEmptyEntries);
                    lFileDescription = lResults[0];

                    
                    if (lFileDescription[0] == FTPHelperConst.FTP_FILE_SIGNATURE)
                    {
                        if (lResults.Length > 7)
                        {
                            lFileName = lResults[8];
                        }
                        else
                        {
                            lFileName = "";
                        }

                        if (!String.IsNullOrWhiteSpace(AFileExtList))
                        {
                            if (!string.IsNullOrWhiteSpace(lFileName))
                            {
                                if (isStrInArraty(Path.GetExtension(lFileName), lFileExtArray))
                                {
                                    isNeedAdd = true;
                                }
                                else
                                {
                                    isNeedAdd = false;
                                }
                            }
                            else
                            {
                                isNeedAdd = false;
                            }
                        }
                        else
                        {
                            isNeedAdd = true;
                        }
                        if (isNeedAdd)
                        {
                            lList.Add(CurrentDir + lFileName);
                        }
                    }

                }
                reader.Close();
                Response.Close();                
            }
            return lList;
        }
        public Boolean DisConnect() 
        {
            if (Connected)
            {
                Request = null;
                Connected = false;
            }
            return !Connected;
        }
        public Boolean NewConnection(string APathToServer, string AUserName, string APassword)
        {
            Console.WriteLine(APathToServer);
            try
            {
              Request = (FtpWebRequest)WebRequest.Create(APathToServer);
              Request.Credentials = new NetworkCredential(AUserName, APassword);
              return true;
            }
            catch (Exception e)
            {
                return false;
            }            
        }
        public Boolean Connect()
        {
            if (!Connected)
            {
                string lPathToServer = string.Format(FTPHelperConst.PATH_TO_SERVER, ServerName, Port) 
                    + CurrentDir + CurrentFileName;                
                Connected = NewConnection(lPathToServer, UserName, Password);
            }
            return Connected;
        }
        public void LoadFromAppConfig(string AKeyFtpPathToServer = "", string AKeyFtpUserName = "", string AKeyFtpPasword = "", string AKeyFtpPort = "")
        {
            string lStr;
            if (!string.IsNullOrWhiteSpace(AKeyFtpPathToServer))
            {
                lStr = ConfigurationManager.AppSettings[AKeyFtpPathToServer];
                if (!string.IsNullOrWhiteSpace(lStr))
                {
                    ServerName = lStr;
                }
            }
            if (!string.IsNullOrWhiteSpace(AKeyFtpUserName))
            {
                lStr = ConfigurationManager.AppSettings[AKeyFtpUserName];
                if (!string.IsNullOrWhiteSpace(lStr))
                {
                    UserName = lStr;
                }
            }
            if (!string.IsNullOrWhiteSpace(AKeyFtpPasword))
            {
                lStr = ConfigurationManager.AppSettings[AKeyFtpPasword];
                if (!string.IsNullOrWhiteSpace(lStr))
                {
                    Password = lStr;
                }
            }
            if (!string.IsNullOrWhiteSpace(AKeyFtpPort))
            {
                lStr = ConfigurationManager.AppSettings[AKeyFtpPort];
                if (!string.IsNullOrWhiteSpace(lStr))
                {
                    try
                    {
                        Port = Convert.ToInt16(lStr);
                    }
                    catch (Exception e)
                    { 
                        
                    }                    
                }
            }
          
        }
        public void LoadFromAppConfig()
        { 
            LoadFromAppConfig(KeyFtpPathToServer, KeyFtpUserName, KeyFtpPasword, KeyFtpPort);
            return;
        }
        public FTPHelper()
        {
            Connected = false;
            RootDir = FTPHelperConst.DEFAULT_ROOT_DIR;
            UpLoadBuffSize = FTPHelperConst.DEFAULT_UP_LOAD_BUFF_SIZE;
            CurrentFileName = FTPHelperConst.DEFAULT_FILE_NAME;
            CurrentDir = FTPHelperConst.DEFAULT_ROOT_DIR;
            Request = null;
            KeepAlive = FTPHelperConst.DEFAULT_USE_KEEP_ALIVE;
            UseBinary = FTPHelperConst.DEFAULT_USE_BINARY;
            UsePassive = FTPHelperConst.DEFAULT_USE_PASSIVE;
            Port = FTPHelperConst.DEFAULT_PORT;
            KeyFtpPathToServer = FTPHelperConst.DEFAULT_KEY_FTP_PATH_TO_SERVER;
            KeyFtpPort = FTPHelperConst.DEFAULT_KEY_FTP_PORT;
            KeyFtpUserName = FTPHelperConst.DEFAULT_KEY_FTP_USER_NAME;
            KeyFtpPasword = FTPHelperConst.DEFAULT_KEY_FTP_PASSWORD;
        }
    }
}
