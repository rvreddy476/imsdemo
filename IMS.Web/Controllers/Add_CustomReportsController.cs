using IMS.BLL;
using IMS.BLL.Interfaces;
using IMS.DAL;
using IMS.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using IMS.Models;
using System.IO;
using System.Web.UI.WebControls;
using System.Drawing;
using System.Web.UI;

namespace IMS.Web.Controllers
{
    public class Add_CustomReportsController : Controller
    {
        // GET: Add_CustomReports
        private ServiceVMSEntities context = new ServiceVMSEntities();
        private UnitOfWork unitOfWork = new UnitOfWork();
        private IMExceptionLogger exceptionLogger = BLLObjectCreator.CreateIMSLogger(ExceptionLoggerType.IMSText);



        public ActionResult Index()
        {
            return View();
        }

        public ActionResult CustomReport()
        {
            if (@Session["Email"] != null)
            {
                var vendor = new HTV_Vendor();
                var material_Category = new Material_Category();
                ViewBag.vendorList = unitOfWork.DepartmentRepository.Get(vendor, null);
                ViewBag.materialCategoryname = unitOfWork.DepartmentRepository.GetMaterialCategory(material_Category, 0);
                if (Session["role"].ToString() != null)
                {
                    ViewBag.role = Session["role"].ToString();
                }
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }

        [HttpPost]
        public ActionResult CustomReport(IMSEntity entity, string filterType, string monthly = "", int yearly = 0, string financialyearly = "", DateTime? startDate = null, DateTime? endDate = null)
        {
            DateTime start, end;
            string role = string.Empty;
            List<InwardReportEntity> inwardModel = new List<InwardReportEntity>();
            if (Session["role"].ToString() != null)
            {
                role = Session["role"].ToString();
                entity.RoleInIMS = Session["role"].ToString();
            }


            if (role != "Inventory Manager")
            {
                if (Session["Location"].ToString() != null)
                {
                    ViewBag.Location = Session["Location"].ToString();
                    entity.userLocation = Session["role"].ToString();
                }
            }
            if (Session["DepartmentID"].ToString() != null)
            {
                ViewBag.Location = Session["DepartmentID"].ToString();
                entity.userDepartmentId = int.Parse(Session["DepartmentID"].ToString());
            }
            switch (filterType)
            {
                case "Monthly":
                    start = new DateTime(yearly, DateTime.ParseExact(monthly, "MMMM", System.Globalization.CultureInfo.InvariantCulture).Month, 1);
                    end = start.AddMonths(1).AddDays(-1);
                    break;
                case "Yearly":
                    start = new DateTime(yearly, 1, 1);
                    end = new DateTime(yearly, 12, 31);
                    break;

                case "FinancialYear":
                    var years = financialyearly.Split('-');
                    start = new DateTime(int.Parse(years[0]), 4, 1);
                    end = new DateTime(int.Parse(years[1]), 3, 31);
                    break;

                case "Custom":
                    if (entity.inwardenddate != null && entity.inwardstartdate != null)
                    {
                        start = DateTime.Parse(entity.inwardstartdate.ToString());
                        end = DateTime.Parse(entity.inwardenddate.ToString());
                    }
                    else
                    {
                        return View(new List<InwardReportEntity>());
                    }
                    break;

                default:
                    // Handle error
                    return View(new List<InwardReportEntity>());
            }


            entity.startdate = start;
            entity.enddate = end;

            IMSReportGenerator reportGenerator = new BLL.Implementation.ReportGenerator.ExcelIMSReportGenerator();
            inwardModel = reportGenerator.ReportGenerationonInward(entity);
            //var materials = unitOfWork.DepartmentRepository.(start, end);
            ViewBag.StartDate = start;
            ViewBag.EndDate = end;


            var gv = new GridView();
            gv.AllowPaging = false;
            gv.BackColor = Color.White;
            gv.HorizontalAlign = HorizontalAlign.Center;

            gv.Style.Add("font-family", "Arial, Helvetica, sans-serif;");
            gv.Style.Add("font-size", "8px");

            gv.DataSource = inwardModel;
            gv.DataBind();

            gv.Attributes.Add("style", "word-break:break-all; word-wrap:break-word");
            Response.ClearContent();
            Response.Buffer = true;

            if (monthly != "")
            {
                Response.AddHeader("content-disposition", "attachment; filename=Monthly-" + monthly + "-Report.xls");
            }
            else if (yearly != 0 && monthly == "" && financialyearly == "")
            {
                Response.AddHeader("content-disposition", "attachment; filename=Yearly-" + yearly + "-Report.xls");
            }
            else if (financialyearly != "" && yearly == 0 && monthly == "")
            {
                Response.AddHeader("content-disposition", "attachment; filename=FinancialYearly-" + financialyearly + "-Report.xls");
            }
            else if (filterType == "Custom")
            {
                Response.AddHeader("content-disposition", "attachment; filename=Customdate" + "-Report.xls");
            }
            Response.ContentType = "application/ms-excel";
            Response.Charset = "";
            StringWriter objStringWriter = new StringWriter();
            HtmlTextWriter objHtmlTextWriter = new HtmlTextWriter(objStringWriter);
            gv.RenderControl(objHtmlTextWriter);
            Response.Output.Write(objStringWriter.ToString());
            Response.Flush();
            Response.End();
            var vendor = new HTV_Vendor();
            ViewBag.vendorList = unitOfWork.DepartmentRepository.Get(vendor, null);
            return View();
        }


        public ActionResult GatepassReport()
        {


            var expenseNatureList = new List<SelectListItem>()
                {
                    new SelectListItem{ Text = "Capex",Value="Capex" },
                    new SelectListItem{ Text = "Opex",Value="Opex" },
                    new SelectListItem{ Text = "Inventory",Value="Inventory" }
                };
            ViewBag.expenseNatureList = expenseNatureList;

            // Fetch data for departments dropdown
            var userDepartmentList = unitOfWork.DepartmentRepository.GetServiceDepartments();

            ViewBag.userDepartmentList = userDepartmentList;


            if (Session["role"] != null)
            {
                ViewBag.role = Session["role"].ToString();
            }

            return View();


        }



        [HttpPost]
        public ActionResult GatepassReport(IMSEntity entity, string filterType, string monthly = "", int yearly = 0, string financialyearly = "", DateTime? startDate = null, DateTime? endDate = null, string expenseNature = "", string deptName = "", string gatepassCategory = "", string gatepassType = "")
        {
            DateTime start, end;
            string role = string.Empty;
            List<GatepassReportEntity> gatepassModel = new List<GatepassReportEntity>();

            if (Session["role"]?.ToString() != null)
            {
                role = Session["role"].ToString();
                entity.RoleInIMS = Session["role"].ToString();
            }

            if (role != "Inventory Manager")
            {
                if (Session["Location"]?.ToString() != null)
                {
                    ViewBag.Location = Session["Location"]?.ToString();
                    entity.userLocation = Session["role"]?.ToString();
                }
            }

            if (Session["DepartmentID"]?.ToString() != null)
            {
                ViewBag.Location = Session["DepartmentID"]?.ToString();
                entity.userDepartmentId = int.Parse(Session["DepartmentID"]?.ToString());
            }

            switch (filterType)
            {
                case "Monthly":
                    start = new DateTime(yearly, DateTime.ParseExact(monthly, "MMMM", System.Globalization.CultureInfo.InvariantCulture).Month, 1);
                    end = start.AddMonths(1).AddDays(-1);
                    break;
                case "Yearly":
                    start = new DateTime(yearly, 1, 1);
                    end = new DateTime(yearly, 12, 31);
                    break;
                case "FinancialYear":
                    var years = financialyearly.Split('-');
                    start = new DateTime(int.Parse(years[0]), 4, 1);
                    end = new DateTime(int.Parse(years[1]), 3, 31);
                    break;
                case "Custom":
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        start = startDate.Value;
                        end = endDate.Value;
                    }
                    else
                    {
                        return View(new List<GatepassReportEntity>());
                    }
                    break;
                default:
                    // Handle error
                    return View(new List<GatepassReportEntity>());
            }

            entity.startdate = start;
            entity.enddate = end;

            IMSReportGenerator reportGenerator = new BLL.Implementation.ReportGenerator.ExcelIMSReportGenerator();
            gatepassModel = reportGenerator.GenerateGatepassReport(entity, expenseNature, deptName, gatepassCategory, gatepassType);

            ViewBag.StartDate = start;
            ViewBag.EndDate = end;

            var gv = new GridView();
            gv.AllowPaging = false;
            gv.BackColor = Color.White;
            gv.HorizontalAlign = HorizontalAlign.Center;
            gv.Style.Add("font-family", "Arial, Helvetica, sans-serif;");
            gv.Style.Add("font-size", "8px");

            gv.DataSource = gatepassModel;
            gv.DataBind();
            gv.Attributes.Add("style", "word-break:break-all; word-wrap:break-word");

            Response.ClearContent();
            Response.Buffer = true;

            if (monthly != "")
            {
                Response.AddHeader("content-disposition", "attachment; filename=Monthly-" + monthly + "-Report.xls");
            }
            else if (yearly != 0 && monthly == "" && financialyearly == "")
            {
                Response.AddHeader("content-disposition", "attachment; filename=Yearly-" + yearly + "-Report.xls");
            }
            else if (financialyearly != "" && yearly == 0 && monthly == "")
            {
                Response.AddHeader("content-disposition", "attachment; filename=FinancialYearly-" + financialyearly + "-Report.xls");
            }
            else if (filterType == "Custom")
            {
                Response.AddHeader("content-disposition", "attachment; filename=Customdate" + "-Report.xls");
            }

            Response.ContentType = "application/ms-excel";
            Response.Charset = "";
            StringWriter objStringWriter = new StringWriter();
            HtmlTextWriter objHtmlTextWriter = new HtmlTextWriter(objStringWriter);
            gv.RenderControl(objHtmlTextWriter);
            Response.Output.Write(objStringWriter.ToString());
            Response.Flush();
            Response.End();

            var vendor = new HTV_Vendor();
            ViewBag.vendorList = unitOfWork.DepartmentRepository.Get(vendor, null);
            return View();
        }
        public ActionResult OutwardCustomReport()
        {
            if (@Session["Email"] != null)
            {
                var vendor = new HTV_Vendor();
                var material_Category = new Material_Category();
                ViewBag.vendorList = unitOfWork.DepartmentRepository.Get(vendor, null);
                ViewBag.materialCategoryname = unitOfWork.DepartmentRepository.GetMaterialCategory(material_Category, 0);
                if (Session["role"].ToString() != null)
                {
                    ViewBag.role = Session["role"].ToString();
                }
                return View();
            }
            else
            {
                return RedirectToAction("SignOut", "Login");
            }
        }

