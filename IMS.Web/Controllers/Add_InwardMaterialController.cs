using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.Objects.DataClasses;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using IMS.BLL;
using IMS.BLL.Interfaces;
using IMS.DAL;
using IMS.Entities;
using IMS.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using Newtonsoft.Json;
using PagedList;


namespace IMS.Web.Controllers
{
    public class Add_InwardMaterialController : Controller
    {
        // GET: Add_InwardMaterial

        private ServiceVMSEntities context = new ServiceVMSEntities();
        private UnitOfWork unitOfWork = new UnitOfWork();
        private IMExceptionLogger exceptionLogger = BLLObjectCreator.CreateIMSLogger(ExceptionLoggerType.IMSText);
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Add_InwardMaterial()
        {
            if (Session["role"] != null)
            {
                var userDepartmenttype = new ServiceUserDepartment();
                var material = new Material_Category();
                var vendor = new HTV_Vendor();

                ViewBag.userDepartment = unitOfWork.DepartmentRepository.GetUserDepartment(userDepartmenttype, 0, null);
                ViewBag.materialcategory = unitOfWork.DepartmentRepository.GetMaterialCategory(material, 0);
                ViewBag.InwardID = unitOfWork.DepartmentRepository.auto_generatedCode("Inward");
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
        public ActionResult Add_InwardMaterial(IMSEntity model, HttpPostedFileBase[] DC_Filename)
        {
            string role = string.Empty;
            if (Session["role"] != null)
            {
                role = Session["role"].ToString();
            }

            if (role != null)
            {

                string userId = string.Empty;
                string DC_Number = null;


                if (Session["UserID"] != null)
                {
                    userId = Session["UserID"].ToString();
                }

                try
                {
                    string finaldata = string.Empty;
                    int j = 0;
                    List<string> materiallist = new List<string>(model.material);
                    materiallist.RemoveAt(0);

                    List<string> materialcatlist = new List<string>(model.mcatid);
                    for (int i = 0; i < model.SerialNumber; i++)
                    {

                        j = i;
                        var ii = i + 1;
                        finaldata = finaldata + ii + ";" + materiallist[j] + ";" + model.m_quantity[j] + ";" + model.invtype[j] + ";" + model.matremark[j];
                        if (i != model.SerialNumber)
                        {
                            finaldata = finaldata + "|";
                        }

                    }

                    string loc = string.Empty;
                    if (Session["role"].ToString() == "Security Guard" && Session["Location"].ToString() == "Global Port")
                    {
                        loc = "Global Port";
                    }
                    else if (Session["role"].ToString() == "Security Guard" && Session["Location"].ToString() == "SEZ")
                    {
                        loc = "SEZ";
                    }
                    else if (Session["role"].ToString() == "Security Guard" && Session["Location"].ToString() == "Siddhant")
                    {
                        loc = "Siddhant";
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
                        }
                    }
                    InwardMaterial material = new InwardMaterial();
                    material.InwardID = model.InwardID;
                    material.vendorId = model.vendorId;
                    material.TempVendorID = model.TempVendorID;
                    material.InwardDateTime = DateTime.Now;
                    material.InwardNote = model.InwardNote;
                    material.userDepartmentId = model.userDepartmentId;
                    if (model.Location != null)
                    {
                        material.Location = model.Location;
                    }
                    else
                    {
                        material.Location = loc;
                    }
                    if (model.BillofEntry != null)
                    {
                        material.BillofEntry = model.BillofEntry;
                    }
                    material.Inward_Status = "Inward Added";
                    if (model.InwardEnteredBy != null)
                    {
                        material.InwardEnteredBy = model.InwardEnteredBy;
                    }
                    else
                    {
                        material.InwardEnteredBy = userId;
                    }

                    unitOfWork.DepartmentRepository.Insert(material);
                    unitOfWork.Save();



                    for (int i = 0; i < model.SerialNumber; i++)
                    {
                        j = i;
                        if (i != model.SerialNumber)
                        {
                            InwardMaterial_Temp materialtemp = new InwardMaterial_Temp();
                            materialtemp.InvMaterialTemp_ID = unitOfWork.DepartmentRepository.GenerateInwardIDS("InwardTemp");
                            materialtemp.InwardID = model.InwardID;
                            materialtemp.Material_CategoryID = int.Parse(materialcatlist[j].ToString());
                            materialtemp.MaterialID = model.mid[j];
                            materialtemp.MaterialName = materiallist[j];
                            materialtemp.Quantity = model.m_quantity[j];
                            materialtemp.Inward_Type = model.invtype[j];
                            materialtemp.Material_Remark = model.matremark[j];
                            unitOfWork.DepartmentRepository.Insert(materialtemp);
                            unitOfWork.Save();
                        }
                    }

                    bool file_Value = generatedInwardMaterial(model, finaldata);

                    bool file_value_extra = false;
                    string DCid = string.Empty;
                    for (int i = 0; i < DC_Filename.Length; i++)
                    {
                        DCid = unitOfWork.DepartmentRepository.GenerateInwardIDS("Deliverychallen");
                        file_value_extra = unitOfWork.DepartmentRepository.filename(DC_Filename[i], "Delivery Challen", DCid, model.InwardID);

                        if (i == 0)
                        {
                            DC_Number = model.DC_Number;
                        }
                        else if (i == 1)
                        {
                            DC_Number = model.DC_Number1;
                        }
                        else if (i == 2)
                        {
                            DC_Number = model.DC_Number2;
                        }
                        else if (i == 3)
                        {
                            DC_Number = model.DC_Number3;
                        }
                        if (file_value_extra == true)
                        {
                            var DeliveryChallen = new DeliveryChallen()
                            {
                                DC_ID = DCid,
                                DC_Number = DC_Number,
                                DC_Filename = DC_Filename[i].FileName,
                                DC_Filesize = DC_Filename[i].ContentLength.ToString(),
                                InwardID = model.InwardID

                            };
                            unitOfWork.DepartmentRepository.Insert(DeliveryChallen);
                            unitOfWork.Save();
                        }


                    }

                    exceptionLogger.LogCreationForInward(model.InwardID, "added", model, null);
                    var mailsend = sendInwardMail(model.InwardID, "Inward Added");
                }
                catch (Exception e)
                {

                    var exceptionLogger = new IMSExceptionLogger()
                    {
                        controllerName = "Add_InwardMaterialController",
                        actionName = "Add_InwardMaterial(post)",
                        exceptionStackTrace = e.StackTrace,
                        exceptionMessage = e.Message,
                        exceptionLogTime = DateTime.Now

                    };
                }
                return RedirectToAction("InwardList", "Add_InwardMaterial");
            }
            else
            {
                return HttpNotFound();
            }
        }

        public bool generatedInwardMaterial(IMSEntity model1, string finaldata)
        {
            ServiceVMSEntities context = new ServiceVMSEntities();
            InwardMaterial model = (from t in context.InwardMaterials
                                    where t.InwardID == model1.InwardID
                                    select t).FirstOrDefault();
            ServiceUserDepartment department = (from t in context.ServiceUserDepartments
                                                where t.userDepartmentId == model.userDepartmentId
                                                select t).FirstOrDefault();
            int k = 0;
            string checkforbudgeted = null;
            string data = null;
            string Final_Data = string.Empty;

            if (finaldata != null)
            {
                Final_Data = finaldata;
            }

            float total_amt = 0;
            string[] rowList = Final_Data.Split('|');
            string[] dataList = null;

            for (int i = 0; i < rowList.Length; i++)
            {
                dataList = rowList[i].Split(';');
                for (int j = 0; j < dataList.Length; j++)
                {
                    if (j == 0)
                    {
                        data = data + "<tr><td>" + dataList[j] + "</td>";
                    }
                    else if (j != 0 && j != dataList.Length - 1)
                    {
                        if (j == 4)
                        {
                            total_amt = total_amt + float.Parse(dataList[j]);
                        }
                        data = data + "<td>" + dataList[j] + "</td>";
                    }
                    else if (j == dataList.Length - 1)
                    {
                        data = data + "<td>" + dataList[j] + "</td></tr>";
                    }
                }
            }

            var total_Amount = Convert.ToDecimal(total_amt).ToString("#,##0.00");
            var OutputPath = @"D:\IMSDocs\InwardMaterial\" + model.InwardID;

            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }
            else
            {
                string[] oldfilename = Directory.GetFiles(@"D:\IMSDocs\InwardMaterial\" + model.InwardID);
                List<string> items = new List<string>();
                foreach (string file1 in oldfilename)
                {
                    System.IO.File.Delete(file1);
                }
            }

            Document document = new iTextSharp.text.Document();

            Font boldFont = FontFactory.GetFont("Verdana", 10, iTextSharp.text.Font.BOLD);

            Font simple = FontFactory.GetFont("Verdana", 10);

            Font boldItalicFont = FontFactory.GetFont("Verdana", 10, iTextSharp.text.Font.BOLDITALIC);

            var imagepath = @"D:\IMSDocs\AllWebSites\";

            Image png = iTextSharp.text.Image.GetInstance(imagepath + "Harbinger Group Logo.jpg");

            PdfPTable tblImg = new PdfPTable(1);

            tblImg.WidthPercentage = 35;

            tblImg.HorizontalAlignment = Element.ALIGN_LEFT;

            PdfPCell imgCell = new PdfPCell();

            imgCell.AddElement(png);

            imgCell.BorderWidth = PdfPCell.NO_BORDER;

            tblImg.AddCell(imgCell);

            Font verdenabold = FontFactory.GetFont("verdana", 10, Font.BOLD);
            Font verdenabold1 = FontFactory.GetFont("verdana", 9, Font.BOLD);
            Font verdena = FontFactory.GetFont("verdana", 10);
            Font verdena1 = FontFactory.GetFont("verdana", 9);
            Chunk glue = new Chunk(new VerticalPositionMark());
            Phrase ph1 = new Phrase();
            Paragraph main1 = new Paragraph(model.GRN_Number, boldFont);
            main1.Alignment = Element.ALIGN_RIGHT;

            Paragraph p1 = new Paragraph("Harbinger Group", boldFont);
            p1.Alignment = Element.ALIGN_CENTER;
            Paragraph p = new Paragraph("Inward Material Form", boldFont);
            p.Alignment = Element.ALIGN_CENTER;


            Phrase phrase_hgroup = new Phrase();
            Phrase phrase_form = new Phrase();
            phrase_hgroup.Add(new Chunk("Harbinger Group", verdenabold));
            phrase_form.Add(new Chunk("Inward Material Form", verdenabold));

            Paragraph para_title = new Paragraph();
            para_title.Add(phrase_hgroup);
            para_title.Add("\n");
            para_title.Add(phrase_form);
            para_title.Alignment = Element.ALIGN_CENTER;
            k = k + 1;

            string InwordMaterial_info = "<font face=\"Verdana\" size=2><br/><br/><b><u>" + k + ".Add Inward Material Information:</u></b><br/><br/>";

            PdfPTable table = new PdfPTable(3);
            float[] width = new float[] { 39f, 2f, 59f };
            table.SetWidths(width);
            table.WidthPercentage = 100f;

            var phrase = new Phrase();
            table.DefaultCell.Border = Rectangle.NO_BORDER;
            table.HorizontalAlignment = Element.ALIGN_LEFT;

            PdfPCell cell = new PdfPCell(new Phrase("Inward Date", verdenabold1));
            cell.Border = Rectangle.NO_BORDER;
            table.AddCell(cell);
            cell = new PdfPCell(new Phrase(":", verdenabold));

            cell.Border = Rectangle.NO_BORDER;

            table.AddCell(cell);



            cell = new PdfPCell(new Phrase(DateTime.Now.ToShortDateString(), verdena1));

            // cell.Colspan = 2;

            cell.Border = Rectangle.NO_BORDER;

            cell.HorizontalAlignment = Element.ALIGN_LEFT;

            table.AddCell(cell);



            cell = new PdfPCell(new Phrase("Inward EnteredBy", verdenabold1));

            cell.Border = Rectangle.NO_BORDER;

            table.AddCell(cell);



            cell = new PdfPCell(new Phrase(":", verdenabold));

            cell.Border = Rectangle.NO_BORDER;

            table.AddCell(cell);

            if (model.InwardEnteredBy != null)
            {
                var raisedby_name = (from t in context.InwardMaterials where t.InwardID == model.InwardID select new { t.InwardEnteredBy }).FirstOrDefault();

                if (raisedby_name == null)
                {
                    cell = new PdfPCell(new Phrase(model.InwardEnteredBy, verdena1));
                }
                else
                {
                    cell = new PdfPCell(new Phrase(raisedby_name.InwardEnteredBy, verdena1));
                }

            }
            cell.Border = Rectangle.NO_BORDER;

            //  cell.Colspan = 2;

            cell.HorizontalAlignment = 0;

            //cell.Padding = 1;

            table.AddCell(cell);



            cell = new PdfPCell(new Phrase("Location", verdenabold1));

            cell.Border = Rectangle.NO_BORDER;

            table.AddCell(cell);



            cell = new PdfPCell(new Phrase(":", verdenabold));

            cell.Border = Rectangle.NO_BORDER;

            table.AddCell(cell);



            cell = new PdfPCell(new Phrase((model.Location).ToString(), verdena1));

            // cell.Colspan = 2;

            cell.Border = Rectangle.NO_BORDER;

            cell.HorizontalAlignment = Element.ALIGN_LEFT;

            table.AddCell(cell);



            cell = new PdfPCell(new Phrase("UserDepartment", verdenabold1));

            cell.Border = Rectangle.NO_BORDER;

            table.AddCell(cell);



            cell = new PdfPCell(new Phrase(":", verdenabold));

            cell.Border = Rectangle.NO_BORDER;

            table.AddCell(cell);



            cell = new PdfPCell(new Phrase((department.userDepartmentName).ToString(), verdena1));

            // cell.Colspan = 2;

            cell.Border = Rectangle.NO_BORDER;

            cell.HorizontalAlignment = Element.ALIGN_LEFT;

            table.AddCell(cell);



            cell = new PdfPCell(new Phrase("Inward ExpenseNature", verdenabold1));

            cell.Border = Rectangle.NO_BORDER;

            table.AddCell(cell);



            cell = new PdfPCell(new Phrase(":", verdenabold));

            cell.Border = Rectangle.NO_BORDER;

            table.AddCell(cell);



            cell = new PdfPCell(new Phrase(model.Inward_ExpenseNature, verdena1));

            // cell.Colspan = 2;

            cell.Border = Rectangle.NO_BORDER;

            cell.HorizontalAlignment = Element.ALIGN_LEFT;

            table.AddCell(cell);



            cell = new PdfPCell(new Phrase("Inward Status", verdenabold1));

            cell.Border = Rectangle.NO_BORDER;

            table.AddCell(cell);



            cell = new PdfPCell(new Phrase(":", verdenabold));

            cell.Border = Rectangle.NO_BORDER;

            table.AddCell(cell);



            cell = new PdfPCell(new Phrase((model.Inward_Status).ToString(), verdena1));

            // cell.Colspan = 2;

            cell.Border = Rectangle.NO_BORDER;

            cell.HorizontalAlignment = Element.ALIGN_LEFT;

            table.AddCell(cell);



            k = k + 1;

            string service_info = "<font face=\"Verdana\" size=2><br/><br/><b><u>" + k + ". Inward Material Information:</u></b><br/><br/>";

            string tabledata = "<table border = 1 style =\"text-align:center\"><tr><b><th>Sr.no.</th>" +

                        "<th>Material</th><th>Quantity</th><th>Inward_Type</th><th>Remark</th></b></tr>" + data +

                        "</table></font>";

            string More_info = null;

            PdfPTable more_info_table = new PdfPTable(3);

            width = new float[] { 20f, 2f, 78f };

            more_info_table.SetWidths(width);

            more_info_table.WidthPercentage = 100f;

            k = k + 1;

            string Vendor_info = "<font face=\"Verdana\" size=2><br/><br/><b><u>" + k + ".Vendor Information:</u></b><br/><br/>";


            PdfPTable recomended_vendor_info_table = new PdfPTable(3);

            float[] width1 = new float[] { 39f, 2f, 59f };
            recomended_vendor_info_table.SetWidths(width1);
            recomended_vendor_info_table.WidthPercentage = 100f;

            phrase = new Phrase();
            recomended_vendor_info_table.DefaultCell.Border = Rectangle.NO_BORDER;
            recomended_vendor_info_table.HorizontalAlignment = Element.ALIGN_LEFT;
            //recomended_vendor_info_table.AddCell(cell);
            string vendorName = null;
            string vendorAddress = null;
            string mobileNo = null;
            var vendorInfo = (from u in context.HTV_Vendor
                              where u.vendorId == model.vendorId
                              select new { u.vendorName, u.vendorAddress, u.mobileNo }).FirstOrDefault();

            if (vendorInfo == null)
            {
                vendorInfo = (from t in context.TempVendors
                              where t.TempVendorID == model.TempVendorID
                              select new
                              {
                                  vendorName = t.TempVendorName,
                                  vendorAddress = t.TempVendorAddress,
                                  mobileNo = t.TempVendorContact
                              }).FirstOrDefault();
            }


            if (vendorInfo != null)
            {
                vendorName = vendorInfo.vendorName;
                vendorAddress = vendorInfo.vendorAddress;
                mobileNo = vendorInfo.mobileNo;
            }
            more_info_table.DefaultCell.Border = Rectangle.NO_BORDER;

            more_info_table.HorizontalAlignment = Element.ALIGN_LEFT;



            PdfPCell cell1 = new PdfPCell(new Phrase("Vendor Name", verdenabold1));
            cell1.Border = Rectangle.NO_BORDER;
            recomended_vendor_info_table.AddCell(cell1);

            cell1 = new PdfPCell(new Phrase(":", verdenabold));
            cell1.Border = Rectangle.NO_BORDER;
            recomended_vendor_info_table.AddCell(cell1);

            cell1 = new PdfPCell(new Phrase(vendorName, verdena1));
            cell1.Border = Rectangle.NO_BORDER;
            cell1.HorizontalAlignment = Element.ALIGN_LEFT;
            recomended_vendor_info_table.AddCell(cell1);

            cell1 = new PdfPCell(new Phrase("Address", verdenabold1));
            cell1.Border = Rectangle.NO_BORDER;
            recomended_vendor_info_table.AddCell(cell1);

            cell1 = new PdfPCell(new Phrase(":", verdenabold));
            cell1.Border = Rectangle.NO_BORDER;
            recomended_vendor_info_table.AddCell(cell1);


            cell1 = new PdfPCell(new Phrase(vendorAddress, verdena1));
            cell1.Border = Rectangle.NO_BORDER;
            cell1.HorizontalAlignment = 0;
            recomended_vendor_info_table.AddCell(cell1);



            cell1 = new PdfPCell(new Phrase("Contact Details", verdenabold1));
            cell1.Border = Rectangle.NO_BORDER;
            recomended_vendor_info_table.AddCell(cell1);

            cell1 = new PdfPCell(new Phrase(":", verdenabold));
            cell1.Border = Rectangle.NO_BORDER;
            recomended_vendor_info_table.AddCell(cell1);


            cell1 = new PdfPCell(new Phrase(mobileNo, verdena1));
            cell1.Border = Rectangle.NO_BORDER;
            cell1.HorizontalAlignment = 0;
            recomended_vendor_info_table.AddCell(cell1);


            table.SpacingBefore = 10f;

            table.SpacingAfter = 12.5f;

            more_info_table.SpacingAfter = 12.5f;

            k = k + 1;

            Phrase phrase1 = new Phrase();

            phrase1.Add(new Chunk(k + ".Inward Material Entered By: ", verdenabold));



            if (model.InwardEnteredBy != null)
            {
                phrase1.Add(new Chunk(model.InwardEnteredBy, verdena));
            }

            Paragraph para = new Paragraph();
            para.Add(phrase1);
            k = k + 1;
            Phrase phrase2 = new Phrase();

            phrase2.Add(new Chunk(k + ".Inward Material Approved By: ", verdenabold));

            //var approved_name = null;

            /* if (model.u != null)

             {

                 var approved_name = (from t in context.InwardApprovers where t. == model.approvedBy select new { t.userName }).FirstOrDefault();

                 phrase2.Add(new Chunk(approved_name.userName, verdena));



             }*/
            Paragraph para2 = new Paragraph();

            para2.Add(phrase2);
            string spacing = "<br/>";

            PdfWriter.GetInstance(document, new FileStream(OutputPath + "\\" + model.InwardID + ".pdf", FileMode.Create));





            document.Open();

            iTextSharp.text.html.simpleparser.HTMLWorker hw = new iTextSharp.text.html.simpleparser.HTMLWorker(document);



            iTextSharp.text.html.simpleparser.StyleSheet styles = new iTextSharp.text.html.simpleparser.StyleSheet();



            document.Add(tblImg);

            document.Add(main1);

            document.Add(para_title);



            hw.Parse(new StringReader(InwordMaterial_info));

            document.Add(table);

            //hw.Parse(new StringReader(budget_info));

            //document.Add(Budget_info_table);

            hw.Parse(new StringReader(service_info));

            hw.Parse(new StringReader(tabledata));

            /*   if (model1.paymentDetail != null || (model1.evaluationNote != null))

               {

                   hw.Parse(new StringReader(More_info));

                   document.Add(more_info_table);



               }*/



            hw.Parse(new StringReader(Vendor_info));

            document.Add(recomended_vendor_info_table);

            hw.Parse(new StringReader(spacing));

            hw.Parse(new StringReader(spacing));

            document.Add(para);

            hw.Parse(new StringReader(spacing));

            document.Add(para2);
            document.Close();



            return true;

        }

        public bool UpdateInwardPDF(string InwardId)
        {
            try
            {
                ServiceVMSEntities context = new ServiceVMSEntities();
                string role = string.Empty;
                string empid = string.Empty;
                var httpCookie1 = Session["role"].ToString();
                if (httpCookie1 != null)
                {
                    role = httpCookie1.ToString();
                }
                var httpCookie2 = HttpContext.Session["UserID"].ToString();
                if (httpCookie2 != null)
                {
                    empid = httpCookie2.ToString();
                }

                InwardMaterial model = (from t in context.InwardMaterials where t.InwardID == InwardId select t).FirstOrDefault();

                ServiceUserDepartment department = (from t in context.ServiceUserDepartments where t.userDepartmentId == model.userDepartmentId select t).FirstOrDefault();
                int k = 0;

                string data = null;
                string finaldata = string.Empty;
                int srno = 1;
                if (model.Inward_ExpenseNature == "capex")
                {

                    var materialupdate1 = (from t in context.InwardMaterial_CAPEX
                                           where t.InwardID == InwardId && t.Status != "Material Rejected"
                                           select t).ToList();
                    foreach (var item in materialupdate1)
                    {

                        finaldata = finaldata + srno + ";" + item.MaterialName + ";" + item.Quantity + ";" + item.Inward_Type + ";" + item.Material_Remark;
                        srno++;
                        finaldata = finaldata + "|";
                    }
                }
                else if (model.Inward_ExpenseNature == "opex")
                {
                    var materialupdate2 = (from t in context.InwardMaterial_OPEX
                                           where t.InwardID == InwardId && t.Status != "Material Rejected"
                                           select t).ToList();
                    foreach (var item in materialupdate2)
                    {

                        finaldata = finaldata + srno + ";" + item.Material_Name + ";" + item.Quantity + ";" + item.Inward_Type + ";" + item.Material_Remark;
                        srno++;
                        finaldata = finaldata + "|";
                    }
                }
                else if (model.Inward_ExpenseNature == "rental")
                {
                    var materialupdate3 = (from t in context.InwardMaterial_Rental
                                           where t.InwardID == InwardId && t.Status != "Material Rejected"
                                           select t).ToList();
                    foreach (var item in materialupdate3)
                    {

                        finaldata = finaldata + srno + ";" + item.MaterialName + ";" + item.Quantity + ";" + item.Inward_Type + ";" + item.Material_Remark;
                        srno++;
                        finaldata = finaldata + "|";
                    }
                }
                else if (model.Inward_ExpenseNature == "customerasset")
                {
                    var materialupdate4 = (from t in context.InwardMaterial_CustomerAsset
                                           where t.InwardID == InwardId && t.Status != "Material Rejected"
                                           select t).ToList();
                    foreach (var item in materialupdate4)
                    {

                        finaldata = finaldata + srno + ";" + item.MaterialName + ";" + item.Quantity + ";" + item.Inward_Type + ";" + item.Material_Remark;
                        srno++;
                        finaldata = finaldata + "|";
                    }
                }
                    string Final_Data = string.Empty;

                if (finaldata != null)
                {
                    Final_Data = finaldata;
                }

                float total_amt = 0;

                string[] rowList = finaldata.Split('|');

                string[] dataList = null;

                for (int i = 0; i < rowList.Length; i++)
                {
                    dataList = rowList[i].Split(';');
                    for (int j = 0; j < dataList.Length; j++)
                    {
                        if (j == 0)
                        {
                            data = data + "<tr><td>" + dataList[j] + "</td>";
                        }
                        else if (j != 0 && j != dataList.Length - 1)
                        {
                            if (j == 4)
                            {
                                total_amt = total_amt + float.Parse(dataList[j]);
                            }
                            data = data + "<td>" + dataList[j] + "</td>";
                        }
                        else if (j == dataList.Length - 1)
                        {
                            data = data + "<td>" + dataList[j] + "</td></tr>";
                        }
                    }
                }


                var OutputPath = @"D:\IMSDocs\InwardMaterial\" + InwardId;

                if (!Directory.Exists(OutputPath))
                {
                    Directory.CreateDirectory(OutputPath);
                }
                else
                {
                    string[] oldfilename = Directory.GetFiles(@"D:\IMSDocs\InwardMaterial\" + model.InwardID);

                    List<string> items = new List<string>();

                    foreach (string file1 in oldfilename)
                    {
                        System.IO.File.Delete(file1);
                    }
                }
                Document document = new iTextSharp.text.Document();

                Font boldFont = FontFactory.GetFont("Verdana", 10, iTextSharp.text.Font.BOLD);

                Font simple = FontFactory.GetFont("Verdana", 10);

                Font boldItalicFont = FontFactory.GetFont("Verdana", 10, iTextSharp.text.Font.BOLDITALIC);

                var imagepath = @"D:\IMSDocs\AllWebSites\";

                Image png = iTextSharp.text.Image.GetInstance(imagepath + "Harbinger Group Logo.jpg");

                PdfPTable tblImg = new PdfPTable(1);

                tblImg.WidthPercentage = 35;

                tblImg.HorizontalAlignment = Element.ALIGN_LEFT;

                PdfPCell imgCell = new PdfPCell();
                imgCell.AddElement(png);

                imgCell.BorderWidth = PdfPCell.NO_BORDER;

                tblImg.AddCell(imgCell);



                Font verdenabold = FontFactory.GetFont("verdana", 10, Font.BOLD);

                Font verdenabold1 = FontFactory.GetFont("verdana", 9, Font.BOLD);

                Font verdena = FontFactory.GetFont("verdana", 10);

                Font verdena1 = FontFactory.GetFont("verdana", 9);



                Chunk glue = new Chunk(new VerticalPositionMark());

                Phrase ph1 = new Phrase();



                Paragraph main1 = new Paragraph(model.GRN_Number, boldFont);

                main1.Alignment = Element.ALIGN_RIGHT;



                Paragraph p1 = new Paragraph("Harbinger Group", boldFont);

                p1.Alignment = Element.ALIGN_CENTER;



                Paragraph p = new Paragraph("Inward Material Form", boldFont);

                p.Alignment = Element.ALIGN_CENTER;

                Phrase phrase_hgroup = new Phrase();

                Phrase phrase_form = new Phrase();

                phrase_hgroup.Add(new Chunk("Harbinger Group", verdenabold));

                phrase_form.Add(new Chunk("Inward Material Form", verdenabold));


                Paragraph para_title = new Paragraph();

                para_title.Add(phrase_hgroup);

                para_title.Add("\n");

                para_title.Add(phrase_form);

                para_title.Alignment = Element.ALIGN_CENTER;

                k = k + 1;

                string InwordMaterial_info = "<font face=\"Verdana\" size=2><br/><br/><b><u>" + k + ".Add Inward Material Information:</u></b><br/><br/>";



                PdfPTable table = new PdfPTable(3);

                float[] width = new float[] { 39f, 2f, 59f };

                table.SetWidths(width);

                table.WidthPercentage = 100f;



                var phrase = new Phrase();
                table.DefaultCell.Border = Rectangle.NO_BORDER;

                table.HorizontalAlignment = Element.ALIGN_LEFT;





                PdfPCell cell = new PdfPCell(new Phrase("Inward Date", verdenabold1));

                cell.Border = Rectangle.NO_BORDER;

                table.AddCell(cell);



                cell = new PdfPCell(new Phrase(":", verdenabold));

                cell.Border = Rectangle.NO_BORDER;

                table.AddCell(cell);



                cell = new PdfPCell(new Phrase(DateTime.Now.ToShortDateString(), verdena1));

                // cell.Colspan = 2;

                cell.Border = Rectangle.NO_BORDER;

                cell.HorizontalAlignment = Element.ALIGN_LEFT;

                table.AddCell(cell);



                cell = new PdfPCell(new Phrase("Inward EnteredBy", verdenabold1));

                cell.Border = Rectangle.NO_BORDER;

                table.AddCell(cell);



                cell = new PdfPCell(new Phrase(":", verdenabold));

                cell.Border = Rectangle.NO_BORDER;

                table.AddCell(cell);

                if (model.InwardEnteredBy != null)
                {
                    var raisedby_name = (from t in context.InwardMaterials where t.InwardID == model.InwardID select new { t.InwardEnteredBy }).FirstOrDefault();

                    if (raisedby_name == null)
                    {
                        cell = new PdfPCell(new Phrase(model.InwardEnteredBy, verdena1));
                    }
                    else
                    {
                        cell = new PdfPCell(new Phrase(raisedby_name.InwardEnteredBy, verdena1));
                    }

                }
                cell.Border = Rectangle.NO_BORDER;

                cell.HorizontalAlignment = 0;

                table.AddCell(cell);

                cell = new PdfPCell(new Phrase("Location", verdenabold1));

                cell.Border = Rectangle.NO_BORDER;

                table.AddCell(cell);



                cell = new PdfPCell(new Phrase(":", verdenabold));

                cell.Border = Rectangle.NO_BORDER;

                table.AddCell(cell);



                cell = new PdfPCell(new Phrase((model.Location).ToString(), verdena1));

                // cell.Colspan = 2;

                cell.Border = Rectangle.NO_BORDER;

                cell.HorizontalAlignment = Element.ALIGN_LEFT;

                table.AddCell(cell);



                cell = new PdfPCell(new Phrase("User Department", verdenabold1));

                cell.Border = Rectangle.NO_BORDER;

                table.AddCell(cell);



                cell = new PdfPCell(new Phrase(":", verdenabold));

                cell.Border = Rectangle.NO_BORDER;

                table.AddCell(cell);



                cell = new PdfPCell(new Phrase((department.userDepartmentName).ToString(), verdena1));

                // cell.Colspan = 2;

                cell.Border = Rectangle.NO_BORDER;

                cell.HorizontalAlignment = Element.ALIGN_LEFT;

                table.AddCell(cell);



                cell = new PdfPCell(new Phrase("Inward ExpenseNature", verdenabold1));

                cell.Border = Rectangle.NO_BORDER;

                table.AddCell(cell);



                cell = new PdfPCell(new Phrase(":", verdenabold));

                cell.Border = Rectangle.NO_BORDER;

                table.AddCell(cell);



                cell = new PdfPCell(new Phrase(model.Inward_ExpenseNature, verdena1));

                // cell.Colspan = 2;

                cell.Border = Rectangle.NO_BORDER;

                cell.HorizontalAlignment = Element.ALIGN_LEFT;

                table.AddCell(cell);



                cell = new PdfPCell(new Phrase("Inward Status", verdenabold1));

                cell.Border = Rectangle.NO_BORDER;

                table.AddCell(cell);



                cell = new PdfPCell(new Phrase(":", verdenabold));

                cell.Border = Rectangle.NO_BORDER;

                table.AddCell(cell);



                cell = new PdfPCell(new Phrase((model.Inward_Status).ToString(), verdena1));

                // cell.Colspan = 2;

                cell.Border = Rectangle.NO_BORDER;

                cell.HorizontalAlignment = Element.ALIGN_LEFT;

                table.AddCell(cell);
                k = k + 1;

                string service_info = "<font face=\"Verdana\" size=2><br/><br/><b><u>" + k + ". Inward Material Information:</u></b><br/><br/>";


                string tabledata = "<table border = 1 style =\"text-align:center\"><tr><b><th>Category</th>" +

                            "<th>Material</th><th>Quantity</th><th>Inward_Type</th><th>Remark</th></b></tr>" + data +

                            "</table></font>";

                string More_info = null;


                PdfPTable more_info_table = new PdfPTable(3);

                width = new float[] { 20f, 2f, 78f };

                more_info_table.SetWidths(width);

                more_info_table.WidthPercentage = 100f;


                k = k + 1;

                string Vendor_info = "<font face=\"Verdana\" size=2><br/><br/><b><u>" + k + ".Recomended Vendor Information:</u></b><br/><br/>";

                PdfPTable recomended_vendor_info_table = new PdfPTable(3);

                width = new float[] { 29f, 2f, 69f };

                recomended_vendor_info_table.SetWidths(width);

                recomended_vendor_info_table.WidthPercentage = 100f;



                phrase = new Phrase();
                //recomended_vendor_info_table.AddCell(cell);
                string vendorName = null;
                string vendorAddress = null;
                string mobileNo = null;
                var vendorInfo = (from u in context.HTV_Vendor
                                  where u.vendorId == model.vendorId
                                  select new { u.vendorName, u.vendorAddress, u.mobileNo }).FirstOrDefault();

                if (vendorInfo == null)
                {
                    vendorInfo = (from t in context.TempVendors
                                  where t.TempVendorID == model.TempVendorID
                                  select new
                                  {
                                      vendorName = t.TempVendorName,
                                      vendorAddress = t.TempVendorAddress,
                                      mobileNo = t.TempVendorContact
                                  }).FirstOrDefault();
                }


                if (vendorInfo != null)
                {
                    vendorName = vendorInfo.vendorName;
                    vendorAddress = vendorInfo.vendorAddress;
                    mobileNo = vendorInfo.mobileNo;
                }
                more_info_table.DefaultCell.Border = Rectangle.NO_BORDER;

                more_info_table.HorizontalAlignment = Element.ALIGN_LEFT;



                cell = new PdfPCell(new Phrase("Vendor Name", verdenabold1));

                cell.Border = Rectangle.NO_BORDER;

                recomended_vendor_info_table.AddCell(cell);



                cell = new PdfPCell(new Phrase(":", verdenabold));

                cell.Border = Rectangle.NO_BORDER;

                recomended_vendor_info_table.AddCell(cell);



                cell = new PdfPCell(new Phrase(vendorName, verdena1));

                // cell.Colspan = 2;

                cell.Border = Rectangle.NO_BORDER;

                cell.HorizontalAlignment = Element.ALIGN_LEFT;

                recomended_vendor_info_table.AddCell(cell);



                cell = new PdfPCell(new Phrase("Address", verdenabold1));

                cell.Border = Rectangle.NO_BORDER;

                recomended_vendor_info_table.AddCell(cell);



                cell = new PdfPCell(new Phrase(":", verdenabold));

                cell.Border = Rectangle.NO_BORDER;

                recomended_vendor_info_table.AddCell(cell);

                cell = new PdfPCell(new Phrase(vendorAddress, verdena1));

                cell.Border = Rectangle.NO_BORDER;

                //  cell.Colspan = 2;

                cell.HorizontalAlignment = 0;

                //cell.Padding = 1;

                recomended_vendor_info_table.AddCell(cell);



                cell = new PdfPCell(new Phrase("Contact Details", verdenabold1));

                cell.Border = Rectangle.NO_BORDER;

                recomended_vendor_info_table.AddCell(cell);



                cell = new PdfPCell(new Phrase(":", verdenabold));

                cell.Border = Rectangle.NO_BORDER;

                recomended_vendor_info_table.AddCell(cell);



                cell = new PdfPCell(new Phrase(mobileNo, verdena1));

                cell.Border = Rectangle.NO_BORDER;

                //  cell.Colspan = 2;

                cell.HorizontalAlignment = 0;

                //cell.Padding = 1;

                recomended_vendor_info_table.AddCell(cell);
                table.SpacingBefore = 10f;

                table.SpacingAfter = 12.5f;


                more_info_table.SpacingAfter = 12.5f;

                k = k + 1;

                Phrase phrase1 = new Phrase();

                phrase1.Add(new Chunk(k + ".Inward Material Entered By: ", verdenabold));



                if (model.InwardEnteredBy != null)
                {
                    phrase1.Add(new Chunk(model.InwardEnteredBy, verdena));
                }
                Paragraph para = new Paragraph();

                para.Add(phrase1);


                k = k + 1;
                Phrase phrase2 = new Phrase();

                phrase2.Add(new Chunk(k + ".Inward Material Approved By: ", verdenabold));

                if (Session["UserID"] != null)
                {
                    empid = Session["UserID"].ToString();

                }

                //var approved_name = (from t in context.IMSUsers
                //                     where t.userId == empid
                //                     select new { t.userName }).FirstOrDefault();
                //phrase2.Add(new Chunk(approved_name.userName, verdena));

                //var approved_name = null;

                /* if (model.u != null)

                 {

                     var approved_name = (from t in context.InwardApprovers where t. == model.approvedBy select new { t.userName }).FirstOrDefault();

                     phrase2.Add(new Chunk(approved_name.userName, verdena));



                 }*/

                Paragraph para2 = new Paragraph();

                para2.Add(phrase2);
                string spacing = "<br/>";

                PdfWriter.GetInstance(document, new FileStream(OutputPath + "\\" + model.InwardID + ".pdf", FileMode.Create));

                document.Open();

                iTextSharp.text.html.simpleparser.HTMLWorker hw = new iTextSharp.text.html.simpleparser.HTMLWorker(document);
                iTextSharp.text.html.simpleparser.StyleSheet styles = new iTextSharp.text.html.simpleparser.StyleSheet();

                document.Add(tblImg);

                document.Add(main1);

                document.Add(para_title);
                hw.Parse(new StringReader(InwordMaterial_info));

                document.Add(table);

                hw.Parse(new StringReader(service_info));

                hw.Parse(new StringReader(tabledata));


                hw.Parse(new StringReader(Vendor_info));

                document.Add(recomended_vendor_info_table);

                hw.Parse(new StringReader(spacing));

                hw.Parse(new StringReader(spacing));

                document.Add(para);

                hw.Parse(new StringReader(spacing));

                document.Add(para2);
                document.Close();
                return true;
            }
            catch (Exception e)
            {
                Console.Write(e);
                var exceptionLogger = new IMSExceptionLogger()
                {
                    controllerName = "Add_InwardMaterialController",
                    actionName = "UpdateInwardPDF",
                    exceptionStackTrace = e.StackTrace,
                    exceptionMessage = e.Message,
                    exceptionLogTime = DateTime.Now
                };
                unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                unitOfWork.Save();
                return false;
            }

        }

        [HttpGet]
        public ActionResult Edit_InwardMaterial(string id)
        {
            if (@Session["Email"] != null)
            {
                try
                {
                    id = unitOfWork.DepartmentRepository.Decode(id);
                    var userDepartmenttype = new ServiceUserDepartment();
                    ViewBag.userDepartment = unitOfWork.DepartmentRepository.GetUserDepartment(userDepartmenttype, 0, null);
                    string empid = string.Empty;
                    HTV_Vendor vendor = new HTV_Vendor();
                    Material_Category material = new Material_Category();
                    ViewBag.vendorList = unitOfWork.DepartmentRepository.Get(vendor, null);
                    ViewBag.materialcategory = unitOfWork.DepartmentRepository.GetMaterialCategory(material, 0);
                    HttpCookie httpCookie1 = Request.Cookies["employeeId"];
                    ServiceVMSEntities context = new ServiceVMSEntities();
                    ViewBag.role = Session["role"].ToString();
                    if (Session["Location"] != null)
                    {
                        ViewBag.location = Session["Location"].ToString();
                    }
                    InwardMaterial inward_edit = context.InwardMaterials.Where(s => s.InwardID == id).FirstOrDefault();

                    var vendorDetails = context.TempVendors.FirstOrDefault(v => v.TempVendorID == inward_edit.TempVendorID);

                    ViewBag.VendorName = vendorDetails?.TempVendorName;
                    ViewBag.VendorEmail = vendorDetails?.TempVendorEmail;
                    ViewBag.VendorContact = vendorDetails?.TempVendorContact;
                    ViewBag.VendorAddress = vendorDetails?.TempVendorAddress;

                    var vmsentity = new List<IMSEntity>();
                    var imsentity = new IMSEntity();
                    if (httpCookie1 != null)
                    {
                        empid = unitOfWork.DepartmentRepository.Decode(httpCookie1.Value);
                    }

                    ViewBag.empid = empid;
                    int k = 0;
                    var product_details = new List<IMSEntity>();
                    var materialdetails = (from t in context.Material_Category
                                           join c in context.Materials on t.Material_CategoryID equals c.Material_CategoryID
                                           join i in context.InwardMaterial_Temp on c.MaterialID equals i.MaterialID
                                           where i.InwardID.Equals(id) && i.Status != "Deleted"
                                           select new
                                           {
                                               i.InwardID,
                                               i.MaterialID,
                                               i.Material_Remark,
                                               i.MaterialName,
                                               i.Inward_Type,
                                               i.Quantity,
                                               i.InvMaterialTemp_ID,
                                               t.Material_CategoryID,
                                               t.Material_CategoryName
                                           });

                    ViewBag.rowlength = materialdetails.Count();
                    string details = null;
                    string qty = null;
                    string rate = null;
                    string total = null;
                    float totalValue = 0;
                    string remarks = null;
                    string srno = null;

                    foreach (var item in materialdetails)
                    {
                        vmsentity.Add(new IMSEntity()
                        {
                            InvMaterialTemp_ID = item.InvMaterialTemp_ID,
                            MaterialID = item.MaterialID,
                            MaterialName = item.MaterialName,
                            Material_Remark = item.Material_Remark,
                            Quantity = item.Quantity,
                            Inward_Type = item.Inward_Type,
                            Material_CategoryID = item.Material_CategoryID,
                            Material_CategoryName = item.Material_CategoryName
                        });


                    }

                    Material mat = new Material();
                    var matlist = unitOfWork.DepartmentRepository.GetInwardMaterials(mat, id);
                    List<string> materiallist = matlist.Select(x => x.MaterialID).ToList();
                    ViewBag.MaterialIdlist = materiallist;

                    DeliveryChallen delivery = new DeliveryChallen();
                    var deliverychallenlist = unitOfWork.DepartmentRepository.Get(delivery, id);
                    ViewBag.DClist = deliverychallenlist;
                    ViewBag.Count = materialdetails.Count();
                    ViewBag.Details = vmsentity;
                    ViewBag.billofentry = inward_edit.BillofEntry;
                    ViewBag.InwardID = inward_edit.InwardID;

                    List<string> dcNumber = deliverychallenlist.Select(item => item.DC_Number).ToList();
                    ViewBag.DCNUmberyest = dcNumber;
                    imsentity.InwardID = inward_edit.InwardID;
                    imsentity.vendorId = inward_edit.vendorId;
                    imsentity.TempVendorID = inward_edit.TempVendorID;
                    imsentity.InwardDateTime = inward_edit.InwardDateTime;
                    imsentity.userDepartmentId = inward_edit.userDepartmentId;
                    imsentity.Location = inward_edit.Location;
                    imsentity.InwardEnteredBy = inward_edit.InwardEnteredBy;

                    return View(imsentity);
                }
                catch (Exception e)
                {
                    Console.Write(e);
                    var exceptionLogger = new IMSExceptionLogger()
                    {
                        controllerName = "Add_InwardMaterialController",
                        actionName = "Edit_InwardMaterial(Get)",
                        exceptionStackTrace = e.StackTrace,
                        exceptionMessage = e.Message,
                        exceptionLogTime = DateTime.Now
                    };
                    unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                    unitOfWork.Save();
                    return View();
                }
            }
            else
            {
                return HttpNotFound();
            }
        }

        public ActionResult CheckDCNumberExists(string DCNumber)
        {
            bool exists = context.DeliveryChallens.Any(item => item.DC_Number == DCNumber);

            return Json(new { exists = exists });
        }
        [HttpPost]
        public ActionResult Edit_InwardMaterial(IMSEntity model, HttpPostedFileBase[] DC_Filename, HttpPostedFileBase DC_Filename_EXI, IEnumerable<IMSEntity> editedRows)
        {
            string role = string.Empty;
            string empid = string.Empty;
            string DC_Number = null;
            HttpCookie httpCookie1 = Request.Cookies["role"];
            if (httpCookie1 != null)
            {
                role = unitOfWork.DepartmentRepository.Decode(httpCookie1.Value);
            }
            HttpCookie httpCookie2 = Request.Cookies["employeeId"];
            if (httpCookie1 != null)
            {
                empid = unitOfWork.DepartmentRepository.Decode(httpCookie2.Value);
            }
            exceptionLogger.LogCreationForInwardEdit("Edit_InwardMaterial", $"User: {empid}");
            if (empid == "")
            {
                if (@Session["Email"] != null)
                {
                    empid = @Session["Email"].ToString();
                }
            }

            try
            {
                exceptionLogger.LogCreationForInwardEdit("Edit_InwardMaterial", $"Started editing inward material with ID: {model.InwardID}");
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
                    }
                }
                string finaldata = string.Empty;
                int j = 0;
                List<string> materiallist = new List<string>(model.material);
                materiallist.RemoveAt(0);

                List<string> inwardtemplist = new List<string>(model.InvTemp_ID);
                inwardtemplist.RemoveAt(0);

                List<string> materialcatlist = new List<string>(model.mcatid);


                for (int i = 0; i < model.SerialNumber; i++)
                {

                    j = i;
                    var ii = i + 1;
                    finaldata = finaldata + ii + ";" + materiallist[j] + ";" + model.m_quantity[j] + ";" + model.invtype[j] + ";" + model.matremark[j];
                    if (i != model.SerialNumber)
                    {
                        finaldata = finaldata + "|";
                    }

                }
                bool file_Value = generatedInwardMaterial(model, finaldata);
                var edit_inv = new List<IMSEntity>();
                var edit_data = (from i in context.InwardMaterials
                                 where i.InwardID.Equals(model.InwardID)
                                 select new
                                 {
                                     i.InwardID,
                                     i.vendorId,
                                     i.TempVendorID,
                                     i.GRN_Number,
                                     i.InwardDateTime,
                                     i.InwardNote,
                                     i.Inward_ExpenseNature,
                                     i.userDepartmentId,
                                     i.Location,
                                     i.InwardEnteredBy,
                                     i.BillofEntry,
                                     i.Inward_Status
                                 });
                var locations = edit_data.Select(s => s.TempVendorID).ToList();
                foreach (var e in edit_data)
                {
                    edit_inv.Add(new IMSEntity
                    {
                        InwardID = e.InwardID,
                        vendorId = e.vendorId,
                        TempVendorID = e.TempVendorID,
                        GRN_Number = e.GRN_Number,
                        InwardDateTime = e.InwardDateTime,
                        InwardNote = e.InwardNote,
                        Inward_ExpenseNature = e.Inward_ExpenseNature,
                        userDepartmentId = e.userDepartmentId,
                        Location = e.Location,
                        InwardEnteredBy = e.InwardEnteredBy,
                        BillofEntry = e.BillofEntry,
                        Inward_Status = e.Inward_Status
                    });
                }

                var original = new EntityCollection<IMSEntity>();

                foreach (var item in edit_inv)
                {
                    original.Add(item);

                }
                try
                {
                    var inwardMaterial = context.InwardMaterials.SingleOrDefault(x => x.InwardID == model.InwardID);
                    if (inwardMaterial != null)
                    {
                        inwardMaterial.vendorId = model.vendorId;
                        inwardMaterial.TempVendorID = model.TempVendorID;
                        inwardMaterial.InwardEnteredBy = model.InwardEnteredBy;
                        inwardMaterial.userDepartmentId = model.userDepartmentId;
                        inwardMaterial.InwardDateTime = DateTime.Now;
                        context.SaveChanges();
                    }
                    foreach (var row in editedRows)
                    {
                        exceptionLogger.LogCreationForInwardEdit("Edit_InwardMaterial", $"Updated existing Delivery Challen for Inward ID: {model.InwardID}, DC ID: {row.DC_ID}");

                        if (row.DC_Filename != null)
                        {
                            var DC_Challen = context.DeliveryChallens.Single(x => x.DC_ID == row.DC_ID);
                            string abc = row.DC_Filename.FileName;
                            DC_Challen.DC_ID = row.DC_ID;
                            DC_Challen.DC_Number = row.DC_Number;
                            DC_Challen.DC_Filename = abc;
                            DC_Challen.DC_Filesize = row.DC_Filename.ContentLength.ToString();
                            DC_Challen.InwardID = model.InwardID;
                        }
                        else
                        {
                            var DC_Challen = context.DeliveryChallens.Single(x => x.DC_ID == row.DC_ID);
                            string abc = row.DC_Filename_EXI;
                            string size = row.DC_Filesize_EXI;
                            DC_Challen.DC_ID = row.DC_ID;
                            DC_Challen.DC_Number = row.DC_Number;
                            DC_Challen.DC_Filename = abc;
                            DC_Challen.DC_Filesize = size;
                            DC_Challen.InwardID = model.InwardID;
                        }

                        /*unitOfWork.DepartmentRepository.Insert(dc);*/
                        context.SaveChanges();
                    }

                }
                catch (Exception e)
                {
                    var exceptionLogger = new IMSExceptionLogger()
                    {
                        controllerName = "Add_InwardMaterialController",
                        actionName = "Edit_InwardMaterial(POST)",
                        exceptionStackTrace = e.StackTrace,
                        exceptionMessage = e.Message,
                        exceptionLogTime = DateTime.Now
                    };
                    unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                    unitOfWork.Save();
                }

                IMSEntity original1 = original.FirstOrDefault();
                var query = context.InwardMaterials.Single(x => x.InwardID == model.InwardID);
                query.vendorId = model.vendorId;
                model.TempVendorID = model.TempVendorID;
                query.InwardEnteredBy = model.InwardEnteredBy;
                query.userDepartmentId = model.userDepartmentId;
                query.InwardDateTime = DateTime.Now;
                context.SaveChanges();
                //  exceptionLogger.LogCreationForInwardEdit(model, "Edit");
                for (int i = 0; i < model.SerialNumber; i++)
                {
                    j = i;
                    if (i != model.SerialNumber)
                    {
                        if (inwardtemplist[i] == "")
                        {
                            InwardMaterial_Temp materialtemp = new InwardMaterial_Temp();
                            materialtemp.InvMaterialTemp_ID = unitOfWork.DepartmentRepository.auto_generatedCode();
                            materialtemp.InwardID = model.InwardID;
                            materialtemp.Material_CategoryID = int.Parse(materialcatlist[j].ToString());
                            materialtemp.MaterialID = model.mid[j];
                            materialtemp.MaterialName = materiallist[j];
                            materialtemp.Quantity = model.m_quantity[j];
                            materialtemp.Inward_Type = model.invtype[j];
                            materialtemp.Material_Remark = model.matremark[j];
                            unitOfWork.DepartmentRepository.Insert(materialtemp);
                            unitOfWork.Save();
                        }
                        else
                        {
                            //var tempid = inwardtemplist[i].ToString();
                            //var materialtemp = context.InwardMaterial_Temp.Single(x => x.InvMaterialTemp_ID == tempid);
                            //materialtemp.InwardID = model.InwardID;
                            //materialtemp.Material_CategoryID = int.Parse(materialcatlist[j].ToString());
                            //materialtemp.MaterialID = model.mid[j];
                            //materialtemp.MaterialName = materiallist[j];
                            //materialtemp.Quantity = model.m_quantity[j];
                            //materialtemp.Inward_Type = model.invtype[j];
                            //materialtemp.Material_Remark = model.matremark[j];
                            //context.SaveChanges();

                            var inwardMaterial_Temp_List = context.InwardMaterial_Temp.Where(x => x.InwardID == model.InwardID).ToList();
                            if(inwardMaterial_Temp_List.Count > 0)
                            {
                                foreach (var materialtemp in inwardMaterial_Temp_List)
                                {

                                    if (materiallist.Contains(materialtemp.MaterialName)){

										materialtemp.InwardID = model.InwardID;
										materialtemp.Material_CategoryID = int.Parse(materialcatlist[j].ToString());
										materialtemp.MaterialID = context.Materials.Where(x => x.MaterialName == materialtemp.MaterialName).Select(x => x.MaterialID).FirstOrDefault();
										materialtemp.MaterialName = materiallist.Where(x => x == materialtemp.MaterialName).Select(x => x).FirstOrDefault();
										materialtemp.Quantity = model.m_quantity[j];
										materialtemp.Inward_Type = model.invtype[j];
										materialtemp.Material_Remark = model.matremark[j];
										materialtemp.Status = "Deleted";
										context.SaveChanges();
									}
                                    else
                                    {										
                                        materialtemp.Status = "Deleted";
										context.SaveChanges();
									}
                                        
                                        
                                }
                            }
                        }


                    }
                }
                //bool file_Value = generatedInwardMaterial(model, finaldata);
                bool file_value_extra = false;
                string DCid = string.Empty;
                for (int i = 0; i < DC_Filename.Length; i++)
                {
                    DCid = unitOfWork.DepartmentRepository.GenerateInwardIDS("Deliverychallen");
                    file_value_extra = unitOfWork.DepartmentRepository.filename(DC_Filename[0], "Delivery Challen", DCid, model.InwardID);

                    if (i == 0)
                    {
                        DC_Number = model.DC_Number;
                    }
                    else if (i == 1)
                    {
                        DC_Number = model.DC_Number1;
                    }
                    else if (i == 2)
                    {
                        DC_Number = model.DC_Number2;
                    }
                    else if (i == 3)
                    {
                        DC_Number = model.DC_Number3;
                    }
                    if (file_value_extra == true)
                    {
                        if (DC_Number != null)
                        {
                            exceptionLogger.LogCreationForInwardEdit("Edit_InwardMaterial", $"Created new Delivery Challen for Inward ID: {model.InwardID}, DC ID: {DCid}");
                            var DeliveryChallen = new DeliveryChallen()
                            {
                                DC_ID = DCid,
                                DC_Number = DC_Number,
                                DC_Filename = DC_Filename[0].FileName,
                                DC_Filesize = DC_Filename[0].ContentLength.ToString(),
                                InwardID = model.InwardID

                            };
                            unitOfWork.DepartmentRepository.Insert(DeliveryChallen);
                            unitOfWork.Save();
                        }

                        // exceptionLogger.LogCreationForInwardEdit(model, "Edit");

                    }
                }
                exceptionLogger.LogCreationForInward(model.InwardID, "edited", model, original1);
                //var pdfii = UpdateInwardPDF(model.InwardID);

            }
            catch (Exception e)
            {

                var exceptionLogger = new IMSExceptionLogger()
                {
                    controllerName = "Add_InwardMaterialController",
                    actionName = "Edit_InwardMaterial(post)",
                    exceptionStackTrace = e.StackTrace,
                    exceptionMessage = e.Message,
                    exceptionLogTime = DateTime.Now

                };
            }
            return RedirectToAction("InwardList", "Add_InwardMaterial");
        }

