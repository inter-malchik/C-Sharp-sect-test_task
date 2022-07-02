namespace TaskManager;

[Serializable]
class TasksGeneric
{
    protected uint identificator;
    protected string summary;
    protected bool done;

    public TasksGeneric(uint id, string name)
    {
        identificator = id;
        summary = name;
        done = false;
    }

    public uint get_id()
    {
        return identificator;
    }

    public string get_summary()
    {
        return summary;
    }

    public bool is_done()
    {
        return done;
    }

    public void mark_done()
    {
        done = true;
    }

    public bool is_time_timited()
    {
        return false;
    }

    public DateTime get_time_limit()
    {
        return DateTime.MinValue;
    }
}