using IMS.Entities;
using IMS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Text.RegularExpressions;
using IMS.DAL.Infrastructure;


namespace IMS.DAL.Infrastructure
{
    public abstract class RepositoryBase<TEntity> where TEntity : class
    {
        public static UnitOfWork unitOfWork = new UnitOfWork();
        internal ServiceVMSEntities context;
        internal DbSet<TEntity> dbSet;
        internal DbSet<ServiceVMSEntities> db;

        // internal User<userentity> _myclass;
        // protected MyClass _myclass;

        public RepositoryBase(ServiceVMSEntities context)
        {
            this.context = context;
            this.dbSet = context.Set<TEntity>();
            this.db = context.Set<ServiceVMSEntities>();
            //_myclass = new MyClass();
        }

        public void Insert(IMSExceptionLogger exceptionLogger)
        {
            try
            {
                context.IMSExceptionLoggers.Add(exceptionLogger);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }

        public void DeleteGatepass(GatePass material)
        {
            try
            {

                var inverntory_from_db = from t in context.GatePasses
                                         join g in context.OutMaterials on t.GatepassID equals (g.GatepassID)
                                         where t.GatepassID == material.GatepassID
                                         select new { t.GatepassID, g.OutMat_ID, g.InventoryReg_ID };
                string gatepassstatus;
                string outmaterialstatus;
                foreach (var t in inverntory_from_db)
                {
                    gatepassstatus = t.GatepassID;

                    outmaterialstatus = t.OutMat_ID;


                    GatePass service1 = context.GatePasses.Find(gatepassstatus);
                    OutMaterial service2 = context.OutMaterials.Find(outmaterialstatus);


                    service1.GatePass_Status = "Deleted";
                    service2.Out_Status = "Deleted";

                    var inventoryRegister = context.Inventory_Register.Find(t.InventoryReg_ID);
                    if (inventoryRegister != null)
                    {
                        inventoryRegister.IR_Status = "Material In";
                    }

                }
                context.SaveChanges();
            }
            catch (Exception e)
            {
                Console.Write(e);

                var exceptionLogger = new IMSExceptionLogger()
                {
                    controllerName = "IMS_Add_GatepassController",
                    actionName = "DeleteGatepass",
                    exceptionStackTrace = e.StackTrace,
                    exceptionMessage = e.Message,
                    exceptionLogTime = DateTime.Now

                };
                Insert(exceptionLogger);

            }
        }
        public void Insert(OutwardMaterial outward)
        {
            try
            {
                context.OutwardMaterials.Add(outward);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }

        public void Insert(TempVendor tempVendor)
        {
            try
            {
                context.TempVendors.Add(tempVendor);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }

        public string getSessionValue()
        {

            string empid = null;
            empid = HttpContext.Current.Session["UserID"].ToString();        
            
            return empid;

        }

        public string auto_generatedCode(string typeofdox)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";



            var stringChars = new char[4];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            var finalString = new String(stringChars);

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
            time_stampValue = finalString + final_datevalue + final_timevalue + DateTime.Now.Millisecond;
            int time_stampValue_length = time_stampValue.Length;
            if (time_stampValue_length > 16)
            {
                time_stampValue = time_stampValue.Substring(0, 16);
            }

            if (typeofdox == "ProfessionalNumber")
            {
                typeofdox = "PNo";
            }
            else if (typeofdox == "PFNumber")
            {
                typeofdox = "pfno";
            }

            else if (typeofdox == "Inward")
            {
                typeofdox = "INW";
            }

            else if (typeofdox == "Gatepass")
            {
                typeofdox = "Gatepass";
            }

            string final_val = typeofdox + time_stampValue;
            return final_val;
        }

        public List<IMSEntity> GetDesignation()
        {
            var designation = new List<IMSEntity>();
            try
            {

                var designation_list = (from t in context.IMSUsers
                                        join r in context.IMSRoles on t.userId equals r.IMSuser_Id
                                        join d in context.ServiceUserDepartments on r.userDepartmentId equals d.userDepartmentId
                                        select new { r.RoleInIMS, t.userEmail, r.userDepartmentId, d.userDepartmentName });

                foreach (var t in designation_list)
                {
                    designation.Add(new IMSEntity()
                    {
                        RoleInIMS = t.RoleInIMS,
                        userEmail = t.userEmail,
                        userDepartmentId = t.userDepartmentId,
                        userDepartmentName = t.userDepartmentName,
                    });
                }


            }
            catch (Exception e)
            {
                Console.Write(e);

                var exceptionLogger = new IMSExceptionLogger()
                {
                    controllerName = "LoginController",
                    actionName = "Login",
                    exceptionStackTrace = e.StackTrace,
                    exceptionMessage = e.Message,
                    exceptionLogTime = DateTime.Now

                };

                Insert(exceptionLogger);
            }



            return designation;
        }

        public List<IMSEntity> GetDesignationByEmail(string uemail)
        {
            var designation = new List<IMSEntity>();
            try
            {

                var designation_list = (from t in context.IMSUsers
                                        join r in context.IMSRoles on t.userId equals r.IMSuser_Id
                                        join d in context.ServiceUserDepartments on r.userDepartmentId equals d.userDepartmentId
                                        where t.userEmail == uemail
                                        select new { r.RoleInIMS, t.userEmail, r.userDepartmentId, d.userDepartmentName, r.userLocation});

                foreach (var t in designation_list)
                {
                    designation.Add(new IMSEntity()
                    {
                        RoleInIMS = t.RoleInIMS,
                        userEmail = t.userEmail,
                        userDepartmentId = t.userDepartmentId,
                        userDepartmentName = t.userDepartmentName,
                        userLocation = t.userLocation
                    });
                }


            }
            catch (Exception e)
            {
                Console.Write(e);

                var exceptionLogger = new IMSExceptionLogger()
                {
                    controllerName = "LoginController",
                    actionName = "Login",
                    exceptionStackTrace = e.StackTrace,
                    exceptionMessage = e.Message,
                    exceptionLogTime = DateTime.Now
                };

                Insert(exceptionLogger);
            }
            return designation;
        }

        public List<IMSEntity> GetUserDepartment(ServiceUserDepartment userDepartmenttype, int userDepartmentId, string deptname)
        {
            var service_user = new List<IMSEntity>();
            IQueryable<ServiceUserDepartment> department_List = null;
            if (userDepartmentId == 0)
            {
                if (deptname == "Talent Acquisition")
                {
                    department_List = (from t in context.ServiceUserDepartments
                                       where t.userDepartmentName == "Talent Acquisition"
                                       select t);

                }
                else
                {
                    department_List = (from t in context.ServiceUserDepartments
                                       where t.userDepartmentName != "Talent Acquisition"
                                       select t);
                    var count = department_List.Count();
                }
                foreach (var t in department_List)
                {

                    service_user.Add(new IMSEntity()
                    {
                        userDepartmentId = t.userDepartmentId,
                        userDepartmentName = t.userDepartmentName
                    });
                }
            }
            else if (userDepartmentId > 0)
            {
                var department_name = (from department in context.ServiceUserDepartments
                                       where department.userDepartmentId == userDepartmentId
                                       select new { department.userDepartmentName });
                foreach (var department in department_name)
                {

                    service_user.Add(new IMSEntity()
                    {
                        userDepartmentName = department.userDepartmentName
                    });
                }
            }
            return service_user;
        }

        public List<IMSEntity> GetMaterialCategory(Material_Category material, int id)
        {
            var materials = new List<IMSEntity>();
            IQueryable<Material_Category> material_List = null;

            if (id != null)
            {

                material_List = (from t in context.Material_Category
                                 select t);
            }
            else
            {
                material_List = (from t in context.Material_Category
                                 where t.Material_CategoryID == id
                                 select t);
            }
            foreach (var t in material_List)
            {
                materials.Add(new IMSEntity()
                {
                    Material_CategoryID = t.Material_CategoryID,
                    Material_CategoryName = t.Material_CategoryName
                });
            }

            return materials;
        }

        public void Insert(InwardMaterial inward)
        {
            context.InwardMaterials.Add(inward);
        }

        public void Insert(InwardMaterial_CAPEX inward)
        {
            context.InwardMaterial_CAPEX.Add(inward);
        }

        public void Insert(InwardMaterial_OPEX inward)
        {
            context.InwardMaterial_OPEX.Add(inward);
        }

        public void Insert(InwardMaterial_Temp inward)
        {
            context.InwardMaterial_Temp.Add(inward);
        }
        public void Insert(DeliveryChallen inward)
        {
            context.DeliveryChallens.Add(inward);
        }

        public void Insert(GatePass signature)
        {
            context.GatePasses.Add(signature);
        }

        public void Insert(IMSUser user)
        {
            context.IMSUsers.Add(user);
        }

        public void Insert(IMS_Signature signature)
        {
            context.IMS_Signature.Add(signature);
        }

        public void Insert(IMSRole role)
        {
            context.IMSRoles.Add(role);
        }

        public string Update(Serial_Number serial_number, string serialname, string year_range, string location)
        {
            var categorynumList = new List<IMSEntity>();

            string categoryresult = null;

            var categorynumberUpdate = (from t in context.Serial_Number
                                        where t.Year_Range == year_range && t.Location == location
                                        select new
                                        {
                                            t.Serial_GRN_Number,
                                            t.Serial_Gatepass_Number,
                                            t.Serial_Asset_Number,
                                            t.Serial_Outward_Number
                                        });
            foreach (var t in categorynumberUpdate)
            {
                categorynumList.Add(new IMSEntity()
                {
                    Serial_GRN_Number = t.Serial_GRN_Number.ToString(),
                    Serial_Gatepass_Number = t.Serial_Gatepass_Number.ToString(),
                    Serial_Asset_Number = t.Serial_Asset_Number.ToString(),
                    Serial_Outward_Number = t.Serial_Outward_Number.ToString()
                });
            }
            if (serialname == "GRN")
            {
                categoryresult = categorynumList[0].Serial_GRN_Number;
            }
            else if (serialname == "Gatepass")
            {
                categoryresult = categorynumList[0].Serial_Gatepass_Number;
            }
            else if (serialname == "Asset")
            {
                categoryresult = categorynumList[0].Serial_Asset_Number;
            }
            else if (serialname == "Outward")
            {
                categoryresult = categorynumList[0].Serial_Outward_Number;
            }
            return categoryresult;
        }

        public List<IMSEntity> Get(HTV_Vendor vendor, string vendorId)
        {

            var modelClass = new List<IMSEntity>();
            if (vendorId == null)
            {


                string empid = getSessionValue();
                if (empid == null)
                {
                    empid = HttpContext.Current.Session["UserID"].ToString();
                }
                if (empid.Contains("DUM"))
                {
                    var Vendor_List = (from t in context.HTV_Vendor
                                       where t.registrationResult == true && (t.vendorStatus != "Blacklisted") && (t.vendorStatus != "Inactive") && t.vendorCode.Contains("DUM")
                                       orderby t.vendorName ascending
                                       select new { t.vendorId, t.vendorName, t.IsTAVendor, t.userDepartmentId });

                    var count = Vendor_List.Count();

                    foreach (var t in Vendor_List)
                    {
                        modelClass.Add(new IMSEntity()
                        {
                            vendorId = t.vendorId,
                            vendorName = t.vendorName,
                            //IsTAVendor = t.IsTAVendor,
                           // user_DeptId = t.userDepartmentId
                        });
                    }
                }

                else if (!empid.Contains("DUM"))
                {
                    var Vendor_List = (from t in context.HTV_Vendor
                                       where t.CSTNo != "Reject" && (t.vendorStatus != "Blacklisted") && (t.vendorStatus != "Inactive") && !t.vendorCode.Contains("DUM")
                                       orderby t.vendorName ascending
                                       select new { t.vendorId, t.vendorName, t.userDepartmentId }
                                        );
                    var count = Vendor_List.Count();

                    foreach (var t in Vendor_List)
                    {
                        modelClass.Add(new IMSEntity()
                        {
                            vendorId = t.vendorId,
                            vendorName = t.vendorName,
                            //user_DeptId = t.userDepartmentId
                        });
                    }



                }


            }
            else if (vendorId != null)
            {
                var Vendor_List = (from t in context.HTV_Vendor
                                   where t.vendorId == vendorId
                                   select new
                                   {
                                       t.vendorName,
                                       t.vendorId,
                                       t.vendorCode,
                                       t.legalStatus,
                                       t.businessNature,
                                       t.MSME_vendor_category,
                                       t.MsmeVendorRegistrationNo,
                                       t.poc,
                                       t.vendorAddress,
                                       t.phoneNo,
                                       t.mobileNo,
                                       t.vendorUnitAddresses,
                                       t.vendorEmail,
                                       t.beneficiaryName,
                                       t.bankName,
                                       t.branchAddress,
                                       t.accountNo,
                                       t.IFSCCode,
                                       t.PANNo,
                                       t.TANNo,
                                       t.ESICNo,
                                       t.PFNo,
                                       t.ProfessionalTaxNo,
                                       t.vendorPassword,
                                       t.GSTNo,
                                       t.IsTAVendor,
                                       t.userDepartmentId
                                   }
                                            );



                foreach (var t in Vendor_List)
                {
                    modelClass.Add(new IMSEntity()
                    {
                        vendorId = t.vendorId,
                        vendorName = t.vendorName,
                        //vendorCode = t.vendorCode,
                        //legalStatus = t.legalStatus,
                       // businessNature = t.businessNature,
                        //MsmeVendorRegistrationNo = t.MsmeVendorRegistrationNo,
                        //MSME_vendor_category = t.MSME_vendor_category,
                        //poc = t.poc,
                       // vendorAddress = t.vendorAddress,
                        //phoneNo = t.phoneNo,
                        //mobileNo = t.mobileNo,
                       // vendorEmail = t.vendorEmail,
                        //beneficiaryName = t.beneficiaryName,
                        //bankName = t.bankName,
                        //branchAddress = t.branchAddress,
                        //accountNo = t.accountNo,
                        //IFSCCode = t.IFSCCode,
                        //PANNo = t.PANNo,
                        //TANNo = t.TANNo,
                        //ESICNo = t.ESICNo,
                        //PFNo = t.PFNo,
                        //ProfessionalTaxNo = t.ProfessionalTaxNo,
                        //GSTNo = t.GSTNo,
                       // vendorPassword = t.vendorPassword,
                        //user_DeptId = t.userDepartmentId
                    });
                }
            }
            return modelClass;
        }


        /// <summary>
        /// get user list
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public List<IMSEntity> Get(IMSUser user)
        {
            var user_list = new List<IMSEntity>();
            string dept = null;            
            var httpCookie_value = HttpContext.Current.Session["DepartmentID"];
            if (httpCookie_value != null)
            {
                dept = httpCookie_value.ToString();
            }
            bool isTAUser = dept == "8"; // dept id of talent acquision

            var Approver1_List = (from t in context.IMSUsers
                                  join r in context.IMSRoles on t.userId equals r.IMSuser_Id 
                                  where r.Role_Status != "Deleted"
                                  select new { t.userId , t.userName, r.RoleInIMS, r.userDepartmentId }
                                 );

            if (isTAUser)
            {
                Approver1_List = Approver1_List.Where(d => d.userDepartmentId == 8);
            }
            else
            {
                Approver1_List = Approver1_List.Where(d => d.userDepartmentId != 8);
            }

            foreach (var t in Approver1_List)
            {

                user_list.Add(new IMSEntity()
                {
                    userId = t.userId,
                    userName = t.userName,
                    RoleInIMS = t.RoleInIMS
                });
            }
            return user_list;
        }


        public bool filename(HttpPostedFileBase Docfile, string Name, string id, string inwardid)
        {



            bool Value = false;
            if (Docfile == null)
            {
                Value = false;
            }
            else
            {
                if (Docfile.ContentLength > 0)
                {
                    var path = string.Empty;
                    Value = true;
                    string fileExtn = Path.GetExtension(Docfile.FileName);
                    string mimetype = Docfile.ContentType;
                    var filename = Docfile.FileName;
                    string vendorName = null;
                    string newFileName = id; // Without Extn.

                    switch (Name)
                    {

                        case "Delivery Challen":
                            path = Path.Combine(@"D:\IMSDocs\DeliveryChallen\" + inwardid );
                            path = path + "\\" + id;
                            break;

                        default:
                            path = Path.Combine(@"D:\IMSDocs\DeliveryChallen\" + inwardid);
                            path = path + "\\" + id;
                            break;
                    }




                    if (!Directory.Exists(path))
                    {


                        Directory.CreateDirectory(path);
                    }
                    else
                    {
                        //string desination  =  Path.Combine(path, "Deleted");
                        string[] oldfilename = Directory.GetFiles(path);
                        List<string> items = new List<string>();
                        foreach (string file1 in oldfilename.ToList())
                        {
                            //File.Copy(file1, desination + Path.GetFileName(file1));

                            if (Name != "Supportingdocx")
                            {
                                System.IO.File.Delete(file1);
                            }



                        }
                    }
                    string StrFilePathName = path + "\\" + filename;
                    if (Name == "Supportingdocx")
                    {
                        int count = 0;
                        string extension = Path.GetExtension(filename);
                        string filename_without_extension = Path.GetFileNameWithoutExtension(filename);

                        while (File.Exists(StrFilePathName))
                        {
                            count++;
                            filename = filename_without_extension + "(" + count + ")" + extension;
                            StrFilePathName = path + "\\" + filename;
                        }

                        //var path_deleted = Path.Combine(@"D:\HTV-VMSDocs\Supportingdocx\Deleted", id);
                        //string deleted_file_path = path_deleted + "\\" + filename;

                        //while (File.Exists(deleted_file_path))
                        //{
                        //    count++;
                        //    filename = filename_without_extension + "(" + count + ")" + extension;
                        //    StrFilePathName = path + "\\" + filename;
                        //}
                        //StrFilePathName = "D:\\test" + count + ".csv";

                    }

                    //StrFilePathName = path + "\\" + filename;


                    Docfile.SaveAs(StrFilePathName);
                    var filelength = Docfile.ContentLength;
                    var fileName = StrFilePathName;
                }
            }



            return Value;
        }


        public List<DeliveryChallen> Get(DeliveryChallen deliveryChallen, string inward_id)
        {
            var deliveryChallens = new List<DeliveryChallen>();
            var doc_in_db_List = (from t in context.DeliveryChallens
                                  where t.InwardID == inward_id
                                  select new { t.DC_ID, t.DC_Filesize, t.DC_Filename, t.DC_Number, t.InwardID }
                                             );

            if (doc_in_db_List.Any())
            {
                foreach (var t in doc_in_db_List)
                {
                    deliveryChallens.Add(new DeliveryChallen()
                    {
                        DC_ID = t.DC_ID,
                        DC_Number = t.DC_Number,
                        DC_Filename = t.DC_Filename,
                        DC_Filesize = t.DC_Filesize,
                        InwardID = t.InwardID,
                    });
                }
            }

            return deliveryChallens;
        }

        public List<IMSEntity> Get(ServiceUserDepartment userDepartmenttype, int userDepartmentId)
        {
            var service_user = new List<IMSEntity>();

            string dept = null;
            HttpCookie httpCookie_value = HttpContext.Current.Request.Cookies["department"];
            if (httpCookie_value != null)
            {
                dept = unitOfWork.DepartmentRepository.Decode(httpCookie_value.Value);
            }

            bool isTAUser = dept == "8"; // dept id of talent acquision

            if (userDepartmentId == 0)
            {
                var department_List = (from t in context.ServiceUserDepartments
                                       select new { t.userDepartmentId, t.userDepartmentName }
                                             );

                if (isTAUser)
                {
                    department_List = department_List.Where(d => d.userDepartmentId == 8);
                }
                else
                {
                    department_List = department_List.Where(d => d.userDepartmentId != 8);
                }

                foreach (var t in department_List)
                {

                    service_user.Add(new IMSEntity()
                    {
                        userDepartmentId = t.userDepartmentId,
                        userDepartmentName = t.userDepartmentName
                    });
                }
            }
            else if (userDepartmentId > 0)
            {
                var department_name = (from department in context.ServiceUserDepartments
                                       where department.userDepartmentId == userDepartmentId
                                       select new { department.userDepartmentName });
                foreach (var department in department_name)
                {

                    service_user.Add(new IMSEntity()
                    {
                        userDepartmentName = department.userDepartmentName
                    });
                }
            }


            return service_user;
        }


        public void Insert(OutMaterial outMaterial)
        {
            context.OutMaterials.Add(outMaterial);
        }


        public void Insert(Inventory_Register inventory)
        {
            context.Inventory_Register.Add(inventory);
        }

        public List<IMSEntity> GetMaterials(Material material, int id)
        {
            var materials = new List<IMSEntity>();
           

            if (id != 0)
            {

             var material_List = (from t in context.Materials
                                 join c in context.Material_Category on t.Material_CategoryID equals c.Material_CategoryID
                                 where t.Material_CategoryID == id && t.Material_Status != "Deleted"
                                 select new {t.MaterialID,t.MaterialName,t.MaterialPrefix,t.Material_CategoryID,c.Material_CategoryName });

                foreach (var t in material_List)
                {

                    materials.Add(new IMSEntity()
                    {
                        MaterialID = t.MaterialID,
                        MaterialName = t.MaterialName,
                        CategoryID = t.Material_CategoryID,
                        Material_CategoryName = t.Material_CategoryName,
                        MaterialPrefix = t.MaterialPrefix
                    });
                }
            }
            else
            {
                var material_List = (from t in context.Materials
                                     join c in context.Material_Category on t.Material_CategoryID equals c.Material_CategoryID
                                     where t.Material_Status != "Deleted"
                                     select new { t.MaterialID, t.MaterialName, t.MaterialPrefix, t.Material_CategoryID, c.Material_CategoryName });
                foreach (var t in material_List)
                {
                    materials.Add(new IMSEntity()
                    {
                        MaterialID = t.MaterialID,
                        MaterialName = t.MaterialName,
                        CategoryID = t.Material_CategoryID,
                        Material_CategoryName = t.Material_CategoryName,
                        MaterialPrefix = t.MaterialPrefix
                    });
                }
            }           

            return materials;
        }

        public List<IMSEntity> GetInwardMaterials(Material material, string id)
        {
            var materials = new List<IMSEntity>();


            if (id != "")
            {

                var material_List = (from t in context.InwardMaterials
                                     join c in context.InwardMaterial_Temp on t.InwardID equals c.InwardID
                                     join m in context.Materials on c.MaterialID equals m.MaterialID
                                     where t.InwardID == id
                                     select new { c.MaterialID, c.MaterialName });

                foreach (var t in material_List)
                {

                    materials.Add(new IMSEntity()
                    {
                        MaterialID = t.MaterialID,
                        MaterialName = t.MaterialName,
                    });
                }
            }
           

            return materials;
        }

        public List<IMSEntity> GetFileDetails(string id, string categoryName)
        {
            string cat_name = categoryName.Replace(" ", string.Empty);
            var fileDetails = new List<IMSEntity>();
            var path = Path.Combine(@"D:\IMSDocs\" + cat_name + "\\", id);
            string[] oldfilename = Directory.GetFiles(path);
            List<string> items = new List<string>();

            foreach (string file1 in oldfilename)
            {
                var split_file = file1.Split('\\');
                var name = split_file[split_file.Length - 1];
                fileDetails.Add(new IMSEntity()
                {
                    dummyFilename = name,
                    dummyFilesize = new System.IO.FileInfo(file1).Length,
                });
            }

            return fileDetails;
        }

        public void Delete(InwardMaterial material)
        {
            try
            {

                var inverntory_from_db = from t in context.InwardMaterials
                                         join g in context.InwardMaterial_Temp on t.InwardID equals (g.InwardID)
                                         where t.InwardID == material.InwardID
                                         select new { t.InwardID, g.InvMaterialTemp_ID };
                string InwardStatus1;
                string InwardStatus1Temp;
                foreach (var t in inverntory_from_db)
                {
                    InwardStatus1 = t.InwardID;

                    InwardStatus1Temp = t.InvMaterialTemp_ID;


                    InwardMaterial service1 = context.InwardMaterials.Find(InwardStatus1);
                    InwardMaterial_Temp service2 = context.InwardMaterial_Temp.Find(InwardStatus1Temp);


                    service1.Inward_Status = "Deleted";
                    service2.Status = "Deleted";

                }
                context.SaveChanges();
            }
            catch (Exception e)
            {
                Console.Write(e);

                var exceptionLogger = new IMSExceptionLogger()
                {
                    controllerName = "IMS_Add_InwardMaterialController",
                    actionName = "DeleteInward",
                    exceptionStackTrace = e.StackTrace,
                    exceptionMessage = e.Message,
                    exceptionLogTime = DateTime.Now

                };
                Insert(exceptionLogger);

            }
        }

        public void DeletedFiles(string Name, string id, string InwardId)
        {
            try
            {
                string path = string.Empty;
                string desination = string.Empty;
                //In Invoices folder create deleted_invoice Folder and inside it store all invoices inside its ids
                if (Name == "DeliveryChallen")
                {                  
                    path = Path.Combine(@"D:\IMSDocs\DeliveryChallen\" + InwardId + "\\", id);
                    desination = Path.Combine(@"D:\IMSDocs\DeliveryChallen\", "Deleted");
                }

                if(Name == "InwardMaterial")
                {
                    path = Path.Combine(@"D:\IMSDocs\InwardMaterial\", InwardId);
                    desination = Path.Combine(@"D:\IMSDocs\InwardMaterial\", "Deleted");
                }
                if (!Directory.Exists(desination))
                {
                    Directory.CreateDirectory(desination);
                }
                if (Directory.Exists(path))
                {
                    string destinationpath = string.Empty;
                    string[] oldfilename = Directory.GetFiles(path);
                    if(Name == "InwardMaterial")
                    {
                         destinationpath = desination + "\\" + InwardId;
                    }
                    else if(Name == "DeliveryChallen")
                    {
                        destinationpath = desination + "\\" + InwardId + "\\" + id;
                    }
                   
                    if (!Directory.Exists(destinationpath))
                    {
                        Directory.CreateDirectory(destinationpath);
                    }

                    int count = 0;
                    foreach (string file1 in oldfilename.ToList())
                    {
                        string extension = Path.GetExtension(file1);
                        string filename_without_extension = Path.GetFileNameWithoutExtension(file1);
                        string destination_file_path = destinationpath + "\\" + Path.GetFileName(file1);

                        if (Name != "Supportingdocx")
                        {

                            File.Copy(file1, destinationpath + "\\" + Path.GetFileName(file1));

                        }


                        if (Name != "Supportingdocx")
                        {
                            System.IO.File.Delete(file1);
                        }

                    }


                    var content = Directory.GetFiles(path);

                    if (content.Length == 0)
                    {
                        if (Directory.Exists(path))
                        {
                            Directory.Delete(path);
                        }
                    }
                }

            }
            catch (Exception e)
            {

            }
        }
        public string GenerateUniqueCode()
        {

            string guidPart = Guid.NewGuid().ToString("N").Substring(0, 7);


            long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;


            string uniqueCode = guidPart + milliseconds.ToString();

            return uniqueCode;
        }

        public string GenerateInwardIDS(string type)
        {
            DateTime now = DateTime.Now;
            string code = string.Empty;
            string timestamp = now.ToString("yyyyMMddHHmmssfff"); // Format: YearMonthDayHourMinuteSecondMillisecond
            Random random = new Random();
            string guidPart = Guid.NewGuid().ToString("N").Substring(0, 3);
            int randomNumber = random.Next(1000); // Generates a random number between 0 and 999
            if (type == "InwardTemp")
            {
                code = $"INWDTEMP-{timestamp}-{randomNumber:D3}-{guidPart}";
            }
            else if (type == "Inwardopex")
            {
                code = $"INWDOPEX-{timestamp}-{randomNumber:D3}-{guidPart}";
            }
            else if (type == "Inwardcapex")
            {
                code = $"INWDCAPEX-{timestamp}-{randomNumber:D3}-{guidPart}";
            }
            else if (type == "Inwardcustasset")
            {
                code = $"INWDCA-{timestamp}-{randomNumber:D3}-{guidPart}";
            }
            else if (type == "Inwardrental")
            {
                code = $"INWDRENTAL-{timestamp}-{randomNumber:D3}-{guidPart}";
            }
            else if (type == "Deliverychallen")
            {
                code = $"DC-{timestamp}-{randomNumber:D3}-{guidPart}";
            }
            else if (type == "InventoryRegister")
            {
                code = $"IR-{timestamp}-{randomNumber:D3}-{guidPart}";
            }
            else if (type == "OutwardMaterial")
            {
                code = $"OUT-{timestamp}-{randomNumber:D3}-{guidPart}";
            }
            return code;
        }

        public string Encode(string encodeMe)
        {
            byte[] encoded = System.Text.Encoding.UTF8.GetBytes(encodeMe);
            return Convert.ToBase64String(encoded);
        }

        public string Decode(string decodeMe)
        {
            byte[] encoded = Convert.FromBase64String(decodeMe);
            return System.Text.Encoding.UTF8.GetString(encoded);
        }

        public string auto_generatedCode()
        {

            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            var stringChars = new char[4];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            var finalString = new String(stringChars);

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
            time_stampValue = finalString + final_datevalue + final_timevalue + DateTime.Now.Millisecond;
            int time_stampValue_length = time_stampValue.Length;
            if (time_stampValue_length > 16)
            {
                time_stampValue = time_stampValue.Substring(0, 16);
            }

            return time_stampValue;
        }
      

        public string generateToken(string employeeid)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";



            var stringChars = new char[4];
            var random = new Random();



            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }



            var finalString = new String(stringChars);



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
            time_stampValue = finalString + final_datevalue + final_timevalue + DateTime.Now.Millisecond;
            int time_stampValue_length = time_stampValue.Length;
            if (time_stampValue_length > 16)
            {
                time_stampValue = time_stampValue.Substring(0, 16);
            }

            string final_val = "Auth_" + time_stampValue + "_" + employeeid;
            return final_val;
        }

        public string GetUserEmail(IMSUser user, string id)
        {
            string useremail = null;
            var Approvers_List = (from t in context.IMSUsers
                                  where t.userId == id
                                  select new { t.userEmail }
                                 );
            foreach (var t in Approvers_List)
            {
                useremail = t.userEmail;
            }
            return useremail;
        }

        public virtual List<ServiceUserDepartment> GetServiceDepartments()
        {
            return new List<ServiceUserDepartment>();
        }
    }
}
