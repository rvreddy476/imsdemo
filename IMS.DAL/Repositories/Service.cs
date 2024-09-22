using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IMS.DAL.Infrastructure;
using IMS.Entities;

namespace VMS.DAL.Repositories
{
    internal class Service : RepositoryBase<Service>
    {
        public Service(ServiceVMSEntities context) : base(context)
        {

        }
    }
}
