using JsonLD.Core;
using Newtonsoft.Json.Linq;
using NuGet.Services.Metadata.Catalog.JsonLDIntegration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;
using VDS.RDF.Writing;

namespace Transformation
{
    class Program
    {
        // Computer science tells us any data set can be represented with a graph. The W3C have defined a Web friendly, standard,
        // model for graphs. The model is simple: nodes and arcs are represented by URIs. Graph data can be readily represented
        // by an array of triples where the elements of the triple correspond to two nodes and the arc that connects them.
        // The three parts of the triple are cannonically refered to as Subject. Predicate and Object. The model is refered to
        // as RDF. RDF data sets have a some appealing characteristsics, for example, they can often be trivially merged in memory,
        // they are easily queried and transformed and they have an inherent idempotentence that can dramantically simplify much of
        // teh processing involved. RDF is a conceptual model rather than a specific data format. When it comes
        // to formats we will chiefly be concerned with Turtle, a handy human readable format, then XML for legacy integration and
        // finally a new JSON based format called JSON-LD for compatibility with modern Web programming langauges. 

        // Test0: load multiple data sets form Turtle files, merge the data data and print the result.

        static void Test0()
        {
            // Turtle is a standard textual format for graph data sets.

            // Turtle is a very direct representation of the idea of triples of URIs. In order to make things more readable Turtle
            // allows the declaration of namespace prefixes. Otherwise the syntax is minimal, essentially peridod '.' to separate triples,
            // then semicolon ';' to seperate Predicate Object pairs that share the same Subject and comma ',' to seperate Objects that
            // share the same Subject Predicate.

            TurtleParser parser = new TurtleParser();

            // Loading a Turtle file results in a graph structure in memory. A graph structure is an indexed collection of triples.

            IGraph john = new Graph();
            parser.Load(john, @"Data\john.ttl");
            
            IGraph paul = new Graph();
            parser.Load(paul, @"Data\paul.ttl");

            IGraph george = new Graph();
            parser.Load(george, @"Data\george.ttl");

            IGraph ringo = new Graph();
            parser.Load(john, @"Data\ringo.ttl");

            IGraph beatles = new Graph();
            parser.Load(beatles, @"Data\beatles.ttl");

            IGraph all = new Graph();

            // because we have avoided blank nodes these data sets can be trivially merged

            all.Merge(john);
            all.Merge(paul);
            all.Merge(george);
            all.Merge(ringo);
            all.Merge(beatles);

            // Note that if we repeat any of these steps it would be harmless because the structure is intrinsically idempotent

            // Print out the mereged data set

            CompressingTurtleWriter writer = new CompressingTurtleWriter();
            writer.DefaultNamespaces.AddNamespace("c", new Uri("http://tempuri.org/schema/catalog#"));
            writer.DefaultNamespaces.AddNamespace("m", new Uri("http://tempuri.org/schema/music#"));
            writer.DefaultNamespaces.AddNamespace("b", new Uri("http://tempuri.org/british/music#"));
            writer.Save(all, Console.Out);
        }

        // Test1: RDF is a conceptual model, we can load Turtle and save XML

        static void Test1()
        {
            TurtleParser parser = new TurtleParser();

            IGraph john = new Graph();
            parser.Load(john, @"Data\john.ttl");

            RdfXmlWriter writer = new RdfXmlWriter();
            writer.Save(john, Console.Out);
        }

        // test2: most likely the XML you have is not RDFXML, however it is easily translated with XSLT

        static void Test2()
        {
            XDocument original = XDocument.Load(new StreamReader(@"Data\john.xml"));

            XDocument result = new XDocument();

            using (XmlWriter writer = result.CreateWriter())
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(XmlReader.Create(new StreamReader(@"XSLT\musician.xslt")));
                xslt.Transform(original.CreateReader(), writer);
            }

            IGraph john = new Graph();
            RdfXmlParser reader = new RdfXmlParser();
            reader.Load(john, new StringReader(result.ToString()));

            foreach (Triple triple in john.Triples)
            {
                Console.WriteLine("{0} {1} {2}", triple.Subject, triple.Predicate, triple.Object);
            }
        }

