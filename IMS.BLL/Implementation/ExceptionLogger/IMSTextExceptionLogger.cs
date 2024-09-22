using AutoMapper;
using IMS.BLL.Interfaces;
using IMS.DAL;
using IMS.Entities;
using IMS.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.BLL.Implementation.ExceptionLogger
{
    public class IMSTextExceptionLogger : IMExceptionLogger
    {

        private UnitOfWork unitOfWork = new UnitOfWork();
        string filepath = AppDomain.CurrentDomain.BaseDirectory + "Logs\\IMSLog.txt";
        private ServiceVMSEntities context = new ServiceVMSEntities();
        public object HttpContext { get; private set; }

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

       
        public string getSessionValue()
        {
            string empid = unitOfWork.DepartmentRepository.getSessionValue();          
            string userName = null;
            var name = from t in context.IMSUsers
                       where t.userId == empid 
                       select new { t.userName };
            foreach (var i in name)
            {
                userName = i.userName;
            }
            return userName;



        }
        public string LogCreationForUser(IMS_Users userTableDto, string data)
        {
            string empty_val = string.Empty;
            var content1 = contents(empty_val);
            var name = getSessionValue();

            using (StreamWriter sw = System.IO.File.AppendText(filepath))
            {
                sw.WriteLine("");
                sw.Write("• " + DateTime.Now + ":- ");
                if (data == "Logged in")
                {
                    sw.Write(name + " has " + data);
                }
                else
                {
                    sw.Write(name + " has " + data);
                }


                sw.WriteLine(content1);
            }
            return "true";
        }

        public void LogException(ExceptionModel exceptionDetails)
        {
            throw new NotImplementedException();
        }

        public string LogCreationForInward(string ID, string data,IMSEntity model,IMSEntity editdata)
        {
            string empty_val = string.Empty;
            string data_write = string.Empty;
            var content2 = contents(empty_val);


            var name = getSessionValue();


            if (data == "added")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has added Inward " + ID;

            }
            else if (data == "edited")
            {

                var diffs = ReflectionClass.SimpleComparer.Differences<IMSEntity>(model, editdata);

                foreach (var diff in diffs)
                {
                    if (diff.Item1 != "SupportFile")
                        data_write = data_write + Environment.NewLine + "• " + DateTime.Now + ":- " + name + " has changed " + diff.Item1 + " from " + diff.Item3 + " to " + diff.Item2 + " having InwardID " + model.InwardID;
                }

            }
            else if (data == "Deleted")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has deleted Inward " +ID;

            }


            using (StreamWriter sw = System.IO.File.AppendText(filepath))
            {
                sw.WriteLine("");
                sw.Write(data_write);

                sw.WriteLine(content2);
            }

            return "true";
        }

        public string LogCreationForInwardStatus(string id, string data,string materialID)
        {
            string empty_val = string.Empty;
            string data_write = string.Empty;
            var content2 = contents(empty_val);

            var name = getSessionValue();

            if (data == "change status opex")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has change Inward status opex having InwardID is"  + id;

            }
            else if (data == "change status capex")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has change Inward status capex having InwardID is" + id;
            }
            else if (data == "change status rental")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has change Inward status opex having InwardID is" + id;

            }
            else if (data == "change status customerasset")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has change Inward status customerasset having InwardID is" + id;

            }
            else if (data == "Material Accepted OPEX")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has change status Material Accepted OPEX having MaterialID is " + materialID + "and InwardID is" + id;
            }
            else if (data == "Material Accepted customerasset")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has change status Material Accepted customerasset having MaterialID is " + materialID + "and InwardID is" + id;
            }
            else if (data == "Material Rejected OPEX")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has change status Material Rejected OPEX having MaterialID is " + materialID + "and InwardID is" + id;
            }
            else if (data == "Material Pending CAPEX")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has change status Material Accepted OPEX having MaterialID is " + materialID + "and InwardID is" + id;
            }
            else if (data == "Material Pending customerasset")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has change status Material Accepted customerasset having MaterialID is " + materialID + "and InwardID is" + id;
            }
            else if (data == "Material Rejected CAPEX")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has change status Material Rejected OPEX having MaterialID is " + materialID + "and InwardID is" + id;
            }
            else if (data == "Material Pending rental")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has change status Material Accepted OPEX having MaterialID is " + materialID + "and InwardID is" + id;
            }
            else if (data == "Material Rejected rental")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has change status Material Rejected OPEX having MaterialID is " + materialID + "and InwardID is" + id;
            }
            else if (data == "Material Rejected customerasset")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has change status Material Rejected customerasset having MaterialID is " + materialID + "and InwardID is" + id;
            }

            else if (data == "Change Inward Accepted status for capex")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has Change Inward Pending status change to Inward Accepted for capex" + id;
            }
            else if (data == "Change Inward Accepted status for rental")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has Change Inward Pending status change to Inward Accepted for rental" + id;
            }
            else if (data == "Change Inward Accepted status for customerasset")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has Change Inward Pending status change to Inward Accepted for customerasset" + id;
            }




            using (StreamWriter sw = System.IO.File.AppendText(filepath))
            {
                sw.WriteLine("");
                sw.Write(data_write);

                sw.WriteLine(content2);
            }

            return "true";
        }

        public string LogCreationForInwardEdit(string model, string data)
        {
            string empty_val = string.Empty;
            string data_write = string.Empty;
            var content2 = contents(empty_val);


            var name = getSessionValue();

            data_write = model + "" + data;


            using (StreamWriter sw = System.IO.File.AppendText(filepath))
            {
                sw.WriteLine("");
                sw.Write(data_write);
                sw.WriteLine(content2);
            }

            return "true";
        }

        public string LogCreationForCategoryFeature(Material_Category model, string data, Material_Category original, string cat_name)
        {
            string username = getSessionValue();
            string data_write = string.Empty;
            string empty_val = string.Empty;
            if (data == "Create Category")
            {
                data_write = username + " has added a category - " + model.Material_CategoryName;
            }
            else if (data == "Update Category")
            {
                var diffs = ReflectionClass.SimpleComparer.Differences<Material_Category>(model, original);

                foreach (var diff in diffs)
                {
                    //Console.WriteLine("'{0}' changed from {1} to {2}", diff.Item1, diff.Item3, diff.Item2);
                    data_write = username + "has changed " + diff.Item1 + " from " + diff.Item3 + " to " + diff.Item2;
                }
            }
            else if (data == "delete Category")
            {
                data_write = username + " has deleted a category - " + cat_name;

            }



            var content1 = contents(empty_val);
            using (StreamWriter sw = System.IO.File.AppendText(filepath))
            {
                sw.WriteLine("");
                sw.Write("• " + DateTime.Now + ":- " + data_write);
                //sw.Write(DMName + " has uploaded a SOW " + ckEditor.proposalName + " for project " + pname + " \t");
                sw.WriteLine(content1);
            }
            return "true";
        }

        public string LogCreationForMaterialFeature(Material model, string data, Material original, string mat_name)
        {
            string username = getSessionValue();
            string data_write = string.Empty;
            string empty_val = string.Empty;
            if (data == "Create Material")
            {
                data_write = username + " has added a material - " + model.MaterialName;
            }
            else if (data == "Update Material")
            {
                var diffs = ReflectionClass.SimpleComparer.Differences<Material>(model, original);

                foreach (var diff in diffs)
                {                   
                    data_write = username + "has changed " + diff.Item1 + " from " + diff.Item3 + " to " + diff.Item2;
                }
            }
            else if (data == "Delete Material")
            {
                data_write = username + " has deleted a material - " + mat_name;

            }
            var content1 = contents(empty_val);
            using (StreamWriter sw = System.IO.File.AppendText(filepath))
            {
                sw.WriteLine("");
                sw.Write("• " + DateTime.Now + ":- " + data_write);                
                sw.WriteLine(content1);
            }
            return "true";
        }

        public string LogCreationForUserCreation(IMSEntity user, string data, IMSEntity original)
        {
            string empty_val = string.Empty;
            string data_write = string.Empty;
            var content1 = contents(empty_val);


            var username = getSessionValue();

            if (data == "add")
            {
                data_write = username + " has added user " + user.userId + " as name " + user.userName;

            }
            else if (data == "edit")
            {

                var diffs = ReflectionClass.SimpleComparer.Differences<IMSEntity>(user, original);

                foreach (var diff in diffs)
                {
                    Console.WriteLine("'{0}' changed from {1} to {2}", diff.Item1, diff.Item3, diff.Item2);
                    data_write = data_write + Environment.NewLine + "• " + DateTime.Now + ":- " + username + "has changed user data of " + original.vendorId;
                }

            }
            else if (data == "delete")
            {
                data_write = username + " has deleted user " + user.userId;

            }

            using (StreamWriter sw = System.IO.File.AppendText(filepath))
            {
                sw.WriteLine("");
                sw.Write("• " + DateTime.Now + ":- ");
                //sw.Write(DMName + " has uploaded a SOW " + ckEditor.proposalName + " for project " + pname + " \t");
                sw.WriteLine(content1);
            }
            return "true";
        }

        public string LogCreationForGatepass(string ID, string data, IMSEntity model, IMSEntity editdata)
        {
            string empty_val = string.Empty;
            string data_write = string.Empty;
            var content2 = contents(empty_val);


            var name = getSessionValue();


            if (data == "added")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has added Gatepass " + ID;

            }
            else if (data == "edited")
            {

                var diffs = ReflectionClass.SimpleComparer.Differences<IMSEntity>(model, editdata);

                foreach (var diff in diffs)
                {                 
                 data_write = data_write + Environment.NewLine + "• " + DateTime.Now + ":- " + name + " has changed " + diff.Item1 + " from " + diff.Item3 + " to " + diff.Item2 + " having InwardID " + model.InwardID;
                }

            }
            else if (data == "Deleted")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has deleted Gatepass " + ID;

            }
            using (StreamWriter sw = System.IO.File.AppendText(filepath))
            {
                sw.WriteLine("");
                sw.Write(data_write);
                sw.WriteLine(content2);
            }
            return "true";
        }

        public string LogCreationForGatepassStatus(string ID, string data)
        {
            string empty_val = string.Empty;
            string data_write = string.Empty;
            var content2 = contents(empty_val);


            var name = getSessionValue();


            if (data == "change status Approved")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has change Gatepass status Approved having GatepassID is" + ID;

            }
           else if (data == "change status Rejected")
            {
                data_write = "• " + DateTime.Now + ":- " + name + " has change Gatepass status Rejected having GatepassID is" + ID;

            }
            using (StreamWriter sw = System.IO.File.AppendText(filepath))
            {
                sw.WriteLine("");
                sw.Write(data_write);
                sw.WriteLine(content2);
            }
            return "true";
        }


    }

}     
