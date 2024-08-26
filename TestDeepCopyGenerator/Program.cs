using TestDeepCopyGenerator.Models;


var student = new StudentDto
{
    LastName = "Lincoln",
    Age = 25,
    CourseNumber = 5
};

var copiedStudent = student.DeepCopy();

Console.WriteLine($"FN: {student.FirstName}, LN: {student.LastName}, Age: {student.Age}, CN: {student.CourseNumber}");
Console.WriteLine($"FN: {copiedStudent.FirstName}, LN: {copiedStudent.LastName}, Age: {copiedStudent.Age}, CN: {copiedStudent.CourseNumber}");

copiedStudent.FirstName = "upd";
copiedStudent.LastName = "upd";
copiedStudent.Age = 1;
copiedStudent.CourseNumber = 1;

Console.WriteLine($"FN: {student.FirstName}, LN: {student.LastName}, Age: {student.Age}, CN: {student.CourseNumber}");
Console.WriteLine($"FN: {copiedStudent.FirstName}, LN: {copiedStudent.LastName}, Age: {copiedStudent.Age}, CN: {copiedStudent.CourseNumber}");

Console.ReadLine();