﻿using System;
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
            using (var pipeline = Pipeline.Create())
            {
                var store = Store.Create(pipeline, opts.Name, opts.Output);
                var dynamicSerializer = new DynamicSerializers(bag.KnownRosMessageDefinitions);
                foreach (var topic in topicList)
                {
                    var messageDef = bag.GetMessageDefinition(topic);
                    var messages = bag.ReadTopic(topic);
                    dynamicSerializer.SerializeMessages(pipeline, store, topic, messageDef.Type, messages);
                }

                // store.Write(pipeline.Diagnostics, "diagnostics");

                pipeline.Run();
            }

            return 1;
        }

        private static int InfoOnBag(Verbs.InfoOption opts)
        {
            // create bag object
            var bag = new RosBag(opts.Input.ToList());

            Console.WriteLine("Info for Bags");
            Console.WriteLine($"Earliest Message Time:{bag.StartTime}");
            Console.WriteLine($"Latest Message Time:{bag.EndTime}");
            Console.WriteLine("Topic List:");
            foreach(var topic in bag.TopicList)
            {
                Console.WriteLine(topic);
            }
            return 1;
        }
    }
}