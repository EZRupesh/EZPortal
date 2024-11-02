using System;
using System.Collections.Generic;
using System.Data;
using System.Device.Location;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using DBAccess;
using EZJOB.Models;
using Newtonsoft.Json;

namespace EZJOB.Controllers
{
    public class DashboardController : Controller
    {
        // GET: Dashbord
        APICall request;
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult DutyOn(string lat, string longt)
        {


            Dashboard model = new Dashboard();
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
            HttpCookie reqCookies1 = Request.Cookies["userInfo1"];
            if (reqCookies != null)
            {
                var username = reqCookies["UserName"].ToString();
                ConnectionHandler con = new ConnectionHandler();



                DataTable dtRec = new DataTable();
                string sqlQuery = "execute SPGetEmployee 1,1," + username + ",'','3'";
                dtRec = con.executeSelect(sqlQuery);

                ViewBag.username = dtRec.Rows[0]["Empname"].ToString();
                Session["Empname"] = dtRec.Rows[0]["Empname"].ToString();

                dtRec = new DataTable();
                sqlQuery = "[ezbusdb].[SPEmployeeTimeSheet] " + username + "," + lat + "," + longt + ",3";
                dtRec = con.executeSelect(sqlQuery);

                if (dtRec.Rows.Count == 0)
                {
                    sqlQuery = "[ezbusdb].[SPEmployeeTimeSheet] " + username + "," + lat + "," + longt + ",1";
                    dtRec = con.executeSelect(sqlQuery);

                }

                dtRec = new DataTable();
                sqlQuery = "[ezbusdb].[SPEmployeeTimeSheet] " + username + ",'','',3";
                dtRec = con.executeSelect(sqlQuery);
                if (dtRec.Rows.Count > 0)
                {
                    if (dtRec.Rows[0]["StartTime"].ToString() == "")
                    {
                        model.check = false;
                    }
                    else
                    {
                        if (dtRec.Rows[0]["EndTime"].ToString() == "")
                        {
                            model.check = true;
                        }
                        else
                        {
                            model.check = false;
                        }
                    }
                }
                else
                {
                    model.check = false;

                }

            }





            request = new APICall();
            string jsonData = request.callAPI("/Api/Job/GetDashbordCount", "GET", accessToken);
            var a = JsonConvert.DeserializeObject<List<DashboardList>>(jsonData.ToString());
            var model1 = JsonConvert.DeserializeObject<Dashboard>(jsonData.ToString().Replace("[", "").Replace("]", ""));
            model.Opn = model1.Opn;
            model.Closed = model1.Closed;
            model.Hold = model1.Hold;
            model.Total = model1.Total;
            model.Inprogress = model1.Inprogress;
            
            return View("Dashboard", model);
        }

        public ActionResult DutyOff(string lat, string longt)
        {


            Dashboard model = new Dashboard();
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
            HttpCookie reqCookies1 = Request.Cookies["userInfo1"];
            if (reqCookies != null)
            {
                var username = reqCookies["UserName"].ToString();
                ConnectionHandler con = new ConnectionHandler();



                DataTable dtRec = new DataTable();
                string sqlQuery = "execute SPGetEmployee 1,1," + username + ",'','3'";
                dtRec = con.executeSelect(sqlQuery);

                ViewBag.username = dtRec.Rows[0]["Empname"].ToString();
                Session["Empname"] = dtRec.Rows[0]["Empname"].ToString();

                dtRec = new DataTable();
                sqlQuery = "[ezbusdb].[SPEmployeeTimeSheet] " + username + "," + lat + "," + longt + ",3";
                dtRec = con.executeSelect(sqlQuery);

                if (dtRec.Rows.Count > 0)
                {
                    if (dtRec.Rows[0]["EndTime"].ToString() == "")
                    {
                        sqlQuery = "[ezbusdb].[SPEmployeeTimeSheet] " + username + "," + lat + "," + longt + ",2";
                        dtRec = con.executeSelect(sqlQuery);
                    }

                }

                dtRec = new DataTable();
                sqlQuery = "[ezbusdb].[SPEmployeeTimeSheet] " + username + ",'','',3";
                dtRec = con.executeSelect(sqlQuery);
                if (dtRec.Rows.Count > 0)
                {
                    if (dtRec.Rows[0]["StartTime"].ToString() == "")
                    {
                        model.check = false;
                    }
                    else
                    {
                        if (dtRec.Rows[0]["EndTime"].ToString() == "")
                        {
                            model.check = true;
                        }
                        else
                        {
                            model.check = false;
                        }
                    }
                }
                else
                {
                    model.check = false;

                }
            }



            request = new APICall();
            string jsonData = request.callAPI("/Api/Job/GetDashbordCount", "GET", accessToken);
            var a = JsonConvert.DeserializeObject<List<DashboardList>>(jsonData.ToString());
            var model1 = JsonConvert.DeserializeObject<Dashboard>(jsonData.ToString().Replace("[", "").Replace("]", ""));
            model.Opn = model1.Opn;
            model.Closed = model1.Closed;
            model.Hold = model1.Hold;
            model.Total = model1.Total;
            model.Inprogress = model1.Inprogress;
            
            return View("Dashboard", model);
        }

        public ActionResult Dashboard()
        {
            Dashboard model = new Dashboard();
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
            HttpCookie reqCookies1 = Request.Cookies["userInfo1"];
            if (reqCookies != null)
            {
                var username = reqCookies["UserName"].ToString();
                ConnectionHandler con = new ConnectionHandler();



                DataTable dtRec = new DataTable();
                string sqlQuery = "execute SPGetEmployee 1,1," + username + ",'','3'";
                dtRec = con.executeSelect(sqlQuery);

                ViewBag.username = dtRec.Rows[0]["Empname"].ToString();
                Session["Empname"] = dtRec.Rows[0]["Empname"].ToString();
                dtRec = new DataTable();
                sqlQuery = "[ezbusdb].[SPEmployeeTimeSheet] " + username + ",'','',3";
                dtRec = con.executeSelect(sqlQuery);
                if (dtRec.Rows.Count > 0)
                {
                    if (dtRec.Rows[0]["StartTime"].ToString() == "")
                    {
                        model.check = false;
                    }
                    else
                    {
                        if (dtRec.Rows[0]["EndTime"].ToString() == "")
                        {
                            model.check = true;
                        }
                        else
                        {
                            model.check = false;
                        }
                    }
                }
                else
                {
                    model.check = false;

                }



            }



            request = new APICall();
            string jsonData = request.callAPI("/Api/Job/GetDashbordCount", "GET", accessToken);
            var a = JsonConvert.DeserializeObject<List<DashboardList>>(jsonData.ToString());
            var model1 = JsonConvert.DeserializeObject<Dashboard>(jsonData.ToString().Replace("[", "").Replace("]", ""));
            model.Opn = model1.Opn;
            model.Closed = model1.Closed;
            model.Hold = model1.Hold;
            model.Total = model1.Total;
            model.Inprogress = model1.Inprogress;

            return View(model);
        }
    }
}