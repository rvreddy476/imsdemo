using IMS.DAL.Infrastructure;
using IMS.Entities;

namespace IMS.DAL
{
    internal class ExceptionLoggerRepository : RepositoryBase<ExceptionLogger>
    {
        public ExceptionLoggerRepository(ServiceVMSEntities context) : base(context)
        {
        }
    }
}
