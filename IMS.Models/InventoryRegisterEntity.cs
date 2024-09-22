using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Models
{
    public class InventoryRegisterEntity
    {
        public DateTime? AssetGenerateDate { get; set; }
        public string GRNNumber { get; set; }
        public string AssetID { get; set; }
        public string ExpenseNature { get; set; }
        public string VendorName { get; set; }
        public string MaterialCategoryName { get; set; }
        public string MaterialName { get; set; }
        public int? Quantity { get; set; }
        public double? PurchaseRate { get; set; }
        public string Make { get; set; }
        public string SerialNumber { get; set; }
        public string ModelNumber { get; set; }
        public string MachineID { get; set; }
        public string RAM { get; set; }
        public string HDD { get; set; }
        public string OS { get; set; }
        public string IRStatus { get; set; }
        public string Location { get; set; }
        public string DepartmentName { get; set; }
    }
}
