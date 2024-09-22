using IMS.BLL;
using IMS.BLL.Interfaces;
using IMS.DAL;
using IMS.Entities;
using IMS.Models;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Configuration;
using System.Data.Entity;
using System.Threading.Tasks;

namespace IMS.Web.Controllers
{
    public class Add_OutwardMaterialController : Controller
    {
        // GET: Add_OutwardMaterial
        private ServiceVMSEntities context = new ServiceVMSEntities();
        private UnitOfWork unitOfWork = new UnitOfWork();
        private IMExceptionLogger exceptionLogger = BLLObjectCreator.CreateIMSLogger(ExceptionLoggerType.IMSText);

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult OutwardList(string sortOrder, string searchString, string Name, string SearchValue1, string SearchValue2, string SearchValue, string SearchValue_ExpenseN, FormCollection form, DateTime? searchdate, int? Page_no)
        {
            string role = null;
            if (Session["role"] != null)
            {
                role = Session["role"].ToString();
            }
            if (role != null)
            {
                string location = null;
                int pagesize = 20;
                int pagenumber = (Page_no ?? 1);
                ViewBag.page = Page_no;

                int countForsearch = 0;
                string display = "0";
                ViewBag.display = display;
                int count = 0;
                string empid = null;
                DateTime val = DateTime.Now;

                if (searchdate.HasValue)
                {
                    val = searchdate ?? DateTime.Now;

                }
                if (Session["Location"] != null)
                {
                    location = Session["Location"].ToString();
                    ViewBag.location = Session["Location"].ToString();
                }

                ViewBag.Outward_no = sortOrder == "outwardNo_asc" ? "outwardNo_desc" : "outwardNo_asc";
                ViewBag.Gatepass_Number = sortOrder == "gatepassNo_asc" ? "gatepassNo_desc" : "gatepassNo_asc";
                ViewBag.Department = sortOrder == "dept_asc" ? "dept_desc" : "dept_asc";
                ViewBag.Location = sortOrder == "loc_asc" ? "loc_desc" : "loc_asc";
                ViewBag.OutwardDatetime = sortOrder == "datetime_asc" ? "datetime_desc" : "datetime_asc";
                ViewBag.ReceiverName = sortOrder == "receiver_asc" ? "receiver_desc" : "receiver_asc";
                ViewBag.OutwardStatus = sortOrder == "status_asc" ? "status_desc" : "status_asc";

                if (Session["UserID"] != null)
                {
                    empid = Session["UserID"].ToString();
                    ViewBag.userid = empid;
                }


                string dept = null;

                if (Session["DepartmentID"] != null)
                {
                    dept = Session["DepartmentID"].ToString();

                }
                ViewBag.role = role;
                var imsentity = new List<IMSEntity>();


                //ViewBag.empid = empid;
                try
                {
                    ServiceVMSEntities context = new ServiceVMSEntities();
                    var serviceuser = new ServiceUserDepartment();
                    var name_department = unitOfWork.DepartmentRepository.Get(serviceuser, int.Parse(dept));
                    string deptName = name_department[0].userDepartmentName;




                    var OutwardDetails = (from t in context.OutwardMaterials
                                          join g in context.GatePasses on t.GatepassID equals g.GatepassID
                                          join d in context.ServiceUserDepartments on g.userDepartmentId equals d.userDepartmentId
                                          where t.Outward_Status != "Deleted"
                                          select new
                                          {
                                              t.OW_MaterialID,
                                              t.Outward_Status,
                                              t.OutwardDatetime,
                                              t.OutwardNote,
                                              g.Location,
                                              g.userDepartmentId,
                                              g.GatepassType,
                                              g.Gatepass_Number,
                                              g.ReceiverID,
                                              g.ReceiverName,
                                              d.userDepartmentName,
                                              t.OutwardNo
                                          });
                    if (empid != null)
                    {
                        if (empid.Contains("DUM"))
                        {
                            if (role == "Inventory Manager")
                            {
                                OutwardDetails = OutwardDetails.Where(u => u.OW_MaterialID.Contains("DUM"));

                            }
                            else
                            {
                                OutwardDetails = OutwardDetails.Where(u => u.OW_MaterialID.Contains("DUM") && u.userDepartmentId == int.Parse(dept));
                            }


                        }
                        else
                        {
                            var cc = OutwardDetails.Count();
                            int deptid = int.Parse(dept);
                            if (role == "Administrator")
                            {
                                OutwardDetails = OutwardDetails.Where(u => !u.OW_MaterialID.Contains("DUM"));
                            }
                            else if (role == "Inventory Manager")
                            {
                                OutwardDetails = OutwardDetails.Where(u => !u.OW_MaterialID.Contains("DUM") && u.userDepartmentId == deptid);
                            }
                            else if (role == "Security Guard")
                            {
                                OutwardDetails = OutwardDetails.Where(u => !u.OW_MaterialID.Contains("DUM") && u.Location == location);
                            }
                            else if (role == "Inventory Incharge" || role == "Receiver")
                            {
                                OutwardDetails = OutwardDetails.Where(u => !u.OW_MaterialID.Contains("DUM") && u.Location == location && u.userDepartmentId == deptid);
                            }
                            else
                            {

                                OutwardDetails = OutwardDetails.Where(u => !u.OW_MaterialID.Contains("DUM") && u.userDepartmentId == deptid);
                            }
                        }
                    }
                    var c = OutwardDetails.Count();

                    if (Name == "actionable")
                    {
                        if (role == "Security Guard")
                        {
                            OutwardDetails = OutwardDetails.Where(u => u.Outward_Status == "Material Out");
                        }
                    }
                    if (!String.IsNullOrEmpty(SearchValue) || !String.IsNullOrEmpty(SearchValue_ExpenseN) || !String.IsNullOrEmpty(SearchValue1) || searchdate != null)
                    {
                        String Searchby = form["Name"].ToString();

                        if (Searchby == "Outward_id")
                        {
                            countForsearch = 1;
                            OutwardDetails = OutwardDetails.Where(u => u.OW_MaterialID.Contains(SearchValue));
                        }
                        else if (Searchby == "Outward_department")
                        {
                            countForsearch = 1;
                            int DepartmentName;
                            var InwardValue = (from i in context.ServiceUserDepartments
                                               where i.userDepartmentName == SearchValue1
                                               select new { i.userDepartmentId }).ToList();
                            foreach (var t in InwardValue)
                            {
                                DepartmentName = t.userDepartmentId;
                                OutwardDetails = OutwardDetails.Where(u => u.userDepartmentId == DepartmentName);
                            }
                        }
                        else if (Searchby == "Outward_Location")
                        {
                            countForsearch = 1;
                            OutwardDetails = OutwardDetails.Where(u => u.Location.Contains(SearchValue));
                        }
                        else if (Searchby == "OutwardStatus")
                        {
                            countForsearch = 1;
                            OutwardDetails = OutwardDetails.Where(u => u.Outward_Status.Contains(SearchValue));
                        }
                        else if (Searchby == "OutwardDatetime" && (searchdate != null))
                        {
                            countForsearch = 1;
                            val = val.Date;
                            OutwardDetails = OutwardDetails.Where(u => u.OutwardDatetime == (val));
                        }
                    }

                    switch (sortOrder)
                    {
                        case "outwardNo_desc":
                            OutwardDetails = OutwardDetails.OrderByDescending(o => o.OutwardNo);
                            break;
                        case "outwardNo_asc":
                            OutwardDetails = OutwardDetails.OrderBy(o => o.OutwardNo);
                            break;
                        case "gatepassNo_asc":
                            OutwardDetails = OutwardDetails.OrderBy(o => o.Gatepass_Number);
                            break;
                        case "gatepassNo_desc":
                            OutwardDetails = OutwardDetails.OrderByDescending(o => o.Gatepass_Number);
                            break;
                        case "dept_asc":
                            OutwardDetails = OutwardDetails.OrderBy(o => o.userDepartmentName);
                            break;
                        case "dept_desc":
                            OutwardDetails = OutwardDetails.OrderByDescending(o => o.userDepartmentName);
                            break;
                        case "loc_asc":
                            OutwardDetails = OutwardDetails.OrderBy(o => o.Location);
                            break;
                        case "loc_desc":
                            OutwardDetails = OutwardDetails.OrderByDescending(o => o.Location);
                            break;
                        case "datetime_asc":
                            OutwardDetails = OutwardDetails.OrderBy(o => o.OutwardDatetime);
                            break;
                        case "datetime_desc":
                            OutwardDetails = OutwardDetails.OrderByDescending(o => o.OutwardDatetime);
                            break;
                        case "receiver_asc":
                            OutwardDetails = OutwardDetails.OrderBy(o => o.ReceiverName);
                            break;
                        case "receiver_desc":
                            OutwardDetails = OutwardDetails.OrderByDescending(o => o.ReceiverName);
                            break;
                        case "status_asc":
                            OutwardDetails = OutwardDetails.OrderBy(o => o.Outward_Status);
                            break;
                        case "status_desc":
                            OutwardDetails = OutwardDetails.OrderByDescending(o => o.Outward_Status);
                            break;
                        default:
                            OutwardDetails = OutwardDetails.OrderByDescending(o => o.OutwardDatetime); // Default sort by Outward_No
                            break;

                    }

                    foreach (var t in OutwardDetails)
                    {

                        imsentity.Add(new IMSEntity()
                        {
                            OW_MaterialID = t.OW_MaterialID,
                            userDepartmentId = (int)(t != null ? t.userDepartmentId : 0),
                            userDepartmentName = t != null ? t.userDepartmentName : null,
                            Location = t.Location,
                            Gatepass_Number = t.Gatepass_Number,
                            OutwardDatetime = t.OutwardDatetime,
                            OutwardNote = t.OutwardNote,
                            GatepassType = t.GatepassType,
                            ReceiverID = t.ReceiverID,
                            ReceiverName = t.ReceiverName,
                            Outward_Status = t.Outward_Status,
                            OutwardNo = t.OutwardNo,
                        });
                    }

                    ViewBag.count = count;
                    ViewBag.countForsearch = countForsearch;
                    /*  return View(imsentity);*/
                    return View(imsentity.ToPagedList(pagenumber, pagesize));
                }
                catch (Exception e)
                {

                    Console.Write(e);

                    var exceptionLogger = new IMSExceptionLogger()
                    {
                        controllerName = "Add_InwardMaterialController",
                        actionName = "InwardList",
                        exceptionStackTrace = e.StackTrace,
                        exceptionMessage = e.Message,
                        exceptionLogTime = DateTime.Now

                    };
                    unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                    unitOfWork.Save();
                }


                /* return View(imsentity);*/
                return View(imsentity.ToPagedList(pagenumber, pagesize));
            }
            else
            {
                return HttpNotFound();
            }
        }

