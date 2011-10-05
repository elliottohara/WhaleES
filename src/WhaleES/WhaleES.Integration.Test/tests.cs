using System;
using System.Collections.Generic;
using System.IO;
using Amazon.S3;
using NUnit.Framework;
using WhaleES.Serialization;

namespace WhaleES.Integration.Test
{
   
    [TestFixture]
    public class tests
    {
        private string _key;
        private string _secret;
        
        private AmazonS3 client;
        private string _id;

        [SetUp]
        public void get_amazon_client()
        {
            var streamReader = new StreamReader(@"C:\SpecialSuperSecret\elliott.ohara@gmail.com.txt");
            var dictionary = new Dictionary<string, string>();
            while (!streamReader.EndOfStream)
            {
                var line = streamReader.ReadLine();
                var keyValue = line.Split(':');
                dictionary.Add(keyValue[0], keyValue[1]);
            }
            _key = dictionary["UserName"];
            _secret = dictionary["Password"];
            client = Amazon.AWSClientFactory.CreateAmazonS3Client(_key,_secret);
            _id = "test";
        }
        [Test]
        public void store_some_events()
        {
           
            var repo = new StreamOfEventsFor<TestAggrigateRoot>(client,"WhaleES_tests",new JsonSerializer());
            var ar = new TestAggrigateRoot {Id = _id};
            var @event = new ATestEvent {What = "Blah"};
            repo.Persist(ar.Id,@event);
            repo.Dispose();


        }
        [Test]
        public void GetSomeEvents()
        {
            var repo = new StreamOfEventsFor<TestAggrigateRoot>(client, "WhaleES_tests", new JsonSerializer());
            var events = repo.GetEventStream(_id);
            foreach (var @event in events)
            {
                var testEvent = @event as ATestEvent;
                if (testEvent != null)
                    Console.WriteLine(testEvent.What);
            }
        }
    }

    public class TestAggrigateRoot
    {
        public string Id { get; set; }
    }

    public class ATestEvent
    {
        public string What { get; set; }
    }
}
