using System;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;


namespace FLEXX.Pages
{
    public class FacultySignupModel : PageModel
    {
        public void OnGet()
        {
        }

        private readonly IConfiguration _configuration;

        public FacultySignupModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        [BindProperty]
        public string First_Name { get; set; }

        [BindProperty]
        public string Last_Name { get; set; }

        public string Message { get; set; }

        public IActionResult OnPost()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd;
                string query = "Insert into users values(@Email,@Password,@First_Name,@Last_Name, 'F')";

                cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Email", Email);
                cmd.Parameters.AddWithValue("@Password", Password);
                cmd.Parameters.AddWithValue("@First_Name", First_Name);
                cmd.Parameters.AddWithValue("@Last_Name", Last_Name);

                try
                {
                    int rowsAffected = cmd.ExecuteNonQuery();

                    cmd.Dispose();
                    conn.Close();

                    if (rowsAffected > 0)
                    {
                        // Insertion was successful
                        Message = "User registration successful!";
                        return RedirectToPage("/FacultyLogin"); // Redirect to login page or any other page you'd like
                    }
                    else
                    {
                        // Insertion failed
                        Message = "User registration failed. Please try again.";
                        return Page(); // Stay on the same page and show the error message
                    }
                }
                catch (SqlException ex)
                {
                    cmd.Dispose();
                    conn.Close();

                    if (ex.Number == 2627)
                    {
                        // Primary key violation (duplicate email)
                        Message = "The email address is already in use. Please choose another one.";
                    }
                    else
                    {
                        // Other SQL errors
                        Message = "An error occurred during registration. Please try again.";
                    }

                    return Page(); // Stay on the same page and show the error message
                }
            }

        }

    }
}
