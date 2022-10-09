using HtmlAgilityPack;
using KbbiApi;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<KbbiContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Sqlite")));

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => 
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin()));

var app = builder.Build();

var db = app.Services.CreateScope().ServiceProvider.GetRequiredService<KbbiContext>();
db.Database.EnsureCreated();

app.UseHttpsRedirection();
app.UseFileServer();
app.UseCors();

const string baseUrl = "https://kbbi.kemdikbud.go.id/entri";
var htmlWeb = new HtmlWeb();

app.MapGet("/words/{kata}", async (string kata, CancellationToken cancellationToken, KbbiContext context) =>
{
    // find data in the local database first
    var dict = await context.Dictionary.AsNoTracking()
        .Include(x => x.Definitions)
        .FirstOrDefaultAsync(x => x.Word == kata.ToLower(), cancellationToken);
    if (dict != null)
        return new WordResponse(kata, dict.Definitions.Select(x => x.Description));
    
    // scrap data from kbbi website
    var url = $"{baseUrl}/{kata}";
    var doc = htmlWeb.Load(url);
    var h2Nodes = doc.DocumentNode.SelectNodes("//h2");

    if (h2Nodes == null)
        return new WordResponse(kata, Enumerable.Empty<string>());

    var definitions = new List<string>();
    foreach (var h2 in h2Nodes)
    {
        var liNodes = h2?.NextSibling?.NextSibling?.ChildNodes;
        
        // handle single result (ex: saya)
        if (liNodes == null || liNodes.Count == 0)
            liNodes = h2?.SelectSingleNode("//ol").ChildNodes;

        if (liNodes == null) continue;
        
        foreach (var li in liNodes)
        {
            var innerText = li.InnerText;
            if (string.IsNullOrWhiteSpace(innerText)) continue;
            
            // remove pollution (ex: v Huk, n, v cak, etc)
            var arti = innerText.Split("    ").Last().Trim();
            
            definitions.Add(arti);
        }
    }
    
    // save to local database
    if (definitions.Count > 0)
    {
        var newWord = new Dictionary
        {
            Word = kata.ToLower(),
            Definitions = definitions.Select(x => new Definition
            {
                Description = x
            }).ToList()
        };
        context.Dictionary.Add(newWord);
        await context.SaveChangesAsync(cancellationToken);
    }
    
    return new WordResponse(kata, definitions);
});

app.Run();

public record WordResponse(string Word, IEnumerable<string> Definitions);