namespace EZJOB.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("ezbusdb.CustomerJob")]
    public partial class CustomerJob
    {
        public int Id { get; set; }

        public string Title { get; set; }

        [StringLength(50)]
        public string Type { get; set; }

        public string Description { get; set; }

        [StringLength(50)]
        public string Priority { get; set; }

        public long? AssignToId { get; set; }

        public string AssignToName { get; set; }

        public long? RequestedId { get; set; }

        public string RequestedName { get; set; }

        public DateTime? RequestedDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; }

        public string CustomerName { get; set; }

        public string CustomerAddress { get; set; }

        [StringLength(10)]
        public string CustomerPinCode { get; set; }

        [StringLength(50)]
        public string CustomerLat { get; set; }

        [StringLength(50)]
        public string CustomerLog { get; set; }

        public string CustomerReview { get; set; }

        [StringLength(10)]
        public string JobStatus { get; set; }

        public string JobId { get; set; }

        public long? CompanyId { get; set; }

        public long? BranchId { get; set; }

        public DateTime? JobStartDate { get; set; }

        public DateTime? JobEndDate { get; set; }

        public string HoldRemark { get; set; }
    }
}
