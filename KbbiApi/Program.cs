using HtmlAgilityPack;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => 
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin()));

var app = builder.Build();
app.UseHttpsRedirection();
app.UseFileServer();
app.UseCors();

const string baseUrl = "https://kbbi.kemdikbud.go.id";
var htmlWeb = new HtmlWeb();

app.MapGet("/{kata}", (string kata) =>
{
    var url = $"{baseUrl}/entri/{kata}";
    var doc = htmlWeb.Load(url);
    var h2Nodes = doc.DocumentNode.SelectNodes("//h2");

    if (h2Nodes == null)
        return new ResponseModel(kata, Enumerable.Empty<string>());

    var listArti = new List<string>();
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
            
            listArti.Add(arti);
        }
    }
    
    return new ResponseModel(kata, listArti);
});

app.Run();

public record ResponseModel(string Kata, IEnumerable<string> Arti);