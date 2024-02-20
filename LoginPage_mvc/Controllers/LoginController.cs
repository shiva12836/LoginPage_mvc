using LoginPage_mvc.Models;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace LoginPage_mvc.Controllers
{

    public class LoginController : Controller
    {
        public object executiveId;
        public object leadId;
        public object AssignedExecutiveId;



        // GET: Login
        public ActionResult Index()
        {
            return View();
        }

        public class Lead
        {
            public int LeadId { get; set; }
            public string LName { get; set; }
            public string Address { get; set; }
            public string Program { get; set; }
            public string PhoneNo { get; set; }
            public string MailId { get; set; }
            public string Qualification { get; set; }
            public string University { get; set; }
            public string Website { get; set; }
            public string Source { get; set; }
            public string TypeofSource { get; set; }
            public string AssignedExecutiveId { get; set; }
            public string Assign { get; set; }
            public string MEName { get; set; }
            public string Uploadedby { get; set; }
        }

        public ActionResult Admin()
        {
            // Check user role before allowing access to the admin page
            string role = GetUserRoleFromSession();
            if (!string.IsNullOrEmpty(role) && role.ToLower() == "admin")
            {
                List<Lead> leads = GetLeadsFromDatabase();
                return View(leads);
            }
            else
            {
                // Redirect to login if user is not authenticated or not authorized
                return RedirectToAction("Index");
            }
        }

        public ActionResult Logout()
        {

            FormsAuthentication.SignOut();

           
            return RedirectToAction("Index"); 
        }

        [HttpPost]
        public ActionResult Index(Models.Login model)
        {
            LoginForm loginForm = new LoginForm();
            string role = loginForm.GetUserRole(model.EMAILID, model.Password);
            if (!string.IsNullOrEmpty(role))
            {
                // Store user role in session
                Session["UserRole"] = role.ToLower();
                switch (role.ToLower())
                {
                    case "admin":
                        return RedirectToAction("Admin");
                    case "manager":
                        return RedirectToAction("Manager");
                    case "marketing executive":
                        return RedirectToAction("MarketingExecutive");
                    default:
                        ModelState.AddModelError("", "Invalid role");
                        break;
                }
            }
            else
            {
                ModelState.AddModelError("", "Invalid username or password");
            }
            return View(model);
        }

        private string GetUserRoleFromSession()
        {
            return Session["UserRole"].ToString();
        }

        private List<Lead> GetLeadsFromDatabase()
        {
            List<Lead> leads = new List<Lead>();
            using (SqlConnection con = new SqlConnection("Server=HARISH\\MSSQLSERVER1;initial catalog=LMS;integrated security=true"))
            {
                string query = "SELECT LeadId, LName, Address, Program, PhoneNo, MailId, Qualification, University, Website, Source, TypeofSource FROM tblLeads";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        Lead lead = new Lead
                        {
                            LeadId = reader["LeadID"] != DBNull.Value ? Convert.ToInt32(reader["LeadID"]) : 0,
                            LName = reader["LName"] != DBNull.Value ? reader["LName"].ToString() : string.Empty,
                            Address = reader["Address"] != DBNull.Value ? reader["Address"].ToString() : string.Empty,
                            Program = reader["Program"] != DBNull.Value ? reader["Program"].ToString() : string.Empty,
                            PhoneNo = reader["PhoneNo"] != DBNull.Value ? reader["PhoneNo"].ToString() : string.Empty,
                            MailId = reader["MailId"] != DBNull.Value ? reader["MailId"].ToString() : string.Empty,
                            Qualification = reader["Qualification"] != DBNull.Value ? reader["Qualification"].ToString() : string.Empty,
                            University = reader["University"] != DBNull.Value ? reader["University"].ToString() : string.Empty,
                            Website = reader["Website"] != DBNull.Value ? reader["Website"].ToString() : string.Empty,
                            Source = reader["Source"] != DBNull.Value ? reader["Source"].ToString() : string.Empty,
                            TypeofSource = reader["TypeofSource"] != DBNull.Value ? reader["TypeofSource"].ToString() : string.Empty,

                        };
                        leads.Add(lead);
                    }
                }
            }
            return leads;
        }

        public ActionResult Manager(HttpPostedFileBase file, Models.Login model)
        {
            if (file != null && file.ContentLength > 0)
            {
                string filePath = Path.Combine(Server.MapPath("~/App_Data"), Path.GetFileName(file.FileName));
                file.SaveAs(filePath);

                InsertDataIntoDatabase(filePath);

                ViewBag.message = "File uploaded successfully";

                // Set the manager's name in session
                Session["User"] = model.EMAILID;
            }
            else
            {
                ViewBag.message = "No file uploaded";
            }

            string role = GetUserRoleFromSession();
            if (!string.IsNullOrEmpty(role) && role.ToLower() == "manager")
            {
                List<Lead> leads = GetLeadsFromDatabase();

                // Fetch marketing executives for dropdown
                List<Lead> marketingExecutives = GetMarketingExecutivesFromDatabase();
                ViewBag.MarketingExecutives = new SelectList(marketingExecutives, "MEName", "MEName");

                // Update uploadedby column
                UpdateUploadedBy();

                return View(leads);
            }
            else
            {
                return RedirectToAction("Index");
            }
        }


        private void UpdateUploadedBy()
        {
            string managerName = Session["User"] as string;

            if (!string.IsNullOrEmpty(managerName))
            {
                string connectionString = "Server=HARISH\\MSSQLSERVER1;initial catalog=LMS;integrated security=true";

                // SQL update statement
                string updateStatement = "UPDATE tblLeads SET uploadedby = @managerName WHERE uploadedby IS NULL";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    using (SqlCommand command = new SqlCommand(updateStatement, connection))
                    {
                        // Add parameters to the command
                        command.Parameters.AddWithValue("@managerName", managerName);

                        try
                        {
                            connection.Open();
                            int rowsAffected = command.ExecuteNonQuery();
                            // Optionally, you can check the number of rows affected
                        }
                        catch (Exception ex)
                        {
                            // Handle exceptions
                            Console.WriteLine("Error updating uploadedby column: " + ex.Message);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Manager name not found in session.");
            }
        }



        private void InsertDataIntoDatabase(string filePath)
        {
            string connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath + ";Extended Properties=Excel 12.0;";
            using (OleDbConnection conn = new OleDbConnection(connectionString))
            {
                conn.Open();
                OleDbCommand cmd = new OleDbCommand("SELECT * FROM [Sheet1$]", conn);
                using (OleDbDataReader dr = cmd.ExecuteReader())
                {
                    string sqlConnectionString = "Server=HARISH\\MSSQLSERVER1;initial catalog=LMS;integrated security=true";
                    using (SqlConnection connection = new SqlConnection(sqlConnectionString))
                    {
                        connection.Open();

                        while (dr.Read())
                        {
                            string insertQuery = "INSERT INTO tblLeads (LName, Address, Program, PhoneNo, MailId, Qualification, University, Website, Source, utm_source, utm_medium, utm_campaign, utm_term, utm_content, TypeofSource, ManagerID, AssignedExecutiveId) " +
                                "VALUES (@LName, @Address, @Program, @PhoneNo, @MailId, @Qualification, @University, @Website, @Source, @utm_source, @utm_medium, @utm_campaign, @utm_term, @utm_content, @TypeofSource, @ManagerID, @AssignedExecutiveId)";
                            using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                            {
                                insertCommand.Parameters.AddWithValue("@LName", dr["LName"].ToString());
                                insertCommand.Parameters.AddWithValue("@Address", dr["Address"].ToString());
                                insertCommand.Parameters.AddWithValue("@Program", dr["Program"].ToString());
                                insertCommand.Parameters.AddWithValue("@PhoneNo", dr["PhoneNo"].ToString().Substring(0, Math.Min(dr["PhoneNo"].ToString().Length, 50))); // Assuming a maximum length of 50 characters for the PhoneNo column
                                insertCommand.Parameters.AddWithValue("@MailId", dr["MailId"].ToString());
                                insertCommand.Parameters.AddWithValue("@Qualification", dr["Qualification"].ToString());
                                insertCommand.Parameters.AddWithValue("@University", dr["University"].ToString());
                                insertCommand.Parameters.AddWithValue("@Website", dr["Website"].ToString());
                                insertCommand.Parameters.AddWithValue("@Source", dr["Source"].ToString());
                                insertCommand.Parameters.AddWithValue("@utm_source", dr["utm_source"].ToString());
                                insertCommand.Parameters.AddWithValue("@utm_medium", dr["utm_medium"].ToString());
                                insertCommand.Parameters.AddWithValue("@utm_campaign", dr["utm_campaign"].ToString());
                                insertCommand.Parameters.AddWithValue("@utm_term", dr["utm_term"].ToString());
                                insertCommand.Parameters.AddWithValue("@utm_content", dr["utm_content"].ToString());
                                insertCommand.Parameters.AddWithValue("@TypeofSource", dr["TypeofSource"].ToString());
                                insertCommand.Parameters.AddWithValue("@ManagerID", dr["ManagerID"].ToString());
                                insertCommand.Parameters.AddWithValue("@AssignedExecutiveId", dr["AssignedExecutiveId"].ToString().Substring(0, Math.Min(dr["AssignedExecutiveId"].ToString().Length, 50)));
                                insertCommand.ExecuteNonQuery();
                            }
                        }

                    }
                }
            }
        }

        //[HttpPost]
        //public ActionResult Manager(string MEName, int LeadId)
        //{
        //    try
        //    {
        //        using (SqlConnection connection = new SqlConnection("Server=HARISH\\MSSQLSERVER1;initial catalog=LMS;integrated security=true"))
        //        {
        //            string query = "UPDATE tblLeads SET AssignedExecutiveId = @MEName WHERE LeadId = @LeadId";

        //            using (SqlCommand command = new SqlCommand(query, connection))
        //            {
        //                command.Parameters.AddWithValue("@MEName", MEName);
        //                command.Parameters.AddWithValue("@LeadId", LeadId);

        //                connection.Open();
        //                int rowsAffected = command.ExecuteNonQuery();

        //                if (rowsAffected > 0)
        //                {
        //                    TempData["SuccessMessage"] = "Lead assigned successfully.";
        //                    return Json(new { success = true }); // Return JSON response for successful assignment
        //                }
        //                else
        //                {
        //                    TempData["ErrorMessage"] = "Failed to assign lead. Please try again.";
        //                    return Json(new { success = false }); // Return JSON response for failed assignment
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["ErrorMessage"] = "An error occurred while assigning lead. Please try again later.";
        //        return Json(new { success = false, message = ex.Message }); // Return JSON response for exception
        //    }
        //}


        //public ActionResult Manager()
        //{


        //    string role = GetUserRoleFromSession();
        //    if (!string.IsNullOrEmpty(role) && role.ToLower() == "manager")
        //    {
        //        List<Lead> leads = GetLeadsFromDatabase();
        //        List<Lead> marketingExecutives = GetMarketingExecutivesFromDatabase(); 
        //        ViewBag.MarketingExecutives = new SelectList(marketingExecutives, "MEName", "MEName");
        //        return View(leads);
        //    }
        //    else
        //    {

        //        return RedirectToAction("Index");
        //    }
        //}



        

        //[HttpPost] // Change to HttpPost
        //public ActionResult Assign(string MEName, int LeadId)
        //{
        //    try
        //    {
        //        using (SqlConnection connection = new SqlConnection("Server = HARISH\\MSSQLSERVER1; initial catalog = LMS; integrated security = true"))
        //        {
        //            string query = "UPDATE tblLeads SET AssignedExecutiveId = @MEName WHERE LeadId = @LeadId";

        //            using (SqlCommand command = new SqlCommand(query, connection))
        //            {
        //                command.Parameters.AddWithValue("@MEName", MEName);
        //                command.Parameters.AddWithValue("@LeadId", LeadId);

        //                connection.Open();
        //                int rowsAffected = command.ExecuteNonQuery();

        //                if (rowsAffected > 0)
        //                {
        //                    return Json(new { success = true }); // Return JSON indicating success
        //                }
        //                else
        //                {
        //                    return Json(new { success = false, error = "Failed to assign lead. Please try again." });
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, error = "An error occurred while assigning lead. Please try again later." });
        //    }
        //}

        [HttpGet]
        public ActionResult MarketingExecutive()
        {


            return View();
        }

        private List<Lead> GetMarketingExecutivesFromDatabase()
        {
            List<Lead> executives = new List<Lead>();


            string query = "SELECT MEName FROM tblMarketingExecutive";

            using (SqlConnection connection = new SqlConnection("Server = HARISH\\MSSQLSERVER1; initial catalog = LMS; integrated security = true"))
            {
                using (SqlCommand command = new SqlCommand(query, connection))

                {
                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();

                    while (reader.Read())
                    {
                        Lead executive = new Lead();
                        executive.MEName = reader["MEName"].ToString();

                        executives.Add(executive);
                    }
                }
            }

            return executives;
        }
    }
}