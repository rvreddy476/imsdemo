using IMS.BLL;
using IMS.BLL.Interfaces;
using IMS.DAL;
using IMS.Entities;
using IMS.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using AutoMapper;
using System.Data.Entity.Core.Objects.DataClasses;
using System.Data.Entity;

namespace IMS.Web.Controllers
{
    public class Add_UserController : Controller
    {
        // GET: Add_User
        private ServiceVMSEntities context = new ServiceVMSEntities();
        private UnitOfWork unitOfWork = new UnitOfWork();
        private IMExceptionLogger exceptionLogger = BLLObjectCreator.CreateIMSLogger(ExceptionLoggerType.IMSText);

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult UserRegistration()
        {
            var userDepartmenttype = new ServiceUserDepartment();
            ViewBag.userDepartment = unitOfWork.DepartmentRepository.GetUserDepartment(userDepartmenttype, 0, null);
            return View();
        }

        [HttpPost]
        public ActionResult UserRegistration(IMSEntity entity,string userexist)
        {
            if (Session["Email"] != null)
            {
                try
                {                  
                    bool isEmailValid = false;
                 
                    string userEmail = entity.userEmail;
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        string emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                        Regex reEmail = new Regex(emailRegex);

                        if (!reEmail.IsMatch(userEmail))
                        {
                            isEmailValid = false;
                            TempData["Msg"] = "Email is not valid!! ";
                            return RedirectToAction("UserRegistration", "Add_User");
                        }

                        else
                        {
                            isEmailValid = true;
                        }
                    }
                    else
                    {
                        TempData["Msg"] = "Email is required !! ";
                        return RedirectToAction("UserRegistration", "Add_User");
                    }

                    if (isEmailValid )
                    {
                        if (userexist == "0")
                        {
                            var userID = unitOfWork.DepartmentRepository.auto_generatedCode();
                            var password = Createrandompassword();
                            IMSUser user = new IMSUser();
                            user.userId = userID;
                            user.userEmail = entity.userEmail;
                            user.userName = entity.userName;
                            user.userPassword = password;
                            user.userphone = entity.userphone;
                            unitOfWork.DepartmentRepository.Insert(user);
                            unitOfWork.Save();

                            IMSRole role = new IMSRole();
                            role.IMSRole_Id = unitOfWork.DepartmentRepository.auto_generatedCode();
                            role.IMSuser_Id = userID;
                            role.userDepartmentId = int.Parse(entity.userDepartmentId.ToString());
                            role.RoleInIMS = entity.RoleInIMS;
                            role.userLocation = entity.userLocation;
                            context.IMSRoles.Add(role);
                            //unitOfWork.DepartmentRepository.Insert(role);
                            //    unitOfWork.Save();
                            context.SaveChanges();

                            var mail = sendCreadentialMail(entity.userEmail, password);
                        }
                        else
                        {
                            var userdetails = (from u in context.IMSUsers                                              
                                               select new { u.userId}).FirstOrDefault();

                            var roledetails = (from u in context.IMSUsers
                                               join r in context.IMSRoles on u.userId equals r.IMSuser_Id
                                               where u.userEmail == entity.userEmail
                                               select new { u.userId, r.userDepartmentId, r.RoleInIMS, r.userLocation });

                            foreach (var a in roledetails)
                            {
                                if (a.userDepartmentId == entity.userDepartmentId && a.RoleInIMS == entity.RoleInIMS && a.userLocation == entity.userLocation)
                                {
                                    TempData["Msg"] = "Role already exist for this location and department!!";
                                    return RedirectToAction("UserRegistration", "Add_User");
                                }
                            }
                            IMSRole role = new IMSRole();
                            role.IMSRole_Id = unitOfWork.DepartmentRepository.auto_generatedCode();
                            role.IMSuser_Id = userdetails.userId.ToString();
                            role.userDepartmentId = entity.userDepartmentId;
                            role.RoleInIMS = entity.RoleInIMS;
                            role.userLocation = entity.userLocation;
                            context.IMSRoles.Add(role);
                            context.SaveChanges();

                            
                        }
                    }
                    else
                    {
                        TempData["Msg"] = "Email or Password is not valid !! ";
                        return RedirectToAction("UserRegistration", "Add_User");
                    }

                    exceptionLogger.LogCreationForUserCreation(entity, "add", null);

                }
                catch (Exception e)
                {
                    var exceptionLogger = new IMSExceptionLogger()
                    {
                        controllerName = "Add_UserController",
                        actionName = "UserRegistration(post)",
                        exceptionStackTrace = e.StackTrace,
                        exceptionMessage = e.Message,
                        exceptionLogTime = DateTime.Now
                    };
                }
                return RedirectToAction("UserView", "Add_User");
            }

