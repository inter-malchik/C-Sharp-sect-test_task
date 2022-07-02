using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace TaskManager;

[Serializable]
internal class ProgramState
{
    public uint LastTaskId = 0;
    public uint LastGroupId = 0;
    public uint LastSubtaskId = 0;

    public Dictionary<uint, Task> Tasks = new Dictionary<uint, Task>();
    public Dictionary<uint, TaskGroup> TaskGroups = new Dictionary<uint, TaskGroup>();

    public Dictionary<uint, List<Task>> TasksInGroups = new Dictionary<uint, List<Task>>();
    public Dictionary<uint, List<SubTask>> SubtasksInTasks = new Dictionary<uint, List<SubTask>>();

    

    public void Save(string filepath)
    {
        BinaryFormatter b = new BinaryFormatter();
        using (Stream stream = File.Open(filepath, FileMode.Create))
            b.Serialize(stream, this);
    }

    public void Load(string filepath)
    {
        BinaryFormatter b = new BinaryFormatter();
        ProgramState other = new ProgramState();
        using (Stream stream = File.Open(filepath, FileMode.Open))
            other = (ProgramState)b.Deserialize(stream);
        LastTaskId     =  other.LastTaskId;
        LastGroupId    =  other.LastGroupId;
        LastSubtaskId  =  other.LastSubtaskId;
        Tasks            =  other.Tasks;
        TaskGroups       =  other.TaskGroups;
        TasksInGroups   =  other.TasksInGroups;
        SubtasksInTasks =  other.SubtasksInTasks;
    }
}