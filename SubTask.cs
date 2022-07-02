namespace TaskManager;

[Serializable]
class SubTask : TasksGeneric
{
    public SubTask(uint id, string summary) : base(id, summary)
    {
    }

    public new string ToString()
    {
        return string.Format("[{0}] ({1}st) {2}", ((done) ? "X" : " "), Identificator.ToString(), Summary);
    }
}