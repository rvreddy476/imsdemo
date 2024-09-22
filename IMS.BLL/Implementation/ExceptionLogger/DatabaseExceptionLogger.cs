using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IMS.BLL.Interfaces;
using IMS.DAL.Infrastructure;
using IMS.Entities;
using IMS.Models;
using AutoMapper;
using IMS.DAL;

namespace IMS.BLL
{
    public class DatabaseExceptionLogger : IMExceptionLogger
    {
        private UnitOfWork unitOfWork = new UnitOfWork();

        public string LogCreationForCategoryFeature(Material_Category model, string data, Material_Category original, string category_name)
        {
            throw new NotImplementedException();
        }

        public string LogCreationForInward(IMSEntity model, string data)
        {
            throw new NotImplementedException();
        }

        public string LogCreationForInwardEdit(string model, string data)
        {
            throw new NotImplementedException();
        }

        public string LogCreationForInwardStatus(string id, string data, string materialID)
        {
            throw new NotImplementedException();
        }

        public string LogCreationForUser(IMS_Users userTableDto, string data)
        {
            throw new NotImplementedException();
        }

        public void LogException(ExceptionModel exceptionDetails)
        {
            var entity = Mapper.Map<ExceptionModel, IMSExceptionLogger>(exceptionDetails);

            unitOfWork.ExceptionLoggerRepository.Insert(entity);
            unitOfWork.Save();
            unitOfWork.Dispose();
        }

        string IMExceptionLogger.LogCreationForCategoryFeature(Material_Category model, string data, Material_Category original, string category_name)
        {
            throw new NotImplementedException();
        }

        string IMExceptionLogger.LogCreationForInward(string id, string data,IMSEntity model,IMSEntity iMSEntity)
        {
            throw new NotImplementedException();
        }

        string IMExceptionLogger.LogCreationForInwardEdit(string model, string data)
        {
            throw new NotImplementedException();
        }

        string IMExceptionLogger.LogCreationForInwardStatus(string id, string data, string da)
        {
            throw new NotImplementedException();
        }
        string LogCreationForUserCreation(IMSEntity model, string data, IMSEntity original)
        {
            throw new NotImplementedException();
        }

        string IMExceptionLogger.LogCreationForMaterialFeature(Material model, string data, Material original, string mat_name)
        {
            throw new NotImplementedException();
        }

        string IMExceptionLogger.LogCreationForUser(IMS_Users userTableDto, string data)
        {
            throw new NotImplementedException();
        }
        void IMExceptionLogger.LogException(ExceptionModel exceptionDetails)
        {
            throw new NotImplementedException();
        }

        string IMExceptionLogger.LogCreationForUserCreation(IMSEntity model, string data, IMSEntity original)
        {
            throw new NotImplementedException();
        }

        string IMExceptionLogger.LogCreationForGatepass(string id, string data, IMSEntity model, IMSEntity editdata)
        {
            throw new NotImplementedException();
        }

        string IMExceptionLogger.LogCreationForGatepassStatus(string id, string data)
        {
            throw new NotImplementedException();
        }
    }
}
