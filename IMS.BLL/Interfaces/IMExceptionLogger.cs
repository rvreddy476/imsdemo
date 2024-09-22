using IMS.Entities;
using IMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.BLL.Interfaces
{
   public interface IMExceptionLogger
    {
        void LogException(ExceptionModel exceptionDetails);

        string LogCreationForUser(IMS_Users userTableDto, string data);        
        string LogCreationForInward(string id, string data,IMSEntity model,IMSEntity editdata);
        string LogCreationForInwardStatus(string id, string data, string materialID);
        string LogCreationForInwardEdit(string model, string data);
        string LogCreationForCategoryFeature(Material_Category model, string data, Material_Category original, string category_name);

        string LogCreationForUserCreation(IMSEntity model, string data, IMSEntity original);
        string LogCreationForMaterialFeature(Material model, string data, Material original, string mat_name);
        string LogCreationForGatepass(string id, string data, IMSEntity model, IMSEntity editdata);
        string LogCreationForGatepassStatus(string id, string data);
    }
}
