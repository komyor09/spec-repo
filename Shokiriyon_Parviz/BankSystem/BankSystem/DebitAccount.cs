namespace BankSystem
{
  public class DebitAccount : Account
  {
    public DebitAccount(string accountNumber, string ownerName, decimal initialBalance = 0) : base(accountNumber, ownerName, initialBalance)
    {    }
  
    public override string GetAccountInfo(){
      return $"[Дебетовый] {base.GetAccountInfo()}";
    }

  }
}