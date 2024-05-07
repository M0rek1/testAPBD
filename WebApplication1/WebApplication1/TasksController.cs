using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly DataBaseContext _dataBaseContext;

    public TasksController(DataBaseContext dataBaseContext)
    {
        _dataBaseContext = dataBaseContext;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        try
        {
            dynamic teamMember = null;
            List<dynamic> assignedTasks = new List<dynamic>();
            List<dynamic> createdTasks = new List<dynamic>();

            using (var connection = _dataBaseContext.Connection)
            {
           
                
                using (var command = new SqlCommand("SELECT FirstName, LastName, Email FROM TeamMember WHERE IdTeamMember = @TeamMemberId", connection))
                {
                    command.Parameters.AddWithValue("@TeamMemberId", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            teamMember = new
                            {
                                FirstName = reader["FirstName"],
                                LastName = reader["LastName"],
                                Email = reader["Email"]
                            };
                        }
                        else
                        {
                            return NotFound($"No team member found with ID {id}.");
                        }
                    }
                }

              
                using (var command = new SqlCommand(@"
                SELECT t.Name, t.Description, t.Deadline, p.Name AS ProjectName, tt.Name AS TaskTypeName
                FROM Task t
                JOIN Project p ON t.IdProject = p.IdProject
                JOIN TaskType tt ON t.IdTaskType = tt.IdTaskType
                WHERE t.IdAssignedTo = @TeamMemberId
                ORDER BY t.Deadline DESC", connection))
                {
                    command.Parameters.AddWithValue("@TeamMemberId", id);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            assignedTasks.Add(new
                            {
                                TaskName = reader["Name"],
                                Description = reader["Description"],
                                Deadline = reader["Deadline"],
                                ProjectName = reader["ProjectName"],
                                TaskType = reader["TaskTypeName"]
                            });
                        }
                    }
                }
                            
                using (var command = new SqlCommand(@"
                SELECT t.Name, t.Description, t.Deadline, p.Name AS ProjectName, tt.Name AS TaskTypeName
                FROM Task t
                JOIN Project p ON t.IdProject = p.IdProject
                JOIN TaskType tt ON t.IdTaskType = tt.IdTaskType
                WHERE t.IdCreator = @TeamMemberId
                ORDER BY t.Deadline DESC", connection))
                {
                    command.Parameters.AddWithValue("@TeamMemberId", id);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            createdTasks.Add(new
                            {
                                TaskName = reader["Name"],
                                Description = reader["Description"],
                                Deadline = reader["Deadline"],
                                ProjectName = reader["ProjectName"],
                                TaskType = reader["TaskTypeName"]
                            });
                        }
                    }
                }
            }

            var result = new
            {
                TeamMember = teamMember,
                AssignedTasks = assignedTasks,
                CreatedTasks = createdTasks
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
          
            return StatusCode(500, "Internal Server Error: " + ex.Message);
        }
    }


    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteById([FromRoute] int id)
    {
        try
        {
            using (var connection = _dataBaseContext.Connection)
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                      
                        using (var checkCommand =
                               new SqlCommand("SELECT COUNT(1) FROM Project WHERE IdProject = @ProjectId", connection,
                                   transaction))
                        {
                            checkCommand.Parameters.AddWithValue("@ProjectId", id);
                            int exists = (int)checkCommand.ExecuteScalar();
                            if (exists == 0)
                            {
                                transaction.Rollback();
                                return NotFound($"No project found with ID {id}.");
                            }
                        }

                       
                        using (var commandDeleteTasks = new SqlCommand("DELETE FROM Task WHERE IdProject = @ProjectId",
                                   connection, transaction))
                        {
                            commandDeleteTasks.Parameters.AddWithValue("@ProjectId", id);
                            commandDeleteTasks.ExecuteNonQuery();
                        }

                  
                        using (var commandDeleteProject =
                               new SqlCommand("DELETE FROM Project WHERE IdProject = @ProjectId", connection,
                                   transaction))
                        {
                            commandDeleteProject.Parameters.AddWithValue("@ProjectId", id);
                            commandDeleteProject.ExecuteNonQuery();
                        }
                        transaction.Commit(); 
                        return Ok($"Project with ID {id} and all associated tasks have been deleted.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); 
                        return StatusCode(500,
                            "Internal Server Error: Could not delete the project and its tasks - " + ex.Message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Internal Server Error: " + ex.Message);
        }
    }
}