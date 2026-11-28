namespace BankSystem
{
public class Account{
  private string _accountNumber;
  protected decimal _balance;
  private string _ownerName;

  public string AccountNumber => _accountNumber;
  public decimal Balance => _balance;
  public string OwnerName => _ownerName;

  public Account(string accountNumber, string ownerName, decimal initialBalance = 0){
    _accountNumber = accountNumber;
    _ownerName = ownerName;
    _balance = initialBalance;
  }
  public void Deposit(decimal amount){
    if(amount <= 0){
      throw new ArgumentException("Сумма должна быть больше нуля");
    }
    _balance += amount;
    Console.WriteLine($"Пополнено {amount} руб. Баланс: {_balance} руб.");
  }

  public virtual void Withdraw(decimal amount){
    if(amount <= 0){
      throw new ArgumentException("Сумма должна быть больше нуля");
    }
    if(amount > _balance){
      throw new InvalidOperationException("Недостаточно средств");
    }
    _balance -= amount;
    Console.WriteLine($"Снято {amount} руб. Баланс: {_balance} руб.");
  }

  public virtual string GetAccountInfo()
  {
    return $"Счёт {_accountNumber} | Владелец: {_ownerName} | Баланс: {_balance} руб.";
  }
}
}