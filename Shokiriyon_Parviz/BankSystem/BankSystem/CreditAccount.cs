namespace BankSystem
{
  public class CreditAccount : Account
  {
    private decimal _creditLimit;
    public decimal CreditLimit => _creditLimit;
    public CreditAccount(string accountNumber, string ownerName, decimal creditLimit, decimal initialBalance = 0): base(accountNumber, ownerName, initialBalance){
      _creditLimit = creditLimit;
    }

    public override void Withdraw(decimal amount){
      if(amount <= 0){
        throw new ArgumentException("Сумма должна быть больше нуля");
      }
      if(amount > Balance + _creditLimit){
        throw new InvalidOperationException($"Превышен кредитный лимит. Доступно: {Balance + _creditLimit} руб.");
      }

      _balance -= amount;
      Console.WriteLine($"Снято {amount} руб. Баланс: {_balance} руб.");
      // if(amount <= Balance){
      //   base.Withdraw(amount);
      // }else{
      //   decimal needed = amount - Balance;
      //   Console.WriteLine($"Снято {amount} руб. (включая {needed} руб. в кредит). Баланс: {Balance - amount} руб.");
      // }
    }

    public override string GetAccountInfo(){
      return $"[Кредитный] {base.GetAccountInfo()} | Лимит: {_creditLimit} руб.";
    }
  }
}