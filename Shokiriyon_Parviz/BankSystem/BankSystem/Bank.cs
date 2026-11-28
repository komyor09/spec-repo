using System;
using System.Collections.Generic;

namespace BankSystem
{
  public class Bank
  {
    private string _name;
    private List<Client> _clients;

    public string Name => _name;
    public Bank(string name){
      _name = name;
      _clients = new List<Client>();
    }

    public void RegisterClient(Client client){
      _clients.Add(client);
      Console.WriteLine($"Клиент {client.Name} зарегистрирован в банке {_name}.");
    }

    private Account? FindAccount(string accountNumber)
    {
      foreach(var client in _clients){
        foreach(var acc in client.Accounts){
          if(acc.AccountNumber == accountNumber)
            return acc;
        }
      }
      return null;
    }
    public void Transfer(string fromAccountNumber, string toAccountNumber, decimal amount){
      Console.WriteLine($"\n--- Перевод {amount} руб. со счёта {fromAccountNumber} на {toAccountNumber} ---");

      Account? fromAccount = FindAccount(fromAccountNumber);
      Account? toAccount = FindAccount(toAccountNumber);

      if(fromAccount == null){
        throw new ArgumentException($"Счёт отправителя {fromAccountNumber} не найден.");
      }
      if(toAccount == null){
        throw new ArgumentException($"Счёт получателя {toAccountNumber} не найден.");
      }

      fromAccount.Withdraw(amount);
      toAccount.Deposit(amount);

      Console.WriteLine("Перевод успешно выполнен!");
    }
  }
}