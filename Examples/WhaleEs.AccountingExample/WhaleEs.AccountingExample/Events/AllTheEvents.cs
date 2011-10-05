using System;
using System.Runtime.Serialization;

namespace WhaleEs.AccountingExample.Events
{
    [DataContract][Serializable]
    public class AccountOpened
    {
        [DataMember(Order = 1)] public DateTime On { get; set; }
        [DataMember(Order = 2)] public String AccountId { get; set; }
        [DataMember(Order = 3)] public decimal InitialDeposit { get; set; }
    }
    [DataContract][Serializable]
    public class DepositMade
    {
        [DataMember(Order = 1)] public string AccountId { get; set; }
        [DataMember(Order = 2)] public decimal Amount { get; set; }
        [DataMember(Order = 3)] public DateTime On { get; set; }
    }
    [DataContract][Serializable]
    public class WithdrawMade
    {
        [DataMember(Order = 1)] public string AccountId { get; set; }
        [DataMember(Order = 2)] public decimal Amount { get; set; }
        [DataMember(Order = 3)] public DateTime On { get; set; }
    }
    [DataContract][Serializable]
    public class AccountOverdrawn
    {
        [DataMember(Order = 1)] public string AccountId { get; set; }
        [DataMember(Order = 2)] public decimal Amount { get; set; }
        [DataMember(Order = 3)] public DateTime On { get; set; }
    }
}