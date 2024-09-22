using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IMS.Models;

namespace IMS.BLL.Interfaces
{
    public interface IMSReportGenerator
    {
        List<InwardReportEntity> ReportGenerationonInward(IMSEntity entity);
        List<GatepassReportEntity> GenerateGatepassReport(IMSEntity entity, string expenseNature, string deptName, string gatepassCategory, string gatepassType);
        List<OutwardMaterialReportEntity> GenerateOutWardReport(IMSEntity entity);
        List<InventoryRegisterEntity> GenerateInventoryRegisterReport(IMSEntity entity, string expenseNature, string deptName);

    }
}