        [HttpPost]
        public ActionResult OutwardCustomReport(IMSEntity entity, string filterType, string monthly = "", int yearly = 0, string financialyearly = "", DateTime? startDate = null, DateTime? endDate = null)
        {
            DateTime start, end;
            string role = string.Empty;
            List<OutwardMaterialReportEntity> outWardModel = new List<OutwardMaterialReportEntity>();

            if (Session["role"]?.ToString() != null)
            {
                role = Session["role"].ToString();
                entity.RoleInIMS = Session["role"].ToString();
            }

            if (role != "Inventory Manager")
            {
                if (Session["Location"]?.ToString() != null)
                {
                    ViewBag.Location = Session["Location"]?.ToString();
                    entity.userLocation = Session["role"]?.ToString();
                }
            }

            if (Session["DepartmentID"]?.ToString() != null)
            {
                ViewBag.Location = Session["DepartmentID"]?.ToString();
                entity.userDepartmentId = int.Parse(Session["DepartmentID"]?.ToString());
            }

            switch (filterType)
            {
                case "Monthly":
                    start = new DateTime(yearly, DateTime.ParseExact(monthly, "MMMM", System.Globalization.CultureInfo.InvariantCulture).Month, 1);
                    end = start.AddMonths(1).AddDays(-1);
                    break;
                case "Yearly":
                    start = new DateTime(yearly, 1, 1);
                    end = new DateTime(yearly, 12, 31);
                    break;
                case "FinancialYear":
                    var years = financialyearly.Split('-');
                    start = new DateTime(int.Parse(years[0]), 4, 1);
                    end = new DateTime(int.Parse(years[1]), 3, 31);
                    break;
                case "Custom":
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        start = startDate.Value;
                        end = endDate.Value;
                    }
                    else
                    {
                        return View(new List<OutwardMaterialReportEntity>());
                    }
                    break;
                default:
                    // Handle error
                    return View(new List<OutwardMaterialReportEntity>());
            }

