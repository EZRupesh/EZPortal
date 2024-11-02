using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Web.Http;
using DBAccess;
using EZJOB.Models;
using Newtonsoft.Json;
using static EZJOB.Models.CustomerJobModel;

namespace EZJOB.Controllers.API
{
    [Authorize]
    public class JobController : ApiController
    {
        private EZJob _context;
        private string  username,password="";
        private long companyid, branchid, assigntoid;

        public JobController()
        {

        }
        public List<CustomerJob> GetAllJobDetails()
        {
            ConnectionHandler con = new ConnectionHandler();
            var identity = (ClaimsIdentity)User.Identity;
            companyid =long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());
            branchid = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "branchid").Value.ToString());
            username= identity.Claims.FirstOrDefault(c => c.Type == "username").Value.ToString();

            DataTable dtRec = new DataTable();
            string sqlQuery = "execute SPGetEmployee " + companyid + "," + branchid + ",'" + username + "','" + password + "','3'";
            dtRec = con.executeSelect(sqlQuery);
            if(dtRec.Rows.Count>0)
            {
                assigntoid = long.Parse(dtRec.Rows[0]["Id"].ToString());
            }
            else
            {
                assigntoid = 0;
            }
            assigntoid = Int32.Parse(username.Substring(username.Length - 2));
            using (_context = new EZJob())
            {
                var filteredJobs = new CustomerJobDetails();
                var customerJobsDetail= filteredJobs.CustomerDet = _context.CustomerJobs.ToList();
                return customerJobsDetail.Where(x => x.AssignToId == assigntoid && x.CompanyId == companyid && x.BranchId == branchid).ToList();
            }
            
        }
        [HttpGet]
        public List<CustomerJob> GetAllJobDetails(string status ,string Priority)
        {
            ConnectionHandler con = new ConnectionHandler();
            var identity = (ClaimsIdentity)User.Identity;
            companyid = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());
            branchid = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "branchid").Value.ToString());
            username = identity.Claims.FirstOrDefault(c => c.Type == "username").Value.ToString();

            DataTable dtRec = new DataTable();
            string sqlQuery = "execute SPGetEmployee " + companyid + "," + branchid + ",'" + username + "','" + password + "','3'";
            dtRec = con.executeSelect(sqlQuery);
            if (dtRec.Rows.Count > 0)
            {
                assigntoid = long.Parse(dtRec.Rows[0]["Id"].ToString());
            }
            else
            {
                assigntoid = 0;
            }
            assigntoid = Int32.Parse(username.Substring(username.Length - 2));
            using (_context = new EZJob())
            {
                var filteredJobs = new CustomerJobDetails();
                var customerJobsDetail = filteredJobs.CustomerDet = _context.CustomerJobs.ToList();
                if (Priority == "All")
                {
                    return customerJobsDetail.Where(x => x.AssignToId == assigntoid && x.CompanyId == companyid && x.BranchId == branchid && x.Status == status).ToList();
                }
                else
                {
                    return customerJobsDetail.Where(x => x.AssignToId == assigntoid && x.CompanyId == companyid && x.BranchId == branchid && x.Status == status && x.Priority==Priority).ToList();
                }
                
            }

        }

        [HttpGet]
        [Route("api/Job/GetAllJobDetailsid")]
        public List<CustomerJob> GetAllJobDetails(string id)
        {
            ConnectionHandler con = new ConnectionHandler();
            var identity = (ClaimsIdentity)User.Identity;
            companyid = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());
            branchid = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "branchid").Value.ToString());
            username = identity.Claims.FirstOrDefault(c => c.Type == "username").Value.ToString();

            DataTable dtRec = new DataTable();
            string sqlQuery = "execute SPGetEmployee " + companyid + "," + branchid + ",'" + username + "','" + password + "','3'";
            dtRec = con.executeSelect(sqlQuery);
            if (dtRec.Rows.Count > 0)
            {
                assigntoid = long.Parse(dtRec.Rows[0]["Id"].ToString());
            }
            else
            {
                assigntoid = 0;
            }
            assigntoid = Int32.Parse(username.Substring(username.Length - 2));
            using (_context = new EZJob())
            {
                var filteredJobs = new CustomerJobDetails();
                var customerJobsDetail = filteredJobs.CustomerDet = _context.CustomerJobs.ToList();
                
                    return customerJobsDetail.Where(x => x.AssignToId == assigntoid && x.CompanyId == companyid && x.BranchId == branchid && x.Id==Int32.Parse(id)).ToList();
              

            }

        }

        [Route("api/Job/GetDashbordCount")]
        [HttpGet]
        public HttpResponseMessage GetDashbordCount()
        {
            var identity = (ClaimsIdentity)User.Identity;
            companyid = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());
            branchid = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "branchid").Value.ToString());
            username = identity.Claims.FirstOrDefault(c => c.Type == "username").Value.ToString();
            
            
            
            DataTable dtRec;
            dtRec = new DataTable();
            string sqlQuery;
            ConnectionHandler con = new ConnectionHandler();

            sqlQuery = "execute SPGetEmployee " + companyid + "," + branchid + ",'" + username + "','" + password + "','3'";
            dtRec = con.executeSelect(sqlQuery);
            if (dtRec.Rows.Count > 0)
            {
                assigntoid = long.Parse(dtRec.Rows[0]["Id"].ToString());
            }
            else
            {
                assigntoid = 0;
            }
            assigntoid = Int32.Parse(username.Substring(username.Length-2));
             dtRec = new DataTable();
            sqlQuery = "execute SPGetDashbord " + companyid + "," + branchid + "," + assigntoid;
            
            dtRec = con.executeSelect(sqlQuery);

            if (dtRec.Rows.Count>0)
            {
                var JSONresult = JsonConvert.SerializeObject(dtRec, Formatting.Indented);
                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(JSONresult.ToString(), Encoding.UTF8, "application/json");
                return response;
            }
            else
            {
                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent("{\"ErrorDetails\":\"Dashbord details not found\"}", Encoding.UTF8, "application/json");
                return response;
            }
        }

        
        //public List<CustomerJob> GetJobDetails(List<CustomerJob> depObj)
        //{

        //}
    }
}
