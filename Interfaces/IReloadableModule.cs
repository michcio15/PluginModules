namespace PluginModules.Interfaces;

/// <summary>
///     No do komendy zeby dzialalo
/// </summary>
public interface IReloadableModule
{
    /// <summary>
    ///     Próbuje załadować ponownie config
    /// </summary>
    /// <returns>Czy się udało</returns>
    bool TryReloadConfig();
}