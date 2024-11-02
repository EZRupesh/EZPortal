using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EZJOB.Models
{
    public class JobDetails
    {
        public List<CustomerJob> list { get; set; }

        public List<PausedDetail> Pausedlist { get; set; }

        public List<PausedDetail> Resumedlist { get; set; }

        public List<ItemDetails> ItemList { get; set; }

        public List<FileDetails> FileList { get; set; }

        public List<AssetDetails> AssetList { get; set; }
        public List<WorkOrderHeaderDetails> WorkOrderHeader { get; set; }
        public string customerReview { get; set; }

        public string HoldRemark { get; set; }
    }
    
}