using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EZJOB.Models
{
    public class ItemDetails
    {
        public int PortalId { get; set; }
        public int id { get; set; }
        public string Qty { get; set; }
        
        public string Specification { get; set; }
    }
}