 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IMS.DAL.Infrastructure;
using IMS.Entities;

namespace IMS.DAL
{
    internal class DepartmentRepository : RepositoryBase<ServiceVMSEntities>
    {
        public DepartmentRepository(ServiceVMSEntities context) : base(context)
        {


        }
    }
}