            else
            {
                return HttpNotFound();
            }
        }

        public string Createrandompassword()
        {
            string allowedChars = "";

            allowedChars = "a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z,";

            allowedChars += "A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z,";

            allowedChars += "1,2,3,4,5,6,7,8,9,0,!,@,#,$,%,&,?";

            char[] sep = { ',' };

            string[] arr = allowedChars.Split(sep);

            string passwordString = "";

            string temp = "";

            Random rand = new Random();

            for (int i = 0; i < Convert.ToInt32(8); i++)
            {

                temp = arr[rand.Next(0, arr.Length)];

                passwordString += temp;

            }           
            return passwordString;

        }

        [HttpPost]
        public JsonResult GetEmailIDs()
        {
            ServiceVMSEntities context = new ServiceVMSEntities();

            var UserList = from u in context.IMSUsers  select new { u.userEmail, u.userId, u.userphone, u.userName };

            var viewjson = JsonConvert.SerializeObject(UserList);
            return Json(viewjson, JsonRequestBehavior.AllowGet);

        }

        public bool sendCreadentialMail(string email,string upassword)
        {
            try
            {
                string Body = string.Empty;
                string sub = string.Empty;
                MailMessage mail = new MailMessage();
                string username = ConfigurationManager.AppSettings["SmtpUserId"].ToString();
                var materialhtml = string.Empty;
                var count = 1;
               
               
                mail.To.Add(email.ToString());
                sub = "IMS Credentials";
                    Body = "We are delighted to inform you that your credentials has been successfully created. Your login credentials are provided below::" +
                           "<br/><br/>" + "UserEmail: " + email.ToString()
                           + "<br/>" + "Password:" + upassword.ToString()
                           + "<br/><br/>";

                    var end_body = "<br/><font size=\"2\" color=\"red\"> <i> Note : This is a system generated email. Please do not reply to this email.</i></font>";
                    Body = Body + end_body;
                



                mail.Subject = sub;
                mail.From = new MailAddress(username);
                mail.Body = Body;
                mail.IsBodyHtml = true;
                SmtpClient smtp = new SmtpClient();
                smtp.Host = ConfigurationManager.AppSettings["SmtpServerHost"].ToString();
                smtp.Port = Convert.ToInt32(ConfigurationManager.AppSettings["SmtpServerPort"]);
                smtp.UseDefaultCredentials = false;
                smtp.EnableSsl = true;
                string password = ConfigurationManager.AppSettings["SmtpUserPassword"].ToString();
                smtp.Credentials = new System.Net.NetworkCredential(username, password);
                smtp.Send(mail);
                return true;
            }
            catch (Exception e)
            {
                var exceptionLogger = new IMSExceptionLogger()
                {
                    controllerName = "Add_UserController",
                    actionName = "sendCredentialMail",
                    exceptionStackTrace = e.StackTrace,
                    exceptionMessage = e.Message,
                    exceptionLogTime = DateTime.Now
                };
                return false;
            }
        }

