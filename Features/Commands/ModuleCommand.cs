using CommandSystem;
using PluginModules.Features.Commands.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace PluginModules.Features.Commands;

public abstract class ModuleCommand<T> : IHasModule, ICommand where T : Module
{
    public T Module { get; private set; } = null!;
    public ModuleLog ModuleLog => Module.ModuleLog;

    public abstract bool Execute(ArraySegment<string> arguments, ICommandSender sender,
        [UnscopedRef] out string response);

    public abstract string Command { get; }
    public abstract string[] Aliases { get; }
    public abstract string Description { get; }

    public void SetModule(Module module)
    {
        Module = (T)module;
    }
}