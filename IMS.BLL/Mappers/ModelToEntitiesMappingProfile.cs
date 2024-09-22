using AutoMapper;
using IMS.Entities;
using IMS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.BLL.Mappers
{
   
        internal class ModelToEntitiesMappingProfile : Profile
        {
            public ModelToEntitiesMappingProfile()
            {
                CreateMap<ExceptionModel, ExceptionLogger>();
               
            }
        }
}
