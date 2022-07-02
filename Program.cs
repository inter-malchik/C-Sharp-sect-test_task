using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;

namespace TaskManager;

internal class Program
{

    static private HashSet<string> _stop_words = new String[] { "/stop", "/shutdown", "/STOP" }.ToHashSet();
    static private HashSet<string> _help_words = new String[] { "/help", "/HELP" }.ToHashSet();

    static ProgramState State = new ProgramState();

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
                    ShowAllTasks(x => !x.is_done());
                    break;
                case "/show-done":
                    ShowAllTasks(x => x.is_done());
                    break;
                case "/show-today":
                    ShowAllTasks(x =>
                        (x.GetType() == typeof(TaskGroup)) || x.GetType() == typeof(SubTask) ||
                        (x.GetType() == typeof(Task) && x.is_time_timited()) &&
                        x.get_time_limit().Year == DateTime.Today.Year &&
                        x.get_time_limit().Month == DateTime.Today.Month &&
                        x.get_time_limit().Day == DateTime.Today.Day);
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
                    TaskFromGroup(Convert.ToUInt32(command[1].Substring(0, command[1].Length - 2)),
                        Convert.ToUInt32(command[2].Substring(0, command[1].Length - 2)));
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
                    switch (typing)
                    {
                        case "ts":
                            DeleteTask(Convert.ToUInt32(command[1].Substring(0, command[1].Length - 2)));
                            break;
                        case "st":
                            DeleteSubTask(Convert.ToUInt32(command[1].Substring(0, command[1].Length - 2)));
                            break;
                        case "gr":
                            DeleteTaskGroup(Convert.ToUInt32(command[1].Substring(0, command[1].Length - 2)));
                            break;
                    }

                    break;
                case "/set-time":
                    DateTime time_tag = DateTime.ParseExact(command[2], "dd/MM/yyyy/HH:mm:ss",
                        CultureInfo.InvariantCulture);
                    SetTimeLimit(Convert.ToUInt32(command[1].Substring(0, command[1].Length - 2)), time_tag);
                    break;
                case "/save":
                    State.save(command[1]);
                    Console.WriteLine("Saved to {0}", command[1]);
                    break;
                case "/load":
                    State.load(command[1]);
                    Console.WriteLine("Loaded from {0}", command[1]);
                    break;
                case "/hard-reset":
                    State = new ProgramState();
                    Console.WriteLine("Data was cleared");
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
            State._tasks.Remove(tsid);
        else
            foreach (uint parent in State.tasks_in_groups.Keys)
            {
                foreach (Task ts in State.tasks_in_groups[parent])
                    if (ts.get_id() == tsid)
                    {
                        State.tasks_in_groups[parent].Remove(ts);
                        return;
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
                    return;
                }
        }
    }

    static void DeleteTaskGroup(uint grid)
    {
        if (!State._taskgroups.ContainsKey(grid)) return;
        State._taskgroups.Remove(grid);
        if (!State.tasks_in_groups.ContainsKey(grid))
            State.tasks_in_groups.Remove(grid);
    }

    static void CreateTask(string name)
    {
        State._tasks.Add(++State._last_task_id,
            new Task(State._last_task_id,
                (name.Length > 0) ? name : String.Format("unnamed{0}", State._last_task_id.ToString())));
        Console.WriteLine("Task created. Id: {0}ts", State._last_task_id.ToString());
    }

    static void CreateGroup(string name)
    {
        State._taskgroups.Add(++State._last_group_id,
            new TaskGroup(State._last_group_id,
                (name.Length > 0) ? name : String.Format("unnamed{0}", State._last_group_id.ToString())));
        Console.WriteLine("Group created. Id: {0}gr", State._last_group_id.ToString());
    }

    static void TaskToGroup(uint taskid, uint groupid)
    {
        if (!State.tasks_in_groups.ContainsKey(groupid))
            State.tasks_in_groups.Add(groupid, new List<Task>());
        State.tasks_in_groups[groupid].Add(State._tasks[taskid]);
        State._tasks.Remove(taskid);
    }

    static void TaskFromGroup(uint taskid, uint groupid)
    {
        if (!State.tasks_in_groups.ContainsKey(groupid)) return;
        foreach (Task ts in State.tasks_in_groups[groupid])
            if (ts.get_id() == taskid)
            {
                State._tasks.Add(taskid, ts);
                State.tasks_in_groups[groupid].Remove(ts);
                return;
            }
    }

    static void ShowAllTasks(Func<TasksGeneric, bool> pred)
    {
        foreach (Task ts in State._tasks.Values)
        {
            if (!pred(ts))
                continue;
            Console.WriteLine(ts.ToString());
            if (!State.subtasks_in_tasks.ContainsKey(ts.get_id()))
                continue;
            foreach (SubTask st in State.subtasks_in_tasks[ts.get_id()])
            {
                if (!pred(st))
                    continue;
                Console.WriteLine("\t{0}", st.ToString());
            }
        }

        foreach (TaskGroup temp_group in State._taskgroups.Values)
        {
            if (!pred(temp_group))
                continue;
            Console.WriteLine(temp_group.ToString());
            if (!State.tasks_in_groups.ContainsKey(temp_group.get_id()))
                continue;
            foreach (Task ts in State.tasks_in_groups[temp_group.get_id()])
            {
                if (!pred(ts))
                    continue;
                Console.WriteLine("\t{0}", ts.ToString());
                if (!State.subtasks_in_tasks.ContainsKey(ts.get_id()))
                    continue;
                foreach (SubTask st in State.subtasks_in_tasks[ts.get_id()])
                {
                    if (!pred(st))
                        continue;
                    Console.WriteLine("\t\t{0}", st.ToString());
                }
            }
        }
    }

    static void CreateSubTask(uint taskid, string name)
    {
        if (!State.subtasks_in_tasks.ContainsKey(taskid))
            State.subtasks_in_tasks.Add(taskid, new List<SubTask>());
        State.subtasks_in_tasks[taskid]
            .Add(new SubTask(++State._last_subtask_id,
                (name.Length > 0) ? name : "unnamed" + State._last_task_id));
    }

    static void MarkTaskDone(uint taskid)
    {
        if (State._tasks.ContainsKey(taskid))
        {
            State._tasks[taskid].mark_done();
            return;
        }

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
        if (State._tasks.ContainsKey(taskid))
        {
            State._tasks[taskid].give_time_limit(dl);
            return;
        }

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

    static void ShowHelp()
    {
        Console.WriteLine(String.Concat(Enumerable.Repeat("-", 15)));
        Console.WriteLine("COMMANDS:");
        Console.WriteLine(String.Concat(Enumerable.Repeat("-", 15)));
        Console.WriteLine("{0} - ask for help", string.Join(" ", _help_words));
        Console.WriteLine("{0} - stutdown app", string.Join(" ", _stop_words));
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
        Console.WriteLine("/hard-reset clear all data about tasks/subtasks/groups");
        Console.WriteLine(String.Concat(Enumerable.Repeat("-", 15)));
    }
}