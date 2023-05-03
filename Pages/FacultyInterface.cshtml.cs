using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace FLEXX.Pages
{


    public class MarksDistributionTable {
        public string? CourseCode { get; set; }
        public string? CourseTitle { get; set; }
        public string? SectionName { get; set; }

        public string? TeacherName { get; set; }


    }

    public class Student
    {
        public string? RollNum { get; set; }

        public string? Name { get; set; }

        public string? section { get; set; }

    }


    public class studentAttendance {
    
        public string? RollNum { get; set;}
        public string? Name { get; set;}
        public string? Section { get; set;}
        public DateTime? Date { get; set;}
        public string? Status { get; set;}
    }




    public class FacultyInterfaceModel : PageModel
    {

        private readonly IConfiguration _configuration;

        public FacultyInterfaceModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]

        public string Message { get; set; }

        public List<MarksDistributionTable> AssignedSections { get; set; }

        public List<Student> Students { get; set; }
        [BindProperty]
        public string TeacherEmail { get; set; }

        [BindProperty]
        public string TeacherPassword { get; set; }

        [BindProperty]

        public string NSectionID { get; set; }

        [BindProperty]
        public string NEvaluationType { get; set; }

        [BindProperty]

        public Int32 NMaxMarks { get; set; }

        [BindProperty]

        public Int32 NWeightage { get; set; }



        [BindProperty]

        public List<studentAttendance> Attendances { get; set; }    = new List<studentAttendance>();

        public async Task<IActionResult> OnPostAddEvaluationAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString)) { 
            
                await connection.OpenAsync();

                // attendance ids
                string q = "SELECT TOP 1 EvaluationID FROM Evaluation ORDER BY EvaluationID DESC";
                SqlCommand cmd = new SqlCommand(q, connection);

                SqlDataReader reader = await cmd.ExecuteReaderAsync();
                int evaluationID = 1;

                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        evaluationID = reader.GetInt32(0) + 1;
                        break;
                    }
                }

                reader.Close();
                cmd.Dispose();

                string query = "Insert into Evaluation (EvaluationID, SectionID, EvaluationType, Weightage, MaxMarks) " +
                    "VALUES (@EvaluationID, @NSectionID, @NEvaluationType, @NWeightage, @NMaxMarks);";
                SqlCommand insertEvaluation = new SqlCommand(query, connection);
                insertEvaluation.Parameters.AddWithValue("@NsectionID", NSectionID);
                insertEvaluation.Parameters.AddWithValue("@NEvaluationType", NEvaluationType);
                insertEvaluation.Parameters.AddWithValue("@NWeightage", NWeightage);
                insertEvaluation.Parameters.AddWithValue("@NMaxMarks", NMaxMarks);
                insertEvaluation.Parameters.AddWithValue("@EvaluationID", evaluationID);
                try
                {

                    int rowsEffected = await insertEvaluation.ExecuteNonQueryAsync();
                    insertEvaluation.Dispose();
                    connection.Close();
                    if (rowsEffected > 0)
                    {
                        Message = "Evaluation Added!";
                        await OnGetAsync(TeacherEmail, TeacherPassword);
                        return Page();
                    }
                } catch(SqlException sqlEx)
                {
                    insertEvaluation.Dispose();
                    connection.Close();

                    Message = "An error occured while adding evaluation";
                    await OnGetAsync(TeacherEmail, TeacherPassword);
                    return Page();
                }

                await OnGetAsync(TeacherEmail, TeacherPassword);
                return Page();

            }
        }

        public async Task<IActionResult> OnPostAddAttendanceAsync(string[] studentIds, string sectionId, DateTime date, string[] statuses)
        {


            Console.Write("Section: ", sectionId);
            Console.Write("Date: ", date);
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                // attendance ids
                string q = "SELECT TOP 1 AttendanceID FROM Attendance ORDER BY AttendanceID DESC";
                SqlCommand cmd = new SqlCommand(q, connection);

                SqlDataReader reader = await cmd.ExecuteReaderAsync();
                int attendanceID = 1;

                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        attendanceID = reader.GetInt32(0) + 1;
                        break;
                    }
                }

                reader.Close();
                cmd.Dispose();

                // insert attendance records
                string query = "INSERT INTO Attendance (AttendanceID, StudentID, SectionID, Date, Status) " +
                "VALUES (@AttendanceID, @StudentID, @SectionID, @Date, @Status)";
                SqlCommand cmd2 = new SqlCommand(query, connection);

                for (int i = 0; i < studentIds.Length; i++)
                {
                    cmd2.Parameters.Clear();
                    cmd2.Parameters.AddWithValue("@AttendanceID", attendanceID);
                    cmd2.Parameters.AddWithValue("@StudentID", studentIds[i]);
                    cmd2.Parameters.AddWithValue("@SectionID", sectionId);
                    cmd2.Parameters.AddWithValue("@Date", date);
                    cmd2.Parameters.AddWithValue("@Status", statuses[i]);

                    try
                    {
                        int rowsAffected = await cmd2.ExecuteNonQueryAsync();

                        cmd2.Dispose();
                        connection.Close();

                        if (rowsAffected > 0)
                        {
                            // Insertion was successful
                            Message = "Attendance adding successful!";
                            await OnGetAsync(TeacherEmail, TeacherPassword);
                            return Page(); // Redirect to login page or any other page you'd like
                        }
                        else
                        {
                            // Insertion failed
                            Message = "Attendance Adding Failed";
                            await OnGetAsync(TeacherEmail, TeacherPassword);
                            return Page(); // Stay on the same page and show the error message
                        }
                    }
                    catch (SqlException ex)
                    {
                        cmd2.Dispose();
                        connection.Close();

                        if (ex.Number == 2627)
                        {
                            // Primary key violation (duplicate email)
                            Message = "The course already exists";
                        }
                        else
                        {
                            // Other SQL errors
                            Message = "An error occurred during registration. Please try again.";
                        }

                        await OnGetAsync(TeacherEmail, TeacherPassword);
                        return Page(); // Stay on the same page and show the error message
                    }

                }
            }
            await OnGetAsync(TeacherEmail, TeacherPassword);
            return Page();
        }




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

                string q2 = "SELECT u.Username, u.FName, u.LName, ss.sectionid FROM student_section AS ss JOIN users AS u ON ss.STUDENTID = u.Username WHERE u.Role = 'S';";
                SqlCommand cum = new SqlCommand(q2, connection);
                SqlDataReader readStudents = await cum.ExecuteReaderAsync();
                Students = new List<Student>();
                while (await  readStudents.ReadAsync())
                {
                    Student student = new Student()
                    {
                        Name = readStudents.GetString(1) + " " + readStudents.GetString(2),
                        RollNum = readStudents.GetString(0),
                        // section soch bsdk
                        section = readStudents.GetString(2)

                    };
                    Students.Add(student);
                }
                // get students of a sections
                readStudents.Close();
                cum.Dispose();
                connection.Close();
            }

            
        }
    }
}


