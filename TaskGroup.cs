namespace TaskManager;

[Serializable]
class TaskGroup : TasksGeneric
{
    public TaskGroup(uint id, string summary) : base(id, summary)
    {
    }

    public new string ToString()
    {
        return string.Format("Task-group ({0}tg) {1}" ,Identificator.ToString(),Summary);
    }
}