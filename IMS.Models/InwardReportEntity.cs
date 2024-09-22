using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Models
{
    public class InwardReportEntity
    {
        public DateTime? InwardDateTime { get; set; }
        public string GRN_Number { get; set; }
        public string Inward_ExpenseNature { get; set; }
        public string vendorName { get; set; }
        public string Material_CategoryName { get; set; }
        public string Material_Name { get; set; }
        public int? Quantity { get; set; }
        public string Inward_Type { get; set; }
        public string userDepartmentName { get; set; }
        public string Location { get; set; }
        public string Inward_Status { get; set; }
        public string SecurityName { get; set; }
        public string Status { get; set; }
        public string MaterialRemarks { get; set; }

        public string InwardID { get; set; }
        public int userDepartmentId { get; set; }
        public int? Material_CategoryID { get; set; }
    }
}