            entity.startdate = start;
            entity.enddate = end;

            IMSReportGenerator reportGenerator = new BLL.Implementation.ReportGenerator.ExcelIMSReportGenerator();
            outWardModel = reportGenerator.GenerateOutWardReport(entity);

            ViewBag.StartDate = start;
            ViewBag.EndDate = end;

            var gv = new GridView();
            gv.AllowPaging = false;
            gv.BackColor = Color.White;
            gv.HorizontalAlign = HorizontalAlign.Center;
            gv.Style.Add("font-family", "Arial, Helvetica, sans-serif;");
            gv.Style.Add("font-size", "8px");

            gv.DataSource = outWardModel;
            gv.DataBind();
            gv.Attributes.Add("style", "word-break:break-all; word-wrap:break-word");

            Response.ClearContent();
            Response.Buffer = true;

            if (monthly != "")
            {
                Response.AddHeader("content-disposition", "attachment; filename=Monthly-" + monthly + "-Report.xls");
            }
            else if (yearly != 0 && monthly == "" && financialyearly == "")
            {
                Response.AddHeader("content-disposition", "attachment; filename=Yearly-" + yearly + "-Report.xls");
            }
            else if (financialyearly != "" && yearly == 0 && monthly == "")
            {
                Response.AddHeader("content-disposition", "attachment; filename=FinancialYearly-" + financialyearly + "-Report.xls");
            }
            else if (filterType == "Custom")
            {
                Response.AddHeader("content-disposition", "attachment; filename=Customdate" + "-Report.xls");
            }

