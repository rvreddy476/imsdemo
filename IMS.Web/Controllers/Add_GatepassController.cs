using IMS.DAL;
using IMS.Entities;
using IMS.Models;
using iTextSharp.text;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using PagedList;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using IMS.BLL;
using IMS.BLL.Interfaces;
using System.DirectoryServices.Protocols;
using System.Net;

namespace IMS.Web.Controllers
{
    public class Add_GatepassController : Controller
    {
        // GET: Add_Gatepass
        private ServiceVMSEntities context = new ServiceVMSEntities();
        private UnitOfWork unitOfWork = new UnitOfWork();
        private IMExceptionLogger exceptionLogger = BLLObjectCreator.CreateIMSLogger(ExceptionLoggerType.IMSText);
        IMSExceptionLoggerController logger = new IMSExceptionLoggerController();
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Create_Gatepass()
        {
            string empid = string.Empty;
           
            if (Session["UserID"] != null)
            {
                empid = Session["UserID"].ToString();
            }
            var userDepartmenttype = new ServiceUserDepartment();
            var material = new Material_Category();
            var vendor = new HTV_Vendor();
            var user = new IMSUser();
            ViewBag.userDepartment = unitOfWork.DepartmentRepository.GetUserDepartment(userDepartmenttype, 0, null);
            ViewBag.materialcategory = unitOfWork.DepartmentRepository.GetMaterialCategory(material, 0);
            ViewBag.GatepassID = unitOfWork.DepartmentRepository.auto_generatedCode("Gatepass");
            ViewBag.vendorList = unitOfWork.DepartmentRepository.Get(vendor, null);
            ViewBag.userList = unitOfWork.DepartmentRepository.Get(user);
            var list = Getinventorymaterials();
            ViewBag.Searchmaterials = list;
            ViewBag.role = Session["role"].ToString();
            var section = string.Empty;
            if (Session["Location"] != null)
            {
                ViewBag.location = Session["Location"].ToString();
                if (ViewBag.location == "Global Port")
                {
                    section = "Global Port";
                }
                else if (ViewBag.location == "Siddhant")
                {
                    section = "Siddhant";
                }
                else if (ViewBag.location == "SEZ")
                {
                    section = "SEZ";
                }

            }
            var serial_number = new Serial_Number();

            string currentdatetime = DateTime.Now.ToShortDateString();
            currentdatetime = currentdatetime.Replace("-", "");

            string year_range = string.Empty;
            int currentyear = 0;
            int currentmonth = DateTime.Now.Month;
            int firsthalfyear = 0;
            if (currentmonth <= 3)
            {
                year_range = "2023-24";
                currentyear = DateTime.Now.Year - 1;
                firsthalfyear = DateTime.Now.Year;
            }
            else if (currentmonth > 3)
            {
                year_range = "2024-25";
                currentyear = DateTime.Now.Year;
                firsthalfyear = DateTime.Now.Year + 1;
            }
            var categoryNumber = unitOfWork.DepartmentRepository.Update(serial_number, "Gatepass", year_range, section);

            string finalyear = firsthalfyear.ToString().Substring(2, 2);

            var rem = 0;
            var suffixpo = string.Empty;
            int num = int.Parse(categoryNumber);
            var finalnumber = num + 1;
            int count = 0;
            while (num != 0)
            {
                rem = num % 10;
                count++;
                num = num / 10;
            }
            switch (count)
            {
                case 1:
                    suffixpo = "00";
                    break;
                case 2:
                    suffixpo = "0";
                    break;
                case 3:
                    suffixpo = "";
                    break;
                case 0:
                    suffixpo = "00";
                    break;
            }

            if (empid.Contains("DUM"))
            {
                ViewBag.gatepassnumber = "DUMMYGRN";
                ViewBag.finalnumber = finalnumber - 1;
            }
            else
            {
                ViewBag.gatepassnumber = section + "/" + year_range + "/" + suffixpo + finalnumber;
                TempData["myData"] = section + "/" + suffixpo + finalnumber;
                ViewBag.finalnumber = finalnumber;
            }

            return View();
        }

        public JsonResult getVendorreceiverdetails()
        {
            var vendor = new HTV_Vendor();
            var vendorList = unitOfWork.DepartmentRepository.Get(vendor, null);
            return Json(vendorList, JsonRequestBehavior.AllowGet);

        }

