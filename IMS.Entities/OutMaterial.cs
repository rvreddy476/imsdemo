//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IMS.Entities
{
    using System;
    using System.Collections.Generic;
    
    public partial class OutMaterial
    {
        public string OutMat_ID { get; set; }
        public string GatepassID { get; set; }
        public string Material_CategoryID { get; set; }
        public string MaterialID { get; set; }
        public string MaterialName { get; set; }
        public Nullable<int> Quantity { get; set; }
        public string Serial_Number { get; set; }
        public string Out_Status { get; set; }
        public Nullable<System.DateTime> Material_InDate { get; set; }
        public string Model_Number { get; set; }
        public string Remarks { get; set; }
        public string InwardID { get; set; }
        public string InventoryReg_ID { get; set; }
        public Nullable<System.DateTime> MaterialDispatchDate { get; set; }
    
        public virtual GatePass GatePass { get; set; }
    }
}