        public ActionResult OutwardDetails(string id)
        {
            try
            {
                if (Session["Email"] != null)
                {
                    string role = Session["role"].ToString();
                    ViewBag.role = role;
                    ServiceVMSEntities context = new ServiceVMSEntities();

                    var outwardDetail = (from t in context.OutwardMaterials
                                         join g in context.GatePasses on t.GatepassID equals g.GatepassID
                                         join d in context.ServiceUserDepartments on g.userDepartmentId equals d.userDepartmentId
                                         where t.OW_MaterialID == id && t.Outward_Status != "Deleted"
                                         select new IMSEntity
                                         {

                                             OW_MaterialID = t.OW_MaterialID,
                                             GatepassID = g.GatepassID,
                                             userDepartmentId = (int)g.userDepartmentId,
                                             userDepartmentName = d.userDepartmentName,
                                             Location = g.Location,
                                             Gatepass_Number = g.Gatepass_Number,
                                             OutwardDatetime = t.OutwardDatetime,
                                             ReceiverID = g.ReceiverID,
                                             ReceiverName = g.ReceiverName,
                                             OutwardNo = t.OutwardNo,
                                             GatepassType = g.GatepassType
                                         }).FirstOrDefault();

                    if (outwardDetail == null)
                    {
                        return HttpNotFound("Outward Material not found.");
                    }

                    var materialItems = (from om in context.OutMaterials
                                         join ir in context.Inventory_Register on om.InventoryReg_ID equals ir.InventoryReg_ID
                                         where om.GatepassID == outwardDetail.GatepassID
                                         select new MaterialItem
                                         {

                                             AssetId = ir.AssetID,
                                             ExpenseNature = ir.ExpenseNature,
                                             MaterialName = ir.MaterialName,
                                             ModelNo = ir.Model_Number,
                                             SerialNo = ir.Serial_No,
                                             GRN_Number = ir.GRN_Number,
                                             OutStatus = om.Out_Status,
                                             OutMat_ID = om.OutMat_ID,
                                         }).ToList();

                    var viewModel = new OutwardDetailViewModel
                    {
                        OutwardDetail = outwardDetail,
                        MaterialItems = materialItems
                    };

                    // List<string> sampleOutMatIds = materialItems.Select(m => m.OutMatID).ToList(); // Replace with actual OutMat_IDs
                    // string sampleGatepassID = outwardDetail.GatepassID;

                    // // Call the MarkAsReceived method
                    //bool result = MarkAsReceived(sampleOutMatIds, sampleGatepassID);

                    // // Handle result if needed
                    //ViewBag.MarkAsReceivedStatus = result ? "Success" : "Failed";
                    return View(viewModel);

                }

                else

                {

                    return RedirectToAction("RedirectToLogin", "Authentication");

                }

            }

            catch (Exception ex)

            {

                // Log exception here

                var exceptionLogger = new IMSExceptionLogger()
                {

                    controllerName = "Add_OutwardMaterialController",
                    actionName = "GetOutwardDetailById",
                    exceptionStackTrace = ex.StackTrace,
                    exceptionMessage = ex.Message,
                    exceptionLogTime = DateTime.Now
                };

                unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                unitOfWork.Save();
                return View("Error");

            }

        }


