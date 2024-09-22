using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Models
{
    public class GatepassReportEntity
    {
        public DateTime? GatepassDateTime { get; set; }
        public string GatepassNo { get; set; }
        public string Location { get; set; }
        public string DeptName { get; set; }
        public string GatepassType { get; set; }
        public string MaterialName { get; set; }
        public string SerialNo { get; set; }
        public string ModelNo { get; set; }
        public string MaterialStatus { get; set; }
        public DateTime? MaterialInDate { get; set; }
        public DateTime? MaterialDispatchDate { get; set; }
        public string SenderName { get; set; }
        public string ReceiverName { get; set; }
        public DateTime? ExpectedDateOfReturn { get; set; }
        public string GatepassCategory { get; set; }
        public string FromOffice { get; set; }
        public string ToOffice { get; set; }
        public string GatepassStatus { get; set; }
        public string ApprovedBy { get; set; }
        public string DispatchBy { get; set; }
        public int? userDepartmentId { get; set; }
        public string MaterialCategoryID { get; set; }
    }
}
