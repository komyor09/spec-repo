using System;
using BankSystem;

Console.WriteLine("=== ЗАПУСК БАНКОВСКОЙ СИСТЕМЫ ===\n");

Bank myBank = new Bank("ООО \"Курсовой Банк\"");
Console.WriteLine($"Добро пожаловать в {myBank.Name}!\n");

Client ivan = new Client("Иван Иванов", "1234 567890");
Client maria = new Client("Мария Петрова", "0987 654321");

myBank.RegisterClient(ivan);
myBank.RegisterClient(maria);
Console.WriteLine();

Account ivanAccount = new DebitAccount("ACC-001", ivan.Name, 5000m);
Account mariaAccount = new CreditAccount("ACC-002", maria.Name, 10000m, 0m); 

ivan.AddAccount(ivanAccount);
maria.AddAccount(mariaAccount);
Console.WriteLine();

Console.WriteLine("--- СОСТОЯНИЕ СЧЕТОВ ДО ОПЕРАЦИЙ ---");
Console.WriteLine(ivan.GetClientInfo());
Console.WriteLine(maria.GetClientInfo());

Console.WriteLine("\n=== НАЧИНАЕМ ОПЕРАЦИИ ===");

try 
{
    myBank.Transfer("ACC-001", "ACC-002", 2000m);
} 
catch (Exception ex) 
{
    Console.WriteLine($"❌ Ошибка: {ex.Message}");
}

try 
{
    myBank.Transfer("ACC-002", "ACC-001", 5000m);
} 
catch (Exception ex) 
{
    Console.WriteLine($"❌ Ошибка: {ex.Message}");
}

try 
{
    myBank.Transfer("ACC-001", "ACC-002", 10000m);
} 
catch (Exception ex) 
{
    Console.WriteLine($"❌ Ошибка: {ex.Message}");
}

Console.WriteLine("\n=== ИТОГОВОЕ СОСТОЯНИЕ ===");
Console.WriteLine(ivan.GetClientInfo());
Console.WriteLine(maria.GetClientInfo());

Console.WriteLine("\nНажмите любую клавишу для выхода...");
Console.ReadKey();