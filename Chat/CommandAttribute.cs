namespace Chat;

[System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
sealed class CommandAttribute : Attribute
{
    public CommandAttribute() { }

    public CommandAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; private set; }
}
