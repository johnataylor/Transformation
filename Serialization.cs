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
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;
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
        public static Package CreatePackage()
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

            return package;
        }

        public static void Test0()
        {
            Package package = CreatePackage();

            string compactedJson = JsonConvert.SerializeObject(package, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
            });

            Console.WriteLine("---------------- COMPACTED ----------------");
            Console.WriteLine(compactedJson);
            Console.WriteLine("---------------- --------- ----------------");

            JObject obj = JObject.Parse(compactedJson);

            JObject context = JObject.Parse((new StreamReader(@"Context\package.json")).ReadToEnd());
            obj["@context"] = context["@context"];

            JToken flattened = JsonLdProcessor.Flatten(obj, new JsonLdOptions());

            string flattenedJson = flattened.ToString();

            Console.WriteLine("---------------- FLATTENED ----------------");
            Console.WriteLine(flattenedJson);
            Console.WriteLine("---------------- --------- ----------------");

            IRdfReader rdfReader = new JsonLdReader();
            IGraph graph = new Graph();
            rdfReader.Load(graph, new StringReader(flattenedJson));

            Console.WriteLine("---------------- TURTLE ----------------");
            CompressingTurtleWriter writer = new CompressingTurtleWriter();
            writer.CompressionLevel = 0;
            writer.DefaultNamespaces.AddNamespace("nuget", new Uri("http://nuget.org/schema#"));
            writer.Save(graph, Console.Out);
            Console.WriteLine("---------------- ------ ----------------");
        }

        public static void Test1()
        {
            Package package = CreatePackage();

            string compactedJson = JsonConvert.SerializeObject(package, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
            });

            //Console.WriteLine("---------------- COMPACTED ----------------");
            //Console.WriteLine(compactedJson);
            //Console.WriteLine("---------------- --------- ----------------");

            JObject obj = JObject.Parse(compactedJson);

            JObject context = JObject.Parse((new StreamReader(@"Context\package.json")).ReadToEnd());
            obj["@context"] = context["@context"];

            JToken flattened = JsonLdProcessor.Flatten(obj, new JsonLdOptions());

            string flattenedJson = flattened.ToString();

            //Console.WriteLine("---------------- FLATTENED ----------------");
            //Console.WriteLine(flattenedJson);
            //Console.WriteLine("---------------- --------- ----------------");

            IRdfReader rdfReader = new JsonLdReader();
            IGraph graph = new Graph();
            rdfReader.Load(graph, new StringReader(flattenedJson));

            //  now we have a graph we can do interesting things like apply SPARQL transforms

            INode root = MapType(graph, "Transformation.Package, Transformation", new Uri("http://nuget.org/schema#Package")).FirstOrDefault();

            Uri packageUri = MakePackageUri(graph, root, "http://tempuri.org/package");

            graph = RewriteBlankNodes(graph, packageUri, root);

            //  now we have a graph without blank nodes which makes things like merging data sets a trivial operation 

            //graph = ApplySparqlTransform(graph, @"Sparql\package.rq");

            Console.WriteLine("---------------- TURTLE ----------------");
            CompressingTurtleWriter writer = new CompressingTurtleWriter();
            writer.CompressionLevel = 0;
            writer.DefaultNamespaces.AddNamespace("nuget", new Uri("http://nuget.org/schema#"));
            writer.DefaultNamespaces.AddNamespace("package", new Uri(packageUri.ToString() + "#"));
            writer.Save(graph, Console.Out);
            Console.WriteLine("---------------- ------ ----------------");
        }

        static IEnumerable<INode> MapType(IGraph graph, string serializationType, Uri rdfType)
        {
            INode SerializationTypePredicate = graph.CreateUriNode(new Uri("http://nuget.org/schema#$type"));
            INode RdfTypePredicate = graph.CreateUriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));

            // assert the RDF type where we see the corresponding serialization type

            foreach (Triple triple in graph.GetTriplesWithPredicateObject(SerializationTypePredicate, graph.CreateLiteralNode(serializationType)))
            {
                graph.Assert(triple.Subject, RdfTypePredicate, graph.CreateUriNode(rdfType));
            }

            //  in addition to updating the graph return the list of subjects now typed

            return graph.GetTriplesWithPredicateObject(RdfTypePredicate, graph.CreateUriNode(rdfType)).Select((t) => t.Subject);
        }

        static IGraph ApplySparqlTransform(IGraph original, string filename)
        {
            string sparql = new StreamReader(filename).ReadToEnd();

            TripleStore store = new TripleStore();
            store.Add(original);

            InMemoryDataset ds = new InMemoryDataset(store);
            ISparqlQueryProcessor processor = new LeviathanQueryProcessor(ds);
            SparqlQueryParser sparqlparser = new SparqlQueryParser();
            SparqlQuery query = sparqlparser.ParseFromString(sparql);

            IGraph result = (IGraph)processor.ProcessQuery(query);

            return result;
        }

        static void Walk(IGraph graph, INode subject, int depth)
        {
            //  this is an example of a recursive walk into a set of triples we know logically forms a tree
            //  this is conceptually all a json-ld processor is doing when it builds nested json from a flat set
            //  in the world of RDF the processing steps are very transparent, there really is no magic

            foreach (Triple triple in graph.GetTriplesWithSubject(subject))
            {
                Console.WriteLine("{0} {1} {2} {3}", Indent(depth), triple.Subject, triple.Predicate, triple.Object);

                Walk(graph, triple.Object, depth + 1);
            }
        }

        static string Indent(int depth)
        {
            StringBuilder sb = new StringBuilder();
            while (depth-- > 0)
            {
                sb.Append(' ');
            }
            return sb.ToString();
        }

        static Uri MakePackageUri(IGraph graph, INode packageNode, string baseAddress)
        {
            string id = graph.GetTriplesWithSubjectPredicate(packageNode, graph.CreateUriNode(new Uri("http://nuget.org/schema#Id"))).FirstOrDefault().Object.ToString();
            string version = graph.GetTriplesWithSubjectPredicate(packageNode, graph.CreateUriNode(new Uri("http://nuget.org/schema#Version"))).FirstOrDefault().Object.ToString();

            return new Uri(baseAddress + "/" + id + "." + version + ".json");
        }

        static IGraph RewriteBlankNodes(IGraph graph, Uri uri, INode root)
        {
            IGraph target = new Graph();

            foreach (Triple triple in graph.Triples)
            {
                INode s = (triple.Subject is IBlankNode) ? CreateNode(target, triple.Subject, uri) : triple.Subject.CopyNode(target);
                INode p = triple.Predicate.CopyNode(target);
                INode o = (triple.Object is IBlankNode) ? CreateNode(target, triple.Object, uri) : triple.Object.CopyNode(target);

                target.Assert(s, p, o);
            }

            return target;
        }

        static INode CreateNode(IGraph graph, INode blankNode, Uri uri)
        {
            string fragment = blankNode.ToString().TrimStart('_').TrimStart(':');
            return graph.CreateUriNode(new Uri(uri.ToString() + "#" + fragment));
        }

        static void CollectBlankNodes(IGraph graph, INode subject, IDictionary<string, string> blankNodes, Stack<INode> path)
        {
            foreach (Triple triple in graph.GetTriplesWithSubject(subject))
            {
                if (triple.Object is IBlankNode)
                {
                    path.Push(triple.Predicate);

                    blankNodes[triple.Object.ToString()] = MakePath(path);

                    CollectBlankNodes(graph, triple.Object, blankNodes, path);

                    path.Pop();
                }
            }
        }

        static string MakePath(Stack<INode> path)
        {
            StringBuilder sb = new StringBuilder();

            foreach (INode node in path.Reverse())
            {
                string s = node.ToString();
                sb.AppendFormat("/{0}", s.Substring(s.IndexOf('#') + 1));
            }

            return sb.ToString().TrimStart('/');
        }
    }
}
