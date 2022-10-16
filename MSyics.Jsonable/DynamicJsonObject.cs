using System.ComponentModel;
using System.Dynamic;
using System.Text.Json.Serialization;

namespace System.Text.Json;

public class DynamicJsonObject : DynamicObject, INotifyPropertyChanged
{
    [JsonExtensionData]
    public IDictionary<string, object?> Members { get; } = new Dictionary<string, object?>();

    [JsonIgnore]
    protected IDictionary<string, object?> DynamicMembers { get; } = new Dictionary<string, object?>();

    [JsonIgnore]
    public bool IsEmpty => Members.Count == 0;

    [JsonIgnore]
    protected DynamicJsonObject? Parent { get; set; }

    [JsonIgnore]
    protected string? Key { get; set; }

    public virtual object? this[string key]
    {
        get
        {
            if (string.IsNullOrWhiteSpace(key)) return null;

            if (!Members.TryGetValue(key, out var value) && !DynamicMembers.TryGetValue(key, out value))
            {
                value = new DynamicJsonObject
                {
                    Parent = this,
                    Key = key,
                };
                DynamicMembers[key] = value;
            }

            return value;
        }
        set => Add(key, value);
    }

    public override bool TryGetMember(GetMemberBinder binder, out object? result)
    {
        result = this[binder.Name];
        return true;
    }

    public override bool TrySetMember(SetMemberBinder binder, object? value)
    {
        Add(binder.Name, value);
        return true;
    }

    public override IEnumerable<string> GetDynamicMemberNames()
    {
        return DynamicMembers.Keys;
    }

    protected bool LinkParents(DynamicJsonObject? @object)
    {
        if (@object is null) return false;
        if (@object.Members.Count == 0) return false;
        if (@object is { Parent: null } or { Key: null }) return false;
        if (@object.Parent.Members.ContainsKey(@object.Key)) return false;

        @object.Parent.Members[@object.Key] = @object;
        @object.Parent.RaisePropertyChanged(@object.Key);
        return LinkParents(@object.Parent);
    }

    public virtual DynamicJsonObject? Add(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;

        if (value is DynamicJsonObject @object)
        {
            @object.Parent = this;
            @object.Key = key;
            DynamicMembers[key] = @object;
            RaisePropertyChanged(key);
            LinkParents(@object);
        }
        else
        {
            Members[key] = value;
            DynamicMembers[key] = value;
            RaisePropertyChanged(key);
            LinkParents(this);
        }

        return this;
    }

    public virtual DynamicJsonObject Remove(string key)
    {
        Members.Remove(key);
        DynamicMembers.Remove(key);
        return this;
    }

    public virtual DynamicJsonObject Clear()
    {
        Members.Clear();
        DynamicMembers.Clear();
        return this;
    }

    public override string? ToString() => Members.Aggregate(new StringBuilder(), (x, y) => x.Append(y)).ToString();

    protected virtual void RaisePropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    public event PropertyChangedEventHandler? PropertyChanged;
}