        [HttpPost]
        public async Task<ActionResult> MarkAsReceived(string[] outMatIds, string gatepassID)
        {
            try
            {
                // Validate input
                if (outMatIds == null || !outMatIds.Any() || string.IsNullOrEmpty(gatepassID))
                {
                    return Json(new { message = "Invalid input: Material IDs or Gatepass ID missing" });
                }

                // Fetch the gatepass
                var gatepass = await context.GatePasses.FirstOrDefaultAsync(g => g.GatepassID == gatepassID);
                if (gatepass == null || gatepass.GatepassType != "Returnable")
                {
                    return Json(new { message = "Invalid gatepass: Either not found or not returnable" });
                }

                // Uncomment and use userLocation if needed
                var userLocation = Session["userLocation"] as string;
                if (!(gatepass.Location != userLocation))
                {
                    return Json(new { message = "Location mismatch: You do not have access to this gatepass" }, JsonRequestBehavior.AllowGet);
                }

                // Fetch outward materials
                var outwardMaterials = await context.OutMaterials
                    .Where(om => outMatIds.Contains(om.OutMat_ID))
                    .ToListAsync();

                if (!outwardMaterials.Any())
                {
                    return Json(new { message = "No materials found for the provided IDs" });
                }

                // Update Out_Status for materials and Inventory_Register IR_Status
                foreach (var material in outwardMaterials)
                {
                    material.Out_Status = "Material In";
                    material.Material_InDate = DateTime.Now; // Update Material_InDate with current DateTime
                    var inventoryRecord = await context.Inventory_Register
                        .FirstOrDefaultAsync(ir => ir.InventoryReg_ID == material.InventoryReg_ID);

                    if (inventoryRecord != null)
                    {
                        inventoryRecord.IR_Status = "Material In";
                    }
                }

                await context.SaveChangesAsync();

                // Check if all materials for the gatepass are received
                var totalMaterialsCount = await context.OutMaterials.CountAsync(om => om.GatepassID == gatepassID);
                var receivedMaterialsCount = await context.OutMaterials
                    .CountAsync(om => om.GatepassID == gatepassID && om.Out_Status == "Material In");

                // Update Outward_Status based on whether all or some materials were received
                var outwardMaterial = await context.OutwardMaterials.FirstOrDefaultAsync(om => om.GatepassID == gatepassID);

                if (outwardMaterial != null)
                {
                    if (totalMaterialsCount == receivedMaterialsCount)
                    {
                        // All materials received
                        outwardMaterial.Outward_Status = "Received";
                        SendEmailNotification(gatepassID, true); // Notify all materials received
                    }
                    else
                    {
                        // Some materials received, keep status as "Material Out"
                        outwardMaterial.Outward_Status = "Material Out";
                        SendEmailNotification(gatepassID, false); // Notify partial receipt
                    }

                    await context.SaveChangesAsync();
                }

                // Return success message
                return Json(new { message = "success" });
            }
            catch (Exception ex)
            {
                // Log the exception
                var exceptionLogger = new IMSExceptionLogger
                {
                    controllerName = "Add_OutwardMaterialController",
                    actionName = "MarkAsReceived",
                    exceptionStackTrace = ex.StackTrace,
                    exceptionMessage = ex.Message,
                    exceptionLogTime = DateTime.Now
                };
                context.IMSExceptionLoggers.Add(exceptionLogger);
                await context.SaveChangesAsync();

                // Return error message
                return Json(new { message = "An error occurred while marking materials as received" });
            }
        }


