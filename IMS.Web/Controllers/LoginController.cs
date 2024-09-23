using IMS.BLL;
using IMS.BLL.Implementation.ExceptionLogger;
using IMS.BLL.Interfaces;
using IMS.DAL;
using IMS.Entities;
using IMS.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.DirectoryServices.Protocols;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI.WebControls;
using System.Xml;

namespace IMS.Web.Controllers
{
    [AllowAnonymous]
    public class LoginController : Controller
    {
        // GET: Login
        private UnitOfWork unitOfWork = new UnitOfWork();
        private ServiceVMSEntities context = new ServiceVMSEntities();
        private static int logincount = 0;
        private static DateTime? loginoldtime;
        private static DateTime? logindateafter20mins;
        private static int loginAttempt = 0;
        IMSExceptionLoggerController logger = new IMSExceptionLoggerController();
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Login()
        {

            ViewBag.designation = unitOfWork.DepartmentRepository.GetDesignation();
            Session.Abandon();
            Session.Clear();
            Session.RemoveAll();
            return View();
        }


        [HttpPost]
        public ActionResult Login(IMS_Users _userTableDto, string user_department)
        {
            try
            {
                var jsonResult = string.Empty;
                string result = "";

                ServiceVMSEntities context = new ServiceVMSEntities();
                var context1 = HttpContext.Response.Cookies;
                var user = unitOfWork.Login(_userTableDto.userEmail, _userTableDto.userPassword);              
                bool isValidUser = context.IMSUsers.Any(x => x.userEmail == _userTableDto.userEmail && x.userPassword == _userTableDto.userPassword);


                if (user)
                {
                    var dept = int.Parse(_userTableDto.userDepartmentId.ToString());
                    var des = _userTableDto.RoleInIMS;
                    var user_data = (from i in context.IMSUsers
                                     join r in context.IMSRoles on i.userId equals r.IMSuser_Id
                                     where i.userEmail == _userTableDto.userEmail && r.userDepartmentId == dept && r.userLocation == _userTableDto.userLocation
                                     select new { i.userId, i.userName, i.userEmail, r.RoleInIMS, r.userDepartmentId, r.userLocation }).FirstOrDefault();


                    Session["Email"] = user_data.userEmail;
                    Session["UserName"] = user_data.userName;
                    Session["UserID"] = user_data.userId;
                    Session["DepartmentID"] = user_data.userDepartmentId;
                    Session["role"] = user_data.RoleInIMS;
                    Session["Location"] = user_data.userLocation;

                    bool isblock = InvalidLoginAttempts(_userTableDto.userEmail, true);
                    if (!isblock)
                    {
                        jsonResult = JsonConvert.SerializeObject(result);
                        return RedirectToAction("InwardList", "Add_InwardMaterial");
                    }
                    else
                    {
                        var Result = new { ErrorMsg = "Sorry! your account is blocked for next 20 mins", data = "11" };
                        jsonResult = JsonConvert.SerializeObject(Result);
                    }
                    return Json(jsonResult, JsonRequestBehavior.AllowGet);

                }
                else
                {
                    bool isblock = InvalidLoginAttempts(_userTableDto.userEmail, false);
                    if (!isblock)
                    {
                        var Result = new { ErrorMsg = "Invalid Password.", data = "11" };
                        jsonResult = JsonConvert.SerializeObject(Result);
                    }
                    else
                    {
                        var Result = new { ErrorMsg = "Sorry! your account is blocked for next 20 mins", data = "11" };
                        jsonResult = JsonConvert.SerializeObject(Result);
                    }
                    return Json(jsonResult, JsonRequestBehavior.AllowGet);
                }

                
            }
            catch (Exception E1)
            {
                TempData["Msg"] = "Login Failed  " + E1.Message;
                Console.WriteLine(E1);

                var exceptionLogger = new IMSExceptionLogger()
                {
                    controllerName = "LoginController",
                    actionName = "Login",
                    exceptionStackTrace = E1.StackTrace,
                    exceptionMessage = E1.Message,
                    exceptionLogTime = DateTime.Now

                };

                unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                unitOfWork.Save();

                return RedirectToAction("Login", "Login");
            }
        }
        [HttpPost]
        public ActionResult IMSLogin(string userEmail, string userPassword)
        {
            string jsonResult = string.Empty;
            bool isLDAP = false;
            bool isDeleted = context.IMSUsers.Any(x => x.userEmail.Equals(userEmail, StringComparison.InvariantCulture) && x.userStatus == "Deleted");
            logger.LogCreationForExceptions("isDeleted" + isDeleted);
            if (isDeleted == false)
            {
                //isLDAP = LDAP_Connection(userEmail, userPassword);
                logger.LogCreationForExceptions("isLDAP" + isLDAP);
            }
            try
            {
                bool isValidUser = context.IMSUsers.Any(x => x.userEmail == userEmail && x.userPassword == userPassword);

                if (isLDAP) //available in LDAP
                {
                    logger.LogCreationForExceptions("isLDAP" + isLDAP);
                    jsonResult = JsonConvert.SerializeObject("10");
                    if (isValidUser == true)
                    {
                        logger.LogCreationForExceptions("isvalid_UserLDAP" + isValidUser);
                        var Result = new { data = "10" };
                        logger.LogCreationForExceptions("Result" + Result);
                        jsonResult = JsonConvert.SerializeObject(Result);
                        return Json(jsonResult, JsonRequestBehavior.AllowGet);
                    }

                }
                else if (isValidUser == true)//available in DB
                {
                    logger.LogCreationForExceptions("isValidUser" + isValidUser);
                    bool isvalid_User = context.IMSUsers.Any(x => x.userEmail == userEmail && x.userPassword == userPassword);
                    if (isvalid_User)
                    {
                        var validDH = context.IMSUsers.Where(x => x.userEmail == userEmail && x.userPassword == userPassword).Select(x => x);

                        bool isblock = InvalidLoginAttempts(userEmail, true);
                        if (!isblock)
                        {
                            var Result = new { data = "10" };
                            logger.LogCreationForExceptions("Result" + Result);
                            jsonResult = JsonConvert.SerializeObject(Result);
                        }
                        else
                        {
                            var Result = new { ErrorMsg = "Sorry! your account is blocked for next 20 mins", data = "11" };
                            jsonResult = JsonConvert.SerializeObject(Result);
                        }
                        return Json(jsonResult, JsonRequestBehavior.AllowGet);

                    }
                    else
                    {
                        bool isblock = InvalidLoginAttempts(userEmail, true);
                        if (!isblock)
                        {
                            var Result = new { ErrorMsg = "Invalid Password.", data = "11" };
                            jsonResult = JsonConvert.SerializeObject(Result);
                        }
                        else
                        {
                            var Result = new { ErrorMsg = "Sorry! your account is blocked for next 20 mins", data = "11" };
                            jsonResult = JsonConvert.SerializeObject(Result);
                        }

                    }
                }
                else
                {

                    var Result = new { ErrorMsg = "Invalid Email or Password !!", data = "11" };
                    logger.LogCreationForExceptions("result---" + Result);
                    jsonResult = JsonConvert.SerializeObject(Result);
                }
            }
            catch (Exception e)
            {

                if (e != null)
                {
                    string data = "IMSlogin :" + e.Message + " : " + e.StackTrace;
                    logger.LogCreationForExceptions(data);
                }
                else
                {
                    bool isValidUser = context.IMSUsers.Any(x => x.userEmail.Equals(userEmail, StringComparison.InvariantCulture) && x.userPassword.Equals(userPassword));
                    if (isValidUser == true)
                    {
                        var Result = new { data = "10" };
                        jsonResult = JsonConvert.SerializeObject(Result);
                    }
                    else
                    {
                        var Result = new { data = "11" };
                        jsonResult = JsonConvert.SerializeObject(Result);
                    }
                }
            }
            logger.LogCreationForExceptions("jsonresult" + jsonResult);
            return Json(jsonResult, JsonRequestBehavior.AllowGet);
        
        }


