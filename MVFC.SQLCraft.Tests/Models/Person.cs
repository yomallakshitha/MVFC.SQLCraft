namespace MVFC.SQLCraft.Tests.Models;

public record Person
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}