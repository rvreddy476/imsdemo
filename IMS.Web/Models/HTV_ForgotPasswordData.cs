//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IMS.Web.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class HTV_ForgotPasswordData
    {
        public int ForgotPasswordId { get; set; }
        public System.DateTime ForgotPasswordTimeStamp { get; set; }
        public string employeeId { get; set; }
        public string vendorId { get; set; }
    
        public virtual HTV_Vendor HTV_Vendor { get; set; }
        public virtual User User { get; set; }
    }
}
