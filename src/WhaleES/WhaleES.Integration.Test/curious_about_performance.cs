using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Amazon.S3;
using NUnit.Framework;
using WhaleES.Configuration;
using WhaleES.Serialization;

namespace WhaleES.Integration.Test
{
    [TestFixture]
    public class curious_about_performance
    {
        private string _key;
        private string _secret;

        private AmazonS3 client;
        private string _id;
        private StreamOfEventsFor<Account> _eventStore;
        
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
            client = Amazon.AWSClientFactory.CreateAmazonS3Client(_key, _secret);
            _id = "someotherthing";
            _eventStore = new StreamOfEventsFor<Account>(client, "WhaleES_tests", new ProtoBufSerializer());
        }

        [Test]
        public void store_a_bunch_of_events()
        {
            var ar = new Account {Id = _id};
            var eventsToPersist = new List<object>();
            eventsToPersist.Add(new AccountOpened{InitialDeposit = 5,Name = "test"});
            var startEventsAt = DateTime.Now.AddYears(-1);
            for(var i = 0; i< 100; i++)
            {
               object @event = null;
               if(i % 1 == 0)
               {
                   @event = new Deposit {Amount = i,AccountId = ar.Id,At = startEventsAt.AddDays(i) };
               }else
               {
                   @event = new WithDraw {Amount = i, AccountId = ar.Id, At = startEventsAt.AddDays(i)};
               }

               eventsToPersist.Add(@event);
            }
            _eventStore.Persist(ar.Id,eventsToPersist.ToArray());
        }
        [Test]
        public void use_repo()
        {
            ConfigureWhaleEs.With()
                .ForAmazon()
                .KeyIs(_key)
                .SecretIs(_secret)
                .UseS3BucketName("WhaleES_tests")
                .ToSerialize()
                .UseProtocolBuffers();

            var repo = RepositoryFactory.CreateRepositoryFor<Account>();
            var ar = repo.Get(_id);
            foreach(var item in ar.Activity)
            {
                Console.WriteLine(item);
            }
            Console.WriteLine(ar.Activity.Count() + " total lines of activity");

        }
        [Test]
        public void serializer_test()
        {
            var events = new List<object>();
            for(var i = 1; i< 1000000;i++)
            {
                object @event = null;
                if(i % 1 == 0)
                {
                    @event = new Deposit {AccountId = "test", Amount = i, At = DateTime.Now};
                }else
                {
                    @event = new WithDraw {AccountId = "test", Amount = i, At = DateTime.Now};
                }
                events.Add(@event);
            }
            var msJsonSerializer = new JsonSerializer();
            ShowTimeItTakes(events,msJsonSerializer);
            var protoBufSerializer = new ProtoBufSerializer();
            ShowTimeItTakes(events,protoBufSerializer);
        }

        private static void ShowTimeItTakes(List<object> events,ISerializer serializer)
        {
            var startJsonAt = DateTime.Now;
            serializer.Serialize(events);
            Console.WriteLine(serializer.GetType() + " " + (DateTime.Now - startJsonAt).TotalMilliseconds);
        }

        [Test]
        public void get_events()
        {
            var ar = new Account {Id = _id};
            var startedGettingStreamAt = DateTime.Now;
            var stream = _eventStore.GetEventStream(_id).ToList();
            //Console.WriteLine("Took {0} milliseconds to get stream of {1} events", (DateTime.Now - startedGettingStreamAt).TotalMilliseconds, stream.Count());
            var startedApplyingEventsAt = DateTime.Now;
            foreach(var @event in stream)
            {
                var eventType = @event.GetType();
                var applyMethod = ar.GetType().GetMethods()
                    .FirstOrDefault(
                        mi => mi.Name == "Apply" && mi.GetParameters().Any(pi => pi.ParameterType == eventType));
                if (applyMethod != null)
                    applyMethod.Invoke(ar, new[]{@event});
            }
            Console.WriteLine("Took {0} milliseconds to playback events",(DateTime.Now -  startedApplyingEventsAt).TotalMilliseconds);
            foreach (var activity in ar.Activity)
            {
                Console.WriteLine(activity);
            }
            
        }
}

    public class Account
    {
        public string Id { get; set; }
        public decimal _balance;
        private string _name;
        public List<string> Activity = new List<string>(); 

        public void Apply(WithDraw withdraw)
        {
            _balance -= withdraw.Amount;
            Activity.Add("on " + withdraw.At.ToShortDateString() + " Withdraw of " + withdraw.Amount + " balance:" + _balance);
        }
        public void Apply(Deposit deposit)
        {
            _balance += deposit.Amount;
            Activity.Add("on " + deposit.At.ToShortDateString() + " Deposit of " + deposit.Amount + " balance:" + _balance);
        }
        public void Apply(AccountOpened accountOpened)
        {
            _name = accountOpened.Name;
            _balance = accountOpened.InitialDeposit;
            Activity.Add("on " + accountOpened.At.ToShortDateString() + " Account opened with " + accountOpened.InitialDeposit);
        }
        
    }
    [DataContract]
    public class Deposit
    {
        [DataMember(Order = 1)] public Decimal Amount { get; set; }
        [DataMember(Order = 2)] public string AccountId { get; set; }
        [DataMember(Order = 3)] public DateTime At { get; set; }
        
    }
    [DataContract]
    public class AccountOpened
    {
        [DataMember(Order = 1)] public string Name { get; set; }
        [DataMember(Order = 2)] public decimal InitialDeposit { get; set; }
        [DataMember(Order = 3)] public DateTime At { get; set; }
    }
    [DataContract]
    public class WithDraw 
    {
        [DataMember(Order = 1)] public decimal Amount { get; set; }
        [DataMember(Order = 2)] public string AccountId { get; set; }
        [DataMember(Order = 3)] public DateTime At { get; set; }
    }
}