using System;
using System.Data.SqlClient;
using System.Reflection;
using System.Reflection.PortableExecutable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using OfficeOpenXml;

namespace FLEXX.Pages
{

    public class ACourse
    {
        public string OffCourseID { get; set; }
        public string SSID { get; set; }
    }

    public class AFaculty
    {
        public string Username { get; set; }
        public string FName { get; set; }
        public string LName { get; set; }
    }

    public class Registration
    {
        public int RegistrationID { get; set; }
        public string StudentID { get; set; }
        public string OffCourseID { get; set; }
        public string Semester { get; set; }
        public string AcademicID { get; set; }
        public int Approved { get; set; }
    }


    public class Course
    {
        public string CourseCode { get; set; }
        public string CourseName { get; set; }
        public int CreditHours { get; set; }
        public string CoursePrereq { get; set; }
        public int Applications { get; set; }
        public string Semester { get; set; }
    }

    public class RegisteredStudent
    {
        public int RegistrationID { get; set; }
        public string StudentID { get; set; }
        public string OffCourseID { get; set; }
        public string Semester { get; set; }
        public string AcademicID { get; set; }
        public int Approved { get; set; }
    }


    public class AcademicInterfaceModel : PageModel
    {

        private readonly IConfiguration _configuration;

        public AcademicInterfaceModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public string OfficerEmail { get; set; }

        [BindProperty]
        public string OfficerPassword { get; set; }

        [BindProperty]
        public string NCourseName { get; set; }

        [BindProperty]

        public string NCourseCode { get; set; }

        [BindProperty]

        public string NCoursePrereq { get; set; }

        [BindProperty]

        public string NCreditHours { get; set; }

        [BindProperty]

        public string Message { get; set; }


        [BindProperty]
        public int RegistrationID { get; set; }

        [BindProperty]

        public string Message3 { get; set; }

        [BindProperty]
        public string FillCode { get; set; }

        // Add the bind properties
        [BindProperty]
        public string ACourse { get; set; }

        [BindProperty]
        public string AFaculty { get; set; }

        [BindProperty]

        public string Message4 { get; set; }

        // Other properties and methods go here

        // Add the OnPostAssignFacultyAsync method
        public async Task<IActionResult> OnPostAssignFacultyAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            Console.WriteLine("Selected ACourse: " + ACourse);
            Console.WriteLine("Selected AFaculty: " + AFaculty);

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                // Update the FacultyID of the corresponding section
                string updateQuery = "UPDATE Section SET FacultyID = @FacultyID WHERE SectionID = @SectionID";
                SqlCommand updateCmd = new SqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@FacultyID", AFaculty);
                updateCmd.Parameters.AddWithValue("@SectionID", ACourse);

                await updateCmd.ExecuteNonQueryAsync();
                updateCmd.Dispose();
            }

            Message4 = "Faculty Updated!";