        public ActionResult SignOut()
        {
            try
            {
                FormsAuthentication.SignOut();
                IMExceptionLogger exceptionLogger = BLLObjectCreator.CreateIMSLogger(ExceptionLoggerType.Text);
                exceptionLogger.LogCreationForUser(null, "Logged Out");
                Session.Abandon();
                Session.Clear();
                Session.RemoveAll();

            }
            catch (Exception e)
            {
                Console.Write(e);

                var exceptionLogger = new IMSExceptionLogger()
                {
                    controllerName = "LoginController",
                    actionName = "Signout",
                    exceptionStackTrace = e.StackTrace,
                    exceptionMessage = e.Message,
                    exceptionLogTime = DateTime.Now

                };

                unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                unitOfWork.Save();
                //return View();
            }
            return RedirectToAction("Login", "Login");
        }

        [HttpPost]
        public string ForgotPassword(IMSEntity model)
        {
            string empid = string.Empty;
            string id = model.userEmail;
            int count = 0;
            try
            {

                using (var writer = new StreamWriter(@"D:\IMSDocs\logfile.txt"))
                {

                    MailMessage mail = new MailMessage();
                    string username = ConfigurationManager.AppSettings["SmtpUserId"].ToString();
                    mail.To.Add(id);
                    mail.From = new MailAddress(username);
                    mail.Subject = "IMS Password Reset";

                    var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                    writer.WriteLine(chars);
                    var stringChars = new char[8];
                    var random = new Random();

                    for (int i = 0; i < stringChars.Length; i++)
                    {
                        stringChars[i] = chars[random.Next(chars.Length)];
                    }

                    var finalString = new String(stringChars);

                    string Body = finalString;
                    string Data = "Hi," +
                        "<br>" +
                                    "A request has been made to reset your IMS account password. " + "<br><br>" + "Please find your new password below." +
                                 "<br><br>" +
                                    "Password : " + "<b>" + Body + "</b>" +
                                    "<br><br>" +
                                     "Do not share this password with anyone." +
                                     "<br><br>" +
                                    "You can change your password as per your convenience by selecting the ‘’Change Password’’ option.";
                    mail.Body = Data;
                    mail.IsBodyHtml = true;
                    SmtpClient smtp = new SmtpClient();
                    smtp.Host = ConfigurationManager.AppSettings["SmtpServerHost"].ToString();
                    smtp.Port = Convert.ToInt32(ConfigurationManager.AppSettings["SmtpServerPort"]);
                    smtp.UseDefaultCredentials = false;
                    smtp.EnableSsl = true;

                    string password = ConfigurationManager.AppSettings["SmtpUserPassword"].ToString();

                    smtp.Credentials = new System.Net.NetworkCredential(username, password);
                    smtp.Send(mail);
                    count = 1;


                    ServiceVMSEntities context = new ServiceVMSEntities();


                    var vendorid = (from t in context.HTV_Vendor
                                    where t.vendorEmail == model.userEmail
                                    select new { t.vendorId });
                    foreach (var q in vendorid)
                    {
                        empid = q.vendorId;
                    }



                    if (empid != string.Empty)
                    {

                        var vendorDetails = (from t in context.HTV_Vendor
                                             where t.vendorEmail == model.userEmail
                                             select t).SingleOrDefault();
                        vendorDetails.vendorPassword = finalString;

                        context.SaveChanges();

                    }
                    else
                    {
                        var UserDetails = (from t in context.IMSUsers
                                           where t.userEmail == model.userEmail
                                           select t).SingleOrDefault();
                        UserDetails.userPassword = finalString;

                        context.SaveChanges();
                    }



                    // TempData["message"] = "Password sent to your email address";
                }

            }
            catch (Exception e)
            {
                Console.Write(e);

                var exceptionLogger = new IMSExceptionLogger()
                {
                    controllerName = "LoginController",
                    actionName = "ForgotPassword",
                    exceptionStackTrace = e.StackTrace,
                    exceptionMessage = e.Message,
                    exceptionLogTime = DateTime.Now

                };

                unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                unitOfWork.Save();
                //return View();
            }


            var sprintJson = "{\"count\":" + count + "}";
            return sprintJson;
        }

