using PedalLib.Util;

namespace PedalLib;

// Extremely lightweight service locator for shared singletons.
// Extend later if you add caching, logging, etc.
public static class ServiceLocator
{
    // Single shared DataService instance (lazy if you later need).
    public static DataService DataService { get; } = new();
}