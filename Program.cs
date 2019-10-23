using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Psi;
using Microsoft.Psi.Common;
using Microsoft.Psi.Persistence;
using Microsoft.Psi.Serialization;
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
            var bag = new RosBag(opts.Input);
            IEnumerable<string> topicList;
            if (opts.Topics.Count() > 0)
            {
                topicList = opts.Topics;
            }
            else
            {
                topicList = bag.TopicList;
            }
            // create a psi store
            var store = new StoreWriter(opts.Name, opts.Output);
            var dynamicSerializer = new DynamicSerializers(bag.KnownRosMessageDefinitions);

            foreach (var topic in topicList)
            {
                var topicDefinition = bag.GetMessageDefinition(topic);
                var messages = bag.ReadTopic(topic);
                dynamicSerializer.SerializeMessage(store, topic, messages);
            }
            return 1;
        }

        private static int InfoOnBag(Verbs.InfoOption opts)
        {
            // create bag object
            var bag = new RosBag(opts.Input);

            Console.WriteLine("Info for Bag");
            Console.WriteLine("Topic List:");
            foreach(var topic in bag.TopicList)
            {
                Console.WriteLine(topic);
            }
            return 1;
        }
    }
}