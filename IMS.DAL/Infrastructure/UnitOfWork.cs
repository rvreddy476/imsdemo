using IMS.DAL.Infrastructure;
using IMS.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.DAL
{
    public class UnitOfWork : IDisposable
    {
        // Declare member variable for any newly added repo
        private ServiceVMSEntities context = new ServiceVMSEntities();
        private DepartmentRepository departmentRepository;
        private ExceptionLoggerRepository exceptionRepository;
        private UserRepository userRepository;



        // Create getter for any newly added repo

        public RepositoryBase<User> UserRepository
        {
            get
            {
                if (this.userRepository == null)
                {
                    this.userRepository = new UserRepository(context);
                }
                return userRepository;
            }
        }
        public bool Login(string userEmail, string userPassword)
        {
            bool isValid = context.IMSUsers.Any(x => x.userEmail.Equals(userEmail, StringComparison.CurrentCultureIgnoreCase));
            return (isValid);
        }
        public RepositoryBase<ServiceVMSEntities> DepartmentRepository
        {
            get
            {
                if (this.departmentRepository == null)
                {
                    this.departmentRepository = new DepartmentRepository(context);
                }
                return departmentRepository;
            }
        }

        public RepositoryBase<ExceptionLogger> ExceptionLoggerRepository
        {
            get
            {
                if (this.exceptionRepository == null)
                {
                    this.exceptionRepository = new ExceptionLoggerRepository(context);
                }
                return exceptionRepository;
            }
        }

        public void Save()
        {
            context.SaveChanges();
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    context.Dispose();
                }
            }
            this.disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
