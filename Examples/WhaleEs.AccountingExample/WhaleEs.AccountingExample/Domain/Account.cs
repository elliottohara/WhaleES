using System;
using System.Collections.Generic;
using WhaleEs.AccountingExample.Events;

namespace WhaleEs.AccountingExample.Domain
{
    public class Account
    {
        public decimal _balance;
        private string _id;
        private const decimal OVER_LIMIT_FEE = 25;
        public List<String> Activity { get; set; }
        public Account(AccountOpened @event):this()
        {
            Apply(@event);
        } 
        public Account()
        {
            UncommittedEvents = new List<object>();
            Activity = new List<string>();
        }
        public Account Deposit(decimal amount)
        {
            Apply(new DepositMade {Amount = amount, AccountId = _id,On=DateTime.Now});
            return this;
        }

        public List<object> UncommittedEvents { get; set; }

        public string Id{get { return _id; }}

        public Account Withdraw(decimal amount)
        {
            Apply(new WithdrawMade { AccountId = _id, On = DateTime.Now, Amount = amount });
            if(_balance < 0)
            {
                Apply(new AccountOverdrawn{On=DateTime.Now,AccountId = _id,Amount = _balance});
                return this;
            }
            return this;
        }
        public void Apply(AccountOpened @event,bool isReplaying = false)
        {
            _balance = @event.InitialDeposit;
            _id = @event.AccountId;
            Activity.Add("Account Opened On " + @event.On + " with $" + @event.InitialDeposit );
            if (!isReplaying) UncommittedEvents.Add(@event);
        }
        public void Apply(DepositMade depositMade, bool isReplaying = false)
        {
            _balance += depositMade.Amount;
            Activity.Add("Deposit made on " + depositMade.On + " $" + depositMade.Amount);
            if (!isReplaying) UncommittedEvents.Add(depositMade);
        }
        public void Apply(AccountOverdrawn @event, bool isReplaying = false)
        {
            _balance -= OVER_LIMIT_FEE;
            Activity.Add("OVERDRAWN on " + @event.On + " $" + @event.Amount);
            if (!isReplaying) UncommittedEvents.Add(@event);
        }
        public void Apply(WithdrawMade @event,bool isReplaying = false)
        {
            _balance -= @event.Amount;
            Activity.Add("Withdraw on " + @event.On + " $" + @event.Amount);
            if (!isReplaying) UncommittedEvents.Add(@event);
            
        }
    }

    
}