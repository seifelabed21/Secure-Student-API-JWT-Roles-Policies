using Microsoft.AspNetCore.Mvc; 
//using StudentApi.Models;
//using StudentApi.DataSimulation;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using StudentAPIBusinessLayer;
using StudentDataAccessLayer;
using StudentApi.Security;

namespace StudentApi.Controllers 
{
    [ApiController] // Marks the class as a Web API controller with enhanced features.
  //  [Route("[controller]")] // Sets the route for this controller to "students", based on the controller name.
    [Route("api/Students")]
    [Authorize]
    public class StudentsController : ControllerBase // Declare the controller class inheriting from ControllerBase.
    {
        private readonly AuditService _audit;

        public StudentsController(AuditService audit)
        {
            _audit = audit;
        }

        [HttpGet("All", Name ="GetAllStudents")] // Marks this method to respond to HTTP GET requests.
        [Authorize(Roles = "Student,Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]

        //here we used StudentDTO
        public ActionResult<IEnumerable<StudentDTO>> GetAllStudents() // Define a method to get all students.
        {
            //if (StudentDataSimulation.StudentsList.Count == 0) 
            //{
            //    return NotFound("No Students Found!");
            //}
            //return Ok(StudentDataSimulation.StudentsList); // Returns the list of students.

            List<StudentDTO> StudentsList = StudentAPIBusinessLayer.Student.GetAllStudents();
            if (StudentsList.Count == 0)
            {
                return NotFound("No Students Found!");
            }
            return Ok(StudentsList); // Returns the list of students.

        }

        [HttpGet("Passed",Name = "GetPassedStudents")]
        [Authorize(Roles = "Student,Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]

        // Method to get all students who passed
        public ActionResult<IEnumerable<StudentDTO>> GetPassedStudents()

        {
            // var passedStudents = StudentDataSimulation.StudentsList.Where(student => student.Grade >= 50).ToList();

            List<StudentDTO> PassedStudentsList = StudentAPIBusinessLayer.Student.GetPassedStudents();
            if (PassedStudentsList.Count == 0)
            {
                return NotFound("No Students Found!");
            }

            return Ok(PassedStudentsList); // Return the list of students who passed.
        }

        [HttpGet("AverageGrade", Name = "GetAverageGrade")]
        [Authorize(Roles = "Student,Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]

        public ActionResult<double> GetAverageGrade()
        {
            //var averageGrade = StudentDataSimulation.StudentsList.Average(student => student.Grade);
            double  averageGrade = StudentAPIBusinessLayer.Student.GetAverageGrade();
            return Ok(averageGrade);
        }


        [HttpGet("{id}", Name = "GetStudentById")]
        [Authorize(Policy = "StudentOwnerOrAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]

        public ActionResult<StudentDTO> GetStudentById(int id)
        {

            if (id < 1)
            {
                return BadRequest($"Not accepted ID {id}");
            }

            //var student = StudentDataSimulation.StudentsList.FirstOrDefault(s => s.Id == id);
            //if (student == null)
            //{
            //    return NotFound($"Student with ID {id} not found.");
            //}
            StudentAPIBusinessLayer.Student student = StudentAPIBusinessLayer.Student.Find(id);

            if (student == null)
            {
                return NotFound($"Student with ID {id} not found.");
            }

            //here we get only the DTO object to send it back.
            StudentDTO SDTO = student.SDTO;

            //we return the DTO not the student object.
            return Ok(SDTO);

        }

        //for add new we use Http Post
        
        [HttpPost(Name = "AddStudent")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<StudentDTO> AddStudent(StudentDTO newStudentDTO)
        {
            //we validate the data here
            if (newStudentDTO == null || string.IsNullOrEmpty(newStudentDTO.Name) || newStudentDTO.Age < 0 || newStudentDTO.Grade < 0)
            {
                return BadRequest("Invalid student data.");
            }

          //newStudent.Id = StudentDataSimulation.StudentsList.Count > 0 ? StudentDataSimulation.StudentsList.Max(s => s.Id) + 1 : 1;

          StudentAPIBusinessLayer.Student student = new StudentAPIBusinessLayer.Student( new StudentDTO(newStudentDTO.Id, newStudentDTO.Name, newStudentDTO.Age, newStudentDTO.Grade ) ) ;
            student.Save();

            newStudentDTO.Id = student.ID;
            _audit.Add(
                User.Identity?.Name,
                "AddStudent",
                $"Student/{newStudentDTO.Id}",
                $"Created student {newStudentDTO.Name}");

            //we return the DTO only not the full student object
            //we dont return Ok here,we return createdAtRoute: this will be status code 201 created.
            return CreatedAtRoute("GetStudentById", new { id = newStudentDTO.Id }, newStudentDTO);

        }

       

        //here we use http put method for update
        [HttpPut("{id}", Name = "UpdateStudent")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<StudentDTO> UpdateStudent(int id, StudentDTO updatedStudent)
        {
            if (id < 1 || updatedStudent == null || string.IsNullOrEmpty(updatedStudent.Name) || updatedStudent.Age < 0 || updatedStudent.Grade < 0)
            {
                return BadRequest("Invalid student data.");
            }

            //var student = StudentDataSimulation.StudentsList.FirstOrDefault(s => s.Id == id);

            StudentAPIBusinessLayer.Student student = StudentAPIBusinessLayer.Student.Find(id);
          

            if (student == null)
            {
                return NotFound($"Student with ID {id} not found.");
            }


            student.Name = updatedStudent.Name;
            student.Age = updatedStudent.Age;
            student.Grade = updatedStudent.Grade;
            student.Save();
            _audit.Add(
                User.Identity?.Name,
                "UpdateStudent",
                $"Student/{id}",
                $"Updated student {student.Name}");

            //we return the DTO not the full student object.
            return Ok(student.SDTO);

        }


        //here we use HttpDelete method
        [HttpDelete("{id}", Name = "DeleteStudent")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult DeleteStudent(int id)
        {
            if (id < 1)
            {
                return BadRequest($"Not accepted ID {id}");
            }

            // var student = StudentDataSimulation.StudentsList.FirstOrDefault(s => s.Id == id);
            // StudentDataSimulation.StudentsList.Remove(student);

           if(  StudentAPIBusinessLayer.Student.DeleteStudent(id))
            {
                _audit.Add(
                    User.Identity?.Name,
                    "DeleteStudent",
                    $"Student/{id}",
                    $"Deleted student {id}");
                return Ok($"Student with ID {id} has been deleted.");
            }
            else
                return NotFound($"Student with ID {id} not found. no rows deleted!");
        }

    }
}
