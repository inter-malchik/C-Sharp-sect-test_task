using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace TaskManager
{
    internal class Program
    {
        [Serializable]
        internal class TaskState
        {
            public uint _last_task_id = 0;
            public uint _last_group_id = 0;
            public uint _last_subtask_id = 0;

            public Dictionary<uint, Task> _tasks = new Dictionary<uint, Task>();
            public Dictionary<uint, TaskGroup> _taskgroups = new Dictionary<uint, TaskGroup>();

            public Dictionary<uint, List<Task>> tasks_in_groups = new Dictionary<uint, List<Task>>();
            public Dictionary<uint, List<SubTask>> subtasks_in_tasks = new Dictionary<uint, List<SubTask>>();
        }

        static private HashSet<string> _stop_words = new String[] { "/stop", "/shutdown", "/STOP" }.ToHashSet();
        static private HashSet<string> _help_words = new String[] { "/help", "/HELP" }.ToHashSet();

        static TaskState State = new TaskState();

        static void Main(string[] args)
        {
            Console.WriteLine("TaskManager is opened");

            Console.WriteLine("You can type \"/help\" to see available commands");
            while (true)
            {
                string[] command = Console.ReadLine().Split(' ');

                if (_stop_words.Contains(command[0]))
                    break;
                if (_help_words.Contains(command[0]))
                {
                    ShowHelp();
                    continue;
                }

                switch (command[0])
                {
                    case "/list-tasks":
                        ShowAllTasks(x => true);
                        break;
                    case "/show-undone":
                        ShowAllTasks(x => !x.done);
                        break;
                    case "/show-done":
                        ShowAllTasks(x => x.done);
                        break;
                    case "/show-today":
                        ShowTodayTasks();
                        break;
                    case "/create-task":
                        if (command.Length == 1)
                            CreateTask("");
                        else
                            CreateTask(String.Join(" ", command[1..]));
                        break;
                    case "/create-group":
                        if (command.Length == 1)
                            CreateGroup("");
                        else
                            CreateGroup(String.Join(" ", command[1..]));
                        break;
                    case "/add-task-to-group":
                        TaskToGroup(Convert.ToUInt32(command[1].Substring(0, command[1].Length - 2)),
                            Convert.ToUInt32(command[2].Substring(0, command[1].Length - 2)));
                        break;
                    case "/take-task-from-group":
                        try
                        {
                            TaskFromGroup(Convert.ToUInt32(command[1].Substring(0, command[1].Length - 2)),
                                Convert.ToUInt32(command[2].Substring(0, command[1].Length - 2)));
                        }
                        catch
                        {
                            Console.WriteLine("an error occured while extracting");
                        }

                        break;
                    case "/bind-subtask":
                        CreateSubTask(Convert.ToUInt32(command[1].Substring(0, command[1].Length - 2)),
                            (command.Length) == 2 ? "" : String.Join(" ", command[2..]));
                        break;
                    case "/mark-done":
                        string typin = command[1].Substring(command[1].Length - 2, 2);
                        switch (typin)
                        {
                            case "ts":
                                MarkTaskDone(Convert.ToUInt32(command[1].Substring(0, command[1].Length - 2)));
                                break;
                            case "st":
                                MarkSubTaskDone(Convert.ToUInt32(command[1].Substring(0, command[1].Length - 2)));
                                break;
                        }

                        break;
                    case "/delete":
                        string typing = command[1].Substring(command[1].Length - 2, 2);
                        try
                        {
                            switch (typing)
                            {
                                case "ts":
                                    DeleteTask(Convert.ToUInt32(command[1].Substring(0, command[1].Length - 2)));
                                    break;
                                case "st":
                                    DeleteSubTask(Convert.ToUInt32(command[1].Substring(0, command[1].Length - 2)));
                                    break;
                                case "gr":
                                    State._taskgroups.Remove(
                                        Convert.ToUInt32(command[1].Substring(0, command[1].Length - 2)));
                                    break;
                            }
                        }
                        catch
                        {
                            Console.WriteLine("an error occured while deleting");
                        }

                        break;
                    case "/set-time":
                        DateTime time_tag = DateTime.ParseExact(command[2], "dd/MM/yyyy/HH:mm:ss",
                            CultureInfo.InvariantCulture);
                        SetTimeLimit(Convert.ToUInt32(command[1].Substring(0, command[1].Length - 2)), time_tag);
                        break;
                    case "/save":
                        using (Stream stream = File.Open(command[1], FileMode.Create))
                        {
                            BinaryFormatter b = new BinaryFormatter();
                            b.Serialize(stream, State);
                        }

                        break;
                    case "/load":
                        using (Stream stream = File.Open(command[1], FileMode.Open))
                        {
                            BinaryFormatter b = new BinaryFormatter();
                            State = (TaskState)b.Deserialize(stream);
                        }

                        break;
                    default:
                        Console.WriteLine("Command not found");
                        break;
                }
            }

            Console.WriteLine("TaskManager is being closed");
        }

        static void DeleteTask(uint tsid)
        {
            if (State._tasks.ContainsKey(tsid))
            {
                State._tasks.Remove(tsid);
            }
            else
            {
                foreach (uint parent in State.tasks_in_groups.Keys)
                {
                    foreach (Task ts in State.tasks_in_groups[parent])
                        if (ts.get_id() == tsid)
                        {
                            State.tasks_in_groups[parent].Remove(ts);
                        }
                }
            }
        }

        static void DeleteSubTask(uint stid)
        {
            foreach (uint parent in State.subtasks_in_tasks.Keys)
            {
                foreach (SubTask st in State.subtasks_in_tasks[parent])
                    if (st.get_id() == stid)
                    {
                        State.subtasks_in_tasks[parent].Remove(st);
                    }
            }
        }

        static void CreateTask(string name)
        {
            State._tasks.Add(++State._last_task_id,
                new Task(State._last_task_id, (name.Length > 0) ? name : "unnamed" + State._last_task_id));
            Console.WriteLine("Task created. Id: " + State._last_task_id.ToString() + "ts");
        }

        static void CreateGroup(string name)
        {
            State._taskgroups.Add(++State._last_group_id,
                new TaskGroup(State._last_group_id, (name.Length > 0) ? name : "unnamed" + State._last_group_id));
            Console.WriteLine("Group created. Id: " + State._last_group_id.ToString() + "gr");
        }

        static void TaskToGroup(uint taskid, uint groupid)
        {
            try
            {
                State.tasks_in_groups[groupid].Add(State._tasks[taskid]);
            }
            catch
            {
                State.tasks_in_groups.Add(groupid, new List<Task>());
                State.tasks_in_groups[groupid].Add(State._tasks[taskid]);
            }

            State._tasks.Remove(taskid);
        }

        static void TaskFromGroup(uint taskid, uint groupid)
        {
            foreach (Task ts in State.tasks_in_groups[groupid])
            {
                if (ts.get_id() == taskid)
                {
                    State._tasks.Add(taskid, ts);
                    State.tasks_in_groups[groupid].Remove(ts);
                    return;
                }
            }
        }

        static void ShowAllTasks(Func<Tasks_generic, bool> pred)
        {
            foreach (Task ts in State._tasks.Values)
            {
                if (!pred(ts))
                    continue;
                Console.WriteLine(ts.ToString());
                try
                {
                    foreach (SubTask st in State.subtasks_in_tasks[ts.get_id()])
                    {
                        if (!pred(st))
                            continue;
                        Console.WriteLine("\t" + st.ToString());
                    }
                }
                catch
                {
                    continue;
                }
            }

            foreach (TaskGroup temp_group in State._taskgroups.Values)
            {
                if (!pred(temp_group))
                    continue;
                Console.WriteLine("Task-group (" + temp_group.get_id() + "tg) " + temp_group.get_summary());
                try
                {
                    foreach (Task ts in State.tasks_in_groups[temp_group.get_id()])
                    {
                        if (!pred(ts))
                            continue;
                        Console.WriteLine("\t" + ts.ToString());
                        try
                        {
                            foreach (SubTask st in State.subtasks_in_tasks[ts.get_id()])
                            {
                                if (!pred(st))
                                    continue;
                                Console.WriteLine("\t\t" + st.ToString());
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        static void CreateSubTask(uint taskid, string name)
        {
            try
            {
                State.subtasks_in_tasks[taskid]
                    .Add(new SubTask(++State._last_subtask_id,
                        (name.Length > 0) ? name : "unnamed" + State._last_task_id));
            }
            catch
            {
                State.subtasks_in_tasks.Add(taskid, new List<SubTask>());
                State.subtasks_in_tasks[taskid]
                    .Add(new SubTask(++State._last_subtask_id,
                        (name.Length > 0) ? name : "unnamed" + State._last_task_id));
            }
        }

        static void MarkTaskDone(uint taskid)
        {
            try
            {
                State._tasks[taskid].done = true;
            }
            catch
            {
                foreach (List<Task> ls in State.tasks_in_groups.Values)
                {
                    foreach (Task ts in ls)
                    {
                        if (ts.get_id() == taskid)
                        {
                            ts.mark_done();
                            return;
                        }
                    }
                }
            }
        }

        static void MarkSubTaskDone(uint taskid)
        {
            foreach (List<SubTask> ls in State.subtasks_in_tasks.Values)
            {
                foreach (SubTask st in ls)
                {
                    if (st.get_id() == taskid)
                    {
                        st.mark_done();
                        return;
                    }
                }
            }
        }

        static void SetTimeLimit(uint taskid, DateTime dl)
        {
            try
            {
                State._tasks[taskid].give_time_limit(dl);
            }
            catch
            {
                foreach (List<Task> ls in State.tasks_in_groups.Values)
                {
                    foreach (Task ts in ls)
                    {
                        if (ts.get_id() == taskid)
                        {
                            ts.give_time_limit(dl);
                            return;
                        }
                    }
                }
            }
        }

        static void ShowTodayTasks()
        {
            foreach (Task ts in State._tasks.Values)
            {
                if (!(ts.is_time_timited() && ts.get_time_limit().Year == DateTime.Today.Year
                                           && ts.get_time_limit().Month == DateTime.Today.Month &&
                                           ts.get_time_limit().Day == DateTime.Today.Day))
                    continue;
                Console.WriteLine(ts.ToString());
                try
                {
                    foreach (SubTask st in State.subtasks_in_tasks[ts.get_id()])
                        Console.WriteLine("\t" + st.ToString());
                }
                catch
                {
                    continue;
                }
            }

            foreach (TaskGroup temp_group in State._taskgroups.Values)
            {
                Console.WriteLine("Task-group (" + temp_group.get_id() + "tg) " + temp_group.get_summary());
                try
                {
                    foreach (Task ts in State.tasks_in_groups[temp_group.get_id()])
                    {
                        if (!(ts.is_time_timited() && ts.get_time_limit().Year == DateTime.Today.Year
                                                   && ts.get_time_limit().Month == DateTime.Today.Month &&
                                                   ts.get_time_limit().Day == DateTime.Today.Day))
                            continue;
                        Console.WriteLine("\t" + ts.ToString());
                        try
                        {
                            foreach (SubTask st in State.subtasks_in_tasks[ts.get_id()])
                                Console.WriteLine("\t\t" + st.ToString());
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine(String.Concat(Enumerable.Repeat("-", 15)));
            Console.WriteLine("COMMANDS:");
            Console.WriteLine(String.Concat(Enumerable.Repeat("-", 15)));
            foreach (string command_example in _help_words)
                Console.Write(command_example + ' ');
            Console.WriteLine("- ask for help");
            foreach (string command_example in _stop_words)
                Console.Write(command_example + ' ');
            Console.WriteLine("- stutdown app");
            Console.WriteLine("/list-tasks - show all your tasks and taskgroups");
            Console.WriteLine("/show-undone - show tasks/subtasks which is undone");
            Console.WriteLine("/show-done - show tasks/subtasks which is done");
            Console.WriteLine("/show-today - show tasks that are needed to be finished today");
            Console.WriteLine("/create-task {task-name} - create task with given name/summary (or unnamed)");
            Console.WriteLine("/create-group {group-name} - create group with given name/summary (or unnamed)");
            Console.WriteLine("/add-task-to-group {task-id} {group-id} - add task to an existing group");
            Console.WriteLine("/bind-subtask {task-id} {subtask-name} - create subtask for an existing task");
            Console.WriteLine("/mark-done - mark task/subtask done");
            Console.WriteLine("/delete {id} - delete object");
            Console.WriteLine("/set-time {task-id} {dd/MM/yyyy/HH:mm:ss}");
            Console.WriteLine("/save {path} save the current state to file");
            Console.WriteLine("/load {path} load state from file");
            Console.WriteLine(String.Concat(Enumerable.Repeat("-", 15)));
        }
    }

    [Serializable]
    class Tasks_generic
    {
        protected uint identificator;
        protected string summary;
        public bool done;

        public Tasks_generic(uint id, string summary)
        {
            this.identificator = id;
            this.summary = summary;
            this.done = false;
        }

        public uint get_id()
        {
            return identificator;
        }

        public string get_summary()
        {
            return summary;
        }

        public void mark_done()
        {
            done = true;
        }
    }

    [Serializable]
    class TaskGroup : Tasks_generic
    {
        public TaskGroup(uint id, string summary) : base(id, summary)
        {
        }
    }

    [Serializable]
    class Task : Tasks_generic
    {
        private uint _last_subtask_id = 0;

        public Task(uint id, string summary) : base(id, summary)
        {
            this.time_limited = false;
        }

        public Task(uint id, string summary, DateTime time_limit) : base(id, summary)
        {
            this.time_limited = true;
            this.time_limit = time_limit;
        }

        private bool time_limited;
        private DateTime time_limit;

        public bool is_time_timited()
        {
            return time_limited;
        }

        public DateTime get_time_limit()
        {
            return time_limit;
        }

        public void give_time_limit(DateTime limit)
        {
            time_limit = limit;
            time_limited = true;
        }

        public void mark_done()
        {
            done = true;
            time_limited = false;
        }


        public string ToString()
        {
            return "[" + ((done) ? "X" : " ") + "]" + " " +
                   ((time_limited) ? time_limit.ToString() + " " : "") +
                   "(" + identificator.ToString() + "ts) " + summary.ToString();
        }
    }

    [Serializable]
    class SubTask : Tasks_generic
    {
        public SubTask(uint id, string summary) : base(id, summary)
        {
        }

        public string ToString()
        {
            return "[" + ((done) ? "X" : " ") + "]" + " " +
                   "(" + identificator.ToString() + "st) " + summary.ToString();
        }
    }
}