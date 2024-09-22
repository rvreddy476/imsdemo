using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace IMS.Web.Controllers
{
    public class IMSExceptionLoggerController : Controller
    {
        string filepath = AppDomain.CurrentDomain.BaseDirectory + "Logs\\IMSLoginLogs" + ".txt";

        string createTempFile(string contents)
        {
            string milliseconds = DateTime.Now.ToString();

            string[] value = milliseconds.Split(' ');
            string final_datevalue = value[0].Replace("/", "");
            var time_length = value[1].Length;
            string time_substring = null;
            string time_stampValue = null;
            if (time_length == 7)
            {
                time_substring = value[1].Substring(0, 4);
            }
            else
            {
                time_substring = value[1].Substring(0, 5);
            }
            string final_timevalue = time_substring.Replace(":", "");
            time_stampValue = "Temp_" + final_datevalue + "_" + final_timevalue + "_" + DateTime.Now.Millisecond;
            string tempfilename = time_stampValue + ".txt";
            string tempfilepath = AppDomain.CurrentDomain.BaseDirectory + "Logs\\" + tempfilename;
            using (StreamWriter sw = System.IO.File.AppendText(tempfilepath))
            {
                sw.WriteLine("");
                sw.Write("• " + DateTime.Now + ":- ");
                sw.WriteLine(contents);
            }
            IEnumerable<FileInfo> myFiles = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "Logs\\").EnumerateFiles()
                                                              .Where(fileInfo => fileInfo.Name.StartsWith("Temp_", StringComparison.OrdinalIgnoreCase) && fileInfo.Name.ToString() != tempfilename);

            if (myFiles.Count() > 0)
            {
                foreach (FileInfo f in myFiles)
                {
                    if (f.Exists)
                    {
                        f.Delete();
                    }
                }
            }

            return "true";
        }

        string contents(string empty_val)
        {


            string contents = null;
            using (StreamReader r = System.IO.File.OpenText(filepath))
            {
                contents += r.ReadToEnd();
            }
            string tempFileName = createTempFile(contents);


            FileInfo filenew = new FileInfo(filepath);
            if (filenew.Exists)
            {
                filenew.Delete();
            }
            return contents;
        }

        public string LogCreationForExceptions(string data)
        {

            string empty_val = string.Empty;
            var content1 = contents(empty_val);

            using (StreamWriter sw = System.IO.File.AppendText(filepath))
            {
                sw.WriteLine("");
                sw.Write("• " + DateTime.Now + ":- ");
                sw.Write(data);
                sw.WriteLine(content1);
            }
            return "true";
        }

        public string LogCreationForUser(string data)
        {

            string empty_val = string.Empty;
            var content1 = contents(empty_val);

            using (StreamWriter sw = System.IO.File.AppendText(filepath))
            {
                sw.WriteLine("");
                sw.Write("• " + DateTime.Now + ":- ");
                if (data == "Logged in")
                {
                    sw.Write(data);
                }
                else
                {
                    sw.Write(data);
                }
                sw.WriteLine(content1);
            }
            return "true";
        }
    }
}