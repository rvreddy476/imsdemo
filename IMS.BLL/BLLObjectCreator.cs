using IMS.BLL.Implementation.ExceptionLogger;
using IMS.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.BLL
{
    public static class BLLObjectCreator
    {
          public static IMExceptionLogger CreateIMSLogger(ExceptionLoggerType logger = ExceptionLoggerType.IMSText)
         {
            switch (logger)
            {
                case ExceptionLoggerType.IMSText:
                    return new IMSTextExceptionLogger();
                case ExceptionLoggerType.Database:
                    return new DatabaseExceptionLogger();
                default:
                    return new IMSTextExceptionLogger();
            }
        }
    }
}
