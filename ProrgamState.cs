using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace TaskManager;

[Serializable]
internal class ProgramState
{
    public uint _last_task_id = 0;
    public uint _last_group_id = 0;
    public uint _last_subtask_id = 0;

    public Dictionary<uint, Task> _tasks = new Dictionary<uint, Task>();
    public Dictionary<uint, TaskGroup> _taskgroups = new Dictionary<uint, TaskGroup>();

    public Dictionary<uint, List<Task>> tasks_in_groups = new Dictionary<uint, List<Task>>();
    public Dictionary<uint, List<SubTask>> subtasks_in_tasks = new Dictionary<uint, List<SubTask>>();

    

    public void save(string filepath)
    {
        BinaryFormatter b = new BinaryFormatter();
        using (Stream stream = File.Open(filepath, FileMode.Create))
            b.Serialize(stream, this);
    }

    public void load(string filepath)
    {
        BinaryFormatter b = new BinaryFormatter();
        ProgramState other = new ProgramState();
        using (Stream stream = File.Open(filepath, FileMode.Open))
            other = (ProgramState)b.Deserialize(stream);
        _last_task_id     =  other._last_task_id;
        _last_group_id    =  other._last_group_id;
        _last_subtask_id  =  other._last_subtask_id;
        _tasks            =  other._tasks;
        _taskgroups       =  other._taskgroups;
        tasks_in_groups   =  other.tasks_in_groups;
        subtasks_in_tasks =  other.subtasks_in_tasks;
    }
}