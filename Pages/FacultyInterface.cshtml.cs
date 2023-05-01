using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace FLEXX.Pages
{


    public class MarksDistributionTable {
        public string? CourseCode { get; set; }
        public string? CourseTitle { get; set; }
        public string? SectionName { get; set; }

        public string? TeacherName { get; set; }


    }

    

    public class FacultyInterfaceModel : PageModel
    {

        private readonly IConfiguration _configuration;

        public FacultyInterfaceModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<MarksDistributionTable> AssignedSections { get; set; }


        [BindProperty]
        public string TeacherEmail { get; set; }

        [BindProperty]
        public string TeacherPassword { get; set; }

        

        public async Task OnGetAsync(string email, string password)
        {

            TeacherEmail = email;
            TeacherPassword = password;
            Console.Write(TeacherEmail);
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = "select SectionID, OfferedCourseID, FacultyID from Section where FacultyID = @TeacherEmail";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@TeacherEmail", TeacherEmail);
                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                AssignedSections = new List<MarksDistributionTable>();

                while(await reader.ReadAsync())
                {

                    // getting teacher's name from faculty email

                    string teacher_mail = reader.GetString(2);
                    Console.Write($"{teacher_mail}");
                    string nested_query = "select FName, LName from users where Username = @TeacherEmail";

                    SqlCommand cmd2 = new SqlCommand(nested_query, connection);
                    cmd2.Parameters.AddWithValue("@TeacherEmail", TeacherEmail);
                    SqlDataReader reader2 = await cmd2.ExecuteReaderAsync();
                    string teacher_name = "";
                    if (reader2.Read()) {
                         teacher_name = reader2.GetString(0);
                        teacher_name += " ";
                        teacher_name += reader2.GetString(1);
                    }
                    cmd2.Dispose();
                    reader2.Close();
                    // getting course title using course id
                  
                    
                    string course_id = reader.GetString(1);
                    string nestedQuery2 = "select CourseName from Course where CourseCode = @course_id";
                    SqlCommand cmd3 = new SqlCommand(nestedQuery2, connection);
                    cmd3.Parameters.AddWithValue("@course_id", course_id);
                    SqlDataReader reader3 = await cmd3.ExecuteReaderAsync();
                    string course_name = "";
                    if (reader3.Read())
                    {
                        course_name = reader3.GetString(0);

                    }
                    cmd3.Dispose();
                    reader3.Close();
                

                    MarksDistributionTable table = new MarksDistributionTable {
                        CourseCode = reader.GetString(1),
                        TeacherName = teacher_name,
                        SectionName = reader.GetString(0),
                        CourseTitle = course_name



                    };




                    AssignedSections.Add(table);

                }

                reader.Close();
                cmd.Dispose();

                connection.Close();
            }

            
        }
    }
}

// ibrahim was here
// another run after running
