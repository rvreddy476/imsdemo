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
    
    public partial class RejectedInvMaterial
    {
        public string RejectedInv_ID { get; set; }
        public string InvMaterialOP_ID { get; set; }
        public string InvMaterialCP_ID { get; set; }
        public Nullable<System.DateTime> RejectedInvDateTime { get; set; }
        public string RejectedReason { get; set; }
        public string RejectedBy { get; set; }
    
        public virtual InwardMaterial_CAPEX InwardMaterial_CAPEX { get; set; }
        public virtual InwardMaterial_OPEX InwardMaterial_OPEX { get; set; }
    }
}
