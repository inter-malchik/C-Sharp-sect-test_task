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

    public bool IsTimeLimited()
    {
        return time_limited;
    }

    public new DateTime GetTimeLimit()
    {
        return time_limit;
    }

    public void GiveTimeLimit(DateTime limit)
    {
        time_limit = limit;
        time_limited = true;
    }

    public new void MarkDone()
    {
        done = true;
        time_limited = false;
    }


    public new string ToString()
    {
        if (!time_limited)
            return string.Format("[{0}] ({1}ts) {2}", ((done) ? "X" : " "), Identificator.ToString(), Summary);

        return string.Format("[{0}] {1} ({2}ts) {3}", ((done) ? "X" : " "), time_limit.ToString(),
            Identificator.ToString(), Summary);
    }
}