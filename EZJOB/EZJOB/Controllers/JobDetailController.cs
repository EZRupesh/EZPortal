using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DBAccess;
using EZJOB.Models;
using Newtonsoft.Json;

namespace EZJOB.Controllers
{
    public class JobDetailController : Controller
    {
        // GET: JobDetail
        APICall request;
        private EZJob _context;
        public ActionResult JobDetail(string id)
        
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
            string jsonData = request.callAPI("/api/Job/GetAllJobDetailsid?id=" + id, "GET", accessToken);
            model.list = JsonConvert.DeserializeObject<List<CustomerJob>>(jsonData.ToString());

            Session["WorkId"] = model.list[0].JobId;
            int jid = Int32.Parse(id);
            using (var db = new EZJob())
            {
                var pausedList = (from c in db.PausedDetails
                                  where c.JobId == jid && c.PausedDate != null
                                  select c).ToList();
                model.Pausedlist = pausedList;
            }

            using (var db = new EZJob())
            {
                var pausedList = (from c in db.PausedDetails
                                  where c.JobId == jid && c.ResumeDate != null
                                  select c).ToList();
                model.Resumedlist = pausedList;
            }
            DataTable dtRec;

            ConnectionHandler con = new ConnectionHandler();

            dtRec = new DataTable();
            string sqlQuery1 = "execute ezbusdb.SPGetItemDetails '" + id + "'";
            dtRec = con.executeSelect(sqlQuery1);

            List<ItemDetails> list = new List<ItemDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new ItemDetails();
                obj.id = Convert.ToInt32(row["id"]);
                obj.PortalId = Convert.ToInt32(row["PortalId"]);
                obj.Qty = row["Qty"].ToString();
                obj.Specification = row["Specification"].ToString();
                list.Add(obj);
            }

            model.ItemList = list;

            dtRec = new DataTable();
            string sqlQuery11 = "execute ezbusdb.SPGetFileDetails '" + id + "'";
            dtRec = con.executeSelect(sqlQuery11);

            List<FileDetails> list1 = new List<FileDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new FileDetails();
                obj.id = Convert.ToInt32(row["id"]);
                obj.PortalId = Convert.ToInt32(row["PortalId"]);
                obj.filename = row["filename"].ToString();

