using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.Web.UI.WebControls;


namespace LoginPage_mvc.Models
{

    public class Login
    {
       
        public string EMAILID { get; set; }
      
        public string Password { get; set; }

        public string GetUserRole(string EMAILID)
        {
            LoginForm loginForm = new LoginForm();
            return loginForm.GetUserRole(EMAILID,Password);
        }
    }

    public class LoginForm
    {
        SqlConnection con = new SqlConnection("Server=HARISH\\MSSQLSERVER1;initial catalog=LMS;integrated security=true");

        internal string GetUserRole(string eMAILID, string v, object password)
        {
            throw new NotImplementedException();
        }

        public bool UserLogin(Login L)
        {
            string query = "SELECT COUNT(*) FROM tblLogin WHERE EMAILID = @Email AND Password = @Password";
            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@Email", L.EMAILID);
            cmd.Parameters.AddWithValue("@Password", L.Password); 

            con.Open();
           

            int count = (int)cmd.ExecuteScalar();
            con.Close();
            return count > 0;
        }

        public string GetUserRole(string eMAILID, string Password)
        {
            if (string.IsNullOrEmpty(eMAILID) || string.IsNullOrEmpty(Password))
            {
                return string.Empty; // If email or password is empty, return empty string
            }

            SqlConnection con = new SqlConnection("Server=HARISH\\MSSQLSERVER1;initial catalog=LMS;integrated security=true");
            {
                string query = "SELECT Role FROM tblLogin WHERE EMAILID = @Email AND Password = @Password";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Email", eMAILID);
                cmd.Parameters.AddWithValue("@Password", Password);

                con.Open();
                object result = cmd.ExecuteScalar();
                con.Close();

                return result != null ? result.ToString() : string.Empty;

            }

        }
    }
}
