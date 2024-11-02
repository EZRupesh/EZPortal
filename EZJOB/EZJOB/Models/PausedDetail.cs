namespace EZJOB.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("ezbusdb.PausedDetails")]
    public partial class PausedDetail
    {
        public long id { get; set; }

        public long? JobId { get; set; }

        public DateTime? PausedDate { get; set; }

        public string PausedRemark { get; set; }

        public DateTime? ResumeDate { get; set; }

        public string ResumeRemark { get; set; }
    }
}
