// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using Infragraph.Common.Models.Domain;
using Infragraph.Common.Models.Former2;
using Infragraph.Core.Modeling;
using Infragraph.Core.Parsing;
using Infragraph.Core.Relationships;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    cts.Cancel();
    eventArgs.Cancel = true;
};

// var dir = @"C:\dev\Infragraph\tmp\archive"; // args[0];
var dirOut = @"C:\dev\Infragraph\tmp\"; // args[0];
var pathOut = Path.Combine(dirOut, "resources.json");

var filterTypes = new List<string> {
    "cloudwatch.logstream"
};

await ResourceActions.ImportAwsResources(pathOut, filterTypes, cts.Token);

public static class ResourceActions
{
    public static async Task ImportAwsResources(string path, List<string> filterTypes, CancellationToken cancel)
    {
        var allRelationships = AllRelationships.All();
        var resourceFactory = new ResourceModelFactory(allRelationships);
        await using var stream = File.Open(path, FileMode.Open);

        var former2Resources = new List<Former2Resource>();
        await foreach (var result in Former2Parser.ParseStreamAsync(stream, filterTypes, cancel))
        {
            if (result.Result(out var resource, out _))
            {
                former2Resources.Add(resource);
            }
        }
        
        var resourceSet = resourceFactory.CreateResourceSet(former2Resources);
        Console.WriteLine(resourceSet.Relationships.Count);
    } 
    
    public static async Task MergeAsync(string dir, string pathOut, List<string> filterTypes, CancellationToken cancel)
    {
        var okTypes = new Dictionary<string, int>();
        var invalidTypes = new List<string>();
        var resources = new List<Former2Resource>();

        foreach (var path in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
        {
            var account = Path.GetFileNameWithoutExtension(path);
            Console.WriteLine($"Processing {account}");
            await ImportResources(cancel, path, filterTypes, okTypes, account, resources, invalidTypes);
        }

        if (invalidTypes.Count > 0)
        {
            Console.WriteLine("Invalid type(s):");
            foreach (var invalidType in invalidTypes)
            {
                Console.WriteLine(invalidType);
            }

            return;
        }

        foreach (var t in okTypes.OrderByDescending(k => k.Value))
        {
            Console.WriteLine($"{t.Key}: {t.Value}");
        }

        {
            await using var outStream = File.Open(pathOut, FileMode.Create);
            await Former2JsonContext.SerializeAsync(outStream, resources, cancel);
        }
    }

    private static async Task ImportResources(CancellationToken cancel, string path, List<string> filterTypes, 
        Dictionary<string, int> okTypes, string account, List<Former2Resource> resources, List<string> invalidTypes)
    {
        await using var stream = File.Open(path, FileMode.Open);
        await foreach (var result in Former2Parser.ParseStreamAsync(stream, filterTypes, cancel))
        {
            if (result.Result(out var resource, out var invalid))
            {
                okTypes.TryGetValue(resource.Type, out var v);
                okTypes[resource.Type] = v + 1;
            
                if (string.IsNullOrWhiteSpace(resource.Account))
                {
                    resource.Account = account;
                }
            
                resources.Add(resource);
            }
            else
            {
                invalidTypes.Add(invalid.Value.ToString());
            }
        }
    }
}