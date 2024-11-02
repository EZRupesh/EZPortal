using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using DBAccess;
using EZJOB.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EZJOB.Controllers
{
    public class LoginController : Controller
    {
        APICall request;
        string jsonData;
        public ActionResult Login()
        {
            var model = new Login();

            request = new APICall();
            jsonData = request.callAPI("/api/Job/GetCompanyList", "GET");
            model.CompanyName = JsonConvert.DeserializeObject<List<SelectListItem>>(jsonData.ToString());

            return View(model);
        }
        [HttpPost]
        public ActionResult Login(Login model)
        {
            var model1 = new Login();
            request = new APICall();
            var token = request.callToken(model.UserName, model.Password, model.SelectedCompanyId.ToString(), model.SelectedCompanyId.ToString());
            if (token.Item1 != "")
            {
                HttpCookie userInfo = new HttpCookie("userInfo");
                userInfo["AccessToken"] = token.Item1;                
                userInfo.Expires.Add(new TimeSpan(0, 1, 0));
                Response.Cookies.Add(userInfo);

                HttpCookie userInfo1 = new HttpCookie("userInfo1");
                userInfo["UserName"] = model.UserName;
                userInfo.Expires.Add(new TimeSpan(0, 1, 0));
                Response.Cookies.Add(userInfo1);

                //Session["AccessToken"] = token.Item1;
                return RedirectToAction("Dashboard", "Dashboard");
              
            }
            else
            {              
                request = new APICall();
                jsonData = request.callAPI("/api/Job/GetCompanyList", "GET");
                model1.CompanyName = JsonConvert.DeserializeObject<List<SelectListItem>>(jsonData.ToString());
                model1.ErrorMessage = token.Item2;
                return View(model1);
            }

        }

        [Route("Login/ForgotPassword")]
        public ActionResult ForgotPassword()
       {
            var model = new Login();

            request = new APICall();
            jsonData = request.callAPI("/api/Job/GetCompanyList", "GET");
            model.CompanyName = JsonConvert.DeserializeObject<List<SelectListItem>>(jsonData.ToString());

            return View("ForgotPassword",model);
            
        }

        [HttpPost]
        public ActionResult ForgotPassword(Login model)
        {
            var model1 = new Login();
            request = new APICall();
            jsonData = request.callAPI("/api/Job/GetCompanyList", "GET");
            model1.CompanyName = JsonConvert.DeserializeObject<List<SelectListItem>>(jsonData.ToString());

            request = new APICall();
            jsonData = request.callAPI("/Api/Users/ForgetPassword", "POST", model.UserName.ToString(), model.SelectedCompanyId, model.SelectedCompanyId);

            dynamic data = JObject.Parse(jsonData);
            string Status= data.status;
            if(Status.ToLower()!="true")
            {
                model1.ErrorMessage = data.errorMessage;
            }
            else if (Status.ToLower() == "true")
            {
                model1.ErrorMessage = "success";
            }
            
            return View(model1);
        }
    }
}