namespace TaskManager;

[Serializable]
class Task : TasksGeneric
{
    public Task(uint id, string summary) : base(id, summary)
    {
        time_limited = false;
    }

    public Task(uint id, string summary, DateTime tl) : base(id, summary)
    {
        time_limited = true;
        time_limit = tl;
    }

    private bool time_limited;
    private DateTime time_limit;

    public bool is_time_limited()
    {
        return time_limited;
    }

    public new DateTime get_time_limit()
    {
        return time_limit;
    }

    public void give_time_limit(DateTime limit)
    {
        time_limit = limit;
        time_limited = true;
    }

    public new void mark_done()
    {
        done = true;
        time_limited = false;
    }


    public new string ToString()
    {
        if (!time_limited)
            return string.Format("[{0}] ({1}ts) {2}", ((done) ? "X" : " "), identificator.ToString(), summary);

        return string.Format("[{0}] {1} ({2}ts) {3}", ((done) ? "X" : " "), time_limit.ToString(),
            identificator.ToString(), summary);
    }
}