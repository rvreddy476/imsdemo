using IMS.DAL.Infrastructure;
using IMS.Entities;
using System.Linq;


namespace IMS.DAL
{
    internal class UserRepository : RepositoryBase<User>
    {
        public UserRepository(ServiceVMSEntities context) : base(context)
        {

          
        }



        }
}
