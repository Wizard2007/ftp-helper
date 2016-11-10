using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using FTPHelperLib;

namespace ConsoleFtp
{
    //https://msdn.microsoft.com/ru-ru/library/system.net.ftpwebrequest%28v=vs.110%29.aspx
    //https://msdn.microsoft.com/ru-ru/library/ms229711%28v=vs.110%29.aspx
    class Program
    {
        static void Main(string[] args)
        {
            FTPHelper lFtp = new FTPHelper();
            lFtp.UsePassive = false;
            lFtp.LoadFromAppConfig();
            lFtp.ChangeDir("/");
            
            
            MemoryStream ms  = new MemoryStream();
            if (lFtp.Connected)
            {
                List<string> lList = lFtp.GetFileList(".xml");
                foreach(string i in lList)
                {
                    Console.WriteLine(i);                     
                    //lFtp.DownloadFile(@"c:\" + Path.GetFileName(i), "/", Path.GetFileName(i), 1024);
                }
            }
            else 
            {
                Console.WriteLine("FTP not Connected!");
            }         
        }
    }
}
