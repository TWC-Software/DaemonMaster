using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Input;

namespace DaemonMaster.Utilities
{
    //based on: https://github.com/CommunityToolkit/MVVM-Samples/issues/41
    public static class RelayCommandHelper
    {
        private static readonly List<WeakReference<IRelayCommand>> Commands = [];
        private static bool _connected;

        public static void RegisterCommandsWithCommandManager(object container)
        {
            List<WeakReference<IRelayCommand>> commands = [];

            foreach (var p in container.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!typeof(IRelayCommand).IsAssignableFrom(p.PropertyType))
                    continue;

                var command = (IRelayCommand?)p.GetValue(container);
                if (command != null)
                    commands.Add(new WeakReference<IRelayCommand>(command));
            }

            lock (Commands)
            {
                Commands.AddRange(commands);

                if (!_connected && Commands.Any())
                {
                    CommandManager.RequerySuggested += OnRequerySuggested;
                    _connected = true;
                }
            }
        }

        private static void OnRequerySuggested(object? sender, EventArgs args)
        {
            List<WeakReference<IRelayCommand>> commands;
            List<WeakReference<IRelayCommand>> commandsToDelete = [];

            lock (Commands)
            {
                commands = Commands;
            }

            foreach (var command in commands)
            {
                if (command.TryGetTarget(out var c))
                    c.NotifyCanExecuteChanged();
                else
                    commandsToDelete.Add(command);
            }

            // delete dead commands
            if (commandsToDelete.Any())
            {
                lock (Commands)
                {
                    foreach (var command in commandsToDelete)
                        Commands.Remove(command);

                    if (_connected && !Commands.Any())
                    {
                        CommandManager.RequerySuggested -= OnRequerySuggested;
                        _connected = false;
                    }
                }
            }
        }
    }
}