        private void SendEmailNotification(string gatepassID, bool allReceived)
        {
            // Retrieve details
            var inwardDetails = (from g in context.GatePasses
                                 where g.GatepassID == gatepassID
                                 select new { g.ReceiverEmail, g.ReceiverName, g.Gatepass_Number }).FirstOrDefault();

            if (inwardDetails == null) return;

            string subject;
            string body;

            if (allReceived)
            {
                // Full acceptance
                subject = "Acceptance of the Outward Material";
                body = $"Dear {inwardDetails.ReceiverName},<br/><br/>" +
                       "Greetings from Harbinger IMS!<br/><br/>" +
                       "This is a confirmation email for the acceptance of the material received against the Gate Pass Number " + inwardDetails.Gatepass_Number + ".<br/><br/>" +
                       "Best Regards,<br/>" +
                       "Harbinger IMS";
            }
            else
            {
                // Partial acceptance
                subject = "Partial Acceptance of the Outward Material";
                body = $"Dear {inwardDetails.ReceiverName},<br/><br/>" +
                       "Greetings from Harbinger IMS!<br/><br/>" +
                       "This is a confirmation email that we have only received some of the materials against the Gate Pass Number " + inwardDetails.Gatepass_Number + ".<br/>" +
                       "We request you to update us on the pending material delivery status at the earliest.<br/><br/>" +
                       "Best Regards,<br/>" +
                       "Harbinger IMS";
            }

            // Retrieve SMTP configuration from web.config
            var smtpHost = ConfigurationManager.AppSettings["SmtpServerHost"];
            var smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpServerPort"]);
            var smtpUser = ConfigurationManager.AppSettings["SmtpUserId"];
            var smtpPassword = ConfigurationManager.AppSettings["SmtpUserPassword"];
            var enableSsl = bool.Parse(ConfigurationManager.AppSettings["SmtpEnableSsl"]);

            // Create mail message
            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpUser), // Email from config
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(inwardDetails.ReceiverEmail); // Add recipient email

            // Send email using SMTP client with configuration settings
            using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
            {
                smtpClient.Credentials = new NetworkCredential(smtpUser, smtpPassword);
                smtpClient.EnableSsl = enableSsl;
                smtpClient.Send(mailMessage);
            }
        }





    }
}