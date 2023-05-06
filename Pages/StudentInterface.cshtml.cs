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

            public string section { get; set; }

        }


        public class Attendance {
            
            public DateTime Date { get; set; }

            public string Status { get; set; }

            public int contactHours { get; set; }

            public string courseCode { get; set; }
        }


        public class EnrolledCourse
        {
            public string id { get; set; }
            public string name { get; set; }
        }


        public List<Attendance> courseAttendances { get; set; }
        public List<EnrolledCourse> enrolledCourses { get; set; }

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

                query = "select sectionid from student_section where STUDENTID = @StudentId";
                SqlCommand cmd3 = new SqlCommand(query, conn);
                cmd3.Parameters.AddWithValue("@StudentId", StudentId);
                reader = await cmd3.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    currStudent.section = reader.GetString(0);
                }
                reader.Close();
                cmd3.Dispose();


                courseAttendances = new List<Attendance>();
                query = "select * from attendance where StudentID = @StudentId";
                SqlCommand cmd4 = new SqlCommand(query, conn);
                cmd4.Parameters.AddWithValue("@StudentId", StudentId);
                reader = await cmd4.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Attendance attendance = new Attendance
                    {
                        courseCode = reader.GetString(2),
                        contactHours = reader.GetInt32(4),
                        Date = reader.GetDateTime(5),
                        Status = reader.GetString(6),
                    };

                    courseAttendances.Add(attendance);

                }


                reader.Close();
                cmd4.Dispose();

                enrolledCourses = new List<EnrolledCourse>();
                query = "select OffCourseID, CourseName from Registration inner join Course on Registration.OffCourseID = Course.CourseCode WHERE StudentID = @StudentId and APPROVED = 1;";

                SqlCommand cmd5 = new SqlCommand(query, conn);
                cmd5.Parameters.AddWithValue("@StudentId", StudentId);

                reader = await cmd5.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    EnrolledCourse course = new EnrolledCourse { 
                        id = reader.GetString(0),
                        name = reader.GetString(1),
                    
                    };

                    enrolledCourses.Add(course);
                }

                reader.Close();
                cmd5.Dispose();


            }

        }
    }
}
