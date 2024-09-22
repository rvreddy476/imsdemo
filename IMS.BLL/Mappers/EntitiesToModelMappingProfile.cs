using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using IMS.Entities;
using IMS.Models;

namespace IMS.BLL.Mappers
{
    internal class EntitiesToModelMappingProfile : Profile
    {
        public EntitiesToModelMappingProfile()
        {
            CreateMap<ExceptionLogger, ExceptionModel>();
        }
    }
}
