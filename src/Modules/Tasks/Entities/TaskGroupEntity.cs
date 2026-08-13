namespace Tasks.Entities;

public class TaskGroup
{
    public Guid Id {get; set;}
    public string Title {get; set;} = string.Empty;
    public Guid ProfileId {get; set;}
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
    public DateTime UpdatedAt {get; set;} = DateTime.UtcNow;

}