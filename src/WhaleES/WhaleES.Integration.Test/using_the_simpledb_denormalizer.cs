using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using NUnit.Framework;
using WhaleES.Denormalizers;

namespace WhaleES.Integration.Test
{
    [TestFixture]
    public class using_the_simpledb_denormalizer
    {
        private string _key;
        private string _secret;
        private AmazonSimpleDB _client;

        [SetUp] 
        public void arrange()
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
            _client = Amazon.AWSClientFactory.CreateAmazonSimpleDBClient(_key, _secret);
        }
        [Test]
        public void put()
        {
            var entity = new SimpleDbTestEntity {SomeProperty = "Test this"};
            entity.Id = new Guid("42bfbad1-c128-46f1-8f59-62580ca6bf5c");
            var denormalizer = new TestDenormalizer(_client);
            denormalizer.Put(entity);

            var getRequest = new GetAttributesRequest()
                .WithDomainName(typeof (SimpleDbTestEntity).FullName)
                .WithItemName(entity.Id.ToString());
            var results = _client.GetAttributes(getRequest).GetAttributesResult;
            foreach (var attribute in results.Attribute)
            {
                Console.WriteLine("{0}:{1}",attribute.Name,attribute.Value);
            }
        }
        [Test]
        public void the_repo()
        {
            var repo = new SimpleDbRepository<SimpleDbTestEntity>(_client);
            var result = repo.Get("ba565691-a83a-4836-9023-a643b4b9b0f9");
            Console.WriteLine(result.Id);
            Console.WriteLine(result.CreatedOn);
        }
        [Test]
        public void get_with_parameters()
        {
            var repo = new SimpleDbRepository<SimpleDbTestEntity>(_client);
            var results = repo.Get(e => e.Id == new Guid("ba565691-a83a-4836-9023-a643b4b9b0f9"));
            Console.WriteLine(results.First().Id);
        }
        [Test]
        public void get_with_predicate()
        {
            var repo = new SimpleDbRepository<SimpleDbTestEntity>(_client);
            var results = repo.Get(t => t.Id == new Guid("42bfbad1-c128-46f1-8f59-62580ca6bf5c") && t.SomeProperty == "Test this");
            foreach (var simpleDbTestEntity in results)
            {
                Console.WriteLine(simpleDbTestEntity.SomeProperty);
                Console.WriteLine(simpleDbTestEntity.Id);
            }


        }
        [Test]
        public void get_all()
        {
            var repo = new SimpleDbRepository<SimpleDbTestEntity>(_client);
            foreach(var item in repo.Get())
            {
                Console.WriteLine(item.Id);
                Console.WriteLine(item.SomeProperty);
                Console.WriteLine(item.CreatedOn);
                Console.WriteLine("**************");
            }
        }
    }
    public class TestDenormalizer:SimpleDbDenormalizer<SimpleDbTestEntity>
    {
        public TestDenormalizer(AmazonSimpleDB client) : base(client)
        {
        }

        protected override Func<SimpleDbTestEntity, object> IdProperty
        {
            get { return e => e.Id; }
        }
    }
    public class SimpleDbTestEntity
    {
        public SimpleDbTestEntity()
        {
            Id = Guid.NewGuid();
            CreatedOn = DateTime.Now;
        }
        public Guid Id { get; set; }
        public string SomeProperty { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}