        public bool sendInwardMail(string id, string type)
        {
            try
            {
                var inwarddetails = (from i in context.InwardMaterials
                                     join v in context.HTV_Vendor on i.vendorId equals v.vendorId
                                     where i.InwardID == id
                                     select new { i.Location, i.userDepartmentId, v.vendorEmail, v.vendorName, i.InwardDateTime, i.InwardEnteredBy, i.InwardID }).FirstOrDefault();

                var Approverdetails = (from a in context.InwardApprovers
                                       where a.Location == inwarddetails.Location && a.userDepartmentId == inwarddetails.userDepartmentId && a.RoleInIMS == "Receiver"
                                       select new { a.userEmail });

                var materialdetails = (from i in context.InwardMaterial_Temp
                                       where i.InwardID == id
                                       select new { i.MaterialName, i.Quantity, i.Material_Remark, i.Inward_Type });

                var deliverchallen = (from a in context.DeliveryChallens
                                      where a.InwardID == id
                                      select new { a.DC_ID, a.DC_Filename });
                string Body = string.Empty;
                string sub = string.Empty;
                MailMessage mail = new MailMessage();
                string username = ConfigurationManager.AppSettings["SmtpUserId"].ToString();
                var materialhtml = string.Empty;
                var count = 1;
                foreach (var m in materialdetails)
                {
                    materialhtml = materialhtml + "<tr><td>" + count + "</td><td>" + m.MaterialName + "</td><td>" + m.Quantity + "</td><td>" + m.Inward_Type + "</td></tr>";
                    count++;
                }
                foreach (var m in Approverdetails)
                {
                    mail.To.Add(m.userEmail.ToString());
                }

                //mail.To.Add(inwarddetails.vendorEmail.ToString());

                foreach (var f in deliverchallen)
                {
                    DirectoryInfo dir = new DirectoryInfo(@"D:\IMSDocs\DeliveryChallen\" + id + "\\" + f.DC_ID);
                    foreach (FileInfo file2 in dir.GetFiles("*.*"))
                    {
                        if (file2.Exists)
                        {
                            mail.Attachments.Add(new Attachment(file2.FullName));
                        }
                    }
                }


                if (type == "Inward Added")
                {
                    sub = "Inward Entry Approval Required";
                    Body = "Hi,<br/> This is to inform you that an inward entry has been added by the Security Desk, attached are the details of the inward material. " +
                           "Please provide the approval after your review of the same.<br/>"+
                           "<table border = 1> <tr><th>srno.</th><th>Material Name</th><th>Qty</th><th> Inward Type </th>";
                    var note = materialhtml + "</table>";
                    Body = Body + note;
                    //var regards = "<br/><br/>Regards,<br/>" + "Security(" + inwarddetails.InwardEnteredBy.ToString() + ") - " + inwarddetails.Location.ToString();
                    var regards = "<br/><br/>Best Regards <br/> Team Harbinger IMS";
                    Body = Body + regards;
                    var remark = "<br/><font size=\"2\" color=\"red\"> <i> Note : This is a system generated email. Please do not reply to this email.</i></font>";
                    Body = Body + remark;

                }
                else if (type == "Inward Rejected")
                {
                    sub = "Inward Material Rejected on IMS";
                    Body = "I hope this email finds you well. This is to inform you that the following materials have been rejected: Please find the details below:" +
                           "<br/><br/>" + "Vendor Name: " + inwarddetails.vendorName.ToString()
                           + "<br/>" + "Inward Date:" + inwarddetails.InwardDateTime.ToString()
                           + "<br/><br/><table border = 1> <tr><th>srno.</th><th>Material Name</th><th>Qty</th><th> Inward Type </th>" + materialhtml
                           + "<br/><br/>Best Regards <br/> Team Harbinger IMS";

                    var end_body = "<br/><font size=\"2\" color=\"red\"> <i> Note : This is a system generated email. Please do not reply to this email.</i></font>";
                    Body = Body + end_body;
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
                    controllerName = "Add_InwardMaterialController",
                    actionName = "sendInwardMail",
                    exceptionStackTrace = e.StackTrace,
                    exceptionMessage = e.Message,
                    exceptionLogTime = DateTime.Now
                };
                return false;
            }
        }

        public JsonResult Getvendor()
        {
            var Vendor_List = (from t in context.HTV_Vendor
                               where t.CSTNo != "Reject" && (t.vendorStatus != "Blacklisted") && (t.vendorStatus != "Inactive") && !t.vendorCode.Contains("DUM")
                               orderby t.vendorName ascending
                               select new { t.vendorId, t.vendorName, t.userDepartmentId }).ToList();
            return Json(Vendor_List, JsonRequestBehavior.AllowGet);
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

        public JsonResult Getmaterialcategory()
        {
            var materiallist = new List<IMSEntity>();
            var materialcat = new Material_Category();
            materiallist = unitOfWork.DepartmentRepository.GetMaterialCategory(materialcat, 0);
            var result = from a in materiallist select new Material_Category { Material_CategoryID = a.Material_CategoryID, Material_CategoryName = a.Material_CategoryName };
            var jsonResult = JsonConvert.SerializeObject(result);
            return Json(jsonResult, JsonRequestBehavior.AllowGet);
        }

        public string GetFileType(string filename)
        {

            int count = 0;
            string contentType = MimeMapping.GetMimeMapping(filename);
            int contentLenght = filename.Length;
            if (contentType == "application/pdf" || contentType == "text/plain" || contentType == "application/msword"
                || contentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" || contentType == "image/jpeg"
                 || contentType == "image/png" || contentType == "application/zip" || contentType == "application/x-zip-compressed")
            {
                count = 1;
            }
            else
            {

                count = 0;
            }

            var sprintJson = "{\"count\":\"" + count + "\",\"contentlength\":" + contentLenght + "}";
            return sprintJson;
        }


        //public ActionResult InwardList(string sortOrder, string searchString, string Name, string SearchValue, FormCollection form, DateTime? searchdate, int? Page_no)
        //{
        //    string userrole = null;
        //    string location = null;
        //    HttpCookie httpCookie_role = Request.Cookies["role"];

        //    if (httpCookie_role != null)
        //    {
        //        userrole = unitOfWork.DepartmentRepository.Decode(httpCookie_role.Value);
        //    }

        //    int countForsearch = 0;
        //    string display = "0";
        //    ViewBag.display = display;
        //    int count = 0;
        //    string empid = null;
        //    string Email = null;
        //    DateTime val = DateTime.Now;
        //    int pagesize = 2;
        //    int pagenumber = (Page_no ?? 1);
        //    ViewBag.page = Page_no;
        //    if (searchdate.HasValue)
        //    {
        //        val = searchdate ?? DateTime.Now;

        //    }

        //    if (Session["Location"] != null)
        //    {
        //        location = Session["Location"].ToString();
        //        ViewBag.location = Session["Location"].ToString();
        //    }

        //    ViewBag.Inward_id = String.IsNullOrEmpty(sortOrder) ? "Inward_id_desc" : "";
        //    ViewBag.Inward_department = sortOrder == "Inward_department" ? "Inward_department_desc" : "Inward_department";
        //    ViewBag.Inward_Location = sortOrder == "Inward_Location" ? "Inward_Location_desc" : "Inward_Location";
        //    ViewBag.Inward_nature = sortOrder == "Inward_nature" ? "Inward_nature_desc" : "Inward_nature";
        //    ViewBag.GRNNumber = sortOrder == "GRNNumber" ? "GRNNumber_desc" : "GRNNumber";
        //    ViewBag.Inward_datetime = sortOrder == "Inward_datetime" ? "Inward_datetime_desc" : "Inward_datetime";
        //    ViewBag.Inward_raised_by = sortOrder == "Inward_raised_by" ? "Inward_raised_by_desc" : "Inward_raised_by";
        //    ViewBag.vendorId = sortOrder == "vendorId" ? "vendorId_desc" : "vendorId";


        //    HttpCookie httpCookie1 = Request.Cookies["employeeId"];
        //    if (httpCookie1 != null)
        //    {
        //        empid = unitOfWork.DepartmentRepository.Decode(httpCookie1.Value);
        //        ViewBag.empid = empid;
        //    }

        //    string role = null;
        //    string dept = null;
        //    HttpCookie httpCookie3 = Request.Cookies["role"];

        //    if (httpCookie3 != null)
        //    {
        //        role = unitOfWork.DepartmentRepository.Decode(httpCookie3.Value);
        //    }
        //    HttpCookie httpCookie4 = Request.Cookies["department"];
        //    if (httpCookie4 != null)
        //    {
        //        dept = unitOfWork.DepartmentRepository.Decode(httpCookie4.Value);
        //    }
        //    ViewBag.role = role;
        //    var imsentity = new List<IMSEntity>();


        //    //ViewBag.empid = empid;
        //    try
        //    {
        //        ServiceVMSEntities context = new ServiceVMSEntities();
        //        var serviceuser = new ServiceUserDepartment();
        //        var name_department = unitOfWork.DepartmentRepository.Get(serviceuser, int.Parse(dept));
        //        string deptName = name_department[0].userDepartmentName;


        //        IQueryable<InwardMaterial> InwardDetails = null;
        //        IQueryable<InwardMaterial> Finallist = null;
        //        IQueryable<InwardMaterial> InwardList = null;

        //        InwardDetails = (from t in context.InwardMaterials where t.Inward_Status != "Deleted" select t);
        //        if (empid != null)
        //        {
        //            if (empid.Contains("DUM"))
        //            {
        //                if (role == "Inventory Manager")
        //                {
        //                    InwardDetails = InwardDetails.Where(u => u.InwardID.Contains("DUM"));

        //                }
        //                else
        //                {
        //                    InwardDetails = InwardDetails.Where(u => u.InwardID.Contains("DUM") && u.userDepartmentId == int.Parse(dept));
        //                }


        //            }
        //            else
        //            {
        //                var cc = InwardDetails.Count();
        //                int deptid = int.Parse(dept);
        //                if (role == "Inventory Manager")
        //                {
        //                    InwardDetails = InwardDetails.Where(u => !u.InwardID.Contains("DUM"));

        //                }
        //                else if (role == "Security Guard")
        //                {
        //                    InwardDetails = InwardDetails.Where(u => !u.InwardID.Contains("DUM") && u.Location == location);
        //                }
        //                else if (role == "Inventory Incharge" || role == "Receiver")
        //                {
        //                    InwardDetails = InwardDetails.Where(u => !u.InwardID.Contains("DUM") && u.Location == location && u.userDepartmentId == deptid);
        //                }
        //                else
        //                {

        //                    InwardDetails = InwardDetails.Where(u => !u.InwardID.Contains("DUM") && u.userDepartmentId == deptid);
        //                }
        //            }
        //        }
        //        var c = InwardDetails.Count();
        //        if (!String.IsNullOrEmpty(SearchValue))
        //        {
        //            String Searchby = form["Name"].ToString();

        //            if (Searchby == "Inward_id")
        //            {
        //                countForsearch = 1;
        //                InwardDetails = InwardDetails.Where(u => u.InwardID.Contains(SearchValue));
        //            }
        //            else if (Searchby == "Inward_department")
        //            {
        //                countForsearch = 1;
        //                InwardDetails = InwardDetails.Where(u => u.userDepartmentId == int.Parse(SearchValue));
        //            }
        //            else if (Searchby == "Inward_Location")
        //            {
        //                countForsearch = 1;
        //                InwardDetails = InwardDetails.Where(u => u.Location.Contains(SearchValue));
        //            }
        //            else if (Searchby == "Inward_nature")
        //            {
        //                countForsearch = 1;
        //                InwardDetails = InwardDetails.Where(u => u.Inward_ExpenseNature.Contains(SearchValue));
        //            }
        //            else if (Searchby == "Inward_datetime" && (searchdate != null))
        //            {
        //                countForsearch = 1;
        //                val = val.Date;
        //                InwardDetails = InwardDetails.Where(u => u.InwardDateTime == (val));
        //            }
        //            else if (Searchby == "GRNNumber")
        //            {
        //                countForsearch = 1;
        //                InwardDetails = InwardDetails.Where(u => u.GRN_Number.Contains(SearchValue));
        //            }
        //            else if (Searchby == "Inward_raised_by")
        //            {
        //                countForsearch = 1;
        //                InwardDetails = InwardDetails.Where(u => u.InwardEnteredBy.Contains(SearchValue));
        //            }
        //            else if (Searchby == "vendorId")
        //            {
        //                countForsearch = 1;
        //                InwardDetails = InwardDetails.Where(u => u.vendorId.Contains(SearchValue));
        //            }

        //        }

        //        switch (sortOrder)
        //        {
        //            case "Inward_id":
        //                InwardDetails = InwardDetails.OrderByDescending(p => p.InwardID);
        //                break;

        //            case "Inward_department":
        //                InwardDetails = InwardDetails.OrderByDescending(p => p.ServiceUserDepartment.userDepartmentName);
        //                break;

        //            case "Inward_department_desc":
        //                InwardDetails = InwardDetails.OrderBy(p => p.ServiceUserDepartment.userDepartmentName);
        //                break;

        //            case "Inward_Location":
        //                InwardDetails = InwardDetails.OrderByDescending(u => u.Location);
        //                break;

        //            case "Inward_Location_desc":
        //                InwardDetails = InwardDetails.OrderBy(u => u.Location);
        //                break;

        //            case "Inward_nature":
        //                InwardDetails = InwardDetails.OrderByDescending(u => u.Inward_ExpenseNature);
        //                break;

        //            case "Inward_nature_desc":
        //                InwardDetails = InwardDetails.OrderBy(u => u.Inward_ExpenseNature);
        //                break;

        //            case "GRNNumber":
        //                InwardDetails = InwardDetails.OrderByDescending(u => u.GRN_Number);
        //                break;

        //            case "GRNNumber_desc":
        //                InwardDetails = InwardDetails.OrderBy(u => u.GRN_Number);
        //                break;

        //            case "vendorId":
        //                InwardDetails = InwardDetails.OrderByDescending(u => u.vendorId);
        //                break;

        //            case "vendorId_desc":
        //                InwardDetails = InwardDetails.OrderBy(u => u.vendorId);
        //                break;

        //            case "Inward_datetime":
        //                InwardDetails = InwardDetails.OrderByDescending(u => (u.InwardDateTime));
        //                break;

        //            case "Inward_datetime_desc":
        //                InwardDetails = InwardDetails.OrderBy(u => (u.InwardDateTime));
        //                break;

        //            case "Inward_raised_by":
        //                InwardDetails = InwardDetails.OrderByDescending(u => u.InwardEnteredBy);
        //                break;

        //            case "Inward_raised_by_desc":
        //                InwardDetails = InwardDetails.OrderBy(u => u.InwardEnteredBy);
        //                break;

        //            default:
        //                InwardDetails = InwardDetails.OrderByDescending(p => p.InwardID);
        //                break;

        //        }

        //        foreach (var t in InwardDetails)
        //        {

        //            imsentity.Add(new IMSEntity()
        //            {
        //                InwardID = t.InwardID,
        //                userDepartmentId = t.userDepartmentId,
        //                vendorId = t.vendorId,
        //                InwardEnteredBy = t.InwardEnteredBy,
        //                BillofEntry = t.BillofEntry,
        //                Location = t.Location,
        //                InwardDateTime = t.InwardDateTime,
        //                GRN_Number = t.GRN_Number,
        //                Inward_ExpenseNature = t.Inward_ExpenseNature,
        //                Inward_Status = t.Inward_Status
        //            });
        //        }

        //        ViewBag.count = count;
        //        ViewBag.countForsearch = countForsearch;
        //        return View(imsentity.ToPagedList(pagenumber, pagesize));
        //    }
        //    catch (Exception e)
        //    {

        //        Console.Write(e);

        //        var exceptionLogger = new ExceptionLogger()
        //        {
        //            controllerName = "Add_InwardMaterialController",
        //            actionName = "InwardList",
        //            exceptionStackTrace = e.StackTrace,
        //            exceptionMessage = e.Message,
        //            exceptionLogTime = DateTime.Now

        //        };
        //        unitOfWork.DepartmentRepository.Insert(exceptionLogger);
        //        unitOfWork.Save();
        //    }


        //    return View(imsentity);
        //}

        public ActionResult InwardList(string sortOrder, string searchString, string Name, string SearchValue1, string SearchValue2, string SearchValue, string SearchValue_ExpenseN, FormCollection form, DateTime? searchdate, int? Page_no)
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
                string Email = null;
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

                ViewBag.Inward_id = String.IsNullOrEmpty(sortOrder) ? "Inward_id_desc" : "";
                ViewBag.Inward_department = sortOrder == "Inward_department" ? "Inward_department_desc" : "Inward_department";
                ViewBag.Inward_Location = sortOrder == "Inward_Location" ? "Inward_Location_desc" : "Inward_Location";
                ViewBag.Inward_nature = sortOrder == "Inward_nature" ? "Inward_nature_desc" : "Inward_nature";
                ViewBag.GRNNumber = sortOrder == "GRNNumber" ? "GRNNumber_desc" : "GRNNumber";
                ViewBag.Inward_datetime = sortOrder == "Inward_datetime" ? "Inward_datetime_desc" : "Inward_datetime";
                ViewBag.Inward_raised_by = sortOrder == "Inward_raised_by" ? "Inward_raised_by_desc" : "Inward_raised_by";
                ViewBag.vendorId = sortOrder == "vendorId" ? "vendorId_desc" : "vendorId";


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


                    IQueryable<InwardMaterial> InwardDetails = null;
                    IQueryable<InwardMaterial> Finallist = null;
                    IQueryable<InwardMaterial> InwardList = null;


                    InwardDetails = (from t in context.InwardMaterials
                                         // join v in context.HTV_Vendor on t.vendorId equals v.vendorId
                                     where t.Inward_Status != "Deleted"
                                     select t);
                    if (empid != null)
                    {
                        if (empid.Contains("DUM"))
                        {
                            if (role == "Inventory Manager")
                            {
                                InwardDetails = InwardDetails.Where(u => u.InwardID.Contains("DUM"));

                            }
                            else
                            {
                                InwardDetails = InwardDetails.Where(u => u.InwardID.Contains("DUM") && u.userDepartmentId == int.Parse(dept));
                            }


                        }
                        else
                        {
                            var cc = InwardDetails.Count();
                            int deptid = int.Parse(dept);
                            if (role == "Administrator")
                            {
                                InwardDetails = InwardDetails.Where(u => !u.InwardID.Contains("DUM"));
                            }
                            else if (role == "Inventory Manager")
                            {
                                InwardDetails = InwardDetails.Where(u => !u.InwardID.Contains("DUM") && u.userDepartmentId == deptid);
                            }
                            else if (role == "Security Guard")
                            {
                                InwardDetails = InwardDetails.Where(u => !u.InwardID.Contains("DUM") && u.Location == location);
                            }
                            else if (role == "Inventory Incharge" || role == "Receiver")
                            {
                                InwardDetails = InwardDetails.Where(u => !u.InwardID.Contains("DUM") && u.Location == location && u.userDepartmentId == deptid);
                            }
                            else
                            {

                                InwardDetails = InwardDetails.Where(u => !u.InwardID.Contains("DUM") && u.userDepartmentId == deptid);
                            }
                        }
                    }
                    var c111 = InwardDetails.Count();

                    if (!string.IsNullOrEmpty(Name) && Name == "actionable")
                    {
                        if (role == "Receiver")
                        {
                            InwardDetails = InwardDetails.Where(u => u.Inward_Status == "Inward Added");
                        }
                        else if (role == "Inventory Incharge")
                        {
                            InwardDetails = InwardDetails.Where(u => u.Inward_Status == "Inward Pending");
                        }
                    }

                    var c11 = InwardDetails.Count();
                    if (!InwardDetails.Any())
                    {
                        ViewBag.NoItemsFound = "No items found.";
                    }

                    var c = InwardDetails.Count();
                    var locations = InwardDetails.Select(s => s.vendorId).ToList();
                    if (!String.IsNullOrEmpty(SearchValue) || !String.IsNullOrEmpty(SearchValue_ExpenseN) || !String.IsNullOrEmpty(SearchValue1) || searchdate != null)
                    {
                        String Searchby = form["Name"].ToString();

                        if (Searchby == "Inward_id")
                        {
                            countForsearch = 1;
                            InwardDetails = InwardDetails.Where(u => u.InwardID.Contains(SearchValue));
                        }
                        else if (Searchby == "Inward_department")
                        {
                            countForsearch = 1;
                            int DepartmentName;
                            var InwardValue = (from i in context.ServiceUserDepartments
                                               where i.userDepartmentName == SearchValue1
                                               select new { i.userDepartmentId }).ToList();
                            foreach (var t in InwardValue)
                            {
                                DepartmentName = t.userDepartmentId;


                                InwardDetails = InwardDetails.Where(u => u.userDepartmentId == DepartmentName);
                            }
                        }
                        else if (Searchby == "Inward_Location")
                        {
                            countForsearch = 1;
                            InwardDetails = InwardDetails.Where(u => u.Location.Contains(SearchValue));
                        }
                        else if (Searchby == "Inward_nature")
                        {
                            countForsearch = 1;
                            InwardDetails = InwardDetails.Where(u => u.Inward_ExpenseNature.Contains(SearchValue_ExpenseN));
                        }
                        else if (Searchby == "Inward_datetime" && (searchdate != null))
                        {
                            countForsearch = 1;
                            val = val.Date;
                            InwardDetails = InwardDetails.Where(u => u.InwardDateTime == (val));
                        }
                        else if (Searchby == "GRNNumber")
                        {
                            countForsearch = 1;
                            InwardDetails = InwardDetails.Where(u => u.GRN_Number.Contains(SearchValue));
                        }
                        else if (Searchby == "Inward_raised_by")
                        {
                            countForsearch = 1;
                            InwardDetails = InwardDetails.Where(u => u.InwardEnteredBy.Contains(SearchValue));
                        }
                        else if (Searchby == "vendorId")
                        {
                            countForsearch = 1;
                            InwardDetails = InwardDetails.Where(u => u.vendorId.Contains(SearchValue));
                        }

                    }

                    switch (sortOrder)
                    {
                        case "Inward_id":
                            InwardDetails = InwardDetails.OrderByDescending(p => p.InwardID);
                            break;

                        case "Inward_department":
                            InwardDetails = InwardDetails.OrderByDescending(p => p.ServiceUserDepartment.userDepartmentName);
                            break;

                        case "Inward_department_desc":
                            InwardDetails = InwardDetails.OrderBy(p => p.ServiceUserDepartment.userDepartmentName);
                            break;

                        case "Inward_Location":
                            InwardDetails = InwardDetails.OrderByDescending(u => u.Location);
                            break;

                        case "Inward_Location_desc":
                            InwardDetails = InwardDetails.OrderBy(u => u.Location);
                            break;

                        case "Inward_nature":
                            InwardDetails = InwardDetails.OrderByDescending(u => u.Inward_ExpenseNature);
                            break;

                        case "Inward_nature_desc":
                            InwardDetails = InwardDetails.OrderBy(u => u.Inward_ExpenseNature);
                            break;

                        case "GRNNumber":
                            InwardDetails = InwardDetails.OrderByDescending(u => u.GRN_Number);
                            break;

                        case "GRNNumber_desc":
                            InwardDetails = InwardDetails.OrderBy(u => u.GRN_Number);
                            break;

                        case "vendorId":
                            InwardDetails = InwardDetails.OrderByDescending(u => u.vendorId);
                            break;

                        case "vendorId_desc":
                            InwardDetails = InwardDetails.OrderBy(u => u.vendorId);
                            break;

                        case "Inward_datetime":
                            InwardDetails = InwardDetails.OrderByDescending(u => (u.InwardDateTime));
                            break;

                        case "Inward_datetime_desc":
                            InwardDetails = InwardDetails.OrderBy(u => (u.InwardDateTime));
                            break;

                        case "Inward_raised_by":
                            InwardDetails = InwardDetails.OrderByDescending(u => u.InwardEnteredBy);
                            break;

                        case "Inward_raised_by_desc":
                            InwardDetails = InwardDetails.OrderBy(u => u.InwardEnteredBy);
                            break;

                        default:
                            InwardDetails = InwardDetails.OrderByDescending(p => p.InwardDateTime);
                            break;

                    }

                    foreach (var t in InwardDetails)
                    {

                        imsentity.Add(new IMSEntity()
                        {
                            InwardID = t.InwardID,
                            userDepartmentId = t.userDepartmentId,
                            vendorId = t.vendorId,
                            InwardEnteredBy = t.InwardEnteredBy,
                            BillofEntry = t.BillofEntry,
                            Location = t.Location,
                            InwardDateTime = t.InwardDateTime,
                            GRN_Number = t.GRN_Number,
                            Inward_ExpenseNature = t.Inward_ExpenseNature,
                            Inward_Status = t.Inward_Status,
                            vendorName = string.IsNullOrEmpty(t.vendorId) ? t.TempVendorID : t.vendorId,
                            TempVendorID = t.TempVendorID

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
        public ActionResult InwardDetails(string inwardID)
        {
            inwardID = unitOfWork.DepartmentRepository.Decode(inwardID);
            string role = string.Empty;
            string userid = string.Empty;
            string dept = null;
            string location = null;
            List<IMSEntity> inwardlistmodel = new List<IMSEntity>();

            if (Session["role"] != null)
            {
                role = Session["role"].ToString();
            }

            if (Session["UserID"] != null)
            {
                userid = Session["UserID"].ToString();
            }

            if (Session["DepartmentID"] != null)
            {
                dept = Session["DepartmentID"].ToString();

            }

            ViewBag.dept = dept;
            ViewBag.userid = userid;
            ViewBag.inwardID = inwardID;
            ViewBag.role = role;
            if (Session["Location"] == null)
            {
                var user = (from i in context.IMSRoles
                            where i.IMSuser_Id == userid && i.RoleInIMS == role
                            select new { i.userLocation }).FirstOrDefault();
                ViewBag.location = user.userLocation.ToString();
            }
            else
            {
                ViewBag.location = Session["Location"].ToString();
            }
            try
            {
                var invdetails = (from d in context.InwardMaterials
                                  join v in context.HTV_Vendor on d.vendorId equals v.vendorId into vendorGroup
                                  from v in vendorGroup.DefaultIfEmpty()
                                  join t in context.TempVendors on d.TempVendorID equals t.TempVendorID into tempVendorGroup
                                  from t in tempVendorGroup.DefaultIfEmpty()
                                  join u in context.ServiceUserDepartments on d.userDepartmentId equals u.userDepartmentId into departmentGroup
                                  from u in departmentGroup.DefaultIfEmpty()
                                  where d.InwardID == inwardID && !d.Inward_Status.Equals("Deleted")
                                  select new
                                  {
                                      d.InwardID,
                                      VendorId = v != null ? v.vendorId : null,
                                      VendorName = v != null ? v.vendorName : null,
                                      d.GRN_Number,
                                      TempVendorId = t != null ? t.TempVendorID : null,
                                      TempVendorName = t != null ? t.TempVendorName : null,
                                      d.InwardDateTime,
                                      d.InwardEnteredBy,
                                      d.InwardNote,
                                      d.Inward_ExpenseNature,
                                      d.Inward_Status,
                                      d.userDepartmentId,
                                      UserDepartmentName = u != null ? u.userDepartmentName : null,
                                      d.Location
                                  }).FirstOrDefault();
                if (invdetails != null)
                {
                    string vendorName = invdetails.VendorName != null ? invdetails.VendorName : invdetails.TempVendorName;

                    if (invdetails.Inward_ExpenseNature != null)
                    {
                        ViewBag.expensenature = invdetails.Inward_ExpenseNature;
                    }
                    if (invdetails.GRN_Number != null)
                    {
                        ViewBag.GRNnumber = invdetails.GRN_Number;
                    }

                    inwardlistmodel.Add(new IMSEntity()
                    {
                        InwardID = invdetails.InwardID,
                        vendorId = invdetails.VendorId,
                        GRN_Number = invdetails.GRN_Number,
                        InwardDateTime = invdetails.InwardDateTime,
                        userDepartmentName = invdetails.UserDepartmentName,
                        InwardNote = invdetails.InwardNote,
                        Inward_ExpenseNature = invdetails.Inward_ExpenseNature,
                        Inward_Status = invdetails.Inward_Status,
                        InwardEnteredBy = invdetails.InwardEnteredBy,
                        Location = invdetails.Location,
                        vendorName = vendorName,
                        TempVendorID = invdetails.TempVendorId
                    });
                }
            }
            catch (Exception e)
            {
                Console.Write(e);
                var exceptionLogger = new IMSExceptionLogger()
                {
                    controllerName = "Add_InwardMaterialController",
                    actionName = "InwardDetails",
                    exceptionStackTrace = e.StackTrace,
                    exceptionMessage = e.Message,
                    exceptionLogTime = DateTime.Now
                };
                unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                unitOfWork.Save();
            }
            return View(inwardlistmodel);
        }

        [HttpGet]
        public ActionResult Delete_InwardMaterial(string id)
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
                    InwardMaterial inward_delete = context.InwardMaterials.Single(x => x.InwardID == id);
                    var deliveryChallen = context.DeliveryChallens.Where(x => x.InwardID == id);
                    unitOfWork.DepartmentRepository.Delete(inward_delete);
                    exceptionLogger.LogCreationForInward(id, "Deleted", null, null);
                    var dccount = deliveryChallen.Count();
                    unitOfWork.DepartmentRepository.DeletedFiles("InwardMaterial", null, id);
                    foreach (var d in deliveryChallen)
                    {
                        unitOfWork.DepartmentRepository.DeletedFiles("DeliveryChallen", d.DC_ID, id);
                    }
                }
                catch (Exception e)
                {
                    Console.Write(e); var exceptionLogger = new IMSExceptionLogger()
                    {
                        controllerName = "Add_InwardMaterialController",
                        actionName = "Delete_InwardMaterial",
                        exceptionStackTrace = e.StackTrace,
                        exceptionMessage = e.Message,
                        exceptionLogTime = DateTime.Now
                    };
                    unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                    unitOfWork.Save();

                }
            }

            var encodedServiceCode = unitOfWork.DepartmentRepository.Encode(id);
            return RedirectToAction("InwardList", "Add_Inwardmaterial", new { id = encodedServiceCode });
        }


        public ActionResult UpdateInwardStatus(string InwardId, string expensenature, string status, string[] materiallist, string materialcount, string inwardnote, List<IMSEntity> pricelist)
        {
            if (InwardId != null)
            {
                string userid = null;

                if (Session["UserID"] != null)
                {
                    userid = Session["UserID"].ToString();
                }
                var materialupdate = (from t in context.InwardMaterial_Temp
                                      where t.InwardID == InwardId
                                      select t).ToList();
                var m_count = materialupdate.Count().ToString();



                if (expensenature == "opex")
                {
                    if (m_count == materialcount)
                    {
                        foreach (var item in materialupdate)
                        {
                            InwardMaterial_OPEX material_OPEX = new InwardMaterial_OPEX();
                            material_OPEX.InvMaterialOP_ID = unitOfWork.DepartmentRepository.GenerateInwardIDS("Inwardopex");
                            material_OPEX.InwardID = item.InwardID;
                            material_OPEX.Material_CategoryID = item.Material_CategoryID;
                            material_OPEX.MaterialID = item.MaterialID;
                            material_OPEX.Material_Name = item.MaterialName;
                            material_OPEX.Quantity = item.Quantity;
                            material_OPEX.Inward_Type = item.Inward_Type;
                            material_OPEX.Material_Remark = item.Material_Remark;
                            material_OPEX.Accepted_Date = DateTime.Now;
                            if (status == "InwardAccepted")
                            {
                                material_OPEX.Status = "Material Accepted";
                            }
                            else
                            {
                                material_OPEX.Status = "Material Rejected";
                            }
                            material_OPEX.Acceptedby = userid;
                            context.InwardMaterial_OPEX.Add(material_OPEX);
                            context.SaveChanges();
                        }
                        var GRN_Number = string.Empty;
                        var InwardUpdate = (from t in context.InwardMaterials
                                            where t.InwardID == InwardId && !t.Inward_Status.Equals("Deleted")
                                            select t).SingleOrDefault();
                        if (status == "InwardAccepted")
                        {
                            InwardUpdate.Inward_Status = "Inward Accepted";
                            GRN_Number = generateGRNNumber(InwardId);
                            InwardUpdate.GRN_Number = GRN_Number;
                        }
                        else
                        {
                            InwardUpdate.Inward_Status = "Inward Rejected";
                        }
                        InwardUpdate.Inward_ExpenseNature = expensenature;
                        InwardUpdate.InwardNote = inwardnote;
                        context.SaveChanges();
                        var vendorId = !string.IsNullOrEmpty(InwardUpdate.vendorId) ? InwardUpdate.vendorId : InwardUpdate.TempVendorID;

                        if (status == "InwardAccepted")
                        {
                            bool mailsend = sendGRNNumber(GRN_Number, InwardId, "Material Accepted", vendorId, "");
                            exceptionLogger.LogCreationForInwardStatus(InwardId, "change status opex", null);
                            exceptionLogger.LogCreationForInwardStatus(InwardId, "Material Accepted OPEX", "");
                        }
                        else
                        {
                            bool mail = sendInwardMail(InwardId, "Inward Rejected");
                            exceptionLogger.LogCreationForInwardStatus(InwardId, "change status opex", null);
                            exceptionLogger.LogCreationForInwardStatus(InwardId, "Material Rejected OPEX", "");

                        }
                        var pdf = UpdateInwardPDF(InwardId);

                    }
                    else
                    {
                        var baselist = (from m in context.InwardMaterial_Temp
                                        where m.InwardID == InwardId
                                        select m.MaterialID.ToString()).ToArray();
                        var filteredlist = baselist.Except(materiallist).ToList();

                        foreach (var item in filteredlist)
                        {
                            var statusopex1 = string.Empty;
                            var materialinfo = (from m in context.InwardMaterial_Temp
                                                where m.InwardID == InwardId && m.MaterialID == item
                                                select m).FirstOrDefault();
                            InwardMaterial_OPEX material_OPEX = new InwardMaterial_OPEX();
                            material_OPEX.InvMaterialOP_ID = unitOfWork.DepartmentRepository.GenerateInwardIDS("Inwardopex");
                            material_OPEX.InwardID = InwardId;
                            material_OPEX.Material_CategoryID = materialinfo.Material_CategoryID;
                            material_OPEX.MaterialID = materialinfo.MaterialID;
                            material_OPEX.Material_Name = materialinfo.MaterialName;
                            material_OPEX.Quantity = materialinfo.Quantity;
                            material_OPEX.Inward_Type = materialinfo.Inward_Type;
                            material_OPEX.Material_Remark = materialinfo.Material_Remark;
                            material_OPEX.Accepted_Date = DateTime.Now;
                            if (status == "InwardAccepted")
                            {
                                statusopex1 = "Material Rejected OPEX";
                                material_OPEX.Status = "Material Rejected";
                            }
                            else
                            {
                                statusopex1 = "Material Accepted OPEX";
                                material_OPEX.Status = "Material Accepted";
                            }
                            material_OPEX.Acceptedby = userid;
                            context.InwardMaterial_OPEX.Add(material_OPEX);
                            context.SaveChanges();
                            exceptionLogger.LogCreationForInwardStatus(InwardId, statusopex1, materialinfo.MaterialID);
                        }
                        foreach (var item1 in materiallist)
                        {
                            var statusopex = string.Empty;
                            var materialunche = (from m in context.InwardMaterial_Temp
                                                 where m.InwardID == InwardId && m.MaterialID == item1
                                                 select m).SingleOrDefault();
                            InwardMaterial_OPEX material_OPEX = new InwardMaterial_OPEX();
                            material_OPEX.InvMaterialOP_ID = unitOfWork.DepartmentRepository.GenerateInwardIDS("Inwardopex");
                            material_OPEX.InwardID = InwardId;
                            material_OPEX.Material_CategoryID = materialunche.Material_CategoryID;
                            material_OPEX.MaterialID = materialunche.MaterialID;
                            material_OPEX.Material_Name = materialunche.MaterialName;
                            material_OPEX.Quantity = materialunche.Quantity;
                            material_OPEX.Inward_Type = materialunche.Inward_Type;
                            material_OPEX.Material_Remark = materialunche.Material_Remark;
                            material_OPEX.Accepted_Date = DateTime.Now;
                            if (status == "InwardAccepted")
                            {
                                statusopex = "Material Accepted OPEX";
                                material_OPEX.Status = "Material Accepted";
                            }
                            else
                            {
                                statusopex = "Material Rejected OPEX";
                                material_OPEX.Status = "Material Rejected";
                            }
                            material_OPEX.Acceptedby = userid;
                            context.InwardMaterial_OPEX.Add(material_OPEX);
                            context.SaveChanges();
                            exceptionLogger.LogCreationForInwardStatus(InwardId, statusopex, materialunche.MaterialID);
                        }

                        var GRN_Number = generateGRNNumber(InwardId);
                        string invvstatus = string.Empty;
                        var InwardUpdate = (from t in context.InwardMaterials
                                            where t.InwardID == InwardId && !t.Inward_Status.Equals("Deleted")
                                            select t).SingleOrDefault();
                        if (status == "InwardAccepted")
                        {
                            invvstatus = "Inward Accepted";
                        }
                        else
                        {
                            invvstatus = "Inward Rejected";
                        }
                        InwardUpdate.Inward_Status = invvstatus;
                        InwardUpdate.Inward_ExpenseNature = expensenature;
                        InwardUpdate.GRN_Number = GRN_Number;
                        InwardUpdate.InwardNote = inwardnote;
                        context.SaveChanges();
                        exceptionLogger.LogCreationForInwardStatus(InwardId, "change status opex", null);
                        var vendorId = !string.IsNullOrEmpty(InwardUpdate.vendorId) ? InwardUpdate.vendorId : InwardUpdate.TempVendorID;
                        bool mailsend = sendGRNNumber(GRN_Number, InwardId, "Material Accepted", vendorId, "");
                    }


                    var pdf2 = UpdateInwardPDF(InwardId);
                }
                else if (expensenature == "capex" && (status == "InwardAccepted" || status == "InwardRejected"))
                {
                    if (m_count == materialcount)
                    {
                        foreach (var item in materialupdate)
                        {
                            InwardMaterial_CAPEX material_capex = new InwardMaterial_CAPEX();
                            material_capex.InvMaterialCP_ID = unitOfWork.DepartmentRepository.GenerateInwardIDS("Inwardcapex");
                            material_capex.InwardID = item.InwardID;
                            material_capex.Material_CategoryID = item.Material_CategoryID;
                            material_capex.MaterialID = item.MaterialID;
                            material_capex.MaterialName = item.MaterialName;
                            material_capex.Quantity = item.Quantity;
                            material_capex.Inward_Type = item.Inward_Type;
                            material_capex.Material_Remark = item.Material_Remark;
                            material_capex.Accepted_Date = DateTime.Now;
                            if (status == "InwardAccepted")
                            {
                                material_capex.Status = "Material Accepted";
                            }
                            else
                            {
                                material_capex.Status = "Material Rejected";
                            }
                            material_capex.Acceptedby = userid;
                            context.InwardMaterial_CAPEX.Add(material_capex);
                            context.SaveChanges();
                        }

                        var InwardUpdate = (from t in context.InwardMaterials
                                            where t.InwardID == InwardId && !t.Inward_Status.Equals("Deleted")
                                            select t).SingleOrDefault();
                        var GRN_Number = string.Empty;
                        if (status == "InwardRejected")
                        {
                            InwardUpdate.Inward_Status = "Inward Rejected";
                        }
                        else
                        {
                            InwardUpdate.Inward_Status = "Inward Pending";
                            GRN_Number = generateGRNNumber(InwardId);
                            InwardUpdate.GRN_Number = GRN_Number.ToString();
                        }
                        InwardUpdate.Inward_ExpenseNature = expensenature;
                        InwardUpdate.InwardNote = inwardnote;
                        context.SaveChanges();
                        var vendorId = !string.IsNullOrEmpty(InwardUpdate.vendorId) ? InwardUpdate.vendorId : InwardUpdate.TempVendorID;

                        if (status == "InwardRejected")
                        {
                            var mailsend = sendInwardMail(InwardId, "Inward Rejected");
                            exceptionLogger.LogCreationForInwardStatus(InwardId, "change status capex", null);
                            exceptionLogger.LogCreationForInwardStatus(InwardId, "Material Rejected CAPEX", "");

                        }
                        else
                        {
                            bool mailsend = sendGRNNumber(GRN_Number, InwardId, "Material Pending", vendorId, expensenature);
                            exceptionLogger.LogCreationForInwardStatus(InwardId, "change status capex", null);
                            exceptionLogger.LogCreationForInwardStatus(InwardId, "Material Pending CAPEX", "");
                        }
                        var pdf3 = UpdateInwardPDF(InwardId);
                    }
                    else
                    {
                        var baselist = (from m in context.InwardMaterial_Temp
                                        where m.InwardID == InwardId
                                        select m.MaterialID.ToString()).ToArray();
                        var filteredlist = baselist.Except(materiallist).ToList();

                        foreach (var item in filteredlist)
                        {
                            var statuscapex = string.Empty;
                            var materialinfo = (from m in context.InwardMaterial_Temp
                                                where m.InwardID == InwardId && m.MaterialID == item
                                                select m).SingleOrDefault();
                            InwardMaterial_CAPEX material_CAPEX = new InwardMaterial_CAPEX();
                            material_CAPEX.InvMaterialCP_ID = unitOfWork.DepartmentRepository.GenerateInwardIDS("Inwardcapex");
                            material_CAPEX.InwardID = InwardId;
                            material_CAPEX.Material_CategoryID = materialinfo.Material_CategoryID;
                            material_CAPEX.MaterialID = materialinfo.MaterialID;
                            material_CAPEX.MaterialName = materialinfo.MaterialName;
                            material_CAPEX.Quantity = materialinfo.Quantity;
                            material_CAPEX.Inward_Type = materialinfo.Inward_Type;
                            material_CAPEX.Material_Remark = materialinfo.Material_Remark;
                            material_CAPEX.Accepted_Date = DateTime.Now;
                            if (status == "InwardAccepted")
                            {
                                statuscapex = "Material Rejected CAPEX";
                                material_CAPEX.Status = "Material Rejected";
                            }
                            else
                            {
                                statuscapex = "Material Pending CAPEX";
                                material_CAPEX.Status = "Material Accepted";
                            }
                            material_CAPEX.Acceptedby = userid;
                            context.InwardMaterial_CAPEX.Add(material_CAPEX);
                            context.SaveChanges();
                            exceptionLogger.LogCreationForInwardStatus(InwardId, statuscapex, materialinfo.MaterialID);
                        }
                        foreach (var item1 in materiallist)
                        {
                            var statuscapex1 = string.Empty;
                            var materialunche = (from m in context.InwardMaterial_Temp
                                                 where m.InwardID == InwardId && m.MaterialID == item1
                                                 select m).SingleOrDefault();
                            InwardMaterial_CAPEX material_CAPEX = new InwardMaterial_CAPEX();
                            material_CAPEX.InvMaterialCP_ID = unitOfWork.DepartmentRepository.GenerateInwardIDS("Inwardcapex");
                            material_CAPEX.InwardID = InwardId;
                            material_CAPEX.Material_CategoryID = materialunche.Material_CategoryID;
                            material_CAPEX.MaterialID = materialunche.MaterialID;
                            material_CAPEX.MaterialName = materialunche.MaterialName;
                            material_CAPEX.Quantity = materialunche.Quantity;
                            material_CAPEX.Inward_Type = materialunche.Inward_Type;
                            material_CAPEX.Material_Remark = materialunche.Material_Remark;
                            material_CAPEX.Accepted_Date = DateTime.Now;
                            if (status == "InwardAccepted")
                            {
                                statuscapex1 = "Material Pending CAPEX";
                                material_CAPEX.Status = "Material Accepted";
                            }
                            else
                            {
                                statuscapex1 = "Material Rejected CAPEX";
                                material_CAPEX.Status = "Material Rejected";

                            }
                            material_CAPEX.Acceptedby = userid;
                            context.InwardMaterial_CAPEX.Add(material_CAPEX);
                            context.SaveChanges();
                            exceptionLogger.LogCreationForInwardStatus(InwardId, statuscapex1, materialunche.MaterialID);
                        }
                        var GRN_Number = generateGRNNumber(InwardId);
                        var InwardUpdate = (from t in context.InwardMaterials
                                            where t.InwardID == InwardId && !t.Inward_Status.Equals("Deleted")
                                            select t).SingleOrDefault();
                        InwardUpdate.Inward_Status = "Inward Pending";
                        InwardUpdate.GRN_Number = GRN_Number.ToString();
                        InwardUpdate.Inward_ExpenseNature = expensenature;
                        InwardUpdate.InwardNote = inwardnote;
                        context.SaveChanges();
                        var vendorId = !string.IsNullOrEmpty(InwardUpdate.vendorId) ? InwardUpdate.vendorId : InwardUpdate.TempVendorID;
                        bool mailsend = sendGRNNumber(GRN_Number, InwardId, "Material Pending", vendorId, expensenature);
                        exceptionLogger.LogCreationForInwardStatus(InwardId, "change status capex", null);
                        var pdf4 = UpdateInwardPDF(InwardId);
                    }
                }
                else if (expensenature == "capex" && status == "Inward Pending")
                {
                    for (var i = 0; i < pricelist.Count; i++)
                    {
                        var invcpid = pricelist[i].invcpid[0].ToString();
                        var Inwardcpdetails = (from t in context.InwardMaterial_CAPEX
                                               join ii in context.InwardMaterials on t.InwardID equals ii.InwardID
                                               where t.InvMaterialCP_ID == invcpid && !t.Status.Equals("Deleted")
                                               select new { ii.TempVendorID, ii.vendorId, t.MaterialID, t.Material_CategoryID, t.MaterialName, ii.GRN_Number, ii.InwardID, ii.Location, ii.userDepartmentId }).SingleOrDefault();

                        var rate = pricelist[i].rate[0].ToString();
                        var serialno = pricelist[i].serialno[0].ToString();
                        var make = pricelist[i].make[0].ToString();
                        var modelno = pricelist[i].modelno[0].ToString();
                        var machineno = pricelist[i].MachineID.ToString();
                        var RAM = pricelist[i].RAM.ToString();
                        var hdd = pricelist[i].HDD.ToString();
                        var os = pricelist[i].OS.ToString();


                        Inventory_Register inventory = new Inventory_Register();
                        inventory.InventoryReg_ID = unitOfWork.DepartmentRepository.GenerateInwardIDS("InventoryRegister");
                        var result = generateAssetID(Inwardcpdetails.MaterialName, Inwardcpdetails.Location, Inwardcpdetails.MaterialID);

                        inventory.AssetID = result.Assetid;
                        inventory.Material_CategoryID = Inwardcpdetails.Material_CategoryID;
                        inventory.MaterialID = Inwardcpdetails.MaterialID;
                        inventory.MaterialName = Inwardcpdetails.MaterialName;
                        inventory.Quantity = 1;
                        inventory.Purchase_Rate = float.Parse(rate);
                        //inventory.Asset_Cost = float.Parse(rate);
                        inventory.Serial_No = serialno;
                        inventory.Model_Number = modelno;
                        inventory.Make = make;
                        inventory.ExpenseNature = "capex";
                        inventory.IR_Status = "Material In";
                        inventory.AssetGenerateDate = DateTime.Now;
                        inventory.Inward_CP_Rental_ID = invcpid;
                        inventory.vendorId = Inwardcpdetails.TempVendorID ?? Inwardcpdetails.vendorId;
                        inventory.userDepartmentId = Inwardcpdetails.userDepartmentId;
                        inventory.Location = Inwardcpdetails.Location;
                        inventory.MachineID = machineno;
                        inventory.RAM = RAM;
                        inventory.HDD = hdd;
                        inventory.OS = os;
                        if (Inwardcpdetails.GRN_Number != null)
                        {
                            inventory.GRN_Number = Inwardcpdetails.GRN_Number;
                        }
                        inventory.InwardID = Inwardcpdetails.InwardID;
                        unitOfWork.DepartmentRepository.Insert(inventory);
                        unitOfWork.Save();

                        var CategoryUpdate = (from t in context.Serial_Number
                                              where t.Year_Range == result.year_range && t.Location == Inwardcpdetails.Location
                                              select t).SingleOrDefault();
                        CategoryUpdate.Serial_Asset_Number = result.FinalNumber;
                        context.SaveChanges();
                    }

                    var InwardUpdate = (from t in context.InwardMaterials
                                        where t.InwardID == InwardId && !t.Inward_Status.Equals("Deleted")
                                        select t).SingleOrDefault();
                    InwardUpdate.Inward_Status = "Inward Accepted";
                    context.SaveChanges();
                    exceptionLogger.LogCreationForInwardStatus(InwardId, "Change Inward Accepted status for capex", null);
                    var pdf5 = UpdateInwardPDF(InwardId);
                    return RedirectToAction("Index", "Add_InventoryRegister");
                }
                else if (expensenature == "customerasset" && (status == "InwardAccepted" || status == "InwardRejected"))
                {
                    if (m_count == materialcount)
                    {
                        foreach (var item in materialupdate)
                        {
                            InwardMaterial_CustomerAsset material_capex = new InwardMaterial_CustomerAsset();
                            material_capex.InvMaterialCA_ID = unitOfWork.DepartmentRepository.GenerateInwardIDS("Inwardcustasset");
                            material_capex.InwardID = item.InwardID;
                            material_capex.Material_CategoryID = item.Material_CategoryID;
                            material_capex.MaterialID = item.MaterialID;
                            material_capex.MaterialName = item.MaterialName;
                            material_capex.Quantity = item.Quantity;
                            material_capex.Inward_Type = item.Inward_Type;
                            material_capex.Material_Remark = item.Material_Remark;
                            material_capex.Accepted_Date = DateTime.Now;
                            if (status == "InwardAccepted")
                            {
                                material_capex.Status = "Material Accepted";
                            }
                            else
                            {
                                material_capex.Status = "Material Rejected";
                            }
                            material_capex.Acceptedby = userid;
                            context.InwardMaterial_CustomerAsset.Add(material_capex);
                            context.SaveChanges();
                        }

                        var InwardUpdate = (from t in context.InwardMaterials
                                            where t.InwardID == InwardId && !t.Inward_Status.Equals("Deleted")
                                            select t).SingleOrDefault();
                        var GRN_Number = string.Empty;
                        if (status == "InwardRejected")
                        {
                            InwardUpdate.Inward_Status = "Inward Rejected";
                        }
                        else
                        {
                            InwardUpdate.Inward_Status = "Inward Pending";
                            GRN_Number = generateGRNNumber(InwardId);
                            InwardUpdate.GRN_Number = GRN_Number.ToString();
                        }
                        InwardUpdate.Inward_ExpenseNature = expensenature;
                        InwardUpdate.InwardNote = inwardnote;
                        context.SaveChanges();
                        var vendorId = !string.IsNullOrEmpty(InwardUpdate.vendorId) ? InwardUpdate.vendorId : InwardUpdate.TempVendorID;

                        if (status == "InwardRejected")
                        {
                            var mailsend = sendInwardMail(InwardId, "Inward Rejected");
                            exceptionLogger.LogCreationForInwardStatus(InwardId, "change status customerasset", null);
                            exceptionLogger.LogCreationForInwardStatus(InwardId, "Material Rejected customerasset", "");

                        }
                        else
                        {
                            bool mailsend = sendGRNNumber(GRN_Number, InwardId, "Material Pending", vendorId, expensenature);
                            exceptionLogger.LogCreationForInwardStatus(InwardId, "change status customerasset", null);
                            exceptionLogger.LogCreationForInwardStatus(InwardId, "Material Pending customerasset", "");
                        }
                        var pdf3 = UpdateInwardPDF(InwardId);
                    }
                    else
                    {
                        var baselist = (from m in context.InwardMaterial_Temp
                                        where m.InwardID == InwardId
                                        select m.MaterialID.ToString()).ToArray();
                        var filteredlist = baselist.Except(materiallist).ToList();

                        foreach (var item in filteredlist)
                        {
                            var statuscapex = string.Empty;
                            var materialinfo = (from m in context.InwardMaterial_Temp
                                                where m.InwardID == InwardId && m.MaterialID == item
                                                select m).SingleOrDefault();
                            InwardMaterial_CustomerAsset material_CAPEX = new InwardMaterial_CustomerAsset();
                            material_CAPEX.InvMaterialCA_ID = unitOfWork.DepartmentRepository.GenerateInwardIDS("Inwardcustasset");
                            material_CAPEX.InwardID = InwardId;
                            material_CAPEX.Material_CategoryID = materialinfo.Material_CategoryID;
                            material_CAPEX.MaterialID = materialinfo.MaterialID;
                            material_CAPEX.MaterialName = materialinfo.MaterialName;
                            material_CAPEX.Quantity = materialinfo.Quantity;
                            material_CAPEX.Inward_Type = materialinfo.Inward_Type;
                            material_CAPEX.Material_Remark = materialinfo.Material_Remark;
                            material_CAPEX.Accepted_Date = DateTime.Now;
                            if (status == "InwardAccepted")
                            {
                                statuscapex = "Material Rejected customerasset";
                                material_CAPEX.Status = "Material Rejected";
                            }
                            else
                            {
                                statuscapex = "Material Pending customerasset";
                                material_CAPEX.Status = "Material Accepted";
                            }
                            material_CAPEX.Acceptedby = userid;
                            context.InwardMaterial_CustomerAsset.Add(material_CAPEX);
                            context.SaveChanges();
                            exceptionLogger.LogCreationForInwardStatus(InwardId, statuscapex, materialinfo.MaterialID);
                        }
                        foreach (var item1 in materiallist)
                        {
                            var statuscapex1 = string.Empty;
                            var materialunche = (from m in context.InwardMaterial_Temp
                                                 where m.InwardID == InwardId && m.MaterialID == item1
                                                 select m).SingleOrDefault();
                            InwardMaterial_CustomerAsset material_CAPEX = new InwardMaterial_CustomerAsset();
                            material_CAPEX.InvMaterialCA_ID = unitOfWork.DepartmentRepository.GenerateInwardIDS("Inwardcustasset");
                            material_CAPEX.InwardID = InwardId;
                            material_CAPEX.Material_CategoryID = materialunche.Material_CategoryID;
                            material_CAPEX.MaterialID = materialunche.MaterialID;
                            material_CAPEX.MaterialName = materialunche.MaterialName;
                            material_CAPEX.Quantity = materialunche.Quantity;
                            material_CAPEX.Inward_Type = materialunche.Inward_Type;
                            material_CAPEX.Material_Remark = materialunche.Material_Remark;
                            material_CAPEX.Accepted_Date = DateTime.Now;
                            if (status == "InwardAccepted")
                            {
                                statuscapex1 = "Material Pending customerasset";
                                material_CAPEX.Status = "Material Accepted";
                            }
                            else
                            {
                                statuscapex1 = "Material Rejected customerasset";
                                material_CAPEX.Status = "Material Rejected";

                            }
                            material_CAPEX.Acceptedby = userid;
                            context.InwardMaterial_CustomerAsset.Add(material_CAPEX);
                            context.SaveChanges();
                            exceptionLogger.LogCreationForInwardStatus(InwardId, statuscapex1, materialunche.MaterialID);
                        }
                        var GRN_Number = generateGRNNumber(InwardId);
                        var InwardUpdate = (from t in context.InwardMaterials
                                            where t.InwardID == InwardId && !t.Inward_Status.Equals("Deleted")
                                            select t).SingleOrDefault();
                        InwardUpdate.Inward_Status = "Inward Pending";
                        InwardUpdate.GRN_Number = GRN_Number.ToString();
                        InwardUpdate.Inward_ExpenseNature = expensenature;
                        InwardUpdate.InwardNote = inwardnote;
                        context.SaveChanges();
                        var vendorId = !string.IsNullOrEmpty(InwardUpdate.vendorId) ? InwardUpdate.vendorId : InwardUpdate.TempVendorID;

                        bool mailsend = sendGRNNumber(GRN_Number, InwardId, "Material Pending", vendorId, expensenature);
                        exceptionLogger.LogCreationForInwardStatus(InwardId, "change status capex", null);
                        var pdf4 = UpdateInwardPDF(InwardId);
                    }

                }
                else if (expensenature == "customerasset" && status == "Inward Pending")
                {
                    for (var i = 0; i < pricelist.Count; i++)
                    {
                        var invcpid = pricelist[i].invcpid[0].ToString();
                        var Inwardcpdetails = (from t in context.InwardMaterial_CustomerAsset
                                               join ii in context.InwardMaterials on t.InwardID equals ii.InwardID
                                               where t.InvMaterialCA_ID == invcpid && !t.Status.Equals("Deleted")
                                               select new { ii.vendorId, ii.TempVendorID, t.MaterialID, t.Material_CategoryID, t.MaterialName, ii.GRN_Number, ii.InwardID, ii.Location, ii.userDepartmentId }).SingleOrDefault();

                        var rate = pricelist[i].rate[0].ToString();
                        var serialno = pricelist[i].serialno[0].ToString();
                        var make = pricelist[i].make[0].ToString();
                        var modelno = pricelist[i].modelno[0].ToString();
                        var machineno = pricelist[i].MachineID[0].ToString();
                        var RAM = pricelist[i].RAM[0].ToString();
                        var hdd = pricelist[i].HDD[0].ToString();
                        var os = pricelist[i].OS[0].ToString();

                        Inventory_Register inventory = new Inventory_Register();
                        inventory.InventoryReg_ID = unitOfWork.DepartmentRepository.GenerateInwardIDS("InventoryRegister");
                        var result = generateAssetID(Inwardcpdetails.MaterialName, Inwardcpdetails.Location, Inwardcpdetails.MaterialID);

                        inventory.AssetID = result.Assetid;
                        inventory.Material_CategoryID = Inwardcpdetails.Material_CategoryID;
                        inventory.MaterialID = Inwardcpdetails.MaterialID;
                        inventory.MaterialName = Inwardcpdetails.MaterialName;
                        inventory.Quantity = 1;
                        inventory.Purchase_Rate = float.Parse(rate);
                       // inventory.Asset_Cost = float.Parse(rate);
                        inventory.Serial_No = serialno;
                        inventory.Model_Number = modelno;
                        inventory.Make = make;
                        inventory.ExpenseNature = "customerasset";
                        inventory.IR_Status = "Material In";
                        inventory.AssetGenerateDate = DateTime.Now;
                        inventory.Location = Inwardcpdetails.Location;
                        inventory.Inward_CP_Rental_ID = invcpid;
                        inventory.vendorId = Inwardcpdetails.TempVendorID ?? Inwardcpdetails.vendorId;
                        inventory.MachineID = machineno;
                        inventory.RAM = RAM;
                        inventory.HDD = hdd;
                        inventory.OS = os;
                        if (Inwardcpdetails.GRN_Number != null)
                        {
                            inventory.GRN_Number = Inwardcpdetails.GRN_Number;
                        }
                        inventory.InwardID = Inwardcpdetails.InwardID;
                        inventory.userDepartmentId = Inwardcpdetails.userDepartmentId;

                        unitOfWork.DepartmentRepository.Insert(inventory);
                        unitOfWork.Save();

                        var CategoryUpdate = (from t in context.Serial_Number
                                              where t.Year_Range == result.year_range && t.Location == Inwardcpdetails.Location
                                              select t).SingleOrDefault();
                        CategoryUpdate.Serial_Asset_Number = result.FinalNumber;
                        context.SaveChanges();
                    }

                    var InwardUpdate = (from t in context.InwardMaterials
                                        where t.InwardID == InwardId && !t.Inward_Status.Equals("Deleted")
                                        select t).SingleOrDefault();
                    InwardUpdate.Inward_Status = "Inward Accepted";
                    context.SaveChanges();
                    exceptionLogger.LogCreationForInwardStatus(InwardId, "Change Inward Accepted status for customerasset", null);
                    var pdf4 = UpdateInwardPDF(InwardId);
                    return RedirectToAction("Index", "Add_InventoryRegister");
                }

                else if (expensenature == "rental" && (status == "InwardAccepted" || status == "InwardRejected"))
                {
                    if (m_count == materialcount)
                    {
                        foreach (var item in materialupdate)
                        {
                            InwardMaterial_Rental material_capex = new InwardMaterial_Rental();
                            material_capex.InvMaterialrental_ID = unitOfWork.DepartmentRepository.GenerateInwardIDS("Inwardrental");
                            material_capex.InwardID = item.InwardID;
                            material_capex.MaterialID = item.MaterialID;
                            material_capex.MaterialName = item.MaterialName;
                            material_capex.Material_CategoryID = item.Material_CategoryID;
                            material_capex.Quantity = item.Quantity;
                            material_capex.Inward_Type = item.Inward_Type;
                            material_capex.Material_Remark = item.Material_Remark;
                            material_capex.Accepted_Date = DateTime.Now;
                            if (status == "InwardAccepted")
                            {
                                material_capex.Status = "Material Accepted";
                            }
                            else
                            {
                                material_capex.Status = "Material Rejected";
                            }
                            material_capex.Acceptedby = userid;
                            context.InwardMaterial_Rental.Add(material_capex);
                            context.SaveChanges();
                        }
                        var GRN_Number = string.Empty;
                        var InwardUpdate = (from t in context.InwardMaterials
                                            where t.InwardID == InwardId && !t.Inward_Status.Equals("Deleted")
                                            select t).SingleOrDefault();
                        if (status == "InwardRejected")
                        {
                            InwardUpdate.Inward_Status = "Inward Rejected";
                        }
                        else
                        {
                            InwardUpdate.Inward_Status = "Inward Pending";
                            GRN_Number = generateGRNNumber(InwardId);
                            InwardUpdate.GRN_Number = GRN_Number.ToString();
                        }
                        InwardUpdate.Inward_ExpenseNature = expensenature;
                        InwardUpdate.InwardNote = inwardnote;
                        context.SaveChanges();
                        var vendorId = !string.IsNullOrEmpty(InwardUpdate.vendorId) ? InwardUpdate.vendorId : InwardUpdate.TempVendorID;

                        if (status == "InwardRejected")
                        {
                            var mailsend = sendInwardMail(InwardId, "Inward Rejected");
                            exceptionLogger.LogCreationForInwardStatus(InwardId, "change status rental", null);
                            exceptionLogger.LogCreationForInwardStatus(InwardId, "Material Rejected rental", "");
                        }
                        else
                        {
                            bool mailsend = sendGRNNumber(GRN_Number, InwardId, "Material Pending", vendorId, expensenature);
                            exceptionLogger.LogCreationForInwardStatus(InwardId, "change status rental", null);
                            exceptionLogger.LogCreationForInwardStatus(InwardId, "Material Pending rental", "");
                        }
                        var pdf5 = UpdateInwardPDF(InwardId);
                    }
                    else
                    {
                        var baselist = (from m in context.InwardMaterial_Temp
                                        where m.InwardID == InwardId
                                        select m.MaterialID.ToString()).ToArray();
                        var filteredlist = baselist.Except(materiallist).ToList();

                        foreach (var item in filteredlist)
                        {
                            var statustrental = string.Empty;
                            var materialinfo = (from m in context.InwardMaterial_Temp
                                                where m.InwardID == InwardId && m.MaterialID == item
                                                select m).SingleOrDefault();
                            InwardMaterial_Rental material_CAPEX = new InwardMaterial_Rental();
                            material_CAPEX.InvMaterialrental_ID = unitOfWork.DepartmentRepository.GenerateInwardIDS("Inwardrental");
                            material_CAPEX.InwardID = InwardId;
                            material_CAPEX.Material_CategoryID = materialinfo.Material_CategoryID;
                            material_CAPEX.MaterialID = materialinfo.MaterialID;
                            material_CAPEX.MaterialName = materialinfo.MaterialName;
                            material_CAPEX.Quantity = materialinfo.Quantity;
                            material_CAPEX.Inward_Type = materialinfo.Inward_Type;
                            material_CAPEX.Material_Remark = materialinfo.Material_Remark;
                            material_CAPEX.Accepted_Date = DateTime.Now;
                            if (status == "InwardAccepted")
                            {
                                statustrental = "Material Rejected rental";
                                material_CAPEX.Status = "Material Rejected";
                            }
                            else
                            {
                                statustrental = "Material Pending rental";
                                material_CAPEX.Status = "Material Accepted";
                            }
                            material_CAPEX.Acceptedby = userid;
                            context.InwardMaterial_Rental.Add(material_CAPEX);
                            context.SaveChanges();
                            exceptionLogger.LogCreationForInwardStatus(InwardId, statustrental, materialinfo.MaterialID);
                        }
                        foreach (var item1 in materiallist)
                        {
                            string statustrental1 = string.Empty;
                            var materialunche = (from m in context.InwardMaterial_Temp
                                                 where m.InwardID == InwardId && m.MaterialID == item1
                                                 select m).SingleOrDefault();
                            InwardMaterial_Rental material_CAPEX = new InwardMaterial_Rental();
                            material_CAPEX.InvMaterialrental_ID = unitOfWork.DepartmentRepository.GenerateInwardIDS("Inwardrental");
                            material_CAPEX.InwardID = InwardId;
                            material_CAPEX.MaterialID = materialunche.MaterialID;
                            material_CAPEX.MaterialName = materialunche.MaterialName;
                            material_CAPEX.Quantity = materialunche.Quantity;
                            material_CAPEX.Inward_Type = materialunche.Inward_Type;
                            material_CAPEX.Material_Remark = materialunche.Material_Remark;
                            material_CAPEX.Accepted_Date = DateTime.Now;
                            if (status == "InwardAccepted")
                            {
                                statustrental1 = "Material Pending rental";
                                material_CAPEX.Status = "Material Accepted";
                            }
                            else
                            {
                                statustrental1 = "Material Rejected rental";
                                material_CAPEX.Status = "Material Rejected";
                            }
                            material_CAPEX.Acceptedby = userid;
                            context.InwardMaterial_Rental.Add(material_CAPEX);
                            context.SaveChanges();
                            exceptionLogger.LogCreationForInwardStatus(InwardId, statustrental1, materialunche.MaterialID);
                        }
                        var GRN_Number = generateGRNNumber(InwardId);
                        var InwardUpdate = (from t in context.InwardMaterials
                                            where t.InwardID == InwardId && !t.Inward_Status.Equals("Deleted")
                                            select t).SingleOrDefault();
                        InwardUpdate.Inward_Status = "Inward Pending";
                        InwardUpdate.GRN_Number = GRN_Number.ToString();
                        InwardUpdate.Inward_ExpenseNature = expensenature;
                        InwardUpdate.InwardNote = inwardnote;
                        context.SaveChanges();
                        var vendorId = !string.IsNullOrEmpty(InwardUpdate.vendorId) ? InwardUpdate.vendorId : InwardUpdate.TempVendorID;

                        bool mailsend = sendGRNNumber(GRN_Number, InwardId, "Material Pending", vendorId, expensenature);
                        exceptionLogger.LogCreationForInwardStatus(InwardId, "change status rental", null);
                        var pdf6 = UpdateInwardPDF(InwardId);
                    }

                }
                else if (expensenature == "rental" && status == "Inward Pending")
                {
                    for (var i = 0; i < pricelist.Count; i++)
                    {
                        var invcpid = pricelist[i].invcpid[0].ToString();
                        var Inwardcpdetails = (from t in context.InwardMaterial_Rental
                                               join ii in context.InwardMaterials on t.InwardID equals ii.InwardID
                                               where t.InvMaterialrental_ID == invcpid && !t.Status.Equals("Deleted")
                                               select new { ii.TempVendorID, ii.vendorId, t.MaterialID, t.Material_CategoryID, t.MaterialName, ii.GRN_Number, ii.InwardID, ii.Location, ii.userDepartmentId }).SingleOrDefault();

                        var rate = pricelist[i].rate[0].ToString();
                        var rentalrate = pricelist[i].rentalcost[0].ToString();
                        var serialno = pricelist[i].serialno[0].ToString();
                        var make = pricelist[i].make[0].ToString();
                        var modelno = pricelist[i].modelno[0].ToString();
                        var machineno = pricelist[i].MachineID.ToString();
                        var RAM = pricelist[i].RAM.ToString();
                        var hdd = pricelist[i].HDD.ToString();
                        var os = pricelist[i].OS.ToString();

                        Inventory_Register inventory = new Inventory_Register();
                        inventory.InventoryReg_ID = unitOfWork.DepartmentRepository.GenerateUniqueCode();
                        var result = generateAssetID(Inwardcpdetails.MaterialName, Inwardcpdetails.Location, Inwardcpdetails.MaterialID);

                        inventory.AssetID = result.Assetid;
                        inventory.Material_CategoryID = Inwardcpdetails.Material_CategoryID;
                        inventory.MaterialID = Inwardcpdetails.MaterialID;
                        inventory.MaterialName = Inwardcpdetails.MaterialName;
                        inventory.Quantity = 1;
                        inventory.Purchase_Rate = float.Parse(rate);
                        inventory.Asset_Cost = float.Parse(rentalrate);
                        inventory.Serial_No = serialno;
                        inventory.Model_Number = modelno;
                        inventory.Location = Inwardcpdetails.Location;
                        inventory.Make = make;
                        inventory.ExpenseNature = "rental";
                        inventory.IR_Status = "Material In";
                        inventory.AssetGenerateDate = DateTime.Now;
                        inventory.vendorId = Inwardcpdetails.TempVendorID ?? Inwardcpdetails.vendorId;
                        inventory.Inward_CP_Rental_ID = invcpid;
                        inventory.MachineID = machineno;
                        inventory.RAM = RAM;
                        inventory.HDD = hdd;
                        inventory.OS = os;
                        inventory.userDepartmentId = Inwardcpdetails.userDepartmentId;

                        if (Inwardcpdetails.GRN_Number != null)
                        {
                            inventory.GRN_Number = Inwardcpdetails.GRN_Number;
                        }
                        inventory.InwardID = Inwardcpdetails.InwardID;
                        unitOfWork.DepartmentRepository.Insert(inventory);
                        unitOfWork.Save();

                        var CategoryUpdate = (from t in context.Serial_Number
                                              where t.Year_Range == result.year_range && t.Location == Inwardcpdetails.Location
                                              select t).SingleOrDefault();
                        CategoryUpdate.Serial_Asset_Number = result.FinalNumber;
                        context.SaveChanges();
                    }

                    var InwardUpdate = (from t in context.InwardMaterials
                                        where t.InwardID == InwardId && !t.Inward_Status.Equals("Deleted")
                                        select t).SingleOrDefault();
                    InwardUpdate.Inward_Status = "Inward Accepted";
                    context.SaveChanges();
                    exceptionLogger.LogCreationForInwardStatus(InwardId, "Change Inward Accepted status for rental", null);
                    var pdf4 = UpdateInwardPDF(InwardId);
                    return RedirectToAction("Index", "Add_InventoryRegister");
                }
            }
            if ((expensenature == "rental" && status == "Inward Pending") || expensenature == "capex" && status == "Inward Pending")
            {
                return RedirectToAction("Index", "Add_InventoryRegister");
            }
            else
            {
                InwardId = unitOfWork.DepartmentRepository.Encode(InwardId);
                return RedirectToAction("InwardDetails", "Add_InwardMaterial", new { inwardID = InwardId });
            }

        }

        public string generateGRNNumber(string InwardId)
        {
            string GRNnumber = string.Empty;
            string userid = string.Empty;
            if (Session["UserID"] != null)
            {
                userid = Session["UserID"].ToString();
            }
            var serial_number = new Serial_Number();
            var InwardDetails = (from t in context.InwardMaterials
                                 where t.InwardID == InwardId && !t.Inward_Status.Equals("Deleted")
                                 select t).SingleOrDefault();
            var section = string.Empty;
            if (InwardDetails.Location == "Global Port")
            {
                section = "H1";
            }
            else if (InwardDetails.Location == "Siddhant")
            {
                section = "H2";
            }
            else if (InwardDetails.Location == "SEZ")
            {
                section = "H3";
            }

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
            var categoryNumber = unitOfWork.DepartmentRepository.Update(serial_number, "GRN", year_range, InwardDetails.Location.ToString());

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
                    suffixpo = "000";
                    break;
                case 2:
                    suffixpo = "00";
                    break;
                case 3:
                    suffixpo = "0";
                    break;
                case 4:
                    suffixpo = "";
                    break;
                case 0:
                    suffixpo = "000"; // Default to empty suffix if count exceeds 5 digits
                    break;
            }

            if (userid.Contains("DUM"))
            {
                GRNnumber = "DUMMYGRN";
                finalnumber = finalnumber - 1;
            }
            else
            {
                GRNnumber = "GRN/" + section + "/" + currentdatetime + "/" + suffixpo + finalnumber;
                finalnumber = finalnumber;
            }

            var CategoryUpdate = (from t in context.Serial_Number
                                  where t.Year_Range == year_range && t.Location == InwardDetails.Location
                                  select t).SingleOrDefault();
            CategoryUpdate.Serial_GRN_Number = finalnumber;
            context.SaveChanges();
            return GRNnumber;
        }

        public bool sendGRNNumber(string grnnumber, string inwardid, string status, string vendorid, string expensenature)
        {
            try
            {
                string userid = string.Empty;
                if (Session["UserID"] != null)
                {
                    userid = Session["UserID"].ToString();
                }

                var Inwarddetails = (from u in context.InwardMaterials
                                     where u.InwardID == inwardid
                                     select new { u.InwardDateTime, u.userDepartmentId,u.Location }).SingleOrDefault();

                var deliverchallen = (from a in context.DeliveryChallens
                                      where a.InwardID == inwardid
                                      select new { a.DC_ID, a.DC_Filename });

                var userdetails = (from u in context.IMSUsers
                                   where u.userId == userid && u.userStatus != "Archived"
                                   select new { u.userEmail }).SingleOrDefault();

                var userinventory = (from u in context.IMSUsers
                                     join i in context.IMSRoles on u.userId equals i.IMSuser_Id
                                     where u.userStatus != "Archived" && i.RoleInIMS == "Inventory Incharge"
                                     && i.userDepartmentId == Inwarddetails.userDepartmentId && 
                                     i.userLocation == Inwarddetails.Location
                                     select new { u.userEmail });
                var vendorname = (from v in context.HTV_Vendor
                                  where v.vendorId == vendorid && v.vendorStatus != "Inactive" && v.vendorStatus != "Blacklisted" && v.vendorStatus != "Reject"
                                  select new { v.vendorEmail, v.vendorName }).SingleOrDefault();

                if (vendorname == null)
                {
                    vendorname = (from t in context.TempVendors
                                  where t.TempVendorID == vendorid
                                  select new { vendorEmail = t.TempVendorEmail, vendorName = t.TempVendorName }).SingleOrDefault();
                }

                string Body = string.Empty;
                string sub = string.Empty;
                MailMessage mail = new MailMessage();
                string username = ConfigurationManager.AppSettings["SmtpUserId"].ToString();
                var mstatus = "Accepted";

                mail.To.Add(vendorname.vendorEmail.ToString());
                mail.CC.Add(userdetails.userEmail.ToString());
                if (expensenature == "capex" || expensenature == "rental" || expensenature == "customerasset")
                {
                    if(userinventory != null)
                    {
                        foreach(var user in userinventory)
                        {
                            mail.CC.Add(user.userEmail.ToString());
                        }
                    }
                }
                
                if (status == "Material Pending")
                {
                    mstatus = "Pending";
                    foreach (var i in userinventory)
                    {
                        mail.To.Add(i.userEmail.ToString());
                    }
                }
                foreach (var f in deliverchallen)
                {
                    DirectoryInfo dir = new DirectoryInfo(@"D:\IMSDocs\DeliveryChallen\" + inwardid + "\\" + f.DC_ID);
                    foreach (FileInfo file2 in dir.GetFiles("*.*"))
                    {
                        if (file2.Exists)
                        {
                            mail.Attachments.Add(new Attachment(file2.FullName));
                        }
                    }
                }
                sub = "Material Accepted and GRN Generated";
                Body = "Hi, <br/><br/> This is to inform you that the GRN  " + grnnumber + ") has been generated for the Inward Material received from "
                        + vendorname.vendorName.ToString() + ". The details are as follows. Request you to use the same GRN Number while uploading the invoice in VMS"
                        + "<br/>" + "<b>GRN Number: </b>" + grnnumber
                        + "<br/>" + "<b>Inward Date: </b>" + Inwarddetails.InwardDateTime.ToString()
                        + "<br/><br/>Best Regards <br/> Team Harbinger IMS";

                var end_body = "<br><br><font size=\"2\" color=\"red\"> <i> Note : This is a system generated email. Please do not reply to this email.</i></font>";
                Body = Body + end_body;

                //else if(status == "Material Pending")
                //{



                //    var materiallist = (from i in context.InwardMaterials
                //                        join m in context.InwardMaterial_CAPEX on i.InwardID equals m.InwardID
                //                        where i.InwardID == inwardid
                //                        select new
                //                        {
                //                            m.MaterialName,
                //                            m.Quantity,
                //                            m.Inward_Type,
                //                            m.Status
                //                        });
                //    var materialbody = string.Empty;
                //    sub = "Material is Pending for Approver on IMS";
                //    Body = "This is inform that Material is Pending for Approver. Please find the details below:" +
                //           "<br<<br>" + "Vendor Name: " + vendorname.vendorName +
                //           "<br>" + "<table border=1><tr><th>Material name</th><th>Quantity</th><th>Inward Type</th><th>Status</th></tr>";

                //    foreach (var m in materiallist)
                //     {
                //      materialbody = "<tr><td>" + m.MaterialName + "</td><td>" + m.Quantity + "</td><td>" + m.Inward_Type + "</td><td>" + m.Status + "</tr>";                       
                //     }

                //    Body = Body + materialbody + "</table>";                           

                //    var end_body = "<br><br><font size=\"2\" color=\"red\"> <i> Note : This is a system generated email. Please do not reply to this email.</i></font>";
                //    Body = Body + end_body;
                //}
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
                    controllerName = "Add_InvoiceController",
                    actionName = "sendGRNNumber",
                    exceptionStackTrace = e.StackTrace,
                    exceptionMessage = e.Message,
                    exceptionLogTime = DateTime.Now
                };

                unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                unitOfWork.Save();
                return false;
            }
        }

        public ActionResult ViewInwarddocx(string id, string name, string InvId)
        {
            if (Session["Email"] != null)
            {
                string filePath = null;
                id = unitOfWork.DepartmentRepository.Decode(id);
                InvId = unitOfWork.DepartmentRepository.Decode(InvId);
                try
                {
                    string fileName = null;
                    string[] filesNames1 = null;
                    if (name == "DeliveryChallen")
                    {
                        filesNames1 = Directory.GetFiles(@"D:\IMSDocs\DeliveryChallen\" + InvId + "\\" + id);
                    }

                    List<string> items1 = new List<string>();
                    foreach (string file in filesNames1)
                    {
                        fileName = Path.GetFileName(file);
                        string fileExtn = Path.GetExtension(file);
                    }

                    if (name == "DeliveryChallen")
                    {
                        filePath = @"D:\IMSDocs\DeliveryChallen\" + InvId + "\\" + id + "\\" + fileName;
                    }

                    Response.AddHeader("Content-Disposition", "inline; filename=" + fileName);
                    byte[] FileBytes = System.IO.File.ReadAllBytes(filePath);
                    return File(FileBytes, "application/unknown");
                }
                catch (Exception e)
                {
                    Console.Write(e);
                    var exceptionLogger = new IMSExceptionLogger()
                    {
                        controllerName = "Add_InvoiceController",
                        actionName = "ViewInvoicedocx",
                        exceptionStackTrace = e.StackTrace,
                        exceptionMessage = e.Message,
                        exceptionLogTime = DateTime.Now
                    };

                    unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                    unitOfWork.Save();
                }
                return File(filePath, "application/unknown");

            }
            else
            {
                return HttpNotFound();
            }

        }

        [HttpGet]
        public ActionResult DownloadInwarddocx(string items, string InvId, string type, string dcid)
        {
            if (Session["Email"] != null)
            {
                if (dcid != null)
                {
                    dcid = unitOfWork.DepartmentRepository.Decode(dcid);
                }
                InvId = unitOfWork.DepartmentRepository.Decode(InvId);
                try
                {
                    if (type == "Deliverychallen")
                    {
                        string FileVirtualPath = @"D:\IMSDocs\DeliveryChallen\" + InvId + "\\" + dcid + "\\" + items;
                        return File(FileVirtualPath, "application/force-download", Path.GetFileName(FileVirtualPath));
                    }
                    if (type == "InwardPDF")
                    {
                        string FileVirtualPath = @"D:\IMSDocs\InwardMaterial\" + InvId + "\\" + items;
                        return File(FileVirtualPath, "application/force-download", Path.GetFileName(FileVirtualPath));
                    }
                    else if (type == "Invoice")
                    {
                        string FileVirtualPath = @"D:\HTV-VMSDocs\Invoice\" + InvId + "/" + items;
                        return File(FileVirtualPath, "application/force-download", Path.GetFileName(FileVirtualPath));
                    }

                }
                catch (Exception e)
                {
                    var exceptionLogger = new IMSExceptionLogger()
                    {
                        controllerName = "Add_InwardMaterialController",
                        actionName = "DownloadInwarddocx",
                        exceptionStackTrace = e.StackTrace,
                        exceptionMessage = e.Message,
                        exceptionLogTime = DateTime.Now
                    };

                    unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                    unitOfWork.Save();
                }
                return View();
            }
            else
            {
                return HttpNotFound();
            }
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

        public ActionResult ViewInvoicedocx(string id, string name)
        {
            string filePath = null;
            try
            {
                id = unitOfWork.DepartmentRepository.Decode(id);
                string fileName = null;
                string[] filesNames1 = null;
                if (name == "Invoice")
                    filesNames1 = Directory.GetFiles(@"D:\HTV-VMSDocs\Invoices\" + id + " / ");
                else
                    filesNames1 = Directory.GetFiles(@"D:\HTV-VMSDocs\InvoiceSupportDoc\" + id + " / ");



                List<string> items1 = new List<string>();
                foreach (string file in filesNames1)
                {
                    fileName = Path.GetFileName(file);
                    string fileExtn = Path.GetExtension(file);



                }
                if (name == "Invoice")
                    filePath = @"D:\HTV-VMSDocs\Invoices\" + id + "/" + fileName;
                else
                    filePath = @"D:\HTV-VMSDocs\InvoiceSupportDoc\" + id + "/" + fileName;
                Response.AddHeader("Content-Disposition", "inline; filename=" + fileName);

                byte[] FileBytes = System.IO.File.ReadAllBytes(filePath);
                return File(FileBytes, "application/unknown");
            }
            catch (Exception e)
            {
                Console.Write(e);



                var exceptionLogger = new IMSExceptionLogger()
                {
                    controllerName = "Add_InwardMaterialController",
                    actionName = "ViewInvoicedocx",
                    exceptionStackTrace = e.StackTrace,
                    exceptionMessage = e.Message,
                    exceptionLogTime = DateTime.Now



                };



                unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                unitOfWork.Save();
            }
            return File(filePath, "application/unknown");



        }


    }
}