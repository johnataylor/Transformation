using JsonLD.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Services.Metadata.Catalog.JsonLDIntegration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Writing;

namespace Transformation
{
    public class Dependency
    {
        public string Id { get; set; }
        public string Range { get; set; }
    }

    public class DependencyGroup
    {
        List<Dependency> _dependencies = new List<Dependency>();

        public string TargetFramework { get; set; }
        public List<Dependency> Dependencies { get { return _dependencies; } }
    }

    public class Package
    {
        List<DependencyGroup> _dependencyGroups = new List<DependencyGroup>();

        public string Id { get; set; }
        public string Version { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Summary { get; set; }
        public bool RequiresLicenseAcceptance { get; set; }
        public Uri LicenseUri { get; set; }
        public List<DependencyGroup> DependencyGroups { get { return _dependencyGroups; } }
    }

    class Serialization
    {
        public static void Test0()
        {
            Package package = new Package
            {
                Id = "MyPackage",
                Version = "1.0.0",
                Title = "My Package",
                Description = "This is my package description",
                Summary = "This is my package summary",
                RequiresLicenseAcceptance = true,
                LicenseUri = new Uri("http://tempuri.org/license"),
                DependencyGroups =
                {
                    new DependencyGroup 
                    { 
                        TargetFramework = "la",
                        Dependencies = 
                        {
                            new Dependency { Id = "a", Range = "[1.0.0]" },
                            new Dependency { Id = "b", Range = "[1.0.0]" },
                            new Dependency { Id = "c", Range = "[1.0.0]" }
                        }
                    },
                    new DependencyGroup
                    { 
                        TargetFramework = "de",
                        Dependencies = 
                        {
                            new Dependency { Id = "x", Range = "[1.0.0]" },
                            new Dependency { Id = "y", Range = "[1.0.0]" }
                        }
                    },
                    new DependencyGroup 
                    { 
                        TargetFramework = "da", 
                        Dependencies = 
                        {
                            new Dependency { Id = "z", Range = "[1.0.0]" }
                        }
                    },
                }
            };

            string json = JsonConvert.SerializeObject(package, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
            });

            JObject obj = JObject.Parse(json);

            JObject context = JObject.Parse((new StreamReader(@"Context\package.json")).ReadToEnd());
            obj["@context"] = context["@context"];

            JToken flattened = JsonLdProcessor.Flatten(obj, new JsonLdOptions());

            string flattenedJson = flattened.ToString();

            Console.WriteLine(flattenedJson);

            IRdfReader rdfReader = new JsonLdReader();
            IGraph graph = new Graph();
            rdfReader.Load(graph, new StringReader(flattenedJson));

            CompressingTurtleWriter writer = new CompressingTurtleWriter();
            writer.CompressionLevel = 0;
            writer.DefaultNamespaces.AddNamespace("nuget", new Uri("http://nuget.org/schema#"));
            writer.Save(graph, Console.Out);
        }
    }
}
