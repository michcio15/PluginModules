using JetBrains.Annotations;

namespace PluginModules.Attributes;

/// <summary>
///     Wyłącza automatyczne patchowanie przez moduł,
///     Registerowanie komendy,
///     Registerwowanie modułu
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
[UsedImplicitly]
public sealed class DisableAutoRegister : Attribute;