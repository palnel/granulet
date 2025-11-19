using System;
using System.CommandLine;
using Granulet.Console.Commands;

var rootCommand = new RootCommand("Granulet - SQL Server Database Migration CLI");

// Add commands
rootCommand.AddCommand(InitCommand.Create());
rootCommand.AddCommand(NewCommand.Create());
rootCommand.AddCommand(StatusCommand.Create());
rootCommand.AddCommand(UpdateCommand.Create());
rootCommand.AddCommand(RollbackCommand.Create());

// Set description
rootCommand.Description = "A lightweight, script-first SQL Server database migration tool";

// Handle the command
return await rootCommand.InvokeAsync(args);
