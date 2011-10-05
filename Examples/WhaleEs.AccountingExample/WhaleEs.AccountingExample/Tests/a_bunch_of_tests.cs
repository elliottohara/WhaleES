using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using WhaleES;
using WhaleES.Configuration;
using WhaleEs.AccountingExample.Domain;
using WhaleEs.AccountingExample.Events;

namespace WhaleEs.AccountingExample.Tests
{
    [TestFixture]
    public class a_bunch_of_tests
    {
        private string _key;
        private string _secret;
        private Repository<Account> _repository;

        [SetUp]
        public void bootstrap()
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
            
            ConfigureWhaleEs
                .With().WithBucket("WhaleES_tests")
                .WithProtocolBufferSerialization()
                .WithSecret(_secret)
                .WithKey(_key);

            _repository = RepositoryFactory.CreateRepositoryFor<Account>();

        }
        [Test] 
        public void create_account()
        {
            var accountOpenedEvent = new AccountOpened {AccountId = "testing", InitialDeposit = 100, On = DateTime.Now};
            var account = new Account(accountOpenedEvent);
            _repository.Put(accountOpenedEvent.AccountId,account);
        }
        [Test]
        public void get_account()
        {
            var account = _repository.Get("boomboompow");
            Console.WriteLine(account._balance);
            foreach (var activity in account.Activity)
            {
                Console.WriteLine(activity);
            }
        }
        [Test]
        public void lets_do_some_stuff()
        {
            var id = "boomboompow";
            var someNewAccountEvent = new AccountOpened
                                          {AccountId = id, InitialDeposit = 1000, On = DateTime.Now};
            var account = new Account(someNewAccountEvent);
            account.Deposit(45);
            account.Withdraw(17);
            
            _repository.Put(id,account);

            var accountFromRepo = _repository.Get(id);
            Assert.AreEqual(1000 + 45 -17,accountFromRepo._balance);
            //test to make sure we're not applying events multiple times
            _repository.Put(accountFromRepo.Id, accountFromRepo);
            
        }
        [Test]
        public void make_a_rich_dude()
        {
            //var account =
            //    new Account(new AccountOpened {AccountId = "richdude", InitialDeposit = 10000, On = DateTime.Now});
            //for(var i = 1; i < 10000; i++)
            //{
            //    account.Deposit(10000);
            //}
            //_repository.Put(account.Id,account);

            var dudeFromRepo = _repository.Get("richdude");
            var originalBalance = dudeFromRepo._balance;
            _repository.Put("richdude", dudeFromRepo);
            var again = _repository.Get(dudeFromRepo.Id);
            Assert.AreEqual(originalBalance,again._balance);
        }
    }
}