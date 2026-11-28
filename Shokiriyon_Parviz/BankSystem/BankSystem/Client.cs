using System;
using System.Collections.Generic;

namespace BankSystem
{
  public class Client{
    private string _name;
    private string _passport;
    private List<Account> _accounts;

    public string Name => _name;
    public string Passport => _passport;

    public IReadOnlyList<Account> Accounts => _accounts.AsReadOnly();
    public Client(string name, string passport){
      _name = name;
      _passport = passport;
      _accounts = new List<Account>();
    }

    public void AddAccount(Account account){
      _accounts.Add(account);
      Console.WriteLine($"Счёт {account.AccountNumber} успешно добавлен клиенту {_name}.");
    }

    public string GetClientInfo(){
      string info = $"Клиент: {_name} (Паспорт: {_passport})\nСчета:\n";
      if(_accounts.Count == 0){
        info += " Нет открытых счетов.\n";
      }else{
        foreach(var acc in _accounts){
          info += $" - {acc.GetAccountInfo()}\n";
        }
      }
      return info;
    }
  }
}