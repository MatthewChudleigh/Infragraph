// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Infragraph.Common.Abstractions;
using Infragraph.Common.Configuration;
using Infragraph.Common.Models.Domain;
using Infragraph.Common.Models.Former2;
using Infragraph.Common.Models.ReactFlow;
using Infragraph.Core.Graph;
using Infragraph.Core.Modeling;
using Infragraph.Core.Parsing;
using Infragraph.Core.Relationships;
using Infragraph.Rendering.ReactFlow;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    cts.Cancel();
    eventArgs.Cancel = true;
};

// var dir = @"C:\dev\Infragraph\tmp\archive"; // args[0];
var dirOut = @"C:\dev\Infragraph\tmp\"; // args[0];
var pathIn = Path.Combine(dirOut, "resources.json");
var pathOut = Path.Combine(dirOut, "resources-no-net.json");
var networkingOut = Path.Combine(dirOut, "networking.json");
var cloudfrontOut = Path.Combine(dirOut, "cloudfront.json");
var networkingFlowOut = Path.Combine(dirOut, "networking-flow.json");

var accounts = JsonSerializer.Deserialize<Dictionary<string, string>>(
    File.ReadAllText(Path.Combine(dirOut, "accounts.json")),
    Former2JsonContext.Default.DictionaryStringString)
    ?? new Dictionary<string, string>();

var filterTypes = new List<string> {
    "cloudwatch.logstream"
};

var (resourceSet, former2Resources)  = 
    await ResourceActions.ImportAwsResources(pathIn, accounts, filterTypes, cts.Token);

foreach (var x in resourceSet.Resources.GroupBy(r => r.Type)
             .ToDictionary(kv => kv.Key, kv => kv.Count())
             .OrderBy(kv => kv.Key))
{
    Console.WriteLine($"{x.Key}: {x.Value}");
}

var networkingResources = ResourceActions.MapNetworking(resourceSet);
var cloudfrontResources = ResourceActions.MapCloudfront(resourceSet);

var networkingGraph = GraphBuilder.BuildGraph(
    GraphBuilder.DefaultGroupingStrategies, 
    networkingResources,
    false);

var networkingIds = new HashSet<string>(networkingResources.Resources.Select(r => r.Id));
{
    await using var outStream = File.Open(networkingOut, FileMode.Create);
    await Former2JsonContext.SerializeAsync(outStream, 
        networkingResources.Resources
            .Select(r => former2Resources[r.Id])
            .ToList(), cts.Token);
}

await ResourceActions.WriteOut(pathOut, resourceSet, former2Resources, [], cts.Token);
await ResourceActions.WriteOut(networkingOut, networkingResources, former2Resources, [], cts.Token);
await ResourceActions.WriteOut(cloudfrontOut, cloudfrontResources, former2Resources, [], cts.Token);

var reactFlow = new ReactFlowRenderer();
var networkingDiagram = reactFlow.Render(networkingGraph, DiagramOptions.Default);

await ResourceActions.WriteOut(networkingFlowOut, networkingDiagram, cts.Token);

public static class ResourceActions
{
    public static async Task WriteOut(string pathOut, ReactFlowDiagram reactFlowDiagram, CancellationToken cancel)
    {
        await using var outStream = File.Open(pathOut, FileMode.Create);
        await Former2JsonContext.SerializeAsync(outStream, reactFlowDiagram, cancel);
    }
    
    public static async Task WriteOut(string pathOut, ResourceSet resourceSet, 
        Dictionary<string, Former2Resource> former2Resources,
        HashSet<string> ids, CancellationToken cancel)
    {
        await using var outStream = File.Open(pathOut, FileMode.Create);
        await Former2JsonContext.SerializeAsync(outStream, 
            resourceSet.Resources
                .Where(r => !ids.Contains(r.Id))
                .Select(r => former2Resources[r.Id])
                .ToList(), cancel);
    }
    
