using ADQFAMS.API.Masters;
using ADQFAMS.Common;
using ADQFAMS.Common.Lookups;
using ADQFAMS.Data.Interfaces;
using ADQFAMS.Data.Interfaces.Company;
using ADQFAMS.Resources;
using ADQFAMS.ViewModels.Branch;
using ADQFAMS.ViewModels.WEBAPIModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Http;
using ADQFAMS.API.Import;
using Newtonsoft.Json.Linq;
using ADQFAMS.ViewModels.Location;
using ADQFAMS.Data.Interfaces.Locations;
using ADQFAMS.ViewModels.Department;
using ADQFAMS.Data.Interfaces.Department;
using ADQFAMS.ViewModels.CostCenter;
using ADQFAMS.Data.Interfaces.CostCenter;
using ADQFAMS.ViewModels.Vendor;
using ADQFAMS.Data.Interfaces.Vendor;
using StructureMap;
using ADQFAMS.Data.Interfaces.Asset;
using ADQFAMS.ViewModels.Import;
using ADQFAMS.Common.Vendor;
using ADQFAMS.ViewModels.Asset;
using ADQFAMS.Web.Controllers;
using ADQFAMS.Data.Interfaces.Consumable;
using ADQFAMS.ViewModels.User;
using ADQFAMS.ViewModels.Common;
using ADQFAMS.Data.Interfaces.User;
using Hangfire;
using ADQFAMS.Data.Interfaces.Workflow;
using ADQFAMS.ViewModels.Consumable;
using ADQFAMS.Common.ApplicationEnums;
using ADQFAMS.Data.Interfaces.Category;
using ADQFAMS.Web.Attributes;
using System.IO;
using ADQFAMS.ViewModels.Organization;
using ADQFAMS.Common.User;
using ADQFAMS.ViewModels;
using ADQFAMS.Common.Organization;
using ADQFAMS.ViewModels.AssetCategory;
using ADQFAMS.API;
using System.Web.Http.Cors;
using ADQFAMS.ViewModels.Procurement;
using System.Xml.Linq;
using System.Web.Script.Serialization;
using ADQFAMS.Common.Asset;
//using System.Web.Mvc;

namespace ADQFAMS.Web.APIs
{
    [Authorize]
    //[EnableCors(origins: "*", headers: "*", methods: "*")]
    public class MastersController : ApiController
    {
        private ICompany _Company;
        private ILocationsService _location;
        private IDepartmentService _department;
        private ICostCenterService _costcenter;
        private IVendor _vendor;
        private static IUser _user;

        private static MastersAPI _masterApi;
        private static ImportAPI _importApi;
        private CompanyAPI _companyApi;

        private BaseInterface _baseInterface;
        private static IAssetService _iAssetService;
        private static IConsumable _consumable;
        private static ICategoryService _iCategoryService;

        public Regex regexSpecialCharacters = new Regex(@"^[a-z0-9A-Z-/\\_]*$");
        public Regex regexSpecialCharacters4 = new Regex("^[a-zA-Z0-9 '!@():;<>#$%&'*+/=?^_`{|}~.-]*$");
        public Regex regexSpecialCharacters2 = new Regex(@"^[a-z0-9A-Z-/\\_&]*$");

        private const string Item_Level1_Category = "Level1 category";
        private const string Item_Level2_Category = "Level2 category";
        private const string Unit_Measure = "Unit of measure";
        private const string Item_Name = "Item Name";
        private const string Item_Description = "Description";
        private const string Item_Reorder_Level = "Reorder level";
        private const string Item_Code = "Code";

        private static ImportController _importCon;

        private UserManagementAPI _userManagementApi;  //Added by Priyanka B on 24062024 for ServiceDeskAPI
        private AssetCategoryAPI _assetCategoryApi;  //Added by Priyanka B on 29062024 for AssetCategoryAPI

        //public MastersController(ICompany company, BaseInterface baseInterface, ILocationsService location, IDepartmentService department, ICostCenterService costcenter, IConsumable consumableService, IWorkFlow workflow, ICategoryService categoryservice)  //Commented by Priyanka B on 24062024 for ServiceDeskAPI
        public MastersController(ICompany company, BaseInterface baseInterface, ILocationsService location, IDepartmentService department, ICostCenterService costcenter, IConsumable consumableService, IWorkFlow workflow, ICategoryService categoryservice, IUser iuser)  //Modified by Priyanka B on 24062024 for ServiceDeskAPI
        {
            _baseInterface = baseInterface;
            _Company = company;
            _location = location;
            _department = department;
            _costcenter = costcenter;
            _consumable = consumableService;
            _iCategoryService = categoryservice;
            _user = iuser; //Added by Priyanka B on 24062024 for ServiceDeskAPI

            baseInterface.ICompany = _Company;
            baseInterface.ILocation = _location;
            baseInterface.IDepartmentService = _department;
            baseInterface.ICostCenter = _costcenter;
            _baseInterface.ICategoryService = _iCategoryService;
            _baseInterface.IUser = _user;  //Added by Priyanka B on 24062024 for ServiceDeskAPI

            _masterApi = new MastersAPI(baseInterface);
            _importApi = new ImportAPI(baseInterface);
            _companyApi = new CompanyAPI(_baseInterface);
            _assetCategoryApi = new AssetCategoryAPI(baseInterface);  //Added by Priyanka B on 29062024 for AssetCategoryAPI

            _iAssetService = ObjectFactory.GetInstance<IAssetService>();
            _vendor = ObjectFactory.GetInstance<IVendor>();
            _user = ObjectFactory.GetInstance<IUser>();

            
            //_importCon = new Controllers.ImportController(workflow);//commented by hamraj for SR014894
            _importCon = new Controllers.ImportController(workflow, consumableService);//added by hamraj for SR014894

            _userManagementApi = new UserManagementAPI(baseInterface);  //Added by Priyanka B on 24062024 for ServiceDeskAPI
        }

        #region Add API Methods
        #region Organization API
        //Add Organization added by hamraj start
        [HttpPost]
        public HttpResponseMessage AddOrganizationDetails(OrganizationAPIViewModel submitData)
        {
            var json = JsonConvert.SerializeObject(submitData.OrganizationDetails);
            DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);

            long companyId, userId;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());

            bool status = true;
            string message = "";

            if (excelDt == null)
            {
                status = false;
                message = "Json OrganizationDetails not found for import.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }

            var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();

            int VendorTypeID = 0;
            try
            {
                if (ValidateOrgExcelColumns(colsList, GetOrganizationColumnsList()))
                {
                    if (excelDt.Rows.Count != 0)
                    {
                        DataTable dttable = ValidateImportAddOrganization(excelDt, companyId);
                        if (dttable.Rows.Count == 0)
                        {
                            foreach (DataRow dr in excelDt.Rows)
                            {
                                var orgTypeStr = dr["OrganizationType"].ToString().Trim();
                                var orgTypes = _Company.GetOrgTypes();
                                var orgType = orgTypes.FirstOrDefault(o => o.Text.Trim().Equals(orgTypeStr, StringComparison.OrdinalIgnoreCase));
                                //var orgType = orgTypes.Where(o => o.Text.Trim().Equals(orgTypeStr, StringComparison.OrdinalIgnoreCase));
                                var chkOrg = _Company.CheckOrgNameExists(dr["OrganizationName"].ToString().Trim());
                                var countryName = dr["CountryName"].ToString().Trim();
                                var country = _Company.GetCountires()
                                    .FirstOrDefault(c => c.Text.Equals(countryName, StringComparison.OrdinalIgnoreCase));
                                //var country = _Company.GetCountires().Where(c => c.Text.Equals(countryName, StringComparison.OrdinalIgnoreCase));

                                if (chkOrg == false)
                                {
                                    var orgObj = new OrganizationDetailsModel
                                    {
                                        OrganizationName = dr["OrganizationName"].ToString().Trim(),
                                        OrganizationKnownAs = dr["OrganizationKnownAs"].ToString().Trim(),
                                        OrganizationType = Convert.ToInt32(orgType.Value),
                                        OrgDomain = dr["OrganizationDomain"].ToString().Trim(),
                                        PanNumber = dr["PanNumber"].ToString().Trim(),
                                        AddressLine1 = dr["AddressLine1"].ToString().Trim(),
                                        AddressLine2 = dr["AddressLine2"].ToString().Trim(),
                                        City = dr["City"].ToString().Trim(),
                                        State = dr["State"].ToString().Trim(),
                                        CountryID = Convert.ToInt32(country.Value),
                                        Zip = dr["ZipCode"].ToString().Trim(),
                                        OrganizationEmail = dr["OrganizationEmail"].ToString().Trim(),
                                        OrganizationPhone = dr["OrganizationPhone"].ToString().Trim(),
                                        Website = dr["Website"].ToString().Trim(),
                                        UserID = userId,
                                        SelectedCompanyId = companyId,
                                        ParentID = companyId,
                                    };

                                    long returnResilt = _Company.AddOrganization(orgObj);
                                }

                            }
                            status = true;
                            message = "Organization added successfully.";
                        }
                        else
                        {
                            string JSONresult;
                            status = false;
                            JSONresult = JsonConvert.SerializeObject(dttable, Formatting.Indented);
                            var response = Request.CreateResponse(HttpStatusCode.OK);
                            response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                            return response;
                        }
                    }
                    else
                    {
                        status = false;
                        message = "Json OrganizationDetails not found for import";
                    }
                }
                else
                {
                    status = false;
                    message = "Json format does not match with defined Organization.";

                }
            }
            catch (Exception ex)
            {
                status = false;
                message = ex.Message.ToString();
                message = !string.IsNullOrEmpty(message) ? message : ex.ToString();
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
        }

        [HttpPost]
        public HttpResponseMessage UpdateOrganizationDetails(OrganizationAPIViewModel submitData)
        {
            var json = JsonConvert.SerializeObject(submitData.OrganizationDetails);
            DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);

            long companyId, userId;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());

            bool status = true;
            string message = "";

            if (excelDt == null)
            {
                status = false;
                message = "Json OrganizationDetails not found for import.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }

            var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();

            int VendorTypeID = 0;
            try
            {
                if (ValidateOrgExcelColumns(colsList, GetOrganizationColumnsList()))
                {
                    if (excelDt.Rows.Count != 0)
                    {
                        DataTable dttable = ValidateImportUpdateOrganization(excelDt, companyId);
                        if (dttable.Rows.Count == 0)
                        {
                            foreach (DataRow dr in excelDt.Rows)
                            {
                                var orgTypeStr = dr["OrganizationType"].ToString().Trim();
                                var orgTypes = _Company.GetOrgTypes();
                                var orgType = orgTypes.FirstOrDefault(o => o.Text.Trim().Equals(orgTypeStr, StringComparison.OrdinalIgnoreCase));
                                var chkOrg = _Company.CheckOrgNameExists(dr["OrganizationName"].ToString().Trim());
                                var countryName = dr["CountryName"].ToString().Trim();
                                var country = _Company.GetCountires()
                                    .FirstOrDefault(c => c.Text.Equals(countryName, StringComparison.OrdinalIgnoreCase));

                                if (chkOrg == true)
                                {
                                    var OrgName = dr["OrganizationName"].ToString().Trim();

                                    var compId = _Company.GetCompaniesByName(OrgName);
                                    var compDet = _Company.GetOrganizationDetailsByCompId(compId[0].CompanyId);

                                    var orgObj = new OrganizationDetailsModel
                                    {
                                        OrganizationID = compId[0].CompanyId,
                                        OrganizationName = dr["OrganizationName"].ToString().Trim(),
                                        OrganizationKnownAs = dr["OrganizationKnownAs"].ToString().Trim(),
                                        OrganizationType = Convert.ToInt32(orgType.Value),
                                        OrgDomain = dr["OrganizationDomain"].ToString().Trim(),
                                        PanNumber = dr["PanNumber"].ToString().Trim(),
                                        AddressLine1 = dr["AddressLine1"].ToString().Trim(),
                                        AddressLine2 = dr["AddressLine2"].ToString().Trim(),
                                        City = dr["City"].ToString().Trim(),
                                        State = dr["State"].ToString().Trim(),
                                        CountryID = Convert.ToInt32(country.Value),
                                        Zip = dr["ZipCode"].ToString().Trim(),
                                        OrganizationEmail = dr["OrganizationEmail"].ToString().Trim(),
                                        OrganizationPhone = dr["OrganizationPhone"].ToString().Trim(),
                                        Website = dr["Website"].ToString().Trim(),
                                        UserID = compDet.UserID,
                                        SelectedCompanyId = compDet.SelectedCompanyId,
                                        ParentID = compDet.ParentID
                                    };

                                    int result = _Company.UpdateOrganization(orgObj);
                                }
                            }
                            status = true;
                            message = "Organization Updated successfully.";
                        }
                        else
                        {
                            string JSONresult;
                            status = false;
                            JSONresult = JsonConvert.SerializeObject(dttable, Formatting.Indented);
                            var response = Request.CreateResponse(HttpStatusCode.OK);
                            response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                            return response;
                        }
                    }
                    else
                    {
                        status = false;
                        message = "Json OrganizationDetails not found for import";
                    }
                }
                else
                {
                    status = false;
                    message = "Json format does not match with defined Organization.";

                }
            }
            catch (Exception ex)
            {
                status = false;
                message = ex.Message.ToString();
                message = !string.IsNullOrEmpty(message) ? message : ex.ToString();
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
        }

        public List<OrganizationsViewModel> GetOrganizationColumnsList()
        {
            var OrganizationsViewModel = new List<OrganizationsViewModel>
                {
                    new OrganizationsViewModel{ Checked = true,Required = true,ColumnName = "OrganizationName",DisplayName = "Organization Name" ,ColumnDescription = "Organization Name",Attribute = "required",DropDown = false},
                    new OrganizationsViewModel{ Checked = true,Required = true,ColumnName = "OrganizationKnownAs",DisplayName = "Organization Known As" ,ColumnDescription = "Organization Known As",Attribute = "required",DropDown = false},
                    new OrganizationsViewModel{ Checked = true,Required = false,ColumnName = "OrganizationType",DisplayName = "Organization Type" ,ColumnDescription = "Organization Type",Attribute = "required",DropDown = false},
                    new OrganizationsViewModel{ Checked = true,Required = false,ColumnName = "OrganizationDomain",DisplayName = "Organization Domain" ,ColumnDescription = "Organization Domain",Attribute = "notreq",DropDown = false},
                    new OrganizationsViewModel{ Checked = true,Required = false,ColumnName = "PanNumber",DisplayName = "Reg / PAN" ,ColumnDescription = "Reg / PAN",Attribute = "notreq",DropDown = false},
                    new OrganizationsViewModel{ Checked = true,Required = false,ColumnName = "AddressLine1",DisplayName = "Address Line1" ,ColumnDescription = "Address Line1",Attribute = "required",DropDown = false},
                    new OrganizationsViewModel{ Checked = true,Required = false,ColumnName = "AddressLine2",DisplayName = "Address Line2" ,ColumnDescription = "Address Line2",Attribute = "notreq",DropDown = false},
                    new OrganizationsViewModel{ Checked = true,Required = false,ColumnName = "City",DisplayName = "City" ,ColumnDescription = "City",Attribute = "required",DropDown = false},
                    new OrganizationsViewModel{ Checked = true,Required = false,ColumnName = "State",DisplayName = "State" ,ColumnDescription = "State",Attribute = "required",DropDown = false},
                    new OrganizationsViewModel{ Checked = true,Required = false,ColumnName = "CountryName",DisplayName = "Country" ,ColumnDescription = "Country",Attribute = "required",DropDown = false},
                    new OrganizationsViewModel{ Checked = true,Required = false,ColumnName = "ZipCode",DisplayName = "Zip Code" ,ColumnDescription = "Zip Code",Attribute = "required",DropDown = false},
                    new OrganizationsViewModel{ Checked = true,Required = false,ColumnName = "OrganizationEmail",DisplayName = "Email Id" ,ColumnDescription = "Email Id",Attribute = "required",DropDown = false},
                    new OrganizationsViewModel{ Checked = true,Required = false,ColumnName = "OrganizationPhone",DisplayName = "Phone No" ,ColumnDescription = "Phone No",Attribute = "required",DropDown = false},
                    new OrganizationsViewModel{ Checked = true,Required = false,ColumnName = "Website",DisplayName = "Website" ,ColumnDescription = "Website",Attribute = "notreq",DropDown = false},
                };
            return OrganizationsViewModel;
        }

        bool ValidateOrgExcelColumns(List<string> cols, List<OrganizationsViewModel> actualColumns)
        {
            var frstCol = cols.FirstOrDefault();
            var listOfColumns = actualColumns;
            var dicsList = listOfColumns.Select(x => x.ColumnName).ToList();
            var mandatoryList = listOfColumns.Where(x => x.Required == true).Select(x => x.ColumnName).ToList();
            var result = false;
            for (int k = 0; k < mandatoryList.Count; k++)
            {
                result = true;
                if (!cols.Where(x => x.ToString() == mandatoryList[k]).Any())
                {
                    result = false;
                    break;
                }
            }
            if (result == false)
                return false;
            for (int j = 0; j < cols.Count; j++)
            {
                result = false;
                for (int i = 0; i < dicsList.Count; i++)
                {
                    if (dicsList[i] == cols[j])
                    {
                        result = true;
                        break;
                    }
                }
                if (result == false)
                    return false;
            }
            return true;
        }

        private DataTable ValidateImportAddOrganization(DataTable datatable, long companyId)
        {
            DataTable colserror = new DataTable();
            foreach (DataColumn dc in datatable.Columns)
                colserror.Columns.Add(dc.ColumnName);
            colserror.Columns.Add("Error Message");
            var orgKnownAsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var orgNameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var orgDomainSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var existingOrganizations = _Company.GetOrganizations(companyId).ToList();
            var chkOrgType = _Company.GetOrgTypes().ToList();
            var chkCountry = _Company.GetCountires().ToList();

            try
            {
                foreach (DataRow dr in datatable.Rows)
                {
                    string sErrorMsg = "";
                    int nMsgCnt = 0;

                    foreach (DataColumn dc in datatable.Columns)
                    {
                        if (dc.ColumnName == "OrganizationName")
                        {
                            if (dr["OrganizationName"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Organization Name should not be empty ";
                            }
                            else
                            {
                                if (orgNameSet.Contains(dr["OrganizationName"].ToString().Trim()))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + $". Organization Name '{dr["OrganizationName"].ToString().Trim()}' already exists in the import json.";
                                }
                                else
                                {
                                    orgNameSet.Add(dr["OrganizationName"].ToString().Trim());

                                    if (existingOrganizations != null)
                                    {
                                        var duplicateName = existingOrganizations.Any(o => o.Name != null && o.Name.Equals(dr["OrganizationName"].ToString().Trim(), StringComparison.OrdinalIgnoreCase));
                                        if (duplicateName)
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + $". Organization Name '{dr["OrganizationName"].ToString().Trim()}' already exists.";
                                        }
                                    }
                                }
                            }
                        }
                        else if (dc.ColumnName == "OrganizationKnownAs")
                        {
                            if (dr["OrganizationKnownAs"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Organization Known As should not be empty ";
                            }
                            else
                            {
                                if (orgKnownAsSet.Contains(dr["OrganizationKnownAs"].ToString().Trim()))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + $". Organization Known As '{dr["OrganizationKnownAs"].ToString().Trim()}' already exists in the import json.";
                                }
                                else
                                {
                                    orgKnownAsSet.Add(dr["OrganizationKnownAs"].ToString().Trim());
                                    if (existingOrganizations != null)
                                    {
                                        var duplicateKnownAs = existingOrganizations.Any(o => o.KnownAs != null && o.KnownAs.Equals(dr["OrganizationKnownAs"].ToString().Trim(), StringComparison.OrdinalIgnoreCase));
                                        if (duplicateKnownAs)
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + $". Organization Known As '{dr["OrganizationKnownAs"].ToString().Trim()}' already exists.";
                                        }
                                    }
                                }
                            }
                        }
                        else if (dc.ColumnName == "OrganizationType")
                        {
                            if (dr["OrganizationType"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Organization Type should not be empty ";
                            }
                            var duplicateOrgType = chkOrgType.Any(o => o.Text != null && o.Text.Equals(dr["OrganizationType"].ToString().Trim(), StringComparison.OrdinalIgnoreCase));
                            if (!duplicateOrgType)
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + $". Organization Type '{dr["OrganizationType"].ToString().Trim()}' not found.";
                            }
                        }
                        else if (dc.ColumnName == "OrganizationDomain")
                        {
                            var organizationDomain = dr["OrganizationDomain"].ToString().Trim();
                            var chkOrgName = dr["OrganizationName"].ToString().Trim();
                            var regex = new Regex(@"^[a-z0-9]+([\-\.]{1}[a-z0-9]+)*\.[a-z]{2,6}$", RegexOptions.IgnoreCase);
                            if (!regex.IsMatch(organizationDomain))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Invalid Organization Domain format.";
                            }
                            else
                            {
                                if (orgDomainSet.Contains(organizationDomain))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + $". Organization Domain '{organizationDomain}' already exists in the import json.";
                                }
                                else
                                {
                                    orgDomainSet.Add(organizationDomain);
                                    if (existingOrganizations != null)
                                    {
                                        var duplicateDomain = existingOrganizations.Any(o => o.OrgDomain != null && o.OrgDomain.Equals(organizationDomain, StringComparison.OrdinalIgnoreCase));
                                        var OrgName = existingOrganizations.Any(o => o.Name != null && o.Name.Equals(chkOrgName, StringComparison.OrdinalIgnoreCase));
                                        if (duplicateDomain && !OrgName)
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + $". Organization Domain '{organizationDomain}' already exists.";
                                        }
                                    }
                                }
                            }
                        }
                        else if (dc.ColumnName == "AddressLine1")
                        {
                            if (dr["AddressLine1"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Address Line1 should not be empty ";
                            }
                        }
                        else if (dc.ColumnName == "City")
                        {
                            if (dr["City"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". City should not be empty ";
                            }
                        }
                        else if (dc.ColumnName == "State")
                        {
                            if (dr["State"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". State should not be empty ";
                            }
                        }
                        else if (dc.ColumnName == "CountryName")
                        {
                            if (dr["CountryName"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Country Name should not be empty ";
                            }
                            var CountryChk = chkCountry.Any(o => o.Text != null && o.Text.Equals(dr["CountryName"].ToString().Trim(), StringComparison.OrdinalIgnoreCase));
                            if (!CountryChk)
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + $". Country Name '{dr["CountryName"].ToString().Trim()}' not found.";
                            }
                        }
                        else if (dc.ColumnName == "ZipCode")
                        {
                            if (dr["ZipCode"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Zip code should not be empty ";
                            }
                            var indiaReg = new Regex(@"^\d{6}$");
                            var alphanumericReg = new Regex(@"^[a-zA-Z0-9]+$");
                            var zipCode = dr["ZipCode"].ToString().Trim();
                            var country = dr["CountryName"].ToString().Trim();
                            if (country.Equals("INDIA", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!indiaReg.IsMatch(zipCode))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + $". Invalid ZIP code '{zipCode}' for country '{country}'.";
                                }
                            }
                            else
                            {
                                if (!alphanumericReg.IsMatch(zipCode))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + $". Invalid ZIP code '{zipCode}' for country '{country}'.";
                                }
                            }
                        }
                        else if (dc.ColumnName == "OrganizationEmail")
                        {
                            if (dr["OrganizationEmail"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Organization Email should not be empty ";
                            }
                            var emailValidationExpression = new Regex(@"\b[a-zA-Z0-9._%-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}\b", RegexOptions.IgnoreCase);
                            var email = dr["OrganizationEmail"].ToString().Trim();
                            if (!string.IsNullOrEmpty(email) && !emailValidationExpression.IsMatch(email))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + $". Invalid email address '{email}'.";
                            }
                        }
                        else if (dc.ColumnName == "OrganizationPhone")
                        {
                            if (dr["OrganizationPhone"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Organization Phone should not be empty ";
                            }
                            var phoneNumberExpression = new Regex(@"\d{10}$");
                            var phoneNumber = dr["OrganizationPhone"].ToString().Trim();
                            if (!string.IsNullOrEmpty(phoneNumber) && !phoneNumberExpression.IsMatch(phoneNumber))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + $". Invalid phone number '{phoneNumber}'.";
                            }
                        }
                        else if (dc.ColumnName == "Website")
                        {
                            var website = dr["Website"].ToString().Trim();
                            var websiteRegExpression = new Regex(@"(https?:\/\/(?:www\.|(?!www))[^\s\.]+\.[^\s]{2,}|www\.[^\s]+\.[^\s]{2,})", RegexOptions.IgnoreCase);

                            if (!string.IsNullOrEmpty(website) && !websiteRegExpression.IsMatch(website))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + $". Invalid website URL '{website}'.";
                            }
                        }
                    }

                    if (sErrorMsg != "")
                    {
                        DataRow errorRow = colserror.NewRow();
                        errorRow.ItemArray = dr.ItemArray;
                        errorRow["Error Message"] = sErrorMsg;
                        colserror.Rows.Add(errorRow);
                    }
                }

                return colserror;
            }
            finally
            {
                datatable?.Dispose();
            }
        }

        private DataTable ValidateImportUpdateOrganization(DataTable datatable, long companyId)
        {
            DataTable colserror = new DataTable();
            foreach (DataColumn dc in datatable.Columns)
                colserror.Columns.Add(dc.ColumnName);
            colserror.Columns.Add("Error Message");

            var existingOrganizations = _Company.GetOrganizations(companyId).ToList();
            var orgKnownAsSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var orgNameSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var orgDomainSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var chkOrgType = _Company.GetOrgTypes().ToList();
            var chkCountry = _Company.GetCountires().ToList();

            try
            {
                foreach (DataRow dr in datatable.Rows)
                {
                    string sErrorMsg = "";
                    int nMsgCnt = 0;

                    foreach (DataColumn dc in datatable.Columns)
                    {
                        if (dc.ColumnName == "OrganizationName")
                        {
                            string organizationName = dr["OrganizationName"].ToString().Trim();

                            if (string.IsNullOrWhiteSpace(organizationName))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Organization Name should not be empty ";
                            }
                            else
                            {
                                if (orgNameSet.Contains(organizationName))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + $". Organization Name '{organizationName}' already exists in the import json.";
                                }
                                else
                                {
                                    orgNameSet.Add(organizationName);

                                    if (existingOrganizations != null)
                                    {
                                        var duplicateName = existingOrganizations.Any(o => o.Name != null && o.Name.Equals(organizationName, StringComparison.OrdinalIgnoreCase));
                                        if (!duplicateName)
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + $". Organization Name '{organizationName}' not found.";
                                        }
                                    }
                                }
                            }
                        }
                        else if (dc.ColumnName == "OrganizationKnownAs")
                        {
                            string organizationKnownAs = dr["OrganizationKnownAs"].ToString().Trim();
                            string organizationName = dr["OrganizationName"].ToString().Trim();
                            if (string.IsNullOrWhiteSpace(organizationKnownAs))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Organization Known As should not be empty ";
                            }
                            else
                            {
                                if (orgKnownAsSet.Contains(organizationKnownAs))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + $". Organization Known As '{organizationKnownAs}' already exists in the import json.";
                                }
                                else
                                {
                                    orgKnownAsSet.Add(organizationKnownAs);
                                    var duplicateKnownAs = existingOrganizations.Any(o => o.KnownAs != null && o.KnownAs.Equals(organizationKnownAs, StringComparison.OrdinalIgnoreCase));
                                    var exactMatch = existingOrganizations.Any(o => o.Name.Trim().Equals(organizationName, StringComparison.OrdinalIgnoreCase) &&
                                                                                o.KnownAs != null &&
                                                                                o.KnownAs.Equals(organizationKnownAs, StringComparison.OrdinalIgnoreCase));

                                    if (duplicateKnownAs && !exactMatch)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + $". Organization Known As '{organizationKnownAs}' already exists.";
                                    }
                                }
                            }
                        }
                        else if (dc.ColumnName == "OrganizationType")
                        {
                            if (dr["OrganizationType"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Organization Type should not be empty ";
                            }
                            var duplicateOrgType = chkOrgType.Any(o => o.Text != null && o.Text.Equals(dr["OrganizationType"].ToString().Trim(), StringComparison.OrdinalIgnoreCase));
                            if (!duplicateOrgType)
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + $". Organization Type '{dr["OrganizationType"].ToString().Trim()}' not found.";
                            }
                        }
                        else if (dc.ColumnName == "OrganizationDomain")
                        {
                            var organizationDomain = dr["OrganizationDomain"].ToString().Trim();
                            var chkOrgName = dr["OrganizationName"].ToString().Trim();
                            var regex = new Regex(@"^[a-z0-9]+([\-\.]{1}[a-z0-9]+)*\.[a-z]{2,6}$", RegexOptions.IgnoreCase);
                            if (!regex.IsMatch(organizationDomain))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Invalid Organization Domain format.";
                            }
                            else
                            {
                                if (orgDomainSet.Contains(organizationDomain))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + $". Organization Domain '{organizationDomain}' already exists in the import json.";
                                }
                                else
                                {
                                    orgDomainSet.Add(organizationDomain);

                                    if (existingOrganizations != null)
                                    {
                                        var duplicateDomain = existingOrganizations.Any(o => o.OrgDomain != null && o.OrgDomain.Equals(organizationDomain, StringComparison.OrdinalIgnoreCase));
                                        var orgNameExists = existingOrganizations.Any(o => o.Name != null && o.Name.Equals(chkOrgName, StringComparison.OrdinalIgnoreCase));

                                        if (duplicateDomain && !orgNameExists)
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + $". Organization Domain '{organizationDomain}' already exists.";
                                        }
                                    }
                                }
                            }
                        }
                        else if (dc.ColumnName == "AddressLine1")
                        {
                            if (dr["AddressLine1"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Address Line1 should not be empty ";
                            }
                        }
                        else if (dc.ColumnName == "City")
                        {
                            if (dr["City"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". City should not be empty ";
                            }
                        }
                        else if (dc.ColumnName == "State")
                        {
                            if (dr["State"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". State should not be empty ";
                            }
                        }
                        else if (dc.ColumnName == "CountryName")
                        {
                            if (dr["CountryName"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Country Name should not be empty ";
                            }
                            var CountryChk = chkCountry.Any(o => o.Text != null && o.Text.Equals(dr["CountryName"].ToString().Trim(), StringComparison.OrdinalIgnoreCase));
                            if (!CountryChk)
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + $". Country Name '{dr["CountryName"].ToString().Trim()}' not found.";
                            }
                        }
                        else if (dc.ColumnName == "ZipCode")
                        {
                            if (dr["ZipCode"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Zip code should not be empty ";
                            }
                            var indiaReg = new Regex(@"^\d{6}$");
                            var alphanumericReg = new Regex(@"^[a-zA-Z0-9]+$");
                            var zipCode = dr["ZipCode"].ToString().Trim();
                            var country = dr["CountryName"].ToString().Trim();
                            if (country.Equals("INDIA", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!indiaReg.IsMatch(zipCode))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + $". Invalid ZIP code '{zipCode}' for country '{country}'.";
                                }
                            }
                            else
                            {
                                if (!alphanumericReg.IsMatch(zipCode))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + $". Invalid ZIP code '{zipCode}' for country '{country}'.";
                                }
                            }
                        }
                        else if (dc.ColumnName == "OrganizationEmail")
                        {
                            if (dr["OrganizationEmail"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Organization Email should not be empty ";
                            }
                            var emailValidationExpression = new Regex(@"\b[a-zA-Z0-9._%-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}\b", RegexOptions.IgnoreCase);
                            var email = dr["OrganizationEmail"].ToString().Trim();
                            if (!string.IsNullOrEmpty(email) && !emailValidationExpression.IsMatch(email))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + $". Invalid email address '{email}'.";
                            }
                        }
                        else if (dc.ColumnName == "OrganizationPhone")
                        {
                            if (dr["OrganizationPhone"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Organization Phone should not be empty ";
                            }
                            var phoneNumberExpression = new Regex(@"\d{10}$");
                            var phoneNumber = dr["OrganizationPhone"].ToString().Trim();
                            if (!string.IsNullOrEmpty(phoneNumber) && !phoneNumberExpression.IsMatch(phoneNumber))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + $". Invalid phone number '{phoneNumber}'.";
                            }
                        }
                        else if (dc.ColumnName == "Website")
                        {
                            var website = dr["Website"].ToString().Trim();
                            var websiteRegExpression = new Regex(@"(https?:\/\/(?:www\.|(?!www))[^\s\.]+\.[^\s]{2,}|www\.[^\s]+\.[^\s]{2,})", RegexOptions.IgnoreCase);

                            if (!string.IsNullOrEmpty(website) && !websiteRegExpression.IsMatch(website))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + $". Invalid website URL '{website}'.";
                            }
                        }
                    }

                    if (sErrorMsg != "")
                    {
                        DataRow errorRow = colserror.NewRow();
                        errorRow.ItemArray = dr.ItemArray;
                        errorRow["Error Message"] = sErrorMsg;
                        colserror.Rows.Add(errorRow);
                    }
                }

                return colserror;
            }
            finally
            {
                datatable?.Dispose();
            }
        }
        //Add Organization added by hamraj end
        #endregion

        #region BranchDetails API
        [HttpPost]
        public HttpResponseMessage AddCompanyHierarchyDetails(BranchPostData submitData)
        {
            var json = JsonConvert.SerializeObject(submitData.CompanyHierarchyDetails);
            DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);

            long companyId, userId;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());

            if (excelDt == null)
            {
                bool status = false;
                string message = "Json CompanyHierarchyDetails not found for import.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }

            var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();

            var parametersObj = new GenericLookUp();
            parametersObj.Field1 = Convert.ToInt32(ADQFAMS.Common.Enums.HierarchyMasterTypes.Branch);
            parametersObj.Field2 = companyId;
            var databaseColumns = _masterApi.GetHierarchyDynamicData(parametersObj);

            var States = _baseInterface.ICompany.GetStates(companyId);

            if (databaseColumns.HierarchyDynamicList != null && databaseColumns.HierarchyDynamicList.Count() > 0)
            {
                if (CommonHelper.ValidateExcelColumns(colsList, _importApi.GetBranchDynamicColumnsList(companyId)))
                {
                    try
                    {
                        List<TripleText> currentSessionInsertedDataList = new List<TripleText>();
                        TripleText currentSessionInsertedData = new TripleText();
                        var recordDetails = _baseInterface.ICompany.GetAllBrancheDetails(companyId);
                        int recordCount = 0;
                        long parentId = 0;
                        long currentRowId = 0;
                        var oBranchObj = new BranchViewModel();
                        if (excelDt.Rows.Count != 0)
                        {
                            DataTable dttable = ValidateDynamicBranchImportData(excelDt, companyId, databaseColumns);
                            if (dttable.Rows.Count == 0)
                            {
                                foreach (DataRow dr in excelDt.Rows)
                                {
                                    parentId = 0;
                                    currentRowId = 0;
                                    foreach (var data in databaseColumns.HierarchyDynamicList)
                                    {
                                        currentSessionInsertedData = new TripleText();
                                        oBranchObj = new BranchViewModel();
                                        recordCount = recordDetails.Where(x => x.TypeID == data.LevelType && x.CompanyId == companyId && x.Code.ToString().ToLower().Trim() == dr[data.LevelName + "Code"].ToString().ToLower().Trim()).Count();

                                        if (dr[data.LevelName + "Code"].ToString() != "" && dr[data.LevelName + "Name"].ToString() != "" && recordCount == 0 && currentSessionInsertedDataList != null && currentSessionInsertedDataList.Where(x => x.Text1.ToString().ToLower().Trim() == dr[data.LevelName + "Code"].ToString().ToLower().Trim() && (data.LevelType != 100 ? x.Text3.ToString().ToLower().Trim() == dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim() : x.Text1.ToString().ToLower().Trim() == dr[data.LevelName + "Code"].ToString().ToLower().Trim())).Count() == 0)
                                        {
                                            if (data.LevelType == 100)
                                                parentId = 0;
                                            else if (data.LevelType != 100)
                                            {
                                                parentId = dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().Trim() != "" ? (recordDetails.Where(x => x.TypeID == (data.LevelType - 1) && x.CompanyId == companyId && x.Code.ToString().ToLower().Trim() == dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim()).Count() > 0
                                                    ? recordDetails.Where(x => x.TypeID == (data.LevelType - 1) && x.CompanyId == companyId && x.Code.ToString().ToLower().Trim() == dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim()).FirstOrDefault().BranchId
                                                    : currentSessionInsertedDataList.Where(x => x.Text1.ToString().ToLower().Trim() == dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim()).FirstOrDefault().Value) : 0;
                                            }
                                            oBranchObj = new BranchViewModel
                                            {
                                                CreatedBy = userId,
                                                CreatedDate = DateTime.Now,
                                                IsActive = true,
                                                Name = dr[data.LevelName + "Name"].ToString().Trim(),
                                                Code = dr[data.LevelName + "Code"].ToString().Trim(),
                                                ParentId = parentId,
                                                TypeID = data.LevelType,
                                                CompanyId = companyId
                                            };
                                            currentRowId = _baseInterface.ICompany.AddBranch(oBranchObj);
                                            if (currentRowId > 0)
                                            {
                                                currentSessionInsertedData.Value = currentRowId;
                                                currentSessionInsertedData.Text1 = dr[data.LevelName + "Code"].ToString();
                                                currentSessionInsertedData.Text2 = dr[data.LevelName + "Name"].ToString();
                                                if (data.LevelType != 100)
                                                    currentSessionInsertedData.Text3 = dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString();
                                                else
                                                    currentSessionInsertedData.Text3 = "";
                                                currentSessionInsertedDataList.Add(currentSessionInsertedData);
                                                if (databaseColumns.LeafLevelTypeId == data.LevelType && dr[data.LevelName + "Code"].ToString().Trim() != "")
                                                {
                                                    oBranchObj.TinNo = dr[ResourceFile.TinOrGst].ToString();
                                                    oBranchObj.PanNo = dr[ResourceFile.PanLabel].ToString();
                                                    oBranchObj.Address = dr[ResourceFile.AddressLabel].ToString();
                                                    oBranchObj.City = dr[ResourceFile.City].ToString();
                                                    oBranchObj.State = dr[ResourceFile.State].ToString();
                                                    oBranchObj.ZipCode = dr[ResourceFile.ZipCode].ToString();
                                                    oBranchObj.MobileNo = dr[ResourceFile.MobileNo].ToString();
                                                    // oBranchObj.PhoneNo = dr[ResourceFile.PhoneNo].ToString();
                                                    oBranchObj.BranchId = currentRowId;
                                                    oBranchObj.CreatedBy = userId;
                                                    oBranchObj.StateId = (dr[ResourceFile.State].ToString() != "") ? States.Where(x => x.State.ToString().ToLower().Trim() == dr[ResourceFile.State].ToString().ToLower().Trim()).FirstOrDefault().Id : 0;
                                                    oBranchObj.EmailAddress = dr[ResourceFile.EmailIdLabel].ToString();
                                                    _baseInterface.ICompany.AddBranchAddressDetails(oBranchObj);
                                                }
                                            }
                                        }
                                        else if (recordCount >= 1) //This may be useful for updation of branch Name
                                        {
                                            currentRowId = recordDetails.Where(x => x.TypeID == data.LevelType && x.CompanyId == companyId && x.Code.ToString().ToLower().Trim() == dr[data.LevelName + "Code"].ToString().ToLower().Trim()).Count() > 0 ?
                                                recordDetails.Where(x => x.TypeID == data.LevelType && x.CompanyId == companyId && x.Code.ToString().ToLower().Trim() == dr[data.LevelName + "Code"].ToString().ToLower().Trim()).FirstOrDefault().BranchId :
                                                currentSessionInsertedDataList.Where(x => x.Text1 == dr[data.LevelName + "Code"].ToString()).FirstOrDefault().Value;
                                            oBranchObj = new BranchViewModel
                                            {
                                                ModifiedBy = userId,
                                                ModifiedDate = DateTime.Now,
                                                Name = dr[data.LevelName + "Name"].ToString().Trim(),
                                                Code = dr[data.LevelName + "Code"].ToString().Trim(),
                                                CompanyId = companyId,
                                                BranchId = currentRowId
                                            };
                                            _baseInterface.ICompany.EditBranch(oBranchObj);

                                            if (databaseColumns.LeafLevelTypeId == data.LevelType && dr[data.LevelName + "Code"].ToString().Trim() != "")
                                            {
                                                oBranchObj.TinNo = dr[ResourceFile.TinOrGst].ToString();
                                                oBranchObj.PanNo = dr[ResourceFile.PanLabel].ToString();
                                                oBranchObj.Address = dr[ResourceFile.AddressLabel].ToString();
                                                oBranchObj.City = dr[ResourceFile.City].ToString();
                                                oBranchObj.State = dr[ResourceFile.State].ToString();
                                                oBranchObj.ZipCode = dr[ResourceFile.ZipCode].ToString();
                                                oBranchObj.MobileNo = dr[ResourceFile.MobileNo].ToString();
                                                //  oBranchObj.PhoneNo = dr[ResourceFile.PhoneNo].ToString();
                                                oBranchObj.BranchId = currentRowId;
                                                oBranchObj.CreatedBy = userId;
                                                oBranchObj.StateId = (dr[ResourceFile.State].ToString() != "") ? (States.Where(x => x.State.ToString().ToLower().Trim() == dr[ResourceFile.State].ToString().ToLower().Trim()).FirstOrDefault().Id) : 0;
                                                oBranchObj.EmailAddress = dr[ResourceFile.EmailIdLabel].ToString();
                                                _baseInterface.ICompany.UpdateBranchAddressDetails(oBranchObj);
                                            }
                                        }
                                    }
                                }
                                bool status = true;
                                string message = "Company Hierarchy added successfully.";
                                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                            }
                            else
                            {
                                string JSONresult;
                                bool status = false;
                                JSONresult = JsonConvert.SerializeObject(dttable, Formatting.Indented);
                                var response = Request.CreateResponse(HttpStatusCode.OK);
                                response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                                return response;
                            }
                        }
                        else
                        {
                            bool status = false;
                            string message = "Json CompanyHierarchyDetails not found for import.";
                            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                        }
                    }
                    catch (Exception ex)
                    {
                        bool status = false;
                        string message = ex.Message.ToString();
                        message = !string.IsNullOrEmpty(message) ? message : ex.ToString();
                        return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                        // return Request.CreateResponse(HttpStatusCode.OK, status, message);
                    }
                }
                else
                {
                    bool status = false;
                    string message = "Json format does not match with defined Company Hierarchy. ";
                    return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                }
            }
            else
            {
                bool status = false;
                string message = "Company Hierarchy is not defined.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
        }

        public DataTable ValidateDynamicBranchImportData(DataTable dt, long companyId, HierarchyModel databaseColumns)
        {
            DataTable dtError = new DataTable();
            
            foreach (DataColumn dc in dt.Columns)
                dtError.Columns.Add(dc.ColumnName);
            dtError.Columns.Add("ErrorMessage");
            
            var columnHeaderName = "";
            var columnHeaderCode = "";
            var recordDetails = _baseInterface.ICompany.GetAllBrancheDetails(companyId);
            DataColumnCollection branchColumns = dt.Columns;
            var retval = 0;
            int recordCount = 0;
            List<TripleText> excelDataList = new List<TripleText>();
            TripleText excelData = new TripleText();
            var States = _baseInterface.ICompany.GetStates(companyId);
            
            foreach (DataRow dr in dt.Rows)
            {
                foreach (var data in databaseColumns.HierarchyDynamicList)
                {
                    if (dr[data.LevelName + "Code"].ToString() != "" && dr[data.LevelName + "Name"].ToString() != "")
                    {
                        excelData = new TripleText();
                        excelData.Text1 = dr[data.LevelName + "Code"].ToString();
                        excelData.Text2 = dr[data.LevelName + "Name"].ToString();
                        excelData.Value = data.LevelType;
                        if (data.LevelType != 100 && dr[databaseColumns.HierarchyDynamicList.Where(x => x.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"] != "")
                            excelData.Text3 = dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString();
                        excelDataList.Add(excelData);
                    }
                }
            }
            var branchObj = new List<BranchViewModel>();
            string sErrorMsg = "";
            int nMsgCnt = 0;
            foreach (DataRow dr in dt.Rows)
            {
                try
                {
                    sErrorMsg = "";
                    nMsgCnt = 0;


                    foreach (var data in databaseColumns.HierarchyDynamicList)
                    {

                        columnHeaderCode = data.LevelName + "Code"; columnHeaderName = data.LevelName + "Name";
                        if (branchColumns.Contains(columnHeaderName) && dr[columnHeaderName].ToString().Trim() == "" && data.LevelType == 100)
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderName + " is mandatory ";

                        }
                        if (branchColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString().Trim() == "" && data.LevelType == 100)
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " is mandatory ";

                        }
                        if (branchColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString() != "" && dr[columnHeaderCode].ToString().Length > 20)
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " length should not exceed 20 characters. ";
                        }
                        if (branchColumns.Contains(columnHeaderName) && dr[columnHeaderName].ToString() != "" && !regexSpecialCharacters4.IsMatch(dr[columnHeaderName].ToString().Trim()))
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderName + " accepts letters (a-z), numbers (0-9). Special Characters Double Quotes and Back Slash Are Not Allowed";
                        }
                        if (branchColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString() != "" && !regexSpecialCharacters.IsMatch(dr[columnHeaderCode].ToString().Trim()))
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " " + ResourceFile.AllowSpecialCharectersForExcel + " ";
                        }

                        if (data.LevelType != 100 && databaseColumns.LeafLevelTypeId != 100 && dr[columnHeaderCode].ToString() != "" && dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString() == "")
                        {

                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Parent details should not be empty for " + data.LevelName + " ";

                        }
                        if (branchColumns.Contains(ResourceFile.TinOrGst))
                        {
                            if (dr[ResourceFile.TinOrGst].ToString() != "" && dr[ResourceFile.TinOrGst].ToString().Length > 20)
                            {
                                retval = -1;
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". " + ResourceFile.TinOrGst + " length should not be more than 50 characters ";
                            }
                        }
                        //checking with in the sheet for same name for 1st level 
                        if (data.LevelType == 100 && excelDataList.Where(x => x.Text2.ToString().ToLower().Trim() == dr[columnHeaderName].ToString().ToLower().Trim() && x.Text1.ToString().ToLower().Trim() != dr[columnHeaderCode].ToString().ToLower().Trim() && x.Value == data.LevelType).Count() > 0)
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected in the json file at " + columnHeaderName + " ";

                        }
                        //checking with in the sheet for same name for same level and for same parent
                        if (data.LevelType != 100 && excelDataList.Where(x => x.Text2.ToString().ToLower().Trim() == dr[columnHeaderName].ToString().ToLower().Trim() && x.Text1.ToString().ToLower().Trim() != dr[columnHeaderCode].ToString().ToLower().Trim() && x.Value == data.LevelType && x.Text3.ToString().ToLower().Trim() == dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim()).Count() > 0)
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected in the json file at " + columnHeaderName + " ";
                        }
                        //checking with in the sheet for same code 
                        if (data.LevelType != 100 && excelDataList.Where(x => x.Text1 == dr[columnHeaderCode].ToString() && x.Text3.ToString().ToLower().Trim() != dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim()).Count() > 0)
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected in the json file at " + columnHeaderCode + " ";
                        }

                        //checking if code already exists in database. if exists then check for parent code, if parent is not same then throw error
                        if (data.LevelType != 100)
                        {
                            branchObj = new List<BranchViewModel>();
                            branchObj = recordDetails.Where(x => x.CompanyId == companyId && x.Code == dr[columnHeaderCode].ToString()).ToList();
                            if (branchObj.Count() > 0)
                            {
                                recordCount = 1;
                                recordCount = recordDetails.Where(x => x.BranchId == branchObj.FirstOrDefault().ParentId && x.Code == dr[databaseColumns.HierarchyDynamicList.Where(p => p.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString()).Count();
                                if (recordCount == 0)
                                {
                                    retval = -1;
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Record already exists with the same code at " + columnHeaderCode + " ";
                                }
                            }
                        }
                        // checking if name already exists in database with same level type for 1st level
                        if (data.LevelType == 100 && recordDetails.Where(x => x.TypeID == data.LevelType && x.Name.ToString().ToLower().Trim() == dr[columnHeaderName].ToString().ToLower().Trim() && x.Code.ToString().ToLower().Trim() != dr[columnHeaderCode].ToString().ToLower().Trim()).Count() > 0)
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Record already exists with the same name at " + columnHeaderName + " ";

                        }
                        //checking if name already exists in database with same level type for level types > 100 with the same parent
                        if (data.LevelType != 100)
                        {
                            branchObj = new List<BranchViewModel>();
                            branchObj = recordDetails.Where(x => x.CompanyId == companyId && x.Name.ToString().ToLower().Trim() == dr[columnHeaderName].ToString().ToLower().Trim() && x.TypeID == data.LevelType && x.Code.ToString().ToLower().Trim() != dr[columnHeaderCode].ToString().ToLower().Trim()).ToList();
                            if (branchObj.Count() > 0)
                            {
                                recordCount = 1;
                                recordCount = recordDetails.Where(x => x.BranchId == branchObj.FirstOrDefault().ParentId && x.Code == dr[databaseColumns.HierarchyDynamicList.Where(p => p.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString()).Count();
                                if (recordCount > 0)
                                {
                                    retval = -1;
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Record already exists with the same name at " + columnHeaderName + " ";
                                }
                            }
                        }
                        if (data.LevelType == databaseColumns.LeafLevelTypeId && dr[columnHeaderCode].ToString().Trim() != "")
                        {
                            if (branchColumns.Contains("State") && dr["State"].ToString().Trim() == "")
                            {
                                retval = -1;
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". State is mandatory for last level of company hierarchy ";
                            }
                            else if (branchColumns.Contains("State") && dr["State"].ToString().Trim() != "")
                            {
                                if (States.Where(x => x.State.ToString().ToLower().Trim() == dr["State"].ToString().ToLower().Trim()).Count() <= 0)
                                {
                                    retval = -1;
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Invalid State name ";
                                }
                            }
                            if (branchColumns.Contains(ResourceFile.CIN))
                            {
                                if (dr[ResourceFile.CIN].ToString() != "" && dr[ResourceFile.CIN].ToString().Length > 20)
                                {
                                    retval = -1;
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". CIN length should not be more than 50 characters ";
                                }
                            }
                            if (branchColumns.Contains(ResourceFile.ServiceTaxNo))
                            {
                                if (dr[ResourceFile.ServiceTaxNo].ToString() != "" && dr[ResourceFile.ServiceTaxNo].ToString().Length > 20)
                                {
                                    retval = -1;
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Service Tax No length should not be more than 50 characters ";
                                }
                            }
                            if (branchColumns.Contains(ResourceFile.AddressLabel))
                            {
                                if (dr[ResourceFile.AddressLabel].ToString() != "" && dr[ResourceFile.AddressLabel].ToString().Length > 200)
                                {
                                    retval = -1;
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Address length should not be more than 200 characters ";
                                }
                            }
                            if (branchColumns.Contains(ResourceFile.City))
                            {
                                if (dr[ResourceFile.City].ToString() != "" && dr[ResourceFile.City].ToString().Length > 20)
                                {
                                    retval = -1;
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". City length should not be more than 50 characters ";
                                }
                            }
                            if (branchColumns.Contains(ResourceFile.ZipCode))
                            {
                                if (dr[ResourceFile.ZipCode].ToString() != "")
                                {
                                    var zip = Regex.IsMatch(dr[ResourceFile.ZipCode].ToString().Trim(), @"^([0-9]{6})$");
                                    if (zip == false)
                                    {
                                        retval = -1;
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". ZIP Code is not valid ";
                                    }
                                }
                            }
                            if (branchColumns.Contains(ResourceFile.EmailIdLabel))
                            {
                                if (dr[ResourceFile.EmailIdLabel].ToString().Trim() != "")
                                {
                                    var value = CommonHelper.IsValidEmailId(dr[ResourceFile.EmailIdLabel].ToString().Trim());
                                    if (value == false)
                                    {
                                        retval = -1;
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Email Id is not valid ";
                                    }
                                }
                            }

                            if (branchColumns.Contains(ResourceFile.MobileNo))
                            {
                                if (dr[ResourceFile.MobileNo].ToString() != "" && dr[ResourceFile.MobileNo].ToString().Length > 10 || dr[ResourceFile.MobileNo].ToString() != "" && dr[ResourceFile.MobileNo].ToString().Length < 10)
                                {
                                    retval = -1;
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Mobile Number is not valid ";
                                }
                            }
                        }

                    }
                }
                catch(Exception ex)
                {

                }
                if (sErrorMsg != "")
                {
                    dtError.Rows.Add(dr.ItemArray);
                    dtError.Rows[dtError.Rows.Count - 1]["ErrorMessage"] = sErrorMsg;
                    continue;
                }
            }
            return dtError;
        }

        #endregion

        #region DepartmentDetails API
        [HttpPost]
        public HttpResponseMessage AddDepartmentDetails(DepartmentPostData submitData)
        {
            var json = JsonConvert.SerializeObject(submitData.DepartmentDetails);
            DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);

            long companyId, userId;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());

            if (excelDt == null)
            {
                bool status = false;
                string message = "Json DepartmentDetails not found for import.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }

            var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();
            var parametersObj = new GenericLookUp();
            parametersObj.Field1 = Convert.ToInt16(ADQFAMS.Common.Enums.HierarchyMasterTypes.Department);
            parametersObj.Field2 = companyId;
            var databaseColumns = _masterApi.GetHierarchyDynamicData(parametersObj);

            if (databaseColumns.HierarchyDynamicList != null && databaseColumns.HierarchyDynamicList.Count() > 0)
            {
                if (CommonHelper.ValidateExcelColumns(colsList, _importApi.GetDynamicColumnsList(companyId, Convert.ToInt16(ADQFAMS.Common.Enums.HierarchyMasterTypes.Department), 0)))
                {
                    try
                    {
                        List<TripleText> currentSessionInsertedDataList = new List<TripleText>();
                        TripleText currentSessionInsertedData = new TripleText();
                        var recordDetails = _baseInterface.IDepartmentService.GetDepartments(companyId);
                        int recordCount = 0;
                        long parentId = 0;
                        long currentRowId = 0;
                        var oDepartmentObj = new DepartmentViewModel();
                        if (excelDt.Rows.Count != 0)
                        {
                            DataTable dttable = ValidateDynamicDepartmentImportData(excelDt, companyId, databaseColumns);
                            if (dttable.Rows.Count == 0)
                            {
                                bool status = false;
                                string message = "";
                                foreach (DataRow dr in excelDt.Rows)
                                {
                                    parentId = 0;
                                    currentRowId = 0;
                                    foreach (var data in databaseColumns.HierarchyDynamicList)
                                    {
                                        currentSessionInsertedData = new TripleText();
                                        oDepartmentObj = new DepartmentViewModel();
                                        recordCount = recordDetails.Where(x => x.DepartmentTypeId == data.LevelType && x.CompanyId == companyId && x.Code.ToString().ToLower().Trim() == dr[data.LevelName + "Code"].ToString().ToLower().Trim()).Count();
                                        if (dr[data.LevelName + "Code"].ToString() != "" && dr[data.LevelName + "Name"].ToString() != "" && recordCount == 0 && currentSessionInsertedDataList != null && currentSessionInsertedDataList.Where(x => x.Text1.ToString().ToLower().Trim() == dr[data.LevelName + "Code"].ToString().ToLower().Trim() && (data.LevelType != 100 ? x.Text3.ToString().ToLower().Trim() == dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim() : x.Text1.ToString().ToLower().Trim() == dr[data.LevelName + "Code"].ToString().ToLower().Trim())).Count() == 0)
                                        {
                                            if (data.LevelType == 100)
                                                parentId = 0;
                                            else if (data.LevelType != 100)
                                            {
                                                parentId = recordDetails.Where(x => x.DepartmentTypeId == (data.LevelType - 1) && x.CompanyId == companyId && x.Code.ToString().ToLower().Trim() == dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim()).Count() > 0
                                                    ? recordDetails.Where(x => x.DepartmentTypeId == (data.LevelType - 1) && x.CompanyId == companyId && x.Code.ToString().ToLower().Trim() == dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim()).FirstOrDefault().DepartmentID
                                                    : currentSessionInsertedDataList.Where(x => x.Text1.ToString().ToLower().Trim() == dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim()).FirstOrDefault().Value;
                                            }
                                            oDepartmentObj = new DepartmentViewModel
                                            {
                                                CreatedBy = userId,
                                                CreatedDate = DateTime.Now,
                                                IsActive = true,
                                                Name = dr[data.LevelName + "Name"].ToString().Trim(),
                                                Code = dr[data.LevelName + "Code"].ToString().Trim(),
                                                ParentDepartmentId = parentId,
                                                DepartmentTypeId = (int)data.LevelType,
                                                CompanyId = companyId
                                            };
                                            currentRowId = _baseInterface.IDepartmentService.AddDepartment(oDepartmentObj);
                                            if (currentRowId > 0)
                                            {
                                                currentSessionInsertedData.Value = currentRowId;
                                                currentSessionInsertedData.Text1 = dr[data.LevelName + "Code"].ToString();//for code
                                                currentSessionInsertedData.Text2 = dr[data.LevelName + "Name"].ToString();//for name
                                                if (data.LevelType != 100)
                                                    currentSessionInsertedData.Text3 = dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString();
                                                else
                                                    currentSessionInsertedData.Text3 = ""; //for parent code
                                                currentSessionInsertedDataList.Add(currentSessionInsertedData);
                                            }
                                        }
                                        else if (recordCount >= 1) //This may be useful for updation of Location Name
                                        {
                                            currentRowId = recordDetails.Where(x => x.DepartmentTypeId == data.LevelType && x.CompanyId == companyId && x.Code.ToString().ToLower().Trim() == dr[data.LevelName + "Code"].ToString().ToLower().Trim()).Count() > 0 ?
                                                recordDetails.Where(x => x.DepartmentTypeId == data.LevelType && x.CompanyId == companyId && x.Code.ToString().ToLower().Trim() == dr[data.LevelName + "Code"].ToString().ToLower().Trim()).FirstOrDefault().DepartmentID :
                                                currentSessionInsertedDataList.Where(x => x.Text1.ToString().ToLower().Trim() == dr[data.LevelName + "Code"].ToString().ToLower().Trim()).FirstOrDefault().Value;
                                            oDepartmentObj = new DepartmentViewModel
                                            {
                                                ModifiedBy = userId,
                                                ModifiedDate = DateTime.Now,
                                                Name = dr[data.LevelName + "Name"].ToString().Trim(),
                                                Code = dr[data.LevelName + "Code"].ToString().Trim(),
                                                CompanyId = companyId,
                                                DepartmentID = currentRowId
                                            };
                                            _baseInterface.IDepartmentService.EditDepartment(oDepartmentObj);
                                        }
                                    }
                                }
                                status = true;
                                message = "Department added successfully.";
                                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                            }
                            else
                            {
                                string JSONresult;
                                bool status = false;
                                JSONresult = JsonConvert.SerializeObject(dttable, Formatting.Indented);
                                var response = Request.CreateResponse(HttpStatusCode.OK);
                                response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                                return response;
                            }

                        }
                        else
                        {
                            bool status = false;
                            string message = "Json DepartmentDetails not found for import.";
                            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                        }
                    }
                    catch (Exception ex)
                    {
                        bool status = false;
                        string message = ex.Message.ToString();
                        message = !string.IsNullOrEmpty(message) ? message : ex.ToString();
                        return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                        // return Request.CreateResponse(HttpStatusCode.OK, status, message);
                    }
                }
                else
                {
                    bool status = false;
                    string message = "Json format does not match with defined Department. ";
                    return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                }
            }
            else
            {
                bool status = false;
                string message = "Department is not defined.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
        }

        public DataTable ValidateDynamicDepartmentImportData(DataTable dt, long companyId, HierarchyModel databaseColumns)
        {
            DataTable dtError = new DataTable();
            foreach (DataColumn dc in dt.Columns)
                dtError.Columns.Add(dc.ColumnName);
            dtError.Columns.Add("ErrorMessage");

            var columnHeaderName = "";
            var columnHeaderCode = "";
            var recordDetails = _baseInterface.IDepartmentService.GetDepartments(companyId);
            DataColumnCollection excelColumns = dt.Columns;
            var retval = 0;
            int recordCount = 0;
            List<TripleText> excelDataList = new List<TripleText>();
            TripleText excelData = new TripleText();
            foreach (DataRow dr in dt.Rows)
            {
                foreach (var data in databaseColumns.HierarchyDynamicList)
                {
                    if (dr[data.LevelName + "Code"].ToString() != "" && dr[data.LevelName + "Name"].ToString() != "")
                    {
                        excelData = new TripleText();
                        excelData.Text1 = dr[data.LevelName + "Code"].ToString();
                        excelData.Text2 = dr[data.LevelName + "Name"].ToString();
                        excelData.Value = data.LevelType;
                        if (data.LevelType != 100 && dr[databaseColumns.HierarchyDynamicList.Where(x => x.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"] != "")
                            excelData.Text3 = dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString();
                        else
                            excelData.Text3 = "";
                        excelDataList.Add(excelData);
                    }
                }
            }
            var deptObj = new List<DepartmentViewModel>();

            string sErrorMsg = "";
            int nMsgCnt = 0;

            foreach (DataRow dr in dt.Rows)
            {
                sErrorMsg = "";
                nMsgCnt = 0;

                foreach (var data in databaseColumns.HierarchyDynamicList)
                {
                    recordCount = 0;
                    columnHeaderCode = data.LevelName + "Code"; columnHeaderName = data.LevelName + "Name";

                    //1st level name is mandatory
                    if (excelColumns.Contains(columnHeaderName) && dr[columnHeaderName].ToString().Trim() == "" && data.LevelType == 100)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderName + " is mandatory ";
                    }

                    //1st level code is mandatory
                    if (excelColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString().Trim() == "" && data.LevelType == 100)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " is mandatory ";
                    }

                    if (excelColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString() != "" && dr[columnHeaderCode].ToString().Length > 20)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " length should not exceed 20 characters. ";
                    }

                    if (excelColumns.Contains(columnHeaderName) && dr[columnHeaderName].ToString() != "" && dr[columnHeaderName].ToString().Length > 20)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderName + " length should not exceed 20 characters. ";
                    }

                    if (excelColumns.Contains(columnHeaderName) && dr[columnHeaderName].ToString() != "" && !regexSpecialCharacters4.IsMatch(dr[columnHeaderName].ToString().Trim()))
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderName + " accepts letters (a-z), numbers (0-9). Special Characters Double Quotes and Back Slash Are Not Allowed";
                    }

                    if (excelColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString() != "" && !regexSpecialCharacters.IsMatch(dr[columnHeaderCode].ToString().Trim()))
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " " + ResourceFile.AllowSpecialCharectersForExcel + " ";
                    }
                    if (data.LevelType != 100 && databaseColumns.LeafLevelTypeId != 100 && dr[columnHeaderCode].ToString() != "" && dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString() == "")
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + "Parent details should not be empty for " + data.LevelName + " ";
                    }

                    //checking with in the sheet for same name for 1st level 
                    if (data.LevelType == 100 && excelDataList.Where(x => x.Text2.ToString().ToLower().Trim() == dr[columnHeaderName].ToString().ToLower().Trim() && x.Text1.ToString().ToLower().Trim() != dr[columnHeaderCode].ToString().ToLower().Trim() && x.Value == data.LevelType).Count() > 0)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + "Duplicate entry detected in the json file at " + columnHeaderName + " ";
                    }

                    //checking with in the sheet for same name for same level and for same parent
                    if (data.LevelType != 100 && excelDataList.Where(x => x.Text2.ToString().ToLower().Trim() == dr[columnHeaderName].ToString().ToLower().Trim() && x.Text1.ToString().ToLower().Trim() != dr[columnHeaderCode].ToString().ToLower().Trim() && x.Value == data.LevelType && x.Text3.ToString().ToLower().Trim() == dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim()).Count() > 0)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + "Duplicate entry detected in the json file at " + columnHeaderName + " ";
                    }
                    //checking with in the sheet for same code 
                    //if (data.LevelType != 100 && excelDataList.Where(x => x.Text1 == dr[columnHeaderCode].ToString() && x.Text3.ToString().ToLower().Trim() != dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim()).Count() > 0)
                    //{
                    //    retval = -1;
                    //    nMsgCnt++;
                    //    sErrorMsg = sErrorMsg + nMsgCnt + ". " + "Duplicate entry detected in the json file for " + data.LevelName + " ";
                    //}

                    if (dr != null && databaseColumns != null && databaseColumns.HierarchyDynamicList != null)
                    {
                        var levelName = databaseColumns.HierarchyDynamicList
                            .Where(y => y.LevelType == (data.LevelType - 1))
                            .Select(y => y.LevelName)
                            .FirstOrDefault();

                        if (levelName != null)
                        {
                            var columnName = levelName + "Code";
                            var excelDataCount = excelDataList
                                .Where(x => x.Text1 == dr[columnHeaderCode].ToString() &&
                                            x.Text3 != null &&
                                            dr[columnName] != null &&
                                            x.Text3.ToString().ToLower().Trim() != dr[columnName].ToString().ToLower().Trim())
                                .Count();

                            if (excelDataCount > 0)
                            {
                                retval = -1;
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". " + "Duplicate entry detected in the json file for " + data.LevelName + " code ";
                            }
                        }
                    }

                    if (dr != null && databaseColumns != null && databaseColumns.HierarchyDynamicList != null)
                    {
                        var levelName = databaseColumns.HierarchyDynamicList
                            .Where(y => y.LevelType == (data.LevelType - 1))
                            .Select(y => y.LevelName)
                            .FirstOrDefault();

                        if (levelName != null)
                        {
                            var columnName = levelName + "Name";
                            var excelDataCount = excelDataList
                                .Where(x => x.Text1 == dr[columnHeaderName].ToString() &&
                                            x.Text3 != null &&
                                            dr[columnName] != null &&
                                            x.Text3.ToString().ToLower().Trim() != dr[columnName].ToString().ToLower().Trim())
                                .Count();

                            if (excelDataCount > 0)
                            {
                                retval = -1;
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". " + "Duplicate entry detected in the json file for " + data.LevelName + " name ";
                            }
                        }
                    }


                    //checking if code already exists in database. if exists then check for parent code, if parent is not same then throw error
                    if (data.LevelType != 100)
                    {
                        deptObj = new List<DepartmentViewModel>();
                        deptObj = recordDetails.Where(x => x.CompanyId == companyId && x.Code == dr[columnHeaderCode].ToString()).ToList();
                        if (deptObj.Count() > 0)
                        {
                            recordCount = 1;
                            recordCount = recordDetails.Where(x => x.DepartmentID == deptObj.FirstOrDefault().ParentDepartmentId && x.Code == dr[databaseColumns.HierarchyDynamicList.Where(p => p.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString()).Count();
                            if (recordCount == 0)
                            {
                                retval = -1;
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". " + "Record already exists with the same code at " + columnHeaderCode + " ";
                            }
                        }
                    }

                    // checking if name already exists in database with same level type for 1st level
                    if (data.LevelType == 100 && recordDetails.Where(x => x.DepartmentTypeId == data.LevelType && x.Name.ToString().ToLower().Trim() == dr[columnHeaderName].ToString().ToLower().Trim() && x.Code.ToString().ToLower().Trim() != dr[columnHeaderCode].ToString().ToLower().Trim()).Count() > 0)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + "Record already exists with the same name at " + columnHeaderName + " ";
                    }

                    // checking if name already exists in database with same level type for level types > 100 with the same parent
                    if (data.LevelType != 100)
                    {
                        deptObj = new List<DepartmentViewModel>();
                        deptObj = recordDetails.Where(x => x.CompanyId == companyId && x.Name.ToString().ToLower().Trim() == dr[columnHeaderName].ToString().ToLower().Trim() && x.DepartmentTypeId == data.LevelType && x.Code.ToString().ToLower().Trim() != dr[columnHeaderCode].ToString().ToLower().Trim()).ToList();
                        if (deptObj.Count() > 0)
                        {
                            recordCount = 1;
                            recordCount = recordDetails.Where(x => x.DepartmentID == deptObj.FirstOrDefault().ParentId && x.Code == dr[databaseColumns.HierarchyDynamicList.Where(p => p.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString()).Count();
                            if (recordCount > 0)
                            {
                                retval = -1;
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". " + "Record already exists with the same name at " + columnHeaderName + " ";
                            }
                        }
                    }
                }

                if (sErrorMsg != "")
                {
                    dtError.Rows.Add(dr.ItemArray);
                    dtError.Rows[dtError.Rows.Count - 1]["ErrorMessage"] = sErrorMsg;
                    continue;
                }
            }
            return dtError;
        }

        #endregion

        #region CostCenterDetails API
        [HttpPost]
        public HttpResponseMessage AddCostCenterDetails(CostCenterPostData submitData)
        {
            var json = JsonConvert.SerializeObject(submitData.CostCenterDetails);
            DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);

            long companyId, userId;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());

            if (excelDt == null)
            {
                bool status = false;
                string message = "Json CostCenterDetails not found for import.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }

            var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();

            var parametersObj = new GenericLookUp();
            parametersObj.Field1 = Convert.ToInt16(ADQFAMS.Common.Enums.HierarchyMasterTypes.CostCenter);
            parametersObj.Field2 = companyId;
            var databaseColumns = _masterApi.GetHierarchyDynamicData(parametersObj);

            if (databaseColumns.HierarchyDynamicList != null && databaseColumns.HierarchyDynamicList.Count() > 0)
            {
                if (CommonHelper.ValidateExcelColumns(colsList, _importApi.GetDynamicColumnsList(companyId, Convert.ToInt16(ADQFAMS.Common.Enums.HierarchyMasterTypes.CostCenter), 0)))
                {
                    try
                    {
                        List<TripleText> currentSessionInsertedDataList = new List<TripleText>();
                        TripleText currentSessionInsertedData = new TripleText();
                        var recordDetails = _baseInterface.ICostCenter.GetCostCenters(companyId);
                        int recordCount = 0;
                        long parentId = 0;
                        long currentRowId = 0;
                        var oCostObj = new CostCenterViewModel();
                        var model = new CostCenterViewModel();
                        var columnHeaderCode = ""; var columnHeaderName = "";
                        if (excelDt.Rows.Count != 0)
                        {
                            DataTable dttable = ValidateDynamicCostCenterImportData(excelDt, companyId, databaseColumns);
                            if (dttable.Rows.Count == 0)
                            {
                                bool status = false;
                                string message = "";
                                foreach (DataRow dr in excelDt.Rows)
                                {
                                    parentId = 0;
                                    currentRowId = 0;
                                    foreach (var data in databaseColumns.HierarchyDynamicList)
                                    {
                                        currentSessionInsertedData = new TripleText();
                                        oCostObj = new CostCenterViewModel();
                                        columnHeaderCode = data.LevelName + "Code";
                                        columnHeaderName = data.LevelName + "Name";

                                        recordCount = recordDetails.Where(x => x.CostCenterTypeId == data.LevelType && x.CompanyId == companyId && x.Code.ToString().ToLower().Trim() == dr[columnHeaderCode].ToString().ToLower().Trim()).Count();
                                        if (dr[data.LevelName + "Code"].ToString() != "" && dr[data.LevelName + "Name"].ToString() != "" && recordCount == 0 && currentSessionInsertedDataList != null && currentSessionInsertedDataList.Where(x => x.Text1.ToString().ToLower().Trim() == dr[columnHeaderCode].ToString().ToLower().Trim() && (data.LevelType != 100 ? x.Text3.ToString().ToLower().Trim() == dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim() : x.Text1.ToString().ToLower().Trim() == dr[columnHeaderCode].ToString().ToLower().Trim())).Count() == 0)
                                        {
                                            if (data.LevelType == 100)
                                                parentId = 0;
                                            else if (data.LevelType != 100)
                                            {
                                                parentId = recordDetails.Where(x => x.CostCenterTypeId == (data.LevelType - 1) && x.CompanyId == companyId && x.Code.ToString().ToLower().Trim() == dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim()).Count() > 0
                                                    ? recordDetails.Where(x => x.CostCenterTypeId == (data.LevelType - 1) && x.CompanyId == companyId && x.Code.ToString().ToLower().Trim() == dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim()).FirstOrDefault().CostCenterID
                                                    : currentSessionInsertedDataList.Where(x => x.Text1.ToString().ToLower().Trim() == dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim()).FirstOrDefault().Value;
                                            }
                                            oCostObj = new CostCenterViewModel
                                            {
                                                CreatedBy = userId,
                                                CreatedDate = DateTime.Now,
                                                IsActive = true,
                                                Name = dr[columnHeaderName].ToString().Trim(),
                                                Code = dr[columnHeaderCode].ToString().Trim(),
                                                //Description = dr[data.LevelName + "Name"].ToString().Trim(),
                                                ParentID = parentId,
                                                CostCenterTypeId = (int)data.LevelType,
                                                CompanyId = companyId
                                            };
                                            currentRowId = _baseInterface.ICostCenter.AddCostCenter(oCostObj);
                                            if (currentRowId > 0)
                                            {
                                                currentSessionInsertedData.Value = currentRowId;
                                                currentSessionInsertedData.Text1 = dr[columnHeaderCode].ToString();
                                                currentSessionInsertedData.Text2 = dr[columnHeaderName].ToString();
                                                if (data.LevelType != 100)
                                                    currentSessionInsertedData.Text3 = dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString();
                                                else
                                                    currentSessionInsertedData.Text3 = "";
                                                currentSessionInsertedDataList.Add(currentSessionInsertedData);
                                            }
                                        }
                                        else if (recordCount >= 1) //This may be useful for updation of cost center Name
                                        {
                                            currentRowId = recordDetails.Where(x => x.CostCenterTypeId == data.LevelType && x.CompanyId == companyId && x.Code.ToString().ToLower().Trim() == dr[columnHeaderCode].ToString().ToLower().Trim()).Count() > 0 ?
                                                recordDetails.Where(x => x.CostCenterTypeId == data.LevelType && x.CompanyId == companyId && x.Code.ToString().ToLower().Trim() == dr[columnHeaderCode].ToString().ToLower().Trim()).FirstOrDefault().CostCenterID :
                                                currentSessionInsertedDataList.Where(x => x.Text1.ToString().ToLower().Trim() == dr[columnHeaderCode].ToString().ToLower().Trim()).FirstOrDefault().Value;
                                            oCostObj = new CostCenterViewModel
                                            {
                                                ModifiedBy = userId,
                                                ModifiedDate = DateTime.Now,
                                                Name = dr[columnHeaderName].ToString().Trim(),
                                                Code = dr[columnHeaderCode].ToString().Trim(),
                                                //Description = dr[data.LevelName + "Name"].ToString().Trim(),
                                                CompanyId = companyId,
                                                CostCenterID = currentRowId
                                            };
                                            _baseInterface.ICostCenter.EditCostCenter(oCostObj);
                                        }
                                    }
                                }
                                status = true;
                                message = "Cost Center added successfully.";
                                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                            }
                            else
                            {
                                string JSONresult;
                                bool status = false;
                                JSONresult = JsonConvert.SerializeObject(dttable, Formatting.Indented);
                                var response = Request.CreateResponse(HttpStatusCode.OK);
                                response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                                return response;
                            }

                        }
                        else
                        {
                            bool status = false;
                            string message = "Json CostCenterDetails not found for import.";
                            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                        }
                    }
                    catch (Exception ex)
                    {
                        bool status = false;
                        string message = ex.Message.ToString();
                        message = !string.IsNullOrEmpty(message) ? message : ex.ToString();
                        return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                        // return Request.CreateResponse(HttpStatusCode.OK, status, message);
                    }
                }
                else
                {
                    bool status = false;
                    string message = "Json format does not match with defined Cost Center. ";
                    return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                }
            }
            else
            {
                bool status = false;
                string message = "Cost Center is not defined.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
        }

        public DataTable ValidateDynamicCostCenterImportData(DataTable dt, long companyId, HierarchyModel databaseColumns)
        {
            DataTable dtError = new DataTable();
            foreach (DataColumn dc in dt.Columns)
                dtError.Columns.Add(dc.ColumnName);
            dtError.Columns.Add("ErrorMessage");

            var columnHeaderName = "";
            var columnHeaderCode = "";
            var recordDetails = _baseInterface.ICostCenter.GetCostCenters(companyId);
            DataColumnCollection excelColumns = dt.Columns;
            var retval = 0;
            int recordCount = 0;
            List<TripleText> excelDataList = new List<TripleText>();
            TripleText excelData = new TripleText();
            foreach (DataRow dr in dt.Rows)
            {
                foreach (var data in databaseColumns.HierarchyDynamicList)
                {
                    if (dr[data.LevelName + "Code"].ToString() != "" && dr[data.LevelName + "Name"].ToString() != "")
                    {
                        excelData = new TripleText();
                        excelData.Text1 = dr[data.LevelName + "Code"].ToString();
                        excelData.Text2 = dr[data.LevelName + "Name"].ToString();
                        excelData.Value = data.LevelType;
                        if (data.LevelType != 100 && dr[databaseColumns.HierarchyDynamicList.Where(x => x.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"] != "")
                            excelData.Text3 = dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString();
                        excelDataList.Add(excelData);
                    }
                }
            }
            var costObj = new List<CostCenterViewModel>();

            string sErrorMsg = "";
            int nMsgCnt = 0;

            foreach (DataRow dr in dt.Rows)
            {
                sErrorMsg = "";
                nMsgCnt = 0;

                foreach (var data in databaseColumns.HierarchyDynamicList)
                {
                    recordCount = 0;
                    columnHeaderCode = data.LevelName + "Code"; columnHeaderName = data.LevelName + "Name";

                    //1st level name is mandatory
                    if (excelColumns.Contains(columnHeaderName) && dr[columnHeaderName].ToString().Trim() == "" && data.LevelType == 100)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderName + " is mandatory ";
                    }

                    //1st level code is mandatory
                    if (excelColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString().Trim() == "" && data.LevelType == 100)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " is mandatory ";
                    }
                    if (excelColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString() != "" && dr[columnHeaderCode].ToString().Length > 20)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " length should not exceed 20 characters. ";
                    }

                    if (excelColumns.Contains(columnHeaderName) && dr[columnHeaderName].ToString() != "" && !regexSpecialCharacters4.IsMatch(dr[columnHeaderName].ToString().Trim()))
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderName + " accepts letters (a-z), numbers (0-9). Special Characters Double Quotes and Back Slash Are Not Allowed";
                    }

                    if (excelColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString() != "" && !regexSpecialCharacters.IsMatch(dr[columnHeaderCode].ToString().Trim()))
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " " + ResourceFile.AllowSpecialCharectersForExcel + " ";
                    }
                    if (data.LevelType != 100 && databaseColumns.LeafLevelTypeId != 100 && dr[columnHeaderCode].ToString() != "" && dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString() == "")
                    {

                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". Parent details should not be empty for " + data.LevelName + " ";
                    }

                    //checking with in the sheet for same name for 1st level 
                    if (data.LevelType == 100 && excelDataList.Where(x => x.Text2.ToString().ToLower().Trim() == dr[columnHeaderName].ToString().ToLower().Trim() && x.Text1.ToString().ToLower().Trim() != dr[columnHeaderCode].ToString().ToLower().Trim() && x.Value == data.LevelType).Count() > 0)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected in the json file at " + columnHeaderName + " ";
                    }
                    //checking with in the sheet for same name for same level and for same parent
                    if (data.LevelType != 100 && excelDataList.Where(x => x.Text2.ToString().ToLower().Trim() == dr[columnHeaderName].ToString().ToLower().Trim() && x.Text1.ToString().ToLower().Trim() != dr[columnHeaderCode].ToString().ToLower().Trim() && x.Value == data.LevelType && x.Text3.ToString().ToLower().Trim() == dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim()).Count() > 0)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected in the json file at " + columnHeaderName + " ";
                    }
                    //checking with in the sheet for same code 
                    if (data.LevelType != 100 && excelDataList.Where(x => x.Text1 == dr[columnHeaderCode].ToString() && x.Text3.ToString().ToLower().Trim() != dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim()).Count() > 0)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected in the json file for " + data.LevelName + " ";
                    }

                    //checking if code already exists in database. if exists then check for parent code, if parent is not same then throw error
                    if (data.LevelType != 100)
                    {
                        costObj = new List<CostCenterViewModel>();
                        costObj = recordDetails.Where(x => x.CompanyId == companyId && x.Code == dr[columnHeaderCode].ToString()).ToList();
                        if (costObj.Count() > 0)
                        {
                            recordCount = 1;
                            recordCount = recordDetails.Where(x => x.CostCenterID == costObj.FirstOrDefault().ParentID && x.Code == dr[databaseColumns.HierarchyDynamicList.Where(p => p.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString()).Count();
                            if (recordCount == 0)
                            {
                                retval = -1;
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Record already exists with the same code at " + columnHeaderCode + " ";
                            }
                        }
                    }
                    // checking if name already exists in database with same level type for 1st level
                    if (data.LevelType == 100 && recordDetails.Where(x => x.CostCenterTypeId == data.LevelType && x.Name.ToString().ToLower().Trim() == dr[columnHeaderName].ToString().ToLower().Trim() && x.Code.ToString().ToLower().Trim() != dr[columnHeaderCode].ToString().ToLower().Trim()).Count() > 0)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". Record already exists with the same name at " + columnHeaderName + " ";
                    }
                    // checking if name already exists in database with same level type for level types > 100 with the same parent
                    if (data.LevelType != 100)
                    {
                        costObj = new List<CostCenterViewModel>();
                        costObj = recordDetails.Where(x => x.CompanyId == companyId && x.Name.ToString().ToLower().Trim() == dr[columnHeaderName].ToString().ToLower().Trim() && x.CostCenterTypeId == data.LevelType && x.Code.ToString().ToLower().Trim() != dr[columnHeaderCode].ToString().ToLower().Trim()).ToList();
                        if (costObj.Count() > 0)
                        {
                            recordCount = 1;
                            recordCount = recordDetails.Where(x => x.CostCenterID == costObj.FirstOrDefault().ParentID && x.Code == dr[databaseColumns.HierarchyDynamicList.Where(p => p.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString()).Count();
                            if (recordCount > 0)
                            {
                                retval = -1;
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Record already exists with the same name at " + columnHeaderName + " ";
                            }
                        }
                    }
                }

                if (sErrorMsg != "")
                {
                    dtError.Rows.Add(dr.ItemArray);
                    dtError.Rows[dtError.Rows.Count - 1]["ErrorMessage"] = sErrorMsg;
                    continue;
                }
            }
            return dtError;
        }

        #endregion

        #region AssetLocationDetails API
        [HttpPost]
        public HttpResponseMessage AddAssetLocationDetails(LocationPostData submitData)
        {
            var json = JsonConvert.SerializeObject(submitData.AssetLocationDetails);
            DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);

            long companyId, userId;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());

            if (excelDt == null)
            {
                bool status = false;
                string message = "Json AssetLocationDetails not found for import.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }

            var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();

            var parametersObj = new GenericLookUp();
            parametersObj.Field1 = Convert.ToInt16(ADQFAMS.Common.Enums.HierarchyMasterTypes.Location);
            parametersObj.Field2 = companyId;
            var databaseColumns = _masterApi.GetHierarchyDynamicData(parametersObj);
            parametersObj.Field1 = Convert.ToInt32(ADQFAMS.Common.Enums.HierarchyMasterTypes.Branch);
            var orgLeafLevel = _masterApi.GetHierarchyDynamicData(parametersObj);
            if (databaseColumns.HierarchyDynamicList != null && databaseColumns.HierarchyDynamicList.Count() > 0)
            {
                if (CommonHelper.ValidateExcelColumns(colsList, _importApi.GetDynamicColumnsList(companyId, Convert.ToInt16(ADQFAMS.Common.Enums.HierarchyMasterTypes.Location), 0)))
                {
                    try
                    {
                        List<TripleText> currentSessionInsertedDataList = new List<TripleText>();
                        TripleText currentSessionInsertedData = new TripleText();
                        List<LocationViewModel> locations = _baseInterface.ILocation.GetAllLocations(companyId);
                        List<NumericLookupItem> branchDetails = _baseInterface.ICompany.GetBrancheDetailsByTypeId(companyId, orgLeafLevel.LeafLevelTypeId);
                        int locationCount = 0;
                        long parentId = 0;
                        long currentRowId = 0;
                        long currentBranchId = 0;
                        var oLocationObj = new LocationsModel();
                        if (excelDt.Rows.Count != 0)
                        {
                            DataTable dttable = ValidateDynamicLocationImportData(excelDt, companyId, databaseColumns);

                            if (dttable.Rows.Count == 0)
                            {
                                bool status = false;
                                string message = "";
                                foreach (DataRow dr in excelDt.Rows)
                                {
                                    parentId = 0;
                                    currentRowId = 0;
                                    if (branchDetails.Where(x => x.Text.ToString().ToLower().Trim() == dr[orgLeafLevel.LeafLevelName + "Code"].ToString().ToLower().Trim()).ToList().Count() > 0)
                                    {
                                        currentBranchId = branchDetails.Where(x => x.Text.ToString().ToLower().Trim() == dr[orgLeafLevel.LeafLevelName + "Code"].ToString().ToLower().Trim()).FirstOrDefault().Value;
                                        try
                                        {
                                            foreach (var data in databaseColumns.HierarchyDynamicList)
                                            {
                                                currentSessionInsertedData = new TripleText();
                                                oLocationObj = new LocationsModel();
                                                locationCount = locations.Where(x => x.LocationTypeId == data.LevelType && x.CompanyId == companyId && x.LocationCode.ToString().ToLower().Trim() == dr[data.LevelName + "Code"].ToString().ToLower().Trim()).Count();
                                                if (dr[data.LevelName + "Code"].ToString() != "" && dr[data.LevelName + "Name"].ToString() != "" && locationCount == 0 && currentSessionInsertedDataList != null && currentSessionInsertedDataList.Where(x => x.Text1.ToString().ToLower().Trim() == dr[data.LevelName + "Code"].ToString().ToLower().Trim() && (data.LevelType != 100 ? x.Text3.ToString().ToLower().Trim() == dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim() : x.Text1.ToString().ToLower().Trim() == dr[data.LevelName + "Code"].ToString().ToLower().Trim())).Count() == 0)
                                                {
                                                    if (data.LevelType == 100)
                                                        parentId = 0;
                                                    else if (data.LevelType != 100)
                                                    {
                                                        parentId = locations.Where(x => x.LocationTypeId == (data.LevelType - 1) && x.CompanyId == companyId && x.LocationCode.ToString().ToLower().Trim() == dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim()).Count() > 0
                                                            ? locations.Where(x => x.LocationTypeId == (data.LevelType - 1) && x.CompanyId == companyId && x.LocationCode.ToString().ToLower().Trim() == dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim()).FirstOrDefault().LocationId
                                                            : currentSessionInsertedDataList.Where(x => x.Text1.ToString().ToLower().Trim() == dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim()).FirstOrDefault().Value;
                                                    }
                                                    oLocationObj = new LocationsModel
                                                    {
                                                        UserId = userId,
                                                        LocationName = dr[data.LevelName + "Name"].ToString().Trim(),
                                                        Code = dr[data.LevelName + "Code"].ToString().Trim(),
                                                        ParentLocationId = parentId,
                                                        LocationTypeId = (int)data.LevelType,
                                                        CompanyId = companyId,
                                                        BranchId = currentBranchId
                                                    };
                                                    currentRowId = _baseInterface.ILocation.AddLocation(oLocationObj);
                                                    if (currentRowId > 0)
                                                    {
                                                        currentSessionInsertedData.Value = currentRowId;
                                                        currentSessionInsertedData.Text1 = dr[data.LevelName + "Code"].ToString();
                                                        currentSessionInsertedData.Text2 = dr[data.LevelName + "Name"].ToString();
                                                        if (data.LevelType != 100)
                                                            currentSessionInsertedData.Text3 = dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString();
                                                        else
                                                            currentSessionInsertedData.Text3 = "";
                                                        currentSessionInsertedDataList.Add(currentSessionInsertedData);
                                                    }
                                                }
                                                else if (locationCount >= 1) //This may be useful for updation of Location Name
                                                {
                                                    currentRowId = locations.Where(x => x.LocationTypeId == data.LevelType && x.CompanyId == companyId && x.LocationCode.ToString().ToLower().Trim() == dr[data.LevelName + "Code"].ToString().ToLower().Trim()).Count() > 0 ? locations.Where(x => x.LocationTypeId == data.LevelType && x.CompanyId == companyId && x.LocationCode.ToString().ToLower().Trim() == dr[data.LevelName + "Code"].ToString().ToLower().Trim()).FirstOrDefault().LocationId : currentSessionInsertedDataList.Where(x => x.Text1.ToString().ToLower().Trim() == dr[data.LevelName + "Code"].ToString().ToLower().Trim()).FirstOrDefault().Value;
                                                    oLocationObj = new LocationsModel
                                                    {
                                                        UserId = userId,
                                                        LocationName = dr[data.LevelName + "Name"].ToString().Trim(),
                                                        CompanyId = companyId,
                                                        LocationId = currentRowId
                                                    };
                                                    _baseInterface.ILocation.Update(oLocationObj);
                                                }
                                                //else
                                                //{
                                                //    status = false;
                                                //    message = "Json AssetLocationDetails not found for import.";
                                                //    return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                                                //}
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            status = false;
                                            message = ex.Message.ToString();
                                            message = !string.IsNullOrEmpty(message) ? message : ex.ToString();
                                            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                                        }
                                        //status = true;
                                        //message = "Asset Location added successfully.";
                                        //return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                                    }
                                    else
                                    {
                                        status = false;
                                        message = "Json AssetLocationDetails not found for import.";
                                        return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                                    }
                                }
                                status = true;
                                message = "Asset Location added successfully.";
                                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                            }
                            else
                            {
                                string JSONresult;
                                bool status = false;
                                JSONresult = JsonConvert.SerializeObject(dttable, Formatting.Indented);
                                var response = Request.CreateResponse(HttpStatusCode.OK);
                                response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                                return response;
                            }
                        }
                        else
                        {
                            bool status = false;
                            string message = "Json AssetLocationDetails not found for import.";
                            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                        }
                    }
                    catch (Exception ex)
                    {
                        bool status = false;
                        string message = ex.Message.ToString();
                        message = !string.IsNullOrEmpty(message) ? message : ex.ToString();
                        return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                        // return Request.CreateResponse(HttpStatusCode.OK, status, message);
                    }
                }
                else
                {
                    bool status = false;
                    string message = "Json format does not match with defined Asset Location. ";
                    return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                }
            }
            else
            {
                bool status = false;
                string message = "Asset Location is not defined.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
        }

        public DataTable ValidateDynamicLocationImportData(DataTable dt, long companyId, HierarchyModel databaseColumns)
        {
            DataTable dtError = new DataTable();
            try
            {
                foreach (DataColumn dc in dt.Columns)
                    dtError.Columns.Add(dc.ColumnName);
                dtError.Columns.Add("ErrorMessage");
                var columnHeaderName = "";
                var columnHeaderCode = "";
                DataColumnCollection excelColumns = dt.Columns;
                var retval = 0;
                int recordCount = 0;
                List<TripleText> excelDataList = new List<TripleText>();
                TripleText excelData = new TripleText();

                var parametersObj = new GenericLookUp();
                parametersObj.Field1 = Convert.ToInt32(ADQFAMS.Common.Enums.HierarchyMasterTypes.Branch);
                parametersObj.Field2 = companyId;
                var orgLeafLevel = _masterApi.GetHierarchyDynamicData(parametersObj);
                var recordDetails = _baseInterface.ILocation.GetAllLocations(companyId);
                long excelBranchid = 0;
                List<NumericLookupItem> branchDetails = _baseInterface.ICompany.GetBrancheDetailsByTypeId(companyId, orgLeafLevel.LeafLevelTypeId);
                foreach (DataRow dr in dt.Rows)
                {
                    foreach (var data in databaseColumns.HierarchyDynamicList)
                    {
                        if (dr[data.LevelName + "Code"].ToString() != "" && dr[data.LevelName + "Name"].ToString() != "")
                        {
                            excelData = new TripleText();
                            excelData.Text1 = dr[data.LevelName + "Code"].ToString();
                            excelData.Text2 = dr[data.LevelName + "Name"].ToString();
                            excelData.Value = data.LevelType;

                            if (data.LevelType != 100 && dr[databaseColumns.HierarchyDynamicList.Where(x => x.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().Trim() != "")
                                excelData.Text3 = dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString();
                            else excelData.Text3 = "";
                            if (data.LevelType == 100 && dr[orgLeafLevel.LeafLevelName + "Code"].ToString() != "")
                                excelData.Text4 = dr[orgLeafLevel.LeafLevelName + "Code"].ToString();
                            else excelData.Text4 = "";
                            excelDataList.Add(excelData);
                        }
                    }
                }
                var locationObj = new List<LocationViewModel>();

                string sErrorMsg = "";
                int nMsgCnt = 0;
                long v = 0;  //Added by Priyanka B on 16032024 for SR010863
                foreach (DataRow dr in dt.Rows)
                {
                    sErrorMsg = "";
                    nMsgCnt = 0;

                    foreach (var data in databaseColumns.HierarchyDynamicList)
                    {
                        recordCount = 0;
                        columnHeaderCode = data.LevelName + "Code"; columnHeaderName = data.LevelName + "Name";
                        if (branchDetails.Where(x => x.Text.ToString().ToLower().Trim() == dr[orgLeafLevel.LeafLevelName + "Code"].ToString().ToLower().Trim()).ToList().Count() == 0)
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". " + orgLeafLevel.LeafLevelName + " code does not exist ";
                        }

                        //1st level name is mandatory
                        if (excelColumns.Contains(columnHeaderName) && dr[columnHeaderName].ToString().Trim() == "" && data.LevelType == 100)
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderName + " is mandatory ";
                        }

                        //1st level code is mandatory
                        if (excelColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString().Trim() == "" && data.LevelType == 100)
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " is mandatory ";
                        }
                        if (excelColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString() != "" && dr[columnHeaderCode].ToString().Length > 20)
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " length should not exceed 20 characters. ";
                        }

                        if (excelColumns.Contains(columnHeaderName) && dr[columnHeaderName].ToString() != "" && !regexSpecialCharacters4.IsMatch(dr[columnHeaderName].ToString().Trim()))
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderName + " accepts letters (a-z), numbers (0-9). Special Characters Double Quotes and Back Slash Are Not Allowed";
                        }

                        if (excelColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString() != "" && !regexSpecialCharacters.IsMatch(dr[columnHeaderCode].ToString().Trim()))
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " " + ResourceFile.AllowSpecialCharectersForExcel + " ";
                        }
                        if (data.LevelType != 100 && databaseColumns.LeafLevelTypeId != 100 && (dr[columnHeaderCode].ToString() != "" || dr[columnHeaderName].ToString() != "") && (dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString() == "" || dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Name"].ToString() == ""))
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Parent details should not be empty for " + data.LevelName + " ";
                        }

                        //checking with in the sheet for same name for 1st level 
                        if (data.LevelType == 100 && excelDataList.Where(x => x.Text2.ToString().ToLower().Trim() == dr[columnHeaderName].ToString().ToLower().Trim() && x.Text1.ToString().ToLower().Trim() != dr[columnHeaderCode].ToString().ToLower().Trim() && x.Value == data.LevelType && x.Text4.ToString().ToLower().Trim() == dr[orgLeafLevel.LeafLevelName + "Code"].ToString().ToLower().Trim()).Count() > 0)
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected in the json file at " + columnHeaderName + " ";
                        }
                        //checking with in the sheet for same name for same level and for same parent
                        if (data.LevelType != 100 && excelDataList.Where(x => x.Text2.ToString().ToLower().Trim() == dr[columnHeaderName].ToString().ToLower().Trim() && x.Text1.ToString().ToLower().Trim() != dr[columnHeaderCode].ToString().ToLower().Trim() && x.Value == data.LevelType && x.Text3.ToString().ToLower().Trim() == dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim()).Count() > 0)
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected in the json file at " + columnHeaderName + " ";
                        }

                        //checking with in the sheet for same code for level type 100
                        if (data.LevelType == 100 && excelDataList.Where(x => x.Text1.ToString().ToLower().Trim() == dr[columnHeaderCode].ToString().ToLower().Trim() && x.Text4.ToString().ToLower().Trim() != dr[orgLeafLevel.LeafLevelName + "Code"].ToString().ToLower().Trim()).Count() > 0)
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected in the json file at " + columnHeaderCode + " ";
                        }
                        //checking with in the sheet for same code for level type more than 100
                        if (data.LevelType != 100 && excelDataList.Where(x => x.Text1.ToString().ToLower().Trim() == dr[columnHeaderCode].ToString().ToLower().Trim() && x.Text3.ToString().ToLower().Trim() != dr[databaseColumns.HierarchyDynamicList.Where(y => y.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString().ToLower().Trim()).Count() > 0)
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected in the json file at " + columnHeaderCode + " ";
                        }

                        //checking if code already exists in database. if exists then check for parent code, if parent is not same then throw error
                        if (data.LevelType > 99)
                        {
                            locationObj = new List<LocationViewModel>();
                            locationObj = recordDetails.Where(x => x.CompanyId == companyId && x.LocationCode == dr[columnHeaderCode].ToString()).ToList();
                            //Added by Priyanka B on 16032024 for SR010863 Start
                            if (locationObj.Count == 0 && v == 0)
                            {
                                v = data.LevelType - 1;
                            }
                            //Added by Priyanka B on 16032024 for SR010863 End

                            if (locationObj.Count() > 0)
                            {
                                recordCount = 1;
                                //if (data.LevelType != 100)  //Commented by Priyanka B on 16032024 for SR010863
                                if (data.LevelType >= v && v > 0)  //Modified by Priyanka B on 16032024 for SR010863
                                {
                                    //recordCount = recordDetails.Where(x => x.LocationId == locationObj.FirstOrDefault().ParentLocationId && x.LocationCode == dr[databaseColumns.HierarchyDynamicList.Where(p => p.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString()).Count();    //Commented by Priyanka B on 16032024 for SR010863
                                    recordCount = recordDetails.Where(x => x.LocationId == locationObj.FirstOrDefault().LocationId && x.LocationCode == dr[databaseColumns.HierarchyDynamicList.Where(p => p.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString()).Count();  //Modified by Priyanka B on 16032024 for SR010863
                                    if (recordCount == 0)
                                    {
                                        retval = -1;
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Record already exists with the same code at " + columnHeaderCode + " ";
                                    }
                                }
                                else //for level type 100 need to check with company hierarchy last level
                                {
                                    recordCount = recordDetails.Where(x => x.BranchId == locationObj.FirstOrDefault().BranchId && x.BranchCode.ToString().ToLower().Trim() == dr[orgLeafLevel.LeafLevelName + "Code"].ToString().ToLower().Trim()).Count();
                                    if (recordCount == 0)
                                    {
                                        retval = -1;
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Record already exists with the same code at " + columnHeaderCode + " ";
                                    }
                                }
                            }
                        }

                        // checking if name already exists in database with same level type for 1st level
                        if (branchDetails.Where(x => x.Text.ToString().ToLower().Trim() == dr[orgLeafLevel.LeafLevelName + "Code"].ToString().ToLower().Trim()).Count() > 0)
                        {
                            excelBranchid = branchDetails.Where(x => x.Text.ToString().ToLower().Trim() == dr[orgLeafLevel.LeafLevelName + "Code"].ToString().ToLower().Trim()).FirstOrDefault().Value;
                        }
                        //Commented by Priyanka B on 01042024 for SR010863 Start
                        //if (data.LevelType == 100 && recordDetails.Where(x => x.BranchId == excelBranchid && x.LocationTypeId == data.LevelType && x.LocationName.ToString().ToLower().Trim() == dr[columnHeaderName].ToString().ToLower().Trim() && x.LocationCode.ToString().ToLower().Trim() != dr[columnHeaderCode].ToString().ToLower().Trim()).Count() > 0)
                        //{
                        //    retval = -1;
                        //    nMsgCnt++;
                        //    sErrorMsg = sErrorMsg + nMsgCnt + ". Record already exists with the same name at " + columnHeaderName + " ";
                        //}
                        //// checking if name already exists in database with same level type for level types > 100 with the same parent
                        //if (data.LevelType != 100)
                        //{
                        //    locationObj = new List<LocationViewModel>();
                        //    locationObj = recordDetails.Where(x => x.CompanyId == companyId && x.LocationName.ToString().ToLower().Trim() == dr[columnHeaderName].ToString().ToLower().Trim() && x.LocationTypeId == data.LevelType && x.LocationCode.ToString().ToLower().Trim() != dr[columnHeaderCode].ToString().ToLower().Trim()).ToList();
                        //    if (locationObj.Count() > 0)
                        //    {
                        //        //Commented by Priyanka B on 13032024 for SR010863 Start
                        //        //recordCount = 1;
                        //        //recordCount = recordDetails.Where(x => x.LocationId == locationObj.FirstOrDefault().ParentLocationId && x.LocationCode == dr[databaseColumns.HierarchyDynamicList.Where(p => p.LevelType == (data.LevelType - 1)).FirstOrDefault().LevelName + "Code"].ToString()).Count();

                        //        //if (recordCount == 0)
                        //        //{
                        //        //    retval = -1;
                        //        //    nMsgCnt++;
                        //        //    sErrorMsg = sErrorMsg + nMsgCnt + ". Record already exists with the same name at " + columnHeaderName + " ";
                        //        //}
                        //        //Commented by Priyanka B on 13032024 for SR010863 End
                        //    }
                        //}
                        //Commented by Priyanka B on 01042024 for SR010863 End
                    }
                    if (sErrorMsg != "")
                    {
                        dtError.Rows.Add(dr.ItemArray);
                        dtError.Rows[dtError.Rows.Count - 1]["ErrorMessage"] = sErrorMsg;
                        v = 0;  //Added by Priyanka B on 16032024 for SR010863
                        continue;
                    }
                    v = 0;  //Added by Priyanka B on 16032024 for SR010863
                }
                return dtError;
            }
            finally
            {
                dtError?.Dispose();
            }
        }

        #endregion

        #region VendorDetails API
        [HttpPost]
        public HttpResponseMessage AddVendorDetails(VendorCustomerViewModel submitData)
        {
            var json = JsonConvert.SerializeObject(submitData.VendorCustomerDetails);
            DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);

            long companyId, userId;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());

            bool status = true;
            string message = "";

            if (excelDt == null)
            {
                status = false;
                message = "Json VendorDetails not found for import.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }

            var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();

            int VendorTypeID = 0;
            try
            {
                if (ValidateExcelColumns(colsList, GetVendorColumnsList()))
                {
                    if (excelDt.Rows.Count != 0)
                    {
                        DataTable dttable = ValidateImportVendor(excelDt, companyId);
                        if (dttable.Rows.Count == 0)
                        {
                            foreach (DataRow dr in excelDt.Rows)
                            {
                                var vendortypeidchk = (Convert.ToInt32(new VendorLookUps().GetVendorType().Where(x => x.Text.ToLower() == dr[ResourceFile.VendorTypelabel].ToString().ToLower().Trim()).FirstOrDefault().Value));
                                var v = _vendor.GetVendorTypeId(companyId, dr[ResourceFile.VendorNameLabel].ToString().Trim(), vendortypeidchk);
                                if (v.Count() == 0)
                                {
                                    var ovendorObj = new VendorViewModel
                                    {
                                        Createdby = userId,
                                        IsActive = true,
                                        VendorName = dr[ResourceFile.VendorNameLabel].ToString().Trim(),
                                        VendorTypeID = (Convert.ToInt32(new VendorLookUps().GetVendorType().Where(x => x.Text.ToLower() == dr[ResourceFile.VendorTypelabel].ToString().ToLower().Trim()).FirstOrDefault().Value)),
                                        //VendorTypeID= VendorTypeID,
                                        VendorCode = dr[ResourceFile.VendorCodeLabel].ToString().Trim(),
                                        Mobile = dr[ResourceFile.MobileNo].ToString().Trim(),
                                        City = dr[ResourceFile.City].ToString().Trim(),
                                        Phone = dr[ResourceFile.PhoneNo].ToString().Trim(),
                                        State = dr[ResourceFile.State].ToString().Trim(),
                                        CountryName = dr[ResourceFile.CountryLabel].ToString().Trim(),
                                        ZipCode = dr[ResourceFile.ZipCode].ToString().Trim(),
                                        VendorEmailId = dr[ResourceFile.EmailIdLabel].ToString().Trim(),
                                        PanNo = dr[ResourceFile.PanLabel].ToString().Trim(),
                                        TanNo = dr[ResourceFile.TinOrGst].ToString().Trim(),
                                        Description = dr[ResourceFile.Description].ToString(),
                                        Address = dr[ResourceFile.AddressLabel].ToString(),
                                        ContactPerson = dr[ResourceFile.ContactPerson].ToString(),
                                        CompanyID = companyId
                                    };

                                    int result = _vendor.AddVendor(ovendorObj);
                                }
                                else if (v.Count() > 0)
                                {
                                    var VendorName = dr[ResourceFile.VendorNameLabel].ToString().Trim();
                                    VendorViewModel vvm = _vendor.GetVendorByName(VendorName, companyId);
                                    var ovendorObj = new VendorViewModel
                                    {
                                        Createdby = userId,
                                        IsActive = true,
                                        VendorName = dr[ResourceFile.VendorNameLabel].ToString().Trim(),
                                        VendorTypeID = (Convert.ToInt32(new VendorLookUps().GetVendorType().Where(x => x.Text.ToLower() == dr[ResourceFile.VendorTypelabel].ToString().ToLower().Trim()).FirstOrDefault().Value)),
                                        VendorID = v.Select(x => x.Id).FirstOrDefault(),
                                        VendorCode = dr[ResourceFile.VendorCodeLabel].ToString().Trim(),
                                        Mobile = dr[ResourceFile.MobileNo].ToString().Trim(),
                                        City = dr[ResourceFile.City].ToString().Trim(),
                                        Phone = dr[ResourceFile.PhoneNo].ToString().Trim(),
                                        State = dr[ResourceFile.State].ToString().Trim(),
                                        CountryName = dr[ResourceFile.CountryLabel].ToString().Trim(),
                                        ZipCode = dr[ResourceFile.ZipCode].ToString().Trim(),
                                        VendorEmailId = dr[ResourceFile.EmailIdLabel].ToString().Trim(),
                                        PanNo = dr[ResourceFile.PanLabel].ToString().Trim(),
                                        TanNo = dr[ResourceFile.TinOrGst].ToString().Trim(),
                                        Description = dr[ResourceFile.Description].ToString(),
                                        Address = dr[ResourceFile.AddressLabel].ToString(),
                                        ContactPerson = dr[ResourceFile.ContactPerson].ToString(),
                                        CompanyID = companyId,
                                        AddressID = vvm.AddressID

                                    };

                                    int result = _vendor.UpdateVendor(ovendorObj);
                                }
                            }
                            status = true;
                            message = "Vendor added successfully.";
                        }
                        else
                        {
                            string JSONresult;
                            status = false;
                            JSONresult = JsonConvert.SerializeObject(dttable, Formatting.Indented);
                            var response = Request.CreateResponse(HttpStatusCode.OK);
                            response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                            return response;
                        }
                    }
                    else
                    {
                        status = false;
                        message = "Json VendorDetails not found for import";
                    }
                }
                else
                {
                    status = false;
                    message = "Json format does not match with defined Vendor.";

                }
            }
            catch (Exception ex)
            {
                status = false;
                message = ex.Message.ToString();
                message = !string.IsNullOrEmpty(message) ? message : ex.ToString();
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
        }

        private DataTable ValidateImportVendor(DataTable datatable, long companyId)
        {
            DataTable dterror = new DataTable();
            var colserror = new DataTable();
            foreach (DataColumn dc in datatable.Columns)
                colserror.Columns.Add(dc.ColumnName);
            colserror.Columns.Add("Error Message");
            string sErrorMsg = "";
            int nMsgCnt = 0;

            try
            {
                var vendors = new VendorLookUps().GetVendorType().ToList();
                {
                    foreach (DataRow dr in datatable.Rows)
                    {
                        sErrorMsg = "";
                        nMsgCnt = 0;

                        string strvendorName = null;
                        foreach (DataColumn dc in colserror.Columns)
                        {
                            if (dc.ColumnName == ResourceFile.VendorTypelabel)
                            {
                                if (dr[ResourceFile.VendorTypelabel].ToString().Trim() == "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Vendor Type should not be empty ";
                                }
                                else if (dr[ResourceFile.VendorTypelabel].ToString().Trim() != "")
                                {
                                    var vendortype = vendors.Where(x => x.Text.ToString().Trim().ToLower() == dr[ResourceFile.VendorTypelabel].ToString().Trim().ToLower()).FirstOrDefault();
                                    if (vendortype == null)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Vendor type doesn't exists ";
                                    }
                                    //else
                                    //{
                                    //    var resultuser = _vendor.CheckVendorname(companyId, strvendorName, Convert.ToInt32(vendortype.Value));
                                    //    if (resultuser == true)
                                    //    {
                                    //        nMsgCnt++;
                                    //        sErrorMsg = sErrorMsg + nMsgCnt + ". Vendor name already exists ";
                                    //    }
                                    //}
                                }
                            }
                            else if (dc.ColumnName == ResourceFile.VendorNameLabel)
                            {
                                if (dr[ResourceFile.VendorNameLabel].ToString().Trim() == "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Vendor Name should not be empty ";
                                }
                                else if (dr[ResourceFile.VendorNameLabel].ToString().Trim() != "")
                                {
                                    var validatevendorname = Regex.IsMatch(dr[ResourceFile.VendorNameLabel].ToString().Trim(), @"\t|""|\\");

                                    if (validatevendorname == true)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Special Characters Double Quotes, Back Slash and Tab Are Not Allowed In Vendor Name ";
                                    }
                                    else
                                        strvendorName = dr[ResourceFile.VendorNameLabel].ToString().Trim();
                                }
                                else if ((dr[ResourceFile.VendorNameLabel].ToString().Length > 100))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Vendor Name length should not be more than 100 characters ";
                                }
                            }
                            else if (dc.ColumnName == ResourceFile.VendorCodeLabel)
                            {
                                if (dr[ResourceFile.VendorCodeLabel].ToString().Trim() != "" && !regexSpecialCharacters2.IsMatch(dr[ResourceFile.VendorCodeLabel].ToString().Trim()))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". " + @"Vendor Code accepts letters (a-z), numbers (0-9), and charecters (-_/\&) ";
                                }
                                else if ((dr[ResourceFile.VendorCodeLabel].ToString().Length > 50))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Vendor Code length should not be more than 50 characters ";
                                }
                            }
                            else if (dc.ColumnName == ResourceFile.ZipCode)
                            {
                                if (dr[ResourceFile.ZipCode].ToString().Trim() != "")
                                {
                                    var zip = Regex.IsMatch(dr[ResourceFile.ZipCode].ToString().Trim(), @"^([0-9]{6})$");
                                    if (zip == false)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". ZIP code is not valid ";
                                    }
                                }
                            }
                            else if (dc.ColumnName == ResourceFile.City)
                            {
                                if (dr[ResourceFile.City].ToString().Trim() != "")
                                {
                                    var zip = Regex.IsMatch(dr[ResourceFile.City].ToString().Trim(), "^[a-zA-Z ]+$");
                                    if (zip == false)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". City is not valid ";
                                    }
                                }
                                else if ((dr[ResourceFile.City].ToString().Length == 30) || (dr[ResourceFile.City].ToString().Length >= 30))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". City length should not be more 30 characters ";
                                }
                            }
                            else if (dc.ColumnName == ResourceFile.State)
                            {
                                if (dr[ResourceFile.State].ToString().Trim() != "")
                                {
                                    var zip = Regex.IsMatch(dr[ResourceFile.State].ToString().Trim(), "^[a-zA-Z ]+$");
                                    if (zip == false)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". State is not valid ";
                                    }
                                }
                                else if ((dr[ResourceFile.State].ToString().Length == 30) || (dr[ResourceFile.State].ToString().Length >= 30))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". State length should not be more 30 characters ";
                                }
                            }
                            else if (dc.ColumnName == ResourceFile.CountryLabel)
                            {
                                if (dr[ResourceFile.CountryLabel].ToString().Trim() != "")
                                {
                                    var zip = Regex.IsMatch(dr[ResourceFile.CountryLabel].ToString().Trim(), "^[a-zA-Z ]+$");
                                    if (zip == false)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Country is not valid ";
                                    }
                                }
                                else if ((dr[ResourceFile.CountryLabel].ToString().Length > 30))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Country length should not be more than 30 characters ";
                                }
                            }
                            else if (dc.ColumnName == ResourceFile.TinOrGst)
                            {
                                if (dr[ResourceFile.TinOrGst].ToString().Trim() != "")
                                {
                                    if (dr[ResourceFile.TinOrGst].ToString().Length > 20)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". TIN or GSTIN is not valid ";
                                    }
                                }
                            }
                            else if (dc.ColumnName == ResourceFile.EmailIdLabel)
                            {
                                if (dr[ResourceFile.EmailIdLabel].ToString().Trim() != "")
                                {
                                    var value = IsValidEmailId(dr[ResourceFile.EmailIdLabel].ToString().Trim());
                                    if (value == false)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Email Id is not valid ";
                                    }
                                }
                            }
                            else if (dc.ColumnName == ResourceFile.AddressLabel)
                            {
                                if (dr[ResourceFile.AddressLabel].ToString().Trim() != "")
                                {
                                    if (dr[ResourceFile.AddressLabel].ToString().Length > 200)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Address length should not be more than 200 characters. ";
                                    }
                                }
                            }
                        }

                        if (sErrorMsg != "")
                        {
                            colserror.Rows.Add(dr.ItemArray);
                            colserror.Rows[colserror.Rows.Count - 1]["Error Message"] = sErrorMsg;
                            continue;
                        }
                    }
                    return colserror;
                }
            }
            finally
            {
                dterror?.Dispose();
            }
        }

        public List<ImportViewModel> GetVendorColumnsList()
        {
            var importViewModel = new List<ImportViewModel>
                {
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.VendorNameLabel,DisplayName = ResourceFile.VendorNameLabel ,ColumnDescription = ResourceFile.VendorNameLabel,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.VendorTypelabel,DisplayName = ResourceFile.VendorTypelabel ,ColumnDescription = ResourceFile.VendorTypelabel,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.VendorCodeLabel,DisplayName = ResourceFile.VendorCodeLabel ,ColumnDescription = ResourceFile.VendorCodeLabel,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.MobileNo,DisplayName = ResourceFile.MobileNo ,ColumnDescription = ResourceFile.MobileNo,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.PhoneNo,DisplayName = ResourceFile.PhoneNo ,ColumnDescription = ResourceFile.PhoneNo,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.AddressLabel,DisplayName = ResourceFile.AddressLabel ,ColumnDescription = ResourceFile.AddressLabel,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.City,DisplayName = ResourceFile.City ,ColumnDescription = ResourceFile.City,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.State,DisplayName = ResourceFile.State ,ColumnDescription = ResourceFile.State,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.CountryLabel,DisplayName = ResourceFile.CountryLabel ,ColumnDescription = ResourceFile.CountryLabel,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.ZipCode,DisplayName = ResourceFile.ZipCode ,ColumnDescription = ResourceFile.ZipCode,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.EmailIdLabel,DisplayName = ResourceFile.EmailIdLabel ,ColumnDescription = ResourceFile.EmailIdLabel,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.PanLabel,DisplayName = ResourceFile.PanLabel ,ColumnDescription = ResourceFile.PanLabel,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.TinOrGst,DisplayName = ResourceFile.TinOrGst ,ColumnDescription = ResourceFile.TinOrGst,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.Description,DisplayName = ResourceFile.Description ,ColumnDescription = ResourceFile.Description,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.ContactPerson,DisplayName = ResourceFile.ContactPerson ,ColumnDescription = ResourceFile.ContactPerson,Attribute = "notreq",DropDown = false},

                };
            return importViewModel;
        }

        #endregion

        #region User API
        [HttpPost]
        public HttpResponseMessage AddUserDetails(UserAPIViewModel submitData)
        {
            var json = JsonConvert.SerializeObject(submitData.UserDetails);
            DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);

            long companyId, userId;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());

            bool status = true;
            string message = "";

            ResultMessage result = new ResultMessage();  //Added by Priyanka B on 24062024 for ServiceDeskAPI

            if (excelDt == null)
            {
                status = false;
                message = "Json UserDetails not found for import.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }

            var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();

            try
            {
                if (ValidateExcelColumns(colsList, GetUserColumnsList()))
                {
                    if (excelDt.Rows.Count != 0)
                    {
                        DataTable dttable = ValidateImportUser(excelDt, companyId);
                        if (dttable.Rows.Count == 0)
                        {
                            foreach (DataRow dr in excelDt.Rows)
                            {
                                //Commented by Priyanka B on 24062024 for ServiceDeskAPI Start
                                //var user = new UserViewModel();
                                //user.FirstName = dr["FirstName"].ToString().Trim();
                                //user.LastName = dr["LastName"].ToString().Trim();
                                //user.FullName = dr["FirstName"].ToString().Trim() + "" + dr["LastName"].ToString().Trim();
                                //user.UserName = dr["UserName"].ToString().Trim().Replace(" ", "");
                                //user.EmailId = dr["Email"].ToString().Trim();
                                //user.Phone = dr["PhoneNumber"].ToString().Trim();
                                //user.Mobile = dr["MobileNumber"].ToString().Trim();
                                //user.Password = string.Empty;
                                //user.PasswordSalt = null;
                                //user.AssignedRoles = new List<CommonViewModel> { new CommonViewModel { Item = "103" } };
                                //if (dr["EmployeeId"].ToString().Trim() != "")
                                //{
                                //    user.EmpId = dr["EmployeeId"].ToString().Trim();
                                //}
                                //user.AssignedCompanies = new List<CommonViewModel> { new CommonViewModel { Item = companyId.ToString() } };

                                //user.MainLocationId = 0;
                                //user.RoleId = GetRolesWithPermissions(companyId).Where(x => x.ParentId == 103).FirstOrDefault().Id;
                                //user.CompanyId = companyId;
                                //var dt = _user.AddEmployee(user);
                                //UserViewModel user1 = _user.GetEmployees(companyId).Where(x => x.UserId == long.Parse(dt.Rows[0][0].ToString())).ToList().FirstOrDefault();
                                //if (dt.Rows.Count > 0)
                                //{
                                //    _importCon.sendNotifyMail("1", user1, "insert");
                                //}
                                //Commented by Priyanka B on 24062024 for ServiceDeskAPI End

                                //Modified by Priyanka B on 24062024 for ServiceDeskAPI Start
                                var model = new UserViewModel();
                                model.FirstName = dr["FirstName"].ToString().Trim();
                                model.LastName = dr["LastName"].ToString().Trim();
                                model.FullName = dr["FirstName"].ToString().Trim() + "" + dr["LastName"].ToString().Trim();
                                model.UserName = dr["UserName"].ToString().Trim().Replace(" ", "");
                                model.EmailId = dr["Email"].ToString().Trim();
                                model.Phone = dr["PhoneNumber"].ToString().Trim();
                                model.Mobile = dr["MobileNumber"].ToString().Trim();
                                model.Password = dr["Password"].ToString().Trim();
                                model.PasswordSalt = null;
                                model.ConfirmPassword = dr["ConfirmPassword"].ToString().Trim();
                                model.DeviceName = dr["DeviceName"].ToString().Trim();
                                model.RoleName = dr["RoleName"].ToString().Trim();
                                if (model.RoleName != "Asset User")
                                    model.IsServiceDesk = string.IsNullOrEmpty(dr["IsServiceDesk"].ToString().Trim()) ? false : Convert.ToBoolean(dr["IsServiceDesk"].ToString().Trim());
                                model.EmpId = dr["EmployeeId"].ToString().Trim();

                                model.RoleId = GetRolesWithPermissions(companyId).Where(x => x.Name == model.RoleName).FirstOrDefault().Id;
                                model.RoleTypeId = Convert.ToInt16(GetRolesWithPermissions(companyId).Where(x => x.Name == model.RoleName).FirstOrDefault().ParentId);
                                model.CompanyId = companyId;

                                if (model.RoleTypeId == 103)
                                {
                                    model.Password = "Tracet!%54321";
                                    model.ConfirmPassword = "Tracet!%54321";
                                }

                                if (model.RoleTypeId != 100)
                                {
                                    model.DepartmentList = _baseInterface.IDepartmentService.GetDepartments(companyId).Select(x => new NumericLookupItem { Text = x.Name, Value = x.DepartmentID }).ToList();
                                    string[] dlist = dr["Department"].ToString().Split(',');
                                    if ((dlist.Count() == 1 && dlist[0].ToString() != "") || dlist.Count() > 1)
                                    {
                                        foreach (string s in dlist)
                                        {
                                            if (model.DepartmentList.Where(x => x.Text.Trim() == s.Trim()).Select(x => x.Value).Count() > 0)
                                            {
                                                model.DepartmentIds = model.DepartmentIds
                                                + "," + model.DepartmentList.Where(x => x.Text.Trim() == s.Trim()).Select(x => x.Value).FirstOrDefault().ToString();
                                            }
                                            else
                                            {
                                                message = "Department(s) '" + s + "' not found ";
                                                status = false;
                                                goto Next;
                                            }
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(model.DepartmentIds))
                                    {
                                        if (model.DepartmentIds.StartsWith(","))
                                            model.DepartmentIds = model.DepartmentIds.Substring(1);
                                        if (model.DepartmentIds.EndsWith(","))
                                            model.DepartmentIds = model.DepartmentIds.Substring(0, model.DepartmentIds.Length - 1);
                                    }

                                    model.BranchList = _baseInterface.ICompany.GetAllBrancheDetails(companyId).Select(x => new NumericLookupItem { Text = x.Name, Value = x.BranchId }).ToList();
                                    string[] blist = dr["Branch"].ToString().Split(',');
                                    if ((blist.Count() == 1 && blist[0].ToString() != "") || blist.Count() > 1)
                                    {
                                        foreach (string s in blist)
                                        {
                                            if (model.BranchList.Where(x => x.Text.Trim() == s.Trim()).Select(x => x.Value).Count() > 0)
                                            {
                                                model.BranchIds = model.BranchIds
                                                + "," + model.BranchList.Where(x => x.Text.Trim() == s.Trim()).Select(x => x.Value).FirstOrDefault().ToString();
                                            }
                                            else
                                            {
                                                message = "Branch(s) '" + s + "' not found ";
                                                status = false;
                                                goto Next;
                                            }
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(model.BranchIds))
                                    {
                                        if (model.BranchIds.StartsWith(","))
                                            model.BranchIds = model.BranchIds.Substring(1);
                                        if (model.BranchIds.EndsWith(","))
                                            model.BranchIds = model.BranchIds.Substring(0, model.BranchIds.Length - 1);
                                    }

                                    OrganizationDetailsModel companydetails = _masterApi.GetOrganizationDetailsByCompId(companyId);
                                    model.MainCategoriesList = _masterApi.GetAssetCategories(companyId, userId, model.RoleTypeId.HasValue ? model.RoleTypeId.Value : 0, companydetails.FirmCategory ?? 0).Where(x => x.ParentID == null).Select(x => new NumericLookupItem { Text = x.Name, Value = x.AssetCategoryId }).ToList();
                                    string[] clist = dr["Categories"].ToString().Split(',');
                                    if ((clist.Count() == 1 && clist[0].ToString() != "") || clist.Count() > 1)
                                    {
                                        foreach (string s in clist)
                                        {
                                            if (model.MainCategoriesList.Where(x => x.Text.Trim() == s.Trim()).Select(x => x.Value).Count() > 0)
                                            {
                                                model.CategoryIds = model.CategoryIds
                                                + "," + model.MainCategoriesList.Where(x => x.Text.Trim() == s.Trim()).Select(x => x.Value).FirstOrDefault().ToString();
                                            }
                                            else
                                            {
                                                message = "Category(s) '" + s + "' not found ";
                                                status = false;
                                                goto Next;
                                            }
                                        }
                                    }

                                    if (!string.IsNullOrEmpty(model.CategoryIds))
                                    {
                                        if (model.CategoryIds.StartsWith(","))
                                            model.CategoryIds = model.CategoryIds.Substring(1);
                                        if (model.CategoryIds.EndsWith(","))
                                            model.CategoryIds = model.CategoryIds.Substring(0, model.BranchIds.Length - 1);
                                    }
                                }

                                if (status == true)
                                {
                                    if (ModelState.IsValid)
                                    {
                                        if (UserHelper.IsValidPassword(model.Password))
                                        {
                                            if (model.RoleTypeId == 103)
                                            {
                                                var ITAssetusers = _user.GetEmployees(companyId).Where(x => x.IsActivechk == true && x.ITAssetschk == true).ToList();
                                                var helper = new ADQFAMS.Web.Helpers.LicenseHelper();
                                                //var noofITAssetuser = helper.GetNoOfITAssetUsers();
                                                var noofITAssetuser = helper.GetNoOfITAssetUsers(companyId);
                                                var isAllowITAssetUserCreate = noofITAssetuser > ITAssetusers.Count;
                                                if (model.ITAssetschk)
                                                {
                                                    if (!isAllowITAssetUserCreate)
                                                    {
                                                        message = "Unable to create a new IT Asset user since the number of users limit is reached";
                                                        status = false;
                                                        goto Next;
                                                        //result.ReturnValue = "1";
                                                        //return Json(new { result }, JsonRequestBehavior.AllowGet);
                                                    }
                                                }
                                            }

                                            if (model.RoleTypeId != 103)
                                            {
                                                var users = _user.GetEmployees(companyId).Where(x => x.RoleTypeId != 103).ToList();

                                                var ITAssetusers = _user.GetEmployees(companyId).Where(x => x.IsActivechk == true && x.ITAssetschk == true).ToList();

                                                var helper = new ADQFAMS.Web.Helpers.LicenseHelper();
                                                //var noofuser = helper.GetNoOfUsers();
                                                var noofuser = helper.GetNoOfUsers(companyId);

                                                //var noofITAssetuser = helper.GetNoOfITAssetUsers();
                                                var noofITAssetuser = helper.GetNoOfITAssetUsers(companyId);
                                                var isAllowITAssetUserCreate = noofITAssetuser > ITAssetusers.Count;

                                                var isAllowUserCreate = noofuser > users.Count;

                                                var ServiceDeskUsers = _user.GetEmployees(companyId).Where(x => x.IsServiceDesk == true).ToList();
                                                //var noofServiceDeskUser = helper.GetNoOfServiceDeskUser();
                                                var noofServiceDeskUser = helper.GetNoOfServiceDeskUser(companyId);
                                                var isAllowServiceDeskUserCreate = noofServiceDeskUser > ServiceDeskUsers.Count;
                                                if (model.IsServiceDesk && isAllowUserCreate)
                                                {
                                                    if (!isAllowServiceDeskUserCreate)
                                                    {
                                                        message = "Unable to create a new Service Desk user since the number of users limit is reached";
                                                        status = false;
                                                        goto Next;
                                                        //result.ReturnValue = "1";
                                                        //return Json(new { result }, JsonRequestBehavior.AllowGet);
                                                    }
                                                }

                                                if (model.ITAssetschk && isAllowUserCreate)
                                                {
                                                    if (!isAllowITAssetUserCreate)
                                                    {
                                                        message = "Unable to create a new IT Asset user since the number of users limit is reached";
                                                        status = false;
                                                        goto Next;
                                                        //result.ReturnValue = "1";
                                                        //return Json(new { result }, JsonRequestBehavior.AllowGet);
                                                    }
                                                }

                                                if (!isAllowUserCreate)
                                                {
                                                    message = "Unable to create a new user since the number of users limit is reached";
                                                    status = false;
                                                    goto Next;
                                                    //result.ReturnValue = "1";
                                                    //return Json(new { result }, JsonRequestBehavior.AllowGet);
                                                }

                                            }
                                            model.CreatedBy = userId;
                                            //AuthUserViewModel authInfo = AuthenticationHelper.GetUserInfo();
                                            var authInfoDet = _user.GetEmployees(companyId).Where(x => x.UserId == userId).ToList().FirstOrDefault();
                                            if (authInfoDet != null && authInfoDet.RoleTypeId != 100 && model.RoleTypeId == 100)
                                            {
                                                message = "Limited Access User cannot add Root admin user.";
                                                status = false;
                                                goto Next;
                                                //result.ReturnValue = "";
                                                //return Json(new { result }, JsonRequestBehavior.AllowGet);
                                            }
                                            result = _userManagementApi.AddEmployee(model);

                                            /*List<UserAdditionalFieldsModel1> addFields = new List<UserAdditionalFieldsModel1>();
                                            if (model.AdditionalFields != null)
                                            {
                                                if (model.AdditionalFields.Count() > 0)
                                                {
                                                    //int resId = string.IsNullOrEmpty(result.ReturnValue) ? 0 : Convert.ToInt32(result.ReturnValue);
                                                    addFields = _commonAPI.ConvertModelToXMLData1(model.AdditionalFields, model.CompanyId, (int)TranscationTypes.User);
                                                    baseInterface.IUser.SaveAdditionalFields(result.Id, addFields, model.CompanyId, model.CreatedBy ?? 0, (int)TranscationTypes.User);
                                                }
                                            }*/

                                            //sendNotifyMail(result.ReturnValue.ToString(), model, "insert");

                                            //return Json(new { result }, JsonRequestBehavior.AllowGet);
                                        }
                                        else
                                        {
                                            //var message = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                                            message = "Password did not match the policy";
                                            status = false;
                                            goto Next;
                                            //result.ReturnValue = "";
                                            //return Json(new { result }, JsonRequestBehavior.AllowGet);
                                        }
                                    }
                                    else
                                    {

                                        message = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                                        status = false;
                                        goto Next;
                                        //result.ReturnValue = "";
                                        //return Json(new { result }, JsonRequestBehavior.AllowGet);
                                    }
                                }
                                //Modified by Priyanka B on 24062024 for ServiceDeskAPI End
                            }
                            //Added by Priyanka B on 24062024 for ServiceDeskAPI Start
                            if (status == true)
                            {
                                //Added by Priyanka B on 24062024 for ServiceDeskAPI End
                                status = true;
                                message = "User added successfully.";
                            }  //Added by Priyanka B on 24062024 for ServiceDeskAPI
                        }
                        else
                        {
                            string JSONresult;
                            status = false;
                            JSONresult = JsonConvert.SerializeObject(dttable, Formatting.Indented);
                            var response = Request.CreateResponse(HttpStatusCode.OK);
                            response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                            return response;
                        }
                    }
                    else
                    {
                        status = false;
                        message = "Json UserDetails not found for import";
                    }
                }
                else
                {
                    status = false;
                    message = "Json format does not match with defined User.";
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = ex.ToString();
                //message = !string.IsNullOrEmpty(message) ? message : ex.ToString();
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
            Next:  //Added by Priyanka B on 24062024 for ServiceDeskAPI
            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
        }

        //Added by Priyanka B on 24062024 for ServiceDeskAPI Start
        public static bool IsValidPassword(string InputPassword)
        {
            Regex regex = new Regex(@"[a-zA-Z0-9 \\\-~!@#$%^*()_+{}:|""?`;',./[\]]{5,20}");
            //var passwordExpression = @"[a-zA-Z0-9 \\\-~!@#$%^*()_+{}:|""?`;',./[\]]{5,20}";
            Match match = regex.Match(InputPassword);
            if (match.Success)
                return true;
            else
                return false;
        }
        //Added by Priyanka B on 24062024 for ServiceDeskAPI End

        private List<RoleViewModel> GetRolesWithPermissions(long companyId)
        {
            var roles = _user.GetRoles(companyId);
            // var rpermissions = UserRepository.GetRolePermissions();
            return (from r in roles
                    select new RoleViewModel
                    {
                        Id = r.Value,
                        Name = r.Text,
                        ParentId = r.ParentId
                    }).GroupBy(s => s.Name).Select(s => s.First()).ToList();
        }

        private DataTable ValidateImportUser(DataTable datatable, long companyId)
        {
            DataTable dterror = new DataTable();
            var colserror = new DataTable(); ;
            foreach (DataColumn dc in datatable.Columns)
                colserror.Columns.Add(dc.ColumnName);
            colserror.Columns.Add("Error Message");
            string sErrorMsg = "";
            int nMsgCnt = 0;

            foreach (DataRow dr in datatable.Rows)
            {
                sErrorMsg = "";
                nMsgCnt = 0;

                if (datatable.AsEnumerable().Where(x => x.Field<string>("UserName") == dr["UserName"].ToString()).Count() > 1)
                {
                    nMsgCnt++;
                    sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected with the same UserName in the Json";
                }
                else if (datatable.AsEnumerable().Where(x => x.Field<string>("Email") == dr["Email"].ToString()).Count() > 1)
                {
                    nMsgCnt++;
                    sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected with the same Email in the Json";
                }
                else if (datatable.AsEnumerable().Where(x => x.Field<string>("EmployeeId") == dr["EmployeeId"].ToString()).Count() > 1)
                {
                    nMsgCnt++;
                    sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected with the same EmployeeId in the Json";
                }
                else
                {
                    foreach (DataColumn dc in colserror.Columns)
                    {
                        if (dc.ColumnName == "FirstName")
                        {
                            if (dr["FirstName"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". First Name should not be empty ";
                            }
                            else if ((dr["FirstName"].ToString().Length > 100))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". First Name length should not be more than 100 characters ";
                            }
                        }
                        else if (dc.ColumnName == "LastName")
                        {
                            if (dr["LastName"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Last Name should not be empty ";

                            }
                            else if ((dr["LastName"].ToString().Length > 100))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Last Name length should not be more 100 characters ";
                            }
                        }
                        else if (dc.ColumnName == "EmployeeId")
                        {
                            if (dr["EmployeeId"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Employee Id should not be empty ";
                            }
                            else
                            {
                                var resultEmpId = _user.CheckEmpId(companyId, dr["EmployeeId"].ToString().Trim());
                                if (resultEmpId == true)
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Employee Id already exists ";
                                }
                            }
                        }
                        else if (dc.ColumnName == "UserName")
                        {
                            if (dr["UserName"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". User Name should not be empty ";
                            }
                            else if ((dr["UserName"].ToString().Length < 4))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". User Name length should not be less than 4 characters ";
                            }
                            else if ((dr["UserName"].ToString().Length > 50))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". User Name length should not be more 50 characters ";
                            }
                            else
                            {
                                var resultuser = _user.CheckUsername(companyId, dr["UserName"].ToString().Trim());
                                if (resultuser == true)
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". User Name already exists ";
                                }
                            }
                        }
                        else if (dc.ColumnName == "Email")
                        {
                            if (dr["Email"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Email Id should not be empty ";
                            }
                            else if (dr["Email"].ToString().Trim() != "")
                            {
                                var value = IsValidEmailId(dr["Email"].ToString().Trim());
                                if (value == false)
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Email Id is not Valid ";
                                }
                                var em = _user.CheckEmailId(companyId, dr["Email"].ToString().Trim());
                                if (em)
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Email Id already exists ";
                                }
                            }
                        }
                        //Added by Priyanka B on 24062024 for ServiceDeskAPI Start
                        else if (dc.ColumnName == "RoleName")
                        {
                            if (dr["RoleName"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Role Name should not be empty ";

                            }
                            var value = GetRolesWithPermissions(companyId).Where(x => x.Name == dr["RoleName"].ToString().Trim());
                            if (value != null)
                            {
                                if (value.Count() == 0)
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Role Name does not exists ";
                                }
                            }
                        }
                        else if (dc.ColumnName == "Password")
                        {
                            if (dr["RoleName"].ToString().Trim() != "Asset User")
                            {
                                if (dr["Password"].ToString().Trim() == "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Password should not be empty ";
                                }
                                else if (dr["Password"].ToString().Trim() != "")
                                {
                                    var value = IsValidPassword(dr["Password"].ToString().Trim());
                                    if (value == false)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Password is not Valid ";
                                    }
                                }
                            }
                            else if (dr["RoleName"].ToString().Trim() == "Asset User")
                            {
                                if (dr["Password"].ToString().Trim() != "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Password not required for 'Asset User' ";
                                }
                            }
                        }
                        else if (dc.ColumnName == "ConfirmPassword")
                        {
                            if (dr["RoleName"].ToString().Trim() != "Asset User")
                            {
                                if (dr["ConfirmPassword"].ToString().Trim() == "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". ConfirmPassword should not be empty ";
                                }
                                else if (dr["ConfirmPassword"].ToString().Trim() != "")
                                {
                                    var value = IsValidPassword(dr["ConfirmPassword"].ToString().Trim());
                                    if (value == false)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". ConfirmPassword is not Valid ";
                                    }
                                }
                            }
                            else if (dr["RoleName"].ToString().Trim() == "Asset User")
                            {
                                if (dr["ConfirmPassword"].ToString().Trim() != "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". ConfirmPassword not required for 'Asset User' ";
                                }
                            }
                        }

                        else if (dc.ColumnName == "Department")
                        {
                            if (dr["RoleName"].ToString().Trim() == "Root Admin")
                            {
                                if (dr["Department"].ToString().Trim() != "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Department not required for Root Admin ";
                                }
                            }
                            else
                            {
                                if (dr["Department"].ToString().Trim() == "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Department should not be empty ";
                                }
                            }
                        }
                        else if (dc.ColumnName == "Branch")
                        {
                            if (dr["RoleName"].ToString().Trim() == "Root Admin")
                            {
                                if (dr["Branch"].ToString().Trim() != "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Branch not required for Root Admin ";
                                }
                            }
                            else
                            {
                                if (dr["Branch"].ToString().Trim() == "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Branch should not be empty ";
                                }
                            }
                        }
                        else if (dc.ColumnName == "Categories")
                        {
                            if (dr["RoleName"].ToString().Trim() == "Root Admin")
                            {
                                if (dr["Categories"].ToString().Trim() != "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Categories not required for Root Admin ";
                                }
                            }
                            //else
                            //{
                            //    if (dr["Categories"].ToString().Trim() == "")
                            //    {
                            //        nMsgCnt++;
                            //        sErrorMsg = sErrorMsg + nMsgCnt + ". Categories should not be empty ";
                            //    }
                            //}
                        }
                        //Added by Priyanka B on 24062024 for ServiceDeskAPI End
                    }
                    //Added by Priyanka B on 24062024 for ServiceDeskAPI Start
                    if (dr["RoleName"].ToString().Trim() != "Asset User")
                    {
                        if (!string.IsNullOrEmpty(dr["Password"].ToString().Trim()) && !string.IsNullOrEmpty(dr["ConfirmPassword"].ToString().Trim()))
                        {
                            if (dr["Password"].ToString().Trim() != dr["ConfirmPassword"].ToString().Trim())
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Password and Confirm Password does not match ";
                            }
                        }
                    }
                    //Added by Priyanka B on 24062024 for ServiceDeskAPI End
                }
                if (sErrorMsg != "")
                {
                    colserror.Rows.Add(dr.ItemArray);
                    colserror.Rows[colserror.Rows.Count - 1]["Error Message"] = sErrorMsg;
                    continue;
                }
            }
            return colserror;
        }

        public List<ImportViewModel> GetUserColumnsList()
        {
            var importViewModel = new List<ImportViewModel>
                {
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.FirstNameLabel,DisplayName = ResourceFile.FirstNameLabel ,ColumnDescription = ResourceFile.FirstNameLabel,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.LastNameLabel,DisplayName = ResourceFile.LastNameLabel ,ColumnDescription = ResourceFile.LastNameLabel,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.EmailLabel,DisplayName = ResourceFile.EmailLabel ,ColumnDescription = ResourceFile.EmailLabel,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.MobileNumber,DisplayName = ResourceFile.MobileNumber ,ColumnDescription = ResourceFile.MobileNumber,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.PhoneNumber,DisplayName = ResourceFile.PhoneNumber ,ColumnDescription = ResourceFile.PhoneNumber,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.UserName,DisplayName = ResourceFile.UserName ,ColumnDescription = ResourceFile.UserName,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.EmployeeId,DisplayName = ResourceFile.EmployeeId ,ColumnDescription = ResourceFile.EmployeeId,Attribute = "required",DropDown = false},

                     //Added by Priyanka B on 24062024 for ServiceDeskAPI Start
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.DeviceName,DisplayName = ResourceFile.DeviceName ,ColumnDescription = ResourceFile.DeviceName,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.Password,DisplayName = ResourceFile.Password ,ColumnDescription = ResourceFile.Password,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.ConfirmPassword,DisplayName = ResourceFile.ConfirmPassword ,ColumnDescription = ResourceFile.ConfirmPassword,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = "RoleName",DisplayName = "RoleName" ,ColumnDescription = "RoleName",Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = "IsServiceDesk",DisplayName = "IsServiceDesk" ,ColumnDescription = "IsServiceDesk",Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = "Department",DisplayName = "Department" ,ColumnDescription = "Department",Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = "Categories",DisplayName = "Categories" ,ColumnDescription = "Categories",Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = "Branch",DisplayName = "Branch" ,ColumnDescription = "Branch",Attribute = "notreq",DropDown = false},
                    //Added by Priyanka B on 24062024 for ServiceDeskAPI End
                };
            return importViewModel;
        }

        #endregion

        #region CustomerDetails API
        [HttpPost]
        public HttpResponseMessage AddCustomerDetails(VendorCustomerViewModel submitData)
        {
            var json = JsonConvert.SerializeObject(submitData.VendorCustomerDetails);
            DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);

            long companyId, userId;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());

            bool status = true;
            string message = "";

            if (excelDt == null)
            {
                status = false;
                message = "Json CustomerDetails not found for import.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }

            var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();

            int VendorTypeID = 0;
            try
            {
                if (ValidateExcelColumns(colsList, GetCustomerColumnsList()))
                {
                    if (excelDt.Rows.Count != 0)
                    {
                        DataTable dttable = ValidateImportCustomer(excelDt, companyId);
                        if (dttable.Rows.Count == 0)
                        {
                            foreach (DataRow dr in excelDt.Rows)
                            {
                                string BranchIds = "";
                                var BranchList = _baseInterface.ICompany.GetAllBrancheDetails(companyId).Select(x => new NumericLookupItem { Text = x.Name, Value = x.BranchId }).ToList();
                                string[] blist = dr["Branch"].ToString().Split(',');
                                if ((blist.Count() == 1 && blist[0].ToString() != "") || blist.Count() > 1)
                                {
                                    foreach (string s in blist)
                                    {
                                        if (BranchList.Where(x => x.Text.Trim() == s.Trim()).Select(x => x.Value).Count() > 0)
                                        {
                                            BranchIds = BranchIds + "," + BranchList.Where(x => x.Text.Trim() == s.Trim()).Select(x => x.Value).FirstOrDefault().ToString();
                                        }
                                        else
                                        {
                                            message = "Branch(s) '" + s + "' not found ";
                                            status = false;
                                            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                                        }
                                    }
                                }
                                if (!string.IsNullOrEmpty(BranchIds))
                                {
                                    if (BranchIds.StartsWith(","))
                                        BranchIds = BranchIds.Substring(1);
                                    if (BranchIds.EndsWith(","))
                                        BranchIds = BranchIds.Substring(0, BranchIds.Length - 1);
                                }

                                var ovendorObj = new VendorViewModel
                                {
                                    Createdby = userId,
                                    IsActive = true,
                                    VendorName = dr[ResourceFile.CustomerNameLabel].ToString().Trim(),
                                    VendorTypeID = 110, //for customer
                                    Mobile = dr[ResourceFile.MobileNo].ToString().Trim(),
                                    City = dr[ResourceFile.City].ToString().Trim(),
                                    Phone = dr[ResourceFile.PhoneNo].ToString().Trim(),
                                    State = dr[ResourceFile.State].ToString().Trim(),
                                    CountryName = dr[ResourceFile.CountryLabel].ToString().Trim(),
                                    ZipCode = dr[ResourceFile.ZipCode].ToString().Trim(),
                                    VendorEmailId = dr[ResourceFile.EmailIdLabel].ToString().Trim(),
                                    PanNo = dr[ResourceFile.PanLabel].ToString().Trim(),
                                    TanNo = dr[ResourceFile.TinOrGst].ToString().Trim(),
                                    Description = dr[ResourceFile.Description].ToString(),
                                    AddOnAddress = dr[ResourceFile.AddressLabel].ToString(),
                                    ContactPerson = dr[ResourceFile.ContactPerson].ToString(),
                                    BranchIds = BranchIds,
                                    BillingAddress = dr[ResourceFile.BillingAddress].ToString(),
                                    Attachment1 = dr[ResourceFile.Attachment1].ToString(),
                                    Attachment2 = dr[ResourceFile.Attachment2].ToString(),
                                    Attachment3 = dr[ResourceFile.Attachment3].ToString(),
                                    CompanyID = companyId
                                };

                                //int result = _vendor.AddVendor(ovendorObj);
                                //if (result > 0 && dr[ResourceFile.MainLocation] != "" && dr[ResourceFile.SubLocation] != "")
                                //{
                                //    ovendorObj.MainLocation = dr[ResourceFile.MainLocation].ToString();
                                //    ovendorObj.SubLocation = dr[ResourceFile.SubLocation].ToString();
                                //    _vendor.AddMainSubLocation(ovendorObj, result);
                                //}
                                //status = true;
                                //message = "Customer added successfully.";

                                string strvendorName = "";
                                strvendorName = dr[ResourceFile.CustomerNameLabel].ToString().ToLower().Trim();
                                var resultuser = _vendor.CheckVendorname(companyId, strvendorName, 110);
                                if (resultuser == false)
                                {
                                    int result = _vendor.AddVendor(ovendorObj);
                                    if (result > 0 && dr[ResourceFile.MainLocation] != "" && dr[ResourceFile.SubLocation] != "")
                                    {
                                        ovendorObj.MainLocation = dr[ResourceFile.MainLocation].ToString();
                                        ovendorObj.SubLocation = dr[ResourceFile.SubLocation].ToString();
                                        _vendor.AddMainSubLocation(ovendorObj, result);
                                    }
                                    //status = true;
                                    //message = "Customer added successfully.";
                                }
                                else
                                {
                                    VendorViewModel vvm = _vendor.GetVendorByName(strvendorName, companyId);

                                    ovendorObj.VendorTypeID = 110;
                                    ovendorObj.AddressID = vvm.AddressID;
                                    ovendorObj.VendorID = vvm.VendorID;
                                    int result = _vendor.UpdateVendor(ovendorObj);
                                    if (result > 0)
                                    {
                                        ovendorObj.MainLocation = dr[ResourceFile.MainLocation].ToString();
                                        ovendorObj.SubLocation = dr[ResourceFile.SubLocation].ToString();
                                        _vendor.UpdateMainSubLocationAPI(ovendorObj, vvm.VendorID);
                                    }
                                }

                                status = true;
                                message = "Customer added successfully.";
                            }
                        }
                        else
                        {
                            string JSONresult;
                            status = false;
                            JSONresult = JsonConvert.SerializeObject(dttable, Formatting.Indented);
                            var response = Request.CreateResponse(HttpStatusCode.OK);
                            response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                            return response;
                        }
                    }
                    else
                    {
                        status = false;
                        message = "Json CustomerDetails not found for import";
                    }
                }
                else
                {
                    status = false;
                    message = "Json format does not match with defined Customer.";
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = ex.Message.ToString();
                message = !string.IsNullOrEmpty(message) ? message : ex.ToString();
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
        }

        public DataTable ValidateImportCustomer(DataTable datatable, long companyId)
        {
            DataTable dterror = new DataTable();
            var colserror = new DataTable(); ;
            foreach (DataColumn dc in datatable.Columns)
                colserror.Columns.Add(dc.ColumnName);
            colserror.Columns.Add("Error Message");
            var locationModel = new VendorLocationViewModel();
            locationModel.CustomerList = _iAssetService.GetLetOutVendors(companyId);
            var vendorLocations = _vendor.GetAllVendorLocations(companyId);
            string mainLocationName = ""; string subLocationName = "";
            string strvendorName = "";
            List<TripleText> excelDataList = new List<TripleText>();
            TripleText excelData = new TripleText();
            foreach (DataRow dr in datatable.Rows)
            {
                if (dr[ResourceFile.CustomerNameLabel].ToString().Trim() != "")
                {
                    excelData = new TripleText();
                    excelData.Text1 = dr[ResourceFile.CustomerNameLabel].ToString();
                    excelDataList.Add(excelData);
                }
            }
            string sErrorMsg = "";
            int nMsgCnt = 0;
            foreach (DataRow dr in datatable.Rows)
            {
                sErrorMsg = "";
                nMsgCnt = 0;

                strvendorName = null;
                mainLocationName = ""; subLocationName = "";
                foreach (DataColumn dc in colserror.Columns)
                {
                    if (dc.ColumnName == ResourceFile.CustomerNameLabel)
                    {
                        if (dr[ResourceFile.CustomerNameLabel].ToString().Trim() == "")
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Customer Name should not be empty ";
                        }
                        else if (dr[ResourceFile.CustomerNameLabel].ToString().Trim() != "" && (dr[ResourceFile.CustomerNameLabel].ToString().Length > 100))
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Customer Name length should not be more than 100 characters ";
                        }
                        else if (excelDataList.Where(x => x.Text1.ToString().ToLower().Trim() == dr[ResourceFile.CustomerNameLabel].ToString().ToLower().Trim()).Count() > 1)
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry of Customer name in json file ";
                        }
                        //else if (dr[ResourceFile.CustomerNameLabel].ToString().Trim() != "")
                        //{
                        //    strvendorName = dr[ResourceFile.CustomerNameLabel].ToString().ToLower().Trim();
                        //    var resultuser = _vendor.CheckVendorname(companyId, strvendorName, 110);
                        //    if (resultuser == true)
                        //    {
                        //        nMsgCnt++;
                        //        sErrorMsg = sErrorMsg + nMsgCnt + ". Customer name already exists ";
                        //    }
                        //}
                    }
                    else if (dc.ColumnName == ResourceFile.ZipCode)
                    {
                        if (dr["ZIP Code"].ToString().Trim() != "")
                        {
                            var zip = Regex.IsMatch(dr["ZIP Code"].ToString().Trim(), @"^([0-9]{6})$");
                            if (zip == false)
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". ZIP code is not valid ";
                            }
                        }
                    }
                    else if (dc.ColumnName == ResourceFile.City)
                    {
                        if (dr["City"].ToString().Trim() != "" && (dr["City"].ToString().Length == 30) || (dr["City"].ToString().Length >= 30))
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". City length should not be more 30 characters ";
                        }
                    }
                    else if (dc.ColumnName == ResourceFile.State)
                    {
                        if (dr["State"].ToString().Trim() != "" && (dr["State"].ToString().Length == 30) || (dr["State"].ToString().Length >= 30))
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". State length should not be more 30 characters ";
                        }
                    }
                    else if (dc.ColumnName == ResourceFile.CountryLabel)
                    {
                        if (dr["Country"].ToString().Trim() != "" && (dr["Country"].ToString().Length > 30))
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Country length should not be more than 30 characters ";
                        }
                    }
                    else if (dc.ColumnName == ResourceFile.TinOrGst)
                    {
                        if (dr[ResourceFile.TinOrGst].ToString().Trim() != "")
                        {
                            if (dr[ResourceFile.TinOrGst].ToString().Length > 20)
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". TIN or GSTIN length should not be more than 20 characters ";
                            }
                        }
                    }
                    else if (dc.ColumnName == ResourceFile.EmailIdLabel)
                    {
                        if (dr[ResourceFile.EmailIdLabel].ToString().Trim() != "")
                        {
                            var value = IsValidEmailId(dr[ResourceFile.EmailIdLabel].ToString().Trim());
                            if (value == false)
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Email Id is not valid ";
                            }
                        }
                    }
                    else if (dc.ColumnName == ResourceFile.AddressLabel)
                    {
                        if (dr[ResourceFile.AddressLabel].ToString().Trim() != "")
                        {
                            if (dr[ResourceFile.AddressLabel].ToString().Length > 500)
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". " + ResourceFile.VendorAddressMaxLengthMessage + " ";
                            }
                        }
                    }
                    else if (dc.ColumnName == ResourceFile.MainLocation)
                    {
                        if (dr[ResourceFile.MainLocation].ToString().Trim() == "")
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Main location is mandatory ";
                        }
                        else if (dr[ResourceFile.MainLocation].ToString().Trim() != "")
                        {
                            mainLocationName = dr[ResourceFile.MainLocation].ToString().ToLower().Trim();
                            if (vendorLocations != null && vendorLocations.Any(x => x.LocationName.Trim().ToLower() == mainLocationName && x.CustomerName == strvendorName && x.LocationTypeId == 100))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Main location already exists ";
                            }
                        }
                    }
                    else if (dc.ColumnName == ResourceFile.SubLocation)
                    {
                        if (dr[ResourceFile.SubLocation].ToString().Trim() == "")
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Sub location is mandatory ";
                        }
                    }
                }
                if (sErrorMsg != "")
                {
                    colserror.Rows.Add(dr.ItemArray);
                    colserror.Rows[colserror.Rows.Count - 1]["Error Message"] = sErrorMsg;
                    continue;
                }
            }
            return colserror;
        }

        public List<ImportViewModel> GetCustomerColumnsList()
        {
            var importViewModel = new List<ImportViewModel>
                {
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.CustomerNameLabel,DisplayName = ResourceFile.CustomerNameLabel ,ColumnDescription = ResourceFile.CustomerNameLabel,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.PanLabel,DisplayName = ResourceFile.PanLabel ,ColumnDescription = ResourceFile.PanLabel,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.TinOrGst,DisplayName = ResourceFile.TinOrGst ,ColumnDescription = ResourceFile.TinOrGst,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.AddressLabel,DisplayName = ResourceFile.AddressLabel ,ColumnDescription = ResourceFile.AddressLabel,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.City,DisplayName = ResourceFile.City ,ColumnDescription = ResourceFile.City,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.State,DisplayName = ResourceFile.State ,ColumnDescription = ResourceFile.State,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.CountryLabel,DisplayName = ResourceFile.CountryLabel ,ColumnDescription = ResourceFile.CountryLabel,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.ZipCode,DisplayName = ResourceFile.ZipCode ,ColumnDescription = ResourceFile.ZipCode,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.MobileNo,DisplayName = ResourceFile.MobileNo ,ColumnDescription = ResourceFile.MobileNo,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.PhoneNo,DisplayName = ResourceFile.PhoneNo ,ColumnDescription = ResourceFile.PhoneNo,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.EmailIdLabel,DisplayName = ResourceFile.EmailIdLabel ,ColumnDescription = ResourceFile.EmailIdLabel,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.MainLocation,DisplayName = ResourceFile.MainLocation ,ColumnDescription = ResourceFile.MainLocation,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.SubLocation,DisplayName = ResourceFile.SubLocation ,ColumnDescription = ResourceFile.SubLocation,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.Description,DisplayName = ResourceFile.Description ,ColumnDescription = ResourceFile.Description,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.ContactPerson,DisplayName = ResourceFile.ContactPerson ,ColumnDescription = ResourceFile.ContactPerson,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = "Branch",DisplayName = "Branch" ,ColumnDescription = "Branch",Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.BillingAddress,DisplayName = ResourceFile.BillingAddress ,ColumnDescription = ResourceFile.BillingAddress,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.Attachment1,DisplayName = ResourceFile.Attachment1 ,ColumnDescription = ResourceFile.Attachment1,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.Attachment2,DisplayName = ResourceFile.Attachment2 ,ColumnDescription = ResourceFile.Attachment2,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.Attachment3,DisplayName = ResourceFile.Attachment3 ,ColumnDescription = ResourceFile.Attachment3,Attribute = "notreq",DropDown = false},
                };
            return importViewModel;
        }

        bool ValidateExcelColumns(List<string> cols, List<ImportViewModel> actualColumns)
        {
            var frstCol = cols.FirstOrDefault();
            var listOfColumns = actualColumns;
            var dicsList = listOfColumns.Select(x => x.ColumnName).ToList();
            var mandatoryList = listOfColumns.Where(x => x.Required == true).Select(x => x.ColumnName).ToList();
            var result = false;
            for (int k = 0; k < mandatoryList.Count; k++)
            {
                result = true;
                if (!cols.Where(x => x.ToString() == mandatoryList[k]).Any())
                {
                    result = false;
                    break;
                }
            }
            if (result == false)
                return false;
            for (int j = 0; j < cols.Count; j++)
            {
                result = false;
                for (int i = 0; i < dicsList.Count; i++)
                {
                    if (dicsList[i] == cols[j])
                    {
                        result = true;
                        break;
                    }
                }
                if (result == false)
                    return false;
            }
            return true;
        }

        public static bool IsValidEmailId(string InputEmail)
        {
            Regex regex = new Regex(@"\b[a-zA-Z0-9._%-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}\b");
            Match match = regex.Match(InputEmail);
            if (match.Success)
                return true;
            else
                return false;
        }

        #endregion

        #region AssetCategory API
        [HttpPost]
        public HttpResponseMessage AddAssetCategoryDetails(AssetCategoryPostData submitData)
        {
            var json = JsonConvert.SerializeObject(submitData.AssetCategoryDetails);
            DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);

            long companyId, userId;

            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());

            string username, password;
            username = identity.Claims.FirstOrDefault(c => c.Type == "username").Value.ToString();
            password = identity.Claims.FirstOrDefault(c => c.Type == "password").Value.ToString();

            bool status = true;
            string message = "";

            if (excelDt == null)
            {
                status = false;
                message = "Json StoreDetails not found for import.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }

            var comDetails = _Company.GetCompanyDetails(companyId, userId, username, password);

            var depApplicable = comDetails.IsDepreciationApplicable;
            var usrId = userId;

            var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();

            DataTable dtError = new DataTable();
            var retval = 0;

            if (CommonHelper.ValidateExcelColumns(colsList, _importApi.GetAssetCategoryColumnsList(comDetails.IsDepreciationApplicable)))
            {
                if (excelDt.Rows.Count != 0)
                {
                    DataTable dttable = excelDt;
                    long? attributegroupid = 0; long? costbreakupgroupid = 0;
                    int lifespan;
                    double salvagevalue;
                    int salvageValueType;
                    string Maincode = string.Empty;
                    string Subcode = string.Empty;
                    var categories = _baseInterface.ICategoryService.GetAssetCategoryList(companyId);
                    DataColumnCollection categoryColumns = dttable.Columns;

                    if (dttable.Rows.Count > 0)
                    {
                        dtError = ValidateCategoryImportData(excelDt, companyId);
                        if (dtError.Rows.Count > 0)
                            retval = -1;
                        if (retval != -1)
                        {
                            var listOfOrgAttributeGroups = _baseInterface.ICategoryService.GetAdditionalFieldMapping(companyId).ToList();
                            var listOfOrgcostBreakUp = _baseInterface.ICategoryService.GetCostBreakupFieldMapping(companyId).ToList();
                            long subCatId = 0;
                            long mainCatId = 0;
                            long subCategoryId = 0;
                            long newCatId = 0;
                            List<ViewModels.AssetCategory.AssetCategoryViewModel> listOfOrgCategories = _baseInterface.ICategoryService.GetAssetCategoryList(companyId).ToList();
                            var description = "";
                            var assetacquis = "";
                            var assetacc = "";
                            var profitloss = "";
                            //var prefixvalue = "";
                            List<ViewModels.AssetCategory.AssetCategoryViewModel> catlist = new List<ViewModels.AssetCategory.AssetCategoryViewModel>();
                            List<ViewModels.AssetCategory.AssetCategoryViewModel> mainCategoryCurrentList = new List<ViewModels.AssetCategory.AssetCategoryViewModel>();
                            List<ViewModels.AssetCategory.AssetCategoryViewModel> subCategoryList = new List<ViewModels.AssetCategory.AssetCategoryViewModel>();
                            ViewModels.AssetCategory.AssetCategoryViewModel subCategoryCurrentList = new ViewModels.AssetCategory.AssetCategoryViewModel();
                            foreach (DataRow dr in excelDt.Rows)
                            {
                                subCatId = 0;
                                mainCatId = 0;
                                subCategoryId = 0;
                                newCatId = 0;
                                //subCategoryCurrentList = new ViewModels.AssetCategory.AssetCategoryViewModel();
                                Maincode = dr["Main category code"].ToString();
                                catlist = listOfOrgCategories.Where(x => x.ParentID == null && x.Code.ToLower().Trim() == Maincode.ToLower().Trim()).ToList();
                                if (mainCategoryCurrentList != null && mainCategoryCurrentList.Where(x => x.Code.Trim().ToLower() == Maincode.Trim().ToLower() && x.CompanyId == companyId && x.ParentID == null).Count() > 0)
                                    mainCatId = mainCategoryCurrentList.Where(x => x.Code.Trim().ToLower() == Maincode.Trim().ToLower() && x.CompanyId == companyId && x.ParentID == null).FirstOrDefault().Id;
                                else if (catlist.Count != 0)
                                    mainCatId = catlist.FirstOrDefault().AssetCategoryId;
                                subCategoryList = listOfOrgCategories.Where(x => x.ParentID == mainCatId && x.Code.ToLower().Trim() == dr["Sub category code"].ToString().ToLower().Trim()).ToList();
                                if (subCategoryList.Count != 0)
                                    subCategoryId = subCategoryList.FirstOrDefault().AssetCategoryId;
                                if (mainCatId > 0 && mainCategoryCurrentList.Where(x => x.Code.Trim().ToLower() == Maincode.Trim().ToLower() && x.CompanyId == companyId && x.ParentID == null).Count() <= 0)
                                {
                                    description = catlist.FirstOrDefault().Description;
                                    assetacquis = catlist.FirstOrDefault().BalAquAccount;
                                    assetacc = catlist.FirstOrDefault().BalAccAccount;
                                    profitloss = catlist.FirstOrDefault().ProfitLossAccount;
                                    if (dr["Main category description"].ToString() != "")
                                        description = dr["Main category description"].ToString();
                                    if (depApplicable)
                                    {
                                        if (dr["Asset Acquisition Account"].ToString() != "")
                                            assetacquis = dr["Asset Acquisition Account"].ToString();
                                        if (dr["Asset Depreciation Account"].ToString() != "")
                                            assetacc = dr["Asset Depreciation Account"].ToString();
                                        if (dr["Depreciation Account"].ToString() != "")
                                            profitloss = dr["Depreciation Account"].ToString();
                                    }
                                    _baseInterface.ICategoryService.UpdateCat(mainCatId, catlist.FirstOrDefault().Name, dr["Main category name"].ToString(), catlist.FirstOrDefault().ParentID, catlist.FirstOrDefault().AssetClassId, companyId, description, usrId, DateTime.Now, "", Maincode, catlist.FirstOrDefault().AdditionalGroupId.ToString(), catlist.FirstOrDefault().AdditionalCostGroupId, catlist.FirstOrDefault().LifeSpan, catlist.FirstOrDefault().SalvageValue, false, catlist.FirstOrDefault().SalvageValueType, assetacquis, assetacc, profitloss);
                                    subCategoryCurrentList = new ViewModels.AssetCategory.AssetCategoryViewModel { Name = dr["Main category name"].ToString(), Code = Maincode, Id = mainCatId, CompanyId = companyId, ParentID = null };
                                    mainCategoryCurrentList.Add(subCategoryCurrentList);
                                }
                                else if (mainCatId == 0)
                                {
                                    if (depApplicable)
                                    {
                                        newCatId = _baseInterface.ICategoryService.AddLevel1Category(dr["Main category name"].ToString(), Maincode, companyId, usrId, dr["Main category description"].ToString(), dr["Asset Acquisition Account"].ToString(), dr["Asset Depreciation Account"].ToString(), dr["Depreciation Account"].ToString());
                                    }
                                    else
                                    {
                                        newCatId = _baseInterface.ICategoryService.AddLevel1Category(dr["Main category name"].ToString(), Maincode, companyId, usrId, dr["Main category description"].ToString(), "", "", "");
                                    }
                                    subCategoryCurrentList = new ViewModels.AssetCategory.AssetCategoryViewModel { Name = dr["Main category name"].ToString(), Code = Maincode, Id = newCatId, CompanyId = companyId, ParentID = null };
                                    mainCategoryCurrentList.Add(subCategoryCurrentList);
                                }
                                if (depApplicable)
                                {
                                    lifespan = (dr["Life span"].ToString() != "" ? Convert.ToInt32(dr["Life span"]) : 0);
                                    if (categoryColumns.Contains("Salvage value percentage") && (dr["Salvage value percentage"].ToString() != "0" && dr["Salvage value percentage"].ToString().Length > 0))
                                        salvagevalue = Convert.ToDouble(dr["Salvage value percentage"]);
                                    else if (categoryColumns.Contains("Salvage value amount") && (dr["Salvage value amount"].ToString() != "0" && dr["Salvage value amount"].ToString().Length > 0))
                                        salvagevalue = Convert.ToDouble(dr["Salvage value amount"]);
                                    else
                                        salvagevalue = 0;
                                    if (categoryColumns.Contains("Salvage value percentage") && (dr["Salvage value percentage"].ToString() != "0" && dr["Salvage value percentage"].ToString().Length > 0))
                                        salvageValueType = (int)SalvageValueType.Rate;
                                    else if (categoryColumns.Contains("Salvage value amount") && (dr["Salvage value amount"].ToString() != "0" && dr["Salvage value amount"].ToString().Length > 0))
                                        salvageValueType = (int)SalvageValueType.Value;
                                    else
                                        salvageValueType = (int)SalvageValueType.Rate;
                                }
                                else
                                {
                                    lifespan = 0;
                                    salvagevalue = 0;
                                    salvageValueType = (int)SalvageValueType.Rate;
                                }
                                //prefix = dr["Prefix"].ToString();
                                Subcode = dr["Sub category code"].ToString();
                                if (dr["Attribute group"].ToString() != "")
                                    attributegroupid = listOfOrgAttributeGroups.Where(x => x.Name.ToString().Trim() == dr["Attribute group"].ToString().Trim()).FirstOrDefault().GroupingId;
                                if (dr["Costbreakup group"].ToString() != "")
                                    costbreakupgroupid = listOfOrgcostBreakUp.Where(x => x.Name.ToString().Trim().ToLower() == dr["Costbreakup group"].ToString().Trim().ToLower()).FirstOrDefault().GroupingId;
                                if (subCategoryId > 0)
                                {
                                    description = "";
                                    //prefixvalue = "";
                                    if (dr["Sub category description"].ToString() != "")
                                        description = dr["Sub category description"].ToString();
                                    else
                                        description = subCategoryList.FirstOrDefault().Description;
                                    //if (dr["Prefix"].ToString() != "")
                                    //    prefixvalue = dr["Prefix"].ToString();
                                    //else
                                    //    prefixvalue = catlist2.FirstOrDefault().Prefix;
                                    _baseInterface.ICategoryService.UpdateCat(subCategoryId, subCategoryList.FirstOrDefault().Name, dr["Sub category name"].ToString(), subCategoryList.FirstOrDefault().ParentID, subCategoryList.FirstOrDefault().AssetClassId, companyId, description, usrId, DateTime.Now, "", Subcode, attributegroupid.ToString(), costbreakupgroupid ?? 0, lifespan, lifespan, false, salvageValueType, subCategoryList.FirstOrDefault().BalAquAccount, subCategoryList.FirstOrDefault().BalAccAccount, subCategoryList.FirstOrDefault().ProfitLossAccount);
                                }
                                else if (!string.IsNullOrEmpty(dr["Sub category name"].ToString()) && !string.IsNullOrEmpty(Subcode))
                                {
                                    if (newCatId == 0)
                                        newCatId = mainCatId;
                                    subCatId = _baseInterface.ICategoryService.AddLevel2Category(companyId, Subcode, userId, newCatId, dr["Sub category name"].ToString(), dr["Sub category description"].ToString(), "", attributegroupid, costbreakupgroupid, lifespan, salvagevalue, salvageValueType);
                                }
                            }
                            status = true;
                            message = "Asset Category added successfully.";
                        }
                        else
                        {
                            string JSONresult;
                            status = false;
                            JSONresult = JsonConvert.SerializeObject(dtError, Formatting.Indented);
                            var response = Request.CreateResponse(HttpStatusCode.OK);
                            response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                            return response;
                        }
                    }
                    else
                    {
                        status = false;
                        message = "Json AssetCategoryDetails not found for import.";
                    }

                    if (retval == -1)
                    {
                        if (dtError.Rows.Count > 0)
                        {
                            string JSONresult;
                            status = false;
                            JSONresult = JsonConvert.SerializeObject(dtError, Formatting.Indented);
                            var response = Request.CreateResponse(HttpStatusCode.OK);
                            response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                            return response;
                        }
                    }
                }
                else
                {
                    status = false;
                    message = "Json AssetCategoryDetails not found for import.";
                }
            }
            else
            {
                status = false;
                message = "Json format does not match with defined Asset Category. ";
            }
            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
        }

        public DataTable ValidateCategoryImportData(DataTable dt, long companyId)  //, bool depApplicable, long usrId, int? roleTypeId, int? firmCategoryId)
        {
            var colserror = new DataTable();
            try
            {
                foreach (DataColumn dc in dt.Columns)
                    colserror.Columns.Add(dc.ColumnName);
                colserror.Columns.Add("Error Message");
                var category = _baseInterface.ICategoryService.GetAssetCategoryList(companyId);
                var errorValidation = false;
                var attributegroup = _baseInterface.ICategoryService.GetAdditionalFieldMapping(companyId).Select(x => new NumericLookupItem { Value = x.GroupingId, Text = x.Name }).ToList();
                var costBreakUp = _baseInterface.ICategoryService.GetCostBreakupFieldMapping(companyId).Select(x => new NumericLookupItem { Value = x.GroupingId, Text = x.Name }).ToList();
                var listOfLevel1Category = category.Where(x => x.ParentID == null && x.CompanyId == companyId).Select(
                    x => new NumericLookupItem
                    {
                        Value = x.AssetCategoryId,
                        Text = x.Name
                    }).ToList();

                string sErrorMsg = "";
                int nMsgCnt = 0;

                foreach (DataRow dr in dt.Rows)
                {
                    sErrorMsg = "";
                    nMsgCnt = 0;

                    if (dt.AsEnumerable().Where(x => dr["Sub category name"].ToString() != "" && dr["Main category name"].ToString() != "" && x.Field<string>("Sub category name").ToLower().Trim() == dr["Sub category name"].ToString().ToLower().Trim()
                    && x.Field<string>("Sub category name") == dr["Sub category name"].ToString() && x.Field<string>("Main category name").ToLower().Trim() == dr["Main category name"].ToString().ToLower().Trim()).Count() > 1)
                    {
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected with the same sub category name in the json file ";
                    }
                    else if (dt.AsEnumerable().Where(x => dr["Sub category code"].ToString() != "" && x.Field<string>("Sub category code").ToLower().Trim() == dr["Sub category code"].ToString().ToLower().Trim()
                    && x.Field<string>("Sub category code") == dr["Sub category code"].ToString()).Count() > 1)
                    {
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected with the same sub category code in the json file ";
                    }
                    else if (dt.AsEnumerable().Where(x => dr["Sub category code"].ToString() != "" && dr["Main category code"].ToString() != "" && x.Field<string>("Sub category code").ToLower().Trim() == dr["Main category code"].ToString().ToLower().Trim()
                    && x.Field<string>("Sub category code").ToLower().Trim() == dr["Main category code"].ToString().ToLower().Trim()).Count() > 1)
                    {
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected with the same sub category code in the json file ";
                    }
                    else
                    {
                        long mainCatId = 0;
                        long subCatId = 0;
                        List<ViewModels.AssetCategory.AssetCategoryViewModel> mainCategoryList = new List<ViewModels.AssetCategory.AssetCategoryViewModel>();
                        List<ViewModels.AssetCategory.AssetCategoryViewModel> subCategoryList = new List<ViewModels.AssetCategory.AssetCategoryViewModel>();
                        bool acqAccount;
                        bool depAccount;
                        double salvageValue = 0;
                        int life = 0;
                        foreach (DataColumn dc in dt.Columns)
                        {
                            //mainCatId = 0;
                            //subCatId = 0;
                            try
                            {
                                if (dc.ColumnName == "Main category name")
                                {
                                    if (dr["Main category name"].ToString() == "")
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Main category name cannot be empty ";
                                        errorValidation = true;
                                    }
                                    else if (dr["Main category name"].ToString().Length > 50)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Main category name length should not be more 50 characters ";
                                        errorValidation = true;
                                    }
                                    else if (dr["Main category name"].ToString() != "" && !regexSpecialCharacters4.IsMatch(dr["Main category name"].ToString().Trim()))
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Main Category Name accepts letters (a-z), numbers (0-9). Special Characters Double Quotes and Back Slash Are Not Allowed";
                                        errorValidation = true;
                                    }
                                }
                                else if (dc.ColumnName == "Main category code")
                                {
                                    if (dr["Main category code"].ToString() == "")
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Main category code cannot be empty ";
                                        errorValidation = true;
                                    }
                                    else if (dr["Main category code"].ToString().Length > 20)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Main category code length should not be more than 20 characters ";
                                        errorValidation = true;
                                    }
                                    mainCategoryList = category.Where(x => x.ParentID == null && x.Code.ToLower().Trim() == dr["Main category code"].ToString().ToLower().Trim()).ToList();
                                    if (mainCategoryList.Count != 0)
                                        mainCatId = mainCategoryList.FirstOrDefault().AssetCategoryId;
                                    else
                                        mainCatId = 0;
                                    if (category.Any(x => x.AssetCategoryId != mainCatId && x.Code != null && x.Code.ToLower().Trim() == dr["Main category code"].ToString().ToLower().Trim()))
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Main category code already exists ";
                                        errorValidation = true;
                                    }
                                }
                                else if (dc.ColumnName == "Asset Acquisition Account")
                                {
                                    if (dr["Asset Acquisition Account"].ToString() != "")
                                    {
                                        acqAccount = Regex.IsMatch(dr["Asset Acquisition Account"].ToString().Trim(), @"^[a-zA-Z0-9]*$");
                                        if (acqAccount == false)
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Asset Acquisition Account accepts only alpha numeric values ";
                                            errorValidation = true;
                                        }
                                        else if (dr["Asset Acquisition Account"].ToString().Length < 8)
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Asset Acquisition Account must be minimum of 8 characters ";
                                            errorValidation = true;
                                        }
                                        else if (dr["Asset Acquisition Account"].ToString() != "" && dr["Asset Depreciation Account"].ToString() != "" && dr["Asset Acquisition Account"].ToString() == dr["Asset Depreciation Account"].ToString())
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Asset Acquisition Account & Asset Depreciation Account can't be same ";
                                            errorValidation = true;
                                        }
                                    }
                                }
                                else if (dc.ColumnName == "Asset Depreciation Account")
                                {
                                    if (dr["Asset Depreciation Account"].ToString() != "")
                                    {
                                        depAccount = Regex.IsMatch(dr["Asset Depreciation Account"].ToString().Trim(), @"^[a-zA-Z0-9]*$");
                                        if (depAccount == false)
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Asset Depreciation Account accepts only alpha numeric values ";
                                            errorValidation = true;
                                        }
                                        else if (dr["Asset Depreciation Account"].ToString().Length < 8)
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Asset Depreciation Account must be minimum of 8 characters ";
                                            errorValidation = true;
                                        }
                                        else if (dr["Asset Depreciation Account"].ToString() != "" && dr["Depreciation Account"].ToString() != "" && dr["Asset Depreciation Account"].ToString() == dr["Depreciation Account"].ToString())
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Asset Depreciation Account & Depreciation Account can't be same ";
                                            errorValidation = true;
                                        }
                                    }
                                }
                                else if (dc.ColumnName == "Depreciation Account")
                                {
                                    if (dr["Depreciation Account"].ToString() != "")
                                    {
                                        depAccount = Regex.IsMatch(dr["Depreciation Account"].ToString().Trim(), @"^[a-zA-Z0-9]*$");
                                        if (depAccount == false)
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Depreciation Account accepts only alpha numeric values ";
                                            errorValidation = true;
                                        }
                                        else if (dr["Depreciation Account"].ToString().Length < 8)
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Depreciation Account must be minimum of 8 characters ";
                                            errorValidation = true;
                                        }
                                        else if (dr["Depreciation Account"].ToString() != "" && dr["Asset Acquisition Account"].ToString() != "" && dr["Depreciation Account"].ToString() == dr["Asset Acquisition Account"].ToString())
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Depreciation Account & Asset Acquisition Account can't be same ";
                                            errorValidation = true;
                                        }
                                    }
                                }
                                else if (dc.ColumnName == "Sub category name")
                                {
                                    if (!String.IsNullOrEmpty(dr["Sub category name"].ToString()))
                                    {
                                        if (dr["Sub category name"].ToString().Trim().Length > 50)
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Sub category name length should not be more than 50 characters ";
                                            errorValidation = true;
                                        }
                                        else if (dr["Sub category name"].ToString() != "" && !regexSpecialCharacters4.IsMatch(dr["Sub category name"].ToString().Trim()))
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Sub Category Name accepts letters (a-z), numbers (0-9). Special Characters Double Quotes and Back Slash Are Not Allowed";
                                            errorValidation = true;
                                        }
                                        else
                                        {
                                            subCategoryList = category.Where(x => x.ParentID != null && x.TypeId == 101 && x.ParentID == mainCatId && x.Name.ToLower().Trim() == dr["Sub category name"].ToString().ToLower().Trim()).ToList();
                                            if (subCategoryList.Count != 0)
                                                subCatId = subCategoryList.FirstOrDefault().AssetCategoryId;
                                            else
                                                subCatId = 0;
                                            if (subCatId > 0 && category.Any(x => x.AssetCategoryId != subCatId && x.ParentID == mainCatId && x.Name.ToLower().Trim() == dr["Sub category name"].ToString().ToLower().Trim()))
                                            {
                                                nMsgCnt++;
                                                sErrorMsg = sErrorMsg + nMsgCnt + ". Sub category name already exists ";
                                                errorValidation = true;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (dr["Sub category name"].ToString() == "")
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Sub category name cannot be empty ";
                                            errorValidation = true;
                                        }
                                    }
                                }
                                else if (dc.ColumnName == "Sub category code")
                                {
                                    if (!String.IsNullOrEmpty(dr["Sub category code"].ToString()))
                                    {
                                        if (dr["Sub category code"].ToString().Trim().Length > 20)
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Sub category code length should not be more than 20 characters ";
                                            errorValidation = true;
                                        }
                                        if (dr["Main category code"].ToString().ToLower().Trim() == dr["Sub category code"].ToString().ToLower().Trim())
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Main category code & Sub category code cannot be same ";
                                            errorValidation = true;
                                        }
                                        if (category.Any(x => x.AssetCategoryId != subCatId && x.Code != null && x.Code.ToLower().Trim() == dr["Sub category code"].ToString().ToLower().Trim()))
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Sub category code already exists ";
                                            errorValidation = true;
                                        }
                                    }
                                    else
                                    {
                                        if (dr["Sub category code"].ToString() == "")
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Sub category code cannot be empty ";
                                            errorValidation = true;
                                        }
                                    }
                                }
                                else if (dc.ColumnName == "Main category description")
                                {
                                    if (dr["Main category description"].ToString().Trim().Length > 500)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Main category description description length should not be more than 500 characters ";
                                        errorValidation = true;
                                    }
                                }
                                else if (dc.ColumnName == "Sub category description")
                                {
                                    if (dr["Sub category description"].ToString().Trim().Length > 500)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Main category description description length should not be more than 500 characters ";
                                        errorValidation = true;
                                    }
                                }
                                else if (dc.ColumnName == "Salvage value percentage")
                                {
                                    salvageValue = 0;
                                    if (dr["Salvage value percentage"].ToString() == "")
                                        dr["Salvage value percentage"] = 0;
                                    if (!Double.TryParse(dr["Salvage value percentage"].ToString(), out salvageValue))
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Salvage Value percentage accepts only double values ";
                                        errorValidation = true;
                                    }
                                }
                                else if (dc.ColumnName == "Salvage value amount")
                                {
                                    salvageValue = 0;
                                    if (dr["Salvage value amount"].ToString() == "")
                                        dr["Salvage value amount"] = 0;
                                    if (!Double.TryParse(dr["Salvage value amount"].ToString(), out salvageValue))
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Salvage Value amount accepts only double values ";
                                        errorValidation = true;
                                    }
                                }
                                else if (dc.ColumnName == "Life span")
                                {
                                    life = 0;
                                    if (dr["Life span"].ToString() == "")
                                        dr["Life span"] = 0;
                                    if (!int.TryParse(dr["Life span"].ToString(), out life))
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Asset life accepts only integers ";
                                        errorValidation = true;
                                    }
                                }
                                else if (dc.ColumnName == "Attribute group")
                                {
                                    if (dr["Attribute group"].ToString() != "" && attributegroup.Where(x => x.Text.ToLower().Trim() == dr["Attribute group"].ToString().ToLower().Trim()).Count() <= 0)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Enter valid Attribute group ";
                                        errorValidation = true;
                                    }
                                }
                                else if (dc.ColumnName.ToLower() == "costbreakup group")
                                {
                                    if (dr["Costbreakup group"].ToString() != "" && costBreakUp.Where(x => x.Text.ToLower().Trim() == dr["Costbreakup group"].ToString().ToLower().Trim()).Count() <= 0)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Enter valid Costbreakup group ";
                                        errorValidation = true;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    if (sErrorMsg != "")
                    {
                        colserror.Rows.Add(dr.ItemArray);
                        colserror.Rows[colserror.Rows.Count - 1]["Error Message"] = sErrorMsg;
                        continue;
                    }
                }

                return colserror;
            }
            finally
            {
                colserror?.Dispose();
            }
        }
        #endregion

        #region UserAttributesDetails API

        [HttpPost]
        public HttpResponseMessage AddUserAttributesDetails(UserAttributesAPI submitData)
        {
            long companyId, userId;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());

            bool status = true;
            string message = "";

            //Validations Start
            if (submitData == null)
            {
                status = false;
                message = "Json UserAttributesDetails not found for import.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
            if (string.IsNullOrEmpty(submitData.GroupName) || submitData.GroupName == null)
            {
                status = false;
                message = "Please Enter Group Name.";
                return Request.CreateResponse(HttpStatusCode.ExpectationFailed, new { status, message });
            }

            foreach (var d in submitData.ListOfUserAttributes)
            {
                if (string.IsNullOrEmpty(d.AdditionalFieldName))
                {
                    status = false;
                    message = "Please Enter Additional Field Name.";
                    return Request.CreateResponse(HttpStatusCode.ExpectationFailed, new { status, message });
                }
                if (string.IsNullOrEmpty(d.ControlName))
                {
                    status = false;
                    message = "Please Enter Control Name.";
                    return Request.CreateResponse(HttpStatusCode.ExpectationFailed, new { status, message });
                }
                var list = new AssetLookups().AdditionalFieldControls().Where(x => x.Text == d.ControlName);
                if (list.Count() == 0)
                {
                    status = false;
                    message = "Please Enter Valid ControlName.";
                    return Request.CreateResponse(HttpStatusCode.ExpectationFailed, new { status, message });
                }
            }
            //Validations End

            submitData.GroupId = 0;

            var i = 0;

            foreach (var d in submitData.ListOfUserAttributes)
            {
                if (string.IsNullOrEmpty(Convert.ToString(d.IsMandatory)))
                {
                    d.IsMandatory = false;
                }
                if (d.XmlFieldData == "")
                {
                    d.XmlFieldData = "[]";
                }
                else
                {
                    string[] s = d.XmlFieldData.Split(',');
                    List<XMLFieldDataAPI> xmlflddata = new List<XMLFieldDataAPI>();
                    for (int j = 0; j < s.Length; j++)
                    {
                        xmlflddata.Add(new XMLFieldDataAPI
                        {
                            Value = j + 1,
                            Text = s[j],
                            ParentId = 0,
                            TypeId = 0
                        });
                    }
                    // Convert list to JSON array
                    string json = JsonConvert.SerializeObject(xmlflddata, Formatting.Indented);
                    d.XmlFieldData = json;
                }
            }
            string parameter = string.Empty;
            XElement root = new XElement("Groups");

            var serializer = new JavaScriptSerializer();

            var additionalFieldModel = _iCategoryService.GetAdditionalFieldMapping(companyId);

            XElement elem1 = new XElement("Group"); elem1.SetAttributeValue("Name", submitData.GroupName.Trim());

            foreach (var data in submitData.ListOfUserAttributes)
            {
                var list = new AssetLookups().AdditionalFieldControls().Where(x => x.Text == data.ControlName);
                if (list.Count() == 0)
                {
                    status = false;
                    message = "Please Enter Valid ControlName.";
                    return Request.CreateResponse(HttpStatusCode.ExpectationFailed, new { status, message });
                }
                data.ControlTypeId = Convert.ToInt32(list.FirstOrDefault().Value);

                var checkstatus = _iCategoryService.CheckAdditionalGroupingFieldMapping2Details(data.AdditionalFieldName, companyId, 0);
                if(checkstatus==false)
                {
                    status = false;
                    message = "User Attribute name already exists.";
                    return Request.CreateResponse(HttpStatusCode.ExpectationFailed, new { status, message });
                }

                string AttributeType = new AssetLookups().AdditionalFieldControls().FirstOrDefault(x => x.Value == data.ControlTypeId).Text;
                i++;
                var invoiceItemList = serializer.Deserialize<List<NumericLookupItem>>(data.XmlFieldData);
                XElement ch1;
                if (invoiceItemList.Count > 0)
                    ch1 = new XElement("Attribute");
                else
                    ch1 = new XElement("Attribute", 0);
                ch1.SetAttributeValue("Id", i);
                ch1.SetAttributeValue("Name", data.AdditionalFieldName);
                ch1.SetAttributeValue("IsMandatory", data.IsMandatory ? "True" : "False");
                ch1.SetAttributeValue("AttributeType", AttributeType);
                ch1.SetAttributeValue("Value", "");

                int k = 0;
                foreach (var innerData in invoiceItemList)
                {
                    k++;
                    XElement child = new XElement("AdditionalDesign", 0);
                    child.SetAttributeValue("Id", innerData.Value);
                    child.SetAttributeValue("Text", innerData.Text);
                    ch1.Add(child);
                }
                elem1.Add(ch1);
            }

            root.Add(elem1);

            try
            {
                var status1 = false;
               
                    status1 = _iCategoryService.AddAdditionalGroupingFieldMapping2Details(submitData.GroupName, root.ToString(), userId, companyId);
                    if (status1 == true)
                    {
                        //return Json(new { Status = true, Message = "User Attribute Group Added Successfully" }, JsonRequestBehavior.AllowGet);
                        status = true;
                        message = "User Attribute Group Added Successfully";
                        return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                    }
                    else
                    {
                        //return Json(new { Status = false, Message = "Group name already exists" }, JsonRequestBehavior.AllowGet);
                        status = false;
                        message = "Group name already exists";
                        return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                    }
            }
            catch (Exception ex)
            {
                //return Json(new { Status = false, Message = ex.Message }, JsonRequestBehavior.AllowGet);
                status = false;
                message = ex.ToString();
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
        }
        #endregion

        #region Store API
        [HttpPost]
        public HttpResponseMessage AddStoreDetails(StoreViewModel submitData)
        {
            var json = JsonConvert.SerializeObject(submitData.StoreDetails);
            DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);

            long companyId, userId;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());

            var branchName = SingleTon.GetInstance.GetHierarchyMasterData(companyId).HierarchyBranchName;
            var stores = _consumable.GetAllStores(companyId);

            bool status = true;
            string message = "";

            if (excelDt == null)
            {
                status = false;
                message = "Json StoreDetails not found for import.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }

            var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();

            int VendorTypeID = 0;
            try
            {
                if (ValidateExcelColumns(colsList, GetStoreColumnsList()))
                {
                    if (excelDt.Rows.Count != 0)
                    {
                        DataTable dttable = ValidateImportStore(excelDt, companyId);
                        if (dttable.Rows.Count == 0)
                        {
                            foreach (DataRow dr in excelDt.Rows)
                            {
                                var BranchID = _consumable.GetBranchID(companyId, dr[ResourceFile.BranchName].ToString().Trim(), dr[ResourceFile.BranchCode].ToString().Trim());

                                var ovendorObj = new StoreMasterViewModel
                                {
                                    CreatedBy = userId,
                                    IsActive = true,
                                    StoreName = dr[ResourceFile.StoreName].ToString().Trim(),
                                    Description = dr[ResourceFile.StoreDescription].ToString().Trim(),
                                    CompanyId = companyId,
                                    CreatedDate = DateTime.Now,
                                    BranchId = BranchID
                                };

                                var result = _consumable.AddConsumableStoreMaster(ovendorObj);
                            }
                            status = true;
                            message = "Store added successfully.";
                        }
                        else
                        {
                            string JSONresult;
                            status = false;
                            JSONresult = JsonConvert.SerializeObject(dttable, Formatting.Indented);
                            var response = Request.CreateResponse(HttpStatusCode.OK);
                            response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                            return response;
                        }
                    }
                    else
                    {
                        status = false;
                        message = "Json StoreDetails not found for import";
                    }
                }
                else
                {
                    status = false;
                    message = "Json format does not match with defined Store.";

                }
            }
            catch (Exception ex)
            {
                status = false;
                message = ex.Message.ToString();
                message = !string.IsNullOrEmpty(message) ? message : ex.ToString();
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
        }

        public List<ImportViewModel> GetStoreColumnsList()
        {
            var importViewModel = new List<ImportViewModel>
              {
                  new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.StoreName,DisplayName = ResourceFile.StoreName ,ColumnDescription = ResourceFile.StoreName,Attribute = "required",DropDown = false},
                  new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.StoreDescription,DisplayName = ResourceFile.StoreDescription ,ColumnDescription = ResourceFile.StoreDescription,Attribute = "notreq",DropDown = false},
                  new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.BranchName,DisplayName = ResourceFile.BranchName ,ColumnDescription = ResourceFile.BranchName,Attribute = "required",DropDown = false },
                  new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.BranchCode,DisplayName = ResourceFile.BranchCode ,ColumnDescription = ResourceFile.BranchCode,Attribute = "required",DropDown = false }
            };
            return importViewModel;
        }

        private DataTable ValidateImportStore(DataTable datatable, long companyId)
        {
            var branchName = SingleTon.GetInstance.GetHierarchyMasterData(companyId).HierarchyBranchName;

            DataTable dterror = new DataTable();
            var colserror = new DataTable();
            foreach (DataColumn dc in datatable.Columns)
                colserror.Columns.Add(dc.ColumnName);
            colserror.Columns.Add("Error Message");
            string sErrorMsg = "";
            int nMsgCnt = 0;

            var stores = _consumable.GetAllStores(companyId);

            try
            {
                HashSet<string> storeNames = new HashSet<string>();

                foreach (DataRow dr in datatable.Rows)
                {
                    sErrorMsg = "";
                    nMsgCnt = 0;

                    //string strvendorName = null;
                    foreach (DataColumn dc in colserror.Columns)
                    {
                        if (dc.ColumnName == ResourceFile.StoreName)
                        {
                            //for branch validation start
                            var BranchID = _consumable.GetBranchID(companyId, dr[ResourceFile.BranchName].ToString().Trim(), dr[ResourceFile.BranchCode].ToString().Trim());
                            if (BranchID == 0)
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Branch not found. Please ensure that the Branch Name & Code are correct and try again. ";
                            }
                            //for branch validation end

                            if (dr[ResourceFile.BranchName].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Branch name should not be empty ";
                            }
                            if (dr[ResourceFile.BranchCode].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Branch code should not be empty ";
                            }
                            if (dr[ResourceFile.StoreName].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Store name should not be empty ";
                            }
                            else
                            {
                                string storeName = dr[ResourceFile.StoreName].ToString().Trim();
                                if (storeNames.Contains(storeName))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate store name found in JSON data. Please add different store name.";
                                }
                                else
                                {
                                    storeNames.Add(storeName);

                                    //var BranchID = _consumable.GetBranchID(companyId, dr[ResourceFile.BranchName].ToString().Trim(), dr[ResourceFile.BranchCode].ToString().Trim());
                                    if (stores.Any(x => x.StoreName.ToString().Trim().ToLower() == storeName.ToLower() && x.CompanyId == companyId && x.BranchId == BranchID))
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Store name already exists ";
                                    }
                                }
                            }
                        }
                    }

                    if (sErrorMsg != "")
                    {
                        colserror.Rows.Add(dr.ItemArray);
                        colserror.Rows[colserror.Rows.Count - 1]["Error Message"] = sErrorMsg;
                        continue;
                    }
                }
                return colserror;
            }
            finally
            {
                dterror?.Dispose();
            }
        }

        #endregion

        #region Unit Of Measure API
        [HttpPost]
        public HttpResponseMessage AddUnitOfMeasuresDetails(UOMViewModel submitData)
        {
            var json = JsonConvert.SerializeObject(submitData.UOMDetails);
            DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);
            long companyId, userId;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());
            bool status = true;
            string message = "";

            if (excelDt == null)
            {
                status = false;
                message = "Json UnitOfMeasuresDetails not found for import.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
            var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();
            try
            {
                if (ValidateExcelColumns(colsList, GetUnitOfMeasuresColumnsList()))
                {
                    if (excelDt.Rows.Count != 0)
                    {
                        DataTable dttable = ValidateImportUOM(excelDt, companyId);
                        if (dttable.Rows.Count == 0)
                        {
                            foreach (DataRow dr in excelDt.Rows)
                            {
                                var uom = new UnitMeasuresViewModel()
                                {
                                    CreatedBy = userId,
                                    CreatedDate = DateTime.Now,
                                    IsActive = true,
                                    Name = dr[ResourceFile.UOMName].ToString().Trim(),
                                    Description = dr[ResourceFile.Description].ToString().Trim(),
                                    CompanyId = companyId
                                };
                                var unitmeasureId = _consumable.AddUnitMeasure(uom);
                            }
                            status = true;
                            message = "Unit Of Measure added successfully.";
                        }
                        else
                        {
                            string JSONresult;
                            status = false;
                            JSONresult = JsonConvert.SerializeObject(dttable, Formatting.Indented);
                            var response = Request.CreateResponse(HttpStatusCode.OK);
                            response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                            return response;
                        }
                    }
                    else
                    {
                        status = false;
                        message = "Json Unit Of Measure Details not found for import";
                    }
                }
                else
                {
                    status = false;
                    message = "Json format does not match with defined Unit Of Measure.";
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = ex.Message.ToString();
                message = !string.IsNullOrEmpty(message) ? message : ex.ToString();
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    status,
                    message
                });
            }
            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
        }

        private DataTable ValidateImportUOM(DataTable datatable, long companyId)
        {
            DataTable dterror = new DataTable();
            var colserror = new DataTable(); ;
            foreach (DataColumn dc in datatable.Columns)
                colserror.Columns.Add(dc.ColumnName);
            colserror.Columns.Add("Error Message");
            string sErrorMsg = "";
            int nMsgCnt = 0;
            var unitmeasures = _consumable.GetAllUnitMeasures(companyId);

            foreach (DataRow dr in datatable.Rows)
            {
                sErrorMsg = "";
                nMsgCnt = 0;
                foreach (DataColumn dc in colserror.Columns)
                {
                    if (dc.ColumnName == ResourceFile.UOMName)
                    {
                        if (dr[ResourceFile.UOMName].ToString().Trim() == "")
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ".  Name should not be empty ";
                        }
                        else if (unitmeasures.Where(x => x.Name.Trim().ToLower() == dr[ResourceFile.UOMName].ToString().Trim().ToLower() && x.CompanyId == companyId).Count() > 0)
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Unit measure name already exists";
                        }
                    }
                }
                if (sErrorMsg != "")
                {
                    colserror.Rows.Add(dr.ItemArray);
                    colserror.Rows[colserror.Rows.Count - 1]["Error Message"] = sErrorMsg;
                    continue;
                }
            }
            return colserror;
        }

        public List<ImportViewModel> GetUnitOfMeasuresColumnsList()
        {
            var importViewModel = new List<ImportViewModel>
                 {
                     new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.UOMName,DisplayName = ResourceFile.UOMName ,ColumnDescription = ResourceFile.UOMName,Attribute = "required",DropDown = false},
                     new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.Description,DisplayName = ResourceFile.Description ,ColumnDescription = ResourceFile.Description,Attribute = "notreq",DropDown = false},
                 };
            return importViewModel;
        }
        #endregion

        #region ItemCategory API
        [HttpPost]
        public HttpResponseMessage AddItemCategoryDetails(ItemCategoryAPIViewModel submitData)
        {
            try
            {
                var json = JsonConvert.SerializeObject(submitData.ItemCategoryDetails);
                DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);

                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());
                bool status = true;
                string message = "";

                
                var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();
                try
                {
                    if (ValidateExcelColumns(colsList, GetItemCategoryColumnsList()))
                    {
                        if (excelDt.Rows.Count != 0)
                        {
                            DataTable dttable = ValidateItemCategoryImportData(excelDt, companyId);
                            if (dttable.Rows.Count == 0)
                            {
                                foreach (DataRow dr in excelDt.Rows)
                                {
                                    long MainUOMID = _consumable.GetUOMId(companyId, dr[ResourceFile.MUOM].ToString().Trim());
                                    long SubUOMID = _consumable.GetUOMId(companyId, dr[ResourceFile.SUOM].ToString().Trim());
                                    var maincategory = new ViewModels.Consumable.ItemCategoryViewModel
                                    {
                                        CreatedBy = userId,
                                        IsActive = true,
                                        ItemCategoryName = dr[ResourceFile.MainCategoryNameLabel].ToString().Trim(),
                                        CompanyId = companyId,
                                        CreatedDate = DateTime.Now,
                                        Code = dr[ResourceFile.MainCategoryCodeLabel].ToString().Trim(),
                                        Description = dr[ResourceFile.MainCategoryDescription].ToString().Trim(),
                                        UnitOfMeasure = MainUOMID,
                                    };
                                    //for update start
                                    var mainCatid = _consumable.GetmainCatName(companyId, dr[ResourceFile.MainCategoryCodeLabel].ToString().Trim(), dr[ResourceFile.MainCategoryNameLabel].ToString().Trim());
                                    
                                    if (mainCatid > 0)
                                    {
                                        long newCatId = _consumable.EditItemCategoryAPi(maincategory, mainCatid);
                                        if (newCatId > 0)
                                        {
                                            var subcategory = new ViewModels.Consumable.ItemCategoryViewModel
                                            {
                                                CreatedBy = userId,
                                                IsActive = true,
                                                ItemCategoryName = dr[ResourceFile.SubCategoryNameLabel].ToString().Trim(),
                                                CompanyId = companyId,
                                                CreatedDate = DateTime.Now,
                                                Code = dr[ResourceFile.SubCategoryCodeLabel].ToString().Trim(),
                                                Description = dr[ResourceFile.SubCategoryDescription].ToString().Trim(),
                                                UnitOfMeasure = SubUOMID,
                                                ParentId = newCatId
                                            };
                                            var subCatid = _consumable.GetsubCatName(companyId, dr[ResourceFile.SubCategoryCodeLabel].ToString().Trim(), dr[ResourceFile.SubCategoryNameLabel].ToString().Trim());
                                            
                                            if (subCatid > 0)
                                            {
                                                var result = _consumable.EditItemCategoryAPi(subcategory, subCatid);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        long newCatId = _consumable.AddItemCategory(maincategory);
                                        if (newCatId > 0)
                                        {
                                            var subcategory = new ViewModels.Consumable.ItemCategoryViewModel
                                            {
                                                CreatedBy = userId,
                                                IsActive = true,
                                                ItemCategoryName = dr[ResourceFile.SubCategoryNameLabel].ToString().Trim(),
                                                CompanyId = companyId,
                                                CreatedDate = DateTime.Now,
                                                Code = dr[ResourceFile.SubCategoryCodeLabel].ToString().Trim(),
                                                Description = dr[ResourceFile.SubCategoryDescription].ToString().Trim(),
                                                UnitOfMeasure = SubUOMID,
                                                ParentId = newCatId
                                            };
                                            var result = _consumable.AddItemCategory(subcategory);
                                        }
                                    }
                                }
                                message = "Item Category added successfully.";
                                status = true;

                            }
                            else
                            {
                                string JSONresult;
                                status = false;
                                JSONresult = JsonConvert.SerializeObject(dttable, Formatting.Indented);
                                var response = Request.CreateResponse(HttpStatusCode.OK);
                                response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                                return response;
                            }
                        }
                        else
                        {
                            status = false;
                            message = "Json ItemCategoryDetails not found for import.";
                        }
                    }
                    else
                    {
                        status = false;
                        message = "Json format does not match with defined Item Category.";

                    }
                }
                catch (Exception ex)
                {
                    status = false;
                    message = ex.Message.ToString();
                    message = !string.IsNullOrEmpty(message) ? message : ex.ToString();
                    return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                }
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
            catch (Exception ex)
            {
                bool status = false;
                string message = ex.Message.ToString();
                File.WriteAllText(@"D:\csc.txt", ex.ToString());
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
        }

        public List<ImportViewModel> GetItemCategoryColumnsList()
        {
            var importViewModel = new List<ImportViewModel>();

            importViewModel.Add(new ImportViewModel { Checked = true, Required = true, ColumnName = ResourceFile.MainCategoryNameLabel, DisplayName = ResourceFile.MainCategoryNameLabel, ColumnDescription = ResourceFile.MainCategoryNameLabel, Attribute = "required", DropDown = false });
            importViewModel.Add(new ImportViewModel { Checked = true, Required = true, ColumnName = ResourceFile.MainCategoryCodeLabel, DisplayName = ResourceFile.MainCategoryCodeLabel, ColumnDescription = ResourceFile.MainCategoryCodeLabel, Attribute = "required", DropDown = false });
            importViewModel.Add(new ImportViewModel { Checked = true, Required = false, ColumnName = ResourceFile.MainCategoryDescription, DisplayName = ResourceFile.MainCategoryDescription, ColumnDescription = ResourceFile.MainCategoryDescription, Attribute = "notreq", DropDown = false });
            importViewModel.Add(new ImportViewModel { Checked = true, Required = true, ColumnName = ResourceFile.SubCategoryNameLabel, DisplayName = ResourceFile.SubCategoryNameLabel, ColumnDescription = ResourceFile.SubCategoryNameLabel, Attribute = "required", DropDown = false });
            importViewModel.Add(new ImportViewModel { Checked = true, Required = true, ColumnName = ResourceFile.SubCategoryCodeLabel, DisplayName = ResourceFile.SubCategoryCodeLabel, ColumnDescription = ResourceFile.SubCategoryCodeLabel, Attribute = "required", DropDown = false });
            importViewModel.Add(new ImportViewModel { Checked = true, Required = false, ColumnName = ResourceFile.SubCategoryDescription, DisplayName = ResourceFile.SubCategoryDescription, ColumnDescription = ResourceFile.SubCategoryDescription, Attribute = "notreq", DropDown = false });
            //importViewModel.Add(new ImportViewModel { Checked = true, Required = false, ColumnName = ResourceFile.UOM, DisplayName = ResourceFile.UOM, ColumnDescription = ResourceFile.UOM, Attribute = "required", DropDown = false });
            importViewModel.Add(new ImportViewModel { Checked = true, Required = false, ColumnName = ResourceFile.MUOM, DisplayName = ResourceFile.MUOM, ColumnDescription = ResourceFile.MUOM, Attribute = "required", DropDown = false });
            importViewModel.Add(new ImportViewModel { Checked = true, Required = false, ColumnName = ResourceFile.SUOM, DisplayName = ResourceFile.SUOM, ColumnDescription = ResourceFile.SUOM, Attribute = "required", DropDown = false });

            return importViewModel;
        }

        public DataTable ValidateItemCategoryImportData(DataTable dt, long companyId)
        {
            var colserror = new DataTable();
            try
            {
                foreach (DataColumn dc in dt.Columns)
                    colserror.Columns.Add(dc.ColumnName);
                colserror.Columns.Add("Error Message");
                
                var category = _consumable.GetAllItemCategories(AuthenticationHelper.GetCompanyID());
                var errorValidation = false;
                var listOfLevel1Category = category.Where(x => x.ParentId == null && x.CompanyId == companyId).Select(
                    x => new NumericLookupItem
                    {
                        Value = x.ItemCategoryId,
                        Text = x.ItemCategoryName
                    }).ToList();
                var itemcategorylist = _consumable.GetAllItemCategories(companyId);
                string sErrorMsg = "";
                int nMsgCnt = 0;

                foreach (DataRow dr in dt.Rows)
                {
                    sErrorMsg = "";
                    nMsgCnt = 0;

                    var MainUOMID = _consumable.GetUOMId(companyId, dr[ResourceFile.MUOM].ToString().Trim());
                    if (MainUOMID == 0 && dr[ResourceFile.MUOM].ToString().Trim() != "")
                    {
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". Main Category UOM not found. Please ensure that the uom name is correct and try again. ";
                    }
                    var SubUOMID = _consumable.GetUOMId(companyId, dr[ResourceFile.SUOM].ToString().Trim());
                    if (SubUOMID == 0 && dr[ResourceFile.SUOM].ToString().Trim() != "")
                    {
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". Sub Category UOM not found. Please ensure that the uom name is correct and try again. ";
                    }
                    //var mainCat = _consumable.GetmainCat(companyId, dr[ResourceFile.MainCategoryCodeLabel].ToString().Trim(), dr[ResourceFile.MainCategoryNameLabel].ToString().Trim());
                    //if (mainCat == 0)
                    //{
                    //    nMsgCnt++;
                    //    sErrorMsg = sErrorMsg + nMsgCnt + ". Main Category Code already exists. ";
                    //}

                    //var subCat = _consumable.GetsubCat(companyId, dr[ResourceFile.SubCategoryCodeLabel].ToString().Trim(), dr[ResourceFile.SubCategoryNameLabel].ToString().Trim());
                    //if (subCat == 0)
                    //{
                    //    nMsgCnt++;
                    //    sErrorMsg = sErrorMsg + nMsgCnt + ". Sub Category Code already exists. ";
                    //}

                    var mainCatName = _consumable.GetmainCatName(companyId, dr[ResourceFile.MainCategoryCodeLabel].ToString().Trim(), dr[ResourceFile.MainCategoryNameLabel].ToString().Trim());
                    var mainCatCode = _consumable.GetmainCatCode(companyId, dr[ResourceFile.MainCategoryCodeLabel].ToString().Trim(), dr[ResourceFile.MainCategoryNameLabel].ToString().Trim());
                    var subCatName = _consumable.GetsubCatName(companyId, dr[ResourceFile.SubCategoryCodeLabel].ToString().Trim(), dr[ResourceFile.SubCategoryNameLabel].ToString().Trim());
                    var subCatCode = _consumable.GetsubCatCode(companyId, dr[ResourceFile.SubCategoryCodeLabel].ToString().Trim(), dr[ResourceFile.SubCategoryNameLabel].ToString().Trim());
                    if (!(mainCatCode > 0) && (mainCatName > 0))
                    {
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". Main category name already exists and code is different  ";
                    }
                    if ((mainCatCode > 0) && !(mainCatName > 0))
                    {
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". Main category code already exists and name is different";
                    }

                    if ((subCatName > 0) && !(subCatCode > 0))
                    {
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". Sub category Name already exists and code is different ";
                    }
                    if (!(subCatName > 0) && (subCatCode > 0))
                    {
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". Sub category code already exists and name is different ";
                    }

                    if (dt.AsEnumerable().Where(x => dr[ResourceFile.MainCategoryNameLabel].ToString() != ""
                    && x.Field<string>(ResourceFile.MainCategoryNameLabel).ToLower().Trim() == dr[ResourceFile.MainCategoryNameLabel].ToString().ToLower().Trim()).Count() > 1)
                    {
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected with the same main category name in Json data. ";
                    }
                    if (dt.AsEnumerable().Where(x => dr[ResourceFile.MainCategoryCodeLabel].ToString() != ""
                    && x.Field<string>(ResourceFile.MainCategoryCodeLabel).ToLower().Trim() == dr[ResourceFile.MainCategoryCodeLabel].ToString().ToLower().Trim()).Count() > 1)
                    {

                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected with the same main category code in Json data. ";
                    }

                    if (dt.AsEnumerable().Where(x => dr[ResourceFile.SubCategoryNameLabel].ToString() != "" && dr[ResourceFile.MainCategoryNameLabel].ToString() != ""
                    && x.Field<string>(ResourceFile.SubCategoryNameLabel).ToLower().Trim() == dr[ResourceFile.SubCategoryNameLabel].ToString().ToLower().Trim()
                    //&& x.Field<string>(ResourceFile.SubCategoryNameLabel) == dr[ResourceFile.SubCategoryNameLabel].ToString() 
                    //&& x.Field<string>(ResourceFile.MainCategoryNameLabel).ToLower().Trim() == dr[ResourceFile.MainCategoryNameLabel].ToString().ToLower().Trim()
                    ).Count() > 1)
                    {
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected with the same sub category name in Json data. ";
                    }
                    if (dt.AsEnumerable().Where(x => dr[ResourceFile.SubCategoryCodeLabel].ToString() != ""
                    && x.Field<string>(ResourceFile.SubCategoryCodeLabel).ToLower().Trim() == dr[ResourceFile.SubCategoryCodeLabel].ToString().ToLower().Trim()
                    && x.Field<string>(ResourceFile.SubCategoryCodeLabel) == dr[ResourceFile.SubCategoryCodeLabel].ToString()).Count() > 1)
                    {

                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected with the same sub category code in Json data ";
                    }
                    //else if (dt.AsEnumerable().Where(x => dr[ResourceFile.SubCategoryCodeLabel].ToString() != "" && dr[ResourceFile.MainCategoryCodeLabel].ToString() != ""
                    //&& x.Field<string>(ResourceFile.SubCategoryCodeLabel).ToLower().Trim() == dr[ResourceFile.MainCategoryCodeLabel].ToString().ToLower().Trim()
                    //&& x.Field<string>(ResourceFile.SubCategoryCodeLabel).ToLower().Trim() == dr[ResourceFile.MainCategoryCodeLabel].ToString().ToLower().Trim()).Count() > 1)
                    //{
                    //    nMsgCnt++;
                    //    sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected with the same sub category code in Json data. ";
                    //}
                    else
                    {
                        //long mainCatId = 0;
                        //long subCatId = 0;
                        //List<ViewModels.Consumable.ItemCategoryViewModel> mainCategoryList = new List<ViewModels.Consumable.ItemCategoryViewModel>();
                        //List<ViewModels.Consumable.ItemCategoryViewModel> subCategoryList = new List<ViewModels.Consumable.ItemCategoryViewModel>();
                        //bool acqAccount;
                        //bool depAccount;
                        //double salvageValue = 0;
                        //int life = 0;
                        foreach (DataColumn dc in dt.Columns)
                        {
                            //mainCatId = 0;
                            //subCatId = 0;
                            try
                            {
                                if (dc.ColumnName == ResourceFile.MainCategoryNameLabel)
                                {
                                    if (dr[ResourceFile.MainCategoryNameLabel].ToString() == "")
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Main category name cannot be empty ";
                                        errorValidation = true;
                                    }
                                    else if (dr[ResourceFile.MainCategoryNameLabel].ToString().Length > 50)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Main category name length should not be more 50 characters ";
                                        errorValidation = true;
                                    }
                                    else if (dr[ResourceFile.MainCategoryNameLabel].ToString() != "" && !regexSpecialCharacters4.IsMatch(dr[ResourceFile.MainCategoryNameLabel].ToString().Trim()))
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Main Category Name accepts letters (a-z), numbers (0-9). Special Characters Double Quotes and Back Slash Are Not Allowed ";
                                        errorValidation = true;
                                    }
                                }
                                else if (dc.ColumnName == ResourceFile.MainCategoryCodeLabel)
                                {
                                    if (dr[ResourceFile.MainCategoryCodeLabel].ToString() == "")
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Main category code cannot be empty ";
                                        errorValidation = true;
                                    }
                                    else if (dr[ResourceFile.MainCategoryCodeLabel].ToString().Length > 20)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Main category code length should not be more than 20 characters ";
                                        errorValidation = true;
                                    }
                                }
                                else if (dc.ColumnName == ResourceFile.MUOM)
                                {
                                    if (dr[ResourceFile.MUOM].ToString() == "")
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Main category Unit of measure cannot be empty ";
                                        errorValidation = true;
                                    }
                                }
                                else if (dc.ColumnName == ResourceFile.SUOM)
                                {
                                    if (dr[ResourceFile.SUOM].ToString() == "")
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Sub category Unit of measure cannot be empty ";
                                        errorValidation = true;
                                    }
                                }

                                else if (dc.ColumnName == ResourceFile.SubCategoryNameLabel)
                                {
                                    if (!String.IsNullOrEmpty(dr[ResourceFile.SubCategoryNameLabel].ToString()))
                                    {
                                        if (dr[ResourceFile.SubCategoryNameLabel].ToString().Trim().Length > 50)
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Sub category name length should not be more than 50 characters ";
                                            errorValidation = true;
                                        }
                                        else if (dr[ResourceFile.SubCategoryNameLabel].ToString() != "" && !regexSpecialCharacters4.IsMatch(dr["Sub category name"].ToString().Trim()))
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Sub Category Name accepts letters (a-z), numbers (0-9). Special Characters Double Quotes and Back Slash Are Not Allowed ";
                                            errorValidation = true;
                                        }
                                    }
                                    else
                                    {
                                        if (dr[ResourceFile.SubCategoryNameLabel].ToString() == "")
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Sub category name cannot be empty ";
                                            errorValidation = true;
                                        }
                                    }
                                }
                                else if (dc.ColumnName == ResourceFile.SubCategoryCodeLabel)
                                {
                                    if (!String.IsNullOrEmpty(dr[ResourceFile.SubCategoryCodeLabel].ToString()))
                                    {
                                        if (dr[ResourceFile.SubCategoryCodeLabel].ToString().Trim().Length > 20)
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Sub category code length should not be more than 20 characters ";
                                            errorValidation = true;
                                        }
                                        if (dr[ResourceFile.MainCategoryCodeLabel].ToString().ToLower().Trim() == dr[ResourceFile.SubCategoryCodeLabel].ToString().ToLower().Trim())
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Main category code & Sub category code cannot be same ";
                                            errorValidation = true;
                                        }
                                    }
                                    else
                                    {
                                        if (dr[ResourceFile.SubCategoryCodeLabel].ToString() == "")
                                        {
                                            nMsgCnt++;
                                            sErrorMsg = sErrorMsg + nMsgCnt + ". Sub category code cannot be empty ";
                                            errorValidation = true;
                                        }
                                    }
                                }
                                else if (dc.ColumnName == ResourceFile.MainCategoryDescription)
                                {
                                    if (dr[ResourceFile.MainCategoryDescription].ToString().Trim().Length > 500)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Main category description description length should not be more than 500 characters ";
                                        errorValidation = true;
                                    }
                                }
                                else if (dc.ColumnName == ResourceFile.SubCategoryDescription)
                                {
                                    if (dr[ResourceFile.SubCategoryDescription].ToString().Trim().Length > 500)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Main category description description length should not be more than 500 characters ";
                                        errorValidation = true;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {

                                return colserror;
                            }
                        }


                    }
                    if (sErrorMsg != "")
                    {
                        colserror.Rows.Add(dr.ItemArray);
                        colserror.Rows[colserror.Rows.Count - 1]["Error Message"] = sErrorMsg;
                        continue;
                    }
                }


                return colserror;
            }
            finally
            {
                colserror?.Dispose();
            }
        }

        #endregion

        #region Item Master API
        [HttpPost]
        public HttpResponseMessage AddItemMasterDetails(ItemMasterAPIViewModel submitData)
        {
            var json = JsonConvert.SerializeObject(submitData.ItemMasterDetails);
            DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);
            long companyId, userId;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());
            bool status = true;
            string message = "";

            if (excelDt == null)
            {
                status = false;
                message = "Json ItemMasterDetails not found for import.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
            var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();

            List<ItemCategoryViewModel> subcategoriesList = _consumable.GetAllItemCategories(companyId).Where(x => x.ParentId != null).ToList();

            List<NumericLookupItem> unitmeasuresList = _consumable.GetAllUnitMeasures(companyId).Select(
                                                         x => new NumericLookupItem
                                                         {
                                                             Text = x.Name,
                                                             Value = x.UnitMeasureId
                                                         }).ToList();

            try
            {
                if (ValidateExcelColumns(colsList, GetItemMasterColumnsList()))
                {
                    if (excelDt.Rows.Count != 0)
                    {
                        DataTable dttable = ValidateImportItemMaster(excelDt, companyId);
                        if (dttable.Rows.Count == 0)
                        {
                            foreach (DataRow dr in excelDt.Rows)
                            {
                                var unitId = unitmeasuresList.Where(x => x.Text == dr[Unit_Measure].ToString()).FirstOrDefault().Value;
                                var catId = subcategoriesList.Where(x => x.ItemCategoryName == dr[Item_Level2_Category].ToString()).FirstOrDefault().ItemCategoryId;

                                ItemMasterViewModel itemObj = _consumable.GetItemMasterName(companyId, dr[Item_Name].ToString().Trim());
                                //var v = _consumable.GetItemMasterName(companyId, dr[Item_Name].ToString().Trim());
                                if (itemObj == null)
                                {
                                    var cim = new ItemMasterViewModel()
                                    {
                                        CreatedBy = userId,
                                        CreatedDate = DateTime.Now,
                                        IsActive = true,
                                        Name = dr[Item_Name].ToString().Trim(),
                                        Code = dr[Item_Code].ToString().Trim(),
                                        Description = dr[Item_Description].ToString().Trim(),
                                        UnitMeasureId = unitId,
                                        ReorderLevel = Convert.ToInt32(dr[Item_Reorder_Level].ToString().Trim()),
                                        CompanyId = companyId,
                                        ItemCategoryId = catId,
                                        ManufactureDate = DateTime.Now,
                                        ExpiryDate = DateTime.Now,
                                        BatchNo = null,
                                        UnitPrice = Convert.ToDecimal(dr[ResourceFile.UnitPriceLabel].ToString().Trim())
                                    };
                                    var unitmeasureId = _consumable.AddItemMaster(cim);
                                }
                                else if (itemObj != null)
                                {
                                    if (itemObj.ItemMasterID > 0)
                                    {
                                        var cim = new ItemMasterViewModel()
                                        {
                                            ItemMasterID = itemObj.ItemMasterID,
                                            ModifiedBy = userId,
                                            ModifiedDate = DateTime.Now,
                                            IsActive = true,
                                            Name = dr[Item_Name].ToString().Trim(),
                                            Code = dr[Item_Code].ToString().Trim(),
                                            Description = dr[Item_Description].ToString().Trim(),
                                            UnitMeasureId = unitId,
                                            ReorderLevel = Convert.ToInt32(dr[Item_Reorder_Level].ToString().Trim()),
                                            CompanyId = companyId,
                                            ItemCategoryId = catId,
                                            ManufactureDate = DateTime.Now,
                                            ExpiryDate = DateTime.Now,
                                            BatchNo = null,
                                            UnitPrice = Convert.ToDecimal(dr[ResourceFile.UnitPriceLabel].ToString().Trim())
                                        };
                                        var unitmeasureId = _consumable.EditItemMaster(cim);
                                    }
                                }
                            }
                            status = true;
                            message = "Item Master added successfully.";
                        }
                        else
                        {
                            string JSONresult;
                            status = false;
                            JSONresult = JsonConvert.SerializeObject(dttable, Formatting.Indented);
                            var response = Request.CreateResponse(HttpStatusCode.OK);
                            response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                            return response;
                        }
                    }
                    else
                    {
                        status = false;
                        message = "Json ItemMasterDetails not found for import";
                    }
                }
                else
                {
                    status = false;
                    message = "Json format does not match with defined Item Master.";
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = ex.Message.ToString();
                message = !string.IsNullOrEmpty(message) ? message : ex.ToString();
                return Request.CreateResponse(HttpStatusCode.OK, new
                {
                    status,
                    message
                });
            }
            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
        }

        private DataTable ValidateImportItemMaster(DataTable datatable, long companyId)
        {
            DataTable dterror = new DataTable();
            var colserror = new DataTable();
            foreach (DataColumn dc in datatable.Columns)
                colserror.Columns.Add(dc.ColumnName);
            colserror.Columns.Add("Error Message");
            string sErrorMsg = "";
            int nMsgCnt = 0;
            var itemmasters = _consumable.GetAllItemMasters(companyId);
            decimal unitPrice = 0;

            List<NumericLookupItem> maincategoriesList = _consumable.GetAllItemCategories(companyId).Where(x => x.ParentId == null).Select(
                                                         x => new NumericLookupItem
                                                         {
                                                             Text = x.ItemCategoryName,
                                                             Value = x.ItemCategoryId
                                                         }).ToList();
            List<ItemCategoryViewModel> subcategoriesList = _consumable.GetAllItemCategories(companyId).Where(x => x.ParentId != null).ToList();

            List<NumericLookupItem> unitmeasuresList = _consumable.GetAllUnitMeasures(companyId).Select(
                                                         x => new NumericLookupItem
                                                         {
                                                             Text = x.Name,
                                                             Value = x.UnitMeasureId
                                                         }).ToList();
            foreach (DataRow dr in datatable.Rows)
            {
                sErrorMsg = "";
                nMsgCnt = 0;

                ItemMasterViewModel itemObj = _consumable.GetItemMasterName(companyId, dr[Item_Name].ToString().Trim());
                var itemmasterobj = new ItemMasterViewModel();

                //if (!string.IsNullOrEmpty(dr[Item_Name].ToString().ToLower().Trim()) && datatable.AsEnumerable().Where(x => x.Field<string>(Item_Name).ToLower().Trim() == dr[Item_Name].ToString().ToLower().Trim()).Count() > 1)
                //{
                //    nMsgCnt++;
                //    sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected with the same " + Item_Name + " in the Json ";
                //}
                //else if (!string.IsNullOrEmpty(dr[Item_Code].ToString().ToLower().Trim()) && datatable.AsEnumerable().Where(x => x.Field<string>(Item_Code).ToLower().Trim() == dr[Item_Code].ToString().ToLower().Trim()).Count() > 1)
                //{
                //    nMsgCnt++;
                //    sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected with the same " + Item_Code + " in the Json ";
                //}
                //else
                //{

                if (itemObj != null)
                {
                    itemmasterobj = _consumable.GetItemMaster(itemObj.ItemMasterID, companyId);

                    if (_consumable.CheckItemMasters(itemObj.ItemMasterID, companyId) && (itemObj.ItemCategoryId != itemmasterobj.ItemCategoryId))
                    {
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". You cannot modify item category because transactions are already done ";

                    }

                    else if (_consumable.CheckItemMasters(itemObj.ItemMasterID, companyId) && (itemObj.UnitMeasureId != itemmasterobj.UnitMeasureId))
                    {
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". You cannot modify unit measure because transactions are already done ";

                    }
                }
                foreach (DataColumn dc in colserror.Columns)
                {
                    //Reorder start
                    if (dc.ColumnName == Item_Reorder_Level)
                    {
                        if (Convert.ToInt32(dr[Item_Reorder_Level].ToString().Trim()) < 1)
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Reorder level should be greater than or equal to 1 ";
                        }
                        else if (dr[Item_Reorder_Level].ToString().Trim() == "")
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Reorder level is mandatory ";
                        }
                        else if (dr[Item_Reorder_Level].ToString().Trim() != "")
                        {
                            long reorder = 0;
                            if (!Int64.TryParse(dr[Item_Reorder_Level].ToString(), out reorder))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Reorder level will accept only numeric ";
                            }
                        }
                    }
                    //Reorder End
                    //name   start                    
                    else if (dc.ColumnName == Item_Name)
                    {
                        if (dr[Item_Name].ToString().Trim() == "")
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Item name is should not be empty ";
                        }
                        //else if (itemmasters.Where(x => x.Name.Trim().ToLower() == dr[Item_Name].ToString().Trim().ToLower() && x.CompanyId == companyId).Count() > 0)
                        //{
                        //    nMsgCnt++;
                        //    sErrorMsg = sErrorMsg + nMsgCnt + ". Item master name already exists ";
                        //}
                        else if ((dr[Item_Name].ToString().Length > 100))
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Item Name length should not be more than 100 characters ";
                        }

                        if (itemObj == null)
                        {
                            if (itemmasters.Where(x => x.Name.Trim().ToLower() == dr[Item_Name].ToString().Trim().ToLower() && x.CompanyId == companyId).Count() > 0)
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Item master name already exists ";
                            }
                        }
                    }
                    //Name end
                    //Code Start
                    else if (dc.ColumnName == Item_Code)
                    {
                        if (dr[Item_Code].ToString().Trim() == "")
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Code should not be empty ";
                        }
                        else if (dr[Item_Code].ToString().Trim() != "" && !regexSpecialCharacters2.IsMatch(dr[Item_Code].ToString().Trim()))
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Code accepts letters (a-z), numbers (0-9), and charecters (-_/\\&) ";
                        }
                        else if ((dr[Item_Code].ToString().Length > 30))
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Item code length should not be more than 30 characters ";
                        }

                        if (itemObj == null)
                        {
                            if (itemmasters.Where(x => x.Code.Trim().ToLower() == dr[Item_Code].ToString().Trim().ToLower() && x.CompanyId == companyId).Count() > 0)
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Code already exists";
                            }
                        }
                    }
                    //Code End
                    //Description Start
                    else if (dc.ColumnName == Item_Description)
                    {
                        if (dr[Item_Description].ToString().Trim() != "" && dr[Item_Description].ToString().Length > 500)
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Item Description length should not be more than 500 characters ";
                        }
                    }
                    //Description End

                    //Level1/Level2 Start
                    else if (dc.ColumnName == Item_Level1_Category || dc.ColumnName == Item_Level2_Category)
                    {
                        var level1CatId = maincategoriesList.Where(x => x.Text.ToLower().Trim() == dr[Item_Level1_Category].ToString().ToLower().Trim()).FirstOrDefault();
                        if (dc.ColumnName == Item_Level1_Category)
                        {
                            if (dr[Item_Level1_Category].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Level1 Category is mandatory ";
                            }
                            if (level1CatId == null)
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Level1 Category does not exist ";
                            }
                        }
                        if (dc.ColumnName == Item_Level2_Category)
                        {
                            if (dr[Item_Level2_Category].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Level2 Category is mandatory ";
                            }

                            var level2CatId = subcategoriesList.Where(x => x.ItemCategoryName.ToLower().Trim() == dr[Item_Level2_Category].ToString().ToLower().Trim()).FirstOrDefault();
                            if (level2CatId == null)
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Level2 Category does not exist ";
                            }
                            else if (level2CatId.ParentId != level1CatId.Value)
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Level2 Category does not exist for Level1 category ";
                            }
                        }
                    }
                    //Level1/Level2 End
                    //Unit_Measure Start
                    else if (dc.ColumnName == Unit_Measure)
                    {
                        if (dr[Unit_Measure].ToString().Trim() == "")
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Unit of measure is mandatory ";
                        }
                        var unitId = unitmeasuresList.Where(x => x.Text == dr[Unit_Measure].ToString()).FirstOrDefault();
                        if (unitId == null)
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Unit of measure does not exists ";
                        }
                    }
                    //unit measure end
                    //unit price start
                    else if (dc.ColumnName == ResourceFile.UnitPriceLabel)
                    {
                        if (dr[ResourceFile.UnitPriceLabel].ToString().Trim() != "")
                        {
                            unitPrice = 0;
                            if (!Decimal.TryParse(dr[ResourceFile.UnitPriceLabel].ToString(), out unitPrice))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ResourceFile.UnitPriceErrorMessage; ;
                            }
                        }
                    }
                    //unit price end
                }

                if (sErrorMsg != "")
                {
                    colserror.Rows.Add(dr.ItemArray);
                    colserror.Rows[colserror.Rows.Count - 1]["Error Message"] = sErrorMsg;
                    continue;
                }
            }
            return colserror;
        }

        public List<ImportViewModel> GetItemMasterColumnsList()
        {
            var importViewModel = new List<ImportViewModel>
                {
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = Item_Name,DisplayName = Item_Name ,ColumnDescription = Item_Name,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = Item_Code,DisplayName = Item_Code ,ColumnDescription = Item_Code,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = Item_Description,DisplayName = Item_Description ,ColumnDescription = Item_Description,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = Item_Level1_Category,DisplayName = Item_Level1_Category ,ColumnDescription = Item_Level1_Category,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = Item_Level2_Category,DisplayName =Item_Level2_Category ,ColumnDescription = Item_Level2_Category,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = Unit_Measure,DisplayName = Unit_Measure ,ColumnDescription = Unit_Measure,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = Item_Reorder_Level,DisplayName = Item_Reorder_Level ,ColumnDescription = Item_Reorder_Level,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.UnitPriceLabel,DisplayName = ResourceFile.UnitPriceLabel ,ColumnDescription = ResourceFile.UnitPriceLabel,Attribute = "notreq",DropDown = false},
                };
            return importViewModel;
        }
        #endregion
        #endregion

        #region Update API Methods

        #region Company Hierarchy
        [HttpPost]
        public HttpResponseMessage UpdateCompanyHierarchyDetails(long BranchId, BranchPostData submitData)
        {
            var json = JsonConvert.SerializeObject(submitData.CompanyHierarchyDetails);
            DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);

            long companyId, userId;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());

            bool status = false;
            string message = "";

            if (excelDt == null)
            {
                status = false;
                message = "Json CompanyHierarchyDetails not found for update.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }

            var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();
            var parametersObj = new GenericLookUp();
            parametersObj.Field1 = Convert.ToInt32(ADQFAMS.Common.Enums.HierarchyMasterTypes.Branch);
            parametersObj.Field2 = companyId;
            var databaseColumns = _masterApi.GetHierarchyDynamicData(parametersObj);
            var States = _baseInterface.ICompany.GetStates(companyId);
            var recordDetails = _baseInterface.ICompany.GetAllBrancheDetails(companyId);
            if (recordDetails.Where(x => x.CompanyId == companyId && x.BranchId == BranchId).Count() == 0)
            {
                status = false;
                message = "Branch Details not found for BranchId (" + BranchId + ")";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
            if (databaseColumns.HierarchyDynamicList != null && databaseColumns.HierarchyDynamicList.Count() > 0)
            {
                if (CommonHelper.ValidateExcelColumns(colsList, GetBranchDynamicColumnsList(companyId)))
                {
                    try
                    {
                        List<TripleText> currentSessionInsertedDataList = new List<TripleText>();
                        TripleText currentSessionInsertedData = new TripleText();
                        int recordCount = 0;
                        long parentId = 0;
                        long currentRowId = 0;
                        var oBranchObj = new BranchViewModel();
                        if (excelDt.Rows.Count != 0)
                        {
                            DataTable dttable = ValidateDynamicBranchUpdateData(excelDt, companyId, databaseColumns, BranchId);
                            if (dttable.Rows.Count == 0)
                            {
                                foreach (DataRow dr in excelDt.Rows)
                                {
                                    parentId = 0;
                                    currentRowId = 0;
                                    long levelid = recordDetails.Where(x => x.CompanyId == companyId && x.BranchId == BranchId).Select(x => x.TypeID).FirstOrDefault().Value;

                                    DataTable dttable1 = ValidateDynamicBranchUpdateLastLevelData(excelDt, companyId, databaseColumns, levelid);
                                    if (dttable1.Rows.Count == 0)
                                    {

                                        foreach (var data in databaseColumns.HierarchyDynamicList.Where(x => x.LevelType == levelid))
                                        {
                                            currentSessionInsertedData = new TripleText();
                                            oBranchObj = new BranchViewModel();
                                            recordCount = recordDetails.Where(x => x.TypeID == data.LevelType && x.CompanyId == companyId && x.BranchId == BranchId).Count();
                                            if (recordCount >= 1) //This may be useful for updation of branch Name
                                            {
                                                currentRowId = Convert.ToInt64(BranchId);
                                                oBranchObj = new BranchViewModel
                                                {
                                                    ModifiedBy = userId,
                                                    ModifiedDate = DateTime.Now,
                                                    Name = dr["BranchName"].ToString().Trim(),
                                                    Code = dr["BranchCode"].ToString().Trim(),
                                                    CompanyId = companyId,
                                                    BranchId = currentRowId
                                                };
                                                _baseInterface.ICompany.EditBranch(oBranchObj);
                                                if (databaseColumns.LeafLevelTypeId == data.LevelType && dr["BranchCode"].ToString().Trim() != "")
                                                {
                                                    oBranchObj.TinNo = dr[ResourceFile.TinOrGst].ToString();
                                                    oBranchObj.PanNo = dr[ResourceFile.PanLabel].ToString();
                                                    oBranchObj.Address = dr[ResourceFile.AddressLabel].ToString();
                                                    oBranchObj.City = dr[ResourceFile.City].ToString();
                                                    oBranchObj.State = dr[ResourceFile.State].ToString();
                                                    oBranchObj.ZipCode = dr[ResourceFile.ZipCode].ToString();
                                                    oBranchObj.MobileNo = dr[ResourceFile.MobileNo].ToString();
                                                    oBranchObj.BranchId = currentRowId;
                                                    oBranchObj.CreatedBy = userId;
                                                    oBranchObj.StateId = (dr[ResourceFile.State].ToString() != "") ? (States.Where(x => x.State.ToString().ToLower().Trim() == dr[ResourceFile.State].ToString().ToLower().Trim()).FirstOrDefault().Id) : 0;
                                                    oBranchObj.EmailAddress = dr[ResourceFile.EmailIdLabel].ToString();
                                                    _baseInterface.ICompany.UpdateBranchAddressDetails(oBranchObj);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        status = false;
                                        message = "Json CompanyHierarchyDetails not found for import.";
                                        return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                                    }
                                }
                                status = true;
                                message = "Company Hierarchy updated successfully.";
                                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                            }
                            else
                            {
                                string JSONresult;
                                status = false;
                                JSONresult = JsonConvert.SerializeObject(dttable, Formatting.Indented);
                                var response = Request.CreateResponse(HttpStatusCode.OK);
                                response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                                return response;
                            }
                        }
                        else
                        {
                            status = false;
                            message = "Json CompanyHierarchyDetails not found for import.";
                            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                        }
                    }
                    catch (Exception ex)
                    {
                        status = false; message = ex.Message.ToString();
                        message = !string.IsNullOrEmpty(message) ? message : ex.ToString();
                        return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                    }
                }
                else
                {
                    status = false;
                    message = "Json format does not match with defined Company Hierarchy. ";
                    return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                }
            }
            else
            {
                status = false;
                message = "Company Hierarchy is not defined.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
        }

        public List<ImportModel> GetBranchDynamicColumnsList(long companyId)//,long LevelId)
        {
            var parametersObj = new GenericLookUp();
            parametersObj.Field1 = Convert.ToInt16(ADQFAMS.Common.Enums.HierarchyMasterTypes.Branch);
            parametersObj.Field2 = companyId;
            HierarchyModel branchDetails = _masterApi.GetHierarchyDynamicData(parametersObj);
            var branchColumnsList = new List<ImportModel>();
            if (!string.IsNullOrEmpty(branchDetails.LeafLevelName))
            {
                //foreach (var data in branchDetails.HierarchyDynamicList.Where(x=> x.LevelType == LevelId))
                //{
                //    branchColumnsList.Add(new ImportModel { Checked = true, Required = data.LevelType == 100 ? true : false, ColumnName = data.LevelName + ResourceFile.CostCenterName, DisplayName = data.LevelName + ResourceFile.CostCenterName, ColumnDescription = data.LevelName + ResourceFile.CostCenterName, Attribute = "required", DropDown = false });
                //    branchColumnsList.Add(new ImportModel { Checked = true, Required = data.LevelType == 100 ? true : false, ColumnName = data.LevelName + ResourceFile.Code, DisplayName = data.LevelName + ResourceFile.Code, ColumnDescription = data.LevelName + ResourceFile.Code, Attribute = "required", DropDown = false });
                //}
                // branchColumnsList.Add(new ImportModel { Checked = true, Required = true, ColumnName = "BranchId", DisplayName = "BranchId", ColumnDescription = "BranchId", Attribute = "required", DropDown = false });
                branchColumnsList.Add(new ImportModel { Checked = true, Required = true, ColumnName = "BranchName", DisplayName = "BranchName", ColumnDescription = "BranchName", Attribute = "required", DropDown = false });
                branchColumnsList.Add(new ImportModel { Checked = true, Required = true, ColumnName = "BranchCode", DisplayName = "BranchCode", ColumnDescription = "BranchCode", Attribute = "required", DropDown = false });
                branchColumnsList.Add(new ImportModel { Checked = false, Required = false, ColumnName = ResourceFile.TinOrGst, DisplayName = ResourceFile.TinOrGst, ColumnDescription = ResourceFile.TinOrGst, Attribute = "notreq", DropDown = false });
                branchColumnsList.Add(new ImportModel { Checked = false, Required = false, ColumnName = ResourceFile.PanLabel, DisplayName = ResourceFile.PanLabel, ColumnDescription = ResourceFile.PanLabel, Attribute = "notreq", DropDown = false });
                branchColumnsList.Add(new ImportModel { Checked = false, Required = false, ColumnName = ResourceFile.AddressLabel, DisplayName = ResourceFile.AddressLabel, ColumnDescription = ResourceFile.AddressLabel, Attribute = "notreq", DropDown = false });
                branchColumnsList.Add(new ImportModel { Checked = false, Required = false, ColumnName = ResourceFile.City, DisplayName = ResourceFile.City, ColumnDescription = ResourceFile.City, Attribute = "notreq", DropDown = false });
                branchColumnsList.Add(new ImportModel { Checked = false, Required = (branchDetails.HierarchyDynamicList != null && branchDetails.HierarchyDynamicList.Count == 1 ? true : false), ColumnName = ResourceFile.State, DisplayName = ResourceFile.State, ColumnDescription = ResourceFile.State, Attribute = "notreq", DropDown = false });
                branchColumnsList.Add(new ImportModel { Checked = false, Required = false, ColumnName = ResourceFile.ZipCode, DisplayName = ResourceFile.ZipCode, ColumnDescription = ResourceFile.ZipCode, Attribute = "notreq", DropDown = false });
                branchColumnsList.Add(new ImportModel { Checked = false, Required = false, ColumnName = ResourceFile.MobileNo, DisplayName = ResourceFile.MobileNo, ColumnDescription = ResourceFile.MobileNo, Attribute = "notreq", DropDown = false });
                //branchColumnsList.Add(new ImportModel { Checked = false, Required = false, ColumnName = ResourceFile.PhoneNo, DisplayName = ResourceFile.PhoneNo, ColumnDescription = ResourceFile.PhoneNo, Attribute = "notreq", DropDown = false });
                branchColumnsList.Add(new ImportModel { Checked = false, Required = false, ColumnName = ResourceFile.EmailIdLabel, DisplayName = ResourceFile.EmailIdLabel, ColumnDescription = ResourceFile.EmailIdLabel, Attribute = "notreq", DropDown = false });

            }
            return branchColumnsList;
        }

        public DataTable ValidateDynamicBranchUpdateLastLevelData(DataTable dt, long companyId, HierarchyModel databaseColumns,long levelid)
        {
            DataTable dtError = new DataTable();

            foreach (DataColumn dc in dt.Columns)
                dtError.Columns.Add(dc.ColumnName);
            dtError.Columns.Add("ErrorMessage");
            var columnHeaderName = "";
            var columnHeaderCode = "";
            var recordDetails = _baseInterface.ICompany.GetAllBrancheDetails(companyId);
            DataColumnCollection branchColumns = dt.Columns;
            var retval = 0;
            int recordCount = 0;

            var States = _baseInterface.ICompany.GetStates(companyId);

            var branchObj = new List<BranchViewModel>();
            string sErrorMsg = "";
            int nMsgCnt = 0;
            if (levelid == 104)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    try
                    {
                        sErrorMsg = "";
                        nMsgCnt = 0;

                        if (branchColumns.Contains(ResourceFile.TinOrGst))
                        {
                            if (dr[ResourceFile.TinOrGst].ToString() != "" && dr[ResourceFile.TinOrGst].ToString().Length > 20)
                            {
                                retval = -1;
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". " + ResourceFile.TinOrGst + " length should not be more than 50 characters ";
                            }
                        }

                        if (branchColumns.Contains("State") && dr["State"].ToString().Trim() == "")
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". State is mandatory for last level of company hierarchy ";
                        }
                        else if (branchColumns.Contains("State") && dr["State"].ToString().Trim() != "")
                        {
                            if (States.Where(x => x.State.ToString().ToLower().Trim() == dr["State"].ToString().ToLower().Trim()).Count() <= 0)
                            {
                                retval = -1;
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Invalid State name ";
                            }
                        }
                        if (branchColumns.Contains(ResourceFile.CIN))
                        {
                            if (dr[ResourceFile.CIN].ToString() != "" && dr[ResourceFile.CIN].ToString().Length > 20)
                            {
                                retval = -1;
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". CIN length should not be more than 50 characters ";
                            }
                        }
                        if (branchColumns.Contains(ResourceFile.ServiceTaxNo))
                        {
                            if (dr[ResourceFile.ServiceTaxNo].ToString() != "" && dr[ResourceFile.ServiceTaxNo].ToString().Length > 20)
                            {
                                retval = -1;
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Service Tax No length should not be more than 50 characters ";
                            }
                        }
                        if (branchColumns.Contains(ResourceFile.AddressLabel))
                        {
                            if (dr[ResourceFile.AddressLabel].ToString() != "" && dr[ResourceFile.AddressLabel].ToString().Length > 200)
                            {
                                retval = -1;
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Address length should not be more than 200 characters ";
                            }
                        }
                        if (branchColumns.Contains(ResourceFile.City))
                        {
                            if (dr[ResourceFile.City].ToString() != "" && dr[ResourceFile.City].ToString().Length > 20)
                            {
                                retval = -1;
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". City length should not be more than 50 characters ";
                            }
                        }
                        if (branchColumns.Contains(ResourceFile.ZipCode))
                        {
                            if (dr[ResourceFile.ZipCode].ToString() != "")
                            {
                                var zip = Regex.IsMatch(dr[ResourceFile.ZipCode].ToString().Trim(), @"^([0-9]{6})$");
                                if (zip == false)
                                {
                                    retval = -1;
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". ZIP Code is not valid ";
                                }
                            }
                        }
                        if (branchColumns.Contains(ResourceFile.EmailIdLabel))
                        {
                            if (dr[ResourceFile.EmailIdLabel].ToString().Trim() != "")
                            {
                                var value = CommonHelper.IsValidEmailId(dr[ResourceFile.EmailIdLabel].ToString().Trim());
                                if (value == false)
                                {
                                    retval = -1;
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Email Id is not valid ";
                                }
                            }
                        }

                        if (branchColumns.Contains(ResourceFile.MobileNo))
                        {
                            if (dr[ResourceFile.MobileNo].ToString() != "" && dr[ResourceFile.MobileNo].ToString().Length > 10 || dr[ResourceFile.MobileNo].ToString() != "" && dr[ResourceFile.MobileNo].ToString().Length < 10)
                            {
                                retval = -1;
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Mobile Number is not valid ";
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                    if (sErrorMsg != "")
                    {
                        dtError.Rows.Add(dr.ItemArray);
                        dtError.Rows[dtError.Rows.Count - 1]["ErrorMessage"] = sErrorMsg;
                        continue;
                    }
                }
            }
            return dtError;
        }

        public DataTable ValidateDynamicBranchUpdateData(DataTable dt, long companyId, HierarchyModel databaseColumns,long branchId)
        {
            DataTable dtError = new DataTable();

            foreach (DataColumn dc in dt.Columns)
                dtError.Columns.Add(dc.ColumnName);
            dtError.Columns.Add("ErrorMessage");
            var columnHeaderName = "";
            var columnHeaderCode = "";
            var recordDetails = _baseInterface.ICompany.GetAllBrancheDetails(companyId);
            DataColumnCollection branchColumns = dt.Columns;
            var retval = 0;
            int recordCount = 0;

            var branchObj = new List<BranchViewModel>();
            string sErrorMsg = "";
            int nMsgCnt = 0;

            foreach (DataRow dr in dt.Rows)
            {
                try
                {
                    sErrorMsg = "";
                    nMsgCnt = 0;

                    columnHeaderCode = "BranchCode"; columnHeaderName = "BranchName";
                    if (branchColumns.Contains(columnHeaderName) && dr[columnHeaderName].ToString().Trim() == "")
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderName + " is mandatory ";

                    }
                    if (branchColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString().Trim() == "")
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " is mandatory ";

                    }
                    if (branchColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString() != "" && dr[columnHeaderCode].ToString().Length > 20)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " length should not exceed 20 characters. ";
                    }
                    if (branchColumns.Contains(columnHeaderName) && dr[columnHeaderName].ToString() != "" && !regexSpecialCharacters4.IsMatch(dr[columnHeaderName].ToString().Trim()))
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderName + " accepts letters (a-z), numbers (0-9). Special Characters Double Quotes and Back Slash Are Not Allowed";
                    }

                    if (branchColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString() != "" && !regexSpecialCharacters.IsMatch(dr[columnHeaderCode].ToString().Trim()))
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " " + ResourceFile.AllowSpecialCharectersForExcel + " ";
                    }

                    // check if branch name already exists
                    //if (branchColumns.Contains(columnHeaderName))
                    //{
                    //    if (recordDetails.Where(x => x.Name.ToLower() == dr["BranchName"].ToString().Trim().ToLower()).Count() > 0)
                    //    {
                    //        nMsgCnt++;
                    //        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderName + " already exists. ";
                    //    }
                    //}
                    //if (branchColumns.Contains(columnHeaderCode))
                    //{
                    //    if (recordDetails.Where(x => x.Code.ToLower() == dr["BranchCode"].ToString().Trim().ToLower()).Count() > 0)
                    //    {
                    //        nMsgCnt++;
                    //        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " already exists. ";
                    //    }
                    //}

                    if (recordDetails.Where(x => x.BranchId != branchId && (x.Name.ToString().ToLower().Trim() == dr[columnHeaderName].ToString().ToLower().Trim() || x.Code.ToString().ToLower().Trim() == dr[columnHeaderCode].ToString().ToLower().Trim())).Count() > 0)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + "Record already exists with the same Branch Name/Branch Code.";
                    }
                }
                catch (Exception ex)
                {

                }
                if (sErrorMsg != "")
                {
                    dtError.Rows.Add(dr.ItemArray);
                    dtError.Rows[dtError.Rows.Count - 1]["ErrorMessage"] = sErrorMsg;
                    continue;
                }
            }
            return dtError;
        }
        #endregion

        #region DepartmentDetails API
        [HttpPost]
        public HttpResponseMessage UpdateDepartmentDetails(long deptid, DepartmentPostData submitData)
        {
            var json = JsonConvert.SerializeObject(submitData.DepartmentDetails);
            DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);

            long companyId, userId;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());

            if (excelDt == null)
            {
                bool status = false;
                string message = "Json DepartmentDetails not found for update.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }

            var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();
            var parametersObj = new GenericLookUp();
            parametersObj.Field1 = Convert.ToInt16(ADQFAMS.Common.Enums.HierarchyMasterTypes.Department);
            parametersObj.Field2 = companyId;
            var databaseColumns = _masterApi.GetHierarchyDynamicData(parametersObj);

            if (databaseColumns.HierarchyDynamicList != null && databaseColumns.HierarchyDynamicList.Count() > 0)
            {
                if (ValidateExcelColumns(colsList, GetDepartmentColumnsList()))
                {
                    try
                    {
                        List<TripleText> currentSessionInsertedDataList = new List<TripleText>();
                        TripleText currentSessionInsertedData = new TripleText();
                        var recordDetails = _baseInterface.IDepartmentService.GetDepartments(companyId);
                        var recfound=recordDetails.Where(x => x.DepartmentID == deptid).ToList();
                        if(recfound.Count()==0)
                        {
                            bool status = false;
                            string message = "Department Details not found for DepartmentId : " + deptid;
                            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                        }
                        int recordCount = 0;
                        long parentId = 0;
                        long currentRowId = 0;
                        var oDepartmentObj = new DepartmentViewModel();
                        if (excelDt.Rows.Count != 0)
                        {
                            DataTable dttable = ValidateDynamicDepartmentImportDataUpdate(excelDt, companyId, databaseColumns, deptid);
                            if (dttable.Rows.Count == 0)
                            {
                                bool status = false;
                                string message = "";
                                foreach (DataRow dr in excelDt.Rows)
                                {
                                    parentId = 0;
                                    currentRowId = 0;
                                    foreach (var data in databaseColumns.HierarchyDynamicList)
                                    {
                                        currentSessionInsertedData = new TripleText();
                                        oDepartmentObj = new DepartmentViewModel();
                                        //recordCount = recordDetails.Where(x => x.DepartmentTypeId == data.LevelType && x.CompanyId == companyId && x.Code.ToString().ToLower().Trim() == dr[data.LevelName + "Code"].ToString().ToLower().Trim()).Count();
                                        recordCount = recordDetails.Where(x => x.DepartmentID == deptid && x.CompanyId == companyId && x.IsActive == true).Count();
                                        
                                        if (recordCount >= 1) //This may be useful for updation of Location Name
                                        {
                                            currentRowId = deptid;
                                            oDepartmentObj = new DepartmentViewModel
                                            {
                                                ModifiedBy = userId,
                                                ModifiedDate = DateTime.Now,
                                                Name = dr["DepartmentName"].ToString().Trim(),
                                                Code = dr["DepartmentCode"].ToString().Trim(),
                                                CompanyId = companyId,
                                                DepartmentID = currentRowId
                                            };
                                            _baseInterface.IDepartmentService.EditDepartment(oDepartmentObj);
                                        }
                                    }
                                }
                                status = true;
                                message = "Department updated successfully.";
                                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                            }
                            else
                            {
                                string JSONresult;
                                bool status = false;
                                JSONresult = JsonConvert.SerializeObject(dttable, Formatting.Indented);
                                var response = Request.CreateResponse(HttpStatusCode.OK);
                                response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                                return response;
                            }

                        }
                        else
                        {
                            bool status = false;
                            string message = "Json DepartmentDetails not found for update.";
                            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                        }
                    }
                    catch (Exception ex)
                    {
                        bool status = false;
                        string message = ex.Message.ToString();
                        message = !string.IsNullOrEmpty(message) ? message : ex.ToString();
                        return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                        // return Request.CreateResponse(HttpStatusCode.OK, status, message);
                    }
                }
                else
                {
                    bool status = false;
                    string message = "Json format does not match with defined Department. ";
                    return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                }
            }
            else
            {
                bool status = false;
                string message = "Department is not defined.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
        }

        public List<ImportViewModel> GetDepartmentColumnsList()
        {
            var importViewModel = new List<ImportViewModel>
                {
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = "DepartmentName",DisplayName = "DepartmentName" ,ColumnDescription = "DepartmentName",Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = "DepartmentCode",DisplayName = "DepartmentCode" ,ColumnDescription = "DepartmentCode",Attribute = "required",DropDown = false},
                };
            return importViewModel;
        }

        public DataTable ValidateDynamicDepartmentImportDataUpdate(DataTable dt, long companyId, HierarchyModel databaseColumns,long deptid)
        {
            DataTable dtError = new DataTable();
            foreach (DataColumn dc in dt.Columns)
                dtError.Columns.Add(dc.ColumnName);
            dtError.Columns.Add("ErrorMessage");

            var columnHeaderName = "";
            var columnHeaderCode = "";
            var recordDetails = _baseInterface.IDepartmentService.GetDepartments(companyId);
            DataColumnCollection excelColumns = dt.Columns;
            var retval = 0;
            int recordCount = 0;
            
            var deptObj = new List<DepartmentViewModel>();

            string sErrorMsg = "";
            int nMsgCnt = 0;

            foreach (DataRow dr in dt.Rows)
            {
                sErrorMsg = "";
                nMsgCnt = 0;

                foreach (var data in databaseColumns.HierarchyDynamicList)
                {
                    recordCount = 0;
                    columnHeaderCode = "DepartmentCode"; columnHeaderName = "DepartmentName";

                    //1st level name is mandatory
                    if (excelColumns.Contains(columnHeaderName) && dr[columnHeaderName].ToString().Trim() == "")
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderName + " is mandatory ";
                    }

                    //1st level code is mandatory
                    if (excelColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString().Trim() == "")
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " is mandatory ";
                    }

                    if (excelColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString() != "" && dr[columnHeaderCode].ToString().Length > 20)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " length should not exceed 8 characters. ";
                    }

                    if (excelColumns.Contains(columnHeaderName) && dr[columnHeaderName].ToString() != "" && dr[columnHeaderName].ToString().Length > 20)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderName + " length should not exceed 20 characters. ";
                    }

                    if (excelColumns.Contains(columnHeaderName) && dr[columnHeaderName].ToString() != "" && !regexSpecialCharacters4.IsMatch(dr[columnHeaderName].ToString().Trim()))
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderName + " accepts letters (a-z), numbers (0-9). Special Characters Double Quotes and Back Slash Are Not Allowed ";
                    }

                    if (excelColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString() != "" && !regexSpecialCharacters.IsMatch(dr[columnHeaderCode].ToString().Trim()))
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " " + ResourceFile.AllowSpecialCharectersForExcel + " ";
                    }
                   
                    if (recordDetails.Where(x => x.DepartmentID != deptid && (x.Name.ToString().ToLower().Trim() == dr[columnHeaderName].ToString().ToLower().Trim() || x.Code.ToString().ToLower().Trim() == dr[columnHeaderCode].ToString().ToLower().Trim())).Count() > 0)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + "Record already exists with the same Department Name/Department Code.";
                        break;
                    }
                }

                if (sErrorMsg != "")
                {
                    dtError.Rows.Add(dr.ItemArray);
                    dtError.Rows[dtError.Rows.Count - 1]["ErrorMessage"] = sErrorMsg;
                    continue;
                }
            }
            return dtError;
        }
        #endregion

        #region CostCenterDetails API
        [HttpPost]
        public HttpResponseMessage UpdateCostCenterDetails(long costcentid, CostCenterPostData submitData)
        {
            var json = JsonConvert.SerializeObject(submitData.CostCenterDetails);
            DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);

            long companyId, userId;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());

            if (excelDt == null)
            {
                bool status = false;
                string message = "Json CostCenterDetails not found for import.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }

            var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();

            var parametersObj = new GenericLookUp();
            parametersObj.Field1 = Convert.ToInt16(ADQFAMS.Common.Enums.HierarchyMasterTypes.CostCenter);
            parametersObj.Field2 = companyId;
            var databaseColumns = _masterApi.GetHierarchyDynamicData(parametersObj);

            if (databaseColumns.HierarchyDynamicList != null && databaseColumns.HierarchyDynamicList.Count() > 0)
            {
                //if (CommonHelper.ValidateExcelColumns(colsList, _importApi.GetDynamicColumnsList(companyId, Convert.ToInt16(ADQFAMS.Common.Enums.HierarchyMasterTypes.CostCenter), 0)))
                if (ValidateExcelColumns(colsList, GetCostCenterColumnsList()))
                {
                    try
                    {
                        List<TripleText> currentSessionInsertedDataList = new List<TripleText>();
                        TripleText currentSessionInsertedData = new TripleText();
                        var recordDetails = _baseInterface.ICostCenter.GetCostCenters(companyId);
                        var recfound = recordDetails.Where(x => x.CostCenterID == costcentid).ToList();
                        if (recfound.Count() == 0)
                        {
                            bool status = false;
                            string message = "CostCenter Details not found for CostCenterId : " + costcentid;
                            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                        }
                        int recordCount = 0;
                        long parentId = 0;
                        long currentRowId = 0;
                        var oCostObj = new CostCenterViewModel();
                        var model = new CostCenterViewModel();
                        var columnHeaderCode = ""; var columnHeaderName = "";
                        if (excelDt.Rows.Count != 0)
                        {
                            DataTable dttable = ValidateDynamicCostCenterImportDataUpdate(excelDt, companyId, databaseColumns, costcentid);
                            if (dttable.Rows.Count == 0)
                            {
                                bool status = false;
                                string message = "";
                                foreach (DataRow dr in excelDt.Rows)
                                {
                                    parentId = 0;
                                    currentRowId = 0;
                                    foreach (var data in databaseColumns.HierarchyDynamicList)
                                    {
                                        currentSessionInsertedData = new TripleText();
                                        oCostObj = new CostCenterViewModel();
                                        columnHeaderCode = data.LevelName + "Code";
                                        columnHeaderName = data.LevelName + "Name";

                                        recordCount = recordDetails.Where(x => x.CostCenterID == costcentid && x.CompanyId == companyId).Count();

                                        if (recordCount >= 1) //This may be useful for updation of cost center Name
                                        {
                                            currentRowId = costcentid;
                                            oCostObj = new CostCenterViewModel
                                            {
                                                ModifiedBy = userId,
                                                ModifiedDate = DateTime.Now,
                                                Name = dr["CostCenterName"].ToString().Trim(),
                                                Code = dr["CostCenterCode"].ToString().Trim(),
                                                //Description = dr[data.LevelName + "Name"].ToString().Trim(),
                                                CompanyId = companyId,
                                                CostCenterID = currentRowId
                                            };
                                            _baseInterface.ICostCenter.EditCostCenter(oCostObj);
                                        }
                                    }
                                }
                                status = true;
                                message = "Cost Center updated successfully.";
                                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                            }
                            else
                            {
                                string JSONresult;
                                bool status = false;
                                JSONresult = JsonConvert.SerializeObject(dttable, Formatting.Indented);
                                var response = Request.CreateResponse(HttpStatusCode.OK);
                                response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                                return response;
                            }

                        }
                        else
                        {
                            bool status = false;
                            string message = "Json CostCenterDetails not found for update.";
                            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                        }
                    }
                    catch (Exception ex)
                    {
                        bool status = false;
                        string message = ex.Message.ToString();
                        message = !string.IsNullOrEmpty(message) ? message : ex.ToString();
                        return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                        // return Request.CreateResponse(HttpStatusCode.OK, status, message);
                    }
                }
                else
                {
                    bool status = false;
                    string message = "Json format does not match with defined Cost Center. ";
                    return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                }
            }
            else
            {
                bool status = false;
                string message = "Cost Center is not defined.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
        }

        public List<ImportViewModel> GetCostCenterColumnsList()
        {
            var importViewModel = new List<ImportViewModel>
                {
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = "CostCenterName",DisplayName = "CostCenterName" ,ColumnDescription = "CostCenterName",Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = "CostCenterCode",DisplayName = "CostCenterCode" ,ColumnDescription = "CostCenterCode",Attribute = "required",DropDown = false},
                };
            return importViewModel;
        }

        public DataTable ValidateDynamicCostCenterImportDataUpdate(DataTable dt, long companyId, HierarchyModel databaseColumns,long costcentid)
        {
            DataTable dtError = new DataTable();
            foreach (DataColumn dc in dt.Columns)
                dtError.Columns.Add(dc.ColumnName);
            dtError.Columns.Add("ErrorMessage");

            var columnHeaderName = "";
            var columnHeaderCode = "";
            var recordDetails = _baseInterface.ICostCenter.GetCostCenters(companyId);
            DataColumnCollection excelColumns = dt.Columns;
            var retval = 0;
            int recordCount = 0;
           
            var costObj = new List<CostCenterViewModel>();

            string sErrorMsg = "";
            int nMsgCnt = 0;

            foreach (DataRow dr in dt.Rows)
            {
                sErrorMsg = "";
                nMsgCnt = 0;

                foreach (var data in databaseColumns.HierarchyDynamicList)
                {
                    recordCount = 0;
                    columnHeaderCode = "CostCenterCode"; columnHeaderName = "CostCenterName";

                    //1st level name is mandatory
                    if (excelColumns.Contains(columnHeaderName) && dr[columnHeaderName].ToString().Trim() == "")
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderName + " is mandatory ";
                    }

                    //1st level code is mandatory
                    if (excelColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString().Trim() == "")
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " is mandatory ";
                    }
                    if (excelColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString() != "" && dr[columnHeaderCode].ToString().Length > 20)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " length should not exceed 20 characters. ";
                    }

                    if (excelColumns.Contains(columnHeaderName) && dr[columnHeaderName].ToString() != "" && !regexSpecialCharacters4.IsMatch(dr[columnHeaderName].ToString().Trim()))
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderName + " accepts letters (a-z), numbers (0-9). Special Characters Double Quotes and Back Slash Are Not Allowed";
                    }

                    if (excelColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString() != "" && !regexSpecialCharacters.IsMatch(dr[columnHeaderCode].ToString().Trim()))
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " " + ResourceFile.AllowSpecialCharectersForExcel + " ";
                    }

                    if (recordDetails.Where(x => x.CostCenterID != costcentid && (x.Name.ToString().ToLower().Trim() == dr[columnHeaderName].ToString().ToLower().Trim() || x.Code.ToString().ToLower().Trim() == dr[columnHeaderCode].ToString().ToLower().Trim())).Count() > 0)
                    {
                        retval = -1;
                        nMsgCnt++;
                        sErrorMsg = sErrorMsg + nMsgCnt + ". Record already exists with the same CostCenter Name/CostCenter Code";
                        break;
                    }
                }

                if (sErrorMsg != "")
                {
                    dtError.Rows.Add(dr.ItemArray);
                    dtError.Rows[dtError.Rows.Count - 1]["ErrorMessage"] = sErrorMsg;
                    continue;
                }
            }
            return dtError;
        }

        #endregion

        #region AssetLocationDetails API
        [HttpPost]
        public HttpResponseMessage UpdateAssetLocationDetails(long locationid, LocationPostData submitData)
        {
            var json = JsonConvert.SerializeObject(submitData.AssetLocationDetails);
            DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);

            long companyId, userId;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());

            if (excelDt == null)
            {
                bool status = false;
                string message = "Json AssetLocationDetails not found for update.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }

            var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();

            var parametersObj = new GenericLookUp();
            parametersObj.Field1 = Convert.ToInt16(ADQFAMS.Common.Enums.HierarchyMasterTypes.Location);
            parametersObj.Field2 = companyId;
            var databaseColumns = _masterApi.GetHierarchyDynamicData(parametersObj);
            parametersObj.Field1 = Convert.ToInt32(ADQFAMS.Common.Enums.HierarchyMasterTypes.Branch);
            var orgLeafLevel = _masterApi.GetHierarchyDynamicData(parametersObj);
            if (databaseColumns.HierarchyDynamicList != null && databaseColumns.HierarchyDynamicList.Count() > 0)
            {
                if (ValidateExcelColumns(colsList, GetAssetLocationColumnsList()))
                {
                    try
                    {
                        List<TripleText> currentSessionInsertedDataList = new List<TripleText>();
                        TripleText currentSessionInsertedData = new TripleText();
                        List<LocationViewModel> locations = _baseInterface.ILocation.GetAllLocations(companyId);
                        List<NumericLookupItem> branchDetails = _baseInterface.ICompany.GetBrancheDetailsByTypeId(companyId, orgLeafLevel.LeafLevelTypeId);
                        var recfound = locations.Where(x => x.LocationId == locationid).ToList();
                        if (recfound.Count() == 0)
                        {
                            bool status = false;
                            string message = "AssetLocation Details not found for LocationId : " + locationid;
                            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                        }

                        int locationCount = 0;
                        long parentId = 0;
                        long currentRowId = 0;
                        long currentBranchId = 0;
                        var oLocationObj = new LocationsModel();
                        if (excelDt.Rows.Count != 0)
                        {
                            DataTable dttable = ValidateDynamicLocationImportDataUpdate(excelDt, companyId, databaseColumns, locationid);

                            if (dttable.Rows.Count == 0)
                            {
                                bool status = false;
                                string message = "";
                                foreach (DataRow dr in excelDt.Rows)
                                {
                                    parentId = 0;
                                    currentRowId = 0;
                                    locationCount = locations.Where(x => x.LocationId == locationid && x.CompanyId == companyId).Count();
                                    if (locationCount >= 1)
                                    {
                                        currentRowId = locationid;
                                        oLocationObj = new LocationsModel
                                        {
                                            UserId = userId,
                                            LocationName = dr["LocationName"].ToString().Trim(),
                                            Code = dr["LocationCode"].ToString().Trim(),
                                            CompanyId = companyId,
                                            LocationId = currentRowId
                                        };
                                        _baseInterface.ILocation.UpdateLocation(oLocationObj);
                                    }
                                }
                                status = true;
                                message = "Asset Location updated successfully.";
                                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                            }
                            else
                            {
                                string JSONresult;
                                bool status = false;
                                JSONresult = JsonConvert.SerializeObject(dttable, Formatting.Indented);
                                var response = Request.CreateResponse(HttpStatusCode.OK);
                                response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                                return response;
                            }
                        }
                        else
                        {
                            bool status = false;
                            string message = "Json AssetLocationDetails not found for update.";
                            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                        }
                    }
                    catch (Exception ex)
                    {
                        bool status = false;
                        string message = ex.Message.ToString();
                        message = !string.IsNullOrEmpty(message) ? message : ex.ToString();
                        return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                        // return Request.CreateResponse(HttpStatusCode.OK, status, message);
                    }
                }
                else
                {
                    bool status = false;
                    string message = "Json format does not match with defined Asset Location. ";
                    return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                }
            }
            else
            {
                bool status = false;
                string message = "Asset Location is not defined.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
        }

        public List<ImportViewModel> GetAssetLocationColumnsList()
        {
            var importViewModel = new List<ImportViewModel>
                {
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = "LocationName",DisplayName = "LocationName" ,ColumnDescription = "LocationName",Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = "LocationCode",DisplayName = "LocationCode" ,ColumnDescription = "LocationCode",Attribute = "required",DropDown = false},
                };
            return importViewModel;
        }

        public DataTable ValidateDynamicLocationImportDataUpdate(DataTable dt, long companyId, HierarchyModel databaseColumns,long locationId)
        {
            DataTable dtError = new DataTable();
            try
            {
                foreach (DataColumn dc in dt.Columns)
                    dtError.Columns.Add(dc.ColumnName);
                dtError.Columns.Add("ErrorMessage");
                var columnHeaderName = "";
                var columnHeaderCode = "";
                DataColumnCollection excelColumns = dt.Columns;
                var retval = 0;
                int recordCount = 0;
              
                var parametersObj = new GenericLookUp();
                parametersObj.Field1 = Convert.ToInt32(ADQFAMS.Common.Enums.HierarchyMasterTypes.Branch);
                parametersObj.Field2 = companyId;
                var orgLeafLevel = _masterApi.GetHierarchyDynamicData(parametersObj);
                var recordDetails = _baseInterface.ILocation.GetAllLocations(companyId);
                long excelBranchid = 0;
               // List<NumericLookupItem> branchDetails = _baseInterface.ICompany.GetBrancheDetailsByTypeId(companyId, orgLeafLevel.LeafLevelTypeId);
               
                var locationObj = new List<LocationViewModel>();

                string sErrorMsg = "";
                int nMsgCnt = 0;
                long v = 0;
                foreach (DataRow dr in dt.Rows)
                {
                    sErrorMsg = "";
                    nMsgCnt = 0;

                    foreach (var data in databaseColumns.HierarchyDynamicList)
                    {
                        recordCount = 0;
                        columnHeaderCode = "LocationCode"; columnHeaderName = "LocationName";
                        //if (branchDetails.Where(x => x.Text.ToString().ToLower().Trim() == dr[orgLeafLevel.LeafLevelName + "Code"].ToString().ToLower().Trim()).ToList().Count() == 0)
                        //{
                        //    retval = -1;
                        //    nMsgCnt++;
                        //    sErrorMsg = sErrorMsg + nMsgCnt + ". " + orgLeafLevel.LeafLevelName + " code does not exist ";
                        //}

                        //1st level name is mandatory
                        if (excelColumns.Contains(columnHeaderName) && dr[columnHeaderName].ToString().Trim() == "")
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderName + " is mandatory ";
                        }

                        //1st level code is mandatory
                        if (excelColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString().Trim() == "")
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " is mandatory ";
                        }
                        if (excelColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString() != "" && dr[columnHeaderCode].ToString().Length > 20)
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " length should not exceed 20 characters. ";
                        }

                        if (excelColumns.Contains(columnHeaderName) && dr[columnHeaderName].ToString() != "" && !regexSpecialCharacters4.IsMatch(dr[columnHeaderName].ToString().Trim()))
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderName + " accepts letters (a-z), numbers (0-9). Special Characters Double Quotes and Back Slash Are Not Allowed";
                        }

                        if (excelColumns.Contains(columnHeaderCode) && dr[columnHeaderCode].ToString() != "" && !regexSpecialCharacters.IsMatch(dr[columnHeaderCode].ToString().Trim()))
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". " + columnHeaderCode + " " + ResourceFile.AllowSpecialCharectersForExcel + " ";
                        }

                        if (recordDetails.Where(x => x.LocationId != locationId && (x.LocationName.ToString().ToLower().Trim() == dr[columnHeaderName].ToString().ToLower().Trim() || x.LocationCode.ToString().ToLower().Trim() == dr[columnHeaderCode].ToString().ToLower().Trim())).Count() > 0)
                        {
                            retval = -1;
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Record already exists with the same Location Name/Location Code";
                            break;
                        }
                    }
                    if (sErrorMsg != "")
                    {
                        dtError.Rows.Add(dr.ItemArray);
                        dtError.Rows[dtError.Rows.Count - 1]["ErrorMessage"] = sErrorMsg;
                        v = 0;
                        continue;
                    }
                    v = 0;
                }
                return dtError;
            }
            finally
            {
                dtError?.Dispose();
            }
        }

        #endregion

        #region Vendor API
        
        [HttpPost]
        public HttpResponseMessage UpdateVendorDetails(long vendorId, VendorCustomerViewModel submitData)
        {
            var json = JsonConvert.SerializeObject(submitData.VendorCustomerDetails);
            DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);
            long companyId, userId;
            bool status = true;
            string message = "";
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());
            if (excelDt == null)
            {
                status = false;
                message = "Json VendorDetails not found for import.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
            var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();
            int VendorTypeID = 0;
            try
            {
                if (ValidateExcelColumns(colsList, GetUpdateVendorColumnsList()))
                {
                    if (excelDt.Rows.Count != 0)
                    {
                        DataTable dttable = ValidateUpdateImportVendor(excelDt, companyId, vendorId);
                        if (dttable.Rows.Count == 0)
                        {
                            foreach (DataRow dr in excelDt.Rows)
                            {
                                var vendortypeidchk = (Convert.ToInt32(new VendorLookUps().GetVendorType().Where(x => x.Text.ToLower() == dr[ResourceFile.VendorTypelabel].ToString().ToLower().Trim()).FirstOrDefault().Value));
                                var v = _vendor.GetVendorId(companyId, Convert.ToInt64(vendorId), vendortypeidchk).ToList();
                                if (v.Count() > 0)
                                {
                                    var VendorName = dr[ResourceFile.VendorNameLabel].ToString().Trim();
                                    VendorViewModel vvm = _vendor.GetVendorById(Convert.ToInt64(vendorId), companyId);
                                    var ovendorObj = new VendorViewModel
                                    {
                                        Createdby = userId,
                                        IsActive = true,
                                        VendorName = dr[ResourceFile.VendorNameLabel].ToString().Trim(),
                                        VendorTypeID = (Convert.ToInt32(new VendorLookUps().GetVendorType().Where(x => x.Text.ToLower() == dr[ResourceFile.VendorTypelabel].ToString().ToLower().Trim()).FirstOrDefault().Value)),
                                        VendorID = v.Select(x => x.Id).FirstOrDefault(),
                                        VendorCode = dr[ResourceFile.VendorCodeLabel].ToString().Trim(),
                                        Mobile = dr[ResourceFile.MobileNo].ToString().Trim(),
                                        City = dr[ResourceFile.City].ToString().Trim(),
                                        Phone = dr[ResourceFile.PhoneNo].ToString().Trim(),
                                        State = dr[ResourceFile.State].ToString().Trim(),
                                        CountryName = dr[ResourceFile.CountryLabel].ToString().Trim(),
                                        ZipCode = dr[ResourceFile.ZipCode].ToString().Trim(),
                                        VendorEmailId = dr[ResourceFile.EmailIdLabel].ToString().Trim(),
                                        PanNo = dr[ResourceFile.PanLabel].ToString().Trim(),
                                        TanNo = dr[ResourceFile.TinOrGst].ToString().Trim(),
                                        Description = dr[ResourceFile.Description].ToString(),
                                        Address = dr[ResourceFile.AddressLabel].ToString(),
                                        ContactPerson = dr[ResourceFile.ContactPerson].ToString(),
                                        CompanyID = companyId,
                                        AddressID = vvm.AddressID

                                    };
                                    int result = _vendor.UpdateVendor(ovendorObj);
                                }
                            }
                            status = true;
                            message = "Vendor Updated successfully.";
                        }
                        else
                        {
                            string JSONresult;
                            status = false;
                            JSONresult = JsonConvert.SerializeObject(dttable, Formatting.Indented);
                            var response = Request.CreateResponse(HttpStatusCode.OK);
                            response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                            return response;
                        }
                    }
                    else
                    {
                        status = false;
                        message = "Json VendorDetails not found for import";
                    }
                }
                else
                {
                    status = false;
                    message = "Json format does not match with defined Vendor.";
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = ex.Message.ToString();
                message = !string.IsNullOrEmpty(message) ? message : ex.ToString();
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
        }

        public List<ImportViewModel> GetUpdateVendorColumnsList()
        {
            var importViewModel = new List<ImportViewModel>
                {
                    //new ImportViewModel{ Checked = true,Required = true,ColumnName = "VendorID",DisplayName = "VendorID" ,ColumnDescription = "VendorID",Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.VendorNameLabel,DisplayName = ResourceFile.VendorNameLabel ,ColumnDescription = ResourceFile.VendorNameLabel,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.VendorTypelabel,DisplayName = ResourceFile.VendorTypelabel ,ColumnDescription = ResourceFile.VendorTypelabel,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.VendorCodeLabel,DisplayName = ResourceFile.VendorCodeLabel ,ColumnDescription = ResourceFile.VendorCodeLabel,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.MobileNo,DisplayName = ResourceFile.MobileNo ,ColumnDescription = ResourceFile.MobileNo,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.PhoneNo,DisplayName = ResourceFile.PhoneNo ,ColumnDescription = ResourceFile.PhoneNo,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.AddressLabel,DisplayName = ResourceFile.AddressLabel ,ColumnDescription = ResourceFile.AddressLabel,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.City,DisplayName = ResourceFile.City ,ColumnDescription = ResourceFile.City,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.State,DisplayName = ResourceFile.State ,ColumnDescription = ResourceFile.State,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.CountryLabel,DisplayName = ResourceFile.CountryLabel ,ColumnDescription = ResourceFile.CountryLabel,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.ZipCode,DisplayName = ResourceFile.ZipCode ,ColumnDescription = ResourceFile.ZipCode,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.EmailIdLabel,DisplayName = ResourceFile.EmailIdLabel ,ColumnDescription = ResourceFile.EmailIdLabel,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.PanLabel,DisplayName = ResourceFile.PanLabel ,ColumnDescription = ResourceFile.PanLabel,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.TinOrGst,DisplayName = ResourceFile.TinOrGst ,ColumnDescription = ResourceFile.TinOrGst,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.Description,DisplayName = ResourceFile.Description ,ColumnDescription = ResourceFile.Description,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.ContactPerson,DisplayName = ResourceFile.ContactPerson ,ColumnDescription = ResourceFile.ContactPerson,Attribute = "notreq",DropDown = false},

                };
            return importViewModel;
        }

        private DataTable ValidateUpdateImportVendor(DataTable datatable, long companyId, long vendorId)
        {
            DataTable dterror = new DataTable();
            var colserror = new DataTable();
            foreach (DataColumn dc in datatable.Columns)
                colserror.Columns.Add(dc.ColumnName);
            colserror.Columns.Add("Error Message");
            string sErrorMsg = "";
            int nMsgCnt = 0;

            try
            {
                var vendors = new VendorLookUps().GetVendorType().ToList();
                {
                    foreach (DataRow dr in datatable.Rows)
                    {
                        sErrorMsg = "";
                        nMsgCnt = 0;

                        string strvendorName = null;
                        foreach (DataColumn dc in colserror.Columns)
                        {
                            if (dc.ColumnName == ResourceFile.VendorTypelabel)
                            {
                                if (dr[ResourceFile.VendorTypelabel].ToString().Trim() == "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Vendor Type should not be empty ";
                                }
                                else if (dr[ResourceFile.VendorTypelabel].ToString().Trim() != "")
                                {
                                    var vendortype = vendors.Where(x => x.Text.ToString().Trim().ToLower() == dr[ResourceFile.VendorTypelabel].ToString().Trim().ToLower()).FirstOrDefault();
                                    if (vendortype == null)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Vendor type doesn't exists ";
                                    }
                                }
                            }
                            else if (dc.ColumnName == "VendorID")
                            {
                                if (vendorId.ToString().Trim() == "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Vendor ID should not be empty ";
                                }
                            }
                            else if (dc.ColumnName == ResourceFile.VendorNameLabel)
                            {
                                if (dr[ResourceFile.VendorNameLabel].ToString().Trim() == "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Vendor Name should not be empty ";
                                }
                                else if (dr[ResourceFile.VendorNameLabel].ToString().Trim() != "")
                                {
                                    var validatevendorname = Regex.IsMatch(dr[ResourceFile.VendorNameLabel].ToString().Trim(), @"\t|""|\\");

                                    if (validatevendorname == true)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Special Characters Double Quotes, Back Slash and Tab Are Not Allowed In Vendor Name ";
                                    }
                                    else
                                        strvendorName = dr[ResourceFile.VendorNameLabel].ToString().Trim();
                                }
                                else if ((dr[ResourceFile.VendorNameLabel].ToString().Length > 100))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Vendor Name length should not be more than 100 characters ";
                                }
                            }
                            else if (dc.ColumnName == ResourceFile.VendorCodeLabel)
                            {
                                if (dr[ResourceFile.VendorCodeLabel].ToString().Trim() != "" && !regexSpecialCharacters2.IsMatch(dr[ResourceFile.VendorCodeLabel].ToString().Trim()))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". " + @"Vendor Code accepts letters (a-z), numbers (0-9), and charecters (-_/\&) ";
                                }
                                else if ((dr[ResourceFile.VendorCodeLabel].ToString().Length > 50))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Vendor Code length should not be more than 50 characters ";
                                }
                            }
                            else if (dc.ColumnName == ResourceFile.ZipCode)
                            {
                                if (dr[ResourceFile.ZipCode].ToString().Trim() != "")
                                {
                                    var zip = Regex.IsMatch(dr[ResourceFile.ZipCode].ToString().Trim(), @"^([0-9]{6})$");
                                    if (zip == false)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". ZIP code is not valid ";
                                    }
                                }
                            }
                            else if (dc.ColumnName == ResourceFile.City)
                            {
                                if (dr[ResourceFile.City].ToString().Trim() != "")
                                {
                                    var zip = Regex.IsMatch(dr[ResourceFile.City].ToString().Trim(), "^[a-zA-Z ]+$");
                                    if (zip == false)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". City is not valid ";
                                    }
                                }
                                else if ((dr[ResourceFile.City].ToString().Length == 30) || (dr[ResourceFile.City].ToString().Length >= 30))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". City length should not be more 30 characters ";
                                }
                            }
                            else if (dc.ColumnName == ResourceFile.State)
                            {
                                if (dr[ResourceFile.State].ToString().Trim() != "")
                                {
                                    var zip = Regex.IsMatch(dr[ResourceFile.State].ToString().Trim(), "^[a-zA-Z ]+$");
                                    if (zip == false)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". State is not valid ";
                                    }
                                }
                                else if ((dr[ResourceFile.State].ToString().Length == 30) || (dr[ResourceFile.State].ToString().Length >= 30))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". State length should not be more 30 characters ";
                                }
                            }
                            else if (dc.ColumnName == ResourceFile.CountryLabel)
                            {
                                if (dr[ResourceFile.CountryLabel].ToString().Trim() != "")
                                {
                                    var zip = Regex.IsMatch(dr[ResourceFile.CountryLabel].ToString().Trim(), "^[a-zA-Z ]+$");
                                    if (zip == false)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Country is not valid ";
                                    }
                                }
                                else if ((dr[ResourceFile.CountryLabel].ToString().Length > 30))
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Country length should not be more than 30 characters ";
                                }
                            }
                            else if (dc.ColumnName == ResourceFile.TinOrGst)
                            {
                                if (dr[ResourceFile.TinOrGst].ToString().Trim() != "")
                                {
                                    if (dr[ResourceFile.TinOrGst].ToString().Length > 20)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". TIN or GSTIN is not valid ";
                                    }
                                }
                            }
                            else if (dc.ColumnName == ResourceFile.EmailIdLabel)
                            {
                                if (dr[ResourceFile.EmailIdLabel].ToString().Trim() != "")
                                {
                                    var value = IsValidEmailId(dr[ResourceFile.EmailIdLabel].ToString().Trim());
                                    if (value == false)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Email Id is not valid ";
                                    }
                                }
                            }
                            else if (dc.ColumnName == ResourceFile.AddressLabel)
                            {
                                if (dr[ResourceFile.AddressLabel].ToString().Trim() != "")
                                {
                                    if (dr[ResourceFile.AddressLabel].ToString().Length > 200)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Address length should not be more than 200 characters. ";
                                    }
                                }
                            }
                        }
                        if (sErrorMsg != "")
                        {
                            colserror.Rows.Add(dr.ItemArray);
                            colserror.Rows[colserror.Rows.Count - 1]["Error Message"] = sErrorMsg;
                            continue;
                        }
                    }
                    return colserror;
                }
            }
            finally
            {
                dterror?.Dispose();
            }
        }
        
        #endregion

        #region User API
        //Added By Rutik RG for Update User API Start >>
        [HttpPost]
        public HttpResponseMessage UpdateUserDetails(long userId, UserAPIViewModel submitData)
        {
            var json = JsonConvert.SerializeObject(submitData.UserDetails);
            DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);
            long companyId;//, userId;
            var identity = (ClaimsIdentity)User.Identity;
            // userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());
            bool status = true;
            string message = "";
            ResultMessage result = new ResultMessage();
            if (excelDt == null)
            {
                status = false;
                message = "Json UserDetails not found for import.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
            var recfound = _user.GetEmployees(companyId).Where(x => x.UserId == userId).ToList();
            if (recfound.Count == 0)
            {
                status = false;
                message = "User Details not found for UserId : " + userId;
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }

            var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();
            try
            {
                if (ValidateExcelColumns(colsList, GetUpdateUserColumnsList()))
                {
                    if (excelDt.Rows.Count != 0)
                    {
                        DataTable dttable = UpdateValidateImportUser(excelDt, companyId, userId);
                        if (dttable.Rows.Count == 0)
                        {
                            foreach (DataRow dr in excelDt.Rows)
                            {
                                var model = new UserViewModel();
                                model.UserId = Convert.ToInt64(userId);//dr["UserId"].ToString().Trim());
                                model.FirstName = dr["FirstName"].ToString().Trim();
                                model.LastName = dr["LastName"].ToString().Trim();
                                model.FullName = dr["FirstName"].ToString().Trim() + "" + dr["LastName"].ToString().Trim();
                                model.UserName = dr["UserName"].ToString().Trim().Replace(" ", "");
                                model.EmailId = dr["Email"].ToString().Trim();
                                model.Phone = dr["PhoneNumber"].ToString().Trim();
                                model.Mobile = dr["MobileNumber"].ToString().Trim();
                                model.Password = dr["Password"].ToString().Trim();
                                model.PasswordSalt = null;
                                model.ConfirmPassword = dr["ConfirmPassword"].ToString().Trim();
                                model.DeviceName = dr["DeviceName"].ToString().Trim();
                                model.RoleName = dr["RoleName"].ToString().Trim();
                                if (model.RoleName != "Asset User")
                                    model.IsServiceDesk = string.IsNullOrEmpty(dr["IsServiceDesk"].ToString().Trim()) ? false : Convert.ToBoolean(dr["IsServiceDesk"].ToString().Trim());
                                model.EmpId = dr["EmployeeId"].ToString().Trim();
                                model.RoleId = GetRolesWithPermissions(companyId).Where(x => x.Name == model.RoleName).FirstOrDefault().Id;
                                model.RoleTypeId = Convert.ToInt16(GetRolesWithPermissions(companyId).Where(x => x.Name == model.RoleName).FirstOrDefault().ParentId);
                                model.IsDeactive = Convert.ToBoolean(dr["IsDeactive"].ToString().Trim());
                                model.CompanyId = companyId;

                                if (model.IsDeactive == true)
                                {
                                    if (string.IsNullOrEmpty(dr["DeactiveDate"].ToString().Trim()) || Convert.ToDateTime(dr["DeactiveDate"].ToString().Trim()) < DateTime.Now.Date)
                                    {
                                        message = "Deactive Date cannot be empty or not be less than current date!!";
                                        status = false;
                                        goto Next;
                                    }
                                    model.DeactiveDate = Convert.ToDateTime(dr["DeactiveDate"].ToString().Trim());
                                }
                                else
                                {
                                    model.DeactiveDate = Convert.ToDateTime("1900-01-01 00:00:00.000");
                                }
                                //if (string.IsNullOrEmpty(model.UserId.ToString()))
                                //{
                                //    message = "UserId could not be empty!!";
                                //    status = false;
                                //    goto Next;
                                //}

                                if (model.RoleTypeId == 103)
                                {
                                    model.Password = "Tracet!%54321";
                                    model.ConfirmPassword = "Tracet!%54321";
                                }
                                if (model.RoleTypeId != 100)
                                {
                                    model.DepartmentList = _baseInterface.IDepartmentService.GetDepartments(companyId).Select(x => new NumericLookupItem { Text = x.Name, Value = x.DepartmentID }).ToList();
                                    string[] dlist = dr["Department"].ToString().Split(',');
                                    if ((dlist.Count() == 1 && dlist[0].ToString() != "") || dlist.Count() > 1)
                                    {
                                        foreach (string s in dlist)
                                        {
                                            if (model.DepartmentList.Where(x => x.Text.Trim() == s.Trim()).Select(x => x.Value).Count() > 0)
                                            {
                                                model.DepartmentIds = model.DepartmentIds
                                                + "," + model.DepartmentList.Where(x => x.Text.Trim() == s.Trim()).Select(x => x.Value).FirstOrDefault().ToString();
                                            }
                                            else
                                            {
                                                message = "Department(s) '" + s + "' not found ";
                                                status = false;
                                                goto Next;
                                            }
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(model.DepartmentIds))
                                    {
                                        if (model.DepartmentIds.StartsWith(","))
                                            model.DepartmentIds = model.DepartmentIds.Substring(1);
                                        if (model.DepartmentIds.EndsWith(","))
                                            model.DepartmentIds = model.DepartmentIds.Substring(0, model.DepartmentIds.Length - 1);
                                    }

                                    model.BranchList = _baseInterface.ICompany.GetAllBrancheDetails(companyId).Select(x => new NumericLookupItem { Text = x.Name, Value = x.BranchId }).ToList();
                                    string[] blist = dr["Branch"].ToString().Split(',');
                                    if ((blist.Count() == 1 && blist[0].ToString() != "") || blist.Count() > 1)
                                    {
                                        foreach (string s in blist)
                                        {
                                            if (model.BranchList.Where(x => x.Text.Trim() == s.Trim()).Select(x => x.Value).Count() > 0)
                                            {
                                                model.BranchIds = model.BranchIds
                                                + "," + model.BranchList.Where(x => x.Text.Trim() == s.Trim()).Select(x => x.Value).FirstOrDefault().ToString();
                                            }
                                            else
                                            {
                                                message = "Branch(s) '" + s + "' not found ";
                                                status = false;
                                                goto Next;
                                            }
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(model.BranchIds))
                                    {
                                        if (model.BranchIds.StartsWith(","))
                                            model.BranchIds = model.BranchIds.Substring(1);
                                        if (model.BranchIds.EndsWith(","))
                                            model.BranchIds = model.BranchIds.Substring(0, model.BranchIds.Length - 1);
                                    }
                                    OrganizationDetailsModel companydetails = _masterApi.GetOrganizationDetailsByCompId(companyId);
                                    model.MainCategoriesList = _masterApi.GetAssetCategories(companyId, userId, model.RoleTypeId.HasValue ? model.RoleTypeId.Value : 0, companydetails.FirmCategory ?? 0).Where(x => x.ParentID == null).Select(x => new NumericLookupItem { Text = x.Name, Value = x.AssetCategoryId }).ToList();
                                    string[] clist = dr["Categories"].ToString().Split(',');
                                    if ((clist.Count() == 1 && clist[0].ToString() != "") || clist.Count() > 1)
                                    {
                                        foreach (string s in clist)
                                        {
                                            if (model.MainCategoriesList.Where(x => x.Text.Trim() == s.Trim()).Select(x => x.Value).Count() > 0)
                                            {
                                                model.CategoryIds = model.CategoryIds
                                                + "," + model.MainCategoriesList.Where(x => x.Text.Trim() == s.Trim()).Select(x => x.Value).FirstOrDefault().ToString();
                                            }
                                            else
                                            {
                                                message = "Category(s) '" + s + "' not found ";
                                                status = false;
                                                goto Next;
                                            }
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(model.CategoryIds))
                                    {
                                        if (model.CategoryIds.StartsWith(","))
                                            model.CategoryIds = model.CategoryIds.Substring(1);
                                        if (model.CategoryIds.EndsWith(","))
                                            model.CategoryIds = model.CategoryIds.Substring(0, model.BranchIds.Length - 1);
                                    }
                                }
                                if (status == true)
                                {
                                    if (ModelState.IsValid)
                                    {
                                        if (UserHelper.IsValidPassword(model.Password))
                                        {
                                            if (model.RoleTypeId == 103)
                                            {
                                                var ITAssetusers = _user.GetEmployees(companyId).Where(x => x.IsActivechk == true && x.ITAssetschk == true).ToList();
                                                var helper = new ADQFAMS.Web.Helpers.LicenseHelper();
                                                var noofITAssetuser = helper.GetNoOfITAssetUsers(companyId);
                                                var isAllowITAssetUserCreate = noofITAssetuser > ITAssetusers.Count;
                                                if (model.ITAssetschk)
                                                {
                                                    if (!isAllowITAssetUserCreate)
                                                    {
                                                        message = "Unable to create a new IT Asset user since the number of users limit is reached";
                                                        status = false;
                                                        goto Next;
                                                    }
                                                }
                                            }
                                            if (model.RoleTypeId != 103)
                                            {
                                                var users = _user.GetEmployees(companyId).Where(x => x.RoleTypeId != 103).ToList();
                                                var ITAssetusers = _user.GetEmployees(companyId).Where(x => x.IsActivechk == true && x.ITAssetschk == true).ToList();
                                                var helper = new ADQFAMS.Web.Helpers.LicenseHelper();
                                                var noofuser = helper.GetNoOfUsers(companyId);
                                                var noofITAssetuser = helper.GetNoOfITAssetUsers(companyId);
                                                var isAllowITAssetUserCreate = noofITAssetuser > ITAssetusers.Count;
                                                var isAllowUserCreate = noofuser > users.Count;
                                                var ServiceDeskUsers = _user.GetEmployees(companyId).Where(x => x.IsServiceDesk == true).ToList();
                                                var noofServiceDeskUser = helper.GetNoOfServiceDeskUser(companyId);
                                                var isAllowServiceDeskUserCreate = noofServiceDeskUser > ServiceDeskUsers.Count;
                                                if (model.IsServiceDesk && isAllowUserCreate)
                                                {
                                                    if (!isAllowServiceDeskUserCreate)
                                                    {
                                                        message = "Unable to create a new Service Desk user since the number of users limit is reached";
                                                        status = false;
                                                        goto Next;
                                                    }
                                                }
                                                if (model.ITAssetschk && isAllowUserCreate)
                                                {
                                                    if (!isAllowITAssetUserCreate)
                                                    {
                                                        message = "Unable to create a new IT Asset user since the number of users limit is reached";
                                                        status = false;
                                                        goto Next;
                                                    }
                                                }
                                                if (!isAllowUserCreate && !Constants.isDevelopment == true)
                                                {
                                                    message = "Unable to create a new user since the number of users limit is reached";
                                                    status = false;
                                                    goto Next;
                                                }
                                            }
                                            model.CreatedBy = userId;
                                            var authInfoDet = _user.GetEmployees(companyId).Where(x => x.UserId == userId).ToList().FirstOrDefault();
                                            

                                            if (authInfoDet != null && authInfoDet.RoleTypeId != 100 && model.RoleTypeId == 100)
                                            {
                                                message = "Limited Access User cannot add Root admin user.";
                                                status = false;
                                                goto Next;
                                            }
                                            result = _userManagementApi.AddEmployee(model);
                                        }
                                        else
                                        {
                                            message = "Password did not match the policy";
                                            status = false;
                                            goto Next;
                                        }
                                    }
                                    else
                                    {
                                        message = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                                        status = false;
                                        goto Next;
                                    }
                                }
                            }
                            if (status == true)
                            {
                                status = true;
                                message = "User updated successfully.";
                            }
                        }
                        else
                        {
                            string JSONresult;
                            status = false;
                            JSONresult = JsonConvert.SerializeObject(dttable, Formatting.Indented);
                            var response = Request.CreateResponse(HttpStatusCode.OK);
                            response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                            return response;
                        }
                    }
                    else
                    {
                        status = false;
                        message = "Json UserDetails not found for update";
                    }
                }
                else
                {
                    status = false;
                    message = "Json format does not match with defined User.";
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = ex.ToString();
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
            Next:
            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
        }

        public List<ImportViewModel> GetUpdateUserColumnsList()
        {
            var importViewModel = new List<ImportViewModel>
                {
                 //  new ImportViewModel{ Checked = true,Required = true,ColumnName = "UserId",DisplayName = "UserId" ,ColumnDescription = "UserId",Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.FirstNameLabel,DisplayName = ResourceFile.FirstNameLabel ,ColumnDescription = ResourceFile.FirstNameLabel,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.LastNameLabel,DisplayName = ResourceFile.LastNameLabel ,ColumnDescription = ResourceFile.LastNameLabel,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.EmailLabel,DisplayName = ResourceFile.EmailLabel ,ColumnDescription = ResourceFile.EmailLabel,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.MobileNumber,DisplayName = ResourceFile.MobileNumber ,ColumnDescription = ResourceFile.MobileNumber,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.PhoneNumber,DisplayName = ResourceFile.PhoneNumber ,ColumnDescription = ResourceFile.PhoneNumber,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.UserName,DisplayName = ResourceFile.UserName ,ColumnDescription = ResourceFile.UserName,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.EmployeeId,DisplayName = ResourceFile.EmployeeId ,ColumnDescription = ResourceFile.EmployeeId,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.DeviceName,DisplayName = ResourceFile.DeviceName ,ColumnDescription = ResourceFile.DeviceName,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.Password,DisplayName = ResourceFile.Password ,ColumnDescription = ResourceFile.Password,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.ConfirmPassword,DisplayName = ResourceFile.ConfirmPassword ,ColumnDescription = ResourceFile.ConfirmPassword,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = "RoleName",DisplayName = "RoleName" ,ColumnDescription = "RoleName",Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = "IsServiceDesk",DisplayName = "IsServiceDesk" ,ColumnDescription = "IsServiceDesk",Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = "Department",DisplayName = "Department" ,ColumnDescription = "Department",Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = "Categories",DisplayName = "Categories" ,ColumnDescription = "Categories",Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = "Branch",DisplayName = "Branch" ,ColumnDescription = "Branch",Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = "IsDeactive",DisplayName = "IsDeactive" ,ColumnDescription = "IsDeactive",Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = "DeactiveDate",DisplayName = "DeactiveDate" ,ColumnDescription = "DeactiveDate",Attribute = "notreq",DropDown = false},
                };
            return importViewModel;
        }

        private DataTable UpdateValidateImportUser(DataTable datatable, long companyId,long userId)
        {
            DataTable dterror = new DataTable();
            var colserror = new DataTable(); ;
            foreach (DataColumn dc in datatable.Columns)
                colserror.Columns.Add(dc.ColumnName);
            colserror.Columns.Add("Error Message");
            string sErrorMsg = "";
            int nMsgCnt = 0;

            foreach (DataRow dr in datatable.Rows)
            {
                sErrorMsg = "";
                nMsgCnt = 0;

                if (datatable.AsEnumerable().Where(x => x.Field<string>("UserName") == dr["UserName"].ToString()).Count() > 1)
                {
                    nMsgCnt++;
                    sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected with the same UserName in the Json";
                }
                else if (datatable.AsEnumerable().Where(x => x.Field<string>("Email") == dr["Email"].ToString()).Count() > 1)
                {
                    nMsgCnt++;
                    sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected with the same Email in the Json";
                }
                else if (datatable.AsEnumerable().Where(x => x.Field<string>("EmployeeId") == dr["EmployeeId"].ToString()).Count() > 1)
                {
                    nMsgCnt++;
                    sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry detected with the same EmployeeId in the Json";
                }
                else
                {
                    foreach (DataColumn dc in colserror.Columns)
                    {
                        if (dc.ColumnName == "FirstName")
                        {
                            if (dr["FirstName"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". First Name should not be empty ";
                            }
                            else if ((dr["FirstName"].ToString().Length > 100))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". First Name length should not be more than 100 characters ";
                            }
                        }
                        else if (dc.ColumnName == "LastName")
                        {
                            if (dr["LastName"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Last Name should not be empty ";

                            }
                            else if ((dr["LastName"].ToString().Length > 100))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Last Name length should not be more 100 characters ";
                            }
                        }
                        else if (dc.ColumnName == "EmployeeId")
                        {
                            if (dr["EmployeeId"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Employee Id should not be empty ";
                            }
                            else
                            {
                                var resultEmpId = _user.ValidatChkUser(companyId, Convert.ToInt32(userId), "EmployeeId", dr["EmployeeId"].ToString().Trim());
                                if (resultEmpId == true)
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Employee Id already exists ";
                                }
                            }
                        }
                        else if (dc.ColumnName == "UserName")
                        {
                            if (dr["UserName"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". User Name should not be empty ";
                            }
                            else if ((dr["UserName"].ToString().Length < 4))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". User Name length should not be less than 4 characters ";
                            }
                            else if ((dr["UserName"].ToString().Length > 50))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". User Name length should not be more 50 characters ";
                            }
                            else
                            {
                                var resultuser = _user.ValidatChkUser(companyId, Convert.ToInt32(userId), "UserName", dr["UserName"].ToString().Trim());//CheckUsername(companyId, dr["UserName"].ToString().Trim());
                                if (resultuser == true)
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". User Name already exists ";
                                }
                            }
                        }
                        else if (dc.ColumnName == "Email")
                        {
                            if (dr["Email"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Email Id should not be empty ";
                            }
                            else if (dr["Email"].ToString().Trim() != "")
                            {
                                var value = IsValidEmailId(dr["Email"].ToString().Trim());
                                if (value == false)
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Email Id is not Valid ";
                                }
                                var em = _user.ValidatChkUser(companyId, Convert.ToInt32(userId), "EmailId", dr["Email"].ToString().Trim());//CheckEmailId(companyId, dr["Email"].ToString().Trim());
                                if (em)
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Email Id already exists ";
                                }
                            }
                        }
                        //Added by Priyanka B on 24062024 for ServiceDeskAPI Start
                        else if (dc.ColumnName == "RoleName")
                        {
                            if (dr["RoleName"].ToString().Trim() == "")
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Role Name should not be empty ";

                            }
                            string rolenm1 = dr["RoleName"].ToString();
                            var value = GetRolesWithPermissions(companyId).Where(x => x.Name == rolenm1).ToList();
                            if (value != null)
                            {
                                if (value.Count() == 0)
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Role Name does not exists ";
                                }
                            }
                        }
                        else if (dc.ColumnName == "Password")
                        {
                            if (dr["RoleName"].ToString().Trim() != "Asset User")
                            {
                                if (dr["Password"].ToString().Trim() == "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Password should not be empty ";
                                }
                                else if (dr["Password"].ToString().Trim() != "")
                                {
                                    var value = IsValidPassword(dr["Password"].ToString().Trim());
                                    if (value == false)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". Password is not Valid ";
                                    }
                                }
                            }
                            else if (dr["RoleName"].ToString().Trim() == "Asset User")
                            {
                                if (dr["Password"].ToString().Trim() != "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Password not required for 'Asset User' ";
                                }
                            }
                        }
                        else if (dc.ColumnName == "ConfirmPassword")
                        {
                            if (dr["RoleName"].ToString().Trim() != "Asset User")
                            {
                                if (dr["ConfirmPassword"].ToString().Trim() == "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". ConfirmPassword should not be empty ";
                                }
                                else if (dr["ConfirmPassword"].ToString().Trim() != "")
                                {
                                    var value = IsValidPassword(dr["ConfirmPassword"].ToString().Trim());
                                    if (value == false)
                                    {
                                        nMsgCnt++;
                                        sErrorMsg = sErrorMsg + nMsgCnt + ". ConfirmPassword is not Valid ";
                                    }
                                }
                            }
                            else if (dr["RoleName"].ToString().Trim() == "Asset User")
                            {
                                if (dr["ConfirmPassword"].ToString().Trim() != "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". ConfirmPassword not required for 'Asset User' ";
                                }
                            }
                        }

                        else if (dc.ColumnName == "Department")
                        {
                            if (dr["RoleName"].ToString().Trim() == "Root Admin")
                            {
                                if (dr["Department"].ToString().Trim() != "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Department not required for Root Admin ";
                                }
                            }
                            else
                            {
                                if (dr["Department"].ToString().Trim() == "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Department should not be empty ";
                                }
                            }
                        }
                        else if (dc.ColumnName == "Branch")
                        {
                            if (dr["RoleName"].ToString().Trim() == "Root Admin")
                            {
                                if (dr["Branch"].ToString().Trim() != "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Branch not required for Root Admin ";
                                }
                            }
                            else
                            {
                                if (dr["Branch"].ToString().Trim() == "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Branch should not be empty ";
                                }
                            }
                        }
                        else if (dc.ColumnName == "Categories")
                        {
                            if (dr["RoleName"].ToString().Trim() == "Root Admin")
                            {
                                if (dr["Categories"].ToString().Trim() != "")
                                {
                                    nMsgCnt++;
                                    sErrorMsg = sErrorMsg + nMsgCnt + ". Categories not required for Root Admin ";
                                }
                            }
                        }
                    }
                    if (dr["RoleName"].ToString().Trim() != "Asset User")
                    {
                        if (!string.IsNullOrEmpty(dr["Password"].ToString().Trim()) && !string.IsNullOrEmpty(dr["ConfirmPassword"].ToString().Trim()))
                        {
                            if (dr["Password"].ToString().Trim() != dr["ConfirmPassword"].ToString().Trim())
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Password and Confirm Password does not match ";
                            }
                        }
                    }
                }
                if (sErrorMsg != "")
                {
                    colserror.Rows.Add(dr.ItemArray);
                    colserror.Rows[colserror.Rows.Count - 1]["Error Message"] = sErrorMsg;
                    continue;
                }
            }
            return colserror;
        }

        //Added By Rutik RG for Update User API End <<
        #endregion

        #region Customer API
            
        [HttpPost]
        public HttpResponseMessage UpdateCustomerDetails(long customerId, VendorCustomerViewModel submitData)
        {
            var json = JsonConvert.SerializeObject(submitData.VendorCustomerDetails);
            DataTable excelDt = JsonConvert.DeserializeObject<DataTable>(json);
            long companyId, userId;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());

            bool status = true;
            string message = "";

            if (excelDt == null)
            {
                status = false;
                message = "Json CustomerDetails not found for import.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
            var recfound = _vendor.GetVendor(companyId);
            var r = recfound.Select("VendorId=" + customerId).ToList();
                //.Where(x => x.VendorId == customerId);
            if (r.Count == 0)
            {
                status = false;
                message = "Customer Details not found for CustomerId : " + customerId;
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }

            var colsList = (from DataColumn dc in excelDt.Columns select dc.ColumnName).ToList();

            int VendorTypeID = 0;
            try
            {
                if (ValidateExcelColumns(colsList, GetUpdateCustomerColumnsList()))
                {
                    if (excelDt.Rows.Count != 0)
                    {
                        DataTable dttable = ValidateUpdateImportCustomer(excelDt, companyId, customerId);
                        if (dttable.Rows.Count == 0)
                        {
                            foreach (DataRow dr in excelDt.Rows)
                            {
                                string BranchIds = "";
                                var BranchList = _baseInterface.ICompany.GetAllBrancheDetails(companyId).Select(x => new NumericLookupItem { Text = x.Name, Value = x.BranchId }).ToList();
                                string[] blist = dr["Branch"].ToString().Split(',');
                                if ((blist.Count() == 1 && blist[0].ToString() != "") || blist.Count() > 1)
                                {
                                    foreach (string s in blist)
                                    {
                                        if (BranchList.Where(x => x.Text.Trim() == s.Trim()).Select(x => x.Value).Count() > 0)
                                        {
                                            BranchIds = BranchIds + "," + BranchList.Where(x => x.Text.Trim() == s.Trim()).Select(x => x.Value).FirstOrDefault().ToString();
                                        }
                                        else
                                        {
                                            message = "Branch(s) '" + s + "' not found ";
                                            status = false;
                                            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                                        }
                                    }
                                }
                                if (!string.IsNullOrEmpty(BranchIds))
                                {
                                    if (BranchIds.StartsWith(","))
                                        BranchIds = BranchIds.Substring(1);
                                    if (BranchIds.EndsWith(","))
                                        BranchIds = BranchIds.Substring(0, BranchIds.Length - 1);
                                }
                                var ovendorObj = new VendorViewModel
                                {
                                    Createdby = userId,
                                    IsActive = true,
                                    VendorID = Convert.ToInt64(customerId),
                                    //VendorType = dr["Customer Type"].ToString().Trim(),
                                    VendorName = dr[ResourceFile.CustomerNameLabel].ToString().Trim(),
                                    VendorTypeID = 110, //for customer
                                    Mobile = dr[ResourceFile.MobileNo].ToString().Trim(),
                                    City = dr[ResourceFile.City].ToString().Trim(),
                                    Phone = dr[ResourceFile.PhoneNo].ToString().Trim(),
                                    State = dr[ResourceFile.State].ToString().Trim(),
                                    CountryName = dr[ResourceFile.CountryLabel].ToString().Trim(),
                                    ZipCode = dr[ResourceFile.ZipCode].ToString().Trim(),
                                    VendorEmailId = dr[ResourceFile.EmailIdLabel].ToString().Trim(),
                                    PanNo = dr[ResourceFile.PanLabel].ToString().Trim(),
                                    TanNo = dr[ResourceFile.TinOrGst].ToString().Trim(),
                                    Description = dr[ResourceFile.Description].ToString(),
                                    AddOnAddress = dr[ResourceFile.AddressLabel].ToString(),
                                    ContactPerson = dr[ResourceFile.ContactPerson].ToString(),
                                    BranchIds = BranchIds,
                                    BillingAddress = dr[ResourceFile.BillingAddress].ToString(),
                                    Attachment1 = dr[ResourceFile.Attachment1].ToString(),
                                    Attachment2 = dr[ResourceFile.Attachment2].ToString(),
                                    Attachment3 = dr[ResourceFile.Attachment3].ToString(),
                                    CompanyID = companyId
                                };
                                //var vendortypeidchk = (Convert.ToInt32(new VendorLookUps().GetVendorType().Where(x => x.Text.ToLower() == dr["Customer Type"].ToString().ToLower().Trim()).FirstOrDefault().Value));
                                var v = _vendor.GetVendorId(companyId, Convert.ToInt64(customerId), 110).ToList();
                                if (v.Count() > 0)
                                {
                                    //var VendorName = dr[ResourceFile.VendorNameLabel].ToString().Trim();
                                    VendorViewModel vvm = _vendor.GetVendorById(Convert.ToInt64(customerId), companyId);
                                    ovendorObj.VendorTypeID = 110;
                                    ovendorObj.AddressID = vvm.AddressID;
                                    ovendorObj.VendorID = vvm.VendorID;
                                    int result = _vendor.UpdateVendor(ovendorObj);
                                    if (result > 0)
                                    {
                                        ovendorObj.MainLocation = dr[ResourceFile.MainLocation].ToString();
                                        ovendorObj.SubLocation = dr[ResourceFile.SubLocation].ToString();
                                        _vendor.UpdateMainSubLocationAPI(ovendorObj, vvm.VendorID);
                                        status = true;
                                        message = "Customer Updated successfully.";
                                    }
                                    else if(result<0)
                                    {
                                        status = false;
                                        message = "Duplicate Customer found.";
                                    }
                                }
                            }
                        }
                        else
                        {
                            string JSONresult;
                            status = false;
                            JSONresult = JsonConvert.SerializeObject(dttable, Formatting.Indented);
                            var response = Request.CreateResponse(HttpStatusCode.OK);
                            response.Content = new StringContent("{\"ErrorDetails\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
                            return response;
                        }
                    }
                    else
                    {
                        status = false;
                        message = "Json CustomerDetails not found for import";
                    }
                }
                else
                {
                    status = false;
                    message = "Json format does not match with defined Customer.";
                }
            }
            catch (Exception ex)
            {
                status = false;
                message = ex.Message.ToString();
                message = !string.IsNullOrEmpty(message) ? message : ex.ToString();
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
            return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
        }

        public List<ImportViewModel> GetUpdateCustomerColumnsList()
        {
            var importViewModel = new List<ImportViewModel>
                {
                    //new ImportViewModel{ Checked = true,Required = true,ColumnName = "CustomerID",DisplayName = "CustomerID" ,ColumnDescription = "CustomerID",Attribute = "required",DropDown = false},
                    //new ImportViewModel{ Checked = true,Required = true,ColumnName = "Customer Type",DisplayName = "Customer Type" ,ColumnDescription = "Customer Type",Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.CustomerNameLabel,DisplayName = ResourceFile.CustomerNameLabel ,ColumnDescription = ResourceFile.CustomerNameLabel,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.PanLabel,DisplayName = ResourceFile.PanLabel ,ColumnDescription = ResourceFile.PanLabel,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.TinOrGst,DisplayName = ResourceFile.TinOrGst ,ColumnDescription = ResourceFile.TinOrGst,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.AddressLabel,DisplayName = ResourceFile.AddressLabel ,ColumnDescription = ResourceFile.AddressLabel,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.City,DisplayName = ResourceFile.City ,ColumnDescription = ResourceFile.City,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.State,DisplayName = ResourceFile.State ,ColumnDescription = ResourceFile.State,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.CountryLabel,DisplayName = ResourceFile.CountryLabel ,ColumnDescription = ResourceFile.CountryLabel,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.ZipCode,DisplayName = ResourceFile.ZipCode ,ColumnDescription = ResourceFile.ZipCode,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.MobileNo,DisplayName = ResourceFile.MobileNo ,ColumnDescription = ResourceFile.MobileNo,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.PhoneNo,DisplayName = ResourceFile.PhoneNo ,ColumnDescription = ResourceFile.PhoneNo,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.EmailIdLabel,DisplayName = ResourceFile.EmailIdLabel ,ColumnDescription = ResourceFile.EmailIdLabel,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.MainLocation,DisplayName = ResourceFile.MainLocation ,ColumnDescription = ResourceFile.MainLocation,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = true,ColumnName = ResourceFile.SubLocation,DisplayName = ResourceFile.SubLocation ,ColumnDescription = ResourceFile.SubLocation,Attribute = "required",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.Description,DisplayName = ResourceFile.Description ,ColumnDescription = ResourceFile.Description,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.ContactPerson,DisplayName = ResourceFile.ContactPerson ,ColumnDescription = ResourceFile.ContactPerson,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = "Branch",DisplayName = "Branch" ,ColumnDescription = "Branch",Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.BillingAddress,DisplayName = ResourceFile.BillingAddress ,ColumnDescription = ResourceFile.BillingAddress,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.Attachment1,DisplayName = ResourceFile.Attachment1 ,ColumnDescription = ResourceFile.Attachment1,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.Attachment2,DisplayName = ResourceFile.Attachment2 ,ColumnDescription = ResourceFile.Attachment2,Attribute = "notreq",DropDown = false},
                    new ImportViewModel{ Checked = true,Required = false,ColumnName = ResourceFile.Attachment3,DisplayName = ResourceFile.Attachment3 ,ColumnDescription = ResourceFile.Attachment3,Attribute = "notreq",DropDown = false},

                };
            return importViewModel;
        }

        public DataTable ValidateUpdateImportCustomer(DataTable datatable, long companyId, long customerId)
        {
            DataTable dterror = new DataTable();
            var colserror = new DataTable(); ;
            foreach (DataColumn dc in datatable.Columns)
                colserror.Columns.Add(dc.ColumnName);
            colserror.Columns.Add("Error Message");
            var locationModel = new VendorLocationViewModel();
            locationModel.CustomerList = _iAssetService.GetLetOutVendors(companyId);
            var vendorLocations = _vendor.GetAllVendorLocations(companyId);
            string mainLocationName = ""; string subLocationName = "";
            string strvendorName = "";
            List<TripleText> excelDataList = new List<TripleText>();
            TripleText excelData = new TripleText();
            foreach (DataRow dr in datatable.Rows)
            {
                if (dr[ResourceFile.CustomerNameLabel].ToString().Trim() != "")
                {
                    excelData = new TripleText();
                    excelData.Text1 = dr[ResourceFile.CustomerNameLabel].ToString();
                    excelDataList.Add(excelData);
                }
            }
            string sErrorMsg = "";
            int nMsgCnt = 0;
            foreach (DataRow dr in datatable.Rows)
            {
                sErrorMsg = "";
                nMsgCnt = 0;

                strvendorName = null;
                mainLocationName = ""; subLocationName = "";
                foreach (DataColumn dc in colserror.Columns)
                {
                    if (dc.ColumnName == ResourceFile.CustomerNameLabel)
                    {
                        if (dr[ResourceFile.CustomerNameLabel].ToString().Trim() == "")
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Customer Name should not be empty ";
                        }
                        else if (dr[ResourceFile.CustomerNameLabel].ToString().Trim() != "" && (dr[ResourceFile.CustomerNameLabel].ToString().Length > 100))
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Customer Name length should not be more than 100 characters ";
                        }
                        else if (excelDataList.Where(x => x.Text1.ToString().ToLower().Trim() == dr[ResourceFile.CustomerNameLabel].ToString().ToLower().Trim()).Count() > 1)
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Duplicate entry of Customer name in json file ";
                        }
                    }
                    else if (dc.ColumnName == "CustomerID")
                    {
                        if (customerId.ToString().Trim() == "")
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Customer ID should not be empty ";
                        }
                    }
                    else if (dc.ColumnName == ResourceFile.ZipCode)
                    {
                        if (dr["ZIP Code"].ToString().Trim() != "")
                        {
                            var zip = Regex.IsMatch(dr["ZIP Code"].ToString().Trim(), @"^([0-9]{6})$");
                            if (zip == false)
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". ZIP code is not valid ";
                            }
                        }
                    }
                    else if (dc.ColumnName == ResourceFile.City)
                    {
                        if (dr["City"].ToString().Trim() != "" && (dr["City"].ToString().Length == 30) || (dr["City"].ToString().Length >= 30))
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". City length should not be more 30 characters ";
                        }
                    }
                    else if (dc.ColumnName == ResourceFile.State)
                    {
                        if (dr["State"].ToString().Trim() != "" && (dr["State"].ToString().Length == 30) || (dr["State"].ToString().Length >= 30))
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". State length should not be more 30 characters ";
                        }
                    }
                    else if (dc.ColumnName == ResourceFile.CountryLabel)
                    {
                        if (dr["Country"].ToString().Trim() != "" && (dr["Country"].ToString().Length > 30))
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Country length should not be more than 30 characters ";
                        }
                    }
                    else if (dc.ColumnName == ResourceFile.TinOrGst)
                    {
                        if (dr[ResourceFile.TinOrGst].ToString().Trim() != "")
                        {
                            if (dr[ResourceFile.TinOrGst].ToString().Length > 20)
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". TIN or GSTIN length should not be more than 20 characters ";
                            }
                        }
                    }
                    else if (dc.ColumnName == ResourceFile.EmailIdLabel)
                    {
                        if (dr[ResourceFile.EmailIdLabel].ToString().Trim() != "")
                        {
                            var value = IsValidEmailId(dr[ResourceFile.EmailIdLabel].ToString().Trim());
                            if (value == false)
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Email Id is not valid ";
                            }
                        }
                    }
                    else if (dc.ColumnName == ResourceFile.AddressLabel)
                    {
                        if (dr[ResourceFile.AddressLabel].ToString().Trim() != "")
                        {
                            if (dr[ResourceFile.AddressLabel].ToString().Length > 500)
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". " + ResourceFile.VendorAddressMaxLengthMessage + " ";
                            }
                        }
                    }
                    else if (dc.ColumnName == ResourceFile.MainLocation)
                    {
                        if (dr[ResourceFile.MainLocation].ToString().Trim() == "")
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Main location is mandatory ";
                        }
                        else if (dr[ResourceFile.MainLocation].ToString().Trim() != "")
                        {
                            mainLocationName = dr[ResourceFile.MainLocation].ToString().ToLower().Trim();
                            if (vendorLocations != null && vendorLocations.Any(x => x.LocationName.Trim().ToLower() == mainLocationName && x.CustomerName == strvendorName && x.LocationTypeId == 100))
                            {
                                nMsgCnt++;
                                sErrorMsg = sErrorMsg + nMsgCnt + ". Main location already exists ";
                            }
                        }
                    }
                    else if (dc.ColumnName == ResourceFile.SubLocation)
                    {
                        if (dr[ResourceFile.SubLocation].ToString().Trim() == "")
                        {
                            nMsgCnt++;
                            sErrorMsg = sErrorMsg + nMsgCnt + ". Sub location is mandatory ";
                        }
                    }
                }
                if (sErrorMsg != "")
                {
                    colserror.Rows.Add(dr.ItemArray);
                    colserror.Rows[colserror.Rows.Count - 1]["Error Message"] = sErrorMsg;
                    continue;
                }
            }
            return colserror;
        }
        #endregion

        #region UserAttributesDetails API

        [HttpPost]
        public HttpResponseMessage UpdateUserAttributesDetails(long GroupId,UserAttributesAPI submitData)
        {
            long companyId, userId;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());

            bool status = true;
            string message = "";

            //var recfound = _vendor.GetVendor(companyId);
            var recfound = _iCategoryService.GetAdditionalFieldMapping(companyId);
            var r = recfound.Where(x => x.GroupingId == GroupId).ToList();
            if (r.Count == 0)
            {
                status = false;
                message = "UserAttributes Details not found for GroupId : " + GroupId;
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }

            //Validations Start
            if (submitData == null)
            {
                status = false;
                message = "Json UserAttributesDetails not found for update.";
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
            }
            if (string.IsNullOrEmpty(submitData.GroupName) || submitData.GroupName == null)
            {
                status = false;
                message = "Please Enter Group Name.";
                return Request.CreateResponse(HttpStatusCode.ExpectationFailed, new { status, message });
            }

            foreach (var d in submitData.ListOfUserAttributes)
            {
                if (string.IsNullOrEmpty(d.AdditionalFieldName))
                {
                    status = false;
                    message = "Please Enter Additional Field Name.";
                    return Request.CreateResponse(HttpStatusCode.ExpectationFailed, new { status, message });
                }
                if (string.IsNullOrEmpty(d.ControlName))
                {
                    status = false;
                    message = "Please Enter Control Name.";
                    return Request.CreateResponse(HttpStatusCode.ExpectationFailed, new { status, message });
                }
                var list = new AssetLookups().AdditionalFieldControls().Where(x => x.Text == d.ControlName);
                if (list.Count() == 0)
                {
                    status = false;
                    message = "Please Enter Valid ControlName.";
                    return Request.CreateResponse(HttpStatusCode.ExpectationFailed, new { status, message });
                }
            }
            //Validations End

            var i = 0;

            foreach (var d in submitData.ListOfUserAttributes)
            {
                if (string.IsNullOrEmpty(Convert.ToString(d.IsMandatory)))
                {
                    d.IsMandatory = false;
                }
                if (d.XmlFieldData == "")
                {
                    d.XmlFieldData = "[]";
                }
                else
                {
                    string[] s = d.XmlFieldData.Split(',');
                    List<XMLFieldDataAPI> xmlflddata = new List<XMLFieldDataAPI>();
                    for (int j = 0; j < s.Length; j++)
                    {
                        xmlflddata.Add(new XMLFieldDataAPI
                        {
                            Value = j + 1,
                            Text = s[j],
                            ParentId = 0,
                            TypeId = 0
                        });
                    }
                    // Convert list to JSON array
                    string json = JsonConvert.SerializeObject(xmlflddata, Formatting.Indented);
                    d.XmlFieldData = json;
                }
            }
            string parameter = string.Empty;
            XElement root = new XElement("Groups");

            var serializer = new JavaScriptSerializer();

            XElement elem1 = new XElement("Group"); elem1.SetAttributeValue("Name", submitData.GroupName.Trim());

            foreach (var data in submitData.ListOfUserAttributes)
            {
                var list = new AssetLookups().AdditionalFieldControls().Where(x => x.Text == data.ControlName);
                if (list.Count() == 0)
                {
                    status = false;
                    message = "Please Enter Valid ControlName.";
                    return Request.CreateResponse(HttpStatusCode.ExpectationFailed, new { status, message });
                }
                data.ControlTypeId = Convert.ToInt32(list.FirstOrDefault().Value);

                var checkstatus = _iCategoryService.CheckAdditionalGroupingFieldMapping2Details(data.AdditionalFieldName, companyId, 0);
                if (checkstatus == false)
                {
                    status = false;
                    message = "User Attribute name already exists.";
                    return Request.CreateResponse(HttpStatusCode.ExpectationFailed, new { status, message });
                }

                string AttributeType = new AssetLookups().AdditionalFieldControls().FirstOrDefault(x => x.Value == data.ControlTypeId).Text;
                i++;
                var invoiceItemList = serializer.Deserialize<List<NumericLookupItem>>(data.XmlFieldData);
                XElement ch1;
                if (invoiceItemList.Count > 0)
                    ch1 = new XElement("Attribute");
                else
                    ch1 = new XElement("Attribute", 0);
                ch1.SetAttributeValue("Id", i);
                ch1.SetAttributeValue("Name", data.AdditionalFieldName);
                ch1.SetAttributeValue("IsMandatory", data.IsMandatory ? "True" : "False");
                ch1.SetAttributeValue("AttributeType", AttributeType);
                ch1.SetAttributeValue("Value", "");

                int k = 0;
                foreach (var innerData in invoiceItemList)
                {
                    k++;
                    XElement child = new XElement("AdditionalDesign", 0);
                    child.SetAttributeValue("Id", innerData.Value);
                    child.SetAttributeValue("Text", innerData.Text);
                    ch1.Add(child);
                }
                elem1.Add(ch1);
            }

            root.Add(elem1);

            try
            {
                var status1 = false;
                
                status1 = _iCategoryService.EditAdditionalGroupMapping(submitData.GroupName, root.ToString(), userId, companyId, submitData.GroupId);
                if (status1 == true)
                {
                    //return Json(new { Status = true, Message = "User Attribute Group Updated Successfully" }, JsonRequestBehavior.AllowGet);
                    status = true;
                    message = "User Attribute Group Updated Successfully";
                    return Request.CreateResponse(HttpStatusCode.OK, new { status, message });
                }
                else
                {
                    //return Json(new { Status = false, Message = "Group name already exists" }, JsonRequestBehavior.AllowGet);
                    status = false;
                    message = "Group name already exists";
                    return Request.CreateResponse(HttpStatusCode.ExpectationFailed, new { status, message });
                }
            }
            catch (Exception ex)
            {
                //return Json(new { Status = false, Message = ex.Message }, JsonRequestBehavior.AllowGet);
                status = false;
                message = ex.ToString();
                return Request.CreateResponse(HttpStatusCode.ExpectationFailed, new { status, message });
            }
        }
        #endregion

        #endregion


        #region Get All List API

        #region Country & Currency List API
        //Added by Rupesh G on 27062024 for Country&Currency API Start
        //[EnableCors(origins: "*", headers: "*", methods: "*")]
        [HttpGet]
        public HttpResponseMessage GetCountry()
        {
            var result = _Company.GetCountires().ToList();
            var CountryList = result.Select(x => new { CountryId = x.Value, CountryName = x.Text });

            var JSONresult = JsonConvert.SerializeObject(CountryList, Formatting.Indented);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent("{\"CountryList\":" + JSONresult.ToString() + "}", Encoding.UTF8, "application/json");
            return response;
        }

        [HttpGet]
        public HttpResponseMessage GetCurrency(string country)
        {
            var currencyname = "";
            var currencysymbol = "";
            List<CurrencyModel> result = new List<CurrencyModel>();
            result = _Company.GetCurrencyByCountry(country).ToList();
            if (result != null && result.Count > 0)
            {
                var JSONresult = JsonConvert.SerializeObject(result, Formatting.Indented);
                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(JSONresult.ToString(), Encoding.UTF8, "application/json");
                return response;
            }
            else
            {
                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent("{\"ErrorDetails\":\"Currency details not found\"}", Encoding.UTF8, "application/json");
                return response;
            }
        }

        [HttpGet]
        public HttpResponseMessage GetStates()
        {
            long companyId;
            var identity = (ClaimsIdentity)User.Identity;
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());
            List<StateCodeViewModel> result = new List<StateCodeViewModel>();
            result = _Company.GetStates(companyId).ToList();
            if (result != null && result.Count > 0)
            {
                var JSONresult = JsonConvert.SerializeObject(result, Formatting.Indented);
                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent(JSONresult.ToString(), Encoding.UTF8, "application/json");
                return response;
            }
            else
            {
                var response = Request.CreateResponse(HttpStatusCode.OK);
                response.Content = new StringContent("{\"ErrorDetails\":\"State details not found\"}", Encoding.UTF8, "application/json");
                return response;
            }
        }
        //Added by Rupesh G on 27062024 for Country&Currency API End
        #endregion

        #region Organization List API
        //Added by hamraj for Organization List api start
        [HttpGet]
        public HttpResponseMessage GetOrganizations()
        {
            try
            {
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                var organizations = (List<ViewModels.Organization.OrganizationDetailsModelAPI>)null;
                organizations = _Company.GetOrganizationsList(companyId).OrderBy(x => x.OrganizationID).ToList();
                if (organizations.Count() == 0)
                {
                    var response1 = Request.CreateResponse(HttpStatusCode.OK);
                    response1.Content = new StringContent("{\"ErrorDetails\":\"Organization details not found\"}", Encoding.UTF8, "application/json");
                    return response1;
                }
                var orgdetails = organizations.Select(x => new { OrganizationId = x.OrganizationID, OrganizationName = x.OrganizationName, ParentId = x.ParentID, OrganizationKnownAs = x.OrganizationKnownAs, OrganizationTypeId = x.OrganizationType, OrganizationType = new OrganizationLookups().GetOrgTypes().Where(y => y.Value == x.OrganizationType).Select(y => y.Text).FirstOrDefault(), OrganizationDomain = x.OrgDomain, x.PanNumber, x.AddressLine1, x.AddressLine2, x.City, x.State, x.CountryID, x.CountryName, ZipCode = x.Zip, x.OrganizationEmail, x.OrganizationPhone, x.Website, x.CurrencyId, x.Currency, x.CurrencySymbol });
                var response = new
                {
                    organizations = orgdetails
                };
                return Request.CreateResponse(HttpStatusCode.OK, response, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, InternalServerError(ex), Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetOrganizations(long orgId)
        {
            try
            {
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                var organizations = (List<ViewModels.Organization.OrganizationDetailsModelAPI>)null;
                organizations = _Company.GetOrganizationsList(companyId).Where(x => x.OrganizationID == orgId).OrderBy(x => x.OrganizationID).ToList();
                if (organizations.Count() == 0)
                {
                    var response1 = Request.CreateResponse(HttpStatusCode.OK);
                    response1.Content = new StringContent("{\"ErrorDetails\":\"Organization details not found\"}", Encoding.UTF8, "application/json");
                    return response1;
                }
                var orgdetails = organizations.Select(x => new { OrganizationId = x.OrganizationID, OrganizationName = x.OrganizationName, ParentId = x.ParentID, OrganizationKnownAs = x.OrganizationKnownAs, OrganizationTypeId = x.OrganizationType, OrganizationType = new OrganizationLookups().GetOrgTypes().Where(y => y.Value == x.OrganizationType).Select(y => y.Text).FirstOrDefault(), OrganizationDomain = x.OrgDomain, x.PanNumber, x.AddressLine1, x.AddressLine2, x.City, x.State, x.CountryID, x.CountryName, ZipCode = x.Zip, x.OrganizationEmail, x.OrganizationPhone, x.Website, x.CurrencyId, x.Currency, x.CurrencySymbol });
                var response = new
                {
                    organizations = orgdetails
                };
                return Request.CreateResponse(HttpStatusCode.OK, response, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, InternalServerError(ex), Configuration.Formatters.JsonFormatter);
            }
        }
        //Added by hamraj for Organization List api end
        #endregion

        #region Company Hierarchy List API
        //Added By Rutik RG on 27062024 for API Ticket Start >>
        [HttpGet]
        public HttpResponseMessage GetHeirarchyDetails()
        {
            try
            {
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                var JsonList = GetHierarchyBranchDetails_API(companyId, userId);//Creates the Json in var Json List
                var TrimmedJsonList = JsonList.Select(item => new { id = item.id, text = item.text, parent = item.parent, type = item.type, branchId = item.branchId, orginalId = item.originalId }).ToList();//Trims the state column from Json
                return Request.CreateResponse(HttpStatusCode.OK, TrimmedJsonList, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, InternalServerError(ex), Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetHeirarchyDetails(long BranchId)
        {
            try
            {
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                bool status = true;
                string message = "";
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                var JsonList = _Company.GetCompanyHierarchyDetailsByIdAPI(companyId, BranchId);
                var LevelId = JsonList.Where(x => x.TypeID == 104).Count();
                if (LevelId > 0)
                {
                    var TrimmedJsonList = JsonList.Where(x => x.BranchId == BranchId).Select(item => new { BranchId = item.BranchId, BranchName = item.Name, BranchCode = item.Code, ParentId = item.ParentId, TypeId = item.TypeID, PAN = item.PanNo, GSTIN = item.GSTN, Address = item.Address, State = item.State, City = item.City, ZipCode = item.ZipCode, EmailId = item.EmailAddress, MobileNo = item.MobileNo }).ToList();//Trims the state column from Json
                    if (TrimmedJsonList.Count > 0)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, TrimmedJsonList, Configuration.Formatters.JsonFormatter);
                    }
                    else
                    {
                        status = false;
                        message = "No record found for Company Hierarchy Id (" + BranchId + ") Or May Be InActive";
                        return Request.CreateResponse(HttpStatusCode.NotFound, new { status, message }, Configuration.Formatters.JsonFormatter);
                    }
                }
                else
                {
                    var TrimmedJsonList = JsonList.Where(x => x.BranchId == BranchId).Select(item => new { BranchId = item.BranchId, BranchName = item.Name, BranchCode = item.Code, ParentId = item.ParentId, TypeId = item.TypeID }).ToList();//Trims the state column from Json
                    if (TrimmedJsonList.Count > 0)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, TrimmedJsonList, Configuration.Formatters.JsonFormatter);
                    }
                    else
                    {
                        status = false;
                        message = "No record found for Company Hierarchy Id (" + BranchId + ") Or May Be InActive";
                        return Request.CreateResponse(HttpStatusCode.NotFound, new { status, message }, Configuration.Formatters.JsonFormatter);
                    }
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, InternalServerError(ex), Configuration.Formatters.JsonFormatter);
            }
        }

        public List<TreeData> GetHierarchyBranchDetails_API(long companyId, long userId)
        {
            string CompanyName; int? roleTypeId = 100;
            List<HeirarchyLookUp> CompanyList = new List<HeirarchyLookUp>();
            CompanyList = _Company.GetCompanyDetails();
            roleTypeId = _baseInterface.ICompany.GetUserRoleType(userId);
            CompanyName = CompanyList.Where(x => x.Id == companyId).FirstOrDefault().LevelName;
            List<TreeData> data = new List<TreeData>();
            SingleTon singleton = SingleTon.GetInstance;
            var details = new List<BranchViewModel>();
            if (roleTypeId == 100)
                details = _Company.GetBrachLevelDetails(0, companyId);
            else
                details = _Company.GetBrachLevelDetails(0, companyId, userId);
            var levelType = _Company.GetHierarchyLevelscount(companyId).BranchLevelType;
            var companyCode = _Company.GetOrganizationDetailsByCompId(companyId).OrganizationKnownAs;
            //var levelnameslist = singleton.GetHierarchyMasterData().HierarchyBranchList;
            data.Add(new TreeData { id = "0", text = CompanyName + "(" + companyCode + ")", parent = "#", type = "99" });
            foreach (var item in details)
                data.Add(new TreeData { id = item.BranchId.ToString(), text = (string.IsNullOrEmpty(item.Code)) ? item.Name : item.Name + "(" + item.Code + ")", parent = item.ParentId.ToString(), type = item.TypeID.ToString() });
            return data;
        }
        //Added By Rutik RG on 27062024 for API End <<
        #endregion

        #region AssetLocation List API
        //Added By Rutik RG on 27062024 for API Start >>
        [HttpGet]
        public HttpResponseMessage GetAssetLocationDetails(string branchName)
        {
            try
            {
                int? roleTypeId = 100;
                long? cwipBranchId = null;
                string selectedValue = null, message = "";
                long companyId, userId, BranchId;
                string username;
                var identity = (ClaimsIdentity)User.Identity;
                username = identity.Claims.FirstOrDefault(c => c.Type == "username").Value.ToString();
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                roleTypeId = _baseInterface.ICompany.GetUserRoleType(userId);
                List<BranchViewModel> branch = new List<BranchViewModel>();
                branch = _baseInterface.ICompany.GetBranchesByUserID(userId, companyId, roleTypeId);
                if (branchName == "All")
                {
                    BranchId = 0;
                }
                else
                {
                    branch = branch.Where(x => x.BranchName == branchName).ToList();
                    if (branch.Count() == 0)
                    {
                        message = "User '" + username + "' is not having access to branch '" + branchName + "'. Please enter valid branch name. ";
                        return Request.CreateResponse(HttpStatusCode.NotFound, message, Configuration.Formatters.JsonFormatter);
                    }
                    BranchId = branch.FirstOrDefault().BranchId;
                }
                //branch = branch.Where(x => x.BranchName == branchName).ToList();
                //long branchId = branch.FirstOrDefault().BranchId;
                //string branchName = branch.FirstOrDefault().BranchName;
                var JsonList = GetHierarchLocationDetails_API(companyId, userId, branchName, BranchId, roleTypeId, cwipBranchId, selectedValue); //Trims the state column from Json
                var TrimmedJsonList = JsonList.Select(item => new
                {
                    id = item.id,
                    text = item.text,
                    parent = item.parent,
                    type = item.type,
                    branchId = item.branchId,
                    orginalId = item.originalId
                }).ToList(); //Trims the state column from Json
                return Request.CreateResponse(HttpStatusCode.OK, TrimmedJsonList, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, InternalServerError(ex), Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetAssetLocationDetails(string branchName, long locationid)
        {
            try
            {
                int? roleTypeId = 100;
                long? cwipBranchId = null;
                string selectedValue = null;
                long companyId, userId, BranchId;
                string username;
                bool status = true;
                string message = "";
                var identity = (ClaimsIdentity)User.Identity;
                username = identity.Claims.FirstOrDefault(c => c.Type == "username").Value.ToString();
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                roleTypeId = _baseInterface.ICompany.GetUserRoleType(userId);
                List<BranchViewModel> branch = new List<BranchViewModel>();
                branch = _baseInterface.ICompany.GetBranchesByUserID(userId, companyId, roleTypeId);
                if (branchName == "All")
                {
                    BranchId = 0;
                }
                else
                {
                    branch = branch.Where(x => x.BranchName == branchName).ToList();
                    if (branch.Count() == 0)
                    {
                        message = "User '" + username + "' is not having access to branch '" + branchName + "'. Please enter valid branch name. ";
                        return Request.CreateResponse(HttpStatusCode.NotFound, message, Configuration.Formatters.JsonFormatter);
                    }
                    BranchId = branch.FirstOrDefault().BranchId;
                }
                var JsonList = GetHierarchLocationDetails_API(companyId, userId, branchName, BranchId, roleTypeId, cwipBranchId, selectedValue); //Trims the state column from Json
                var TrimmedJsonList = JsonList.Where(x => x.originalId == locationid).Select(item => new { id = item.id, text = item.text, parent = item.parent, type = item.type, branchId = item.branchId, orginalId = item.originalId }).ToList(); //Trims the state column from Json
                if (TrimmedJsonList.Count > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, TrimmedJsonList, Configuration.Formatters.JsonFormatter);
                }
                else
                {
                    status = false;
                    message = "No record found for Asset Location Id (" + locationid + ") Or May Be InActive";
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { status, message }, Configuration.Formatters.JsonFormatter);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, InternalServerError(ex), Configuration.Formatters.JsonFormatter);
            }
        }

        public List<TreeData> GetHierarchLocationDetails_API(long companyId, long userId, string branchName, long branchId, int? roleTypeId, long? cwipBranchId = null, string selectedValue = null)
        {
            string CompanyName;
            List<HeirarchyLookUp> CompanyList = new List<HeirarchyLookUp>();
            CompanyList = _Company.GetCompanyDetails();
            CompanyName = CompanyList.Where(x => x.Id == companyId).FirstOrDefault().LevelName;
            List<TreeData> data = new List<TreeData>();
            List<long> LocSelectedValues = new List<long>();
            if (selectedValue != null && selectedValue != "")
                LocSelectedValues.AddRange(selectedValue.Split(',').Select(x => Convert.ToInt64(x)).ToList());
            cwipBranchId = cwipBranchId ?? 0;
            if (branchId > 0)
            {
                var details = _Company.GetLocationLevelDetails(0, companyId, branchId);
                data.Add(new TreeData { id = "loc_0", text = (branchName == null ? CompanyName : branchName), parent = "#", type = "99", branchId = "0" });
                foreach (var item in details)
                    data.Add(new TreeData { id = "loc_" + item.Id.ToString(), text = (string.IsNullOrEmpty(item.Code)) ? item.LevelName : item.LevelName + "(" + item.Code + ")", parent = (string.IsNullOrEmpty(item.parentId.ToString()) || item.parentId.ToString() == "0") ? "loc_0" : "loc_" + item.parentId.ToString(), type = item.LevelType.ToString(), branchId = item.branchId.ToString(), originalId = item.Id, state = (LocSelectedValues.Where(x => x == item.Id).Count() > 0 ? new state { selected = true, opened = true } : new state { selected = false, opened = false }) });
            }
            else
            {
                var BranchDetails = new List<BranchViewModel>();
                var levelType = _Company.GetHierarchyLevelscount(companyId).BranchLevelType;
                if (roleTypeId != 100)
                {
                    if (cwipBranchId != 0 && cwipBranchId != null)
                        BranchDetails = _Company.GetBrachLevelDetails(0, companyId, userId).Where(x => x.TypeID == levelType && x.BranchId == cwipBranchId).ToList();
                    else
                        BranchDetails = _Company.GetBrachLevelDetails(0, companyId, userId).Where(x => x.TypeID == levelType).ToList();
                }
                else
                    BranchDetails = _Company.GetBrachLevelDetails(0, companyId).Where(x => x.TypeID == levelType).ToList();
                List<long> branchIds = new List<long>();
                if (cwipBranchId != 0 || cwipBranchId != null)
                    branchIds = BranchDetails.Select(x => x.BranchId).ToList();
                else
                    branchIds = BranchDetails.Where(x => x.BranchId == cwipBranchId).Select(x => x.BranchId).ToList();
                var details = _Company.GetHierarchyLocationsBasedonBranchIds(companyId, branchIds);
                foreach (var item in BranchDetails)
                    data.Add(new TreeData { id = "branch_" + item.BranchId.ToString(), text = item.Name, parent = "#", type = "98", branchId = "0", originalId = item.BranchId });
                foreach (var item in details)
                    data.Add(new TreeData { id = "loc_" + item.Id.ToString(), text = (string.IsNullOrEmpty(item.Code)) ? item.LevelName : item.LevelName + "(" + item.Code + ")", parent = ((item.parentId == 0 || string.IsNullOrEmpty(item.parentId.ToString()) || item.branchId.ToString().Contains("branch_")) ? "branch_" + item.branchId.ToString() : "loc_" + item.parentId.ToString()).ToString(), type = item.LevelType.ToString(), branchId = item.branchId.ToString(), originalId = item.Id, state = (LocSelectedValues.Where(x => x == item.Id).Count() > 0 ? new state { selected = true, opened = true } : new state { selected = false, opened = false }) });
            }
            return data;
        }

        
        //Added By Rutik RG on 27062024 for API End <<
        #endregion

        #region Department List API
        //Added By Rutik RG on 27062024 for API Start >>
        [HttpGet]
        public HttpResponseMessage GetDepartmentDetails()
        {
            try
            {
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                bool transactionLevel = false;
                string selectedValue = null;
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                var JsonList = GetHierarchdepartmentDetails_API(companyId, userId, transactionLevel, selectedValue);//Creates the Json in var Json List
                var TrimmedJsonList = JsonList.Select(item => new { id = item.id, text = item.text, parent = item.parent, type = item.type, branchId = item.branchId, orginalId = item.originalId }).ToList();//Trims the state column from Json
                return Request.CreateResponse(HttpStatusCode.OK, TrimmedJsonList, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, InternalServerError(ex), Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetDepartmentDetails(long deptid) //To get Specific Record [Work as Edit] 
        {
            try
            {
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                bool transactionLevel = false;
                string selectedValue = null;
                bool status = true;
                string message = "";
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                var JsonList = GetHierarchdepartmentDetails_API(companyId, userId, transactionLevel, selectedValue);//Creates the Json in var Json List
                var TrimmedJsonList = JsonList.Where(x => x.id == deptid.ToString().Trim()).Select(item => new { id = item.id, text = item.text, parent = item.parent, type = item.type, branchId = item.branchId, orginalId = item.originalId }).ToList();//Trims the state column from Json
                if (TrimmedJsonList.Count > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, TrimmedJsonList, Configuration.Formatters.JsonFormatter);
                }
                else
                {
                    status = false;
                    message = "No record found for Department Id (" + deptid + ") Or May Be InActive";
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { status, message }, Configuration.Formatters.JsonFormatter);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, ex.Message, Configuration.Formatters.JsonFormatter);
            }
        }

        public List<TreeData> GetHierarchdepartmentDetails_API(long companyId, long userId, bool transactionLevel = true, string selectedValue = null)
        {
            string CompanyName;
            int? roleTypeId = 100;
            roleTypeId = _baseInterface.ICompany.GetUserRoleType(userId);
            List<HeirarchyLookUp> CompanyList = new List<HeirarchyLookUp>();
            CompanyList = _Company.GetCompanyDetails();
            CompanyName = CompanyList.Where(x => x.Id == companyId).FirstOrDefault().LevelName;
            var details = _Company.GetDepartmentLevelDetails(0, companyId, userId, roleTypeId, transactionLevel);
            List<TreeData> data = new List<TreeData>();
            var companyCode = _Company.GetOrganizationDetailsByCompId(companyId).OrganizationKnownAs;
            List<long> departmentSelectedValues = new List<long>();
            if (selectedValue != null && selectedValue != "")
                departmentSelectedValues.AddRange(selectedValue.Split(',').Select(x => Convert.ToInt64(x)).ToList());
            data.Add(new TreeData { id = "0", text = CompanyName + "(" + companyCode + ")", parent = "#", type = "99" });
            foreach (var item in details)
                data.Add(new TreeData { id = item.Id.ToString(), text = item.LevelName + "(" + item.Code + ")", parent = item.parentId.ToString(), type = item.LevelType.ToString() });
            return data;
        }
        //Added By Rutik RG on 27062024 for API End <<
        #endregion

        #region CostCenter List API
        //Added By Rutik RG on 27062024 for API Start >>
        [HttpGet]
        public HttpResponseMessage GetCostCenterDetails()
        {
            try
            {
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                string selectedValue = null;
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                var JsonList = GetHierarchCostCenterDetails_API(companyId, selectedValue); //Trims the state column from Json
                var TrimmedJsonList = JsonList.Select(item => new { id = item.id, text = item.text, parent = item.parent, type = item.type, branchId = item.branchId, orginalId = item.originalId }).ToList(); //Trims the state column from Json
                return Request.CreateResponse(HttpStatusCode.OK, TrimmedJsonList, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, InternalServerError(ex), Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetCostCenterDetails(long costcentid)
        {
            try
            {
                long costid = costcentid;
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                string selectedValue = null;
                bool status = true;
                string message = "";
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                var JsonList = GetHierarchCostCenterDetails_API(companyId, selectedValue); //Trims the state column from Json
                var TrimmedJsonList = JsonList.Where(x => x.id == costid.ToString().Trim()).Select(item => new { id = item.id, text = item.text, parent = item.parent, type = item.type, branchId = item.branchId, orginalId = item.originalId }).ToList(); //Trims the state column from Json
                if (TrimmedJsonList.Count > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, TrimmedJsonList, Configuration.Formatters.JsonFormatter);
                }
                else
                {
                    status = false;
                    message = "No record found for Cost Center Id (" + costid + ") Or May Be InActive";
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { status, message }, Configuration.Formatters.JsonFormatter);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, InternalServerError(ex), Configuration.Formatters.JsonFormatter);
            }
        }

        public List<TreeData> GetHierarchCostCenterDetails_API(long companyId, string selectedValue = null)
        {
            string CompanyName;
            var details = _Company.getCostCenterLevelDetails(0, companyId);
            List<HeirarchyLookUp> CompanyList = new List<HeirarchyLookUp>();
            CompanyList = _Company.GetCompanyDetails();
            CompanyName = CompanyList.Where(x => x.Id == companyId).FirstOrDefault().LevelName;
            List<TreeData> data = new List<TreeData>();
            List<long> costCenterSelectedValues = new List<long>();
            if (selectedValue != null && selectedValue != "")
                costCenterSelectedValues.AddRange(selectedValue.Split(',').Select(x => Convert.ToInt64(x)).ToList());
            var companyCode = _Company.GetOrganizationDetailsByCompId(companyId).OrganizationKnownAs;
            data.Add(new TreeData { id = "0", text = CompanyName + "(" + companyCode + ")", parent = "#", type = "99" });
            foreach (var item in details)
                data.Add(new TreeData { id = item.Id.ToString(), text = item.LevelName + "(" + item.Code + ")", parent = item.parentId.ToString(), type = item.LevelType.ToString() });
            return data;
        }
        //Added By Rutik RG on 27062024 for API End <<
        #endregion

        #region Vendor List API
        //Get Vendor Details added by hamraj start
        [HttpGet]
        public HttpResponseMessage GetVendorDetails()
        {
            try
            {
                long companyId, userId, BranchId = 0;
                var identity = (ClaimsIdentity)User.Identity;
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);

                List<VendorViewModel> list = _vendor.GetVendors(companyId).Where(x => x.VendorTypeID != 110).Distinct().ToList(); ;

                var filteredVendors = list.Select(v => new
                {
                    VendorID = v.VendorID,
                    VendorName = v.VendorName,
                    VendorTypeID = v.VendorTypeID,
                    VendorType = string.IsNullOrEmpty(v.VendorType) ? null : v.VendorType,
                    VendorCode = v.VendorCode,
                    PanNo = string.IsNullOrEmpty(v.PanNo) ? null : v.PanNo,
                    GSTIN = string.IsNullOrEmpty(v.GSTIN) ? null : v.GSTIN,
                    AddressID = v.AddressID == 0 ? (long?)null : v.AddressID,
                    Address = string.IsNullOrEmpty(v.Address) ? null : v.Address,
                    City = string.IsNullOrEmpty(v.City) ? null : v.City,
                    State = string.IsNullOrEmpty(v.State) ? null : v.State,
                    Country = string.IsNullOrEmpty(v.Country) ? null : v.Country,
                    ZipCode = string.IsNullOrEmpty(v.ZipCode) ? null : v.ZipCode,
                    Phone = string.IsNullOrEmpty(v.Phone) ? null : v.Phone,
                    Mobile = string.IsNullOrEmpty(v.Mobile) ? null : v.Mobile,
                    VendorEmailId = string.IsNullOrEmpty(v.VendorEmailId) ? null : v.VendorEmailId,
                    Description = string.IsNullOrEmpty(v.Description) ? null : v.Description,
                    ContactPerson = string.IsNullOrEmpty(v.ContactPerson) ? null : v.ContactPerson
                }).ToList();

                //var a= JsonConvert.SerializeObject(filteredVendors, Formatting.Indented).ToString();
                //a.Replace("VendorName", "Vendor Name");
                //filteredVendors[0]=

                var response = new
                {
                    Vendors = filteredVendors
                    //vendors = a
                };

                return Request.CreateResponse(HttpStatusCode.OK, response, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetVendorDetails(long VendorID)
        {
            try
            {
                long companyId, userId, BranchId = 0;
                var identity = (ClaimsIdentity)User.Identity;
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                bool status = true;
                string message = "";
                List<VendorViewModel> list = _vendor.GetVendors(companyId).Where(x => x.VendorTypeID != 110).Distinct().ToList();
                var JsonList = list;
                var TrimmedJsonList = JsonList.Where(x => x.VendorID == VendorID).Select(item => new {
                    VendorId = item.VendorID,
                    VendorName = item.VendorName,
                    VendorTypeId = item.VendorTypeID,
                    VendorType = item.VendorType,
                    VendorCode = item.VendorCode,
                    PAN = item.PanNo,
                    GSTIN = item.GSTIN,
                    AddressId = item.AddressID,
                    Address = item.Address,
                    City = item.City,
                    State = item.State,
                    Country = item.Country,
                    ZipCode = item.ZipCode,
                    PhoneNo = item.Phone,
                    MobileNo = item.Mobile,
                    EmailId = item.VendorEmailId,
                    Description = item.Description,
                    ContactPerson = item.ContactPerson
                }).ToList();//Trims the state column from Json
                if (TrimmedJsonList.Count > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, TrimmedJsonList, Configuration.Formatters.JsonFormatter);
                }
                else
                {
                    status = false;
                    message = "No record found for Vendor Id (" + VendorID + ") Or May Be InActive";
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { status, message }, Configuration.Formatters.JsonFormatter);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, ex.Message, Configuration.Formatters.JsonFormatter);
            }
        }
        //Get Vendor Details added by hamraj end
        #endregion

        #region User List API
        //User List added by hamraj start
        [HttpGet]
        public HttpResponseMessage GetUserDetails()
        {
            try
            {
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                bool status = true;
                string message = "";

                var JsonList = _user.GetAllUsersAPI(companyId);

                var TrimmedJsonList = JsonList.Select(item => new
                {
                    UserId = item.UserID,
                    FirstName = item.FirstName,
                    LastName = item.LastName,
                    Email = item.EmailId,                     //emalid to email
                    EmployeeId = item.EmailId,                //EmployyeID to EmployeeId
                    MobileNumber = item.Mobile,               //MobileNo to MobileNumber
                    PhoneNumber = item.Phone,                 //Phone to PhoneNumber
                    UserName = item.UserName,
                    DeviceName = item.DeviceName,
                    RoleTypeId = item.RoleTypeId,
                    RoleName = item.RoleName,                  //Role to RoleName
                    DepartmentIds = item.DepartmentIds,
                    Department = item.DepartmentNames,         //DepartmentNames to Department
                    AssetCategoryIds = item.AssetCategoryIds,  //CategoriesId to AssetCategoryIds
                    Categories = item.AssetCategoryNames,      //CategoriesNames to Categories
                    BranchIds = item.BranchIds,
                    Branch = item.BranchNames,                 //BranchNames to Branch
                    Deactive = item.IsDeactive,
                    DeactiveDate = item.DeactiveDate,
                    IsServiceDesk = item.IsServiceDesk,
                }).ToList();
                if (TrimmedJsonList.Count > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, TrimmedJsonList, Configuration.Formatters.JsonFormatter);
                }
                else
                {
                    status = false;
                    message = "No record found for User Id (" + userId + ") Or May Be InActive";
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { status, message }, Configuration.Formatters.JsonFormatter);
                }

                //return Request.CreateResponse(HttpStatusCode.OK, TrimmedJsonList, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetUserDetails(long userId)
        {
            try
            {
                long companyId;//, userId;
                var identity = (ClaimsIdentity)User.Identity;
                bool status = true;
                string message = "";
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);

                var result = _user.GetAllUsersAPI(companyId);
                var JsonList = result.Where(x => x.UserID == userId);
                if (JsonList.Where(x => x.RoleName == "Root Admin").Count() > 0)
                {
                    var TrimmedJsonList = JsonList.Select(item => new
                    {
                        UserId = item.UserID,
                        FirstName = item.FirstName,
                        LastName = item.LastName,
                        Email = item.EmailId,           //emalid to email
                        EmployeeId = item.EmailId,      //EmployyeID to EmployeeId
                        MobileNumber = item.Mobile,     //MobileNo to MobileNumber
                        PhoneNumber = item.Phone,       //Phone to PhoneNumber
                        UserName = item.UserName,
                        DeviceName = item.DeviceName,
                        RoleTypeId = item.RoleTypeId,
                        RoleName = item.RoleName,       //Role to RoleName
                        Deactive = item.IsDeactive,
                        DeactiveDate = item.DeactiveDate,
                        IsServiceDesk = item.IsServiceDesk,
                    }).ToList();
                    if (TrimmedJsonList.Count > 0)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, TrimmedJsonList, Configuration.Formatters.JsonFormatter);
                    }
                    else
                    {
                        status = false;
                        message = "No record found for User Id (" + userId + ") Or May Be InActive";
                        return Request.CreateResponse(HttpStatusCode.NotFound, new { status, message }, Configuration.Formatters.JsonFormatter);
                    }
                }
                else
                {
                    var TrimmedJsonList = JsonList.Select(item => new
                    {
                        UserId = item.UserID,
                        FirstName = item.FirstName,
                        LastName = item.LastName,
                        Email = item.EmailId,                     //emalid to email
                        EmployeeId = item.EmailId,                //EmployyeID to EmployeeId
                        MobileNumber = item.Mobile,               //MobileNo to MobileNumber
                        PhoneNumber = item.Phone,                 //Phone to PhoneNumber
                        UserName = item.UserName,
                        DeviceName = item.DeviceName,
                        RoleTypeId = item.RoleTypeId,
                        RoleName = item.RoleName,                  //Role to RoleName
                        DepartmentIds = item.DepartmentIds,
                        Department = item.DepartmentNames,         //DepartmentNames to Department
                        AssetCategoryIds = item.AssetCategoryIds,  //CategoriesId to AssetCategoryIds
                        Categories = item.AssetCategoryNames,      //CategoriesNames to Categories
                        BranchIds = item.BranchIds,
                        Branch = item.BranchNames,                 //BranchNames to Branch
                        Deactive = item.IsDeactive,
                        DeactiveDate = item.DeactiveDate,
                        IsServiceDesk = item.IsServiceDesk,
                    }).ToList();
                    if (TrimmedJsonList.Count > 0)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, TrimmedJsonList, Configuration.Formatters.JsonFormatter);
                    }
                    else
                    {
                        status = false;
                        message = "No record found for User Id (" + userId + ") Or May Be InActive";
                        return Request.CreateResponse(HttpStatusCode.NotFound, new { status, message }, Configuration.Formatters.JsonFormatter);
                    }
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, ex.Message, Configuration.Formatters.JsonFormatter);
            }
        }
        //User List added by hamraj end
        #endregion

        #region Customer List API
        //Get customer Details added by hamraj start
        [HttpGet]
        public HttpResponseMessage GetCustomerDetails(string BranchName)
        {
            try
            {
                int? roleTypeId = 100;
                long companyId, userId, BranchId = 0;
                string username = "";
                var identity = (ClaimsIdentity)User.Identity;
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                username = identity.Claims.FirstOrDefault(c => c.Type == "username").Value.ToString();

                //var branchList = _baseInterface.ICompany.GetBranchIdByBranchName(companyId, BranchName);
                roleTypeId = _baseInterface.ICompany.GetUserRoleType(userId);
                List<BranchViewModel> branch = new List<BranchViewModel>();
                branch = _baseInterface.ICompany.GetBranchesByUserID(userId, companyId, roleTypeId);

                if (BranchName == "All")
                {
                    BranchId = 0;
                }
                else
                {
                    branch = branch.Where(x => x.BranchName == BranchName).ToList();
                    if (branch.Count() == 0)
                    {
                      var message = "User '" + username + "' is not having access to branch '" + BranchName + "'. Please enter valid branch name. ";
                        return Request.CreateResponse(HttpStatusCode.NotFound, message, Configuration.Formatters.JsonFormatter);
                    }
                    BranchId = branch.FirstOrDefault().BranchId;
                }

                //if (branchList.Count > 0)
                //{
                //    BranchId = Convert.ToInt32(branchList.FirstOrDefault().BranchId);
                //}
                //else
                //{
                //    var message = BranchName + " Branch does not exist.";
                //    var response = Request.CreateResponse(HttpStatusCode.OK);
                //    response.Content = new StringContent("{\"Message\":\"" + message + "\"}", Encoding.UTF8, "application/json");
                //    return response;
                //}

                List<VendorViewModel> list = _vendor.GetVendorsByBranchAPI(companyId, BranchId);

                var filteredCustomers = list.Select(v => new
                {
                    CustomerID = v.VendorID,
                    CustomerName = v.VendorName,
                    CustomerTypeID = v.VendorTypeID,
                    CustomerType = string.IsNullOrEmpty(v.VendorType) ? null : v.VendorType,
                    PAN = string.IsNullOrEmpty(v.PanNo) ? null : v.PanNo,
                    GSTIN = string.IsNullOrEmpty(v.GSTIN) ? null : v.GSTIN,
                    AddOnAddressID = v.AddressID == 0 ? (long?)null : v.AddressID,
                    AddOnAddress = string.IsNullOrEmpty(v.Address) ? null : v.Address,
                    City = string.IsNullOrEmpty(v.City) ? null : v.City,
                    State = string.IsNullOrEmpty(v.State) ? null : v.State,
                    Country = string.IsNullOrEmpty(v.Country) ? null : v.Country,
                    ZipCode = string.IsNullOrEmpty(v.ZipCode) ? null : v.ZipCode,
                    PhoneNo = string.IsNullOrEmpty(v.Phone) ? null : v.Phone,
                    MobileNo = string.IsNullOrEmpty(v.Mobile) ? null : v.Mobile,
                    EmailId = string.IsNullOrEmpty(v.VendorEmailId) ? null : v.VendorEmailId,
                    MainLocationId = v.MainLocationId,
                    MainLocation = string.IsNullOrEmpty(v.MainLocation) ? null : v.MainLocation,
                    SubLocationId = v.SubLocationId,
                    SubLocation = string.IsNullOrEmpty(v.SubLocation) ? null : v.SubLocation,
                    Description = string.IsNullOrEmpty(v.Description) ? null : v.Description,
                    ContactPerson = string.IsNullOrEmpty(v.ContactPerson) ? null : v.ContactPerson,
                    BillingAddress = v.BillingAddress,
                    BranchName = v.BranchNames,
                    Attachment1 = v.Attachment1,
                    Attachment2 = v.Attachment2,
                    Attachment3 = v.Attachment3

                }).ToList();

                var resp = new
                {
                    Customers = filteredCustomers
                };

                return Request.CreateResponse(HttpStatusCode.OK, resp, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpGet]
        //public HttpResponseMessage GetCustomerDetails(string BranchName, long CustomerId)
        public HttpResponseMessage GetCustomerDetails(long CustomerId)
        {
            try
            {
                int? roleTypeId = 100;
                long companyId, userId, BranchId = 0;
                string username = "";
                var identity = (ClaimsIdentity)User.Identity;
                bool status = true;
                string message1 = "";
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                username = identity.Claims.FirstOrDefault(c => c.Type == "username").Value.ToString();

                roleTypeId = _baseInterface.ICompany.GetUserRoleType(userId);
                //List<BranchViewModel> branch = new List<BranchViewModel>();
                //branch = _baseInterface.ICompany.GetBranchesByUserID(userId, companyId, roleTypeId);

                //if (BranchName == "All")
                //{
                //    BranchId = 0;
                //}
                //else
                //{
                //    branch = branch.Where(x => x.BranchName == BranchName).ToList();
                //    if (branch.Count() == 0)
                //    {
                //        var message = "User '" + username + "' is not having access to branch '" + BranchName + "'. Please enter valid branch name. ";
                //        return Request.CreateResponse(HttpStatusCode.NotFound, message, Configuration.Formatters.JsonFormatter);
                //    }
                //    BranchId = branch.FirstOrDefault().BranchId;
                //}

                //var branchList = _baseInterface.ICompany.GetBranchIdByBranchName(companyId, BranchName);

                //if (branchList.Count > 0)
                //{
                //    BranchId = Convert.ToInt32(branchList.FirstOrDefault().BranchId);
                //}
                //else
                //{
                //    var message = BranchName + " Branch does not exist.";
                //    var response = Request.CreateResponse(HttpStatusCode.OK);
                //    response.Content = new StringContent("{\"Message\":\"" + message + "\"}", Encoding.UTF8, "application/json");
                //    return response;
                //}

                List<VendorViewModel> list = _vendor.GetVendorsByBranchAPI(companyId, BranchId);
                var JsonList = list;
                var TrimmedJsonList = JsonList.Where(x => x.VendorID == CustomerId).Select(item => new
                {
                    CustomerId = item.VendorID,
                    CustomerName = item.VendorName,
                    CustomerTypeId = item.VendorTypeID,
                    CustomerType = item.VendorType,
                    PAN = item.PanNo,
                    GSTIN = item.GSTIN,
                    AddOnAddressId = item.AddressID,
                    AddOnAddress = item.Address,
                    City = item.City,
                    State = item.State,
                    Country = item.Country,
                    ZipCode = item.ZipCode,
                    PhoneNo = item.Phone,
                    MobileNo = item.Mobile,
                    EmailId = item.VendorEmailId,
                    MainLocationId = item.MainLocationId,
                    MainLocation = item.MainLocation,
                    SubLocationId = item.SubLocationId,
                    SubLocation = item.SubLocation,
                    Description = item.Description,
                    ContactPerson = item.ContactPerson,
                    BillingAddress = item.BillingAddress,
                    BranchName = item.BranchNames,
                    Attachment1 = item.Attachment1,
                    Attachment2 = item.Attachment2,
                    Attachment3 = item.Attachment3


                }).ToList();//Trims the state column from Json
                if (TrimmedJsonList.Count > 0)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, TrimmedJsonList, Configuration.Formatters.JsonFormatter);
                }
                else
                {
                    status = false;
                    message1 = "No record found for Customer Id (" + CustomerId + ") Or May Be InActive";
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { status, message1 }, Configuration.Formatters.JsonFormatter);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, ex.Message, Configuration.Formatters.JsonFormatter);
            }
        }
        //Get customer Details added by hamraj end
        #endregion

        #region AssetCategory List API
        //Added by Priyanka B on 29062024 for AssetCategoryAPI Start
        [HttpGet]
        public HttpResponseMessage GetAssetCategories()
        {
            try
            {
                long companyId;
                var identity = (ClaimsIdentity)User.Identity;
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());
                List<ViewModels.AssetCategory.AssetCategoryViewModel> categories = _assetCategoryApi.GetAssetCategoryList(companyId);
                return Request.CreateResponse(HttpStatusCode.OK, categories, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
        }
        //Added by Priyanka B on 29062024 for AssetCategoryAPI End
        #endregion
        #endregion


        #region All Delete API

        #region Company Hierarchy Delete API
        //Added By Rutik RG on 27062024 for API Start >>
        [HttpPost]
        public HttpResponseMessage DeleteHeirachyDetails(string Name, string Code)
        {
            try
            {
                bool status = true;
                string message = "";
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                string HType = "CompanyHeirachy";
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                var Company_details = _Company.GetLevelDetailssByNameCode(companyId, HType, Name, Code);
                if (Company_details.Rows.Count == 0 || Convert.ToBoolean(Company_details.Rows[0]["IsActive"]) == false)
                {
                    status = false;
                    message = "Record Not Found Or May Be Inactive.";
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { status, message }, Configuration.Formatters.JsonFormatter);
                    //return Request.CreateResponse(HttpStatusCode.NotFound, "Record Not Found Or May Be Inactive.", Configuration.Formatters.JsonFormatter);
                }
                //long type = Convert.ToInt32(Company_details.Rows[0]["LocationTypeID"].ToString().Trim());
                //long locationId = Convert.ToInt32(Company_details.Rows[0]["LocationID"].ToString().Trim());
                long BranchId = Convert.ToInt32(Company_details.Rows[0]["BranchId"].ToString().Trim());
                var DeleteBranch = _Company.DeleteBranchDetailsById(BranchId, companyId, Name, userId);
                status = DeleteBranch.Item1;
                message = DeleteBranch.Item2;
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message }, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, ex.Message, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        public HttpResponseMessage DeleteHeirachyDetailsById(long Id)
        {
            try
            {
                bool status = true;
                string message = "";
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                string HType = "CompanyHeirachy";
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);

                var Company_details = _Company.GetLevelDetailssById(companyId, HType, Id);
                if (Company_details.Rows.Count == 0 || Convert.ToBoolean(Company_details.Rows[0]["IsActive"]) == false)
                {
                    status = false;
                    message = "Record Not Found Or May Be Inactive.";
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { status, message }, Configuration.Formatters.JsonFormatter);
                }

                string Name = "";  // Company_details.Rows[0]["Name"].ToString().Trim();
                int typeid = Convert.ToInt32(Company_details.Rows[0]["TypeID"].ToString().Trim());
                Name = GetHierarchyLevelNameByLevelId(100, typeid, companyId).LevelName;
                var DeleteBranch = _Company.DeleteBranchDetailsById(Id, companyId, Name, userId);
                status = DeleteBranch.Item1;
                message = DeleteBranch.Item2;
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message }, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, ex.Message, Configuration.Formatters.JsonFormatter);
            }
        }

        public HeirarchyLookUp GetHierarchyLevelNameByLevelId(long? type, long? levelId,long companyId)
        {
            var appInstance = SingleTon.GetInstance;
            GenericLookUp obj = new GenericLookUp();
            var hierarchylevelData = new List<HeirarchyLookUp>();
            string levelname = "";
            string leaflevelname = "";
            //long companyId = companyId;
            obj.Field1 = type ?? 0;
            obj.Field2 = companyId;
            var hierarchyDatalist = appInstance.GetHierarchyDynamicData(obj);
            var hierarchyData = hierarchyDatalist.HierarchyDynamicList.Where(x => x.LevelType == levelId).Select(x => x.LevelName);
            if (type == 101)
            {
                obj.Field1 = 100;
                obj.Field2 = companyId;
                leaflevelname = GetcompanyHierarchyLevelName(companyId);
            }
            if (levelId == 99)
                levelname = "Company";
            else
                hierarchylevelData = hierarchyDatalist.HierarchyDynamicList.Where(x => x.LevelType <= levelId).ToList();
            foreach (var item in hierarchylevelData)
            {
                if (string.IsNullOrEmpty(levelname))
                {
                    levelname = (!string.IsNullOrEmpty(leaflevelname) ? leaflevelname : "Company") + "->" + item.LevelName;
                }
                else
                    levelname = levelname + "->" + item.LevelName;
            }
            //var hierarchylevelData1 = new List<HeirarchyLookUp> { Leve };
            HeirarchyLookUp hierarchylevelData1 = new HeirarchyLookUp();
            hierarchylevelData1.LevelName = hierarchyData.FirstOrDefault().ToString();
            hierarchylevelData1.Desc = levelname;
            //return Json(new { Name = hierarchyData, fullName = levelname }, JsonRequestBehavior.AllowGet);
            return hierarchylevelData1;
        }

        public string GetcompanyHierarchyLevelName(long companyId)
        {
            var appInstance = SingleTon.GetInstance;
            GenericLookUp obj = new GenericLookUp();
            var hierarchylevelData = new List<HeirarchyLookUp>();
            string levelname = "";
            string leaflevelname = "";
            obj.Field1 = 100;
            obj.Field2 = companyId;
            var hierarchyDatalist = appInstance.GetHierarchyDynamicData(obj).HierarchyDynamicList.ToList();
            if (hierarchyDatalist.Count > 0)
            {
                foreach (var item in hierarchyDatalist)
                {
                    if (string.IsNullOrEmpty(levelname))
                    {
                        levelname = "Company" + "->" + item.LevelName;
                    }
                    else
                        levelname = levelname + "->" + item.LevelName;
                }
            }
            return levelname;
        }
        //Added By Rutik RG on 27062024 for API End <<
        #endregion

        #region Department Delete API
        //Added By Rutik RG on 27062024 for API Start >>
        [HttpPost]
        public HttpResponseMessage DeleteDepartmentDetails(string Name, string Code)
        {
            try
            {
                bool status = true;
                string message = "";
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                string HType = "Department";
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                var Dept_details = _Company.GetLevelDetailssByNameCode(companyId, HType, Name, Code);
                if (Dept_details.Rows.Count == 0 || Convert.ToBoolean(Dept_details.Rows[0]["IsActive"]) == false)
                {
                    status = false;
                    message = "Record Not Found Or May Be Inactive.";
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { status, message }, Configuration.Formatters.JsonFormatter);
                }
                long Levelid = Convert.ToInt32(Dept_details.Rows[0]["TypeID"].ToString().Trim());
                long Departid = Convert.ToInt32(Dept_details.Rows[0]["DepartmentID"].ToString().Trim());
                var DeleteDepartment = _Company.DeleteDepartmentHierarchy(Departid, companyId, Name);
                status = DeleteDepartment.Item1;
                message = DeleteDepartment.Item2;
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message }, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, ex.Message, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        public HttpResponseMessage DeleteDepartmentDetailsById(long Id)
        {
            try
            {
                bool status = true;
                string message = "";
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                string HType = "Department";
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                var Dept_details = _Company.GetLevelDetailssById(companyId, HType, Id);
                if (Dept_details.Rows.Count == 0 || Convert.ToBoolean(Dept_details.Rows[0]["IsActive"]) == false)
                {
                    status = false;
                    message = "Record Not Found Or May Be Inactive.";
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { status, message }, Configuration.Formatters.JsonFormatter);
                }
                string Name = Dept_details.Rows[0]["Name"].ToString().Trim();

                var DeleteDepartment = _Company.DeleteDepartmentHierarchy(Id, companyId, Name);
                status = DeleteDepartment.Item1;
                message = DeleteDepartment.Item2;
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message }, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, ex.Message, Configuration.Formatters.JsonFormatter);
            }
        }
        //Added By Rutik RG on 27062024 for API End <<
        #endregion

        #region CostCenter Delete API
        //Added By Rutik RG on 27062024 for API Start >>
        [HttpPost]
        public HttpResponseMessage DeleteCostCenterDetails(string Name, string Code)
        {
            try
            {
                bool status = true;
                string message = "";
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                string HType = "CostCenter";
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                var CostCenter_details = _Company.GetLevelDetailssByNameCode(companyId, HType, Name, Code);
                if (CostCenter_details.Rows.Count == 0 || Convert.ToBoolean(CostCenter_details.Rows[0]["IsActive"]) == false)
                {
                    status = false;
                    message = "Record Not Found Or May Be Inactive.";
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { status, message }, Configuration.Formatters.JsonFormatter);
                }
                long Levelid = Convert.ToInt32(CostCenter_details.Rows[0]["TypeID"].ToString().Trim());
                long CostCenterId = Convert.ToInt32(CostCenter_details.Rows[0]["CostCenterID"].ToString().Trim());
                var DeleteCostCenter = _Company.DeleteCostCenterHierarchy(CostCenterId, companyId, Name);
                status = DeleteCostCenter.Item1;
                message = DeleteCostCenter.Item2;
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message }, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, ex.Message, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        public HttpResponseMessage DeleteCostCenterDetailsById(long Id)
        {
            try
            {
                bool status = true;
                string message = "";
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                string HType = "CostCenter";
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                var CostCenter_details = _Company.GetLevelDetailssById(companyId, HType, Id);
                if (CostCenter_details.Rows.Count == 0 || Convert.ToBoolean(CostCenter_details.Rows[0]["IsActive"]) == false)
                {
                    status = false;
                    message = "Record Not Found Or May Be Inactive.";
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { status, message }, Configuration.Formatters.JsonFormatter);
                }

                string Name = CostCenter_details.Rows[0]["Name"].ToString().Trim();
                var DeleteCostCenter = _Company.DeleteCostCenterHierarchy(Id, companyId, Name);
                status = DeleteCostCenter.Item1;
                message = DeleteCostCenter.Item2;
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message }, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, ex.Message, Configuration.Formatters.JsonFormatter);
            }
        }
        //Added By Rutik RG on 27062024 for API End <<
        #endregion

        #region AssetLocation Delete API
        //Added By Rutik RG on 27062024 for API Start >>        
        [HttpPost]
        public HttpResponseMessage DeleteAssetLocationDetails(string BranchName, string BranchCode, string LocationName, string LocationCode)
        {
            try
            {
                bool status = true;
                string message = "";
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                string HType = "AssetLocation";
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                var Location_details = _Company.GetLevelDetailssByNameCode(companyId, HType, LocationName, LocationCode, BranchName, BranchCode);
                if (Location_details.Rows.Count == 0 || Convert.ToBoolean(Location_details.Rows[0]["IsActive"]) == false)
                {
                    status = false;
                    message = "Record Not Found Or May Be Inactive.";
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { status, message }, Configuration.Formatters.JsonFormatter);
                }
                long type = Convert.ToInt32(Location_details.Rows[0]["LocationTypeID"].ToString().Trim());
                long locationId = Convert.ToInt32(Location_details.Rows[0]["LocationID"].ToString().Trim());
                long BranchId = Convert.ToInt32(Location_details.Rows[0]["BranchId"].ToString().Trim());
                var DeleteLocation = _Company.DeleteLocationHierarchy(locationId, companyId, type, BranchId, LocationName);
                status = DeleteLocation.Item1;
                message = DeleteLocation.Item2;
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message }, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, ex.Message, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        public HttpResponseMessage DeleteAssetLocationDetailsById(long Id)
        {
            try
            {
                bool status = true;
                string message = "";
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                string HType = "AssetLocation";
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                var Location_details = _Company.GetLevelDetailssById(companyId, HType, Id);
                if (Location_details.Rows.Count == 0 || Convert.ToBoolean(Location_details.Rows[0]["IsActive"]) == false)
                {
                    status = false;
                    message = "Record Not Found Or May Be Inactive.";
                    return Request.CreateResponse(HttpStatusCode.NotFound, new { status, message }, Configuration.Formatters.JsonFormatter);
                }

                long type = Convert.ToInt32(Location_details.Rows[0]["LocationTypeID"].ToString().Trim());
                long BranchId = Convert.ToInt32(Location_details.Rows[0]["BranchId"].ToString().Trim());
                string Name = Location_details.Rows[0]["LocationName"].ToString().Trim();

                var DeleteLocation = _Company.DeleteLocationHierarchy(Id, companyId, type, BranchId, Name);

                status = DeleteLocation.Item1;
                message = DeleteLocation.Item2;
                return Request.CreateResponse(HttpStatusCode.OK, new { status, message }, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, ex.Message, Configuration.Formatters.JsonFormatter);
            }
        }
        //Added By Rutik RG on 27062024 for API End <<
        #endregion

        #region Vendor Delete API
        //Vendor Delete added by hamraj start
        [HttpPost]
        public HttpResponseMessage DeleteVendor(string vendorName, string vendorType)
        {
            try
            {
                bool status = true;
                string message = "";
                long companyId, userId, BranchId = 0;
                var identity = (ClaimsIdentity)User.Identity;
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                var list = _vendor.GetVendorByType(vendorName, vendorType, companyId);
                if (list != null && list.IsActive == true)
                {
                    var res = _vendor.DeleteVendor(list.VendorID, companyId);
                    if (res == 1)
                    {
                        status = true;
                        message = "Vendor Deleted Successfully!!!";
                        var response = Request.CreateResponse(HttpStatusCode.OK);
                        var jsonMessage = new { status, message };
                        response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                        return response;
                    }
                    else
                    {
                        status = false;
                        message = "Error!!!";
                        var response = Request.CreateResponse(HttpStatusCode.OK);
                        var jsonMessage = new { status, message };
                        response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                        return response;
                    }
                }
                else
                {
                    status = false;
                    message = "Vendor Not Found!!!";
                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    var jsonMessage = new { status, message };
                    response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                    return response;
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, ex.Message, Configuration.Formatters.JsonFormatter);
            }
        }

        [HttpPost]
        public HttpResponseMessage DeleteVendorById(long Id)
        {
            try
            {
                bool status = true;
                string message = "";
                long companyId, userId, BranchId = 0;
                var identity = (ClaimsIdentity)User.Identity;
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);
                var list = _vendor.GetVendorById(Id, companyId);
                if (list != null && list.IsActive == true)
                {
                    var res = _vendor.DeleteVendor(list.VendorID, companyId);
                    if (res == 1)
                    {
                        status = true;
                        message = "Vendor Deleted Successfully!!!";
                        var response = Request.CreateResponse(HttpStatusCode.OK);
                        var jsonMessage = new { status, message };
                        response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                        return response;
                    }
                    else
                    {
                        status = false;
                        message = "Error!!!";
                        var response = Request.CreateResponse(HttpStatusCode.OK);
                        var jsonMessage = new { status, message };
                        response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                        return response;
                    }
                }
                else
                {
                    status = false;
                    message = "Vendor Not Found!!!";
                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    var jsonMessage = new { status, message };
                    response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                    return response;
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, ex.Message, Configuration.Formatters.JsonFormatter);
            }
        }
        //Vendor Delete added by hamraj end
        #endregion

        #region User Delete API
        //User Delete added by hamraj start
        [HttpPost]
        public HttpResponseMessage DeleteUser(string UserName)
        {
            bool status = true;
            string message = "";
            long companyId, userId, BranchId = 0;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);

            long UserID = _user.GetUserIdbyUserNameAPI(UserName);
            if (UserID == 0 || UserID == null)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                var jsonMessage = new { status = false, message = "User not found!!!" };
                response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                return response;
            }
            try
            {
                int result = 0;
                var empDet = _user.GetEmployees(AuthenticationHelper.GetCompanyID()).ToList();
                result = _user.DeleteUsers(UserID.ToString(), AuthenticationHelper.GetCompanyID());

                string[] userIds = UserID.ToString().Split(',');
                foreach (var uid in userIds)
                {
                    long id = long.Parse(uid);
                    UserViewModel user = empDet.Where(x => x.UserId == id).FirstOrDefault();
                    if (user != null && user.RoleTypeId != 100)
                    {
                        sendNotifyMail(result.ToString(), user, "delete");
                    }
                }

                if (result > 0)
                {
                    status = true;
                    message = "User deleted successfully,Users which is assigned to asset can not deleted and root admin users can not be deleted";
                }
                else
                {
                    status = false;
                    message = "Asset(s) is/are assigned to User or Root Admin Users, cannot be deleted";
                }

                var response = Request.CreateResponse(HttpStatusCode.OK);
                var jsonMessage = new { status, message };
                response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                return response;
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                var jsonMessage = new { status = false, message = "Error occurs while deleting users" };
                response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                return response;
            }
        }

        [HttpPost]
        public HttpResponseMessage DeleteUserById(long Id)
        {
            bool status = true;
            string message = "";
            long companyId, userId, BranchId = 0;
            var identity = (ClaimsIdentity)User.Identity;
            userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
            companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);

            var UserDetails = _user.GetUserById(Id, companyId);
            //long UserID = 0;
            //.UserId;
            if (UserDetails == null)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                var jsonMessage = new { status = false, message = "User not found!!!" };
                response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                return response;
            }
            try
            {
                int result = 0;
                var empDet = _user.GetEmployees(companyId).ToList();
                result = _user.DeleteUsers(Id.ToString(), companyId);

                string[] userIds = Id.ToString().Split(',');
                foreach (var uid in userIds)
                {
                    long id = long.Parse(uid);
                    UserViewModel user = empDet.Where(x => x.UserId == id).FirstOrDefault();
                    if (user != null && user.RoleTypeId != 100)
                    {
                        sendNotifyMail(result.ToString(), user, "delete");
                    }
                }

                if (result > 0)
                {
                    status = true;
                    message = "User deleted successfully,Users which is assigned to asset can not deleted and root admin users can not be deleted";
                }
                else
                {
                    status = false;
                    message = "Asset(s) is/are assigned to User or Root Admin Users, cannot be deleted";
                }

                var response = Request.CreateResponse(HttpStatusCode.OK);
                var jsonMessage = new { status, message };
                response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                return response;
            }
            catch (Exception ex)
            {
                var response = Request.CreateResponse(HttpStatusCode.InternalServerError);
                var jsonMessage = new { status = false, message = "Error occurs while deleting users" };
                response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                return response;
            }
        }
        //User Delete added by hamraj end
        #endregion

        #region Customer Delete API
        //Customer Delete added by hamraj start
        [HttpPost]
        public HttpResponseMessage DeleteCustomer(string customerName)
        {
            try
            {
                bool status = true;
                string message = "";
                long companyId, userId, BranchId = 0;
                var identity = (ClaimsIdentity)User.Identity;
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);

                var vendorType = "Customer";
                var list = _vendor.GetVendorByType(customerName, vendorType, companyId);
                var count = _iAssetService.GetCurrentLetOutsOnPartyId(companyId, list.VendorID).Where(x => x.LetOutStatusId == 101 || x.LetOutStatusId == 102).Count();
                if (count > 0)
                {
                    status = false;
                    message = "Customer cannot be deleted as assets are initiated for let out or let out.";
                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    var jsonMessage = new { status, message };
                    response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                    return response;
                }
                if (list != null && list.IsActive == true)
                {
                    var res = _vendor.DeleteVendor(list.VendorID, companyId);
                    if (res == 1)
                    {
                        status = true;
                        message = "Customer Deleted Successfully!!!";
                        var response = Request.CreateResponse(HttpStatusCode.OK);
                        var jsonMessage = new { status, message };
                        response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                        return response;
                    }
                    else
                    {
                        status = false;
                        message = "Error!!!";
                        var response = Request.CreateResponse(HttpStatusCode.OK);
                        var jsonMessage = new { status, message };
                        response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                        return response;
                    }
                }
                else
                {
                    status = false;
                    message = "Customer Not Found!!!";
                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    var jsonMessage = new { status, message };
                    response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                    return response;
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, ex.Message, Configuration.Formatters.JsonFormatter);
            }
        }
        //Customer Delete added by hamraj end

        [HttpPost]
        public HttpResponseMessage DeleteCustomerById(long Id)
        {
            try
            {
                bool status = true;
                string message = "";
                long companyId, userId, BranchId = 0;
                var identity = (ClaimsIdentity)User.Identity;
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value);
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value);

                var vendorType = "Customer";
                var list = _vendor.GetVendorById(Id, companyId);
                if (list == null)
                {
                    status = false;
                    message = "Customer Not Found!!!";
                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    var jsonMessage = new { status, message };
                    response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                    return response;
                }
                var count = _iAssetService.GetCurrentLetOutsOnPartyId(companyId, list.VendorID).Where(x => x.LetOutStatusId == 101 || x.LetOutStatusId == 102).Count();
                if (count > 0)
                {
                    status = false;
                    message = "Customer cannot be deleted as assets are initiated for let out or let out.";
                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    var jsonMessage = new { status, message };
                    response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                    return response;
                }
                if (list != null && list.IsActive == true)
                {
                    var res = _vendor.DeleteVendor(list.VendorID, companyId);
                    if (res == 1)
                    {
                        status = true;
                        message = "Customer Deleted Successfully!!!";
                        var response = Request.CreateResponse(HttpStatusCode.OK);
                        var jsonMessage = new { status, message };
                        response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                        return response;
                    }
                    else
                    {
                        status = false;
                        message = "Error!!!";
                        var response = Request.CreateResponse(HttpStatusCode.OK);
                        var jsonMessage = new { status, message };
                        response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                        return response;
                    }
                }
                else
                {
                    status = false;
                    message = "Customer Not Found!!!";
                    var response = Request.CreateResponse(HttpStatusCode.OK);
                    var jsonMessage = new { status, message };
                    response.Content = new StringContent(JsonConvert.SerializeObject(jsonMessage), Encoding.UTF8, "application/json");
                    return response;
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.OK, ex.Message, Configuration.Formatters.JsonFormatter);
            }
        }

        #endregion

        #endregion


        #region Get All Lookups List API
        [HttpGet]
        public HttpResponseMessage GetUserRoleNameLookups()
        {
            try
            {
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());
                string username;
                username = identity.Claims.FirstOrDefault(c => c.Type == "username").Value.ToString();

                //int? roleTypeId = 100;
                //long branchId;
                //List<BranchViewModel> branch = new List<BranchViewModel>();

                //roleTypeId = _baseInterface.ICompany.GetUserRoleType(userId);
                //branch = _baseInterface.ICompany.GetBranchesByUserID(userId, companyId, roleTypeId);

                var Roles = GetRolesWithPermissions(companyId);
                var RolesDetails = new
                {
                    Roles = Roles.Select(x => new { RoleId = x.Id, RoleName = x.Name, ParentId = x.ParentId })
                };

                return Request.CreateResponse(HttpStatusCode.OK, RolesDetails, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetDepartmentLookups()
        {
            try
            {
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());
                string username;
                username = identity.Claims.FirstOrDefault(c => c.Type == "username").Value.ToString();

                //int? roleTypeId = 100;
                //long branchId;
                //List<BranchViewModel> branch = new List<BranchViewModel>();

                //roleTypeId = _baseInterface.ICompany.GetUserRoleType(userId);
                //branch = _baseInterface.ICompany.GetBranchesByUserID(userId, companyId, roleTypeId);

                var DepartmentList = _baseInterface.IDepartmentService.GetDepartments(companyId).Select(x => new NumericLookupItem { Text = x.Name, Value = x.DepartmentID }).ToList();
                var DepartmentDetails = new
                {
                    DepartmentsLookup = DepartmentList.Select(x => new { DepartmentId = x.Value, DepartmentName = x.Text, ParentId = x.ParentId, TypeId = x.TypeId })
                };

                return Request.CreateResponse(HttpStatusCode.OK, DepartmentDetails, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetCategoryLookups()
        {
            try
            {
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());
                string username;
                username = identity.Claims.FirstOrDefault(c => c.Type == "username").Value.ToString();

                int? roleTypeId = 100;
                //long branchId;
                //List<BranchViewModel> branch = new List<BranchViewModel>();

                roleTypeId = _baseInterface.ICompany.GetUserRoleType(userId);
                //branch = _baseInterface.ICompany.GetBranchesByUserID(userId, companyId, roleTypeId);

                OrganizationDetailsModel companydetails = _masterApi.GetOrganizationDetailsByCompId(companyId);

                var MainCategoriesList = _masterApi.GetAssetCategories(companyId, userId, roleTypeId.HasValue ? roleTypeId.Value : 0, companydetails.FirmCategory ?? 0).Where(x => x.ParentID == null).Select(x => new NumericLookupItem { Text = x.Name, Value = x.AssetCategoryId }).ToList();
                var CategoryDetails = new
                {
                    CategoriesLookup = MainCategoriesList.Select(x => new { CategoryId = x.Value, CategoryName = x.Text, ParentId = x.ParentId, TypeId = x.TypeId })
                };

                return Request.CreateResponse(HttpStatusCode.OK, CategoryDetails, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
        }

        [HttpGet]
        public HttpResponseMessage GetBranchLookups()
        {
            try
            {
                long companyId, userId;
                var identity = (ClaimsIdentity)User.Identity;
                userId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "userid").Value.ToString());
                companyId = long.Parse(identity.Claims.FirstOrDefault(c => c.Type == "companyid").Value.ToString());
                string username;
                username = identity.Claims.FirstOrDefault(c => c.Type == "username").Value.ToString();

                //int? roleTypeId = 100;
                //long branchId;
                //List<BranchViewModel> branch = new List<BranchViewModel>();

                //roleTypeId = _baseInterface.ICompany.GetUserRoleType(userId);
                //branch = _baseInterface.ICompany.GetBranchesByUserID(userId, companyId, roleTypeId);
                
                var BranchList = _baseInterface.ICompany.GetAllBrancheDetails(companyId).Select(x => new NumericLookupItem { Text = x.Name, Value = x.BranchId }).ToList();
                var BranchDetails = new
                {
                    BranchLookup = BranchList.Select(x => new { BranchId = x.Value, BranchName = x.Text, ParentId = x.ParentId, TypeId = x.TypeId })
                };

                return Request.CreateResponse(HttpStatusCode.OK, BranchDetails, Configuration.Formatters.JsonFormatter);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest);
            }
        }
        #endregion


        //Added by hamraj for API start
        public void sendNotifyMail(string result, UserViewModel model, string insertupdate)
        {
            if (result == "1")
            {
                var emailObj = new EmailNotification();
                var notificationDays = _Company.GetCompanyNotificationDaysById(model.CompanyId);
                var notifyUserMappings = _companyApi.GetNotifyUserMappings(AuthenticationHelper.GetCompanyID(), 110).Where(x => x.EmailId != "").ToList();
                if (notificationDays != null && notificationDays.SmtpFromMail != "" && notificationDays.SmtpFromPass != "" && notificationDays.SmtpHost != "")
                {
                    string UserName = "Team";
                    string AssigneeEmails = "";
                    string EmailBody = "";
                    string subject = "";
                    if (insertupdate == "insert")
                    {
                        EmailBody = "<div>User Added Successfully.<br /><br /></div>";
                        EmailBody = EmailBody + "<div><table><tr><td>User Name</td><td>: " + model.UserName + ".</td></tr>";
                        EmailBody = EmailBody + "<tr><td>User Role</td><td>: " + model.RoleName + ".</td></tr></table></div>";
                        subject = "New User Details";
                    }
                    else if (insertupdate == "update")
                    {
                        EmailBody = "<div>User Updated Successfully.<br /><br /></div>";
                        EmailBody = EmailBody + "<div><table><tr><td>User Name</td><td>: " + model.UserName + ".</td></tr>";
                        EmailBody = EmailBody + "<tr><td>User Role</td><td>: " + model.RoleName + ".</td></tr></table></div>";
                        subject = "Updated User Details";
                    }
                    else
                    {
                        EmailBody = "<div>User Deleted Successfully.<br /><br /></div>";
                        EmailBody = EmailBody + "<div><table><tr><td>User Name</td><td>: " + model.UserName + ".</td></tr>";
                        EmailBody = EmailBody + "<tr><td>User Role</td><td>: " + model.RoleName + ".</td></tr></table></div>";
                        subject = "Deleted User Details";
                    }
                    foreach (var id in notifyUserMappings)
                    {
                        if (AssigneeEmails == "")
                        {
                            AssigneeEmails = id.EmailId;
                        }
                        else
                        {
                            AssigneeEmails = AssigneeEmails + "," + id.EmailId;
                        }

                    }
                    //BackgroundJob.Enqueue(() => 
                    //emailObj.SendEMail(UserName, AssigneeEmails, "", notificationDays.SmtpHost, notificationDays.SmtpPort.ToString(), notificationDays.SmtpFromMail, notificationDays.SmtpFromPass, subject, EmailBody);                                  //Commented By Rutik RG on 16032024 for AIPL - 06669 [Sprint-60]
                    emailObj.SendEMailIsSSL(UserName, AssigneeEmails, "", notificationDays.SmtpHost, notificationDays.SmtpPort.ToString(), notificationDays.SmtpFromMail, notificationDays.SmtpFromPass, subject, EmailBody, notificationDays.IsSSL.ToString(), 1, 1);   //Modified By Rutik RG on 16032024 for AIPL - 06669 [Sprint-60]
                }
            }
        }
        //Added by hamraj for API end

        public static string ConvertIntoJson(DataTable dt)
        {
            var jsonString = new StringBuilder();
            if (dt.Rows.Count > 0)
            {
                jsonString.Append("[");
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    jsonString.Append("{");
                    for (int j = 0; j < dt.Columns.Count; j++)
                        jsonString.Append("\"" + dt.Columns[j].ColumnName + "\":\""
                            + dt.Rows[i][j].ToString().Replace('"', '\"') + (j < dt.Columns.Count - 1 ? "\"," : "\""));

                    jsonString.Append(i < dt.Rows.Count - 1 ? "}," : "}");
                }
                return jsonString.Append("]").ToString();
            }
            else
            {
                return "[]";
            }
        }
    }
}
