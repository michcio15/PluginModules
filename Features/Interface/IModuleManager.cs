namespace PluginModules.Features.Interface;

/// <summary>
/// Konstruktor musi miec <see cref="Module"/>
/// </summary>
public interface IModuleManager
{
    Module Module { get; }

    void Enable();

    void Disable();
}