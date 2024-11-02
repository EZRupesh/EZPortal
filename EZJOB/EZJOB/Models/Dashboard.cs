using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace EZJOB.Models
{
    public class Dashboard
    {
        public string Closed { get; set; }
        public string Opn { get; set; }
        public string Hold { get; set; }
        public string Inprogress { get; set; }
        public string Total { get; set; }

        public bool check { get; set; }
    }
    public class DashboardList
    {
        public List<Dashboard> list { get; set; }

    }
}