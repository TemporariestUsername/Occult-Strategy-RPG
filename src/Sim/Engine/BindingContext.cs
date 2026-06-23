using PaleCommunion.Sim.Model;

namespace PaleCommunion.Sim.Engine;

/// <summary>
/// The characters resolved into an event's named scopes (actor, rival, …). A scope
/// may be present but unfilled (null) when its selector was optional and matched
/// nobody. Text, conditions, checks, and effects look characters up here by name.
/// </summary>
public sealed class BindingContext
{
    private readonly Dictionary<string, Member?> _scopes = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, Member?> Scopes => _scopes;

    public void Set(string scope, Member? member) => _scopes[scope] = member;

    public bool TryGet(string scope, out Member? member) => _scopes.TryGetValue(scope, out member);

    public bool Has(string scope) => _scopes.TryGetValue(scope, out Member? m) && m is not null;

    public Member Require(string scope) =>
        _scopes.TryGetValue(scope, out Member? m) && m is not null
            ? m
            : throw new InvalidOperationException($"Scope '{scope}' is not bound to a member.");
}