        public ActionResult UserView(string sortOrder, string SearchString, string searchby, string SearchValue, FormCollection form,string SearchValue1)
        {
            if (Session["role"].ToString() == "Inventory Incharge" || Session["role"].ToString() == "Inventory Manager" || Session["role"].ToString() == "Administrator")
            {
                int countforsearch = 0;
                string display = "0";
                ViewBag.display = display;

                ViewBag.namesortParameter = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
                ViewBag.useridsortpara = sortOrder == "userId" ? "userId_desc" : "userId";
                ViewBag.departmentpara = sortOrder == "department" ? "department_desc" : "department";
                ViewBag.emailpara = sortOrder == "email" ? "email_desc" : "email";
                ViewBag.rolepara = sortOrder == "RoleInIMS" ? "RoleInIMS_desc" : "RoleInIMS";
                ViewBag.locpara = sortOrder == "Location" ? "Location_desc" : "Location";

                int count = 0;
                string userId = null;
                if (Session["UserID"] != null)
                {
                    userId = Session["UserID"].ToString();
                }
                ViewBag.userId = userId;
                var imsentity = new List<IMSEntity>();

                try
                {
                    string dept = null;
                    if (Session["DepartmentID"] != null)
                    {
                        dept = Session["DepartmentID"].ToString();

                    }

                    ServiceVMSEntities context = new ServiceVMSEntities();


                    var userList = from u in context.IMSUsers
                                   join r in context.IMSRoles on u.userId equals r.IMSuser_Id
                                   join d in context.ServiceUserDepartments on r.userDepartmentId equals d.userDepartmentId
                                   where u.userStatus != "Archived" || r.Role_Status != "Deleted"
                                   select new { u.userId, u.userName, r.RoleInIMS, u.userEmail, u.userPassword, u.userphone, d.userDepartmentName, r.userLocation ,r.IMSRole_Id};
                    if (!userId.Contains("DUM"))
                    {
                        userList = userList.Where(u => !u.userId.Contains("DUM"));
                    }
                    else
                    {
                        userList = userList.Where(u => u.userId.Contains("DUM"));
                    }

                    if (dept == "8")
                    {
                        userList = userList.Where(u => u.userDepartmentName == "Talent Acquisition");
                    }
                    else
                    {
                        userList = userList.Where(u => u.userDepartmentName != "Talent Acquisition");
                    }

                    if (!String.IsNullOrEmpty(SearchValue) || !String.IsNullOrEmpty(SearchValue1))
                    {
                        String Searchby = form["Name"].ToString();

                        if (Searchby == "userId")
                        {
                            countforsearch = 1;
                            userList = userList.Where(u => u.userId.Contains(SearchValue));
                        }
                        else if (Searchby == "userName")
                        {
                            countforsearch = 1;
                            userList = userList.Where(u => u.userName.Contains(SearchValue));
                        }
                        else if (Searchby == "userEmail")
                        {
                            countforsearch = 1;
                            userList = userList.Where(u => u.userEmail.Contains(SearchValue1));
                        }
                        else if (Searchby == "RoleInIMS")
                        {
                            countforsearch = 1;
                            userList = userList.Where(u => u.RoleInIMS.Contains(SearchValue));
                        }
                        else if (Searchby == "userDepartmentName")
                        {
                            countforsearch = 1;
                            userList = userList.Where(u => u.userDepartmentName == SearchValue1);
                            
                        }
                        else if (Searchby == "userLocation")
                        {
                            countforsearch = 1;
                            userList = userList.Where(u => u.userLocation.Contains(SearchValue));
                        }
                    }


                    switch (sortOrder)
                    {
                        case "name_desc":
                            userList = userList.OrderByDescending(u => u.userName);
                            break;

                        case "userId_desc":
                            userList = userList.OrderByDescending(u => u.userId);
                            break;

                        case "userId":
                            userList = userList.OrderBy(u => u.userId);
                            break;

                        case "department":
                            userList = userList.OrderBy(u => u.userDepartmentName);
                            break;

                        case "department_desc":
                            userList = userList.OrderByDescending(u => u.userDepartmentName);
                            break;

                        case "email":
                            userList = userList.OrderBy(u => u.userEmail);
                            break;

                        case "email_desc":
                            userList = userList.OrderByDescending(u => u.userDepartmentName);
                            break;

                        case "RoleInIMS":
                            userList = userList.OrderBy(u => u.RoleInIMS);
                            break;

                        case "RoleInIMS_desc":
                            userList = userList.OrderByDescending(u => u.RoleInIMS);
                            break;

                        case "Location":
                            userList = userList.OrderBy(u => u.userLocation);
                            break;

                        case "Location_desc":
                            userList = userList.OrderByDescending(u => u.userLocation);
                            break;

                        default:
                            userList = userList.OrderBy(u => u.userName);
                            break;
                    }

                    foreach (var t in userList)
                    {
                        imsentity.Add(new IMSEntity()
                        {
                            userId = t.userId,
                            userName = t.userName,
                            RoleInIMS = t.RoleInIMS,
                            userEmail = t.userEmail,                           
                            userDepartmentName = t.userDepartmentName,
                            userLocation = t.userLocation,
                            IMSRole_Id = t.IMSRole_Id
                        });
                    }

                    ViewBag.count = count;
                    ViewBag.CountforSearch = countforsearch;
                    return View(imsentity);
                }
                catch (Exception e)
                {
                    Console.Write(e);

                    var exceptionLogger = new IMSExceptionLogger()
                    {
                        controllerName = "Add_UserController",
                        actionName = "UserView",
                        exceptionStackTrace = e.StackTrace,
                        exceptionMessage = e.Message,
                        exceptionLogTime = DateTime.Now

                    };

                    unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                    unitOfWork.Save();
                    //return View();
                }

                return View(imsentity);
            }
            else
            {
                return HttpNotFound();
            }
        }

