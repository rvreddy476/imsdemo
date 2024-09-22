using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Models
{
    public class OutwardMaterialReportEntity
    {
        public string MaterialName { get; set; }
        public string SerialNo { get; set; }
        public string ModelNo { get; set; }
        public string MaterialStatus { get; set; }
        public DateTime? MaterialInDate { get; set; }
        public DateTime? MaterialDispatchDate { get; set; }
        public string Location { get; set; }
        public string UserDepartmentName { get; set; }
    }
}
