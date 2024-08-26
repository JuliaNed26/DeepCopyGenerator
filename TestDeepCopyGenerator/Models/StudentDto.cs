using DeepCopyGenerator;

namespace TestDeepCopyGenerator.Models;

[DeepCopiable]
public partial class StudentDto
{
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public int Age { get; set; }
    
    public int CourseNumber { get; set; }
}