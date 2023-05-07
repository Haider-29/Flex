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


        public class OfferedCourse { 
            public string CourseID { get; set; }
            public string CourseTitle { get; set; }
            public int CreditHours { get; set; }

            public string Semester { get; set; }
        
        }


        public class Evaluations
        {
            public string courseCode { get; set; }
            public string evaluationType { get; set; }
            public int evalNum { get; set; }
            public int evalWeightage { get; set; }
            public int obtained_marks { get; set; }
            public int min { get; set; }
            public int max { get; set; }

            public int total_marks { get; set; }
            public int average { get; set; }

        }


        public class EnrolledCourse
        {
            public string id { get; set; }
            public string name { get; set; }
        }


        public List<Attendance> courseAttendances { get; set; }
        public List<EnrolledCourse> enrolledCourses { get; set; }

        public List<OfferedCourse> offeredCourses { get; set; }
        public List<Evaluations> allEvaluations { get; set; }

        public Student currStudent { get; set; }

        [BindProperty]
        public string StudentId { get; set; }

        [BindProperty]
        public string StudentPassword { get; set; }

        [BindProperty] 

        public string registerMessage { get; set; }

        [BindProperty]
        public string RegisterCourseID { get; set; }

        public async Task<IActionResult> OnPostRegisterForCourseAsync()
        {

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // attendance ids
                string q = "SELECT TOP 1 RegistrationID FROM Registration ORDER BY RegistrationID DESC";
                SqlCommand cmd = new SqlCommand(q, connection);

                SqlDataReader reader = await cmd.ExecuteReaderAsync();
                int resgisterID = 1;

                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        resgisterID = reader.GetInt32(0) + 1;
                        break;
                    }
                }

                reader.Close();
                cmd.Dispose();
                await OnGetAsync(StudentId, StudentPassword);
                string id = RegisterCourseID.Substring(0, RegisterCourseID.IndexOf(" "));


                q = "Select OfferedCourseID, Semester, CourseName, CreditHours from Offered_Course inner join Course on Offered_Course.OfferedCourseID = Course.CourseCode";
                SqlCommand cmd8 = new SqlCommand(q, connection);
                reader = await cmd8.ExecuteReaderAsync();

                offeredCourses = new List<OfferedCourse>();

                while (await reader.ReadAsync())
                {

                    string offerID = reader.GetString(0);
                    string sem = reader.GetString(1);
                    string offerName = reader.GetString(2);
                    int hours = reader.GetInt32(3);

                    OfferedCourse currOffer = new OfferedCourse
                    {
                        CourseID = offerID,
                        CourseTitle = offerName,
                        CreditHours = hours,
                        Semester = sem

                    };

                    offeredCourses.Add(currOffer);
                }


                reader.Close();
                cmd8.Dispose();
                // find semester according to RegisterCourseID and offeredCoursesList
                // Assume that you have a List<OfferedCourse> object called "offeredCourses" and a string variable called "courseId" containing the course ID you want to find the semester for
                string semester = offeredCourses.FirstOrDefault(c => c.CourseID == id)?.Semester;

                q = "INSERT INTO REGISTRATION (RegistrationID, StudentID, OffCourseID, Semester, APPROVED) VALUES(@RegistrationID, @StudentID, @OffCourseID, @Semester, @APPROVED);";
                SqlCommand cmd2 = new SqlCommand(q, connection);
                cmd2.Parameters.AddWithValue("@RegistrationID", resgisterID);
                cmd2.Parameters.AddWithValue("@StudentID", HttpContext.Session.GetString("StudentID"));
                cmd2.Parameters.AddWithValue("@OffCourseID", id);
                cmd2.Parameters.AddWithValue("@Semester", semester);
                cmd2.Parameters.AddWithValue("@APPROVED", 0);



                try
                {
                    int rowsAffected = await cmd2.ExecuteNonQueryAsync();
                    if (rowsAffected > 0)
                    {
                        registerMessage = "Course Applied for Successfully!";
                    }
                } catch (SqlException ex) {

                    if (ex.Number == 2627)
                    {
                        // Primary key violation (duplicate email)
                        registerMessage = "The course has already been applied";
                    }
                    else
                    {
                        // Other SQL errors
                        registerMessage = "An error occurred during registration. Please try again.";
                    }

                }

                cmd2.Dispose();
                connection.Close();
            }


            await OnGetAsync(StudentId, StudentPassword);
            return Page();




        }
        [HttpGet]

            public async Task OnGetAsync(string id, string password)
        {

            StudentId = id;
            StudentPassword = password;
            StudentId = HttpContext.Session.GetString("StudentID");
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
                query = "select * from attendance where StudentID = @StudentId ORDER BY Date ASC";
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

                allEvaluations = new List<Evaluations>();
                query = "SELECT E.*, M.Score FROM Evaluation E INNER JOIN Marks M ON E.EvaluationID = M.EvaluationID WHERE M.StudentID = @StudentId";
                SqlCommand cmd6 = new SqlCommand(query, conn);
                cmd6.Parameters.AddWithValue("@StudentId", StudentId);

                reader = await cmd6.ExecuteReaderAsync();
    
                int evalID;
                while(await reader.ReadAsync())
                {

                    int avg = 0;
                    int minNum = 0;
                    int maxNum = 0;
                    evalID = reader.GetInt32(0);
                    string nested_query = "SELECT MIN(Score) AS MinMarks, MAX(Score) AS MaxMarks, AVG(Score) AS AvgMarks FROM Marks WHERE EvaluationID = @EvaluationID;";
                    SqlCommand cmd7 = new SqlCommand(nested_query, conn);
                    cmd7.Parameters.AddWithValue("@EvaluationID", evalID);
                    
                    SqlDataReader reader7 = await cmd7.ExecuteReaderAsync();

                    while(await reader7.ReadAsync())
                    {

                        minNum = reader7.GetInt32(0);
                        maxNum = reader7.GetInt32(1);
                        avg = reader7.GetInt32(2);
                        break;

                    }


                    reader7.Close();
                    cmd7.Dispose();
                    string evalType = reader.GetString(2);
                    string evalCourseID = reader.GetString(3);
                    int evalNumber = reader.GetInt32(4);
                    int evalWeightage = reader.GetInt32(5);
                    int maxMarks = reader.GetInt32(6);
                    int score = reader.GetInt32(7);

                    Evaluations evaluations = new Evaluations
                    {
                        average = avg,
                        max = maxNum,
                        min = minNum,
                        courseCode = evalCourseID,
                        evalNum = evalNumber, evalWeightage = evalWeightage,
                        obtained_marks = score,
                        total_marks = maxMarks,
                        evaluationType = evalType,
                        

                    };

                    allEvaluations.Add(evaluations);

                }

                reader.Close();
                cmd6.Dispose();


                query = "Select OfferedCourseID, Semester, CourseName, CreditHours from Offered_Course inner join Course on Offered_Course.OfferedCourseID = Course.CourseCode";
                SqlCommand cmd8 = new SqlCommand(query, conn);
                reader = await cmd8.ExecuteReaderAsync();

                offeredCourses = new List<OfferedCourse>();

                while (await reader.ReadAsync())
                {

                    string offerID = reader.GetString(0);
                    string sem = reader.GetString(1);
                    string offerName = reader.GetString(2);
                    int hours = reader.GetInt32(3);

                    OfferedCourse currOffer = new OfferedCourse { 
                        CourseID = offerID,
                        CourseTitle = offerName,
                        CreditHours = hours,
                        Semester = sem
                    
                    };

                    offeredCourses.Add(currOffer);
                }


                reader.Close();
                cmd8.Dispose();


            }

        }
    }
}