    public static ResourceSet MapCloudfront(ResourceSet resourceSet)
    {
        var cfTypes = new HashSet<string>([
            SupportedResourceTypes.CloudfrontDistribution,
            SupportedResourceTypes.CloudfrontFunction,
            SupportedResourceTypes.CloudfrontOac,
            SupportedResourceTypes.CloudfrontOai,
        ]);
 
        return Map(resourceSet, cfTypes);
    }
    
    public static ResourceSet MapNetworking(ResourceSet resourceSet)
    {
        var networkTypes = new HashSet<string>([
            SupportedResourceTypes.Vpc,
            SupportedResourceTypes.VpcEndpoint,
            SupportedResourceTypes.Subnet,
            SupportedResourceTypes.Route,
            SupportedResourceTypes.RouteTable,
            SupportedResourceTypes.SubnetRouteTableAssociation,
            SupportedResourceTypes.TransitGateway,
            SupportedResourceTypes.TransitGatewayAttachment,
            SupportedResourceTypes.TransitGatewayRoute,
            SupportedResourceTypes.TransitGatewayRouteTable,
            SupportedResourceTypes.TransitGatewayRouteTableAssociation,
            SupportedResourceTypes.TransitGatewayRouteTablePropagation,
            SupportedResourceTypes.NatGateway,
            SupportedResourceTypes.InternetGateway,
            SupportedResourceTypes.RamResourceShare,
            SupportedResourceTypes.LoadBalancer,
            SupportedResourceTypes.EcsCluster,
            SupportedResourceTypes.EcsService,
        ]);
 
        return Map(resourceSet, networkTypes);
    }

    private static ResourceSet Map(ResourceSet resourceSet, HashSet<string> resourceTypes)
    {
        var resources = resourceSet.Resources
            .Where(r => resourceTypes.Contains(r.Type))
            .ToList();
        return new ResourceSet()
        {
            Resources = resources.ToList(),
            Relationships = resourceSet.Relationships,
            ResourceIndex = resourceSet.ResourceIndex
        };
    }
   
    public static async Task<(ResourceSet, Dictionary<string, Former2Resource>)> ImportAwsResources(
        string path, Dictionary<string, string> accounts, List<string> filterTypes, CancellationToken cancel)
    {
        var allRelationships = AllRelationships.All();
        var resourceFactory = new ResourceModelFactory(allRelationships);
        await using var stream = File.Open(path, FileMode.Open);

        var former2Resources = new Dictionary<string, Former2Resource>();
        await foreach (var result in Former2Parser.ParseStreamAsync(stream, accounts, filterTypes, cancel))
        {
            if (result.Result(out var resource, out _))
            {
                former2Resources[resource.Id] = resource;
            }
        }
        
        var resourceSet = resourceFactory.CreateResourceSet(former2Resources.Values);
        return (resourceSet, former2Resources);
    } 
    
    public static async Task MergeAsync(string dir, string pathOut, 
        Dictionary<string, string> accounts, List<string> filterTypes, CancellationToken cancel)
    {
        var okTypes = new Dictionary<string, int>();
        var invalidTypes = new List<string>();
        var resources = new List<Former2Resource>();

        foreach (var path in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
        {
            var account = Path.GetFileNameWithoutExtension(path);
            Console.WriteLine($"Processing {account}");
            await ImportResources(cancel, path, accounts, filterTypes, okTypes, account, resources, invalidTypes);
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

    private static async Task ImportResources(CancellationToken cancel, string path, 
        Dictionary<string, string> accounts, List<string> filterTypes, 
        Dictionary<string, int> okTypes, string account, 
        List<Former2Resource> resources, List<string> invalidTypes)
    {
        await using var stream = File.Open(path, FileMode.Open);
        await foreach (var result in Former2Parser.ParseStreamAsync(stream, accounts, filterTypes, cancellationToken: cancel))
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