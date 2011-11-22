using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using NUnit.Framework;
using WhaleES.Caching;
using WhaleES.Serialization;

namespace WhaleES.Integration.Test
{
   
    [TestFixture]
    public class tests
    {
        private string _key;
        private string _secret;
        
        private AmazonS3 _client;
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
            _client = Amazon.AWSClientFactory.CreateAmazonS3Client(_key,_secret);
            _id = "some_changes_for_config";
        }
        [Test]
        public void test_backward_compliance_of_new_version()
        {
            var v1Event = new ATestEvent {What = "Test"};
            var serializer = new ProtoBufSerializer();
            var bytes = serializer.Serialize(v1Event);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(bytes));
            var prefix = "WhaleES.Integration.Test.TestAggrigateRoot";
            var putRequest = (PutObjectRequest)new PutObjectRequest()
                .WithBucketName("WhaleES_tests")
                .WithKey(prefix + "/BACKWARD_COMPATIBILITY_TEST")
                .WithInputStream(stream);

            _client.PutObject(putRequest);

            var aNewEvent = new ATestEvent {What = "THing"};
            var serialzed = serializer.Serialize(aNewEvent);
            var v2Stream = new MemoryStream(Encoding.UTF8.GetBytes(serialzed));


            var v2PutRequest = (PutObjectRequest)new PutObjectRequest()
                .WithBucketName("WhaleES_tests")
                .WithKey(prefix + "/BACKWARD_COMPATIBILITY_TEST/SOMEKEY")
                .WithInputStream(v2Stream);

            _client.PutObject(v2PutRequest);


            var repo = new StreamOfEventsFor<TestAggrigateRoot>(_client, "WhaleES_tests", serializer);
            var events = repo.GetEventStream("BACKWARD_COMPATIBILITY_TEST");
            Assert.AreEqual(2,events.Count());



        }
        [Test]
        public void store_some_events()
        {
           
            var repo = new StreamOfEventsFor<TestAggrigateRoot>(_client,"WhaleES_tests",new ProtoBufSerializer());
            var lotsOfEvents = new List<ATestEvent>();
            var ar = new TestAggrigateRoot { Id = _id };
            for (var i = 0; i < 1000; i++)
            {
                lotsOfEvents.Add(new ATestEvent {What = Guid.NewGuid().ToString()});
                if (lotsOfEvents.Count % 20 == 0)
                {
                    repo.Persist(ar.Id, lotsOfEvents.ToArray());
                    lotsOfEvents.Clear();
                }
            }
            
            repo.Dispose();


        }
        [Test]
        public void GetSomeEvents()
        {
            var stopWatch = new Stopwatch();
            for (var i = 1; i < 9; i++)
            {
                var repo = new StreamOfEventsFor<TestAggrigateRoot>(_client, "WhaleES_tests", new ProtoBufSerializer());
                var number = 0;
                stopWatch.Reset();
                stopWatch.Start();
                var events = repo.GetEventStream(_id);
                foreach (var @event in events)
                { 
                    var testEvent = @event as ATestEvent;
                    if (testEvent == null) continue;
                    number++;
                }
                stopWatch.Stop();
                
                Console.WriteLine("fetched {0} events in {1} milliseconds", number,
                                  stopWatch.ElapsedMilliseconds);
                stopWatch.Reset();
                stopWatch.Start();
                repo.Persist(_id,new ATestEvent{What = Guid.NewGuid().ToString()});
                stopWatch.Stop();
                Console.WriteLine("Put took " + stopWatch.ElapsedMilliseconds + " milliseconds");

                var secondPass = 0;
                stopWatch.Reset();
                stopWatch.Start();
                foreach (var e in repo.GetEventStream(_id))
                {
                    secondPass++;
                }
                stopWatch.Stop();
                Console.WriteLine("fetched {0} events in second pass in {1} milliseconds",secondPass,stopWatch.ElapsedMilliseconds);
                stopWatch.Reset();
                stopWatch.Start();
                var s = 0;
                foreach (var @event in repo.GetEventStream(_id))
                {
                    s++;
                }
                stopWatch.Stop();
                Console.WriteLine("no new events pass took {0} milliseconds",stopWatch.ElapsedMilliseconds);

            }
        }
    }

    public class TestAggrigateRoot
    {
        public string Id { get; set; }
    }
    [DataContract]
    public class ATestEvent
    {
        [DataMember(Order = 1)]
        public string What { get; set; }
    }
}
