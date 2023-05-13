using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Linq;
using OfficeOpenXml;
using System.IO;
using System.Globalization;

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


    public class StudentOnSection
    {
        public string? section { get; set; }
        public List<Student> students { get; set; }
    }


    public class studentAttendance {
    
        public string? RollNum { get; set;}
        public string? Name { get; set;}
        public string? Section { get; set;}
        public DateTime? Date { get; set;}
        public string? Status { get; set;}

        public string? course_id { get; set;}
        public int? credit_hours { get; set;}
    }




    public class FacultyInterfaceModel : PageModel
    {

        private readonly IConfiguration _configuration;

        public FacultyInterfaceModel(IConfiguration configuration)
        {
            _configuration = configuration;
            AssignedSections = new List<MarksDistributionTable>();
            Students = new List<Student>();
        }

        public class StudentMark
        {
            public int MarksObtained { get; set; }
            public int TotalMarks { get; set; }

            public string? id { get; set; }
        }

        [BindProperty]

        public string Message { get; set; }
        [BindProperty]
        public string SelectedEvaluationType { get; set; }
        [BindProperty]
        public int NEvaluationNumber { get; set; }
        [BindProperty]
        public List<StudentMark> StudentMarks { get; set; }



        public List<MarksDistributionTable> AssignedSections { get; set; }

        [BindProperty]
        public DateTime Date { get; set; }

        [BindProperty]
        public int ContactHours { get; set; }

        [BindProperty]

        public string attendanceCourseID { get; set; }

        [BindProperty]
        public string[] StudentIds { get; set; }

        [BindProperty]
        public string[] Statuses { get; set; }

        [BindProperty]
        public string SectionId { get; set; }

        public List<Student> Students { get; set; }

        public List<StudentOnSection> studentsInSecton { get; set; }
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

        public string SaveMarksMessage { get; set; }

        [BindProperty]

        public string NEvalCourseID { get; set; }



        [BindProperty]

        public List<studentAttendance> Attendances { get; set; } = new List<studentAttendance>();

        public async Task<IActionResult> OnPostAddEvaluationAsync()
        {

            string[] studentIds = StudentIds;
            string sectionId = SectionId;
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (SqlConnection connection = new SqlConnection(connectionString))
            {

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

                string query = "Insert into Evaluation (EvaluationID, SectionID, EvaluationType,Courseid, EvaluationNumber, Weightage, MaxMarks) " +
                    "VALUES (@EvaluationID, @NSectionID, @NEvaluationType, @NEvalCourseID, @NEvaluationNumber, @NWeightage, @NMaxMarks);";
                SqlCommand insertEvaluation = new SqlCommand(query, connection);
                insertEvaluation.Parameters.AddWithValue("@NsectionID", NSectionID);
                insertEvaluation.Parameters.AddWithValue("@NEvaluationType", NEvaluationType);
                insertEvaluation.Parameters.AddWithValue("@NEvalCourseID", NEvalCourseID);
                insertEvaluation.Parameters.AddWithValue("@NEvaluationNumber", NEvaluationNumber);
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
                }
                catch (SqlException sqlEx)
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

        // Look here Haider
        // ye function call hota wehn save marks button is pressed
        public async Task<IActionResult> OnPostAsync()
        {
            var studentIds = Request.Form["studentIds[]"].ToList();
            var marks = Request.Form["marks[]"].Select(x => int.Parse(x)).ToList();
            string sectionId = Request.Form["NSectionID"];
            string evaluationType = Request.Form["SelectedEvaluationType"];
            evaluationType = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(evaluationType);

            int evaluationNumber = int.Parse(Request.Form["NEvaluationNumber"]);
            List<StudentMark> studentMarks = StudentMarks;
            TeacherEmail = HttpContext.Session.GetString("UserName");
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string getEvaluationIdQuery = "SELECT EvaluationID FROM Evaluation WHERE SectionID = @SectionID AND EvaluationType = @EvaluationType AND EvaluationNumber = @EvaluationNumber";
                SqlCommand getEvaluationIdCmd = new SqlCommand(getEvaluationIdQuery, connection);
                getEvaluationIdCmd.Parameters.AddWithValue("@SectionID", sectionId);
                getEvaluationIdCmd.Parameters.AddWithValue("@EvaluationType", evaluationType);
                getEvaluationIdCmd.Parameters.AddWithValue("@EvaluationNumber", evaluationNumber);

                int evaluationId = 0;
                SqlDataReader reader = await getEvaluationIdCmd.ExecuteReaderAsync();

                if (reader.HasRows)
                {
                    while (await reader.ReadAsync())
                    {
                        evaluationId = reader.GetInt32(0);
                        break;
                    }
                }
                reader.Close();
                getEvaluationIdCmd.Dispose();

                if (evaluationId == 0)
                {
                    SaveMarksMessage = "Failed to find the EvaluationID. Please check the input data.";
                    await OnGetAsync(TeacherEmail, TeacherPassword);
                    return Page();
                }

                string query = "INSERT INTO Marks (MarksID, StudentID, EvaluationID, EvaluationType, Score) " +
                "VALUES (@MarksID, @StudentID, @EvaluationID, @EvaluationType, @Score)";
                SqlCommand cmd = new SqlCommand(query, connection);

                for (int i = 0; i < studentIds.Count; i++)
                {
                    string generateID = "Select TOP 1 MarksID from Marks ORDER BY MarksID desc";
                    SqlCommand idcommand = new SqlCommand(generateID, connection);
                    SqlDataReader idReader = await idcommand.ExecuteReaderAsync();
                    int marksID = 1;

                    if (idReader.HasRows)
                    {
                        while (await idReader.ReadAsync())
                        {
                            marksID = idReader.GetInt32(0) + 1;
                            break;
                        }
                    }

                    idReader.Close();
                    idcommand.Dispose();
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@MarksID", marksID);
                    cmd.Parameters.AddWithValue("@StudentID", studentIds[i]);
                    cmd.Parameters.AddWithValue("@EvaluationType", evaluationType);
                    cmd.Parameters.AddWithValue("@EvaluationID", evaluationId);
                    cmd.Parameters.AddWithValue("@Score", marks[i]);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    if (rowsAffected <= 0)
                    {
                        SaveMarksMessage = "Failed to save the marks for one or more students.";
                    }
                    else
                    {
                        SaveMarksMessage = "Marks saved!";
                    }
                }

                cmd.Dispose();
                connection.Close();
            }

            await OnGetAsync(TeacherEmail, TeacherPassword);
            return Page();
        }



        public async Task<IActionResult> OnPostAddAttendanceAsync()
        {

            string[] studentIds = StudentIds;
            string sectionId = SectionId;
            DateTime date = Date;
            string[] statuses = Statuses;

            string attendance_course_id = attendanceCourseID;
            int attCredit = ContactHours;


            Console.Write("Student ids: ", studentIds);
            Console.Write("Sextion Id: ", sectionId);
            Console.Write("Date: ", date);
            Console.Write("Statuses: ", statuses);





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
                string query = "INSERT INTO Attendance (AttendanceID, StudentID, CourseID, SectionID, CreditHours, Date, Status) " +
                "VALUES (@AttendanceID, @StudentID, @CourseID, @SectionID, @CreditHours, @Date, @Status)";
                SqlCommand cmd2 = new SqlCommand(query, connection);

                for (int i = 0; i < studentIds.Length; i++)
                {
                    cmd2.Parameters.Clear();
                    cmd2.Parameters.AddWithValue("@AttendanceID", attendanceID);
                    cmd2.Parameters.AddWithValue("@StudentID", studentIds[i]);
                    cmd2.Parameters.AddWithValue("@CourseID", attendance_course_id);

                    cmd2.Parameters.AddWithValue("@SectionID", sectionId);
                    cmd2.Parameters.AddWithValue("@CreditHours", attCredit);
                    cmd2.Parameters.AddWithValue("@Date", date);
                    cmd2.Parameters.AddWithValue("@Status", statuses[i]);

                    try
                    {
                        int rowsAffected = await cmd2.ExecuteNonQueryAsync();

                        if (rowsAffected <= 0)
                        {
                            // Insertion failed
                            Message = "Attendance Adding Failed";
                            break;
                        }
                    }
                    catch (SqlException ex)
                    {
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

                        break;
                    }

                    attendanceID++; // Increment attendanceID for the next record
                }

                cmd2.Dispose();
                connection.Close();

                // Refresh the page and show the message




            }

            await OnGetAsync(TeacherEmail, TeacherPassword);
            return Page();

        }

        public IList<AttendanceModel> AttendanceReport { get; set; } = new List<AttendanceModel>();

        public class AttendanceModel
        {
            public int RegistrationNo { get; set; }
            public string StudentName { get; set; }
            public int CreditHours { get; set; }
            public int TotalClasses { get; set; }
            public int AttendedClasses { get; set; }
            public double AttendancePercentage { get; set; }
        }

        public class GradeCountModel
        {
            public string Grade { get; set; }
            public int Count { get; set; }
        }


        public class EvaluationReportModel
        {
            public string StudentName { get; set; }
            public string EvaluationType { get; set; }
            public int EvaluationNumber { get; set; }
            public int MaxMarks { get; set; }
            public int Score { get; set; }
            public double Percentage { get; set; }
        }

        public List<EvaluationReportModel> EvaluationReport { get; set; } = new List<EvaluationReportModel>();

        public class GradeReportModel
        {
            public string RollNo { get; set; }
            public string Name { get; set; }
            public string Section { get; set; }
            public string Grade { get; set; }
        }
        public List<GradeReportModel> GradeReport { get; set; } = new List<GradeReportModel>();

        public List<GradeCountModel> GradeCountReport { get; set; } = new List<GradeCountModel>();
        public class FeedbackReportItem
        {
            public string StudentId { get; set; }
            public string FeedbackFormData { get; set; }
            public float AppearanceScore { get; set; }
            public float ProfessionalScore { get; set; }
            public float TeachingMethodsScore { get; set; }
            public float DispositionScore { get; set; }
            public float OverallScore { get; set; }
        }

        public class FeedbackReportViewModel
        {
            public List<FeedbackReportItem> FeedbackItems { get; set; }
        }


        public List<FeedbackReportViewModel> FeedbackReport { get; set; } = new List<FeedbackReportViewModel>();


        public async Task<IActionResult> OnPostGenerateFeedbackReportAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                // student feedbasck rfeport:
                string query = "SELECT StudentID, FeedbackFormData FROM Feedback WHERE FacultyID = @TeacherId;";
                SqlCommand cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@TeacherId", HttpContext.Session.GetString("UserName"));

                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                List<FeedbackReportItem> feedbackItems = new List<FeedbackReportItem>();

                while (await reader.ReadAsync())
                {
                    string studentId = reader.GetString(0);
                    string feedbackFormData = reader.GetString(1);

                    // Parse the feedback form data
                    string[] values = feedbackFormData.Split(',');

                    float appearanceScore = (float.Parse(values[0]) + float.Parse(values[1]) + float.Parse(values[2]) + float.Parse(values[3]) + float.Parse(values[4])) / 5;
                    float professionalScore = (float.Parse(values[5]) + float.Parse(values[6]) + float.Parse(values[7]) + float.Parse(values[8]) + float.Parse(values[9]) + float.Parse(values[10])) / 6;
                    float teachingMethodsScore = (float.Parse(values[11]) + float.Parse(values[12]) + float.Parse(values[13]) + float.Parse(values[14]) + float.Parse(values[15])) / 5;
                    float dispositionScore = (float.Parse(values[16]) + float.Parse(values[17]) + float.Parse(values[18]) + float.Parse(values[19])) / 4;
                    float overallScore = (appearanceScore + professionalScore + teachingMethodsScore + dispositionScore) / 4;

                    FeedbackReportItem item = new FeedbackReportItem
                    {
                        StudentId = studentId,
                        FeedbackFormData = feedbackFormData,
                        AppearanceScore = appearanceScore,
                        ProfessionalScore = professionalScore,
                        TeachingMethodsScore = teachingMethodsScore,
                        DispositionScore = dispositionScore,
                        OverallScore = overallScore
                    };

                    feedbackItems.Add(item);

                    FeedbackReportViewModel ok = new FeedbackReportViewModel
                    {
                        FeedbackItems = feedbackItems
                    };

                    FeedbackReport.Add(ok);
                }



                reader.Close();
                cmd.Dispose();

                connection.Close();
            }
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("FeedbackReport");

                // Set column headers
                worksheet.Cells[1, 1].Value = "Student ID";
                worksheet.Cells[1, 2].Value = "Feedback Form Data";
                worksheet.Cells[1, 3].Value = "Appearance Score";
                worksheet.Cells[1, 4].Value = "Professional Score";
                worksheet.Cells[1, 5].Value = "Teaching Methods Score";
                worksheet.Cells[1, 6].Value = "Disposition Score";
                worksheet.Cells[1, 7].Value = "Overall Score";

                // Populate data rows
                int row = 2;
                foreach (var report in FeedbackReport)
                {
                    foreach (var feedbackItem in report.FeedbackItems)
                    {
                        worksheet.Cells[row, 1].Value = feedbackItem.StudentId;
                        worksheet.Cells[row, 2].Value = feedbackItem.FeedbackFormData;
                        worksheet.Cells[row, 3].Value = feedbackItem.AppearanceScore;
                        worksheet.Cells[row, 4].Value = feedbackItem.ProfessionalScore;
                        worksheet.Cells[row, 5].Value = feedbackItem.TeachingMethodsScore;
                        worksheet.Cells[row, 6].Value = feedbackItem.DispositionScore;
                        worksheet.Cells[row, 7].Value = feedbackItem.OverallScore;
                        row++;
                    }
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();
            

                // Convert the Excel package to a byte array
                var excelFile = package.GetAsByteArray();

                // Download the Excel file
                var fileName = "FeedbackReport.xlsx";

                await OnGetAsync(TeacherEmail, TeacherPassword);
                return File(excelFile, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }


        // Look here Haider
        // OnGEtAsync may maybe issue ho with the way indexes are being handled for lists
        [HttpGet]
        public async Task OnGetAsync(string email, string password)
        {


            string[] studentIds = StudentIds;
            string sectionId = NSectionID;
            string evaluationType = SelectedEvaluationType;
            int evaluationNumber = NEvaluationNumber;
            List<StudentMark> studentMarks = StudentMarks;



            TeacherEmail = email;
            TeacherPassword = password;


            TeacherEmail = HttpContext.Session.GetString("UserName");


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

                while (await reader.ReadAsync())
                {

                    // getting teacher's name from faculty email

                    string teacher_mail = reader.GetString(2);
                    Console.Write($"{teacher_mail}");
                    string nested_query = "select FName, LName from users where Username = @TeacherEmail";

                    SqlCommand cmd2 = new SqlCommand(nested_query, connection);
                    cmd2.Parameters.AddWithValue("@TeacherEmail", TeacherEmail);
                    SqlDataReader reader2 = await cmd2.ExecuteReaderAsync();
                    string teacher_name = "";
                    if (reader2.Read())
                    {
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


                    MarksDistributionTable table = new MarksDistributionTable
                    {
                        CourseCode = reader.GetString(1),
                        TeacherName = teacher_name,
                        SectionName = reader.GetString(0),
                        CourseTitle = course_name



                    };




                    AssignedSections.Add(table);

                }


                Console.Write("Assigned Section:", AssignedSections);

                reader.Close();
                cmd.Dispose();

                string q2 = "SELECT u.Username, u.FName, u.LName FROM users u INNER JOIN student_section ss ON u.Username = ss.StudentID INNER JOIN Section s ON ss.SectionID = s.SectionID WHERE s.FacultyID = @TeacherEmail;";
                SqlCommand cum = new SqlCommand(q2, connection);
                cum.Parameters.AddWithValue("@TeacherEmail", TeacherEmail);
                SqlDataReader readStudents = await cum.ExecuteReaderAsync();
                Students = new List<Student>();
                StudentMarks = new List<StudentMark>();
                List<string> tempStudentIds = new List<string>(); // Add this line
                while (await readStudents.ReadAsync())
                {
                    Student student = new Student()
                    {
                        Name = readStudents.GetString(1) + " " + readStudents.GetString(2),
                        RollNum = readStudents.GetString(0),
                        // section soch bsdk
                        section = readStudents.GetString(2)

                    };
                    tempStudentIds.Add(readStudents.GetString(0)); // Add this line
                    Students.Add(student);
                }

                studentsInSecton = new List<StudentOnSection>();

                studentsInSecton = Students
    .GroupBy(s => s.section)
    .Select(g => new StudentOnSection
    {
        section = g.Key,
        students = g.ToList()
    })
    .ToList();


                StudentIds = tempStudentIds.ToArray(); // Add this line

                for (int i = 0; i < StudentIds.Length; i++)
                {
                    StudentMark studentMark = new StudentMark
                    {
                        id = StudentIds[i],
                    };


                    StudentMarks.Add(studentMark);


                }

                // get students of a sections
                readStudents.Close();
                cum.Dispose();

                AttendanceReport = new List<AttendanceModel>();

                query = @"

                    SELECT r.RegistrationID, u.FName + ' ' + u.LName as StudentName, c.CreditHours,
                           COUNT(*) as TotalClasses,
                           SUM(CASE WHEN a.Status = 'Present' THEN 1 ELSE 0 END) as AttendedClasses
                    FROM Attendance a
                    INNER JOIN Registration r ON a.StudentID = r.StudentID AND a.CourseID = r.OffCourseID
                    INNER JOIN student_section ss ON r.StudentID = ss.STUDENTID AND a.SectionID = ss.sectionid
                    INNER JOIN Section s ON a.SectionID = s.SectionID
                    INNER JOIN users u ON r.StudentID = u.Username
                    INNER JOIN Course c ON a.CourseID = c.CourseCode
                    WHERE s.FacultyID = @FacultyID
                    GROUP BY r.RegistrationID, u.FName, u.LName, c.CreditHours
                    ORDER BY r.RegistrationID

                ";



                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@FacultyID", TeacherEmail);




                reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    int registrationNo = reader.GetInt32(0);
                    string studentName = reader.GetString(1);
                    int creditHours = reader.GetInt32(2);
                    int totalClasses = reader.GetInt32(3);
                    int attendedClasses = reader.GetInt32(4);
                    double attendancePercentage = (double)attendedClasses / totalClasses * 100;

                    AttendanceReport.Add(new AttendanceModel
                    {
                        RegistrationNo = registrationNo,
                        StudentName = studentName,
                        CreditHours = creditHours,
                        TotalClasses = totalClasses,
                        AttendedClasses = attendedClasses,
                        AttendancePercentage = Math.Round(attendancePercentage, 2)
                    });
                }


                reader.Close();
                command.Dispose();
                query = @"
                    SELECT u.FName + ' ' + u.LName AS StudentName, 
                           m.EvaluationType, 
                           e.EvaluationNumber,
                           e.MaxMarks, 
                           m.Score AS Score, 
                           (CAST(m.Score AS FLOAT) / e.MaxMarks) * 100 AS Percentage
                    FROM Marks m
                    INNER JOIN Users u ON m.StudentID = u.Username
                    INNER JOIN Evaluation e ON m.EvaluationID = e.EvaluationID
                    INNER JOIN Section s ON e.SectionID = s.SectionID
                    INNER JOIN Offered_Course oc ON s.OfferedCourseID = oc.OfferedCourseID
                    WHERE s.FacultyID = @FacultyID
                    ORDER BY u.FName, u.LName, m.EvaluationType, e.EvaluationNumber
                ";
                command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@FacultyID", HttpContext.Session.GetString("UserName"));  // Set the Faculty ID
                reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    string studentName = reader.GetString(0);
                    string evt = reader.GetString(1);
                    int evn = reader.GetInt32(2);
                    int maxMarks = reader.GetInt32(3);
                    int score = reader.GetInt32(4);
                    double percentage = reader.GetDouble(5);

                    EvaluationReport.Add(new EvaluationReportModel
                    {
                        StudentName = studentName,
                        EvaluationType = evt,
                        EvaluationNumber = evn,
                        MaxMarks = maxMarks,
                        Score = score,
                        Percentage = Math.Round(percentage, 2)
                    });
                }

                reader.Close();
                command.Dispose();


                // student feedbasck rfeport:
                query = "SELECT StudentID, FeedbackFormData FROM Feedback WHERE FacultyID = @TeacherId;";
                cmd = new SqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@TeacherId", TeacherEmail);

                reader = await cmd.ExecuteReaderAsync();

                List<FeedbackReportItem> feedbackItems = new List<FeedbackReportItem>();

                while (await reader.ReadAsync())
                {
                    string studentId = reader.GetString(0);
                    string feedbackFormData = reader.GetString(1);

                    // Parse the feedback form data
                    string[] values = feedbackFormData.Split(',');

                    float appearanceScore = (float.Parse(values[0]) + float.Parse(values[1]) + float.Parse(values[2]) + float.Parse(values[3]) + float.Parse(values[4])) / 5;
                    float professionalScore = (float.Parse(values[5]) + float.Parse(values[6]) + float.Parse(values[7]) + float.Parse(values[8]) + float.Parse(values[9]) + float.Parse(values[10])) / 6;
                    float teachingMethodsScore = (float.Parse(values[11]) + float.Parse(values[12]) + float.Parse(values[13]) + float.Parse(values[14]) + float.Parse(values[15])) / 5;
                    float dispositionScore = (float.Parse(values[16]) + float.Parse(values[17]) + float.Parse(values[18]) + float.Parse(values[19])) / 4;
                    float overallScore = (appearanceScore + professionalScore + teachingMethodsScore + dispositionScore) / 4;

                    FeedbackReportItem item = new FeedbackReportItem
                    {
                        StudentId = studentId,
                        FeedbackFormData = feedbackFormData,
                        AppearanceScore = appearanceScore,
                        ProfessionalScore = professionalScore,
                        TeachingMethodsScore = teachingMethodsScore,
                        DispositionScore = dispositionScore,
                        OverallScore = overallScore
                    };

                    feedbackItems.Add(item);

                    FeedbackReportViewModel ok = new FeedbackReportViewModel
                    {
                        FeedbackItems = feedbackItems
                    };

                    FeedbackReport.Add(ok);
                }



                reader.Close();
                cmd.Dispose();

                connection.Close();
            }


        }

        public async Task<IActionResult> OnPostGenerateAttendanceReportAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                AttendanceReport = new List<AttendanceModel>();

                string query = @"

                    SELECT r.RegistrationID, u.FName + ' ' + u.LName as StudentName, c.CreditHours,
                           COUNT(*) as TotalClasses,
                           SUM(CASE WHEN a.Status = 'Present' THEN 1 ELSE 0 END) as AttendedClasses
                    FROM Attendance a
                    INNER JOIN Registration r ON a.StudentID = r.StudentID AND a.CourseID = r.OffCourseID
                    INNER JOIN student_section ss ON r.StudentID = ss.STUDENTID AND a.SectionID = ss.sectionid
                    INNER JOIN Section s ON a.SectionID = s.SectionID
                    INNER JOIN users u ON r.StudentID = u.Username
                    INNER JOIN Course c ON a.CourseID = c.CourseCode
                    WHERE s.FacultyID = @FacultyID
                    GROUP BY r.RegistrationID, u.FName, u.LName, c.CreditHours
                    ORDER BY r.RegistrationID

                ";



                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@FacultyID", HttpContext.Session.GetString("UserName"));




                SqlDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    int registrationNo = reader.GetInt32(0);
                    string studentName = reader.GetString(1);
                    int creditHours = reader.GetInt32(2);
                    int totalClasses = reader.GetInt32(3);
                    int attendedClasses = reader.GetInt32(4);
                    double attendancePercentage = (double)attendedClasses / totalClasses * 100;

                    AttendanceReport.Add(new AttendanceModel
                    {
                        RegistrationNo = registrationNo,
                        StudentName = studentName,
                        CreditHours = creditHours,
                        TotalClasses = totalClasses,
                        AttendedClasses = attendedClasses,
                        AttendancePercentage = Math.Round(attendancePercentage, 2)
                    });
                }


                reader.Close();
                command.Dispose();
                connection.Close();
            }
            using (var package = new ExcelPackage())
            {
                // Add a new worksheet to the package
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Attendance Report");

                // Set the header row values
                worksheet.Cells[1, 1].Value = "Registration No";
                worksheet.Cells[1, 2].Value = "Student Name";
                worksheet.Cells[1, 3].Value = "Credit Hours";
                worksheet.Cells[1, 4].Value = "Total Classes";
                worksheet.Cells[1, 5].Value = "Attended Classes";
                worksheet.Cells[1, 6].Value = "Attendance Percentage";

                // Loop through the AttendanceReport list and add the data to the worksheet
                int row = 2;
                foreach (AttendanceModel attendance in AttendanceReport)
                {
                    worksheet.Cells[row, 1].Value = attendance.RegistrationNo;
                    worksheet.Cells[row, 2].Value = attendance.StudentName;
                    worksheet.Cells[row, 3].Value = attendance.CreditHours;
                    worksheet.Cells[row, 4].Value = attendance.TotalClasses;
                    worksheet.Cells[row, 5].Value = attendance.AttendedClasses;
                    worksheet.Cells[row, 6].Value = attendance.AttendancePercentage;

                    row++;
                }

                // Convert the Excel package to a byte array
                byte[] fileContents = package.GetAsByteArray();
                await OnGetAsync(TeacherEmail, TeacherPassword);
                // Download the file
                return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "AttendanceReport.xlsx");
            }
        }


        public async Task<IActionResult> OnPostGenerateEvaluationReportAsync()
        {

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                string query = @"
                    SELECT u.FName + ' ' + u.LName AS StudentName, 
                           m.EvaluationType, 
                           e.EvaluationNumber,
                           e.MaxMarks, 
                           m.Score AS Score, 
                           (CAST(m.Score AS FLOAT) / e.MaxMarks) * 100 AS Percentage
                    FROM Marks m
                    INNER JOIN Users u ON m.StudentID = u.Username
                    INNER JOIN Evaluation e ON m.EvaluationID = e.EvaluationID
                    INNER JOIN Section s ON e.SectionID = s.SectionID
                    INNER JOIN Offered_Course oc ON s.OfferedCourseID = oc.OfferedCourseID
                    WHERE s.FacultyID = @FacultyID
                    ORDER BY u.FName, u.LName, m.EvaluationType, e.EvaluationNumber
                ";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@FacultyID", HttpContext.Session.GetString("UserName"));  // Set the Faculty ID
                SqlDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    string studentName = reader.GetString(0);
                    string evt = reader.GetString(1);
                    int evn = reader.GetInt32(2);
                    int maxMarks = reader.GetInt32(3);
                    int score = reader.GetInt32(4);
                    double percentage = reader.GetDouble(5);

                    EvaluationReport.Add(new EvaluationReportModel
                    {
                        StudentName = studentName,
                        EvaluationType = evt,
                        EvaluationNumber = evn,
                        MaxMarks = maxMarks,
                        Score = score,
                        Percentage = Math.Round(percentage, 2)
                    });
                }

                reader.Close();
                command.Dispose();
                connection.Close();
            }
            {
                using var package = new ExcelPackage();

                // Add a new worksheet to the workbook
                var worksheet = package.Workbook.Worksheets.Add("Evaluation Report");

                // Add column headers to the worksheet
                worksheet.Cells[1, 1].Value = "Student Name";
                worksheet.Cells[1, 2].Value = "Evaluation Type";
                worksheet.Cells[1, 3].Value = "Evaluation Number";
                worksheet.Cells[1, 4].Value = "Max Marks";
                worksheet.Cells[1, 5].Value = "Score";
                worksheet.Cells[1, 6].Value = "Percentage";

                // Loop through each EvaluationReportModel and add its data to the worksheet
                int rowIndex = 2;
                foreach (var evaluation in EvaluationReport)
                {
                    worksheet.Cells[rowIndex, 1].Value = evaluation.StudentName;
                    worksheet.Cells[rowIndex, 2].Value = evaluation.EvaluationType;
                    worksheet.Cells[rowIndex, 3].Value = evaluation.EvaluationNumber;
                    worksheet.Cells[rowIndex, 4].Value = evaluation.MaxMarks;
                    worksheet.Cells[rowIndex, 5].Value = evaluation.Score;
                    worksheet.Cells[rowIndex, 6].Value = evaluation.Percentage;

                    rowIndex++;
                }

                // Auto-fit the columns in the worksheet
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Convert the Excel package to a byte array
                byte[] fileContents = package.GetAsByteArray();

                // Download the file as an Excel file

                await OnGetAsync(TeacherEmail, TeacherPassword);
                return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "EvaluationReport.xlsx");
            }
        }


        public async Task<IActionResult> OnPostFilterSectionAsync()
        {
            // Get the selected course ID from the form
            string selectedSectionID = Request.Form["selectedSectionID"];
            TeacherEmail = HttpContext.Session.GetString("UserName");

            string query = "SELECT ss.grade AS Grade, COUNT(*) AS Count FROM student_section ss WHERE ss.sectionid = @SelectedSectionID GROUP BY ss.grade;";
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SelectedSectionID", selectedSectionID); 
                SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var reportRow = new GradeCountModel
                    {
                        Grade = reader.GetString(0),
                        Count = reader.GetInt32(1)
                    };

                    GradeCountReport.Add(reportRow);
                }

                reader.Close();
                command.Dispose();
                connection.Close();
            }


            using (var package = new ExcelPackage())
            {
                // Add a new worksheet to the package
                var worksheet = package.Workbook.Worksheets.Add("Grade Count Report");

                // Add headers to the worksheet
                worksheet.Cells[1, 1].Value = "Grade";
                worksheet.Cells[1, 2].Value = "Count";

                // Add data to the worksheet
                int rowIndex = 2;
                foreach (var gradeCount in GradeCountReport)
                {
                    worksheet.Cells[rowIndex, 1].Value = gradeCount.Grade;
                    worksheet.Cells[rowIndex, 2].Value = gradeCount.Count;
                    rowIndex++;
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Convert the package to a byte array
                byte[] fileContents = package.GetAsByteArray();
                await OnGetAsync(TeacherEmail, TeacherPassword);
                // Return the byte array as a file download
                return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "GradeCountReport.xlsx");
            }
          
        }

        public async Task<IActionResult> OnPostFilterCourseAsync()
        {
            // Get the selected course ID from the form
            string selectedCourse = Request.Form["selectedCourse"];
            TeacherEmail = HttpContext.Session.GetString("UserName");

            // Run the query with the selected course
            var query = @"

                SELECT u.Username AS RollNo, u.FName + ' ' + u.LName AS Name, 
                       s.SectionID AS Section, ss.grade AS Grade
                FROM student_section ss
                INNER JOIN Users u ON ss.STUDENTID = u.Username
                INNER JOIN Section s ON ss.sectionid = s.SectionID
                INNER JOIN Offered_Course oc ON s.OfferedCourseID = oc.OfferedCourseID
                WHERE s.FacultyID = @FacultyID AND oc.OfferedCourseID = @CourseID
                ORDER BY u.Username, s.SectionID

            ";


            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@FacultyID", TeacherEmail); // Set the Faculty ID
                command.Parameters.AddWithValue("@CourseID", selectedCourse); // Set the Course ID
                SqlDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var reportRow = new GradeReportModel
                    {
                        RollNo = reader.GetString(0),
                        Name = reader.GetString(1),
                        Section = reader.GetString(2),
                        Grade = reader.GetString(3)
                    };

                    GradeReport.Add(reportRow);
                }

                reader.Close();
                command.Dispose();

                connection.Close();

            } // Fetch the data and populate the GradeReport list
              // ...



            // Render the view with the updated data
            byte[] fileContents;

            // Create a new Excel package
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Grade Report");

            // Add headers to the worksheet
            worksheet.Cells[1, 1].Value = "Roll No";
            worksheet.Cells[1, 2].Value = "Name";
            worksheet.Cells[1, 3].Value = "Section";
            worksheet.Cells[1, 4].Value = "Grade";
            // Add data to the worksheet
            int row = 2;
            foreach (var grade in GradeReport)
            {
                worksheet.Cells[row, 1].Value = grade.RollNo;
                worksheet.Cells[row, 2].Value = grade.Name;
                worksheet.Cells[row, 3].Value = grade.Section;
                worksheet.Cells[row, 4].Value = grade.Grade;
                row++;
            }

            // Convert the Excel package to a byte array
            fileContents = package.GetAsByteArray();

            await OnGetAsync(TeacherEmail, TeacherPassword);
            return File(fileContents, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Grade Report.xlsx");

        }
    }

    }


