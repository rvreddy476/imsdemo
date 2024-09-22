using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using IMS.BLL;
using IMS.BLL.Interfaces;
using IMS.DAL;
using IMS.Entities;
using IMS.Models;


namespace VMS.Web.Controllers
{
    public class Add_MaterialController : Controller
    {
        private ServiceVMSEntities context = new ServiceVMSEntities();
        private UnitOfWork unitOfWork = new UnitOfWork();
        private IMExceptionLogger exceptionLogger = BLLObjectCreator.CreateIMSLogger(ExceptionLoggerType.IMSText);
        public ActionResult Index()
        {
            Material_Category material_Category = new Material_Category();
            Material material = new Material();
            ViewBag.materialCategoryname = unitOfWork.DepartmentRepository.GetMaterialCategory(material_Category, 0);
            ViewBag.MaterialList = unitOfWork.DepartmentRepository.GetMaterials(material, 0);
            return View();          
        }
              

        [HttpPost]
        public ActionResult Add_MaterialCategory(IMSEntity iMSEntity, string type)
        {
            
                        ServiceVMSEntities db = new ServiceVMSEntities();                   
                        if (type == "AddCategory")
                        {
                            var materialcat = new Material_Category()
                            {
                                Material_CategoryName = iMSEntity.Material_CategoryName
                            };
                            db.Material_Category.Add(materialcat);
                            db.SaveChanges();
                            exceptionLogger.LogCreationForCategoryFeature(materialcat, "Create Category", null, null);

                            return RedirectToAction("Index", "Add_Material");
                        }
                       else if (type == "UpdateCategory")
                        {
                           var categoryupdate = (from t in db.Material_Category
                                                  where t.Material_CategoryID == iMSEntity.CategoryID
                                                  select t).SingleOrDefault();
                            var materialcat = new Material_Category()
                            {
                                Material_CategoryName = categoryupdate.Material_CategoryName,
                                Material_CategoryID = categoryupdate.Material_CategoryID
                            };

                            categoryupdate.Material_CategoryName = iMSEntity.Material_CategoryName;
                           
                             db.SaveChanges();
                             exceptionLogger.LogCreationForCategoryFeature(categoryupdate, "Update Category", materialcat, null);
                             return RedirectToAction("Index", "Add_Material");
                        }
                    else if (type == "DeleteCategory")
                    {
                      var categoryupdate = (from t in db.Material_Category
                                            where t.Material_CategoryID == iMSEntity.CategoryID
                                            select t).SingleOrDefault();

                        string category_name = categoryupdate.Material_CategoryName;
                        db.Material_Category.Remove(categoryupdate);
                        db.SaveChanges();
                
                       exceptionLogger.LogCreationForCategoryFeature(null, "delete Category", null, category_name);
                       return RedirectToAction("Index", "Add_Material");
                    }
                     return RedirectToAction("Index","Add_Material");
        }

        [HttpPost]
        public ActionResult Add_Material(IMSEntity iMSEntity, string mattype)
        {

            ServiceVMSEntities db = new ServiceVMSEntities();
            if (mattype == "AddMaterial")
            {
                var material = new Material()
                {
                    MaterialID = unitOfWork.DepartmentRepository.auto_generatedCode(),
                    Material_CategoryID = iMSEntity.Material_CategoryID,
                    MaterialName = iMSEntity.MaterialName,
                    MaterialPrefix = iMSEntity.MaterialPrefix

                };
                db.Materials.Add(material);
                db.SaveChanges();
                exceptionLogger.LogCreationForMaterialFeature(material, "Create Material", null, null);

                return RedirectToAction("Index", "Add_Material");
            }
            else if (mattype == "UpdateMaterial")
            {
                var categoryupdate = (from t in db.Materials
                                      where t.MaterialID == iMSEntity.MaterialID
                                      select t).SingleOrDefault();
                var materialcat = new Material()
                {
                    MaterialName = categoryupdate.MaterialName,
                    MaterialID = categoryupdate.MaterialID,
                    MaterialPrefix = categoryupdate.MaterialPrefix,
                    Material_CategoryID = categoryupdate.Material_CategoryID
                };

                categoryupdate.MaterialName = iMSEntity.MaterialName;
                categoryupdate.MaterialPrefix = iMSEntity.MaterialPrefix;
                categoryupdate.Material_CategoryID = iMSEntity.Material_CategoryID;
                db.SaveChanges();
                exceptionLogger.LogCreationForMaterialFeature(categoryupdate, "Update Material", materialcat, null);
                return RedirectToAction("Index", "Add_Material");
            }
            else if (mattype == "DeleteMaterial")
            {
                var categoryupdate = (from t in db.Materials
                                      where t.MaterialID == iMSEntity.MaterialID
                                      select t).SingleOrDefault();

                string category_name = categoryupdate.MaterialName;
                db.Materials.Remove(categoryupdate);
                db.SaveChanges();

                exceptionLogger.LogCreationForCategoryFeature(null, "Delete Material", null, category_name);
                return RedirectToAction("Index", "Add_Material");
            }
            return RedirectToAction("Index", "Add_Material");
        }
    }
}