            Response.ContentType = "application/ms-excel";
            Response.Charset = "";
            StringWriter objStringWriter = new StringWriter();
            HtmlTextWriter objHtmlTextWriter = new HtmlTextWriter(objStringWriter);
            gv.RenderControl(objHtmlTextWriter);
            Response.Output.Write(objStringWriter.ToString());
            Response.Flush();
            Response.End();

            var vendor = new HTV_Vendor();
            ViewBag.vendorList = unitOfWork.DepartmentRepository.Get(vendor, null);
            return View();
        }

        public ActionResult InventoryRegisterReport()
        {
            var expenseNatureList = new List<SelectListItem>()
         {
             new SelectListItem{ Text = "Capex",Value="Capex" },
             new SelectListItem{ Text = "Opex",Value="Opex" },
             new SelectListItem{ Text = "Inventory",Value="Inventory" }
         };
            ViewBag.expenseNatureList = expenseNatureList;

            // Fetch data for departments dropdown
            var userDepartmentList = unitOfWork.DepartmentRepository.GetServiceDepartments();

            ViewBag.userDepartmentList = userDepartmentList;


            if (Session["role"] != null)
            {
                ViewBag.role = Session["role"].ToString();
            }

            return View();
        }

        [HttpPost]
        public ActionResult InventoryRegisterReport(IMSEntity entity, string filterType, string monthly = "", int yearly = 0, string financialyearly = "", DateTime? startDate = null, DateTime? endDate = null, string expenseNature = "", string deptName = "")
        {
            DateTime start, end;
            string role = string.Empty;
            List<InventoryRegisterEntity> inventoryModel = new List<InventoryRegisterEntity>();

            // Retrieve role and location information from session
            if (Session["role"]?.ToString() != null)
            {
                role = Session["role"].ToString();
                entity.RoleInIMS = role;
            }

            if (role != "Inventory Manager")
            {
                if (Session["Location"]?.ToString() != null)
                {
                    ViewBag.Location = Session["Location"]?.ToString();
                    entity.userLocation = Session["Location"]?.ToString();
                }
            }

            if (Session["DepartmentID"]?.ToString() != null)
            {
                ViewBag.DepartmentID = Session["DepartmentID"]?.ToString();
                entity.userDepartmentId = int.Parse(Session["DepartmentID"]?.ToString());
            }

            // Handle date filtering logic
            switch (filterType)
            {
                case "Monthly":
                    start = new DateTime(yearly, DateTime.ParseExact(monthly, "MMMM", System.Globalization.CultureInfo.InvariantCulture).Month, 1);
                    end = start.AddMonths(1).AddDays(-1);
                    break;
                case "Yearly":
                    start = new DateTime(yearly, 1, 1);
                    end = new DateTime(yearly, 12, 31);
                    break;
                case "FinancialYear":
                    var years = financialyearly.Split('-');
                    start = new DateTime(int.Parse(years[0]), 4, 1);
                    end = new DateTime(int.Parse(years[1]), 3, 31);
                    break;
                case "Custom":
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        start = startDate.Value;
                        end = endDate.Value;
                    }
                    else
                    {
                        return View(new List<InventoryRegisterEntity>());
                    }
                    break;
                default:
                    return View(new List<InventoryRegisterEntity>());
            }

