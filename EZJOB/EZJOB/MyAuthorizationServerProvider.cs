using DBAccess;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OAuth;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
namespace EZJOB
{
    public class MyAuthorizationServerProvider : OAuthAuthorizationServerProvider
    {
        public override async Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            context.Validated();
        }
        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {
            var identity = new ClaimsIdentity(context.Options.AuthenticationType);
            string sqlQuery = "", errorMessage = "";
            string companyId = "", branchId = "", userName = "", password = "";
            int errnum = 0;
            ConnectionHandler con = new ConnectionHandler();
            eMail mail = new eMail();
          //  var a=mail.SendMail("rupeshghosalkar3333@outlook.com","Tracet@123", "smtp-mail.outlook.com",587,"shabbirbadla@gmail.com","Password Recovery", "Hi Shabbir, <br><br> Your Password:admin@123 <br><br>Regards,<br>Rupesh.");
            DataTable dtRec;
            try
            {
                try
                {
                    companyId = context.Request.Headers["companyid"].ToString().Trim();
                    branchId = context.Request.Headers["branchid"].ToString().Trim();
                    userName = context.Request.Headers["username"].ToString().Trim();
                    password = context.Request.Headers["password"].ToString().Trim();

                }
                catch (Exception e)
                {
                    context.SetError("Invalid grant", "Please provide companyid,branchid,username & password in header parameter");
                }


                dtRec = new DataTable();
                sqlQuery = "execute ezbusdb.SPGetEmployee " + companyId + "," + branchId + "," + userName + "," + password + ",'1'";
                dtRec = con.executeSelect(sqlQuery);

                if (dtRec.Rows.Count == 0)
                {
                    errnum++;
                    errorMessage = errorMessage + errnum + " Provided user is not access for selected company.";
                }

                dtRec = new DataTable();
                sqlQuery = "execute ezbusdb.SPGetEmployee " + companyId + "," + branchId + "," + userName + "," + password + ",'2'";
                dtRec = con.executeSelect(sqlQuery);
                if (dtRec.Rows.Count == 0)
                {
                    errnum++;
                    errorMessage = errorMessage + errnum + " Provided user is not access for selected branch.";
                }


                dtRec = new DataTable();
                sqlQuery = "execute ezbusdb.SPGetEmployee " + companyId + "," + branchId + "," + userName + "," + password + ",'3'";
                dtRec = con.executeSelect(sqlQuery);
                if (dtRec.Rows.Count == 0)
                {
                    errnum++;
                    errorMessage = errorMessage + errnum + " Invalid Username.";
                }

                if (errorMessage == "")
                {
                    dtRec = new DataTable();
                    sqlQuery = "execute ezbusdb.SPGetEmployee " + companyId + "," + branchId + "," + userName + "," + password + ",'4'";
                    dtRec = con.executeSelect(sqlQuery);

                    if (dtRec.Rows.Count > 0)
                    {

                        identity.AddClaim(new Claim(ClaimTypes.Role, "admin"));
                        identity.AddClaim(new Claim("username", context.Request.Headers["username"]));
                        identity.AddClaim(new Claim("password", context.Request.Headers["password"]));
                        identity.AddClaim(new Claim("companyid", context.Request.Headers["companyid"]));
                        identity.AddClaim(new Claim("branchid", context.Request.Headers["branchid"]));
                        identity.AddClaim(new Claim("uname", dtRec.Rows[0]["Empname"].ToString()));

                        
                        var ticket = new AuthenticationTicket(identity, null);
                        context.Validated(ticket);
                    }
                    else
                    {
                        context.SetError("Invalid grant", "Incorrect Password");
                        return;
                    }
                }
                else
                {
                    context.SetError("Invalid grant", errorMessage);
                }

            }
            catch (Exception ex)
            {
                context.SetError("Invalid grant", ex.ToString());

            }
        }
    }
}