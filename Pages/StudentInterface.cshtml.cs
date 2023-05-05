using System;
using System.Data.SqlClient;
using System.IO;
using System.Reflection.PortableExecutable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace FLEXX.Pages
{
    public class StudentInterfaceModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public StudentInterfaceModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public class Student
        {
            public string Rollno { get; set; }
            public string Name { get; set; }
            public string Semester { get; set; }

        }

        public class Course
        {
            public string courseID { get; set; }
            public string courseName { get; set; }
            public string courseHours { get; set; }
        }

        public List<Course> Courses { get; set; }
        public Student currStudent { get; set; }

        [BindProperty]
        public string StudentId { get; set; }

        [BindProperty]
        public string StudentPassword { get; set; }
        public async Task OnGetAsync(string id, string password)
        {
            StudentId = id;
            StudentPassword = password;
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "select Fname, Lname from users where username = @StudentId";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@StudentId", StudentId);

                await conn.OpenAsync(); 

                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Student student = new Student()
                    {
                        Name = reader.GetString(0) + " " + reader.GetString(1)
                    };
                     
                    currStudent = student;
                }

                reader.Close();
                cmd.Dispose();

                query = "select semester from registration where studentid = @StudentId";
                SqlCommand cmd2 = new SqlCommand(query, conn);
                cmd2.Parameters.AddWithValue("@StudentId", StudentId);

                reader = await cmd2.ExecuteReaderAsync();

                while (await reader.ReadAsync()) 
                {
                    currStudent.Semester = reader.GetString(0);
                }

                reader.Close();
                cmd2.Dispose();

                query = "select coursecode, courseName, creditHours from course c join registration r on c.coursecode = r.offcourseid where studentid = @StudentId";
                SqlCommand cmd3 = new SqlCommand(query, conn);
                cmd3.Parameters.AddWithValue("@StudentId", StudentId);

                reader = await cmd3.ExecuteReaderAsync();

                Courses = new List<Course>();
                
                while (await reader.ReadAsync())
                {
                    Course course = new Course()
                    {
                        courseID = reader.GetString(0),
                        courseName = reader.GetString(1),
                        courseHours = reader.GetInt32(2).ToString()
                    };

                    Courses.Add(course);
                }

                reader.Close();
                cmd3.Dispose();


                //conn.Close();
            }
                
        }
    }
}