                list1.Add(obj);
            }
            model.FileList = list1;

            dtRec = new DataTable();
            string sqlQuery12 = "select* from FMAssetMasterDetail";
            dtRec = con.executeSelect(sqlQuery12);
            List<AssetDetails> list12 = new List<AssetDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new AssetDetails();
                obj.AssetID = row["AssetID"].ToString();
                obj.Description = row["Description"].ToString();


                list12.Add(obj);
            }
            model.AssetList = list12;

            var JobId = Session["WorkId"];

            dtRec = new DataTable();
            string sqlQuery13 = "select * from fmworkorderheader where code='" + JobId + "'";
            dtRec = con.executeSelect(sqlQuery13);
            List<WorkOrderHeaderDetails> list13 = new List<WorkOrderHeaderDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new WorkOrderHeaderDetails();
                obj.Code = row["Code"].ToString();
                obj.AssetId = row["AssetId"].ToString();

                obj.AssetDescription = row["AssetDescription"].ToString();
                list13.Add(obj);
            }
            model.WorkOrderHeader = list13;
            return View("JobDetail", model);
        }
        public ActionResult StartJobDetail(string id)
        {
            int Jobid = Int32.Parse(id);
            using (_context = new EZJob())
            {
                var result = _context.CustomerJobs.SingleOrDefault(j => j.Id == Jobid);
                result.JobStartDate = DateTime.Now;
                result.JobStatus = "Start";
                result.Status = "Inprogress";
                _context.SaveChanges();

            }

            ConnectionHandler con11 = new ConnectionHandler();

            bool status = true;
            string message = "Your password is send to your register email id. ";


            DataTable dtRec222 = new DataTable();
            string sqlQuery222 = "select JobId from [ezbusdb].[CustomerJob] where id='"+id+"'";
            dtRec222 = con11.executeSelect(sqlQuery222);

            DataTable dtRec111 = new DataTable();
            string sqlQuery111 = "update [ezbusdb].FMWorkOrderHeader set wostatus='Inprogress' where code='"+dtRec222.Rows[0][0].ToString()+"'";
            dtRec111 = con11.executeSelect(sqlQuery111);


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
            string jsonData = request.callAPI("/api/Job/GetAllJobDetailsid?id=" + id, "GET", accessToken);
            model.list = JsonConvert.DeserializeObject<List<CustomerJob>>(jsonData.ToString());
            //  return RedirectToAction("Login", "Login");

            DataTable dtRec;

            ConnectionHandler con = new ConnectionHandler();

            dtRec = new DataTable();
            string sqlQuery1 = "execute ezbusdb.SPGetItemDetails '" + id + "'";
            dtRec = con.executeSelect(sqlQuery1);

            List<ItemDetails> list = new List<ItemDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new ItemDetails();
                obj.id = Convert.ToInt32(row["id"]);
                obj.PortalId = Convert.ToInt32(row["PortalId"]);
                obj.Qty = row["Qty"].ToString();
                obj.Specification = row["Specification"].ToString();
                list.Add(obj);
            }

            model.ItemList = list;

            dtRec = new DataTable();
            string sqlQuery11 = "execute ezbusdb.SPGetFileDetails '" + id + "'";
            dtRec = con.executeSelect(sqlQuery11);

            List<FileDetails> list1 = new List<FileDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new FileDetails();
                obj.id = Convert.ToInt32(row["id"]);
                obj.PortalId = Convert.ToInt32(row["PortalId"]);
                obj.filename = row["filename"].ToString();

                list1.Add(obj);
            }
            model.FileList = list1;

            dtRec = new DataTable();
            string sqlQuery12 = "select* from FMAssetMasterDetail";
            dtRec = con.executeSelect(sqlQuery12);
            List<AssetDetails> list12 = new List<AssetDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new AssetDetails();
                obj.AssetID = row["AssetID"].ToString();
                obj.Description = row["Description"].ToString();


                list12.Add(obj);
            }
            model.AssetList = list12;

            var JobId = Session["WorkId"];

            dtRec = new DataTable();
            string sqlQuery13 = "select * from fmworkorderheader where code='" + JobId + "'";
            dtRec = con.executeSelect(sqlQuery13);
            List<WorkOrderHeaderDetails> list13 = new List<WorkOrderHeaderDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new WorkOrderHeaderDetails();
                obj.Code = row["Code"].ToString();
                obj.AssetId = row["AssetId"].ToString();

                obj.AssetDescription = row["AssetDescription"].ToString();
                list13.Add(obj);
            }
            model.WorkOrderHeader = list13;
            return View("JobDetail", model);

        }
        public ActionResult EndJobDetail(string id, string customerReview)
        {
            int Jobid = Int32.Parse(id);
            using (_context = new EZJob())
            {
                var result = _context.CustomerJobs.SingleOrDefault(j => j.Id == Jobid);
                result.JobEndDate = DateTime.Now;
                result.JobStatus = "End";
                result.Status = "Closed";
                result.CustomerReview = customerReview;
                _context.SaveChanges();

            }

            ConnectionHandler con11 = new ConnectionHandler();
            DataTable dtRec222 = new DataTable();
            string sqlQuery222 = "select JobId from [ezbusdb].[CustomerJob] where id='" + id + "'";
            dtRec222 = con11.executeSelect(sqlQuery222);

            DataTable dtRec111 = new DataTable();
            string sqlQuery111 = "update [ezbusdb].FMWorkOrderHeader set wostatus='Closed' where code='" + dtRec222.Rows[0][0].ToString() + "'";
            dtRec111 = con11.executeSelect(sqlQuery111);

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
            string jsonData = request.callAPI("/api/Job/GetAllJobDetailsid?id=" + id, "GET", accessToken);
            model.list = JsonConvert.DeserializeObject<List<CustomerJob>>(jsonData.ToString());
            int jid = Int32.Parse(id);
            using (var db = new EZJob())
            {
                var pausedList = (from c in db.PausedDetails
                                  where c.JobId == jid && c.PausedDate != null
                                  select c).ToList();
                model.Pausedlist = pausedList;
            }

            using (var db = new EZJob())
            {
                var pausedList = (from c in db.PausedDetails
                                  where c.JobId == jid && c.ResumeDate != null
                                  select c).ToList();
                model.Resumedlist = pausedList;
            }
            //  return RedirectToAction("Login", "Login");

            DataTable dtRec;

            ConnectionHandler con = new ConnectionHandler();

            dtRec = new DataTable();
            string sqlQuery1 = "execute ezbusdb.SPGetItemDetails '" + id + "'";
            dtRec = con.executeSelect(sqlQuery1);

            List<ItemDetails> list = new List<ItemDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new ItemDetails();
                obj.id = Convert.ToInt32(row["id"]);
                obj.PortalId = Convert.ToInt32(row["PortalId"]);
                obj.Qty = row["Qty"].ToString();
                obj.Specification = row["Specification"].ToString();
                list.Add(obj);
            }

            model.ItemList = list;

            dtRec = new DataTable();
            string sqlQuery11 = "execute ezbusdb.SPGetFileDetails '" + id + "'";
            dtRec = con.executeSelect(sqlQuery11);

            List<FileDetails> list1 = new List<FileDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new FileDetails();
                obj.id = Convert.ToInt32(row["id"]);
                obj.PortalId = Convert.ToInt32(row["PortalId"]);
                obj.filename = row["filename"].ToString();

                list1.Add(obj);
            }
            model.FileList = list1;
            dtRec = new DataTable();
            string sqlQuery12 = "select* from FMAssetMasterDetail";
            dtRec = con.executeSelect(sqlQuery12);
            List<AssetDetails> list12 = new List<AssetDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new AssetDetails();
                obj.AssetID = row["AssetID"].ToString();
                obj.Description = row["Description"].ToString();


                list12.Add(obj);
            }
            model.AssetList = list12;

            var JobId = Session["WorkId"];

            dtRec = new DataTable();
            string sqlQuery13 = "select * from fmworkorderheader where code='" + JobId + "'";
            dtRec = con.executeSelect(sqlQuery13);
            List<WorkOrderHeaderDetails> list13 = new List<WorkOrderHeaderDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new WorkOrderHeaderDetails();
                obj.Code = row["Code"].ToString();
                obj.AssetId = row["AssetId"].ToString();

                obj.AssetDescription = row["AssetDescription"].ToString();
                list13.Add(obj);
            }
            model.WorkOrderHeader = list13;
            return View("JobDetail", model);

        }


        public ActionResult HoldJobDetail(string id, string HoldRemark)
        {
            int Jobid = Int32.Parse(id);
            using (_context = new EZJob())
            {
                var result = _context.CustomerJobs.SingleOrDefault(j => j.Id == Jobid);

                result.JobStatus = "Paused";
                result.Status = "Paused";
                // result.HoldRemark = HoldRemark;
                _context.SaveChanges();
                _context.Dispose();

            }

            ConnectionHandler con11 = new ConnectionHandler();
            DataTable dtRec222 = new DataTable();
            string sqlQuery222 = "select JobId from [ezbusdb].[CustomerJob] where id='" + id + "'";
            dtRec222 = con11.executeSelect(sqlQuery222);

            DataTable dtRec111 = new DataTable();
            string sqlQuery111 = "update [ezbusdb].FMWorkOrderHeader set wostatus='Paused' where code='" + dtRec222.Rows[0][0].ToString() + "'";
            dtRec111 = con11.executeSelect(sqlQuery111);
            if (HoldRemark != "")
            {
                using (_context = new EZJob())
                {
                    PausedDetail obj = new PausedDetail();
                    obj.JobId = Int64.Parse(id);
                    obj.PausedDate = DateTime.Now;
                    obj.PausedRemark = HoldRemark;
                    _context.PausedDetails.Add(obj);
                    _context.SaveChanges();

                }
            }


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
            string jsonData = request.callAPI("/api/Job/GetAllJobDetailsid?id=" + id, "GET", accessToken);
            model.list = JsonConvert.DeserializeObject<List<CustomerJob>>(jsonData.ToString());

            int jid = Int32.Parse(id);
            using (var db = new EZJob())
            {
                var pausedList = (from c in db.PausedDetails
                                  where c.JobId == jid && c.PausedDate != null
                                  select c).ToList();
                model.Pausedlist = pausedList;
            }

            using (var db = new EZJob())
            {
                var pausedList = (from c in db.PausedDetails
                                  where c.JobId == jid && c.ResumeDate != null
                                  select c).ToList();
                model.Resumedlist = pausedList;
            }
            model.HoldRemark = "";
            //  return RedirectToAction("Login", "Login");

            DataTable dtRec;

            ConnectionHandler con = new ConnectionHandler();

            dtRec = new DataTable();
            string sqlQuery1 = "execute ezbusdb.SPGetItemDetails '" + id + "'";
            dtRec = con.executeSelect(sqlQuery1);

            List<ItemDetails> list = new List<ItemDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new ItemDetails();
                obj.id = Convert.ToInt32(row["id"]);
                obj.PortalId = Convert.ToInt32(row["PortalId"]);
                obj.Qty = row["Qty"].ToString();
                obj.Specification = row["Specification"].ToString();
                list.Add(obj);
            }

            model.ItemList = list;

            dtRec = new DataTable();
            string sqlQuery11 = "execute ezbusdb.SPGetFileDetails '" + id + "'";
            dtRec = con.executeSelect(sqlQuery11);

            List<FileDetails> list1 = new List<FileDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new FileDetails();
                obj.id = Convert.ToInt32(row["id"]);
                obj.PortalId = Convert.ToInt32(row["PortalId"]);
                obj.filename = row["filename"].ToString();

                list1.Add(obj);
            }
            model.FileList = list1;

            dtRec = new DataTable();
            string sqlQuery12 = "select* from FMAssetMasterDetail";
            dtRec = con.executeSelect(sqlQuery12);
            List<AssetDetails> list12 = new List<AssetDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new AssetDetails();
                obj.AssetID = row["AssetID"].ToString();
                obj.Description = row["Description"].ToString();


                list12.Add(obj);
            }
            model.AssetList = list12;

            var JobId = Session["WorkId"];

            dtRec = new DataTable();
            string sqlQuery13 = "select * from fmworkorderheader where code='" + JobId + "'";
            dtRec = con.executeSelect(sqlQuery13);
            List<WorkOrderHeaderDetails> list13 = new List<WorkOrderHeaderDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new WorkOrderHeaderDetails();
                obj.Code = row["Code"].ToString();
                obj.AssetId = row["AssetId"].ToString();

                obj.AssetDescription = row["AssetDescription"].ToString();
                list13.Add(obj);
            }
            model.WorkOrderHeader = list13;
            return View("JobDetail", model);

        }

        public ActionResult AddAssetDetails(string AssetDescription, string AssetId, string code,string id)
        {
            int Jobid = 0;

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
            DataTable dtRec;

            ConnectionHandler con = new ConnectionHandler();
            dtRec = new DataTable();
            string sqlQuery = "";

            sqlQuery = "update fmworkorderheader set AssetId='"+AssetId+ "',AssetDescription='"+AssetDescription+"' where Code='"+code+"'";


            dtRec = con.executeSelect(sqlQuery);

            dtRec = new DataTable();
            string sqlQuery1 = "execute ezbusdb.SPGetItemDetails '" + id + "'";
            dtRec = con.executeSelect(sqlQuery1);

            List<ItemDetails> list = new List<ItemDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new ItemDetails();
                obj.id = Convert.ToInt32(row["id"]);
                obj.PortalId = Convert.ToInt32(row["PortalId"]);
                obj.Qty = row["Qty"].ToString();
                obj.Specification = row["Specification"].ToString();
                list.Add(obj);
            }

            model.ItemList = list;

            dtRec = new DataTable();
            string sqlQuery11 = "execute ezbusdb.SPGetFileDetails '" + id + "'";
            dtRec = con.executeSelect(sqlQuery11);

            List<FileDetails> list1 = new List<FileDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new FileDetails();
                obj.id = Convert.ToInt32(row["id"]);
                obj.PortalId = Convert.ToInt32(row["PortalId"]);
                obj.filename = row["filename"].ToString();

                list1.Add(obj);
            }
            model.FileList = list1;

            dtRec = new DataTable();
            string sqlQuery12 = "select* from FMAssetMasterDetail";
            dtRec = con.executeSelect(sqlQuery12);
            List<AssetDetails> list12 = new List<AssetDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new AssetDetails();
                obj.AssetID = row["AssetID"].ToString();
                obj.Description = row["Description"].ToString();


                list12.Add(obj);
            }
            model.AssetList = list12;

            var JobId = Session["WorkId"];

            dtRec = new DataTable();
            string sqlQuery13 = "select * from fmworkorderheader where code='" + JobId + "'";
            dtRec = con.executeSelect(sqlQuery13);
            List<WorkOrderHeaderDetails> list13 = new List<WorkOrderHeaderDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new WorkOrderHeaderDetails();
                obj.Code = row["Code"].ToString();
                obj.AssetId = row["AssetId"].ToString();

                obj.AssetDescription = row["AssetDescription"].ToString();
                list13.Add(obj);
            }
            model.WorkOrderHeader = list13;

            request = new APICall();
            string jsonData = request.callAPI("/api/Job/GetAllJobDetailsid?id=" + id, "GET", accessToken);
            model.list = JsonConvert.DeserializeObject<List<CustomerJob>>(jsonData.ToString());
            return View("JobDetail", model);
        }
        public ActionResult AddItemDetails(string id, string qty, string description, string code, string updateid, string deleteid)
        {
            int Jobid = 0;
            if (id.Contains("update"))
            {
                string jid = id.Replace("update", "");
                Jobid = Int32.Parse(jid);

            }

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
            DataTable dtRec;

            ConnectionHandler con = new ConnectionHandler();
            dtRec = new DataTable();
            string sqlQuery = "";
            if (updateid != "")
            {
                sqlQuery = "execute ezbusdb.SPUpdateItemDetails '" + updateid + "','01','" + code + "','" + qty + "','" + description + "'";
            }
            else if (deleteid != "")
            {
                sqlQuery = "delete ezbusdb.FMWorkOrderMaterialDetail where Id=" + deleteid;
            }
            else
            {
                sqlQuery = "execute ezbusdb.SPAddItemDetails '" + id + "','01','" + code + "','" + qty + "','" + description + "'";
            }

            dtRec = con.executeSelect(sqlQuery);

            dtRec = new DataTable();
            string sqlQuery1 = "execute ezbusdb.SPGetItemDetails '" + id + "'";
            dtRec = con.executeSelect(sqlQuery1);

            List<ItemDetails> list = new List<ItemDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new ItemDetails();
                obj.id = Convert.ToInt32(row["id"]);
                obj.PortalId = Convert.ToInt32(row["PortalId"]);
                obj.Qty = row["Qty"].ToString();
                obj.Specification = row["Specification"].ToString();
                list.Add(obj);
            }

            model.ItemList = list;

            dtRec = new DataTable();
            string sqlQuery11 = "execute ezbusdb.SPGetFileDetails '" + id + "'";
            dtRec = con.executeSelect(sqlQuery11);

            List<FileDetails> list1 = new List<FileDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new FileDetails();
                obj.id = Convert.ToInt32(row["id"]);
                obj.PortalId = Convert.ToInt32(row["PortalId"]);
                obj.filename = row["filename"].ToString();

                list1.Add(obj);
            }
            model.FileList = list1;

            dtRec = new DataTable();
            string sqlQuery12 = "select* from FMAssetMasterDetail";
            dtRec = con.executeSelect(sqlQuery12);
            List<AssetDetails> list12 = new List<AssetDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new AssetDetails();
                obj.AssetID = row["AssetID"].ToString();
                obj.Description = row["Description"].ToString();


                list12.Add(obj);
            }
            model.AssetList = list12;

            var JobId = Session["WorkId"];

            dtRec = new DataTable();
            string sqlQuery13 = "select * from fmworkorderheader where code='" + JobId + "'";
            dtRec = con.executeSelect(sqlQuery13);
            List<WorkOrderHeaderDetails> list13 = new List<WorkOrderHeaderDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new WorkOrderHeaderDetails();
                obj.Code = row["Code"].ToString();
                obj.AssetId = row["AssetId"].ToString();

                obj.AssetDescription = row["AssetDescription"].ToString();
                list13.Add(obj);
            }
            model.WorkOrderHeader = list13;

            request = new APICall();
            string jsonData = request.callAPI("/api/Job/GetAllJobDetailsid?id=" + id, "GET", accessToken);
            model.list = JsonConvert.DeserializeObject<List<CustomerJob>>(jsonData.ToString());
            return View("JobDetail", model);
        }

        public ActionResult UpdateItemDetails(string id, string qty, string description, string code)
        {
            int Jobid = Int32.Parse(id);
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
            DataTable dtRec;

            ConnectionHandler con = new ConnectionHandler();
            dtRec = new DataTable();
            string sqlQuery = "execute ezbusdb.SPUpdateItemDetails '" + id + "','01','" + code + "','" + qty + "','" + description + "'";
            dtRec = con.executeSelect(sqlQuery);

            dtRec = new DataTable();
            string sqlQuery1 = "execute ezbusdb.SPGetItemDetails '" + id + "'";
            dtRec = con.executeSelect(sqlQuery1);

            List<ItemDetails> list = new List<ItemDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new ItemDetails();
                obj.id = Convert.ToInt32(row["id"]);
                obj.PortalId = Convert.ToInt32(row["PortalId"]);
                obj.Qty = row["Qty"].ToString();
                obj.Specification = row["Specification"].ToString();
                list.Add(obj);
            }

            model.ItemList = list;

            dtRec = new DataTable();
            string sqlQuery11 = "execute ezbusdb.SPGetFileDetails '" + id + "'";
            dtRec = con.executeSelect(sqlQuery11);

            List<FileDetails> list1 = new List<FileDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new FileDetails();
                obj.id = Convert.ToInt32(row["id"]);
                obj.PortalId = Convert.ToInt32(row["PortalId"]);
                obj.filename = row["filename"].ToString();

                list1.Add(obj);
            }
            model.FileList = list1;

            dtRec = new DataTable();
            string sqlQuery12 = "select* from FMAssetMasterDetail";
            dtRec = con.executeSelect(sqlQuery12);
            List<AssetDetails> list12 = new List<AssetDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new AssetDetails();
                obj.AssetID = row["AssetID"].ToString();
                obj.Description = row["Description"].ToString();


                list12.Add(obj);
            }
            model.AssetList = list12;

            var JobId = Session["WorkId"];

            dtRec = new DataTable();
            string sqlQuery13 = "select * from fmworkorderheader where code='" + JobId + "'";
            dtRec = con.executeSelect(sqlQuery13);
            List<WorkOrderHeaderDetails> list13 = new List<WorkOrderHeaderDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new WorkOrderHeaderDetails();
                obj.Code = row["Code"].ToString();
                obj.AssetId = row["AssetId"].ToString();

                obj.AssetDescription = row["AssetDescription"].ToString();
                list13.Add(obj);
            }
            model.WorkOrderHeader = list13;

            request = new APICall();
            string jsonData = request.callAPI("/api/Job/GetAllJobDetailsid?id=" + id, "GET", accessToken);
            model.list = JsonConvert.DeserializeObject<List<CustomerJob>>(jsonData.ToString());
            return View("JobDetail", model);
        }
        public ActionResult ResumeJobDetail(string id, string HoldRemark)
        {
            int Jobid = Int32.Parse(id);
            using (_context = new EZJob())
            {
                var result = _context.CustomerJobs.SingleOrDefault(j => j.Id == Jobid);

                result.JobStatus = "Resumed";
                result.Status = "Inprogress";
                //result.HoldRemark = HoldRemark;
                _context.SaveChanges();

            }

            ConnectionHandler con11 = new ConnectionHandler();
            DataTable dtRec222 = new DataTable();
            string sqlQuery222 = "select JobId from [ezbusdb].[CustomerJob] where id='" + id + "'";
            dtRec222 = con11.executeSelect(sqlQuery222);

            DataTable dtRec111 = new DataTable();
            string sqlQuery111 = "update [ezbusdb].FMWorkOrderHeader set wostatus='Inprogress' where code='" + dtRec222.Rows[0][0].ToString() + "'";
            dtRec111 = con11.executeSelect(sqlQuery111);

            using (_context = new EZJob())
            {
                PausedDetail obj = new PausedDetail();
                obj.JobId = Int64.Parse(id);
                obj.ResumeDate = DateTime.Now;
                obj.ResumeRemark = HoldRemark;
                _context.PausedDetails.Add(obj);
                _context.SaveChanges();




            }

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
            string jsonData = request.callAPI("/api/Job/GetAllJobDetailsid?id=" + id, "GET", accessToken);
            model.list = JsonConvert.DeserializeObject<List<CustomerJob>>(jsonData.ToString());

            int jid = Int32.Parse(id);
            using (var db = new EZJob())
            {
                var pausedList = (from c in db.PausedDetails
                                  where c.JobId == jid && c.PausedDate != null
                                  select c).ToList();
                model.Pausedlist = pausedList;
            }

            using (var db = new EZJob())
            {
                var pausedList = (from c in db.PausedDetails
                                  where c.JobId == jid && c.ResumeDate != null
                                  select c).ToList();
                model.Resumedlist = pausedList;
            }
            //  return RedirectToAction("Login", "Login");

            DataTable dtRec;

            ConnectionHandler con = new ConnectionHandler();

            dtRec = new DataTable();
            string sqlQuery1 = "execute ezbusdb.SPGetItemDetails '" + id + "'";
            dtRec = con.executeSelect(sqlQuery1);

            List<ItemDetails> list = new List<ItemDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new ItemDetails();
                obj.id = Convert.ToInt32(row["id"]);
                obj.PortalId = Convert.ToInt32(row["PortalId"]);
                obj.Qty = row["Qty"].ToString();
                obj.Specification = row["Specification"].ToString();
                list.Add(obj);
            }

            model.ItemList = list;

            dtRec = new DataTable();
            string sqlQuery11 = "execute ezbusdb.SPGetFileDetails '" + id + "'";
            dtRec = con.executeSelect(sqlQuery11);

            List<FileDetails> list1 = new List<FileDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new FileDetails();
                obj.id = Convert.ToInt32(row["id"]);
                obj.PortalId = Convert.ToInt32(row["PortalId"]);
                obj.filename = row["filename"].ToString();

                list1.Add(obj);
            }
            model.FileList = list1;
            dtRec = new DataTable();
            string sqlQuery12 = "select* from FMAssetMasterDetail";
            dtRec = con.executeSelect(sqlQuery12);
            List<AssetDetails> list12 = new List<AssetDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new AssetDetails();
                obj.AssetID = row["AssetID"].ToString();
                obj.Description = row["Description"].ToString();


                list12.Add(obj);
            }
            model.AssetList = list12;

            var JobId = Session["WorkId"];

            dtRec = new DataTable();
            string sqlQuery13 = "select * from fmworkorderheader where code='" + JobId + "'";
            dtRec = con.executeSelect(sqlQuery13);
            List<WorkOrderHeaderDetails> list13 = new List<WorkOrderHeaderDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new WorkOrderHeaderDetails();
                obj.Code = row["Code"].ToString();
                obj.AssetId = row["AssetId"].ToString();

                obj.AssetDescription = row["AssetDescription"].ToString();
                list13.Add(obj);
            }
            model.WorkOrderHeader = list13;
            return View("JobDetail", model);

        }


        [HttpPost]
        public JsonResult EditItem(FormCollection formCollection)
        {
            int id = int.Parse(formCollection["id"]);


            return Json("");
        }
        [HttpPost]
        public ActionResult Upload(FormCollection formCollection)
        {
            JobDetails model = new JobDetails();

            var file1 = System.Web.HttpContext.Current.Request.Files[0];
            int id = int.Parse(formCollection["id"]);
            string _FileName = Path.GetFileName(file1.FileName);
            string _path = Path.Combine(Server.MapPath("~/UploadedFiles"), _FileName);
            file1.SaveAs(_path);

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
            string jsonData = request.callAPI("/api/Job/GetAllJobDetailsid?id=" + id, "GET", accessToken);
            model.list = JsonConvert.DeserializeObject<List<CustomerJob>>(jsonData.ToString());
            int jid = id;
            using (var db = new EZJob())
            {
                var pausedList = (from c in db.PausedDetails
                                  where c.JobId == jid && c.PausedDate != null
                                  select c).ToList();
                model.Pausedlist = pausedList;
            }

            using (var db = new EZJob())
            {
                var pausedList = (from c in db.PausedDetails
                                  where c.JobId == jid && c.ResumeDate != null
                                  select c).ToList();
                model.Resumedlist = pausedList;
            }

            DataTable dtRec;

            ConnectionHandler con = new ConnectionHandler();
            dtRec = new DataTable();
            string sqlQuery = "execute ezbusdb.SPAdFileDetails '" + id + "','" + _FileName + "'";
            dtRec = con.executeSelect(sqlQuery);

            dtRec = new DataTable();
            string sqlQuery1 = "execute ezbusdb.SPGetFileDetails '" + id + "'";
            dtRec = con.executeSelect(sqlQuery1);

            List<FileDetails> list1 = new List<FileDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new FileDetails();
                obj.id = Convert.ToInt32(row["id"]);
                obj.PortalId = Convert.ToInt32(row["PortalId"]);
                obj.filename = row["filename"].ToString();

                list1.Add(obj);
            }
            model.FileList = list1;


            dtRec = new DataTable();
            string sqlQuery2 = "execute ezbusdb.SPGetItemDetails '" + id + "'";
            dtRec = con.executeSelect(sqlQuery2);

            List<ItemDetails> list = new List<ItemDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new ItemDetails();
                obj.id = Convert.ToInt32(row["id"]);
                obj.PortalId = Convert.ToInt32(row["PortalId"]);
                obj.Qty = row["Qty"].ToString();
                obj.Specification = row["Specification"].ToString();
                list.Add(obj);
            }

            model.ItemList = list;

            dtRec = new DataTable();
            string sqlQuery12 = "select* from FMAssetMasterDetail";
            dtRec = con.executeSelect(sqlQuery12);
            List<AssetDetails> list12 = new List<AssetDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new AssetDetails();
                obj.AssetID = row["AssetID"].ToString();
                obj.Description = row["Description"].ToString();


                list12.Add(obj);
            }
            model.AssetList = list12;

            var JobId = Session["WorkId"];

            dtRec = new DataTable();
            string sqlQuery13 = "select * from fmworkorderheader where code='" + JobId + "'";
            dtRec = con.executeSelect(sqlQuery13);
            List<WorkOrderHeaderDetails> list13 = new List<WorkOrderHeaderDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new WorkOrderHeaderDetails();
                obj.Code = row["Code"].ToString();
                obj.AssetId = row["AssetId"].ToString();

                obj.AssetDescription = row["AssetDescription"].ToString();
                list13.Add(obj);
            }
            model.WorkOrderHeader = list13;
            return View("JobDetail", model);
        }
        public FileContentResult download(string name, string id)
        {
            if (id != "")
            {
                return null;
            }
            string _path = Path.Combine(Server.MapPath("~/UploadedFiles"), name);
            byte[] fileBytes = System.IO.File.ReadAllBytes(_path);
            string fileName = name;

            return File(fileBytes, System.Net.Mime.MediaTypeNames.Application.Octet, fileName);
        }

        public ActionResult FileDelete(string did, string id)
        {
            int Jobid = Int32.Parse(id);

            ConnectionHandler con1 = new ConnectionHandler();
            DataTable dtRec1 = new DataTable();
            string sqlQuery = "delete ezbusdb.FileDetails where id='" + did + "'";
            dtRec1 = con1.executeSelect(sqlQuery);

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
            string jsonData = request.callAPI("/api/Job/GetAllJobDetailsid?id=" + id, "GET", accessToken);
            model.list = JsonConvert.DeserializeObject<List<CustomerJob>>(jsonData.ToString());

            int jid = Int32.Parse(id);
            using (var db = new EZJob())
            {
                var pausedList = (from c in db.PausedDetails
                                  where c.JobId == jid && c.PausedDate != null
                                  select c).ToList();
                model.Pausedlist = pausedList;
            }

            using (var db = new EZJob())
            {
                var pausedList = (from c in db.PausedDetails
                                  where c.JobId == jid && c.ResumeDate != null
                                  select c).ToList();
                model.Resumedlist = pausedList;
            }
            //  return RedirectToAction("Login", "Login");

            DataTable dtRec;

            ConnectionHandler con = new ConnectionHandler();

            dtRec = new DataTable();
            string sqlQuery1 = "execute ezbusdb.SPGetItemDetails '" + id + "'";
            dtRec = con.executeSelect(sqlQuery1);

            List<ItemDetails> list = new List<ItemDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new ItemDetails();
                obj.id = Convert.ToInt32(row["id"]);
                obj.PortalId = Convert.ToInt32(row["PortalId"]);
                obj.Qty = row["Qty"].ToString();
                obj.Specification = row["Specification"].ToString();
                list.Add(obj);
            }

            model.ItemList = list;

            dtRec = new DataTable();
            string sqlQuery11 = "execute ezbusdb.SPGetFileDetails '" + id + "'";
            dtRec = con.executeSelect(sqlQuery11);

            List<FileDetails> list1 = new List<FileDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new FileDetails();
                obj.id = Convert.ToInt32(row["id"]);
                obj.PortalId = Convert.ToInt32(row["PortalId"]);
                obj.filename = row["filename"].ToString();

                list1.Add(obj);
            }
            model.FileList = list1;
            dtRec = new DataTable();
            string sqlQuery12 = "select* from FMAssetMasterDetail";
            dtRec = con.executeSelect(sqlQuery12);
            List<AssetDetails> list12 = new List<AssetDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new AssetDetails();
                obj.AssetID = row["AssetID"].ToString();
                obj.Description = row["Description"].ToString();


                list12.Add(obj);
            }
            model.AssetList = list12;

            var JobId = Session["WorkId"];

            dtRec = new DataTable();
            string sqlQuery13 = "select * from fmworkorderheader where code='" + JobId + "'";
            dtRec = con.executeSelect(sqlQuery13);
            List<WorkOrderHeaderDetails> list13 = new List<WorkOrderHeaderDetails>();
            // Iterates through each row within the data table
            foreach (DataRow row in dtRec.Rows)
            {
                var obj = new WorkOrderHeaderDetails();
                obj.Code = row["Code"].ToString();
                obj.AssetId = row["AssetId"].ToString();

                obj.AssetDescription = row["AssetDescription"].ToString();
                list13.Add(obj);
            }
            model.WorkOrderHeader = list13;


            return View("JobDetail", model);

        }

        public static List<ItemDetails> DataTableToList(DataTable dt)
        {
            var list = new List<ItemDetails>();

            foreach (DataRow row in dt.Rows)
            {
                var obj = new ItemDetails
                {
                    PortalId = (int)row["PortalId"],
                    Qty = (string)row["Qty"],
                    Specification = (string)row["Specification"],
                    id = (int)row["id"]
                };

                list.Add(obj);
            }

            return list;
        }
    }
}