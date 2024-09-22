namespace IMS.Models
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Web;

    public partial class IMSEntity
    {

        public IMSEntity()
        {

        }

        public string userId { get; set; }
        public string TempVendorID { get; set; }


        public string IMSRole_Id { get; set; }
        public string userEmail { get; set; }

        public string userName { get; set; }
        public string RoleInIMS { get; set; }
        public string userLocation { get; set; }

     

        [Required]
        [Display(Name = "User Department")]
        public int userDepartmentId { get; set; }

        [Required]
        [Display(Name = "User Department")]
        public string userDepartmentName { get; set; }


        [Required]
        [Display(Name = "Materials")]
        public string MaterialID { get; set; }

        [Required]
        [Display(Name = "Material Name")]
        public string MaterialName { get; set; }

        public string userphone { get; set; }
        public string MaterialPrefix { get; set; }

        //Inward
        public string InwardID { get; set; }

        [Required]
        [Display(Name = "BillofEntry")]
        public string BillofEntry { get; set; }

        [Required]
        [Display(Name = "Vendor Name")]
        public string vendorId { get; set; }

        [Required]
        [Display(Name = "Vendor Name")]
        public string vendorName { get; set; }
        public string GRN_Number { get; set; }
        public Nullable<System.DateTime> InwardDateTime { get; set; }
        [Required(ErrorMessage = "Please enter Inward Note.")]
        [Display(Name = "Inward Note")]
        public string InwardNote { get; set; }
        [Required]
        [Display(Name = "Inward ExpenseNature")]
        public string Inward_ExpenseNature { get; set; }

        public string Inward_Status { get; set; }

        public string Material_Remark { get; set; }
        public string Location { get; set; }
        public string InwardEnteredBy { get; set; }

        public string InvMaterialOP_ID { get; set; }

        public string OutwardNo { get; set; }
        public Nullable<int> Quantity { get; set; }
        public string Inward_Type { get; set; }
        public string InvOP_Status { get; set; }
        public string Acceptedby { get; set; }

        public string InvMaterialCP_ID { get; set; }

        public string Status { get; set; }

        [Display(Name = "MaterialCategory Name")]
        public int Material_CategoryID { get; set; }

        public int? CategoryID { get; set; }
        [Display(Name = "MaterialCategory Name" )]
        public string Material_CategoryName { get; set; }

        public int SerialNumber { get; set; }

        public int[] srno { get; set; }
        public string[] material { get; set; }

        public string[] mcategory { get; set; }
        public int[] m_quantity { get; set; }
        public string[] invtype { get; set; }

        public string[] materialremarks { get; set; }

        public string[] matremark { get; set; }

        public string[] materialcatlist { get; set; }
        public string[] InvTemp_ID { get; set; }

        public string[] serial_number { get; set; }

        public string[] mid { get; set; }
        public string[] mcatid { get; set; }

        public string DC_ID { get; set; }

        [Required]
        [Display(Name = "DeliveryChallen Number")]

        public string DC_Number { get; set; }

        [Required]
        [Display(Name = "Filename")]
        public HttpPostedFileBase DC_Filename { get; set; }

        public string DC_Filename_EXI { get; set; }



        public string DC_Filesize_EXI { get; set; }
        public string DC_Filesize { get; set; }

        public string DC_ID1 { get; set; }

        public string DC_ID2 { get; set; }

        public string DC_ID3 { get; set; }
        [Required]
        [Display(Name = "DeliveryChallen Number")]
        public string DC_Number1 { get; set; }

        [Required]
        [Display(Name = "DeliveryChallen Number")]
        public string DC_Number2 { get; set; }

        [Required]
        [Display(Name = "DeliveryChallen Number")]
        public string DC_Number3 { get; set; }
        [Required]
        [Display(Name = "Filename")]
        public string DC_Filename1 { get; set; }
        public string DC_Filename2 { get; set; }
        public string DC_Filename3 { get; set; }
        public string DC_Filesize1 { get; set; }
        public string DC_Filesize2 { get; set; }
        public string DC_Filesize3 { get; set; }
        public string Serial_GRN_Number { get; set; }

        public string[] invcpid { get; set; }

        public string[] invrentalid { get; set; }
        public string[] modelno { get; set; }

        public string[] inward_id { get; set; }

        public double[] rate { get; set; }

        public double[] rentalcost { get; set; }
        public string[] serialno { get; set; }
        public string[] make { get; set; }

        public string InvMaterialTemp_ID { get; set; }

        public string GatepassID { get; set; }

        public string GatepassType { get; set; }
        public Nullable<bool> STP_Bonded_Item { get; set; }
        public Nullable<bool> SEZ_Bonded_Item { get; set; }
        public string SenderID { get; set; }
        public string Reason { get; set; }
        public string Gatepass_Number { get; set; }
        public string ReceiverID { get; set; }
        public string ReceiverName { get; set; }
        public string ReceiverAddress { get; set; }
        public string ReceiverContact { get; set; }
        public string ReceiverEmail { get; set; }
        public Nullable<System.DateTime> Gatepass_DateTime { get; set; }
        public DateTime? Expected_DateofReturn { get; set; }
        public string TO_Office { get; set; }
        public string From_Office { get; set; }
        public string GatePass_Category { get; set; }
        public string GatePass_Status { get; set; }
        public string Serial_Gatepass_Number { get; set; }

        public string Serial_Asset_Number { get; set; }

        public string Serial_Outward_Number { get; set; }

        public string finalnumberforgatepass { get; set; }

        public string InventoryReg_ID { get; set; }
        public string AssetID { get; set; }

        public string IR_Status { get; set; }
        public Nullable<double> Purchase_Rate { get; set; }

        [Display(Name = "Rental Cost")]
        public Nullable<double> Asset_Cost { get; set; }

        public string Makenm { get; set; }
        public string Serial_No { get; set; }

        public string Invoice_Number { get; set; }

        public string Alloted_To { get; set; }
        public string Bonded_Item { get; set; }

        public Nullable<System.DateTime> Allot_date { get; set; }

        public string material_cp_rental_ID { get; set; }
        public string materialtext { get; set; }
        public string Inward_CP_Rental_ID { get; set; }
        public string ExpenseNature { get; set; }
        public Nullable<System.DateTime> AssetGenerateDate { get; set; }
        public string MachineID { get; set; }
        public string RAM { get; set; }
        public string HDD { get; set; }
        public string OS { get; set; }


        public string Model_Number { get; set; }
        public string[] inventoryregid { get; set; }

        public string[] businessnature { get; set; }

        public string Created_By { get; set; }

        public string SenderName { get; set; }

        public string FileName { get; set; }

        [DisplayName("Month")]
        public string monthly { get; set; }

        [DisplayName("Quarter")]
        public string quarterly { get; set; }


        [DisplayName("Year")]
        public string yearly { get; set; }
        [DisplayName("Financial Year")]
        public string financialyearly { get; set; }

        public DateTime startdate { get; set; }
        public DateTime enddate { get; set; }

        public string filterByVendor { get; set; }
        public string filterByCategory { get; set; }
        public string filterByLocation { get; set; }
        public string filterByDepartment { get; set; }
        [DataType(DataType.Date)]
        [DisplayName("End Date")]
        public string inwardenddate { get; set; }

        [DataType(DataType.Date)]
        [DisplayName("Start Date")]
        public string inwardstartdate { get; set; }

        [Required]
        [Display(Name = "signaturePath")]
        public string signaturePath { get; set; }
        public string rejectGatepassId { get; set; }
        public string Gatepass_DispatchBy { get; set; }

        public string GatepassRejectReason { get; set; }
        public string dummyFilename { get; set; }
        public long dummyFilesize { get; set; }
        public string OW_MaterialID { get; set; }       
        public string OutwardNote { get; set; }
        public Nullable<System.DateTime> OutwardDatetime { get; set; }
        public string Outward_Status { get; set; }

        public int? ims_userDepartmentId { get; set; }

    }
    public partial class MyJsonObject
    {
        public string Assetid { get; set; }
        public int FinalNumber { get; set; }
        public string year_range { get; set; }
    }

    public partial class OidcOptions
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Region { get; set; }
        public string AccessToken { get; set; }
    }

    public class LoginViewModel
    {
        public int Id { get; set; }
        public string userEmail { get; set; }
        public string userPassword { get; set; }
    }

    public class MaterialList
    {
        public string MaterialName { get; set; }
        public Nullable<int> Quantity { get; set; }
        public string Material_Remark { get; set; }
        public string Inward_Type { get; set; }

    }

    public class OutwardDetailViewModel

    {

        public IMSEntity OutwardDetail { get; set; }

        public List<MaterialItem> MaterialItems { get; set; }

    }

    public class MaterialItem

    {

        public string AssetId { get; set; }

        public string ExpenseNature { get; set; }

        public string MaterialName { get; set; }

        public string ModelNo { get; set; }

        public string SerialNo { get; set; }

        public string GRN_Number { get; set; }

        public string OutStatus { get; set; }
        public string GatePassID { get; set; }
        public string OutMat_ID { get; set; }



    }


}
