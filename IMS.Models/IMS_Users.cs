using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMS.Models
{
    public class IMS_Users
    {
        public string userId { get; set; }
        public string userName { get; set; }
        public string userEmail { get; set; }
        public string userPassword { get; set; }
        public string userphone { get; set; }
        public string userStatus { get; set; }
        public string archiveduserReason { get; set; }
        public string IMSRole_Id { get; set; }
        public string IMSuser_Id { get; set; }
        public string RoleInIMS { get; set; }
        public string userLocation { get; set; }
        public string Role_Status { get; set; }


        [Required]
        [Display(Name = "User Department")]
        public int userDepartmentId { get; set; }

        [Required]
        [Display(Name = "User Department")]
        public string userDepartmentName { get; set; }

        public string user_department { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; }
    }
}
