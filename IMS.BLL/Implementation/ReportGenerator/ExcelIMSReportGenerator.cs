using IMS.BLL.Interfaces;
using IMS.DAL;
using IMS.Entities;
using IMS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.BLL.Implementation.ReportGenerator
{


    public class ExcelIMSReportGenerator : IMSReportGenerator
    {
        private UnitOfWork unitOfWork = new UnitOfWork();
        private ServiceVMSEntities _context = new ServiceVMSEntities();

        //public ExcelIMSReportGenerator(ServiceVMSEntities context)
        //{
        //    _context = context;
        //}

        public List<InwardReportEntity> ReportGenerationonInward(IMSEntity entity)
        {
            var inwarddata = (from im in _context.InwardMaterials
                              join opex in _context.InwardMaterial_Temp on im.InwardID equals opex.InwardID into opexJoin
                              from opex in opexJoin.DefaultIfEmpty()
                              join d in _context.ServiceUserDepartments on im.userDepartmentId equals d.userDepartmentId
                              join cat in _context.Material_Category on opex.Material_CategoryID equals cat.Material_CategoryID into catJoin
                              from cat in catJoin.DefaultIfEmpty()
                              join v in _context.HTV_Vendor on im.vendorId equals v.vendorId
                              where DbFunctions.TruncateTime(im.InwardDateTime) >= DbFunctions.TruncateTime(entity.startdate) &&
                                    DbFunctions.TruncateTime(im.InwardDateTime) <= DbFunctions.TruncateTime(entity.enddate) &&
                                    im.Inward_Status != "Deleted" &&
                                    im.GRN_Number == null &&
                                    im.Inward_ExpenseNature == null
                              select new InwardReportEntity
                              {
                                  InwardDateTime = im.InwardDateTime,
                                  Inward_ExpenseNature = im.Inward_ExpenseNature ?? "NUll",
                                  Material_Name = opex != null ? opex.MaterialName : null,
                                  vendorName = v.vendorName,
                                  Quantity = opex != null ? opex.Quantity : 0,
                                  Inward_Type = opex != null ? opex.Inward_Type : null,
                                  GRN_Number = im.GRN_Number ?? "NULL",
                                  Location = im.Location,
                                  userDepartmentName = d.userDepartmentName,
                                  SecurityName = im.InwardEnteredBy,
                                  Material_CategoryName = cat != null ? cat.Material_CategoryName : null,
                                  Inward_Status = im.Inward_Status,
                                  MaterialRemarks = im.Inward_ExpenseNature == null
                                                    ? (opex != null ? opex.Material_Remark : "Not Applicable")
                                                    : "N/A", // Adjust as needed for non-null expense nature
                                  InwardID = im.InwardID, // Added for completeness
                                  userDepartmentId = im.userDepartmentId, // Added for completeness
                                  Material_CategoryID = opex != null ? opex.Material_CategoryID : 0 // Added for completeness
                              }).AsQueryable();

            var opexData = (from im in _context.InwardMaterials
                            join opex in _context.InwardMaterial_OPEX on im.InwardID equals opex.InwardID
                            join d in _context.ServiceUserDepartments on im.userDepartmentId equals d.userDepartmentId
                            join cat in _context.Material_Category on opex.Material_CategoryID equals cat.Material_CategoryID
                            join v in _context.HTV_Vendor on im.vendorId equals v.vendorId
                            where DbFunctions.TruncateTime(im.InwardDateTime) >= DbFunctions.TruncateTime(entity.startdate) &&
                                  DbFunctions.TruncateTime(im.InwardDateTime) <= DbFunctions.TruncateTime(entity.enddate) &&
                                  im.Inward_Status != "Deleted"
                            select new InwardReportEntity
                            {
                                InwardDateTime = im.InwardDateTime,
                                Inward_ExpenseNature = im.Inward_ExpenseNature,
                                Material_Name = opex.Material_Name,
                                vendorName = v.vendorName,
                                Quantity = opex.Quantity,
                                Inward_Type = opex.Inward_Type,
                                GRN_Number = im.GRN_Number,
                                Location = im.Location,
                                userDepartmentName = d.userDepartmentName,
                                SecurityName = im.InwardEnteredBy,
                                Material_CategoryName = cat.Material_CategoryName,
                                Inward_Status = im.Inward_Status,
                                MaterialRemarks = opex.Material_Remark, // Assuming it exists in the OPEX table
                                InwardID = im.InwardID, // Added for completeness
                                userDepartmentId = im.userDepartmentId, // Added for completeness
                                Material_CategoryID = opex.Material_CategoryID // Added for completeness
                            }).AsQueryable();

            var capexData = from im in _context.InwardMaterials
                            join capex in _context.InwardMaterial_CAPEX on im.InwardID equals capex.InwardID
                            join d in _context.ServiceUserDepartments on im.userDepartmentId equals d.userDepartmentId
                            join cat in _context.Material_Category on capex.Material_CategoryID equals cat.Material_CategoryID
                            join v in _context.HTV_Vendor on im.vendorId equals v.vendorId
                            where DbFunctions.TruncateTime(im.InwardDateTime) >= DbFunctions.TruncateTime(entity.startdate) &&
                                  DbFunctions.TruncateTime(im.InwardDateTime) <= DbFunctions.TruncateTime(entity.enddate) &&
                                  im.Inward_Status != "Deleted"
                            select new InwardReportEntity
                            {
                                InwardDateTime = im.InwardDateTime,
                                Inward_ExpenseNature = im.Inward_ExpenseNature,
                                Material_Name = capex.MaterialName,
                                vendorName = v.vendorName,
                                Quantity = capex.Quantity,
                                Inward_Type = capex.Inward_Type,
                                GRN_Number = im.GRN_Number,
                                Location = im.Location,
                                userDepartmentName = d.userDepartmentName,
                                SecurityName = im.InwardEnteredBy,
                                Material_CategoryName = cat.Material_CategoryName,
                                Inward_Status = im.Inward_Status,
                                MaterialRemarks = capex.Material_Remark, // Assuming it exists in the CAPEX table
                                InwardID = im.InwardID, // Added for completeness
                                userDepartmentId = im.userDepartmentId, // Added for completeness
                                Material_CategoryID = capex.Material_CategoryID // Added for completeness
                            };

            var rentalData = from im in _context.InwardMaterials
                             join rental in _context.InwardMaterial_Rental on im.InwardID equals rental.InwardID
                             join d in _context.ServiceUserDepartments on im.userDepartmentId equals d.userDepartmentId
                             join cat in _context.Material_Category on rental.Material_CategoryID equals cat.Material_CategoryID
                             join v in _context.HTV_Vendor on im.vendorId equals v.vendorId
                             where DbFunctions.TruncateTime(im.InwardDateTime) >= DbFunctions.TruncateTime(entity.startdate) &&
                                   DbFunctions.TruncateTime(im.InwardDateTime) <= DbFunctions.TruncateTime(entity.enddate) &&
                                   im.Inward_Status != "Deleted"
                             select new InwardReportEntity
                             {
                                 InwardDateTime = im.InwardDateTime,
                                 Inward_ExpenseNature = im.Inward_ExpenseNature,
                                 Material_Name = rental.MaterialName,
                                 vendorName = v.vendorName,
                                 Quantity = rental.Quantity,
                                 Inward_Type = rental.Inward_Type,
                                 GRN_Number = im.GRN_Number,
                                 Location = im.Location,
                                 userDepartmentName = d.userDepartmentName,
                                 SecurityName = im.InwardEnteredBy,
                                 Material_CategoryName = cat.Material_CategoryName,
                                 Inward_Status = im.Inward_Status,
                                 MaterialRemarks = rental.Material_Remark, // Assuming it exists in the Rental table
                                 InwardID = im.InwardID, // Added for completeness
                                 userDepartmentId = im.userDepartmentId, // Added for completeness
                                 Material_CategoryID = rental.Material_CategoryID // Added for completeness
                             };

            var inwardlist = opexData.Union(capexData).Union(rentalData).Union(inwarddata).ToList();

            if (entity.filterByVendor == "yes")
            {
                inwardlist = inwardlist.Where(v => v.vendorName == entity.vendorId).ToList();
            }

            if (entity.filterByCategory == "yes")
            {
                inwardlist = inwardlist.Where(v => v.Material_CategoryID == entity.Material_CategoryID).ToList();
            }

            if (entity.filterByLocation == "yes")
            {
                inwardlist = inwardlist.Where(v => v.Location == entity.userLocation).ToList();
            }

            if (entity.RoleInIMS != "Inventory Incharge")
            {
                inwardlist = inwardlist.Where(v => v.Location == entity.userLocation && v.userDepartmentId == entity.userDepartmentId).ToList();
            }

            return inwardlist;


        }

        public List<GatepassReportEntity> GenerateGatepassReport(IMSEntity entity, string expenseNature, string deptName, string gatepassCategory, string gatepassType)
        {
            try
            {

                var gatepassData = (from gp in _context.GatePasses
                                    join om in _context.OutMaterials on gp.GatepassID equals om.GatepassID
                                    join m in _context.Materials on om.MaterialID equals m.MaterialID
                                    join d in _context.ServiceUserDepartments on gp.userDepartmentId equals d.userDepartmentId
                                    join s in _context.GatepassApprovers on gp.SenderID equals s.employeeId
                                    where (entity.startdate == null || DbFunctions.TruncateTime(gp.Gatepass_DateTime) >= DbFunctions.TruncateTime(entity.startdate)) &&
                                          (entity.enddate == null || DbFunctions.TruncateTime(gp.Gatepass_DateTime) <= DbFunctions.TruncateTime(entity.enddate)) &&
                                          (string.IsNullOrEmpty(gatepassCategory) || gp.GatePass_Category == gatepassCategory) &&
                                          (string.IsNullOrEmpty(gatepassType) || gp.GatepassType == gatepassType)
                                    select new GatepassReportEntity
                                    {
                                        GatepassDateTime = gp.Gatepass_DateTime,
                                        GatepassNo = gp.Gatepass_Number,
                                        Location = gp.Location,
                                        DeptName = d.userDepartmentName,
                                        GatepassType = gp.GatepassType,
                                        MaterialName = m.MaterialName,
                                        SerialNo = om.Serial_Number,
                                        ModelNo = om.Model_Number,
                                        MaterialStatus = om.Out_Status,
                                        MaterialInDate = om.Material_InDate,
                                        MaterialDispatchDate = om.MaterialDispatchDate,
                                        SenderName = gp.SenderID,
                                        ReceiverName = gp.ReceiverName,
                                        ExpectedDateOfReturn = gp.Expected_DateofReturn,
                                        GatepassCategory = gp.GatePass_Category,
                                        FromOffice = gp.From_Office,
                                        ToOffice = gp.TO_Office,
                                        GatepassStatus = gp.GatePass_Status,
                                        ApprovedBy = gp.Approved_By,
                                        userDepartmentId = gp.userDepartmentId,
                                        MaterialCategoryID = om.Material_CategoryID,
                                    }).AsQueryable();

                // Apply additional filters if needed
                if (entity.filterByVendor == "yes")
                {
                    gatepassData = gatepassData.Where(gp => gp.ReceiverName == entity.vendorId); // Assuming ReceiverName corresponds to vendorId
                }

                if (entity.filterByCategory == "yes")
                {
                    gatepassData = gatepassData.Where(gp => gp.MaterialCategoryID == entity.Material_CategoryID.ToString());
                }


                if (entity.filterByLocation == "yes")
                {
                    gatepassData = gatepassData.Where(gp => gp.Location == entity.userLocation); // Filtering by user location
                }

                if (entity.RoleInIMS != "Inventory Incharge")
                {
                    // gatepassData = gatepassData.Where(gp => gp.Location == entity.userLocation && gp.userDepartmentId == entity.userDepartmentId); // Ensure location and department match
                }
                if (entity.filterByDepartment == "yes")
                {
                    gatepassData = gatepassData.Where(x => x.userDepartmentId == entity.userDepartmentId);
                }

                return gatepassData.ToList();
            }
            catch (Exception ex)
            {

                return new List<GatepassReportEntity>(); ;
            }
        }
        public List<OutwardMaterialReportEntity> GenerateOutWardReport(IMSEntity entity)
        {
            var outwardReportData = (from om in _context.OutMaterials
                                     join m in _context.Materials on om.MaterialID equals m.MaterialID
                                     join g in _context.GatePasses on om.GatepassID equals g.GatepassID
                                     join d in _context.ServiceUserDepartments on g.userDepartmentId equals d.userDepartmentId
                                     where DbFunctions.TruncateTime(om.MaterialDispatchDate) >= DbFunctions.TruncateTime(entity.startdate) &&
                                           DbFunctions.TruncateTime(om.MaterialDispatchDate) <= DbFunctions.TruncateTime(entity.enddate) &&
                                           om.Out_Status != "Deleted"
                                     select new OutwardMaterialReportEntity
                                     {
                                         MaterialName = m.MaterialName,
                                         SerialNo = om.Serial_Number,
                                         ModelNo = om.Model_Number,
                                         MaterialStatus = om.Out_Status,
                                         MaterialInDate = g.Expected_DateofReturn,
                                         MaterialDispatchDate = om.MaterialDispatchDate,
                                         Location = g.Location,
                                         UserDepartmentName = d.userDepartmentName
                                     }).AsQueryable();

            // Apply filters if needed based on the entity
            if (entity.filterByLocation == "yes")
            {
                outwardReportData = outwardReportData.Where(o => o.Location == entity.userLocation);
            }

            if (entity.filterByDepartment == "yes")
            {
                outwardReportData = outwardReportData.Where(o => o.UserDepartmentName == entity.userDepartmentName);
            }
            if (entity.filterByCategory == "yes")
            {
                outwardReportData = outwardReportData.Where(g => g.MaterialName == entity.Material_CategoryID.ToString());
            }

            return outwardReportData.ToList();
        }
        public List<InventoryRegisterEntity> GenerateInventoryRegisterReport(IMSEntity entity, string expenseNature, string deptName)

        {

            try

            {

                // Query to join relevant tables and apply necessary filters

                var inventoryRegisterData = (from ir in _context.Inventory_Register

                                             join m in _context.Materials on ir.MaterialID equals m.MaterialID

                                             join v in _context.HTV_Vendor on ir.vendorId equals v.vendorId

                                             join d in _context.ServiceUserDepartments on ir.userDepartmentId equals d.userDepartmentId

                                             where (entity.startdate == null || DbFunctions.TruncateTime(ir.AssetGenerateDate) >= DbFunctions.TruncateTime(entity.startdate)) &&

                                                   (entity.enddate == null || DbFunctions.TruncateTime(ir.AssetGenerateDate) <= DbFunctions.TruncateTime(entity.enddate)) &&

                                                   (string.IsNullOrEmpty(expenseNature) || ir.ExpenseNature == expenseNature) &&

                                                   (string.IsNullOrEmpty(deptName) || d.userDepartmentName == deptName)

                                             select new InventoryRegisterEntity

                                             {

                                                 AssetGenerateDate = ir.AssetGenerateDate,

                                                 GRNNumber = ir.GRN_Number,

                                                 AssetID = ir.AssetID,

                                                 ExpenseNature = ir.ExpenseNature,

                                                 VendorName = v.vendorName,

                                                 MaterialCategoryName = ir.MaterialName,

                                                 MaterialName = m.MaterialName,

                                                 Quantity = ir.Quantity,

                                                 PurchaseRate = ir.Purchase_Rate,

                                                 Make = ir.Make,

                                                 SerialNumber = ir.Serial_No,

                                                 ModelNumber = ir.Model_Number,

                                                 MachineID = ir.MachineID,

                                                 RAM = ir.RAM,

                                                 HDD = ir.HDD,

                                                 OS = ir.OS,

                                                 IRStatus = ir.IR_Status,

                                                 Location = ir.Location,

                                                 DepartmentName = d.userDepartmentName,

                                             }).AsQueryable();

                // Apply additional filters if needed (e.g., department, location, etc.)

                if (entity.filterByLocation == "yes")

                {

                    inventoryRegisterData = inventoryRegisterData.Where(ir => ir.Location == entity.userLocation); // Filtering by user location

                }

                if (entity.filterByDepartment == "yes")

                {

                    inventoryRegisterData = inventoryRegisterData.Where(ir => ir.DepartmentName == deptName); // Filtering by department

                }

                return inventoryRegisterData.ToList();

            }

            catch (Exception ex)

            {

                // Handle the exception gracefully and return an empty list in case of an error

                return new List<InventoryRegisterEntity>();

            }

        }
    }
}
