namespace Onyx.UCI;

public class Option
{
    public string Name = "";
    public string Type = "";
    public string Default = "";
    public string Min = "";
    public string Max = "";
    public int Value;

    public required Action<int> OnApply { get; set; }
    
    public string OptionString()
    {
        return $"option name {Name} type {Type} default {Default} min {Min} max {Max}";
    }
}

public class Options
{
    private readonly List<Option> _options = [];

    public void AddOption(string name, string type, string defaultValue, string min, string max, Action<int> onApply)
    {
        _options.Add(new Option
        {
            Name = name,
            Type = type,
            Default = defaultValue,
            Min = min,
            Max = max,
            OnApply = onApply
        });
    }

    public void SetOption(string name, int value)
    {
        var thisOption = _options.First(o => o.Name == name);
        if (thisOption == null) return;

        if (value < int.Parse(thisOption.Min) || value > int.Parse(thisOption.Max))
            return;
        thisOption.Value = value;
        
        thisOption.OnApply?.Invoke(value);
    }

    
    public void PrintOptions()
    {
        foreach (var option in _options)
        {
            Console.WriteLine(option.OptionString());
        }
    }
}