using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DBAccess;
using EZJOB.Models;

namespace EZJOB.Controllers
{
    public class UsersController : ApiController
    {
       public UsersController()
        {

        }
        [HttpPost]
        public HttpResponseMessage ForgetPassword(int companyid, int branchid, string username)
        {
            string sqlQuery = "", errorMessage = "";
            string companyId = "", branchId = "", userName = "", password = "''";
            int errnum = 0;
            DataTable dtRec;
            ConnectionHandler con = new ConnectionHandler();

            bool status = true;
            string message = "Your password is send to your register email id. ";


            dtRec = new DataTable();
            sqlQuery = "execute SPGetEmployee " + companyid + "," + branchid + "," + username + "," + password + ",'1'";
            dtRec = con.executeSelect(sqlQuery);

            if (dtRec.Rows.Count == 0)
            {
                errnum++;
                errorMessage = errorMessage + errnum + " Provided user is not access for selected company.";
            }

            dtRec = new DataTable();
            sqlQuery = "execute SPGetEmployee " + companyid + "," + branchid + "," + username + "," + password + ",'2'";
            dtRec = con.executeSelect(sqlQuery);
            if (dtRec.Rows.Count == 0)
            {
                errnum++;
                errorMessage = errorMessage + errnum + " Provided user is not access for selected branch.";
            }


            dtRec = new DataTable();
            sqlQuery = "execute SPGetEmployee " + companyid + "," + branchid + "," + username + ","+ password + ",'3'";
            dtRec = con.executeSelect(sqlQuery);
            if (dtRec.Rows.Count == 0)
            {
                errnum++;
                errorMessage = errorMessage + errnum + " Invalid Username.";
            }

            if (errorMessage == "" && dtRec.Rows.Count > 0)
            {
                string toMail = dtRec.Rows[0]["EmailId"].ToString();
                string empName = dtRec.Rows[0]["Name"].ToString();
                string Password = dtRec.Rows[0]["Password"].ToString();
                eMail mail = new eMail();
                var result = mail.SendMail("rupeshghosalkar3333@outlook.com", "Tracet@123", "smtp-mail.outlook.com", 587, toMail, "Password Recovery", "Hi " + empName + ", <br><br> Your Password:" + Password + " <br><br>Regards,<br>Rupesh.");
                if (result == "success")
                {
                    status = true;
                    message = "Your password is send to your register email id.";
                }
                else
                {
                    status = false;
                    message = "fail to send";
                }
            }
            else
            {
                status = false;
                return Request.CreateResponse(HttpStatusCode.OK, new { status, errorMessage });
            }

            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
        }

        [Route("api/Job/GetCompanyList")]
        [HttpGet]
        public List<LookupText> GetCompanyList()
        {
            var items = new List<LookupText>
        {
            new LookupText { Value = "1", Text = "DEMO CO. LLC" },
            new LookupText { Value = "2", Text = "DEMO Engineering LLC" },
            new LookupText { Value = "3", Text = "DEMO General Trading LLC" }
        };
            return items;
        }
    }
}