            // Set the date range in the entity
            entity.startdate = start;
            entity.enddate = end;

            // Generate the Inventory Register report
            IMSReportGenerator reportGenerator = new BLL.Implementation.ReportGenerator.ExcelIMSReportGenerator();
            inventoryModel = reportGenerator.GenerateInventoryRegisterReport(entity, expenseNature, deptName);

            ViewBag.StartDate = start;
            ViewBag.EndDate = end;

            // Create and configure GridView for exporting to Excel
            var gv = new GridView
            {
                AllowPaging = false,
                BackColor = Color.White,
                HorizontalAlign = HorizontalAlign.Center
            };
            gv.Style.Add("font-family", "Arial, Helvetica, sans-serif;");
            gv.Style.Add("font-size", "8px");

            gv.DataSource = inventoryModel;
            gv.DataBind();
            gv.Attributes.Add("style", "word-break:break-all; word-wrap:break-word");

            // Clear and prepare the response for file download
            Response.ClearContent();
            Response.Buffer = true;

            if (!string.IsNullOrEmpty(monthly))
            {
                Response.AddHeader("content-disposition", "attachment; filename=InventoryRegister-Monthly-" + monthly + "-Report.xls");
            }
            else if (yearly != 0 && string.IsNullOrEmpty(monthly) && string.IsNullOrEmpty(financialyearly))
            {
                Response.AddHeader("content-disposition", "attachment; filename=InventoryRegister-Yearly-" + yearly + "-Report.xls");
            }
            else if (!string.IsNullOrEmpty(financialyearly) && yearly == 0 && string.IsNullOrEmpty(monthly))
            {
                Response.AddHeader("content-disposition", "attachment; filename=InventoryRegister-FinancialYearly-" + financialyearly + "-Report.xls");
            }
            else if (filterType == "Custom")
            {
                Response.AddHeader("content-disposition", "attachment; filename=InventoryRegister-CustomDate-Report.xls");
            }

            // Set the content type to Excel
            Response.ContentType = "application/ms-excel";
            Response.Charset = "";

            // Write the GridView content to the response
            StringWriter objStringWriter = new StringWriter();
            HtmlTextWriter objHtmlTextWriter = new HtmlTextWriter(objStringWriter);
            gv.RenderControl(objHtmlTextWriter);
            Response.Output.Write(objStringWriter.ToString());
            Response.Flush();
            Response.End();

            // This line will never be reached due to Response.End(), but it is required to ensure all code paths return a value
            return new EmptyResult();
        }
    }
}