        public JsonResult getUserdetails()
        {
            var user_list = new List<IMSEntity>();            
            var Approver1_List = (from t in context.IMSUsers                                
                                  where t.userStatus != "Deleted"
                                  select new { t.userId, t.userName });

           

            foreach (var t in Approver1_List)
            {

                user_list.Add(new IMSEntity()
                {
                    userId = t.userId,
                    userName = t.userName,                  
                });
            }
           
            return Json(user_list, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Create_Gatepass(IMSEntity model)
        {
            string role = string.Empty;
            string empid = string.Empty;
            if(Session["role"].ToString() != null)
            {
                role = Session["role"].ToString();
            }
            
            if (Session["UserID"].ToString() != null)
            {
                empid = Session["UserID"].ToString();
            }

            try
            {


                int j = 0;
                List<string> remarklist = null;

                List<string> inventory_id = new List<string>(model.inventoryregid);

                List<string> expenselist = new List<string>(model.businessnature);

                if (model.materialremarks != null)
                {
                    remarklist = new List<string>(model.materialremarks);
                }


                string loc = string.Empty;
                if (Session["Location"].ToString() == "Global Port")
                {
                    loc = "Global Port";
                }
                else if (Session["Location"].ToString() == "SEZ")
                {
                    loc = "SEZ";
                }
                else if (Session["Location"].ToString() == "Siddhant")
                {
                    loc = "Siddhant";
                }

                if (model.ReceiverID == null && model.From_Office != null && model.TO_Office != null && model.GatePass_Category == "InterOffice")
                {
                    var receiveremail = model.ReceiverEmail;
                    var tempuser = (from t in context.TempVendors
                                      where t.TempVendorStatus != "Deleted" && t.TempVendorEmail == receiveremail
                                      select new
                                      {
                                          t.TempVendorEmail,
                                          t.TempVendorID,
                                          t.TempVendorName,
                                          t.TempVendorContact,
                                          t.TempVendorAddress
                                      }).FirstOrDefault();

                    if (tempuser != null)
                    {
                        if (tempuser.TempVendorEmail.ToLower() == receiveremail.ToLower())
                        {
                            model.ReceiverID = tempuser.TempVendorID.ToString();
                            model.ReceiverName = tempuser.TempVendorName.ToString();
                            model.ReceiverContact = tempuser.TempVendorContact.ToString();
                            model.ReceiverAddress = tempuser.TempVendorAddress.ToString();
                        }
                    }
                    else
                    {
                        TempVendor vendor = new TempVendor();
                        var vendorid = unitOfWork.DepartmentRepository.auto_generatedCode();
                        vendor.TempVendorID = vendorid;
                        vendor.TempVendorName = model.ReceiverName;
                        vendor.TempVendorEmail = model.ReceiverEmail;
                        vendor.TempVendorContact = model.ReceiverContact;
                        vendor.TempVendorAddress = model.ReceiverAddress;
                        vendor.TempCategory = "User";
                        unitOfWork.DepartmentRepository.Insert(vendor);
                        unitOfWork.Save();

                        model.ReceiverID = vendorid;
                    }

                }
                else if (model.ReceiverID != null && model.GatePass_Category == "InterOffice")
                {
                    var vendor = (from t in context.IMSUsers
                                  where t.userId == model.ReceiverID
                                  select new
                                  {
                                      t.userEmail,
                                      t.userId,
                                      t.userName,                                      
                                  }).FirstOrDefault();

                    model.ReceiverName = vendor.userName;
                    model.ReceiverAddress = model.ReceiverAddress;
                    model.ReceiverContact = model.ReceiverContact;
                    model.ReceiverEmail = vendor.userEmail;
                }

                if (model.ReceiverID == null && model.From_Office == null && model.TO_Office == null && model.GatePass_Category == "External")
                {
                    var receiveremail = model.ReceiverEmail;
                    var tempvendor = (from t in context.TempVendors
                                      where t.TempVendorStatus != "Deleted" && t.TempVendorEmail == receiveremail
                                      select new { t.TempVendorEmail,
                                                   t.TempVendorID,
                                                   t.TempVendorName,
                                                   t.TempVendorContact,
                                                   t.TempVendorAddress
                                                 }).FirstOrDefault();
                     
                    if (tempvendor != null)
                    {
                        if (tempvendor.TempVendorEmail.ToLower() == receiveremail.ToLower())
                        {
                            model.ReceiverID = tempvendor.TempVendorID.ToString();
                            model.ReceiverName = tempvendor.TempVendorName.ToString();
                            model.ReceiverContact = tempvendor.TempVendorContact.ToString();
                            model.ReceiverAddress = tempvendor.TempVendorAddress.ToString();
                        }
                    }
                    else
                    {
                        TempVendor vendor = new TempVendor();
                        var vendorid = unitOfWork.DepartmentRepository.auto_generatedCode();
                        vendor.TempVendorID = vendorid;
                        vendor.TempVendorName = model.ReceiverName;
                        vendor.TempVendorEmail = model.ReceiverEmail;
                        vendor.TempVendorContact = model.ReceiverContact;
                        vendor.TempVendorAddress = model.ReceiverAddress;
                        vendor.TempCategory = "Vendor";
                        unitOfWork.DepartmentRepository.Insert(vendor);
                        unitOfWork.Save(); 

                        model.ReceiverID = vendorid;
                    }
                }
                else if(model.ReceiverID != null && model.GatePass_Category == "External")
                {
                    var vendor = (from t in context.HTV_Vendor
                                      where t.vendorId == model.ReceiverID
                                      select new
                                      {
                                          t.vendorEmail,
                                          t.vendorId,
                                          t.vendorName,
                                          t.phoneNo,
                                          t.vendorAddress
                                      }).FirstOrDefault();

                    model.ReceiverName = vendor.vendorName;
                    model.ReceiverAddress = vendor.vendorAddress;
                    model.ReceiverContact = vendor.phoneNo;
                    model.ReceiverEmail = vendor.vendorEmail;
                }
                GatePass gatePass = new GatePass();
                gatePass.GatepassID = model.GatepassID;
                gatePass.Location = loc;
                gatePass.userDepartmentId = model.userDepartmentId;
                gatePass.GatepassType = model.GatepassType;
                if (model.Bonded_Item == "STP")
                {
                    gatePass.STP_Bonded_Item = true;
                }
                if (model.Bonded_Item == "SEZ")
                {
                    gatePass.SEZ_Bonded_Item = true;
                }
                gatePass.GatePass_Status = "Pending for approver";
                gatePass.Gatepass_DateTime = DateTime.Now;
                gatePass.Created_By = empid;
                gatePass.GatePass_Category = model.GatePass_Category;
                gatePass.SenderID = empid;
                gatePass.Reason = model.Reason;
                gatePass.Gatepass_Number = model.Gatepass_Number;
                if (model.GatePass_Category == "External")
                {
                    var vendorname = (from v in context.HTV_Vendor
                                      where v.vendorId == model.ReceiverID
                                      select new { v.vendorName }).FirstOrDefault();
                    if(vendorname == null)
                    {
                        var yempvendorname = (from v in context.TempVendors
                                          where v.TempVendorID == model.ReceiverID
                                          select new { v.TempVendorName }).FirstOrDefault();
                        model.ReceiverName = yempvendorname.TempVendorName.ToString();
                    }
                    else
                    {
                        model.ReceiverName = vendorname.vendorName.ToString();
                    }
                    
                    gatePass.ReceiverID = model.ReceiverID;
                    gatePass.ReceiverName = model.ReceiverName;
                    gatePass.ReceiverAddress = model.ReceiverAddress;
                    gatePass.ReceiverContact = model.ReceiverContact;
                    gatePass.ReceiverEmail = model.ReceiverEmail;
                }
                else if (model.GatePass_Category == "InterOffice")
                {
                    gatePass.TO_Office = model.TO_Office;
                    gatePass.From_Office = model.From_Office;

                    var vendorname = (from v in context.IMSUsers
                                      where v.userId == model.ReceiverID
                                      select new { v.userName }).FirstOrDefault();
                    if (vendorname == null)
                    {
                        var yempvendorname = (from v in context.TempVendors
                                              where v.TempVendorID == model.ReceiverID
                                              select new { v.TempVendorName }).FirstOrDefault();
                        model.ReceiverName = yempvendorname.TempVendorName.ToString();
                    }
                    else {           
                        model.ReceiverName = vendorname.userName.ToString();
                    }

                    gatePass.ReceiverID = model.ReceiverID;
                    gatePass.ReceiverName = model.ReceiverName;
                    gatePass.ReceiverAddress = model.ReceiverAddress;
                    gatePass.ReceiverContact = model.ReceiverContact;
                    gatePass.ReceiverEmail = model.ReceiverEmail;
                }
                if (model.Expected_DateofReturn != null)
                {
                    gatePass.Expected_DateofReturn = model.Expected_DateofReturn;
                }
                unitOfWork.DepartmentRepository.Insert(gatePass);
                unitOfWork.Save();

                for (int i = 0; i < model.SerialNumber; i++)
                {
                    j = i;
                    if (i != model.SerialNumber)
                    {
                        var ids = inventory_id[j].ToString();

                        var materialdetails = (from ii in context.Inventory_Register
                                               where ii.InventoryReg_ID == ids
                                               select new { ii.MaterialID, ii.MaterialName, ii.Material_CategoryID, ii.InwardID, ii.Serial_No, ii.Model_Number, ii.IR_Status }).FirstOrDefault();

                     
                        OutMaterial outMaterial = new OutMaterial();
                        outMaterial.OutMat_ID = unitOfWork.DepartmentRepository.GenerateInwardIDS("OutwardMaterial");
                        outMaterial.GatepassID = model.GatepassID;
                        outMaterial.MaterialID = materialdetails.MaterialID;
                        outMaterial.MaterialName = materialdetails.MaterialName;
                        outMaterial.Quantity = 1;
                        outMaterial.Material_CategoryID = materialdetails.Material_CategoryID.ToString();
                        outMaterial.Serial_Number = materialdetails.Serial_No;
                        outMaterial.Model_Number = materialdetails.Model_Number;
                        outMaterial.InventoryReg_ID = inventory_id[j];
                        outMaterial.InwardID = materialdetails.InwardID;
                        outMaterial.MaterialDispatchDate = DateTime.Now;

                        if (remarklist[j] != null)
                        {
                            outMaterial.Remarks = remarklist[j];
                        }

                        outMaterial.Out_Status = "Material Out";
                        unitOfWork.DepartmentRepository.Insert(outMaterial);
                        unitOfWork.Save();

                        var inventorydetails = (from ii in context.Inventory_Register
                                               where ii.InventoryReg_ID == ids
                                               select ii).FirstOrDefault();

                        inventorydetails.IR_Status = "Material Out";
                        context.Entry(inventorydetails).State = EntityState.Modified;
                        context.SaveChanges();
                    }
                }

                string year_range = string.Empty;
                int start_date_month = DateTime.Now.Month;
                string start_date_year = DateTime.Now.Year.ToString();

                if ((start_date_month > 3 && start_date_year == "2024") || (start_date_month < 3) && start_date_year == "2025")
                {
                    year_range = "2024-25";
                }
                else
                {
                    year_range = "2023-24";
                }
                var CategoryUpdate = (from t in context.Serial_Number
                                      where t.Year_Range == year_range && t.Location == loc
                                      select t).SingleOrDefault();
                CategoryUpdate.Serial_Gatepass_Number = int.Parse(model.finalnumberforgatepass.ToString());
                context.SaveChanges();
                var sharedData = TempData["myData"] as string;
                bool file_Value = GatepassPDF(model, model.ReceiverAddress, loc, sharedData, empid);

                if(file_Value == true)
                {
                    var details_GP = unitOfWork.DepartmentRepository.GetFileDetails(model.GatepassID, "Gatepasses");
                    var GP_dataUpdate = (from t in context.GatePasses
                                         where t.GatepassID == model.GatepassID
                                         select t).SingleOrDefault();
                    GP_dataUpdate.GatepassFileName = details_GP[0].dummyFilename;
                    GP_dataUpdate.GatepassFileSize = details_GP[0].dummyFilesize.ToString();
                    context.SaveChanges();
                }

                bool sendmail = sendGatepassMail(model.GatepassID,"Create Gatepass");
                exceptionLogger.LogCreationForGatepass(model.GatepassID, "added", null, null);

            }
            catch (Exception e)
            {
                var exceptionLogger = new IMSExceptionLogger()
                {
                    controllerName = "Add_GatepassController",
                    actionName = "Create_Gatepass(post)",
                    exceptionStackTrace = e.StackTrace,
                    exceptionMessage = e.Message,
                    exceptionLogTime = DateTime.Now
                };

                unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                unitOfWork.Save();
            }
            return RedirectToAction("GatepassList", "Add_Gatepass");
        }


        public bool sendGatepassMail(string gatepassID,string status)
        {
            try
            {
                var GPdetails = (from i in context.GatePasses                                    
                                     where i.GatepassID == gatepassID
                                     select new {i.GatepassRejectReason, i.Location, i.ReceiverEmail ,i.userDepartmentId,i.SenderID ,i.Gatepass_Number,i.GatepassID,i.GatepassFileName,i.Gatepass_InvInchargeFileName,i.Gatepass_SecurityGuardFileName}).FirstOrDefault();

                var Approverdetails = (from a in context.GatepassApprovers
                                       where a.Location == GPdetails.Location && a.userDepartmentId == GPdetails.userDepartmentId && a.RoleInIMS == "Inventory Incharge"
                                       select new { a.userEmail });

                var senderdetails = (from i in context.IMSUsers
                                       where i.userId == GPdetails.SenderID
                                       select new { i.userEmail,i.userName }).FirstOrDefault();


                string Body = string.Empty;
                string sub = string.Empty;
                MailMessage mail = new MailMessage();
                string username = ConfigurationManager.AppSettings["SmtpUserId"].ToString();
                var materialhtml = string.Empty;
               
                if (status == "Create Gatepass")
                {
                    foreach (var m in Approverdetails)
                    {
                        mail.To.Add(m.userEmail.ToString());
                    }
                   

                    DirectoryInfo dir = new DirectoryInfo(@"D:\IMSDocs\Gatepasses\" + GPdetails.GatepassID);
                    foreach (FileInfo file2 in dir.GetFiles("*.*"))
                    {
                        if (file2.Exists)
                        {
                            mail.Attachments.Add(new Attachment(file2.FullName));
                        }
                    }
                    sub = "Gate pass Approval Required";
                    Body = "Hi,<br/> We would like to inform you that a gate pass has been raised for the Outward Material as attached. " +
                                "Request you to provide an approval After your review to dispatch the same.";
                    var regards = "<br/><br/>Best Regards,<br/>Team Harbinger IMS";
                    Body = Body + regards;
                    var remark = "<br/><font size=\"2\" color=\"red\"> <i> Note : This is a system generated email. Please do not reply to this email.</i></font>";
                    Body = Body + remark;

                }
                else if(status == "Approved by Approver")
                {
                    var securityemail = (from u in context.IMSUsers
                                         join r in context.IMSRoles on u.userId equals r.IMSuser_Id
                                         where r.userLocation == GPdetails.Location && r.RoleInIMS == "Security Guard"
                                         select new { u.userEmail }).ToList();
                    if (securityemail != null)
                    {
                        foreach (var m in securityemail)
                        {
                            mail.To.Add(m.userEmail.ToString());
                        }
                    }
                    mail.To.Add(senderdetails.userEmail.ToString());

                    var outputpath = @"D:\IMSDocs\Gatepasses\" + gatepassID + "\\" + GPdetails.Gatepass_InvInchargeFileName;
                    if (outputpath != "")
                    {
                        mail.Attachments.Add(new Attachment(outputpath));
                    }
                    sub = "Gate pass" + GPdetails.Gatepass_Number.ToString() +  "Approved";
                    Body = "Hi,<br/> This is to inform you that the Gate Pass Number " + GPdetails.Gatepass_Number.ToString() +
                        ". for the outward material has been approved Please proceed for the further process of the dispatch";
                    var regards = "<br/><br/>Best Regards,<br/> Team Harbinger IMS";
                    Body = Body + regards;
                    var remark = "<br/><font size=\"2\" color=\"red\"> <i> Note : This is a system generated email. Please do not reply to this email.</i></font>";
                    Body = Body + remark;
                }

                else if (status == "Gatepass Rejected")
                {
                   
                    mail.To.Add(senderdetails.userEmail.ToString());

                   
                    sub = "Gate pass" + GPdetails.Gatepass_Number.ToString() + "Rejected";
                    Body = "Hi,<br/> This is to inform you that the gate pass for the outward material has been rejected." +
                        " Please find the details below: <br/>Reason of Rejection: "+GPdetails.GatepassRejectReason.ToString(); 
                            
                    var regards = "<br/><br/>Best Regards,<br/> Team Harbinger IMS";
                    Body = Body + regards;
                    var remark = "<br/><font size=\"2\" color=\"red\"> <i> Note : This is a system generated email. Please do not reply to this email.</i></font>";
                    Body = Body + remark;
                }

                else if (status == "Material Dispatched")
                {
                    
                     mail.To.Add(senderdetails.userEmail.ToString());
                     mail.To.Add(GPdetails.ReceiverEmail.ToString());

                    var outputpath = @"D:\IMSDocs\Gatepasses\" + gatepassID + "\\" + GPdetails.Gatepass_SecurityGuardFileName;
                    if (outputpath != "")
                    {
                        mail.Attachments.Add(new Attachment(outputpath));
                    }
                    sub = "Gate pass" + GPdetails.Gatepass_Number.ToString() + "Material Dispatched";
                    Body = "Hi,<br/> This is to inform you that the Gate Pass Number " + GPdetails.Gatepass_Number.ToString() +
                        ". has been successfully dispatched.";
                    var regards = "<br/><br/>Best Regards,<br/> Team Harbinger IMS";
                    Body = Body + regards;
                    var remark = "<br/><font size=\"2\" color=\"red\"> <i> Note : This is a system generated email. Please do not reply to this email.</i></font>";
                    Body = Body + remark;
                }


                mail.Subject = sub;
                mail.From = new MailAddress(username);
                mail.Body = Body;
                mail.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient();
                smtp.Host = ConfigurationManager.AppSettings["SmtpServerHost"].ToString();
                smtp.Port = Convert.ToInt32(ConfigurationManager.AppSettings["SmtpServerPort"]);
                smtp.UseDefaultCredentials = false;
                smtp.EnableSsl = true;
                string password = ConfigurationManager.AppSettings["SmtpUserPassword"].ToString();
                smtp.Credentials = new System.Net.NetworkCredential(username, password);
                smtp.Send(mail);
                return true;
            }
            catch (Exception e)
            {
                var exceptionLogger = new IMSExceptionLogger()
                {
                    controllerName = "Add_GatepassController",
                    actionName = "sendGatepassMail",
                    exceptionStackTrace = e.StackTrace,
                    exceptionMessage = e.Message,
                    exceptionLogTime = DateTime.Now
                };
                return false;
            }
        }

        [HttpGet]
        public JsonResult getVendorDetails(string vendorID)
        {
            var vendorDetails = (from v in context.HTV_Vendor
                                 where v.vendorId == vendorID
                                 select v).FirstOrDefault();

            return Json(new { success = true, Email = vendorDetails.vendorEmail.ToString(), address = vendorDetails.vendorAddress.ToString(), contact = vendorDetails.mobileNo.ToString()}, JsonRequestBehavior.AllowGet);
        }

        public JsonResult getIMSUserDetails(string vendorID)
        {
            var userDetails = (from v in context.IMSUsers
                                 where v.userId == vendorID
                                 select v).FirstOrDefault();

            return Json(new { success = true, Email = userDetails.userEmail.ToString(), address = "", contact = "" }, JsonRequestBehavior.AllowGet);

        }
        public List<IMSEntity> Getinventorymaterials()
        {
            //var materiallist = new List<Searchmateriallist>();
            var materials = new List<IMSEntity>();
            var materialcat = new Material();
            var materiallist = (from t in context.Inventory_Register
                                join i in context.InwardMaterials on t.InwardID equals i.InwardID
                                where t.IR_Status != "Material Out" && t.IR_Status != "Material Disposed"
                                select t);

            foreach (var item in materiallist)
            {
                StringBuilder dropdownOptions = new StringBuilder();
                dropdownOptions.AppendFormat("{0} | {1} | {2} | {3} | {4} | {5}\n", item.AssetID, item.MaterialName, item.ExpenseNature, item.Model_Number, item.Serial_No, item.GRN_Number);
                materials.Add(new IMSEntity()
                {
                    materialtext = dropdownOptions.ToString(),
                    material_cp_rental_ID = item.Inward_CP_Rental_ID,
                    InventoryReg_ID = item.InventoryReg_ID

                });
            }

            return materials;
            //var jsonResult = JsonConvert.SerializeObject(dropdownOptions);
            //return Json(jsonResult, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GatepassList(string sortOrder, string searchString, string Name, string SearchValue, FormCollection form, DateTime? searchdate, int? Page_no)
        {
            string userrole = null;
            string location = null;
            string role = null;
            string dept = null;
            string empid = null;

            if (Session["UserID"] != null)
            {
                empid = Session["UserID"].ToString();
            }

            if (Session["role"] != null)
            {
                role = Session["role"].ToString();
                userrole = Session["role"].ToString();
            }
            if (Session["DepartmentID"] != null)
            {
                dept = Session["DepartmentID"].ToString();
            }

            int countForsearch = 0;
            string display = "0";
            ViewBag.display = display;
            ViewBag.empid = empid;
            int count = 0;

            string Email = null;
            DateTime val = DateTime.Now;
            int pagesize = 20;
            int pagenumber = (Page_no ?? 1);
            ViewBag.page = Page_no;
            if (searchdate.HasValue)
            {
                val = searchdate ?? DateTime.Now;

            }

            if (Session["Location"] != null)
            {
                location = Session["Location"].ToString();
                ViewBag.location = Session["Location"].ToString();
            }

            ViewBag.GatepassNo = String.IsNullOrEmpty(sortOrder) ? "GatepassNo_desc" : "";
            ViewBag.Gatepass_department = sortOrder == "Gatepass_department" ? "Gatepass_department_desc" : "Gatepass_department";
            ViewBag.Gatepass_Location = sortOrder == "Gatepass_Location" ? "Gatepass_Location_desc" : "Gatepass_Location";
            ViewBag.GatepassType = sortOrder == "GatepassType" ? "GatepassType_desc" : "GatepassType";
            ViewBag.Sender = sortOrder == "Sender" ? "Sender_desc" : "Sender";
            ViewBag.Receiver = sortOrder == "Receiver" ? "Receiver_desc" : "Receiver";
            ViewBag.Category = sortOrder == "Category" ? "Category_desc" : "Category";
            ViewBag.Status = sortOrder == "Status" ? "Status_desc" : "Status";



            ViewBag.role = role;
            var imsentity = new List<IMSEntity>();

            try
            {
                ServiceVMSEntities context = new ServiceVMSEntities();

                IQueryable<GatePass> GatepassDetails = null;
                IQueryable<GatePass> Finallist = null;

                GatepassDetails = (from t in context.GatePasses where t.GatePass_Status != "Deleted" select t);
                if (empid != null)
                {
                    if (empid.Contains("DUM"))
                    {
                        if (role == "Inventory Manager")
                        {
                            GatepassDetails = GatepassDetails.Where(u => u.GatepassID.Contains("DUM"));
                        }
                        else
                        {
                            GatepassDetails = GatepassDetails.Where(u => u.GatepassID.Contains("DUM") && u.userDepartmentId == int.Parse(dept));
                        }
                    }
                    else
                    {
                        int deptid = int.Parse(dept);
                        if (role == "Administrator")
                        {
                            GatepassDetails = GatepassDetails.Where(u => !u.GatepassID.Contains("DUM"));
                        }
                        else if (role == "Inventory Manager")
                        {
                            GatepassDetails = GatepassDetails.Where(u => !u.GatepassID.Contains("DUM") && u.userDepartmentId == deptid);
                        }
                        else if (role == "Security Guard")
                        {
                            GatepassDetails = GatepassDetails.Where(u => !u.GatepassID.Contains("DUM") && u.Location == location);
                        }
                        else if (role == "Inventory Incharge" || role == "Receiver")
                        {
                            GatepassDetails = GatepassDetails.Where(u => !u.GatepassID.Contains("DUM") && u.Location == location && u.userDepartmentId == deptid);
                        }
                        else
                        {
                            GatepassDetails = GatepassDetails.Where(u => !u.GatepassID.Contains("DUM") && u.userDepartmentId == deptid);
                        }
                    }

                }
                if (Name == "Actionalble")
                {
                    if (role == "Inventory Incharge")
                    {
                        GatepassDetails = GatepassDetails.Where(u => u.GatePass_Status == "Pending for approver");
                    }
                    else if (role == "Security Guard")
                    {
                        GatepassDetails = GatepassDetails.Where(u => u.GatePass_Status == "Approved");
                    }
                }

                var c11 = GatepassDetails.Count();
                if (!GatepassDetails.Any())
                {
                    ViewBag.NoItemsFound = "No items found.";
                }
                var c = GatepassDetails.Count();
                if (!String.IsNullOrEmpty(SearchValue))
                {
                    String Searchby = form["Name"].ToString();

                    if (Searchby == "GatepassNo")
                    {
                        countForsearch = 1;
                        GatepassDetails = GatepassDetails.Where(u => u.Gatepass_Number.Contains(SearchValue));
                    }
                    else if (Searchby == "Gatepass_department")
                    {
                        countForsearch = 1;
                        GatepassDetails = GatepassDetails.Where(u => u.ServiceUserDepartment.userDepartmentName == SearchValue);
                    }
                    else if (Searchby == "Gatepass_Location")
                    {
                        countForsearch = 1;
                        GatepassDetails = GatepassDetails.Where(u => u.Location.Contains(SearchValue));
                    }
                    else if (Searchby == "GatepassType")
                    {
                        countForsearch = 1;
                        GatepassDetails = GatepassDetails.Where(u => u.GatepassType.Contains(SearchValue));
                    }
                    else if (Searchby == "Gatepass_datetime" && (searchdate != null))
                    {
                        countForsearch = 1;
                        val = val.Date;
                        GatepassDetails = GatepassDetails.Where(u => u.Gatepass_DateTime == (val));
                    }
                    else if (Searchby == "Sender")
                    {
                        countForsearch = 1;
                        GatepassDetails = (from t in context.GatePasses
                                           join u in context.Users on t.SenderID equals u.employeeId
                                           where t.GatePass_Status != "Deleted" && u.userName.Contains(SearchValue)
                                           select t);
                        //GatepassDetails = GatepassDetails.Where(u => u.SenderID.Contains(SearchValue));
                    }
                    else if (Searchby == "Receiver")
                    {
                        countForsearch = 1;
                        GatepassDetails = GatepassDetails.Where(u => u.ReceiverName.Contains(SearchValue));
                    }
                    else if (Searchby == "Status")
                    {
                        countForsearch = 1;
                        GatepassDetails = GatepassDetails.Where(u => u.GatePass_Status.Contains(SearchValue));
                    }

                }

                switch (sortOrder)
                {
                    case "GatepassNo":
                        GatepassDetails = GatepassDetails.OrderByDescending(p => p.Gatepass_Number);
                        break;

                    case "Gatepass_department":
                        GatepassDetails = GatepassDetails.OrderByDescending(p => p.ServiceUserDepartment.userDepartmentName);
                        break;

                    case "Gatepass_department_desc":
                        GatepassDetails = GatepassDetails.OrderBy(p => p.ServiceUserDepartment.userDepartmentName);
                        break;

                    case "Gatepass_Location":
                        GatepassDetails = GatepassDetails.OrderByDescending(u => u.Location);
                        break;

                    case "Gatepass_Location_desc":
                        GatepassDetails = GatepassDetails.OrderBy(u => u.Location);
                        break;

                    case "GatepassType":
                        GatepassDetails = GatepassDetails.OrderByDescending(u => u.GatepassType);
                        break;

                    case "GatepassType_desc":
                        GatepassDetails = GatepassDetails.OrderBy(u => u.GatepassType);
                        break;

                    case "Sender":
                        GatepassDetails = GatepassDetails.OrderByDescending(u => u.SenderID);
                        break;

                    case "Sender_desc":
                        GatepassDetails = GatepassDetails.OrderBy(u => u.SenderID);
                        break;

                    case "Receiver":
                        GatepassDetails = GatepassDetails.OrderByDescending(u => u.ReceiverName);
                        break;

                    case "Receiver_desc":
                        GatepassDetails = GatepassDetails.OrderBy(u => u.ReceiverName);
                        break;

                    case "Category":
                        GatepassDetails = GatepassDetails.OrderByDescending(u => (u.GatePass_Category));
                        break;

                    case "Category_desc":
                        GatepassDetails = GatepassDetails.OrderBy(u => (u.GatePass_Category));
                        break;

                    case "Status":
                        GatepassDetails = GatepassDetails.OrderByDescending(u => u.GatePass_Status);
                        break;

                    case "Status_desc":
                        GatepassDetails = GatepassDetails.OrderBy(u => u.GatePass_Status);
                        break;

                    default:
                        GatepassDetails = GatepassDetails.OrderByDescending(p => p.Gatepass_Number);
                        break;
                }

                foreach (var t in GatepassDetails)
                {

                    imsentity.Add(new IMSEntity()
                    {
                        Gatepass_Number = t.Gatepass_Number,
                        GatepassID = t.GatepassID,
                        GatePass_Category = t.GatePass_Category,
                        GatepassType = t.GatepassType,
                        Gatepass_DateTime = t.Gatepass_DateTime,
                        Location = t.Location,
                        userDepartmentId = t.userDepartmentId ?? 0,
                        SenderID = t.SenderID,
                        ReceiverName = t.ReceiverName,
                        GatePass_Status = t.GatePass_Status
                    });
                }

                ViewBag.count = count;
                ViewBag.countForsearch = countForsearch;
                return View(imsentity.ToPagedList(pagenumber, pagesize));
            }
            catch (Exception e)
            {

                Console.Write(e);

                var exceptionLogger = new IMSExceptionLogger()
                {
                    controllerName = "Add_GatepassController",
                    actionName = "GatepassList",
                    exceptionStackTrace = e.StackTrace,
                    exceptionMessage = e.Message,
                    exceptionLogTime = DateTime.Now

                };
                unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                unitOfWork.Save();
            }


            return View(imsentity);
        }


        public ActionResult GatepassDetails(string GatepassID)
        {

            GatepassID = unitOfWork.DepartmentRepository.Decode(GatepassID);
            string role = string.Empty;
            string empid = string.Empty;
            string dept = null;
            string location = null;
            List<IMSEntity> inwardlistmodel = new List<IMSEntity>();
            if (Session["UserID"] != null)
            {
                empid = Session["UserID"].ToString();
            }

            if (Session["role"] != null)
            {
                role = Session["role"].ToString();
                
            }
            if (Session["DepartmentID"] != null)
            {
                dept = Session["DepartmentID"].ToString();
            }


            ViewBag.dept = dept;
            ViewBag.empid = empid;
            ViewBag.GatepassID = GatepassID;
            ViewBag.role = role;
            if (Session["Location"] == null)
            {
                var user = (from i in context.IMSRoles
                            where i.IMSuser_Id == empid && i.RoleInIMS == role
                            select new { i.userLocation }).FirstOrDefault();
                ViewBag.location = user.userLocation.ToString();
            }
            else
            {
                ViewBag.location = Session["Location"].ToString();
            }
            try
            {
                var GPdetails = (from d in context.GatePasses
                                 join u in context.ServiceUserDepartments on d.userDepartmentId equals u.userDepartmentId
                                 join e in context.IMSUsers on d.SenderID equals e.userId
                                 where d.GatepassID == GatepassID && !d.GatePass_Status.Equals("Deleted")
                                 select new
                                 {
                                     d.GatepassID,
                                     d.Location,
                                     d.Gatepass_DateTime,
                                     d.Created_By,
                                     d.GatepassType,
                                     d.STP_Bonded_Item,
                                     d.SenderID,
                                     u.userDepartmentName,
                                     u.userDepartmentId,
                                     d.Reason,
                                     d.Gatepass_Number,
                                     d.ReceiverID,
                                     d.ReceiverName,
                                     d.ReceiverAddress,
                                     d.ReceiverContact,
                                     d.ReceiverEmail,
                                     d.Expected_DateofReturn,
                                     d.TO_Office,
                                     d.From_Office,
                                     d.GatePass_Category,
                                     d.GatePass_Status,
                                     d.SEZ_Bonded_Item,
                                     e.userName
                                 }).FirstOrDefault();

                if (GPdetails.Gatepass_Number != null)
                {
                    ViewBag.Gatepass_Number = GPdetails.Gatepass_Number;
                }
                if (GPdetails.Location != null)
                {
                    ViewBag.GPLocation = GPdetails.Location;
                }
                var gpsignature = (from t in context.IMS_Signature
                                where t.userId.Contains(empid) && t.IMSsignatureStatus != "Deleted" && t.role == role
                                select t).FirstOrDefault();

                if (gpsignature != null)
                {
                    ViewBag.SignatureExist = 0;
                }
                ViewBag.GPStatus = GPdetails.GatePass_Status;
                inwardlistmodel.Add(new IMSEntity()
                {
                    GatepassID = GPdetails.GatepassID,
                    Location = GPdetails.Location,
                    Gatepass_DateTime = GPdetails.Gatepass_DateTime,
                    Created_By = GPdetails.Created_By,
                    GatepassType = GPdetails.GatepassType,
                    STP_Bonded_Item = GPdetails.STP_Bonded_Item,
                    SenderID = GPdetails.SenderID,
                    userDepartmentName = GPdetails.userDepartmentName,
                    userDepartmentId = GPdetails.userDepartmentId,
                    Reason = GPdetails.Reason,
                    Gatepass_Number = GPdetails.Gatepass_Number,
                    ReceiverID = GPdetails.ReceiverID,
                    ReceiverName = GPdetails.ReceiverName,
                    ReceiverAddress = GPdetails.ReceiverAddress,
                    ReceiverContact = GPdetails.ReceiverContact,
                    ReceiverEmail = GPdetails.ReceiverEmail,
                    Expected_DateofReturn = GPdetails.Expected_DateofReturn ,
                    TO_Office = GPdetails.TO_Office,
                    From_Office = GPdetails.From_Office,
                    GatePass_Category = GPdetails.GatePass_Category,
                    GatePass_Status = GPdetails.GatePass_Status,
                    SEZ_Bonded_Item = GPdetails.SEZ_Bonded_Item,
                    SenderName = GPdetails.userName
                });
            }
            catch (Exception e)
            {
                Console.Write(e);
                var exceptionLogger = new IMSExceptionLogger()
                {
                    controllerName = "Add_GatepassController",
                    actionName = "GatepassDetails",
                    exceptionStackTrace = e.StackTrace,
                    exceptionMessage = e.Message,
                    exceptionLogTime = DateTime.Now
                };
                unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                unitOfWork.Save();
            }
            return View(inwardlistmodel);
        }

        public void AddCellToBody(PdfPTable table, string cellText)
        {
            table.AddCell(new PdfPCell(new Phrase(35f,cellText, FontFactory.GetFont("Arial", 9)))
            {
                //HorizontalAlignment = Element.ALIGN_CENTER,
                //VerticalAlignment = Element.ALIGN_MIDDLE
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_LEFT
            });
        }

        public void AddCellToHeader(PdfPTable table, string cellText)
        {
            table.AddCell(new PdfPCell(new Phrase(35f,cellText, FontFactory.GetFont("Arial", 10, Font.BOLD)))
            {
                //HorizontalAlignment = Element.ALIGN_CENTER,
                //VerticalAlignment = Element.ALIGN_MIDDLE,
                //BackgroundColor = BaseColor.LIGHT_GRAY
                Border = Rectangle.NO_BORDER,
                HorizontalAlignment = Element.ALIGN_LEFT,
               
            });
        }

        public void AddCellToTableHeader(PdfPTable table, string cellText, Font font)
        {
            table.AddCell(new PdfPCell(new Phrase(cellText, font))
            {
                HorizontalAlignment = Element.ALIGN_CENTER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                BackgroundColor = new BaseColor(26, 52, 85),
                Padding = 5
            });
        }

        public void AddCellToTableBody(PdfPTable table, string cellText, Font font)
        {
            PdfPCell cell = new PdfPCell(new Phrase(cellText, font))
            {
                HorizontalAlignment = Element.ALIGN_CENTER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Padding = 5
            };

            if (table.Rows.Count % 2 == 0)
            {
                cell.BackgroundColor = new BaseColor(216, 217, 219);
            }

            table.AddCell(cell);
        }

        //public void AddSignatureCell(PdfPTable table, string name, string role, string imagePath)
        //{
        //    PdfPCell signatureCell = new PdfPCell();
        //    signatureCell.Border = Rectangle.NO_BORDER;
        //    signatureCell.Padding = 5;
        //    signatureCell.HorizontalAlignment = Element.ALIGN_CENTER;

        //    // Add signature image
        //    if (System.IO.File.Exists(imagePath))
        //    {
        //        Image signature = Image.GetInstance(imagePath);
        //        signature.ScaleToFit(100f, 50f);
        //        signatureCell.AddElement(signature);
        //    }

        //    // Add name and role
        //    Paragraph nameParagraph = new Paragraph(name, FontFactory.GetFont("Arial", 10, Font.BOLD));
        //    nameParagraph.Alignment = Element.ALIGN_CENTER;
        //    signatureCell.AddElement(nameParagraph);

        //    Paragraph roleParagraph = new Paragraph(role, FontFactory.GetFont("Arial", 8));
        //    roleParagraph.Alignment = Element.ALIGN_CENTER;
        //    signatureCell.AddElement(roleParagraph);

        //    table.AddCell(signatureCell);
        //}

        public static void AddSignatureCell(PdfPTable table, string name, string role, string imagePath)
        {
            PdfPCell signatureCell = new PdfPCell();
            signatureCell.Border = Rectangle.NO_BORDER;
            signatureCell.FixedHeight = 100f; // Set a fixed height for consistency
            signatureCell.VerticalAlignment = Element.ALIGN_MIDDLE;
            signatureCell.HorizontalAlignment = Element.ALIGN_CENTER;

            // Add signature image
            if (System.IO.File.Exists(imagePath))
            {
                Image signature = Image.GetInstance(imagePath);
                signature.ScaleToFit(60f, 60f);
                signature.Alignment = Element.ALIGN_CENTER;
                signatureCell.AddElement(signature);
            }
            else
            {
                // Add an empty paragraph to keep the cell structure
                Paragraph emptyParagraph = new Paragraph(" ", new Font(Font.FontFamily.HELVETICA, 12, Font.NORMAL));
                signatureCell.AddElement(emptyParagraph);
            }

            // Add line
            Paragraph lineParagraph = new Paragraph("______________________", new Font(Font.FontFamily.HELVETICA, 12, Font.NORMAL));
            lineParagraph.Alignment = Element.ALIGN_CENTER;
            lineParagraph.SpacingAfter = 5f;
            signatureCell.AddElement(lineParagraph);

            // Add name and role
            Paragraph nameParagraph = new Paragraph(name, FontFactory.GetFont("Arial", 10, Font.BOLD));
            nameParagraph.Alignment = Element.ALIGN_CENTER;
            nameParagraph.SpacingAfter = 2f;
            signatureCell.AddElement(nameParagraph);

            Paragraph roleParagraph = new Paragraph(role, FontFactory.GetFont("Arial", 8));
            roleParagraph.Alignment = Element.ALIGN_CENTER;
            signatureCell.AddElement(roleParagraph);

            table.AddCell(signatureCell);
        }

        public bool GatepassPDF(IMSEntity model1, string ReceiverAddress, string loc, string sharedData, string empid)
        {
            ServiceVMSEntities context = new ServiceVMSEntities();
            var OutputPath = @"D:\IMSDocs\Gatepasses\" + model1.GatepassID;

            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }
            else
            {
                string[] oldfilename = Directory.GetFiles(@"D:\IMSDocs\Gatepasses\" + model1.GatepassID);
                foreach (string file1 in oldfilename)
                {
                    System.IO.File.Delete(file1);
                }
            }

            bool STP_Bonded_Item = true;
            bool SEZ_Bonded_Item = true;
            DateTime? Expected_DateofReturn = null;
            string GatepassType = null;
            string Expecteddate = "";
            string today = DateTime.Now.ToString("MMM dd, yyyy");
            var GatePasses = (from t in context.GatePasses
                              where t.GatepassID == model1.GatepassID
                              select new { t.Expected_DateofReturn,t.Gatepass_Number ,t.STP_Bonded_Item, t.GatepassType ,t.Gatepass_DateTime,t.ReceiverName,t.SEZ_Bonded_Item,t.ReceiverAddress}).FirstOrDefault();
           
          
            STP_Bonded_Item = Convert.ToBoolean(GatePasses.STP_Bonded_Item);
            SEZ_Bonded_Item = Convert.ToBoolean(GatePasses.SEZ_Bonded_Item);
            GatepassType = GatePasses.GatepassType;

            if (GatePasses.Expected_DateofReturn != null)
            {
                DateTime originalDate = DateTime.Parse(GatePasses.Expected_DateofReturn.ToString());
                Expecteddate = originalDate.ToString("MMM dd, yyyy");
            }

            DateTime gpDate = DateTime.Parse(GatePasses.Gatepass_DateTime.ToString());
            var GPCreatedate = gpDate.ToString("MMM dd, yyyy");
            string bondedItemText = STP_Bonded_Item ? "Yes" : "No";
            string SEZbondedItemText = SEZ_Bonded_Item ? "Yes" : "No";

            Document document = new Document(PageSize.A4,50,50,25,25);

            Font boldFont = FontFactory.GetFont("Arial", 10, Font.BOLD);
            Font simple = FontFactory.GetFont("Arial", 10);
            Font boldItalicFont = FontFactory.GetFont("Arial", 10, Font.BOLDITALIC);

            var imagepath = @"D:\IMSDocs\AllWebSites\";
            iTextSharp.text.Image png = iTextSharp.text.Image.GetInstance(imagepath + "Harbinger Group Logo.jpg");

            PdfPCell imgCell;
            PdfPTable tblImg = new PdfPTable(2);
            tblImg.WidthPercentage = 35;
            tblImg.HorizontalAlignment = Element.ALIGN_CENTER;
            tblImg.SetWidthPercentage(new float[2] { 350f, 300f }, PageSize.A4);
            imgCell = new PdfPCell();
            imgCell.HorizontalAlignment = Element.ALIGN_LEFT;
            imgCell.PaddingTop = -30f;
            imgCell.PaddingLeft = 5f;
            png.ScalePercent(12f);
            imgCell.AddElement(png); //parImg);
            imgCell.BorderWidth = PdfPCell.NO_BORDER;
            tblImg.AddCell(imgCell);

            var returnType = string.Empty;
            if(model1.Expected_DateofReturn == null)
            {
                returnType = "NonReturnable Material";
            }
            else
            {
                returnType = "Returnable Material";
            }

            imgCell = new PdfPCell();
            var subtitleParagraph = new Paragraph($"GATE PASS\n({returnType})\n\n", boldFont);          
            subtitleParagraph.Alignment = Element.ALIGN_RIGHT;           
            imgCell.PaddingTop = -30f;
            imgCell.PaddingLeft = 60f;
            imgCell.AddElement(subtitleParagraph);
            imgCell.BorderWidth = PdfPCell.NO_BORDER;
            tblImg.AddCell(imgCell);


            Paragraph msgPhrase = new Paragraph();
            Phrase msgPhrase1 = new Phrase();
            Font lineFont = FontFactory.GetFont("Arial", 10, new iTextSharp.text.BaseColor(System.Drawing.ColorTranslator.FromHtml("#737373")));
            msgPhrase1.Add(new Chunk("_________________________________________________________________________________________", lineFont));
            msgPhrase.Add(msgPhrase1);
            

            PdfPTable headerTable = new PdfPTable(5);
            headerTable.WidthPercentage = 100;
            float[] columnWidths = new float[] { 1f, 1f, 1f, 1f ,1f}; // Adjust widths as needed
            headerTable.SetWidths(columnWidths);

            // Add header row
            AddCellToHeader(headerTable, "Gatepass No.");
            AddCellToHeader(headerTable, "Date");
            AddCellToHeader(headerTable, "Expected Date Of Return");
            AddCellToHeader(headerTable, "STP Bonded Items");
            AddCellToHeader(headerTable, "SEZ Bonded Items");

            // Add data row
            AddCellToBody(headerTable, GatePasses.Gatepass_Number.ToString());
            AddCellToBody(headerTable, gpDate.ToString("MMM dd, yyyy"));
            AddCellToBody(headerTable, Expecteddate);
            AddCellToBody(headerTable, bondedItemText);
            AddCellToBody(headerTable, SEZbondedItemText);

            string locationAddress = null;
            string Receivername = null;
            string Receiveraddress = null;
            string Fromname = null;
            if (model1.TO_Office == null && model1.From_Office == null)
            {
                if (loc == "SEZ" || loc == "Global Port" || loc == "Siddhant")
                {
                    var vendor = (from t in context.OfficeAddresses
                                  where t.locationName == loc
                                  select new { t.locationAddress });
                    foreach (var d in vendor)
                    {
                        locationAddress = d.locationAddress;
                    }
                }
                Receivername = GatePasses.ReceiverName.ToString();
                Receiveraddress = GatePasses.ReceiverAddress.ToString();
                Fromname = "HARBINGER SYSTEMS PVT.LTD.";
            }
            else
            {
                var TOoffice = (from s in context.OfficeAddresses
                                where s.locationName == model1.TO_Office
                                select new { s.locationAddress, s.locationName }).FirstOrDefault();

                var FROMoffice = (from s in context.OfficeAddresses
                                where s.locationName == model1.From_Office
                                select new { s.locationAddress, s.locationName }).FirstOrDefault();

                Fromname = FROMoffice.locationName.ToString();
                locationAddress = FROMoffice.locationAddress.ToString();

                Receivername = TOoffice.locationName.ToString();
                Receiveraddress = TOoffice.locationAddress.ToString();
            }
           //define address table
            Font titleFont = FontFactory.GetFont("Arial", 10, Font.BOLD);
            Font headerFont = FontFactory.GetFont("Arial", 11, Font.BOLD);
            Font cellFont = FontFactory.GetFont("Arial", 10);

            PdfPTable addressTable = new PdfPTable(2);
            addressTable.WidthPercentage = 100;
            addressTable.SetWidths(new float[] { 50f, 50f });

            // Create the "From" cell
            Phrase fromPhrase = new Phrase();
            fromPhrase.Leading = 35f;
            fromPhrase.Add(new Chunk("From,\n\n", titleFont));
            fromPhrase.Add(new Chunk($"{Fromname}\n", headerFont));
            fromPhrase.Add(new Chunk(locationAddress, cellFont));

            PdfPCell fromCell = new PdfPCell(fromPhrase);
            fromCell.Border = Rectangle.NO_BORDER;
            addressTable.AddCell(fromCell);

            // Create the "To" cell
            Phrase toPhrase = new Phrase();
            toPhrase.Leading = 35f;
            toPhrase.Add(new Chunk("To,\n\n", titleFont));
            toPhrase.Add(new Chunk(Receivername + "\n", headerFont));
            toPhrase.Add(new Chunk(Receiveraddress, cellFont));

            PdfPCell toCell = new PdfPCell(toPhrase);
            toCell.Border = Rectangle.NO_BORDER;
            addressTable.AddCell(toCell);

            // Create dynamic table for materials
            PdfPTable table_pdfcell = new PdfPTable(6); // Assuming 7 columns
            table_pdfcell.WidthPercentage = 100;
            table_pdfcell.SetWidths(new float[] { 1f, 1f, 2f, 1f, 2f, 2f });

            Font tableHeaderFont = FontFactory.GetFont("Arial", 10, Font.BOLD, BaseColor.WHITE);
            Font tableCellFont = FontFactory.GetFont("Arial", 9);

            // Create header cells with blue background
            AddCellToTableHeader(table_pdfcell, "Sr. No", tableHeaderFont);
            AddCellToTableHeader(table_pdfcell, "Material Name", tableHeaderFont);
            AddCellToTableHeader(table_pdfcell, "Material Description", tableHeaderFont);
            AddCellToTableHeader(table_pdfcell, "Qty.", tableHeaderFont);           
            AddCellToTableHeader(table_pdfcell, "Asset No", tableHeaderFont);
            AddCellToTableHeader(table_pdfcell, "Remarks", tableHeaderFont);
          
            var OutMaterial = (from t in context.OutMaterials
                               join a in context.Inventory_Register on t.InventoryReg_ID equals a.InventoryReg_ID
                               where t.GatepassID == model1.GatepassID
                               select new { t.Quantity, t.Serial_Number, t.Out_Status, t.Remarks, t.MaterialName,a.AssetID ,t.Model_Number}).ToList();
            int i = 1;
            foreach (var material in OutMaterial)
            {
                var details = "Serial_number :" + material.Serial_Number + "\n" + "Model_Number :" + material.Model_Number;
                AddCellToTableBody(table_pdfcell, i.ToString(), tableCellFont);
                AddCellToTableBody(table_pdfcell, material.MaterialName, tableCellFont);
                AddCellToTableBody(table_pdfcell, details, tableCellFont);
                AddCellToTableBody(table_pdfcell, material.Quantity.ToString(), tableCellFont);           
                AddCellToTableBody(table_pdfcell, material.AssetID, tableCellFont);
                AddCellToTableBody(table_pdfcell, material.Remarks, tableCellFont);               
                i++;
                details = "";
            }


            Font bodyFont = FontFactory.GetFont("Arial", 10);
            Font footerFont = FontFactory.GetFont("Arial", 8);
            Font preparedByFont = FontFactory.GetFont("Arial", 10, Font.BOLD);
           

            Paragraph footer = new Paragraph("Please pass above material through (By Courier/By hand)", footerFont);
            footer.Alignment = Element.ALIGN_CENTER;

            string userName = null;
            var Preparedname = (from t in context.IMSUsers
                                where t.userId == empid
                                select new { t.userName }).FirstOrDefault();

            userName = Preparedname.userName.ToString();
            Paragraph preparedBy = new Paragraph($"Prepared By\n{userName}\n", preparedByFont);
            preparedBy.Alignment = Element.ALIGN_CENTER;



            // Add signature section
            PdfPTable signatureTable = new PdfPTable(3);
            signatureTable.WidthPercentage = 100;
            signatureTable.SetWidths(new float[] { 33f, 33f, 33f });

            var Authorisedname = string.Empty;
            var securityname = string.Empty;

            var authdetails = (from g in context.GatepassApprovers
                               join us in context.IMSUsers on g.employeeId equals us.userId
                               where g.Location == loc && g.userDepartmentId == model1.userDepartmentId && g.RoleInIMS == "Inventory Incharge"
                               select new { g.userEmail, us.userName }).FirstOrDefault();

            var securitydetails = (from u in context.IMSUsers 
                                   join r in context.IMSRoles on u.userId equals r.IMSuser_Id
                                   where r.userLocation == loc && r.userDepartmentId == model1.userDepartmentId && r.RoleInIMS == "Security Guard" && r.Role_Status != "Deleted"
                                   select new { u.userEmail, u.userName }).FirstOrDefault();

            if (authdetails != null)
            {
                Authorisedname = authdetails.userName.ToString();
            }
            if (securitydetails != null)
            {
                securityname = securitydetails.userName.ToString();
            }

           
            AddSignatureCell(signatureTable, Authorisedname, "Authorised By", "");
            AddSignatureCell(signatureTable, securityname , "Security", "");
            AddSignatureCell(signatureTable, GatePasses.ReceiverName, "Received By", "");


            string spacing = "<br/>";
            string dash = "_________________________________________________________________________________________________";
            //  Document document = new Document(PageSize.A4);
            PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(OutputPath + "\\" + model1.GatepassID + ".pdf", FileMode.Create));
            document.Open();

            HTMLWorker hw = new HTMLWorker(document);
            
            hw.Parse(new StringReader(spacing));
            document.Add(tblImg);           
            document.Add(msgPhrase);
           
            hw.Parse(new StringReader(spacing));
            document.Add(headerTable);           
            document.Add(msgPhrase);           
            hw.Parse(new StringReader(spacing));
            document.Add(addressTable);           
            hw.Parse(new StringReader(spacing));            
            document.Add(msgPhrase);
            hw.Parse(new StringReader(spacing));
            document.Add(table_pdfcell);
            hw.Parse(new StringReader(spacing));
            document.Add(footer);
            hw.Parse(new StringReader(spacing));
            document.Add(preparedBy);
            hw.Parse(new StringReader(spacing));            
            document.Add(signatureTable);
            document.Add(msgPhrase);
            hw.Parse(new StringReader(spacing));
            
            document.Close();
            writer.Close();
            return true;
        }

        public bool gatepasssignaturePDF(string gatepassID)
        {
            ServiceVMSEntities context = new ServiceVMSEntities();

            var InvInchargesignature = (from t in context.IMS_Signature
                                where t.referenceId == gatepassID && t.role == "Inventory Incharge" && t.categoryName == "Gatepass" && t.IMSsignatureStatus != "Deleted"
                                select t).FirstOrDefault();

            var Securitysignature = (from t in context.IMS_Signature
                               where t.referenceId == gatepassID && t.role == "Security Guard" && t.categoryName == "Gatepass" && t.IMSsignatureStatus != "Deleted"
                               select t).FirstOrDefault();

            var GatePasses = (from t in context.GatePasses
                              where t.GatepassID == gatepassID && t.GatePass_Status != "Deleted"
                              select new
                              {
                                  t.Expected_DateofReturn,
                                  t.Gatepass_Number,
                                  t.STP_Bonded_Item,
                                  t.GatepassType,
                                  t.Gatepass_DateTime,
                                  t.ReceiverName,
                                  t.SEZ_Bonded_Item,
                                  t.ReceiverAddress,
                                  t.TO_Office,
                                  t.From_Office,
                                  t.ReceiverID,
                                  t.Location,
                                  t.Created_By,
                                  t.userDepartmentId,
                                  t.GatepassFileName,
                                  t.GatePass_Status,

                              }).FirstOrDefault();


           
            //var OutputPath = @"D:\IMSDocs\Gatepasses\" + gatepassID +  "\\" + gatepassfilename;

            //if (!Directory.Exists(OutputPath))
            //{
            //    Directory.CreateDirectory(OutputPath);
            //}
            //else
            //{
            //    string[] oldfilename = Directory.GetFiles(@"D:\IMSDocs\Gatepasses\" + gatepassID);
            //    foreach (string file1 in oldfilename)
            //    {
            //        System.IO.File.Delete(file1);
            //    }
            //}

            bool STP_Bonded_Item = true;
            bool SEZ_Bonded_Item = true;
            DateTime? Expected_DateofReturn = null;
            string GatepassType = null;
            string Expecteddate = "";
            string today = DateTime.Now.ToString("MMM dd, yyyy");
           
            STP_Bonded_Item = Convert.ToBoolean(GatePasses.STP_Bonded_Item);
            SEZ_Bonded_Item = Convert.ToBoolean(GatePasses.SEZ_Bonded_Item);
            GatepassType = GatePasses.GatepassType;

            if (GatePasses.Expected_DateofReturn != null)
            {
                DateTime originalDate = DateTime.Parse(GatePasses.Expected_DateofReturn.ToString());
                Expecteddate = originalDate.ToString("MMM dd, yyyy");
            }

            DateTime gpDate = DateTime.Parse(GatePasses.Gatepass_DateTime.ToString());
            var GPCreatedate = gpDate.ToString("MMM dd, yyyy");
            string bondedItemText = STP_Bonded_Item ? "Yes" : "No";
            string SEZbondedItemText = SEZ_Bonded_Item ? "Yes" : "No";

            Document document = new Document(PageSize.A4, 50, 50, 25, 25);

            Font boldFont = FontFactory.GetFont("Arial", 10, Font.BOLD);
            Font simple = FontFactory.GetFont("Arial", 10);
            Font boldItalicFont = FontFactory.GetFont("Arial", 10, Font.BOLDITALIC);

            var imagepath = @"D:\IMSDocs\AllWebSites\";
            iTextSharp.text.Image png = iTextSharp.text.Image.GetInstance(imagepath + "Harbinger Group Logo.jpg");

            PdfPCell imgCell;
            PdfPTable tblImg = new PdfPTable(2);
            tblImg.WidthPercentage = 35;
            tblImg.HorizontalAlignment = Element.ALIGN_CENTER;
            tblImg.SetWidthPercentage(new float[2] { 350f, 300f }, PageSize.A4);
            imgCell = new PdfPCell();
            imgCell.HorizontalAlignment = Element.ALIGN_LEFT;
            imgCell.PaddingTop = -30f;
            imgCell.PaddingLeft = 5f;
            png.ScalePercent(12f);
            imgCell.AddElement(png); //parImg);
            imgCell.BorderWidth = PdfPCell.NO_BORDER;
            tblImg.AddCell(imgCell);

            var returnType = string.Empty;
            if (GatePasses.Expected_DateofReturn == null)
            {
                returnType = "NonReturnable Material";
            }
            else
            {
                returnType = "Returnable Material";
            }

            imgCell = new PdfPCell();
            var subtitleParagraph = new Paragraph($"GATE PASS\n({returnType})\n\n", boldFont);
            subtitleParagraph.Alignment = Element.ALIGN_RIGHT;
            imgCell.PaddingTop = -30f;
            imgCell.PaddingLeft = 60f;
            imgCell.AddElement(subtitleParagraph);
            imgCell.BorderWidth = PdfPCell.NO_BORDER;
            tblImg.AddCell(imgCell);


            Paragraph msgPhrase = new Paragraph();
            Phrase msgPhrase1 = new Phrase();
            Font lineFont = FontFactory.GetFont("Arial", 10, new iTextSharp.text.BaseColor(System.Drawing.ColorTranslator.FromHtml("#737373")));
            msgPhrase1.Add(new Chunk("_________________________________________________________________________________________", lineFont));
            msgPhrase.Add(msgPhrase1);


            PdfPTable headerTable = new PdfPTable(5);
            headerTable.WidthPercentage = 100;
            float[] columnWidths = new float[] { 1f, 1f, 1f, 1f, 1f }; // Adjust widths as needed
            headerTable.SetWidths(columnWidths);

            // Add header row
            AddCellToHeader(headerTable, "Gatepass No.");
            AddCellToHeader(headerTable, "Date");
            AddCellToHeader(headerTable, "Expected Date Of Return");
            AddCellToHeader(headerTable, "STP Bonded Items");
            AddCellToHeader(headerTable, "SEZ Bonded Items");

            // Add data row
            AddCellToBody(headerTable, GatePasses.Gatepass_Number.ToString());
            AddCellToBody(headerTable, gpDate.ToString("MMM dd, yyyy"));
            AddCellToBody(headerTable, Expecteddate);
            AddCellToBody(headerTable, bondedItemText);
            AddCellToBody(headerTable, SEZbondedItemText);

            string locationAddress = null;
            string Receivername = null;
            string Receiveraddress = null;
            string Fromname = null;
            if (GatePasses.TO_Office == null && GatePasses.From_Office == null)
            {
                if (GatePasses.Location == "SEZ" || GatePasses.Location == "Global Port" || GatePasses.Location == "Siddhant")
                {
                    var vendor = (from t in context.OfficeAddresses
                                  where t.locationName == GatePasses.Location
                                  select new { t.locationAddress });
                    foreach (var d in vendor)
                    {
                        locationAddress = d.locationAddress;
                    }
                }
                Receivername = GatePasses.ReceiverName.ToString();
                Receiveraddress = GatePasses.ReceiverAddress.ToString();
                Fromname = "HARBINGER SYSTEMS PVT.LTD.";
            }
            else
            {
                var TOoffice = (from s in context.OfficeAddresses
                                where s.locationName == GatePasses.TO_Office
                                select new { s.locationAddress, s.locationName }).FirstOrDefault();

                var FROMoffice = (from s in context.OfficeAddresses
                                  where s.locationName == GatePasses.From_Office
                                  select new { s.locationAddress, s.locationName }).FirstOrDefault();

                Fromname = FROMoffice.locationName.ToString();
                locationAddress = FROMoffice.locationAddress.ToString();

                Receivername = TOoffice.locationName.ToString();
                Receiveraddress = TOoffice.locationAddress.ToString();
            }
            //define address table
            Font titleFont = FontFactory.GetFont("Arial", 10, Font.BOLD);
            Font headerFont = FontFactory.GetFont("Arial", 11, Font.BOLD);
            Font cellFont = FontFactory.GetFont("Arial", 10);

            PdfPTable addressTable = new PdfPTable(2);
            addressTable.WidthPercentage = 100;
            addressTable.SetWidths(new float[] { 50f, 50f });

            // Create the "From" cell
            Phrase fromPhrase = new Phrase();
            fromPhrase.Leading = 35f;
            fromPhrase.Add(new Chunk("From,\n\n", titleFont));
            fromPhrase.Add(new Chunk($"{Fromname}\n", headerFont));
            fromPhrase.Add(new Chunk(locationAddress, cellFont));

            PdfPCell fromCell = new PdfPCell(fromPhrase);
            fromCell.Border = Rectangle.NO_BORDER;
            addressTable.AddCell(fromCell);

            // Create the "To" cell
            Phrase toPhrase = new Phrase();
            toPhrase.Leading = 35f;
            toPhrase.Add(new Chunk("To,\n\n", titleFont));
            toPhrase.Add(new Chunk(Receivername + "\n", headerFont));
            toPhrase.Add(new Chunk(Receiveraddress, cellFont));

            PdfPCell toCell = new PdfPCell(toPhrase);
            toCell.Border = Rectangle.NO_BORDER;
            addressTable.AddCell(toCell);

            // Create dynamic table for materials
            PdfPTable table_pdfcell = new PdfPTable(6); // Assuming 7 columns
            table_pdfcell.WidthPercentage = 100;
            table_pdfcell.SetWidths(new float[] { 1f, 1f, 2f, 1f, 2f, 2f });

            Font tableHeaderFont = FontFactory.GetFont("Arial", 10, Font.BOLD, BaseColor.WHITE);
            Font tableCellFont = FontFactory.GetFont("Arial", 9);

            // Create header cells with blue background
            AddCellToTableHeader(table_pdfcell, "Sr. No", tableHeaderFont);
            AddCellToTableHeader(table_pdfcell, "Material Name", tableHeaderFont);
            AddCellToTableHeader(table_pdfcell, "Material Description", tableHeaderFont);
            AddCellToTableHeader(table_pdfcell, "Qty.", tableHeaderFont);
            AddCellToTableHeader(table_pdfcell, "Asset No", tableHeaderFont);
            AddCellToTableHeader(table_pdfcell, "Remarks", tableHeaderFont);

            var OutMaterial = (from t in context.OutMaterials
                               join a in context.Inventory_Register on t.InventoryReg_ID equals a.InventoryReg_ID
                               where t.GatepassID == gatepassID
                               select new { t.Quantity, t.Serial_Number, t.Out_Status, t.Remarks, t.MaterialName, a.AssetID, t.Model_Number }).ToList();
            int i = 1;
            foreach (var material in OutMaterial)
            {
                var details = "Serial_number :" + material.Serial_Number + "\n" + "Model_Number :" + material.Model_Number;
                AddCellToTableBody(table_pdfcell, i.ToString(), tableCellFont);
                AddCellToTableBody(table_pdfcell, material.MaterialName, tableCellFont);
                AddCellToTableBody(table_pdfcell, details, tableCellFont);
                AddCellToTableBody(table_pdfcell, material.Quantity.ToString(), tableCellFont);
                AddCellToTableBody(table_pdfcell, material.AssetID, tableCellFont);
                AddCellToTableBody(table_pdfcell, material.Remarks, tableCellFont);
                i++;
                details = "";
            }


            Font bodyFont = FontFactory.GetFont("Arial", 10);
            Font footerFont = FontFactory.GetFont("Arial", 8);
            Font preparedByFont = FontFactory.GetFont("Arial", 10, Font.BOLD);


            Paragraph footer = new Paragraph("Please pass above material through (By Courier/By hand)", footerFont);
            footer.Alignment = Element.ALIGN_CENTER;

            string userName = null;
            var Preparedname = (from t in context.IMSUsers
                                where t.userId == GatePasses.Created_By
                                select new { t.userName }).FirstOrDefault();

            userName = Preparedname.userName.ToString();
            Paragraph preparedBy = new Paragraph($"Prepared By\n{userName}\n", preparedByFont);
            preparedBy.Alignment = Element.ALIGN_CENTER;



            // Add signature section
            PdfPTable signatureTable = new PdfPTable(3);
            signatureTable.WidthPercentage = 100;
            signatureTable.SetWidths(new float[] { 33f, 33f, 33f });

            var Authorisedname = string.Empty;
            var securityname = string.Empty;

            var authdetails = (from g in context.GatepassApprovers
                               join us in context.IMSUsers on g.employeeId equals us.userId
                               where g.Location == GatePasses.Location && g.userDepartmentId == GatePasses.userDepartmentId && g.RoleInIMS == "Inventory Incharge"
                               select new { g.userEmail, us.userName }).FirstOrDefault();

            var securitydetails = (from u in context.IMSUsers
                                   join r in context.IMSRoles on u.userId equals r.IMSuser_Id
                                   where r.userLocation == GatePasses.Location && r.userDepartmentId == GatePasses.userDepartmentId && r.RoleInIMS == "Security Guard" && r.Role_Status != "Deleted"
                                   select new { u.userEmail, u.userName }).FirstOrDefault();

            if (authdetails != null)
            {
                Authorisedname = authdetails.userName.ToString();
            }
            if (securitydetails != null)
            {
                securityname = securitydetails.userName.ToString();
            }

            if (GatePasses.GatePass_Status == "Approved" && InvInchargesignature.IMSsignaturePath != "")
            {
                AddSignatureCell(signatureTable, Authorisedname, "Authorised By", InvInchargesignature.IMSsignaturePath);
                AddSignatureCell(signatureTable, securityname, "Security", "");
                AddSignatureCell(signatureTable, GatePasses.ReceiverName, "Received By", "");
            }
            else if (GatePasses.GatePass_Status == "Dispatched" && Securitysignature.IMSsignaturePath != "")
            {
                AddSignatureCell(signatureTable, Authorisedname, "Authorised By", InvInchargesignature.IMSsignaturePath);
                AddSignatureCell(signatureTable, securityname, "Security", Securitysignature.IMSsignaturePath);
                AddSignatureCell(signatureTable, GatePasses.ReceiverName, "Received By", "");
            }


            var OutputPath = @"D:\IMSDocs\Gatepasses\" + gatepassID;
            string pdfname = string.Empty;
            string pdf = string.Empty;
            var contains = new List<string>();
            var role = Session["role"].ToString();
            var autocode = unitOfWork.DepartmentRepository.auto_generatedCode();
            if (role == "Inventory Incharge" && GatePasses.GatePass_Status == "Approved")
            {
                pdfname =  "InvIncharge" + "_" + autocode;
                contains = Directory.GetFiles(OutputPath, "*InvIncharge_*").ToList();
                contains.AddRange(Directory.GetFiles(OutputPath, "*Security_*").ToList());
            }
            else if (role == "Security Guard" && GatePasses.GatePass_Status == "Dispatched")
            {
                pdfname = "Security" + "_" + autocode;
                contains = Directory.GetFiles(OutputPath, "*Security_*").ToList();
            }

            var oldpdfname = OutputPath + "\\" + pdfname + ".pdf";
            //var contains = Directory.GetFiles(OutputPath,pdf);
            //var contains = Directory.GetFiles(OutputPath).Where(file => file.Contains("*_TLA_*") || file.Contains("*_FH_*")).ToList();

            foreach (var f in contains)
            {
                try
                {
                    using (FileStream stream = System.IO.File.Open(f, FileMode.Open, FileAccess.Read))
                    {
                        stream.Close();
                        stream.Dispose();
                    }
                    System.IO.File.Delete(f);
                }
                catch (Exception e)
                {
                    Console.Write(e);

                    TempData["errormsg"] = e.Message;
                    ViewBag.Error = e.Message;
                    var exceptionLogger = new IMSExceptionLogger()
                    {
                        controllerName = "Add_GatepassController",
                        actionName = "gatepasssignaturePDF",
                        exceptionStackTrace = e.StackTrace,
                        exceptionMessage = e.Message,
                        exceptionLogTime = DateTime.Now

                    };

                    unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                    unitOfWork.Save();
                    return false;
                }

            }


            string spacing = "<br/>";
            string dash = "_________________________________________________________________________________________________";
            //  Document document = new Document(PageSize.A4);
            PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(OutputPath + "\\" + pdfname + ".pdf", FileMode.Create));
            document.Open();

            HTMLWorker hw = new HTMLWorker(document);

            hw.Parse(new StringReader(spacing));
            document.Add(tblImg);
            document.Add(msgPhrase);

            hw.Parse(new StringReader(spacing));
            document.Add(headerTable);
            document.Add(msgPhrase);
            hw.Parse(new StringReader(spacing));
            document.Add(addressTable);
            hw.Parse(new StringReader(spacing));
            document.Add(msgPhrase);
            hw.Parse(new StringReader(spacing));
            document.Add(table_pdfcell);
            hw.Parse(new StringReader(spacing));
            document.Add(footer);
            hw.Parse(new StringReader(spacing));
            document.Add(preparedBy);
            hw.Parse(new StringReader(spacing));
            document.Add(signatureTable);
            document.Add(msgPhrase);
            hw.Parse(new StringReader(spacing));

            document.Close();

            writer.Close();

            var pdfnm = pdfname + ".pdf";
            if (role == "Inventory Incharge" && GatePasses.GatePass_Status == "Approved")
            {
                var GP_dataUpdate = (from t in context.GatePasses
                                     where t.GatepassID == gatepassID
                                     select t).SingleOrDefault();
                GP_dataUpdate.Gatepass_InvInchargeFileName = pdfnm;
                GP_dataUpdate.Gatepass_InvInchargeFileSize = new FileInfo(oldpdfname).Length.ToString();
                GP_dataUpdate.Gatepass_SecurityGuardFileName = null;
                GP_dataUpdate.Gatepass_SecurityGuardFileSize = null;
                context.SaveChanges();
            }
            else if (role == "Security Guard" && GatePasses.GatePass_Status == "Dispatched")
            {               
                var GP_dataUpdate = (from t in context.GatePasses
                                     where t.GatepassID == gatepassID
                                     select t).SingleOrDefault();
                GP_dataUpdate.Gatepass_SecurityGuardFileName = pdfnm;
                GP_dataUpdate.Gatepass_SecurityGuardFileSize = new FileInfo(oldpdfname).Length.ToString();               
                
                context.SaveChanges();
            }

            return true;
        }

        [HttpPost]
        public JsonResult SaveImage(string ImgStr, string GPID, string role)
        {

            var OutputPath = string.Empty;
            var EmpOutputPath = string.Empty;
            var tempOutputPath = string.Empty;
            GPID = unitOfWork.DepartmentRepository.Decode(GPID);
            string empid = null;
            if (Session["UserID"] != null)
            {
                empid = Session["UserID"].ToString();
            }
            var p = GPID + "_" + empid;
            tempOutputPath = AppDomain.CurrentDomain.BaseDirectory + "\\" + "TempSignature" + "\\" + p;
            string[] tempoldfilename = Directory.GetFiles(tempOutputPath);
            if (Directory.Exists(tempOutputPath))
            {
                foreach (string file in tempoldfilename)
                {
                    System.IO.File.Delete(file);
                }
            }

            if (role == "Security Guard")
            {
                OutputPath = @"D:\IMSDocs\Signature\" + GPID + "\\" + "Security Guard";
            }
            if (role == "Inventory Incharge")
            {
                OutputPath = @"D:\IMSDocs\Signature\" + GPID + "\\" + "Inventory Incharge";
            }
            var code = unitOfWork.DepartmentRepository.auto_generatedCode();
            string imageName = GPID + "_" + code + "_Signature" + ".jpg";
            string empimageName = GPID + "_" + code + "_" + empid + "_Signature" + ".jpg";
            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }
            else
            {
                if (role == "Inventory Incharge")
                {
                    string[] oldfilename = Directory.GetFiles(@"D:\IMSDocs\Signature\" + GPID + "\\" + "Inventory Incharge");
                    foreach (string file in oldfilename)
                    {
                        System.IO.File.Delete(file);
                    }
                }
                if (role == "Security Guard")
                {
                    string[] oldfilename = Directory.GetFiles(@"D:\IMSDocs\Signature\" + GPID + "\\" + "Security Guard");
                    foreach (string file in oldfilename)
                    {
                        System.IO.File.Delete(file);
                    }
                }
            }

            string base64 = ImgStr.Split(',')[1];

            //set the image path
            string imgPath = Path.Combine(OutputPath, imageName);

            byte[] imageBytes = Convert.FromBase64String(base64);

            System.IO.File.WriteAllBytes(imgPath, imageBytes);

            var r = createempsignatue(role, empid, empimageName, imageBytes);

            var result = new { userrole = role, url = imgPath, data = true };
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult PreviewImage(string ImgStr, string GPID)
        {
            var OutputPath = string.Empty;
            var EmpOutputPath = string.Empty;
            GPID = unitOfWork.DepartmentRepository.Decode(GPID);
            string empid = null;
            string role = null;

            if (Session["UserID"] != null)
            {
                empid = Session["UserID"].ToString();
            }

            if (Session["role"] != null)
            {
                role = Session["role"].ToString();
            }
            var p = GPID + "_" + empid;
            //            OutputPath = Server.MapPath("~/~/TempSignature/" + p );
            OutputPath = AppDomain.CurrentDomain.BaseDirectory + "\\" + "TempSignature" + "\\" + p;
           
            var code = unitOfWork.DepartmentRepository.auto_generatedCode();
            string imageName = GPID + "_" + code + "_TempSig" + ".jpg";

            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }
            else
            {
                string[] oldfilename = Directory.GetFiles(OutputPath);
                foreach (string file in oldfilename)
                {
                    System.IO.File.Delete(file);
                }
            }

            string base64 = ImgStr.Split(',')[1];
            //set the image path
            string imgPath = Path.Combine(OutputPath, imageName);
            byte[] imageBytes = Convert.FromBase64String(base64);
            System.IO.File.WriteAllBytes(imgPath, imageBytes);

            var r = @"TempSignature/" + p + "/" + imageName;
            var furl = ConfigurationManager.AppSettings["siteurl"] + r;
            var result = new { url = furl, tempimgpath = imgPath, data = 00 };         
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        public bool createempsignatue(string role, string empid, string empimageName, byte[] imageBytes)
        {
            string EmpOutputPath = @"D:\IMSDocs\Signature\Employeesignature\" + role + "\\" + empid;
            if (!Directory.Exists(EmpOutputPath))
            {
                Directory.CreateDirectory(EmpOutputPath);
                string empsigpath = Path.Combine(EmpOutputPath, empimageName);
                System.IO.File.WriteAllBytes(empsigpath, imageBytes);
            }
            return true;
        }

        //public ActionResult GatepassStatusUpdate(IMSEntity model, string Status)
        //{
        //    string Gatepass_ID = string.Empty;
        //    if (model.GatepassID != "" && model.GatepassID != null)
        //    {
        //        Gatepass_ID = unitOfWork.DepartmentRepository.Decode(model.GatepassID);
        //    }

        //    string role = string.Empty;
        //    string rejectReason = string.Empty;
        //    if (Session["role"].ToString() != null)
        //    {
        //        role = Session["role"].ToString();
        //    }
        //    try
        //    {

        //        string empid = string.Empty;
        //        string gpstatus = string.Empty;
        //        string gpId = string.Empty;
        //        if (Session["UserID"].ToString() != null)
        //        {
        //            empid = Session["UserID"].ToString();
        //        }

        //        switch (role)
        //        {
        //            case "Inventory Incharge":
        //                gpstatus = "Rejected by Inventory Incharge";
        //                break;
        //            case "Security Guard":
        //                gpstatus = "Rejected by Security Guard";
        //                break;
        //            case "Inventory Manager":
        //                gpstatus = "Rejected by Inventory Manager";
        //                break;
        //        }

        //        string serviceCode = string.Empty;
        //        var GatepassUpdate = new GatePass();
        //        var bvalue = string.Empty;
        //        var bremark = string.Empty;
        //        if (Gatepass_ID != "")
        //        {
        //            if (role == "Inventory Incharge" || role == "Security Guard")
        //            {


        //                gpstatus = Status;
        //                GatepassUpdate = (from t in context.GatePasses
        //                                  where t.GatepassID == Gatepass_ID && !t.GatePass_Status.Equals("Deleted")
        //                                  select t).SingleOrDefault();

        //                GatepassUpdate.GatePass_Status = Status;

        //                if(role == "Inventory Incharge")
        //                {
        //                    GatepassUpdate.Approved_By = empid;
        //                }
        //                else if(role == "Security Guard")
        //                {
        //                    GatepassUpdate.Approved_By = empid;
        //                }                      
        //                context.SaveChanges();

        //                List<IMS_Signature> exist_sign = (from t in context.IMS_Signature
        //                                              where t.IMSsignatureStatus != "Deleted"
        //                                              select t).ToList();
        //                foreach (var s in exist_sign)
        //                {
        //                    if (s.referenceId == GatepassUpdate.GatepassID && s.categoryName == "Gatepass" && s.role == role)
        //                    {
        //                        s.IMSsignatureStatus = "Deleted";
        //                        context.SaveChanges();
        //                    }
        //                }
        //                var gatepass = (from t in context.IMS_Signature
        //                                where t.userId.Contains(empid) && t.IMSsignatureStatus != "Deleted"
        //                                select t);
        //                var employeeid = string.Empty;
        //                if (gatepass.Count() == 0)
        //                {
        //                    employeeid = empid;
        //                }


        //                var signature = new IMS_Signature();
        //                if (employeeid == "")
        //                {
        //                    signature = new IMS_Signature()
        //                    {
        //                        IMSsignatureId = unitOfWork.DepartmentRepository.auto_generatedCode(),
        //                        categoryName = "Gatepass",
        //                        referenceId = GatepassUpdate.GatepassID,
        //                        role = role,
        //                        IMSsignaturePath = model.signaturePath
        //                    };
        //                }
        //                else
        //                {
        //                    signature = new IMS_Signature()
        //                    {
        //                        IMSsignatureId = unitOfWork.DepartmentRepository.auto_generatedCode(),
        //                        categoryName = "Gatepass",
        //                        referenceId = GatepassUpdate.GatepassID,
        //                        role = role,
        //                        IMSsignaturePath = model.signaturePath,
        //                        userId = employeeid
        //                    };
        //                }
        //                unitOfWork.DepartmentRepository.Insert(signature);
        //                unitOfWork.Save();
        //            }

        //            var pdf = gatepasssignaturePDF(Gatepass_ID);

        //            OutwardMaterial outward = new OutwardMaterial();
        //            outward.OW_MaterialID = unitOfWork.DepartmentRepository.auto_generatedCode();
        //            outward.GatepassID = Gatepass_ID;
        //            outward.OutwardDatetime = DateTime.Now;
        //            if(GatepassUpdate.GatepassType == "NonReturnable")
        //            {
        //                outward.Outward_Status = "Closed";
        //            }
        //            else
        //            {
        //                outward.Outward_Status = "Material Out";
        //            }

        //            unitOfWork.DepartmentRepository.Insert(outward);
        //            unitOfWork.Save();

        //            exceptionLogger.LogCreationForGatepassStatus(Gatepass_ID, "change status Approved");  

        //        }
        //        else
        //        {                   
        //            model.rejectGatepassId = unitOfWork.DepartmentRepository.Decode(model.rejectGatepassId);
        //            Gatepass_ID = model.rejectGatepassId;
        //            var GP_Update = (from t in context.GatePasses
        //                             where t.GatepassID == model.rejectGatepassId
        //                             select t).SingleOrDefault();

        //            GP_Update.GatePass_Status = gpstatus;
        //            GP_Update.GatepassRejectReason = model.GatepassRejectReason;
        //            GP_Update.Rejected_By = empid;
        //            context.SaveChanges();

        //            var outmateriallist = (from o in context.OutMaterials
        //                                   where o.GatepassID == Gatepass_ID
        //                                   select o).ToList();
        //            foreach(var a in outmateriallist)
        //            {
        //                a.Out_Status = "Rejected";
        //            }
        //            context.SaveChanges();

        //            foreach(var ii in outmateriallist)
        //            {
        //                var inventorydetails = (from inv in context.Inventory_Register
        //                                        where inv.InventoryReg_ID == ii.InventoryReg_ID
        //                                        select inv).SingleOrDefault();
        //                inventorydetails.IR_Status = "Material In";
        //                context.SaveChanges();                        
        //            }
        //            exceptionLogger.LogCreationForGatepassStatus(Gatepass_ID, "change status Rejected");
        //        }

        //        var encodedgatepassID = unitOfWork.DepartmentRepository.Encode(Gatepass_ID);
        //       return RedirectToAction("GatepassDetails", "Add_Gatepass", new { @GatepassID = encodedgatepassID});

        //    }
        //    catch (Exception e)
        //    {
        //        string serviceCode = string.Empty;
        //        var exceptionLogger = new IMSExceptionLogger()
        //        {
        //            controllerName = "Add_GatepassController",
        //            actionName = "GatepassStatusUpdate",
        //            exceptionStackTrace = e.StackTrace,
        //            exceptionMessage = e.Message,
        //            exceptionLogTime = DateTime.Now

        //        };
        //        unitOfWork.DepartmentRepository.Insert(exceptionLogger);
        //        unitOfWork.Save();
        //        var encodedgatepassID = unitOfWork.DepartmentRepository.Encode(Gatepass_ID);
        //        return RedirectToAction("GatepassDetails", "Add_Gatepass", new { @GatepassID = encodedgatepassID });
        //    }
        //}

        public ActionResult GatepassStatusUpdate(IMSEntity model, string Status)
        {
            string Gatepass_ID = string.Empty;
            if (model.GatepassID != "" && model.GatepassID != null)
            {
                Gatepass_ID = unitOfWork.DepartmentRepository.Decode(model.GatepassID);
            }

            string role = string.Empty;
            string rejectReason = string.Empty;
            if (Session["role"].ToString() != null)
            {
                role = Session["role"].ToString();
            }

            try
            {
                string empid = string.Empty;
                string gpstatus = string.Empty;
                string gpId = string.Empty;
                string loc = string.Empty;

                if (Session["Location"].ToString() == "Global Port")
                {
                    loc = "Global Port";
                }

                else if (Session["Location"].ToString() == "SEZ")
                {
                    loc = "SEZ";
                }

                else if (Session["Location"].ToString() == "Siddhant")
                {
                    loc = "Siddhant";
                }

                if (Session["UserID"].ToString() != null)
                {
                    empid = Session["UserID"].ToString();
                }

                switch (role)
                {
                    case "Inventory Incharge":
                        gpstatus = "Rejected by Inventory Incharge";
                        break;

                    case "Security Guard":
                        gpstatus = "Rejected by Security Guard";
                        break;

                    case "Inventory Manager":
                        gpstatus = "Rejected by Inventory Manager";
                        break;

                }

                string serviceCode = string.Empty;
                var GatepassUpdate = new GatePass();
                var bvalue = string.Empty;
                var bremark = string.Empty;

                if (Gatepass_ID != "")
                {
                    if (role == "Inventory Incharge" || role == "Security Guard")
                    {

                        gpstatus = Status;
                        GatepassUpdate = (from t in context.GatePasses
                                          where t.GatepassID == Gatepass_ID && !t.GatePass_Status.Equals("Deleted")
                                          select t).SingleOrDefault();

                        GatepassUpdate.GatePass_Status = Status;

                        if (role == "Inventory Incharge")
                        {
                            GatepassUpdate.Approved_By = empid;
                        }

                        else if (role == "Security Guard")
                        {
                            //GatepassUpdate.Approved_By = empid;
                            GatepassUpdate.Gatepass_DispatchBy = model.Gatepass_DispatchBy;
                        }
                        context.SaveChanges();

                        List<IMS_Signature> exist_sign = (from t in context.IMS_Signature
                                                          where t.IMSsignatureStatus != "Deleted"
                                                          select t).ToList();

                        foreach (var s in exist_sign)
                        {

                            if (s.referenceId == GatepassUpdate.GatepassID && s.categoryName == "Gatepass" && s.role == role)
                            {
                                s.IMSsignatureStatus = "Deleted";
                                context.SaveChanges();
                            }
                        }

                        var gatepass = (from t in context.IMS_Signature
                                        where t.userId.Contains(empid) && t.IMSsignatureStatus != "Deleted"
                                        select t);

                        var employeeid = string.Empty;

                        if (gatepass.Count() == 0)
                        {
                            employeeid = empid;
                        }


                        var signature = new IMS_Signature();
                        if (employeeid == "")
                        {
                            signature = new IMS_Signature()
                            {
                                IMSsignatureId = unitOfWork.DepartmentRepository.auto_generatedCode(),
                                categoryName = "Gatepass",
                                referenceId = GatepassUpdate.GatepassID,
                                role = role,
                                IMSsignaturePath = model.signaturePath
                            };
                        }

                        else
                        {
                            signature = new IMS_Signature()
                            {
                                IMSsignatureId = unitOfWork.DepartmentRepository.auto_generatedCode(),
                                categoryName = "Gatepass",
                                referenceId = GatepassUpdate.GatepassID,
                                role = role,
                                IMSsignaturePath = model.signaturePath,
                                userId = employeeid
                            };

                        }

                        unitOfWork.DepartmentRepository.Insert(signature);
                        unitOfWork.Save();
                        if(role == "Inventory Incharge")
                        {
                            bool sendmail = sendGatepassMail(model.GatepassID, "Approved by Approver");
                        }
                    }

                    var pdf = gatepasssignaturePDF(Gatepass_ID);

                    if (role == "Security Guard" && Status == "Dispatched")
                    {
                        OutwardMaterial outward = new OutwardMaterial();
                        outward.OW_MaterialID = unitOfWork.DepartmentRepository.auto_generatedCode();
                        var result = GenerateOutwardNo(loc);
                        outward.OutwardNo = result.Assetid;
                        outward.GatepassID = Gatepass_ID;
                        outward.OutwardDatetime = DateTime.Now;

                        if (GatepassUpdate.GatepassType == "NonReturnable")
                        {
                            outward.Outward_Status = "Closed";
                        }
                        else
                        {
                            outward.Outward_Status = "Material Out";
                        }

                        unitOfWork.DepartmentRepository.Insert(outward);
                        unitOfWork.Save();

                        var CategoryUpdate = (from t in context.Serial_Number
                                              where t.Year_Range == result.year_range && t.Location == loc
                                              select t).SingleOrDefault();

                        CategoryUpdate.Serial_Outward_Number = result.FinalNumber;
                        context.SaveChanges();
                        bool sendmail = sendGatepassMail(model.GatepassID, "Material Dispatched");
                    }
                    exceptionLogger.LogCreationForGatepassStatus(Gatepass_ID, "change status Approved");

                }
                else
                {

                    model.rejectGatepassId = unitOfWork.DepartmentRepository.Decode(model.rejectGatepassId);
                    Gatepass_ID = model.rejectGatepassId;

                    var GP_Update = (from t in context.GatePasses
                                     where t.GatepassID == model.rejectGatepassId
                                     select t).SingleOrDefault();

                    GP_Update.GatePass_Status = gpstatus;
                    GP_Update.GatepassRejectReason = model.GatepassRejectReason;
                    GP_Update.Rejected_By = empid;
                    context.SaveChanges();

                    var outmateriallist = (from o in context.OutMaterials
                                           where o.GatepassID == Gatepass_ID
                                           select o).ToList();

                    foreach (var a in outmateriallist)
                    {
                        a.Out_Status = "Rejected";
                    }
                    context.SaveChanges();

                    foreach (var ii in outmateriallist)
                    {

                        var inventorydetails = (from inv in context.Inventory_Register
                                                where inv.InventoryReg_ID == ii.InventoryReg_ID
                                                select inv).SingleOrDefault();
                        inventorydetails.IR_Status = "Material In";
                        context.SaveChanges();
                    }
                    bool sendmail = sendGatepassMail(model.GatepassID, "Gatepass Rejected");
                    exceptionLogger.LogCreationForGatepassStatus(Gatepass_ID, "change status Rejected");

                }

                var encodedgatepassID = unitOfWork.DepartmentRepository.Encode(Gatepass_ID);
                return RedirectToAction("GatepassDetails", "Add_Gatepass", new { @GatepassID = encodedgatepassID });
            }

            catch (Exception e)
            {

                string serviceCode = string.Empty;
                var exceptionLogger = new IMSExceptionLogger()
                {
                    controllerName = "Add_GatepassController",
                    actionName = "GatepassStatusUpdate",
                    exceptionStackTrace = e.StackTrace,
                    exceptionMessage = e.Message,
                    exceptionLogTime = DateTime.Now
                };

                unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                unitOfWork.Save();

                var encodedgatepassID = unitOfWork.DepartmentRepository.Encode(Gatepass_ID);
                return RedirectToAction("GatepassDetails", "Add_Gatepass", new { @GatepassID = encodedgatepassID });

            }

        }

        public MyJsonObject GenerateOutwardNo(string Location)
        {

            string empid = null;
            string loc = string.Empty;
            string outward = string.Empty;
            if (Location == "Global Port")
            {
                loc = "GP";
            }

            else if (Location == "SEZ")
            {
                loc = "SEZ";
            }
            else if (Location == "Siddhant")
            {
                loc = "SIDD";
            }

            if (Session["UserID"] != null)
            {
                empid = Session["UserID"].ToString();
            }

            int start_date_month = DateTime.Now.Month;
            string start_date_year = DateTime.Now.Year.ToString();
            string year_range = string.Empty;
            string financialyear = string.Empty;
            var serial_Number = new Serial_Number();
            string categoryNumber = string.Empty;

            if ((start_date_month > 3 && start_date_year == "2024") || (start_date_month < 3) && start_date_year == "2025")
            {
                year_range = "2024-25";
                financialyear = "24-25";
                categoryNumber = unitOfWork.DepartmentRepository.Update(serial_Number, "Outward", year_range, Location);

            }
            else
            {
                year_range = "2023-24";
                financialyear = "23-24";
                categoryNumber = unitOfWork.DepartmentRepository.Update(serial_Number, "Outward", year_range, Location);

            }

            var rem = 0;
            var suffixpo = string.Empty;
            int num = int.Parse(categoryNumber);
            num = num + 1;
            var finalnumber = int.Parse(categoryNumber) + 1;
            int count = 0;

            while (num != 0)
            {
                rem = num % 10;
                count++;
                num = num / 10;
            }

            switch (count)
            {
                case 1:
                    suffixpo = "0000";
                    break;

                case 2:
                    suffixpo = "000";
                    break;

                case 3:
                    suffixpo = "00";
                    break;

                case 4:
                    suffixpo = "0";
                    break;

                case 5:
                    suffixpo = "";
                    break;

                default:
                    suffixpo = "";
                    break;

            }

            if (!empid.Contains("DUM"))
            {
                outward = "OUT/" + loc + "/" + financialyear + "/" + suffixpo + finalnumber;
            }
            else
            {
                outward = "DUMMYASSETID";
            }

            MyJsonObject myJsonObject = new MyJsonObject();
            if (empid.Contains("DUM"))
            {
                finalnumber = finalnumber - 1;                
                return new MyJsonObject { Assetid = outward, FinalNumber = finalnumber, year_range = year_range };
            }
            else
            {
                return new MyJsonObject { Assetid = outward, FinalNumber = finalnumber, year_range = year_range };

            }

        }

        public JsonResult verifyuserpassword(string userpassword)
        {
            ServiceVMSEntities serviceContext = new ServiceVMSEntities();
            string empid = null;
            if (Session["UserID"].ToString() != null)
            {
                empid = Session["UserID"].ToString();
            }
            IMSUser user = new IMSUser();
            var useremail = unitOfWork.DepartmentRepository.GetUserEmail(user, empid);
            //bool isLDAP = LDAP_Connection(useremail, userpassword);
            bool isvalid_User = serviceContext.IMSUsers.Any(x => x.userEmail == useremail && x.userPassword == userpassword);

            //if (isLDAP)
            //{
            //    var result = new { data = true };
            //    logger.LogCreationForExceptions("isLDAP_Verifypassword" + isLDAP);
            //    return Json(result, JsonRequestBehavior.AllowGet);
            //}
            //else if (isvalid_User)
            if (isvalid_User)
            {
                var result = new { data = true };
                logger.LogCreationForExceptions("custompassword_isvalid_User" + isvalid_User);
                return Json(result, JsonRequestBehavior.AllowGet);
            }

            var jsonresult = new { data = false };
            
            return Json(jsonresult, JsonRequestBehavior.AllowGet);

        }

        public bool LDAP_Connection(string userEmail, string userPassword)
        {
            try
            {
                LdapDirectoryIdentifier ldi = new LdapDirectoryIdentifier("10.0.2.228", 389);
                System.DirectoryServices.Protocols.LdapConnection ldapConnection = new System.DirectoryServices.Protocols.LdapConnection(ldi);
                Console.WriteLine("LdapConnection is created successfully.");
                ldapConnection.SessionOptions.ProtocolVersion = 3;
                ldapConnection.SessionOptions.SecureSocketLayer = false;
                ldapConnection.SessionOptions.VerifyServerCertificate += delegate { return true; };
                ldapConnection.AuthType = AuthType.Basic;
                NetworkCredential nc = new NetworkCredential(userEmail, userPassword);
                ldapConnection.Bind(nc);

                Console.WriteLine("LdapConnection authentication success");
                ldapConnection.Dispose();

                return true;
            }
            catch (Exception e)
            {
                string data = "LDAP_Connection :" + e.Message + " : " + e.StackTrace;
                
                return false;
            }


        }

        public JsonResult PickSignature(string gpid, string role)
        {
            string emp_id = string.Empty;
            gpid = unitOfWork.DepartmentRepository.Decode(gpid);
            if (Session["UserID"].ToString() != null)
            {
                emp_id = Session["UserID"].ToString();
            }

            var POReport = (from t in context.IMS_Signature
                            where t.userId.Contains(emp_id) && t.IMSsignatureStatus != "Deleted" && t.role == role && t.categoryName == "Gatepass"
                            select t).FirstOrDefault();

            var imgPath = string.Empty;
            imgPath = POReport.IMSsignaturePath;

            byte[] imgbytes = System.IO.File.ReadAllBytes(imgPath);
            string base64string = Convert.ToBase64String(imgbytes);
            var imagetype = "data:image/png;base64";
            base64string = imagetype + ',' + base64string;
            var urlstring = PreviewImage(base64string, unitOfWork.DepartmentRepository.Encode(gpid));

            var result = new { userrole = role, imgurl = urlstring.Data, baseimgurl = base64string, data = 00 };
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
public ActionResult DeleteGatepass(string id)
        {
            if (@Session["Email"] != null)
            {
                try
                {
                    id = unitOfWork.DepartmentRepository.Decode(id);
                    ServiceVMSEntities context = new ServiceVMSEntities();
                    string userid = string.Empty;


                    ViewBag.role = Session["role"].ToString();
                    if (Session["Location"] != null)
                    {
                        ViewBag.location = Session["Location"].ToString();
                    }


                    if (Session["UserID"] != null)
                    {
                        userid = Session["UserID"].ToString();
                    }
                    GatePass gatepass_delete = context.GatePasses.Single(x => x.GatepassID == id);
                    var deliveryChallen = context.DeliveryChallens.Where(x => x.InwardID == id);
                    unitOfWork.DepartmentRepository.DeleteGatepass(gatepass_delete);
                    exceptionLogger.LogCreationForGatepass(id, "Deleted", null, null);

                }
                catch (Exception e)
                {
                    Console.Write(e); var exceptionLogger = new IMSExceptionLogger()
                    {
                        controllerName = "Add_GatepassController",
                        actionName = "DeleteGatepass",
                        exceptionStackTrace = e.StackTrace,
                        exceptionMessage = e.Message,
                        exceptionLogTime = DateTime.Now
                    };
                    unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                    unitOfWork.Save();
                }
            }

            var encodedServiceCode = unitOfWork.DepartmentRepository.Encode(id);
            return RedirectToAction("GatepassList", "Add_Gatepass", new { id = encodedServiceCode });
        }

        public ActionResult PrintPDF(string gatepassID)
        {
            gatepassID = unitOfWork.DepartmentRepository.Decode(gatepassID);
            string baseDirectory = @"D:\IMSDocs\Gatepasses\";
            string folderPath = Path.Combine(baseDirectory, gatepassID);

            if (!Directory.Exists(folderPath))
            {
                return HttpNotFound("Directory not found.");
            }

            var pdfFiles = Directory.GetFiles(folderPath, "*.pdf");

            if (pdfFiles.Length == 0)
            {
                return HttpNotFound("No PDF files found.");
            }

            var latestFile = pdfFiles
                .Select(file => new FileInfo(file))
                .OrderByDescending(fileInfo => fileInfo.LastWriteTime)
                .First();

            if (!System.IO.File.Exists(latestFile.FullName))
            {
                return HttpNotFound("PDF not found.");
            }

            byte[] fileBytes = System.IO.File.ReadAllBytes(latestFile.FullName);

            return File(fileBytes, "application/pdf");
        }
        public ActionResult DownloadPDF(string gatepassID)
        {
            try
            {
                gatepassID = unitOfWork.DepartmentRepository.Decode(gatepassID);
                string filePath = GetFilePath(gatepassID);
                if (System.IO.File.Exists(filePath))
                {
                    byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                    string fileName = Path.GetFileName(filePath);
                    return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Pdf, fileName);
                }
                else
                {
                    return HttpNotFound();
                }
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, "Internal Server Error");
            }
        }

        private string GetFilePath(string gatepassID)
        {
            string directoryPath = @"D:\IMSDocs\Gatepasses\" + gatepassID;
            if (Directory.Exists(directoryPath))
            {
                string latestPdfFile = System.IO.Directory.GetFiles(directoryPath, "*.pdf")
                                            .OrderByDescending(f => System.IO.File.GetCreationTime(f))
                                            .FirstOrDefault();

                return latestPdfFile;
            }
            return null;
        }
       
    }
}