        [HttpGet]

        public JsonResult GetDesignationdata(string Email)
        {
            ServiceVMSEntities context = new ServiceVMSEntities();
            var jsonResult = string.Empty;

            var DesignationList = unitOfWork.DepartmentRepository.GetDesignation();

            var result = from a in DesignationList select new { a.RoleInIMS, a.userEmail, a.userDepartmentId, a.userDepartmentName };//employeeId = a.employeeId, userName = a.userName, designation = a.designation, userDepartmentId = a.userDepartmentId };

            jsonResult = JsonConvert.SerializeObject(result);

            return Json(jsonResult, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetRolesdata(string Email)
        {
            ServiceVMSEntities context = new ServiceVMSEntities();
            var jsonResult = string.Empty;

            var DesignationList = unitOfWork.DepartmentRepository.GetDesignationByEmail(Email);

            var result = from a in DesignationList select new { a.RoleInIMS, a.userEmail, a.userDepartmentId, a.userDepartmentName, a.userLocation };
            jsonResult = JsonConvert.SerializeObject(result);

            return Json(jsonResult, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetDesignationCount(string Email)
        {

            // var approver1 = new User();

            var DesignationList = unitOfWork.DepartmentRepository.GetDesignation();

            var result = from a in DesignationList where a.userEmail.Equals(Email, StringComparison.CurrentCultureIgnoreCase) select new { a.RoleInIMS, a.userEmail, a.userDepartmentId, a.userDepartmentName };//employeeId = a.employeeId, userName = a.userName, designation = a.designation, userDepartmentId = a.userDepartmentId };
            var cnt = result.Count();

            var jsonResult = JsonConvert.SerializeObject(cnt);

            return Json(jsonResult, JsonRequestBehavior.AllowGet);
        }



        public bool LDAP_Connection(string userEmail, string userPassword)
        {
            try
            {
                LdapDirectoryIdentifier ldi = new LdapDirectoryIdentifier("10.0.2.228", 389);               
                LdapConnection ldapConnection = new LdapConnection(ldi);
                Console.WriteLine("LdapConnection is created successfully.");
                logger.LogCreationForExceptions("LdapConnection is created successfully.");
                ldapConnection.SessionOptions.ProtocolVersion = 3;
                ldapConnection.SessionOptions.SecureSocketLayer = false;
                ldapConnection.SessionOptions.VerifyServerCertificate += delegate { return true; };
                ldapConnection.AuthType = AuthType.Basic;
                NetworkCredential nc = new NetworkCredential(userEmail, userPassword);
                ldapConnection.Bind(nc);
                Console.WriteLine("LdapConnection authentication success");
                ldapConnection.Dispose();
                logger.LogCreationForExceptions("ldaptrue--------------------------");
                return true;
            }
            catch (Exception e)
            {
                string data = "LDAP_Connection :" + e.Message + " : " + e.StackTrace;
                logger.LogCreationForExceptions(data);
                logger.LogCreationForExceptions("ldapfalse--------------------------");
                return false;
            }


        }
        public ActionResult WelcomeUser(string uemail)
        {
            // Decode the email address
            string decodedEmail = unitOfWork.DepartmentRepository.Decode(uemail);
            ViewBag.UserEmail = decodedEmail;
            ViewBag.designation = unitOfWork.DepartmentRepository.GetDesignation();
            ViewBag.request = "welcomeuser";
			//Session["OtpValue"] = "1234";
		 bool OtpSendOrNot = SendOtp(decodedEmail);
			Session["Email"] = decodedEmail;
            return View();
        }

        [HttpGet]
        public bool SendOtp(string Email)
        {
            var uemail = Email;

            try
            {

                var count = 0;
                MailMessage mail = new MailMessage();
                string username = ConfigurationManager.AppSettings["SmtpUserId"].ToString();
                mail.From = new MailAddress(username);
                mail.To.Add(uemail);
                mail.Subject = "Login OTP Mail";
                var chars = "0123456789";
                var stringChars = new char[4];
                var random = new Random();

                for (int i = 0; i < stringChars.Length; i++)
                {
                    stringChars[i] = chars[random.Next(chars.Length)];
                }

                var finalString = new String(stringChars);
                Session["OtpValue"] = "1234";


                if (finalString != null)
                {
                    count++;
                }
                string Body = finalString;

                string Data = "Your OTP for Login is " + "<b>" + Body + "</b> " + ".This OTP is valid for 10 min only.";
                mail.Body = Data;
                mail.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient();
                smtp.Host = ConfigurationManager.AppSettings["SmtpServerHost"].ToString();
                smtp.Port = Convert.ToInt32(ConfigurationManager.AppSettings["SmtpServerPort"]);
                smtp.UseDefaultCredentials = false;
                smtp.EnableSsl = true;
                string password = ConfigurationManager.AppSettings["SmtpUserPassword"].ToString();
                smtp.Credentials = new System.Net.NetworkCredential(username, password);
                if (count <= 5)
                {
                    smtp.Send(mail);
                }
                Session["VerificationTime"] = DateTime.Now;
                ViewBag.VerificationTime = DateTime.Now;
            }


            catch (Exception ex)
            {
                var exceptionLogger = new IMSExceptionLogger()
                {
                    controllerName = "LoginController",
                    actionName = "SendOtp",
                    exceptionStackTrace = ex.StackTrace,
                    exceptionMessage = ex.Message,
                    exceptionLogTime = DateTime.Now

                };

                unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                unitOfWork.Save();
            }
            return true;
        }
        [HttpGet]
        public JsonResult VerifyOTP(string OTPValue, string verificationTime)
        {
            var jsonResult = string.Empty;

            if (Session["OtpValue"] != null && Session["VerificationTime"] != null)
            {
                DateTime storedVerificationTime = Convert.ToDateTime(Session["VerificationTime"]);

                if (storedVerificationTime.AddMinutes(10) < DateTime.Now)
                {
                    var Result = new { isValidOTP = false, Errormsg = "Sorry! your OTP is expired" };
                    jsonResult = JsonConvert.SerializeObject(Result);
                    return Json(jsonResult, JsonRequestBehavior.AllowGet);
                }

                string OTPGenerate = Session["OtpValue"].ToString();

                if (OTPValue == OTPGenerate)
                {
                    var Result = new { isValidOTP = true };
                    jsonResult = JsonConvert.SerializeObject(Result);
                    return Json(jsonResult, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var Result = new { isInValidOTP = true, Errormsg = "Please Enter Valid OTP." };
                    jsonResult = JsonConvert.SerializeObject(Result);
                    return Json(jsonResult, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                var Result = new { isValidOTP = false, Errormsg = "Sorry! your OTP is expired" };
                jsonResult = JsonConvert.SerializeObject(Result);
                return Json(jsonResult, JsonRequestBehavior.AllowGet);
            }
        }
        [HttpGet]
        public JsonResult ResendOtp(string Email)
        {
            var jsonResult = string.Empty;

            try
            {
                MailMessage mail = new MailMessage();
                string username = ConfigurationManager.AppSettings["SmtpUserId"].ToString();
                mail.From = new MailAddress(username);
                mail.To.Add(Email);

                mail.Subject = "Login OTP Mail";
                var chars = "0123456789";
                var stringChars = new char[4];
                var random = new Random();

                for (int i = 0; i < stringChars.Length; i++)
                {
                    stringChars[i] = chars[random.Next(chars.Length)];
                }

                var finalString = new string(stringChars);
                Session["OtpValue"] = finalString;
                Session["VerificationTime"] = DateTime.Now; // Update VerificationTime here

                string Data = "Your OTP for Login is " + "<b>" + finalString + "</b> " + ".This OTP is valid for 10 min only.";
                mail.Body = Data;
                mail.IsBodyHtml = true;

                SmtpClient smtp = new SmtpClient
                {
                    Host = ConfigurationManager.AppSettings["SmtpServerHost"].ToString(),
                    Port = Convert.ToInt32(ConfigurationManager.AppSettings["SmtpServerPort"]),
                    UseDefaultCredentials = false,
                    EnableSsl = true,
                    Credentials = new System.Net.NetworkCredential(username, ConfigurationManager.AppSettings["SmtpUserPassword"].ToString())
                };
                smtp.Send(mail);

                var Result = new { data = true, VerificationTime = DateTime.Now };
                jsonResult = JsonConvert.SerializeObject(Result);
            }
            catch (Exception ex)
            {
                var exceptionLogger = new IMSExceptionLogger
                {
                    controllerName = "LoginController",
                    actionName = "ReSendOtp",
                    exceptionStackTrace = ex.StackTrace,
                    exceptionMessage = ex.Message,
                    exceptionLogTime = DateTime.Now
                };

                unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                unitOfWork.Save();
            }

            return Json(jsonResult, JsonRequestBehavior.AllowGet);
        }


        private bool InvalidLoginAttempts(string useremail, bool isValidUser)
        {
            //logger.LogCreationForExceptions("InvalidLoginAttempts" + useremail + "--" + company + "-----" + isValidUser);
            int logincount = 0;
            DateTime? logindate = null;
            var loginDetails = (from t in context.HTV_Login
                                where t.userEmail == useremail
                                select t).ToList();
            foreach (var y in loginDetails)
            {
                loginoldtime = y.loginDatetime;
                logincount = y.countAttempt;
            }
            logindate = DateTime.Now.AddMinutes(-20);

            if (logincount == 0 && !isValidUser)
            {
                var login = new HTV_Login
                {
                    userEmail = useremail,
                    countAttempt = 1,
                    loginDatetime = DateTime.Now
                };
                logincount = 1;
                context.HTV_Login.Add(login);
                context.SaveChanges();

            }
            else if (logincount <= 5 && loginoldtime > logindate && !isValidUser)
            {
                var loginupdate = (from t in context.HTV_Login
                                   where t.userEmail == useremail
                                   select t).SingleOrDefault();

                loginupdate.loginDatetime = DateTime.Now;
                loginupdate.countAttempt = logincount + 1;
                context.SaveChanges();

            }
            else if (logincount <= 5 && loginoldtime < logindate && !isValidUser)
            {
                var loginupdate = (from t in context.HTV_Login
                                   where t.userEmail == useremail
                                   select t).SingleOrDefault();

                loginupdate.loginDatetime = DateTime.Now;
                loginupdate.countAttempt = 1;

                context.SaveChanges();
                loginAttempt = 1;
                logincount = 1;

            }
            else if (logincount > 5 && loginoldtime > logindate)
            {

                var loginupdate = (from t in context.HTV_Login
                                   where t.userEmail == useremail
                                   select t).SingleOrDefault();

                if (!isValidUser)
                {
                    loginupdate.loginDatetime = DateTime.Now;
                    loginupdate.countAttempt = logincount + 1;
                    context.SaveChanges();
                }
                //return "Sorry! your account is blocked for next 20 mins";
                return true;
            }
            else if (logincount > 5 && loginoldtime < logindate)
            {
                var loginupdate = (from t in context.HTV_Login
                                   where t.userEmail == useremail
                                   select t).SingleOrDefault();

                loginupdate.loginDatetime = DateTime.Now;
                loginupdate.countAttempt = 1;

                context.SaveChanges();
                loginAttempt = 1;
                logincount = 1;

            }


            return false;


        }

        [HttpGet]
        public ActionResult SessionAlert()
        {
            return View();
        }

    }

}
