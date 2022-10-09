using Microsoft.EntityFrameworkCore;

namespace KbbiApi;

public class KbbiContext : DbContext
{
    public KbbiContext(DbContextOptions<KbbiContext> options) : base(options)
    {
    }

    public DbSet<Dictionary> Dictionary { get; set; }
}

public class Dictionary
{
    public int Id { get; set; }
    public string Word { get; set; }
    public List<Definition> Definitions { get; set; }
}

public class Definition
{
    public int Id { get; set; }
    public Dictionary Dictionary { get; set; }
    public int DictionaryId { get; set; }
    public string Description { get; set; }
}