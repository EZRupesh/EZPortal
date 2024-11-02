using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DBAccess;
using EZJOB.Controllers.API;
using EZJOB.Models;
using Newtonsoft.Json;

namespace EZJOB.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        APICall request;
        public ActionResult Home()
        {
            JobDetails model = new JobDetails();
            string accessToken;
            HttpCookie reqCookies = Request.Cookies["userInfo"];
            if (reqCookies != null)
            {
                accessToken = reqCookies["AccessToken"].ToString();
            }
            else
            {
                return RedirectToAction("Login", "Login");
            }
            request = new APICall();
            string jsonData;
            string status = Session["status"].ToString();
            if (status != "All")
            {
                jsonData = request.callAPI("/Api/Job/GetAllJobDetails?status=" + status + "&Priority=All", "GET", accessToken);
            }
            else
            {
                 jsonData = request.callAPI("/Api/Job/GetAllJobDetails", "GET", accessToken);
            }
          
            model.list = JsonConvert.DeserializeObject<List<CustomerJob>>(jsonData.ToString());

           
            return View(model);
        }

        public ActionResult Home1(string status)
        {
            JobDetails model = new JobDetails();
            string accessToken;
            HttpCookie reqCookies = Request.Cookies["userInfo"];
            if (reqCookies != null)
            {
                accessToken = reqCookies["AccessToken"].ToString();
            }
            else
            {
                return RedirectToAction("Login", "Login");
            }
            Session["status"] = status;
            request = new APICall();
            string jsonData;
            if (status!="All")
            {
                 jsonData = request.callAPI("/Api/Job/GetAllJobDetails?status=" + status + "&Priority=All", "GET", accessToken);
            }
            else
            {
                 jsonData = request.callAPI("/Api/Job/GetAllJobDetails", "GET", accessToken);
            }
            
            model.list = JsonConvert.DeserializeObject<List<CustomerJob>>(jsonData.ToString());



            return View("Home",model);
        }
    }
}