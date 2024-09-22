using IMS.DAL;
using IMS.Entities;
using IMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;
using Newtonsoft.Json;
using System.IO;
using ExcelDataReader;
using System.Threading;
using ExcelDataReader.Log;
using System.Data.Entity;

namespace IMS.Web.Controllers
{
    public class Add_InventoryRegisterController : Controller
    {
        private ServiceVMSEntities context = new ServiceVMSEntities();
        private UnitOfWork unitOfWork = new UnitOfWork();
        // GET: Add_InventoryRegister
        public ActionResult Index(string sortOrder, string searchString, string Name, string SearchValue1, string SearchValue2, string SearchValue, string SearchValue_ExpenseN, FormCollection form, DateTime? searchdate, int? Page_no)
        {
            var httpCookie3 = Session["role"];
            string role = null;
            if (httpCookie3 != null)
            {
                role = httpCookie3.ToString();
            }
            if (role != null)
            {
                string location = null;
                int countForsearch = 0;
                string display = "0";
                ViewBag.display = display;
                int count = 0;
                string empid = null;
                string Email = null;
                DateTime val = DateTime.Now;
                int pagesize = 20;
                int pagenumber = (Page_no ?? 1);
                ViewBag.page = Page_no;
                if (Session["Location"] != null)
                {
                    location = Session["Location"].ToString();
                    ViewBag.location = Session["Location"].ToString();
                }

                ViewBag.Asset_id = String.IsNullOrEmpty(sortOrder) ? "Asset_id_desc" : "";
                ViewBag.Inv_department = sortOrder == "Inv_department" ? "Inv_department_desc" : "Inv_department";
                ViewBag.Inv_location = sortOrder == "Inv_location" ? "Inv_location_desc" : "Inv_location";
                ViewBag.Inv_nature = sortOrder == "Inv_nature" ? "Inv_nature_desc" : "Inv_nature";
                ViewBag.Mat_category = sortOrder == "Mat_category" ? "Mat_category_desc" : "Mat_category";
                ViewBag.Mat_name = sortOrder == "Mat_name" ? "Mat_name_desc" : "Mat_name";
                ViewBag.vendorId = sortOrder == "vendorId" ? "vendorId_desc" : "vendorId";
                ViewBag.purchaserate = sortOrder == "purchaserate" ? "purchaserate_desc" : "purchaserate";


                var httpCookie1 = Session["UserID"];
                if (httpCookie1 != null)
                {
                    empid = httpCookie1.ToString();
                    ViewBag.empid = empid;
                }


                string dept = null;

                var httpCookie4 = Session["DepartmentID"];
                if (httpCookie4 != null)
                {
                    dept = httpCookie4.ToString();
                }
                ViewBag.role = role;
                var imsentity = new List<IMSEntity>();


                try
                {
                    ServiceVMSEntities context = new ServiceVMSEntities();
                    var serviceuser = new ServiceUserDepartment();
                    var name_department = unitOfWork.DepartmentRepository.Get(serviceuser, int.Parse(dept));
                    string deptName = name_department[0].userDepartmentName;


                    IQueryable<IMSEntity> InventoryDetails = null;

                    InventoryDetails = (from t in context.Inventory_Register
                                        join i in context.InwardMaterials on t.InwardID equals i.InwardID into inwardGroup
                                        from i in inwardGroup.DefaultIfEmpty()
                                        join cat in context.Material_Category on t.Material_CategoryID equals cat.Material_CategoryID into catGroup
                                        from cat in catGroup.DefaultIfEmpty()
                                        join m in context.Materials on t.MaterialID equals m.MaterialID into materialGroup
                                        from m in materialGroup.DefaultIfEmpty()
                                        join v in context.HTV_Vendor on t.vendorId equals v.vendorId into vendorGroup
                                        from v in vendorGroup.DefaultIfEmpty()
                                        join tv in context.TempVendors on t.vendorId equals tv.TempVendorID into tempVendorGroup
                                        from tv in tempVendorGroup.DefaultIfEmpty()
                                        join s in context.ServiceUserDepartments on t.userDepartmentId equals s.userDepartmentId into departmentGroup
                                        from s in departmentGroup.DefaultIfEmpty()
                                        select new IMSEntity
                                        {
                                            InwardID = i != null ? i.InwardID : null,
                                            InventoryReg_ID = t.InventoryReg_ID,
                                            AssetID = t.AssetID,
                                            Material_CategoryID = cat != null ? cat.Material_CategoryID : 0,
                                            Material_CategoryName = cat != null ? cat.Material_CategoryName : null,
                                            MaterialID = m != null ? m.MaterialID : null,
                                            MaterialName = m != null ? m.MaterialName : null,
                                            Purchase_Rate = t.Purchase_Rate,
                                            Asset_Cost = t.Asset_Cost,
                                            GRN_Number = t.GRN_Number,
                                            Quantity = t.Quantity,
                                            Model_Number = t.Model_Number,
                                            Serial_No = t.Serial_No,
                                            Makenm = t.Make,
                                            vendorName = v != null ? v.vendorName : tv != null ? tv.TempVendorName : null,
                                            ims_userDepartmentId = t != null ? t.userDepartmentId : null,
                                            userDepartmentName = s != null ? s.userDepartmentName : null,
                                            Location = t != null ? t.Location : null,
                                            Inward_ExpenseNature = i != null ? i.Inward_ExpenseNature : null,
                                            ExpenseNature = t.ExpenseNature,
                                            AssetGenerateDate = t.AssetGenerateDate,
                                        });


                    if (empid != null)
                    {
                        if (empid.Contains("DUM"))
                        {
                            if (role == "Inventory Manager")
                            {
                                InventoryDetails = InventoryDetails.Where(u => u.InventoryReg_ID.Contains("DUM"));
                            }
                            else
                            {
                                InventoryDetails = InventoryDetails.Where(u => u.InventoryReg_ID.Contains("DUM") && u.userDepartmentId == int.Parse(dept));
                            }
                        }
                        else
                        {
                            var inventoryList = InventoryDetails.ToList();
                            var locations = inventoryList.Select(s => s.vendorName).ToList();
                            int deptid = int.Parse(dept);
                            if (role == "Administrator")
                            {
                                InventoryDetails = InventoryDetails.Where(u => !u.InventoryReg_ID.Contains("DUM"));
                            }
                            else if (role == "Inventory Manager")
                            {
                                InventoryDetails = InventoryDetails.Where(u => !u.InventoryReg_ID.Contains("DUM") && u.ims_userDepartmentId == deptid);

                            }
                            else if (role == "Security Guard")
                            {
                                InventoryDetails = InventoryDetails.Where(u => !u.InventoryReg_ID.Contains("DUM") && u.Location == location);
                            }
                            else if (role == "Inventory Incharge" || role == "Receiver")
                            {
                                InventoryDetails = InventoryDetails.Where(u => !u.InventoryReg_ID.Contains("DUM") && u.Location == location && u.ims_userDepartmentId == deptid);
                            }
                            else
                            {

                                InventoryDetails = InventoryDetails.Where(u => !u.InventoryReg_ID.Contains("DUM") && u.ims_userDepartmentId == deptid);
                            }
                        }
                    }
                    var c = InventoryDetails.Count();
                    if (!String.IsNullOrEmpty(SearchValue) || !String.IsNullOrEmpty(SearchValue_ExpenseN) || !String.IsNullOrEmpty(SearchValue1) || searchdate != null)
                    {
                        String Searchby = form["Name"].ToString();

                        if (Searchby == "Asset_id")
                        {
                            countForsearch = 1;
                            InventoryDetails = InventoryDetails.Where(u => u.AssetID.Contains(SearchValue));
                        }
                        else if (Searchby == "Inv_department")
                        {

                            countForsearch = 1;
                            InventoryDetails = InventoryDetails.Where(u => u.userDepartmentName == SearchValue1);
                        }
                        else if (Searchby == "Inventorylocation")
                        {
                            countForsearch = 1;
                            InventoryDetails = InventoryDetails.Where(u => u.Location.Contains(SearchValue));
                        }
                        else if (Searchby == "Inv_nature")
                        {
                            countForsearch = 1;
                            InventoryDetails = InventoryDetails.Where(u => u.ExpenseNature.Contains(SearchValue_ExpenseN));
                        }
                        else if (Searchby == "Mat_category")
                        {
                            countForsearch = 1;
                            InventoryDetails = InventoryDetails.Where(u => u.Material_CategoryName.Contains(SearchValue));
                        }
                        else if (Searchby == "Mat_name")
                        {
                            countForsearch = 1;
                            InventoryDetails = InventoryDetails.Where(u => u.MaterialName.Contains(SearchValue));
                        }
                        else if (Searchby == "Vendor_name")
                        {
                            countForsearch = 1;
                            InventoryDetails = InventoryDetails.Where(u => u.vendorName.Contains(SearchValue));
                        }

                    }

                    switch (sortOrder)
                    {
                        case "Asset_id_desc":
                            InventoryDetails = InventoryDetails.OrderBy(p => p.AssetID);
                            break;

                        case "Inv_department":
                            InventoryDetails = InventoryDetails.OrderByDescending(p => p.userDepartmentName);
                            break;

                        case "Inv_department_desc":
                            InventoryDetails = InventoryDetails.OrderBy(p => p.userDepartmentName);
                            break;

                        case "Inv_location":
                            InventoryDetails = InventoryDetails.OrderByDescending(u => u.Location);
                            break;

                        case "Inv_location_desc":
                            InventoryDetails = InventoryDetails.OrderBy(u => u.Location);
                            break;

                        case "Inv_nature":
                            InventoryDetails = InventoryDetails.OrderByDescending(u => u.ExpenseNature);
                            break;

                        case "Inv_nature_desc":
                            InventoryDetails = InventoryDetails.OrderBy(u => u.ExpenseNature);
                            break;

                        case "Mat_name":
                            InventoryDetails = InventoryDetails.OrderByDescending(u => u.MaterialName);
                            break;

                        case "Mat_name_desc":
                            InventoryDetails = InventoryDetails.OrderBy(u => u.MaterialName);
                            break;

                        case "vendorId":
                            InventoryDetails = InventoryDetails.OrderByDescending(u => u.vendorName);
                            break;

                        case "vendorId_desc":
                            InventoryDetails = InventoryDetails.OrderBy(u => u.vendorName);
                            break;

                        case "Mat_category":
                            InventoryDetails = InventoryDetails.OrderByDescending(u => u.Material_CategoryName);
                            break;

                        case "Mat_category_desc":
                            InventoryDetails = InventoryDetails.OrderBy(u => u.Material_CategoryName);
                            break;

                        case "purchaserate_desc":
                            InventoryDetails = InventoryDetails.OrderBy(u => u.Purchase_Rate);
                            break;

                        case "purchaserate":
                            InventoryDetails = InventoryDetails.OrderByDescending(u => u.Purchase_Rate);
                            break;

                        default:
                            InventoryDetails = InventoryDetails.OrderByDescending(p => p.AssetGenerateDate);
                            break;

                    }
                    foreach (var t in InventoryDetails)
                    {

                        imsentity.Add(new IMSEntity()
                        {
                            InwardID = t.InwardID,
                            InventoryReg_ID = t.InventoryReg_ID,
                            AssetID = t.AssetID,
                            Material_CategoryID = t.Material_CategoryID,
                            Material_CategoryName = t.Material_CategoryName,
                            MaterialID = t.MaterialID,
                            MaterialName = t.MaterialName,
                            Purchase_Rate = t.Purchase_Rate,
                            Asset_Cost = t.Asset_Cost,
                            GRN_Number = t.GRN_Number,
                            Quantity = t.Quantity,
                            Model_Number = t.Model_Number,
                            Serial_No = t.Serial_No,
                            Makenm = t.Makenm,
                            vendorName = t.vendorName,
                            userDepartmentId = t.userDepartmentId,
                            userDepartmentName = t.userDepartmentName,
                            Location = t.Location,
                            ExpenseNature = t.ExpenseNature
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
                        controllerName = "Add_InventoryRegisterController",
                        actionName = "Index(InventoryRegister)",
                        exceptionStackTrace = e.StackTrace,
                        exceptionMessage = e.Message,
                        exceptionLogTime = DateTime.Now
                    };
                    unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                    unitOfWork.Save();
                }
                return View(new List<IMSEntity>().ToPagedList(1, pagesize));
            }
            else
            {
                return HttpNotFound();
            }
        }

        public ActionResult InventoryDetails(string id)
        {
            if (Session["Email"] != null)
            {
                id = unitOfWork.DepartmentRepository.Decode(id);
                string role = string.Empty;
                string empid = string.Empty;
                string dept = null;
                string location = null;
                List<IMSEntity> inventorylistmodel = new List<IMSEntity>();
                var httpCookie1 = Session["role"];
                if (httpCookie1 != null)
                {
                    role = httpCookie1.ToString();
                }
                var httpCookie2 = Session["UserID"];
                if (httpCookie2 != null)
                {
                    empid = httpCookie2.ToString();
                }
                var httpCookie4 = Session["DepartmentID"];
                if (httpCookie4 != null)
                {
                    dept = httpCookie4.ToString();
                }

                ViewBag.dept = dept;
                ViewBag.empid = empid;
                ViewBag.role = role;

                try
                {

                    var invdetails = (from t in context.Inventory_Register
                                      join i in context.InwardMaterials on t.InwardID equals i.InwardID into inwardGroup
                                      from i in inwardGroup.DefaultIfEmpty()
                                      join cat in context.Material_Category on t.Material_CategoryID equals cat.Material_CategoryID into catGroup
                                      from cat in catGroup.DefaultIfEmpty()
                                      join m in context.Materials on t.MaterialID equals m.MaterialID into materialGroup
                                      from m in materialGroup.DefaultIfEmpty()
                                      join v in context.HTV_Vendor on t.vendorId equals v.vendorId into vendorGroup
                                      from v in vendorGroup.DefaultIfEmpty()
                                      join s in context.ServiceUserDepartments on t.userDepartmentId equals s.userDepartmentId into departmentGroup
                                      from s in departmentGroup.DefaultIfEmpty()
                                      where t.InventoryReg_ID == id
                                      select new IMSEntity
                                      {
                                          InwardID = i != null ? i.InwardID : null,
                                          InventoryReg_ID = t.InventoryReg_ID,
                                          AssetID = t.AssetID,
                                          Material_CategoryID = cat != null ? cat.Material_CategoryID : 0,
                                          Material_CategoryName = cat != null ? cat.Material_CategoryName : null,
                                          MaterialID = m != null ? m.MaterialID : null,
                                          MaterialName = m != null ? m.MaterialName : null,
                                          Purchase_Rate = t.Purchase_Rate,
                                          Asset_Cost = t.Asset_Cost,
                                          GRN_Number = t.GRN_Number,
                                          Quantity = t.Quantity,
                                          Model_Number = t.Model_Number,
                                          Serial_No = t.Serial_No,
                                          Makenm = t.Make,
                                          vendorName = v != null ? v.vendorName : null,
                                          ims_userDepartmentId = t.userDepartmentId,
                                          userDepartmentName = s != null ? s.userDepartmentName : null,
                                          Location = t.Location,
                                          Inward_ExpenseNature = i != null ? i.Inward_ExpenseNature : null,
                                          ExpenseNature = t.ExpenseNature,
                                          AssetGenerateDate = t.AssetGenerateDate,
                                          MachineID = t.MachineID,
                                          RAM = t.RAM,
                                          HDD = t.HDD,
                                          OS = t.OS
                                      }).FirstOrDefault();

                    if (invdetails.ExpenseNature != null)
                    {
                        ViewBag.expensenature = invdetails.ExpenseNature;
                    }
                    if (invdetails.GRN_Number != null)
                    {
                        ViewBag.GRNnumber = invdetails.GRN_Number;
                    }
                    inventorylistmodel.Add(new IMSEntity()
                    {
                        InwardID = invdetails.InwardID,
                        vendorName = invdetails.vendorName,
                        GRN_Number = invdetails.GRN_Number,
                        userDepartmentName = invdetails.userDepartmentName,
                        ExpenseNature = invdetails.ExpenseNature,
                        Location = invdetails.Location,
                        Material_CategoryName = invdetails.Material_CategoryName,
                        MaterialName = invdetails.MaterialName,
                        Makenm = invdetails.Makenm,
                        Serial_No = invdetails.Serial_No,
                        Model_Number = invdetails.Model_Number,
                        Purchase_Rate = invdetails.Purchase_Rate,
                        Asset_Cost = invdetails.Asset_Cost,
                        Quantity = invdetails.Quantity,
                        AssetID = invdetails.AssetID,
                        MachineID = invdetails.MachineID,
                        HDD = invdetails.HDD,
                        RAM = invdetails.RAM,
                        OS = invdetails.OS,
                    });
                }
                catch (Exception e)
                {
                    Console.Write(e);
                    var exceptionLogger = new IMSExceptionLogger()
                    {
                        controllerName = "Add_InwardMaterialController",
                        actionName = "InventoryDetails",
                        exceptionStackTrace = e.StackTrace,
                        exceptionMessage = e.Message,
                        exceptionLogTime = DateTime.Now
                    };
                    unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                    unitOfWork.Save();
                }
                return View(inventorylistmodel);
            }
            else
            {
                return HttpNotFound();
            }
        }

        [HttpGet]
        public ActionResult AddInventoryRegister()
        {
            if (Session["role"] != null)
            {
                var material = new Material_Category();
                var vendor = new HTV_Vendor();

                ViewBag.materialcategory = unitOfWork.DepartmentRepository.GetMaterialCategory(material, 0);
                ViewBag.vendorList = unitOfWork.DepartmentRepository.Get(vendor, null);

                ViewBag.role = Session["role"].ToString();
                if (Session["Location"] != null)
                {
                    ViewBag.location = Session["Location"].ToString();
                }
                return View();
            }
            else
            {
                return HttpNotFound();
            }
        }


        [HttpPost]
        public ActionResult AddInventoryRegister(IMS.Models.IMSEntity model)
        {
            string role = string.Empty;
            if (Session["role"] != null)
            {
                role = Session["role"].ToString();
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
            string dept = null;

            if (Session["DepartmentID"] != null)
            {
                dept = Session["DepartmentID"].ToString();

            }
            if (model.vendorId == null)
            {
                var receiveremail = model.ReceiverEmail;
                var tempvendor = (from t in context.TempVendors
                                  where t.TempVendorStatus != "Deleted" && t.TempVendorEmail == receiveremail
                                  select new
                                  {
                                      t.TempVendorEmail,
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
                        model.TempVendorID = tempvendor.TempVendorID;
                        model.vendorId = tempvendor.TempVendorID;
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

                    model.TempVendorID = vendorid;
                    model.vendorId = model.TempVendorID;
                }
            }
            Inventory_Register inventory = new Inventory_Register();

            var material = context.Materials.FirstOrDefault(m => m.MaterialID == model.MaterialID);
            if (material != null)
            {
                inventory.Material_CategoryID = material.Material_CategoryID;
            }
            else
            {
                // Handle the case where the MaterialID does not exist
                ModelState.AddModelError("", "Selected material does not exist.");
                ViewBag.MaterialCategories = new SelectList(context.Material_Category.ToList(), "Material_CategoryID", "CategoryName");
                ViewBag.Materials = new SelectList(context.Materials.ToList(), "MaterialID", "MaterialName");
                ViewBag.Vendors = new SelectList(context.HTV_Vendor.ToList(), "vendorId", "vendorName");
                return View(model);
            }

            inventory.MaterialID = model.MaterialID;
            inventory.MaterialName = material.MaterialName;
            inventory.Quantity = 1;
            inventory.Purchase_Rate = model.Purchase_Rate;
            inventory.Asset_Cost = model.Purchase_Rate;
            inventory.Serial_No = model.Serial_No;
            inventory.Model_Number = model.Model_Number;
            inventory.Make = model.Makenm;
            inventory.ExpenseNature = model.ExpenseNature;
            inventory.IR_Status = "Material In";
            inventory.AssetGenerateDate = DateTime.Now;
            inventory.vendorId = model.vendorId;
            inventory.Inward_CP_Rental_ID = null;
            inventory.MachineID = model.MachineID;
            inventory.RAM = model.RAM;
            inventory.HDD = model.HDD;
            inventory.OS = model.OS;
            inventory.userDepartmentId = string.IsNullOrEmpty(dept) ? (int?)null : int.Parse(dept);

            // Set location based on conditions
            if (model.Location != null)
            {
                inventory.Location = model.Location;
            }
            else
            {
                inventory.Location = loc;
            }

            inventory.InventoryReg_ID = unitOfWork.DepartmentRepository.GenerateUniqueCode();
            var result = generateAssetID(inventory.MaterialName, inventory.Location, inventory.MaterialID);
            inventory.AssetID = result.Assetid;

            context.Inventory_Register.Add(inventory);
            context.SaveChanges();

            var CategoryUpdate = (from t in context.Serial_Number
                                  where t.Year_Range == result.year_range && t.Location == inventory.Location
                                  select t).SingleOrDefault();
            CategoryUpdate.Serial_Asset_Number = result.FinalNumber;
            context.SaveChanges();

            return RedirectToAction("Index");
        }

        public MyJsonObject generateAssetID(string Materialname, string Location, string materialid)
        {
            string empid = null;
            string dept = null;
            string deptnm = string.Empty;
            string asset = string.Empty;
            string loc = string.Empty;
            var serviceUser = new ServiceUserDepartment();


            if (Session["UserID"] != null)
            {
                empid = Session["UserID"].ToString();
            }
            if (Session["DepartmentID"] != null)
            {
                dept = Session["DepartmentID"].ToString();
            }

            var name_department = unitOfWork.DepartmentRepository.Get(serviceUser, int.Parse(dept));
            string deptName = name_department[0].userDepartmentName;

            //var materialname = Materialname.Substring(0, 3);

            var materialname = (from m in context.Materials
                                where m.MaterialID == materialid
                                select new { m.MaterialPrefix }).FirstOrDefault();
            if (deptName == "Facilities")
            {
                deptnm = "AD";
            }
            else if (deptName == "CIS")
            {
                deptnm = "CIS";
            }

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

            int start_date_month = DateTime.Now.Month;
            string start_date_year = DateTime.Now.Year.ToString();
            string year_range = string.Empty;
            string financialyear = string.Empty;
            var serial_Number = new Serial_Number();
            var service_category = new ServiceCategory();
            var category_number = new Category_Number();
            List<IMSEntity> categoryName = null;
            string categoryNumber = string.Empty;

            if ((start_date_month > 3 && start_date_year == "2024") || (start_date_month < 3) && start_date_year == "2025")
            {
                year_range = "2024-25";
                financialyear = "24-25";
                categoryNumber = unitOfWork.DepartmentRepository.Update(serial_Number, "Asset", year_range, Location);
            }
            else
            {
                year_range = "2023-24";
                financialyear = "23-24";
                categoryNumber = unitOfWork.DepartmentRepository.Update(serial_Number, "Asset", year_range, Location);
            }
            var sprintJson = string.Empty;
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
                    suffixpo = ""; // Default to empty suffix if count exceeds 5 digits
                    break;
            }

            if (!empid.Contains("DUM"))
            {
                asset = "HG/" + deptnm + "/" + loc + "/" + materialname.MaterialPrefix.ToUpper() + "/" + financialyear + "/" + suffixpo + finalnumber;
            }
            else
            {
                asset = "DUMMYASSETID";
            }

            MyJsonObject myJsonObject = new MyJsonObject();
            if (empid.Contains("DUM"))
            {
                finalnumber = finalnumber - 1;
                //sprintJson = "{\"Assetid\":\"" + asset + "\",\"FinalNumber\":" + finalnumber + "}";
                return new MyJsonObject { Assetid = asset, FinalNumber = finalnumber, year_range = year_range };
            }
            else
            {
                //sprintJson = "{\"Assetid\":\"" + asset + "\",\"FinalNumber\":" + finalnumber + "\",\"year_range\":" + year_range +"}";
                return new MyJsonObject { Assetid = asset, FinalNumber = finalnumber, year_range = year_range };
            }

            //return myJsonObject;
        }
        public JsonResult Getmaterials(int Material_CategoryID)
        {
            var materiallist = new List<IMSEntity>();
            var materialcat = new Material();
            materiallist = unitOfWork.DepartmentRepository.GetMaterials(materialcat, Material_CategoryID);

            var result = from a in materiallist select new Material { MaterialID = a.MaterialID, MaterialName = a.MaterialName, Material_CategoryID = a.CategoryID };
            var jsonResult = JsonConvert.SerializeObject(result);
            return Json(jsonResult, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult UploadInventoryRegister()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UploadInventoryRegister(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                string tempPath = Path.GetTempPath();
                string filePath = Path.Combine(tempPath, Path.GetFileName(file.FileName));

                try
                {
                    if (Path.GetExtension(file.FileName).Equals(".xls", StringComparison.OrdinalIgnoreCase) ||
                        Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        file.SaveAs(filePath);

                        List<Inventory_Register> inventoryList = new List<Inventory_Register>();
                        bool fileIsAvailable = false;
                        int retryCount = 0;

                        while (!fileIsAvailable && retryCount < 10)
                        {
                            try
                            {
                                using (var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                {
                                    fileIsAvailable = true;

                                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                                    {
                                        while (reader.Read())
                                        {
                                            if (reader.Depth == 0) continue;

                                            string materialId = reader.GetValue(1)?.ToString();
                                            var material = context.Materials.SingleOrDefault(m => m.MaterialID == materialId);

                                            // Handle case where material is not found
                                            if (material == null)
                                            {
                                                continue;
                                            }

                                            string userDepartmentName = reader.GetValue(15)?.ToString();
                                            var userDepartmentId = context.ServiceUserDepartments
                                                                            .Where(d => d.userDepartmentName == userDepartmentName)
                                                                            .Select(d => d.userDepartmentId)
                                                                            .FirstOrDefault();

                                            string vendorId = reader.GetValue(13)?.ToString();
                                            var htvVendor = context.HTV_Vendor.SingleOrDefault(v => v.vendorId == vendorId);
                                            if (htvVendor == null)
                                            {
                                                var tempVendor = context.TempVendors.SingleOrDefault(v => v.TempVendorID == vendorId);

                                                if (tempVendor != null)
                                                {
                                                    vendorId = tempVendor.TempVendorID;
                                                }
                                                else
                                                {
                                                    vendorId = null;
                                                }
                                            }

                                            Inventory_Register inventory = new Inventory_Register
                                            {
                                                InventoryReg_ID = unitOfWork.DepartmentRepository.GenerateInwardIDS("InventoryRegister"),
                                                Material_CategoryID = Convert.ToInt32(reader.GetValue(0)),
                                                MaterialID = reader.GetValue(1)?.ToString(),
                                                Purchase_Rate = (double?)Convert.ToDouble(reader.GetValue(2)),
                                                Asset_Cost = (double?)Convert.ToDouble(reader.GetValue(3)),
                                                Make = reader.GetValue(4)?.ToString(),
                                                Serial_No = reader.GetValue(5)?.ToString(),
                                                ExpenseNature = reader.GetValue(6)?.ToString(),
                                                Model_Number = reader.GetValue(7)?.ToString(),
                                                IR_Status = reader.GetValue(8)?.ToString(),
                                                MachineID = string.IsNullOrWhiteSpace(reader.GetValue(9)?.ToString()) ? null : reader.GetValue(9)?.ToString(),
                                                RAM = string.IsNullOrWhiteSpace(reader.GetValue(10)?.ToString()) ? null : reader.GetValue(10)?.ToString(),
                                                HDD = string.IsNullOrWhiteSpace(reader.GetValue(11)?.ToString()) ? null : reader.GetValue(11)?.ToString(),
                                                OS = string.IsNullOrWhiteSpace(reader.GetValue(12)?.ToString()) ? null : reader.GetValue(12)?.ToString(),
                                                Location = reader.GetValue(14)?.ToString(),
                                                userDepartmentId = userDepartmentId,
                                                vendorId = vendorId,
                                                AssetGenerateDate = DateTime.Now,
                                                MaterialName = material.MaterialName, // Safe to use now
                                                Quantity = 1,
                                                GRN_Number = null,
                                                Invoice_Number = null,
                                                Alloted_To = null,
                                                Allot_date = null,
                                                InwardID = null,
                                                Inward_CP_Rental_ID = null
                                            };

                                            var result = generateAssetID(inventory.MaterialName, inventory.Location, inventory.MaterialID);
                                            inventory.AssetID = result.Assetid;

                                            inventoryList.Add(inventory);

                                            var CategoryUpdate = (from t in context.Serial_Number
                                                                  where t.Year_Range == result.year_range && t.Location == inventory.Location
                                                                  select t).SingleOrDefault();
                                            if (CategoryUpdate != null)
                                            {
                                                CategoryUpdate.Serial_Asset_Number = result.FinalNumber;
                                                context.SaveChanges();
                                            }
                                        }
                                    }
                                }
                            }
                            catch (IOException)
                            {
                                retryCount++;
                                Thread.Sleep(1000);
                            }
                        }

                        if (!fileIsAvailable)
                        {
                            ViewBag.Message = "File is being used by another process. Please try again later.";
                            return RedirectToAction("AddInventoryRegister");
                        }

                        context.Inventory_Register.AddRange(inventoryList);
                        context.SaveChanges();

                        ViewBag.Message = "File uploaded successfully!";
                    }
                    else
                    {
                        ViewBag.Message = "Invalid file format. Please upload an Excel file.";
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.Message = "Error: " + ex.Message;
                }
            }
            else
            {
                ViewBag.Message = "Please select a file.";
            }

            return RedirectToAction("Index");
        }

    }
}