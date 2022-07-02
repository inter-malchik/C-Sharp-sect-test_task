using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;

namespace TaskManager;

internal class Program
{
    private static readonly HashSet<string> StopWords = new String[] { "/stop", "/shutdown", "/STOP" }.ToHashSet();
    private static readonly HashSet<string> HelpWords = new String[] { "/help", "/HELP" }.ToHashSet();

    static ProgramState State = new ProgramState();

    static void Main(string[] args)
    {
        Console.WriteLine("TaskManager is opened");

        Console.WriteLine("You can type \"/help\" to see available commands");
        while (true)
        {
            string[] command = Console.ReadLine().Split(' ');

            if (StopWords.Contains(command[0]))
                break;
            if (HelpWords.Contains(command[0]))
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
                    ShowAllTasks(x => !x.IsDone());
                    break;
                case "/show-done":
                    ShowAllTasks(x => x.IsDone());
                    break;
                case "/show-today":
                    ShowAllTasks(x =>
                        (x.GetType() == typeof(TaskGroup)) || x.GetType() == typeof(SubTask) ||
                        (x.GetType() == typeof(Task) && x.IsTimeTimited()) &&
                        x.GetTimeLimit().Year == DateTime.Today.Year &&
                        x.GetTimeLimit().Month == DateTime.Today.Month &&
                        x.GetTimeLimit().Day == DateTime.Today.Day);
                    break;
                case "/create-task":
                    CreateTask(command.Length > 1 ? String.Join(" ", command[1..]) : "");
                    break;
                case "/create-group":
                    CreateGroup(command.Length > 1 ? String.Join(" ", command[1..]) : "");
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
                        command.Length == 2 ? "" : String.Join(" ", command[2..]));
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
                    State.Save(command[1]);
                    Console.WriteLine("Saved to {0}", command[1]);
                    break;
                case "/load":
                    State.Load(command[1]);
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

    static void DeleteTask(uint taskId)
    {
        if (State.Tasks.ContainsKey(taskId))
            State.Tasks.Remove(taskId);
        else
            foreach (uint parent in State.TasksInGroups.Keys)
            {
                foreach (Task task in State.TasksInGroups[parent])
                    if (task.GetId() == taskId)
                    {
                        State.TasksInGroups[parent].Remove(task);
                        return;
                    }
            }
    }

    static void DeleteSubTask(uint subTaskId)
    {
        foreach (uint parent in State.SubtasksInTasks.Keys)
        {
            foreach (SubTask subTask in State.SubtasksInTasks[parent])
                if (subTask.GetId() == subTaskId)
                {
                    State.SubtasksInTasks[parent].Remove(subTask);
                    return;
                }
        }
    }

    static void DeleteTaskGroup(uint groupId)
    {
        if (!State.TaskGroups.ContainsKey(groupId)) return;
        State.TaskGroups.Remove(groupId);
        if (!State.TasksInGroups.ContainsKey(groupId))
            State.TasksInGroups.Remove(groupId);
    }

    static void CreateTask(string name)
    {
        State.Tasks.Add(++State.LastTaskId,
            new Task(State.LastTaskId,
                (name.Length > 0) ? name : String.Format("unnamed{0}", State.LastTaskId.ToString())));
        Console.WriteLine("Task created. Id: {0}ts", State.LastTaskId.ToString());
    }

    static void CreateGroup(string name)
    {
        State.TaskGroups.Add(++State.LastGroupId,
            new TaskGroup(State.LastGroupId,
                (name.Length > 0) ? name : String.Format("unnamed{0}", State.LastGroupId.ToString())));
        Console.WriteLine("Group created. Id: {0}gr", State.LastGroupId.ToString());
    }

    static void TaskToGroup(uint taskId, uint groupId)
    {
        if (!State.TasksInGroups.ContainsKey(groupId))
            State.TasksInGroups.Add(groupId, new List<Task>());
        State.TasksInGroups[groupId].Add(State.Tasks[taskId]);
        State.Tasks.Remove(taskId);
    }

    static void TaskFromGroup(uint taskId, uint groupId)
    {
        if (!State.TasksInGroups.ContainsKey(groupId)) return;
        foreach (Task task in State.TasksInGroups[groupId])
            if (task.GetId() == taskId)
            {
                State.Tasks.Add(taskId, task);
                State.TasksInGroups[groupId].Remove(task);
                return;
            }
    }

    static void ShowAllTasks(Func<TasksGeneric, bool> predicateFunc)
    {
        foreach (Task task in State.Tasks.Values)
        {
            if (!predicateFunc(task))
                continue;
            Console.WriteLine(task.ToString());
            if (!State.SubtasksInTasks.ContainsKey(task.GetId()))
                continue;
            foreach (SubTask subTask in State.SubtasksInTasks[task.GetId()])
            {
                if (!predicateFunc(subTask))
                    continue;
                Console.WriteLine("\t{0}", subTask.ToString());
            }
        }

        foreach (TaskGroup tempGroup in State.TaskGroups.Values)
        {
            if (!predicateFunc(tempGroup))
                continue;
            Console.WriteLine(tempGroup.ToString());
            if (!State.TasksInGroups.ContainsKey(tempGroup.GetId()))
                continue;
            foreach (Task task in State.TasksInGroups[tempGroup.GetId()])
            {
                if (!predicateFunc(task))
                    continue;
                Console.WriteLine("\t{0}", task.ToString());
                if (!State.SubtasksInTasks.ContainsKey(task.GetId()))
                    continue;
                foreach (SubTask subTask in State.SubtasksInTasks[task.GetId()])
                {
                    if (!predicateFunc(subTask))
                        continue;
                    Console.WriteLine("\t\t{0}", subTask.ToString());
                }
            }
        }
    }

    static void CreateSubTask(uint taskId, string name)
    {
        if (!State.SubtasksInTasks.ContainsKey(taskId))
            State.SubtasksInTasks.Add(taskId, new List<SubTask>());
        State.SubtasksInTasks[taskId]
            .Add(new SubTask(++State.LastSubtaskId,
                (name.Length > 0) ? name : "unnamed" + State.LastTaskId));
    }

    static void MarkTaskDone(uint taskId)
    {
        if (State.Tasks.ContainsKey(taskId))
        {
            State.Tasks[taskId].MarkDone();
            return;
        }

        foreach (List<Task> listTasks in State.TasksInGroups.Values)
        {
            foreach (Task task in listTasks)
            {
                if (task.GetId() == taskId)
                {
                    task.MarkDone();
                    return;
                }
            }
        }
    }

    static void MarkSubTaskDone(uint taskId)
    {
        foreach (List<SubTask> listTasks in State.SubtasksInTasks.Values)
        {
            foreach (SubTask subTask in listTasks)
            {
                if (subTask.GetId() == taskId)
                {
                    subTask.MarkDone();
                    return;
                }
            }
        }
    }

    static void SetTimeLimit(uint taskId, DateTime dateLimit)
    {
        if (State.Tasks.ContainsKey(taskId))
        {
            State.Tasks[taskId].GiveTimeLimit(dateLimit);
            return;
        }

        foreach (List<Task> listTasks in State.TasksInGroups.Values)
        {
            foreach (Task task in listTasks)
            {
                if (task.GetId() == taskId)
                {
                    task.GiveTimeLimit(dateLimit);
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
        Console.WriteLine("{0} - ask for help", string.Join(" ", HelpWords));
        Console.WriteLine("{0} - stutdown app", string.Join(" ", StopWords));
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