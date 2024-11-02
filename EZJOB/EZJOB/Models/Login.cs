using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace EZJOB.Models
{
    public class Login
    {
        public int SelectedCompanyId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public IEnumerable<SelectListItem> CompanyName { get; set; }

        public string ErrorMessage { get; set; }
    }
}