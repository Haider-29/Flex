using System;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace FLEXX.Pages
{
    public class FacultyLoginModel : PageModel
    {
        public void OnGet()
        {
        }

        private readonly IConfiguration _configuration;

        public FacultyLoginModel(IConfiguration configuration)
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
                string query = "SELECT * FROM Users WHERE USERNAME = @Email AND Password = @Password and role = 'F'";

                cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Email", Email);
                cmd.Parameters.AddWithValue("@Password", Password);

                SqlDataReader reader = cmd.ExecuteReader();

                //Console.WriteLine(reader["FName"].ToString());

                if (reader.HasRows)
                {
                    Message = "Login Successful!";
                    cmd.Dispose();
                    conn.Close();
                    return RedirectToPage("/FacultyInterface");
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