        static IGraph LoadXml(string dataFileName, string transformToApplyFileName)
        {
            XDocument original = XDocument.Load(new StreamReader(dataFileName));
            XDocument result = new XDocument();
            using (XmlWriter writer = result.CreateWriter())
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(XmlReader.Create(new StreamReader(transformToApplyFileName)));
                xslt.Transform(original.CreateReader(), writer);
            }
            IGraph graph = new Graph();
            RdfXmlParser reader = new RdfXmlParser();
            reader.Load(graph, new StringReader(result.ToString()));
            return graph;
        }

        // test3: repeat test0 but this time we will be translating and loading an XML document. Having translated
        // the XML to RDF we can merge the data data easily.

        static void Test3()
        {
            IGraph all = new Graph();

            all.Merge(LoadXml(@"Data\john.xml", @"XSLT\musician.xslt"));
            all.Merge(LoadXml(@"Data\paul.xml", @"XSLT\musician.xslt"));
            all.Merge(LoadXml(@"Data\george.xml", @"XSLT\musician.xslt"));
            all.Merge(LoadXml(@"Data\ringo.xml", @"XSLT\musician.xslt"));
            all.Merge(LoadXml(@"Data\beatles.xml", @"XSLT\band.xslt"));

            CompressingTurtleWriter writer = new CompressingTurtleWriter();
            writer.DefaultNamespaces.AddNamespace("c", new Uri("http://tempuri.org/schema/catalog#"));
            writer.DefaultNamespaces.AddNamespace("m", new Uri("http://tempuri.org/schema/music#"));
            writer.DefaultNamespaces.AddNamespace("b", new Uri("http://tempuri.org/british/music#"));
            writer.Save(all, Console.Out);
        }

        static IGraph Load(string fileName)
        {
            IGraph graph = new Graph();
            TurtleParser parser = new TurtleParser();
            parser.Load(graph, fileName);
            return graph;
        }

        // test4: back to Turtle, for convience, now we will look at SPARQL to transform merged data sets

        static void Test4()
        {
            TripleStore store = new TripleStore();
            store.Add(Load(@"Data\robert.ttl"), true);
            store.Add(Load(@"Data\jimmy.ttl"), true);
            store.Add(Load(@"Data\johnpaul.ttl"), true);
            store.Add(Load(@"Data\bonzo.ttl"), true);

            string sparql = new StreamReader(@"Sparql\band.rq").ReadToEnd();

            InMemoryDataset ds = new InMemoryDataset(store);
            ISparqlQueryProcessor processor = new LeviathanQueryProcessor(ds);
            SparqlQueryParser sparqlparser = new SparqlQueryParser();
            SparqlQuery query = sparqlparser.ParseFromString(sparql);

            IGraph ledZep = (IGraph)processor.ProcessQuery(query);

            RdfXmlWriter writer = new RdfXmlWriter();
            writer.Save(ledZep, Console.Out);
        }

        // test5: output in JSON, but its not JSON you would like to work with in Javascrip

        static void Test5()
        {
            TripleStore store = new TripleStore();
            store.Add(Load(@"Data\robert.ttl"), true);
            store.Add(Load(@"Data\jimmy.ttl"), true);
            store.Add(Load(@"Data\johnpaul.ttl"), true);
            store.Add(Load(@"Data\bonzo.ttl"), true);

            string sparql = new StreamReader(@"Sparql\bandWithDetail.rq").ReadToEnd();

            InMemoryDataset ds = new InMemoryDataset(store);
            ISparqlQueryProcessor processor = new LeviathanQueryProcessor(ds);
            SparqlQueryParser sparqlparser = new SparqlQueryParser();
            SparqlQuery query = sparqlparser.ParseFromString(sparql);

            IGraph ledZep = (IGraph)processor.ProcessQuery(query);

            ledZep.Assert(
                ledZep.CreateUriNode(new Uri("http://tempuri.org/british/music#led_zepplin")), 
                ledZep.CreateUriNode(new Uri("http://tempuri.org/schema/catalog#name")), 
                ledZep.CreateLiteralNode("Led Zepplin"));

            JsonLdWriter writer = new JsonLdWriter();
            writer.Save(ledZep, Console.Out);
        }

        // tets6: adding a JSON-LD context allows the JSON-LD processor to pivot the JSON to a more friendly form 

        static void Test6()
        {
            TripleStore store = new TripleStore();
            store.Add(Load(@"Data\robert.ttl"), true);
            store.Add(Load(@"Data\jimmy.ttl"), true);
            store.Add(Load(@"Data\johnpaul.ttl"), true);
            store.Add(Load(@"Data\bonzo.ttl"), true);

            string sparql = new StreamReader(@"Sparql\bandWithDetail.rq").ReadToEnd();

            InMemoryDataset ds = new InMemoryDataset(store);
            ISparqlQueryProcessor processor = new LeviathanQueryProcessor(ds);
            SparqlQueryParser sparqlparser = new SparqlQueryParser();
            SparqlQuery query = sparqlparser.ParseFromString(sparql);

            IGraph ledZep = (IGraph)processor.ProcessQuery(query);

            ledZep.Assert(
                ledZep.CreateUriNode(new Uri("http://tempuri.org/british/music#led_zepplin")),
                ledZep.CreateUriNode(new Uri("http://tempuri.org/schema/catalog#name")),
                ledZep.CreateLiteralNode("Led Zepplin"));

            // in order for the JSON-LD processor to know which way to frame the data we must add an RDF type
            // the frame (which includes the context) specifies the same RDF type

            ledZep.Assert(
                ledZep.CreateUriNode(new Uri("http://tempuri.org/british/music#led_zepplin")),
                ledZep.CreateUriNode(new Uri("http://www.w3.org/1999/02/22-rdf-syntax-ns#type")),
                ledZep.CreateUriNode(new Uri("http://tempuri.org/schema/music#Band")));

            System.IO.StringWriter writer = new System.IO.StringWriter();
            IRdfWriter rdfWriter = new JsonLdWriter();
            rdfWriter.Save(ledZep, writer);
            writer.Flush();

            JToken frame = JToken.Parse(new StreamReader(@"Context\band.json").ReadToEnd());

            JToken flattened = JToken.Parse(writer.ToString());
            JObject framed = JsonLdProcessor.Frame(flattened, frame, new JsonLdOptions());
            JObject compacted = JsonLdProcessor.Compact(framed, framed["@context"], new JsonLdOptions());

            Console.WriteLine(compacted);
        }

        static void Main(string[] args)
        {
            try
            {
                //Test0();
                //Test1();
                //Test2();
                //Test3();
                //Test4();
                //Test5();
                //Test6();

                Serialization.Test0();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
