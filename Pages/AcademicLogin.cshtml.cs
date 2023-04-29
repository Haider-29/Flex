using System;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace FLEXX.Pages
{
    public class AcademicLoginModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public AcademicLoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string Message { get; set; }

        public IActionResult OnPost()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd;
                string query = "SELECT Username, Password FROM Users WHERE USERNAME = @Email AND Password = @Password and role = 'A'";

                cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Email", Email);
                cmd.Parameters.AddWithValue("@Password", Password);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    Message = "Login Successful!";
                    cmd.Dispose();
                    conn.Close();
                    return RedirectToPage("/AcademicInterface");
                }
                else
                {
                    Message = "Invalid Information";
                }
                cmd.Dispose();
                conn.Close();
            }
            return Page();
        }
    }
}
