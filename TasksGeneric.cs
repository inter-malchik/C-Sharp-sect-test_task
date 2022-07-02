namespace TaskManager;

[Serializable]
class TasksGeneric
{
    protected uint Identificator;
    protected string Summary;
    protected bool done;

    public TasksGeneric(uint id, string name)
    {
        Identificator = id;
        Summary = name;
        done = false;
    }

    public uint GetId()
    {
        return Identificator;
    }

    public string GetSummary()
    {
        return Summary;
    }

    public bool IsDone()
    {
        return done;
    }

    public void MarkDone()
    {
        done = true;
    }

    public bool IsTimeTimited()
    {
        return false;
    }

    public DateTime GetTimeLimit()
    {
        return DateTime.MinValue;
    }
}