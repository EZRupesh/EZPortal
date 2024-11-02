namespace EZJOB.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("Employee")]
    public partial class Employee
    {
        public long Id { get; set; }

        public string Name { get; set; }

        [StringLength(50)]
        public string UserName { get; set; }

        [StringLength(50)]
        public string Password { get; set; }

        [StringLength(50)]
        public string ConfirmPassword { get; set; }

        [StringLength(100)]
        public string EmailId { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        public long? CompanyId { get; set; }

        public long? BranchId { get; set; }
    }
}
