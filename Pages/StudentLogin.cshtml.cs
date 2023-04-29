using System;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace FLEXX.Pages
{
    public class StudentLoginModel : PageModel
    {
        public void OnGet()
        {
        }

        private readonly IConfiguration _configuration;

        public StudentLoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public string RollNumber { get; set; }

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
                string query = "SELECT Username, Password FROM Users WHERE USERNAME = @Email AND Password = @Password and role = 'S'";

                cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Email", RollNumber);
                cmd.Parameters.AddWithValue("@Password", Password);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.HasRows)
                {
                    Message = "Login Successful!";
                    cmd.Dispose();
                    conn.Close();
                    return RedirectToPage("/StudentInterface");
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
