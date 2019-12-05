using System;
using System.Linq;
using Microsoft.Psi;
using CommandLine;

namespace RosBagConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Ros Bag to PsiStore Converter");

                Parser.Default.ParseArguments<Verbs.InfoOption, Verbs.ConvertOptions>(args)
                    .MapResult(
                        (Verbs.InfoOption opts) => InfoOnBag(opts),
                        (Verbs.ConvertOptions opts) => ConvertBag(opts),
                        errs => 1
                    );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"error: {ex.Message}");
            }
        }

        private static int ConvertBag(Verbs.ConvertOptions opts)
        {
            var bag = new RosBag(opts.Input.ToList());
            var topicList = opts.Topics.Count() > 0 ? opts.Topics : bag.TopicList;

            // create a psi store
            using (var pipeline = Pipeline.Create(true))
            {
                var store = Store.Create(pipeline, opts.Name, opts.Output);
                var dynamicSerializer = new DynamicSerializers(bag.KnownRosMessageDefinitions);
                foreach (var topic in topicList)
                {
                    var messageDef = bag.GetMessageDefinition(topic);
                    var messages = bag.ReadTopic(topic);
                    dynamicSerializer.SerializeMessages(pipeline, store, topic, messageDef.Type, messages);
                }

                // We cannot write the diagnostics information because PsiStudio cannot
                // handle weirdly out of wack times.
                // store.Write(pipeline.Diagnostics, "ReaderDiagnostics");

                pipeline.Run();
            }

            return 1;
        }

        private static int InfoOnBag(Verbs.InfoOption opts)
        {
            // create bag object
            var bag = new RosBag(opts.Input.ToList());

            Console.WriteLine("---------------------");
            Console.WriteLine("Info for Bags");
            Console.WriteLine("---------------------");
            Console.WriteLine($"Earliest Message Time:{bag.StartTime}");
            Console.WriteLine($"Latest Message Time:{bag.EndTime}");
            Console.WriteLine(string.Format("{0,-20}{1,-10}","Name", "Type"));
            Console.WriteLine("-----------------------------------------");
            foreach(var topic in bag.TopicTypeList)
            {
                Console.WriteLine(string.Format("{0,-20}{1,10}",topic.Item1, topic.Item2));
            }
            return 1;
        }
    }
}