        [HttpGet]
        public ActionResult Edit_User(string id,string roleid)
        {
            if (Session["role"].ToString() == "Inventory Incharge")
            {
                try
                {
                    ServiceVMSEntities context = new ServiceVMSEntities();                    
                    id = unitOfWork.UserRepository.Decode(id);
                    roleid = unitOfWork.UserRepository.Decode(roleid);

                    var user_edit = (from u in context.IMSUsers
                                     join r in context.IMSRoles on u.userId equals r.IMSuser_Id
                                     where u.userId == id && r.IMSRole_Id == roleid
                                     select new { u.userId, u.userName, u.userEmail, u.userStatus, u.userphone, r.userDepartmentId, r.userLocation, r.RoleInIMS, r.IMSRole_Id }).FirstOrDefault();


                    var dept = user_edit.userDepartmentId;

                    ServiceUserDepartment serviceUserDepartment = context.ServiceUserDepartments.Where(d => d.userDepartmentId == dept).FirstOrDefault();

                    
                    var vmsentity = new IMSEntity()
                    {
                        userId = user_edit.userId,
                        userName = user_edit.userName,
                        userEmail = user_edit.userEmail,                      
                        userDepartmentId = user_edit.userDepartmentId,
                        userDepartmentName = serviceUserDepartment.userDepartmentName,
                        RoleInIMS = user_edit.RoleInIMS,
                        userLocation = user_edit.userLocation,
                        IMSRole_Id = user_edit.IMSRole_Id,
                        userphone = user_edit.userphone
                    };

                    ViewBag.userID = user_edit.userId;
                    ViewBag.IMSRoleID = user_edit.IMSRole_Id;
                    if (vmsentity == null)
                    {
                        return HttpNotFound();
                    }

                    var dept_info = new ServiceUserDepartment()
                    {
                        userDepartmentId = user_edit.userDepartmentId,
                        userDepartmentName = serviceUserDepartment.userDepartmentName
                    };
                    var userDepartmenttype = new ServiceUserDepartment();
                    ViewBag.userDepartment = unitOfWork.DepartmentRepository.Get(userDepartmenttype, 0);
                    return View(vmsentity);
                }
                catch (Exception e)
                {
                    Console.Write(e);

                    var exceptionLogger = new IMSExceptionLogger()
                    {
                        controllerName = "UserController",
                        actionName = "Edit_User(Get)",
                        exceptionStackTrace = e.StackTrace,
                        exceptionMessage = e.Message,
                        exceptionLogTime = DateTime.Now

                    };

                    unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                    unitOfWork.Save();
                    return View();
                }
            }
            else
            {
                return HttpNotFound();
            }


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit_User(IMSEntity entity, FormCollection collection)
        {
            try
            {
              
                bool isEmailValid = false;
                var userid = unitOfWork.DepartmentRepository.Encode(entity.userId);
                string userEmail = entity.userEmail;
                if (!string.IsNullOrEmpty(userEmail))
                {
                    string emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                    Regex reEmail = new Regex(emailRegex);

                    if (!reEmail.IsMatch(userEmail))
                    {
                        isEmailValid = false;
                        TempData["Msg"] = "Email is not valid!! ";
                        return RedirectToAction("Edit_User", "Add_User", new { id = userid });
                    }

                    else
                    {
                        isEmailValid = true;
                    }
                }
                else
                {
                    TempData["Msg"] = "Email is required !! ";
                    return RedirectToAction("Edit_User", "Add_User", new { id = userid });
                }
                if (isEmailValid)
                {


                    IMSUser user = context.IMSUsers.Single(x => x.userId == entity.userId);
                    user.userId = entity.userId;
                    user.userEmail = entity.userEmail;
                    user.userName = entity.userName;                   
                    user.userphone = entity.userphone;
                    context.Entry(user).State = EntityState.Modified;
                    context.SaveChanges();


                    IMSRole role = context.IMSRoles.Single(x => x.IMSRole_Id == entity.IMSRole_Id);
                    role.IMSRole_Id = entity.IMSRole_Id;
                    role.IMSuser_Id = entity.userId;
                    role.userDepartmentId = int.Parse(entity.userDepartmentId.ToString());
                    role.RoleInIMS = entity.RoleInIMS;
                    role.userLocation = entity.userLocation;
                    context.Entry(role).State = EntityState.Modified;
                 
                    context.SaveChanges();

                    var editService = new List<IMSEntity>();
                    var edit_data = from t in context.IMSUsers
                                    join r in context.IMSRoles on t.userId equals r.IMSuser_Id
                                    where t.userId == entity.userId && r.IMSRole_Id == entity.IMSRole_Id
                                    select new
                                    {
                                        t.userId,
                                        t.userName,
                                        r.RoleInIMS,
                                        r.userDepartmentId,
                                        t.userEmail,
                                        t.userPassword,
                                        t.userphone,
                                        r.userLocation
                                    };
                    foreach (var i in edit_data)
                    {
                        editService.Add(new IMSEntity()
                        {
                            userId = i.userId,
                            userName = i.userName,
                            RoleInIMS = i.RoleInIMS,
                            userDepartmentId = i.userDepartmentId,
                            userEmail = i.userEmail,
                            userLocation = i.userLocation,
                            userphone = i.userphone

                        });

                       

                    }
                    var original = new EntityCollection<IMSEntity>();

                    foreach (var item in editService)
                    {
                        original.Add(item);

                    }

                    IMSEntity original1 = original.FirstOrDefault();
                    exceptionLogger.LogCreationForUserCreation(entity, "edit", original1);
                    return RedirectToAction("UserView", "Add_User");

                }
                else
                {
                    TempData["Msg"] = "Email or Password is not valid !! ";
                    return RedirectToAction("EditUser", "User", new { id = userid });
                }

               
            }
            catch (Exception e)
            {
                Console.Write(e);
                var exceptionLogger = new IMSExceptionLogger()
                {
                    controllerName = "Add_UserController",
                    actionName = "Edit_User(Post)",
                    exceptionStackTrace = e.StackTrace,
                    exceptionMessage = e.Message,
                    exceptionLogTime = DateTime.Now
                };

                unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                unitOfWork.Save();


            }
            return RedirectToAction("UserView", "Add_User");
        }

        [HttpPost]
        public ActionResult DeleteUser(IMSEntity entity)
        {
            if (Session["role"].ToString() == "Inventory Incharge")
            {
                try
                {
                   
                    ServiceVMSEntities context = new ServiceVMSEntities();

                    var rolecount = (from r in context.IMSRoles
                                     where r.IMSuser_Id == entity.userId
                                     select r).Count();
                    if(rolecount == 1)
                    {

                        IMSUser user_in_db = context.IMSUsers.Single(x => x.userId == entity.userId);
                        user_in_db.userStatus = "Deleted";
                        user_in_db.archiveduserReason = entity.Reason;
                        context.Entry(user_in_db).State = EntityState.Modified;
                        context.SaveChanges();

                        IMSRole user_in_db1 = context.IMSRoles.Single(x => x.IMSRole_Id == entity.IMSRole_Id && x.IMSuser_Id == entity.userId);
                        user_in_db1.Role_Status = "Deleted";
                        user_in_db1.Reason = entity.Reason;
                        context.Entry(user_in_db).State = EntityState.Modified;
                        context.SaveChanges();
                    }
                    else
                    {
                        IMSRole user_in_db1 = context.IMSRoles.Single(x => x.IMSRole_Id == entity.IMSRole_Id && x.IMSuser_Id == entity.userId);
                        user_in_db1.Role_Status = "Deleted";
                        user_in_db1.Reason = entity.Reason;
                        context.Entry(user_in_db1).State = EntityState.Modified;
                        context.SaveChanges();

                    }




                    //IExceptionLogger exceptionLogger1 = BLLObjectCreator.CreateLogger(ExceptionLoggerType.Text);
                    exceptionLogger.LogCreationForUserCreation(entity, "delete", null);
                }
                catch (Exception e)
                {
                    Console.Write(e);

                    var exceptionLogger = new IMSExceptionLogger()
                    {
                        controllerName = "Add_UserController",
                        actionName = "DeleteUser",
                        exceptionStackTrace = e.StackTrace,
                        exceptionMessage = e.Message,
                        exceptionLogTime = DateTime.Now

                    };

                    unitOfWork.DepartmentRepository.Insert(exceptionLogger);
                    unitOfWork.Save();

                }
                return RedirectToAction("UserView", "Add_User");
            }
            else
            {
                return HttpNotFound();
            }

        }

        [HttpGet]
        public ActionResult ChangePassword()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ChangePassword(IMS_Users model)
        {
            string userid = string.Empty;
            if (Session["UserID"] != null)
            {
                userid = Session["UserID"].ToString();
            }
            var user = context.IMSUsers.FirstOrDefault(u => u.userId == userid);

            if (user != null)
            {
                if (user.userPassword == model.userPassword)
                {
                    if (model.NewPassword == model.ConfirmNewPassword)
                    {
                        user.userPassword = model.NewPassword;
                        context.SaveChanges();

                        ViewBag.Message = "Password successfully changed.";
                        return RedirectToAction("Index", "Dashboard");
                    }
                    else
                    {
                        ModelState.AddModelError("", "New password and confirm password do not match.");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Old password is incorrect.");
                }
            }
            else
            {
                ModelState.AddModelError("", "User not found.");
            }

            return RedirectToAction("Index", "Dashboard");
        }
    }
}