            await OnGetAsync(OfficerEmail, OfficerPassword);
            return Page();
        }
    

    public async Task<IActionResult> OnPostFillSectionsAsync()
        {
            // 1. Fetch the students who are registered but not in a section for the provided CourseCode
            // 2. Loop through the students and perform the following steps:
            //    a. Check if there's an existing section with available slots for the course
            //    b. If yes, insert the student into that section
            //    c. If not, create a new section and insert the student into that section
            // 3. Save the changes to the database

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                string query = "SELECT R.RegistrationID, R.StudentID, R.OffCourseID, R.Semester, R.APPROVED FROM Registration R LEFT JOIN student_section SS ON R.StudentID = SS.STUDENTID AND R.OffCourseID = SS.sectionid WHERE SS.STUDENTID IS NULL AND R.APPROVED = 1 and R.OffCourseID = @FillCode";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@FillCode", FillCode);

                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                sectionStudents = new List<RegisteredStudent>();

                while (await reader.ReadAsync())
                {
                    RegisteredStudent r = new RegisteredStudent
                    {
                        RegistrationID = reader.GetInt32(0),
                        StudentID = reader.GetString(1),
                        OffCourseID = reader.GetString(2),
                        Semester = reader.GetString(3),
                        Approved = reader.GetInt32(4)
                    };

                    sectionStudents.Add(r);
                }

                reader.Close();
                cmd.Dispose();

                foreach (var student in sectionStudents)
                {

                    string sectionQuery = "SELECT TOP 1 S.SectionID, COUNT(SS.STUDENTID) as StudentCount FROM Section S LEFT JOIN student_section SS ON S.SectionID = SS.sectionid WHERE S.OfferedCourseID = @OffCourseID GROUP BY S.SectionID HAVING COUNT(SS.STUDENTID) < @MaxCapacity";
                    SqlCommand sectionCmd = new SqlCommand(sectionQuery, conn);
                    sectionCmd.Parameters.AddWithValue("@OffCourseID", student.OffCourseID);
                    sectionCmd.Parameters.AddWithValue("@MaxCapacity", 50); // Change this number according to the maximum capacity you want

                    SqlDataReader sectionReader = await sectionCmd.ExecuteReaderAsync();

                    string sectionID;

                    if(await sectionReader.ReadAsync()) { 

                        sectionID = sectionReader.GetString(0);

                    }

                    else
                    {

                        sectionReader.Close();
                        sectionCmd.Dispose();

                        string newSectionID = "S" + (new Random().Next(100000, 999999)).ToString();
                        string insertSectionQuery = "INSERT INTO Section (SectionID, OfferedCourseID, FacultyID) VALUES (@SectionID, @OfferedCourseID, @FacultyID)";
                        SqlCommand insertSectionCmd = new SqlCommand(insertSectionQuery, conn);
                        insertSectionCmd.Parameters.AddWithValue("@SectionID", newSectionID);
                        insertSectionCmd.Parameters.AddWithValue("@OfferedCourseID", student.OffCourseID);
                        insertSectionCmd.Parameters.AddWithValue("@FacultyID", DBNull.Value);

                        await insertSectionCmd.ExecuteNonQueryAsync();
                        insertSectionCmd.Dispose();

                        sectionID = newSectionID;

                    }

                    sectionReader.Close();
                    sectionCmd.Dispose();

                    string waitingStudentsQuery = "SELECT COUNT(*) FROM Registration R LEFT JOIN student_section SS ON R.StudentID = SS.STUDENTID AND R.OffCourseID = SS.sectionid WHERE SS.STUDENTID IS NULL AND R.APPROVED = 1 AND R.OffCourseID = @OffCourseID AND R.Semester = @Semester";
                    SqlCommand waitingStudentsCmd = new SqlCommand(waitingStudentsQuery, conn);
                    waitingStudentsCmd.Parameters.AddWithValue("@OffCourseID", student.OffCourseID);
                    waitingStudentsCmd.Parameters.AddWithValue("@Semester", student.Semester);

                    int waitingStudents = (int)await waitingStudentsCmd.ExecuteScalarAsync();
                    waitingStudentsCmd.Dispose();

                    string maxidquery = "select max(ssid) from student_section";
                    SqlCommand maxidCMD = new SqlCommand(maxidquery, conn);

                    int maxID;
                    object result = await maxidCMD.ExecuteScalarAsync();
                    maxidCMD.Dispose();
                    if (result == DBNull.Value)
                    {
                        maxID = 1;
                    }
                    else
                    {
                        maxID = (int)result + 1;
                    }

                    if (waitingStudents >= 1)
                    {
                        string insertStudentQuery = "INSERT INTO student_section (SSID, sectionid, STUDENTID) VALUES (@SSID, @SectionID, @StudentID)";
                        SqlCommand insertStudentCmd = new SqlCommand(insertStudentQuery, conn);
                        insertStudentCmd.Parameters.AddWithValue("@SSID", maxID); // Generate a unique SSID
                        insertStudentCmd.Parameters.AddWithValue("@SectionID", sectionID);
                        insertStudentCmd.Parameters.AddWithValue("@StudentID", student.StudentID);

                        await insertStudentCmd.ExecuteNonQueryAsync();
                        insertStudentCmd.Dispose();
                    }

                }


                return RedirectToPage();
            }

        } 

        public async Task<IActionResult> OnPostAddApplicationAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = "INSERT INTO Offered_Course Values(@CourseCode, 'Spring 2023')";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CourseCode", CourseCode);

                try
                {
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    cmd.Dispose();
                    conn.Close();

                    if (rowsAffected > 0)
                    {
                        // Insertion was successful
                        Message2 = "Course offered successfully!";
                    }
                    else
                    {
                        // Insertion failed
                        Message2 = "Failed to add application.";
                    }
                }
                catch (SqlException ex)
                {
                    cmd.Dispose();
                    conn.Close();

                    if (ex.Number == 2627)
                    {
                        // Primary key violation (duplicate CourseId)
                        Message2 = "The course has already been added.";
                    }
                    else
                    {
                        // Other SQL errors
                        Message2 = "An error occurred during the process. Please try again.";
                    }
                }
            }

            // Refresh the page to display the updated data
            await OnGetAsync(OfficerEmail, OfficerPassword);
            return Page();
        }

        [BindProperty]
        public string CourseCode { get; set; }

        [BindProperty]

        public string Message2 { get; set; }

        public async Task<IActionResult> OnPostApproveRegistrationAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = "UPDATE Registration SET APPROVED = 1 WHERE APPROVED = 0 AND RegistrationID = @RegistrationID;";
                SqlCommand cmd = new SqlCommand(query, conn);
                Console.WriteLine(OfficerEmail);
                Console.WriteLine("fucku");
                cmd.Parameters.AddWithValue("@RegistrationID", RegistrationID);


                try
                {
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    cmd.Dispose();
                    conn.Close();

                    if (rowsAffected > 0)
                    {
                        // Insertion was successful
                        Message3 = "Approved successfully!";
                    }
                    else
                    {
                        // Insertion failed
                        Message3 = "Failed to add application.";
                    }
            }
                catch (SqlException ex)
            {
                cmd.Dispose();
                conn.Close();

                if (ex.Number == 2627)
                {
                    // Primary key violation (duplicate CourseId)
                    Message3 = "The course has already been added.";
                }
                else
                {
                    // Other SQL errors
                    Message3 = "An error occurred during the process. Please try again.";
                }
            }
        }

            // Refresh the page to display the updated data
            await OnGetAsync(OfficerEmail, OfficerPassword);
            return Page();
        }



        public async Task<IActionResult> OnPostNewCourseAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                SqlCommand cmd;
                string query = "Insert into course values(@NCourseCode,@NCourseName,@NCreditHours,@NCoursePrereq, 0)";

                cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@NCreditHours", NCreditHours);
                cmd.Parameters.AddWithValue("@NCourseName", NCourseName);
                cmd.Parameters.AddWithValue("@NCourseCode", NCourseCode);

                if (string.IsNullOrEmpty(NCoursePrereq))
                {
                    cmd.Parameters.AddWithValue("@NCoursePrereq", DBNull.Value);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@NCoursePrereq", NCoursePrereq);
                }

                Console.WriteLine(NCreditHours);
                Console.WriteLine(query);

                try
                {
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();

                    cmd.Dispose();
                    conn.Close();

                    if (rowsAffected > 0)
                    {
                        // Insertion was successful
                        Message = "Course adding successful!";
                        await OnGetAsync(OfficerEmail, OfficerPassword);
                        return Page(); // Redirect to login page or any other page you'd like
                    }
                    else
                    {
                        // Insertion failed
                        Message = "Course Registration Failed";
                        await OnGetAsync(OfficerEmail, OfficerPassword);
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
                        Message = "The course already exists";
                    }
                    else
                    {
                        // Other SQL errors
                        Message = "An error occurred during registration. Please try again.";
                    }

                    await OnGetAsync(OfficerEmail, OfficerPassword);
                    return Page(); // Stay on the same page and show the error message
                }
            }
        }

        public List<Course> Courses { get; set; }
        public List<Course> AllCourses { get; set; }
        public List<Registration> approvalRequests { get; set; }
        public List<RegisteredStudent> sectionStudents { get; set; }    

        public List<ACourse> aCourses { get; set; }

        public List<AFaculty> aFaculty { get; set; }

        public class CourseModel
        {
            public string CourseCode { get; set; }
            public string CourseName { get; set; }
            public int CreditHours { get; set; }
        }
        public IDictionary<string, IList<CourseModel>> CoursesBySemester { get; set; }
        public IDictionary<string, IList<StudentModel>> StudentsBySection { get; set; }

        public class StudentModel
        {
            public int RegistrationNo { get; set; }
            public string StudentName { get; set; }
        }

        public IList<CourseAllocationModel> CourseAllocations { get; set; }

        public class CourseAllocationModel
        {
            public string CourseCode { get; set; }
            public string CourseName { get; set; }
            public int CreditHours { get; set; }
            public IList<CourseSectionModel> Sections { get; set; }
        }

        public class CourseSectionModel
        {
            public string SectionID { get; set; }
            public string InstructorName { get; set; }
        }


        public async Task<IActionResult> OnPostGenerateCourseAllocationReportAsync()
        {
            CourseAllocations = new List<CourseAllocationModel>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = @"
            SELECT oc.OfferedCourseID, c.CourseName, c.CreditHours, s.SectionID, u.FName + ' ' + u.LName as InstructorName
            FROM Offered_Course oc
            INNER JOIN Section s ON oc.OfferedCourseID = s.OfferedCourseID
            INNER JOIN users u ON s.FacultyID = u.Username
            INNER JOIN Course c ON oc.OfferedCourseID = c.CourseCode
            ORDER BY oc.OfferedCourseID, s.SectionID";

                SqlCommand command = new SqlCommand(query, conn);
                SqlDataReader reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    string courseCode = reader.GetString(0);
                    string courseName = reader.GetString(1);
                    int creditHours = reader.GetInt32(2);
                    string sectionId = reader.GetString(3);
                    string instructorName = reader.GetString(4);

                    // Check if we already have a course allocation for this course code
                    CourseAllocationModel courseAllocation = CourseAllocations
                        .FirstOrDefault(c => c.CourseCode == courseCode);

                    if (courseAllocation == null)
                    {
                        // If not, create a new course allocation
                        courseAllocation = new CourseAllocationModel
                        {
                            CourseCode = courseCode,
                            CourseName = courseName,
                            CreditHours = creditHours,
                            Sections = new List<CourseSectionModel>()
                        };
                        CourseAllocations.Add(courseAllocation);
                    }

                    // Add the section to the course allocation
                    courseAllocation.Sections.Add(new CourseSectionModel
                    {
                        SectionID = sectionId,
                        InstructorName = instructorName
                    });
                }


                reader.Close();
                command.Dispose();

                conn.Close();
            }


            using (var package = new ExcelPackage())
            {
                var sheet = package.Workbook.Worksheets.Add("Course Allocations");
                sheet.Cells["A1"].Value = "Course Code";
                sheet.Cells["B1"].Value = "Course Name";
                sheet.Cells["C1"].Value = "Credit Hours";
                sheet.Cells["D1"].Value = "Section ID";
                sheet.Cells["E1"].Value = "Instructor Name";

                var row = 2;
                foreach (var allocation in CourseAllocations)
                {
                    var courseCode = allocation.CourseCode;
                    var courseName = allocation.CourseName;
                    var creditHours = allocation.CreditHours;

                    foreach (var section in allocation.Sections)
                    {
                        var sectionID = section.SectionID;
                        var instructorName = section.InstructorName;

                        sheet.Cells[$"A{row}"].Value = courseCode;
                        sheet.Cells[$"B{row}"].Value = courseName;
                        sheet.Cells[$"C{row}"].Value = creditHours;
                        sheet.Cells[$"D{row}"].Value = sectionID;
                        sheet.Cells[$"E{row}"].Value = instructorName;

                        row++;
                    }
                }

                // Convert the Excel package to a byte array
                byte[] fileContents = package.GetAsByteArray();

                // Set the content type and file name for the response
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                var fileName = "CourseAllocationReport.xlsx";
                await OnGetAsync(OfficerEmail, OfficerPassword);
                // Return the Excel file as a download
                return File(fileContents, contentType, fileName);
            }

        }


        public async Task<IActionResult> OnPostGenerateStudentSectionsReportAsync()
        {
            StudentsBySection = new Dictionary<string, IList<StudentModel>>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = @"
    SELECT s.SectionID, r.RegistrationID, u.FName, u.LName
    FROM student_section ss
    INNER JOIN Section s ON ss.sectionid = s.SectionID
    INNER JOIN Registration r ON ss.STUDENTID = r.StudentID
    INNER JOIN Users u ON r.StudentID = u.username
    ORDER BY r.RegistrationID";

                SqlCommand command = new SqlCommand(query, conn);
                SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string section = reader.GetString(0);
                    if (!StudentsBySection.ContainsKey(section))
                    {
                        StudentsBySection[section] = new List<StudentModel>();
                    }

                    StudentsBySection[section].Add(new StudentModel
                    {
                        RegistrationNo = reader.GetInt32(1),
                        StudentName = reader.GetString(2) + ' ' + reader.GetString(3),

                    });
                }


                reader.Close();
                command.Dispose();
                conn.Close();
            }
            using (var package = new ExcelPackage())
            {
                foreach (var sectionName in StudentsBySection.Keys)
                {
                    var students = StudentsBySection[sectionName];

                    var sheet = package.Workbook.Worksheets.Add(sectionName);
                    sheet.Cells["A1"].Value = "Registration No";
                    sheet.Cells["B1"].Value = "Student Name";

                    var row = 2;
                    foreach (var student in students)
                    {
                        sheet.Cells[$"A{row}"].Value = student.RegistrationNo;
                        sheet.Cells[$"B{row}"].Value = student.StudentName;
                        row++;
                    }
                }

                // Convert the Excel package to a byte array
                byte[] fileContents = package.GetAsByteArray();

                // Set the content type and file name for the response
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                var fileName = "StudentSectionsReport.xlsx";
                await OnGetAsync(OfficerEmail, OfficerPassword);
                // Return the Excel file as a download
                return File(fileContents, contentType, fileName);
            }
        }


        public async Task<IActionResult> OnPostGenerateOfferedCoursesReportAsync()
        {
            CoursesBySemester = new Dictionary<string, IList<CourseModel>>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                string query = @"SELECT oc.Semester, c.CourseCode, c.CourseName, c.CreditHours
            FROM Offered_Course oc
            INNER JOIN Course c ON oc.OfferedCourseID = c.CourseCode
            ORDER BY oc.Semester";

                SqlCommand command = new SqlCommand(query, conn);
                SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string semester = reader.GetString(0);
                    if (!CoursesBySemester.ContainsKey(semester))
                    {
                        CoursesBySemester[semester] = new List<CourseModel>();
                    }

                    CoursesBySemester[semester].Add(new CourseModel
                    {
                        CourseCode = reader.GetString(1),
                        CourseName = reader.GetString(2),
                        CreditHours = reader.GetInt32(3)
                    });
                }

                reader.Close();
                command.Dispose();
                conn.Close();
            }
            using (var package = new ExcelPackage())
            {
                foreach (var semesterName in CoursesBySemester.Keys)
                {
                    var courses = CoursesBySemester[semesterName];

                    var sheet = package.Workbook.Worksheets.Add(semesterName);
                    sheet.Cells["A1"].Value = "Course Code";
                    sheet.Cells["B1"].Value = "Course Name";
                    sheet.Cells["C1"].Value = "Credit Hours";

                    var row = 2;
                    foreach (var course in courses)
                    {
                        sheet.Cells[$"A{row}"].Value = course.CourseCode;
                        sheet.Cells[$"B{row}"].Value = course.CourseName;
                        sheet.Cells[$"C{row}"].Value = course.CreditHours;
                        row++;
                    }
                }

                // Convert the Excel package to a byte array
                byte[] fileContents = package.GetAsByteArray();

                // Set the content type and file name for the response
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                var fileName = "OfferedCoursesReport.xlsx";
                await OnGetAsync(OfficerEmail, OfficerPassword);
                // Return the Excel file as a download
                return File(fileContents, contentType, fileName);
            }

        }
        public async Task OnGetAsync(string email, string password)
        {

            OfficerEmail = email;
            OfficerPassword = password;

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                string query = "select Offered_Course.OfferedCourseID, Course.CourseName, course.CreditHours, Course.PreRequisiteID, course.Applications, Offered_Course.Semester from Offered_Course inner join Course on course.CourseCode = Offered_Course.OfferedCourseID";
                SqlCommand cmd = new SqlCommand(query, conn);

                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                Courses = new List<Course>();

                while (await reader.ReadAsync())
                {
                    Course course = new Course
                    {
                        CourseCode = reader.GetString(0),
                        CourseName = reader.GetString(1),
                        CreditHours = reader.GetInt32(2),
                        CoursePrereq = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Applications = reader.GetInt32(4),
                        Semester = reader.GetString(5)
                    };

                    Courses.Add(course);
                }

                reader.Close();
                cmd.Dispose();

                string query2 = "select coursecode, coursename, credithours, applications from course";
                SqlCommand cmd2 = new SqlCommand(query2, conn);

                SqlDataReader reader2 = await cmd2.ExecuteReaderAsync();

                AllCourses = new List<Course>();

                while (await reader2.ReadAsync())
                {
                    Course course = new Course
                    {
                        CourseCode = reader2.GetString(0),
                        CourseName = reader2.GetString(1),
                        CreditHours = reader2.GetInt32(2),
                        Applications = reader2.GetInt32(3)
                    };

                    AllCourses.Add(course);
                }

                reader2.Close();
                cmd.Dispose();

                string query3 = "select registrationID, studentID, offcOurseid, semester, approved from registration where approved = 0";
                SqlCommand cmd3 = new SqlCommand(query3, conn);

                SqlDataReader reader3 = await cmd3.ExecuteReaderAsync();

                approvalRequests = new List<Registration>();

                while (await reader3.ReadAsync())
                {
                    Registration r = new Registration
                    {
                        RegistrationID = reader3.GetInt32(0),
                        StudentID = reader3.GetString(1),
                        OffCourseID = reader3.GetString(2),
                        Semester = reader3.GetString(3),
                        Approved = reader3.GetInt32(4)
                    };

                    approvalRequests.Add(r);
                }

                reader3.Close();
                cmd.Dispose();

                string query4 = "select Section.SectionID, section.OfferedCourseID from section where FacultyID is null";
                SqlCommand cmd4 = new SqlCommand(query4, conn);

                SqlDataReader reader4 = await cmd4.ExecuteReaderAsync();

                aCourses = new List<ACourse>();

                while (await reader4.ReadAsync())
                {
                    ACourse acourse = new ACourse
                    {
                        SSID = reader4.GetString(0),
                        OffCourseID = reader4.GetString(1)
                    };

                    aCourses.Add(acourse);
                }

                reader4.Close();
                cmd4.Dispose();

                string query5 = "SELECT U.Username, U.FName, U.LName, COUNT(S.SectionID) as SectionCount FROM users U LEFT JOIN Section S ON U.Username = S.FacultyID WHERE U.Role = 'F' GROUP BY U.Username, U.FName, U.LName HAVING COUNT(S.SectionID) < 3;";
                SqlCommand cmd5 = new SqlCommand(query5, conn);

                SqlDataReader reader5 = await cmd5.ExecuteReaderAsync();

                aFaculty = new List<AFaculty>();

                while (await reader5.ReadAsync())
                {
                    AFaculty afac = new AFaculty
                    {
                        Username = reader5.GetString(0),
                        FName = reader5.GetString(1),
                        LName = reader5.GetString(2)
                    };

                    aFaculty.Add(afac);
                }

                reader5.Close();
                cmd5.Dispose();


                CoursesBySemester = new Dictionary<string, IList<CourseModel>>();

                query = @"SELECT oc.Semester, c.CourseCode, c.CourseName, c.CreditHours
            FROM Offered_Course oc
            INNER JOIN Course c ON oc.OfferedCourseID = c.CourseCode
            ORDER BY oc.Semester";

                SqlCommand command = new SqlCommand(query, conn);
                reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string semester = reader.GetString(0);
                    if (!CoursesBySemester.ContainsKey(semester))
                    {
                        CoursesBySemester[semester] = new List<CourseModel>();
                    }

                    CoursesBySemester[semester].Add(new CourseModel
                    {
                        CourseCode = reader.GetString(1),
                        CourseName = reader.GetString(2),
                        CreditHours = reader.GetInt32(3)
                    });
                }

                reader.Close();
                command.Dispose();


                StudentsBySection = new Dictionary<string, IList<StudentModel>>();

                query = @"
    SELECT s.SectionID, r.RegistrationID, u.FName, u.LName
    FROM student_section ss
    INNER JOIN Section s ON ss.sectionid = s.SectionID
    INNER JOIN Registration r ON ss.STUDENTID = r.StudentID
    INNER JOIN Users u ON r.StudentID = u.username
    ORDER BY r.RegistrationID";

                command = new SqlCommand(query, conn);
                reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string section = reader.GetString(0);
                    if (!StudentsBySection.ContainsKey(section))
                    {
                        StudentsBySection[section] = new List<StudentModel>();
                    }

                    StudentsBySection[section].Add(new StudentModel
                    {
                        RegistrationNo = reader.GetInt32(1),
                        StudentName = reader.GetString(2) + ' ' + reader.GetString(3),

                    });
                }


                reader.Close();
                command.Dispose();


                CourseAllocations = new List<CourseAllocationModel>();

                query = @"
            SELECT oc.OfferedCourseID, c.CourseName, c.CreditHours, s.SectionID, u.FName + ' ' + u.LName as InstructorName
            FROM Offered_Course oc
            INNER JOIN Section s ON oc.OfferedCourseID = s.OfferedCourseID
            INNER JOIN users u ON s.FacultyID = u.Username
            INNER JOIN Course c ON oc.OfferedCourseID = c.CourseCode
            ORDER BY oc.OfferedCourseID, s.SectionID";

                command = new SqlCommand(query, conn);
                reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    string courseCode = reader.GetString(0);
                    string courseName = reader.GetString(1);
                    int creditHours = reader.GetInt32(2);
                    string sectionId = reader.GetString(3);
                    string instructorName = reader.GetString(4);

                    // Check if we already have a course allocation for this course code
                    CourseAllocationModel courseAllocation = CourseAllocations
                        .FirstOrDefault(c => c.CourseCode == courseCode);

                    if (courseAllocation == null)
                    {
                        // If not, create a new course allocation
                        courseAllocation = new CourseAllocationModel
                        {
                            CourseCode = courseCode,
                            CourseName = courseName,
                            CreditHours = creditHours,
                            Sections = new List<CourseSectionModel>()
                        };
                        CourseAllocations.Add(courseAllocation);
                    }

                    // Add the section to the course allocation
                    courseAllocation.Sections.Add(new CourseSectionModel
                    {
                        SectionID = sectionId,
                        InstructorName = instructorName
                    });
                }


                reader.Close();
                command.Dispose();

                conn.Close();
            }
        }


    }
}
