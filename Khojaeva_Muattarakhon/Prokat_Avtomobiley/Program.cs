using System;

class Car
{
    public int Id;
    public string Brand;
    public string Model;
    public int Year;
    public double Price;
    public bool IsRented;

    public Car(int id, string brand, string model, int year, double price)
    {
        Id = id;
        Brand = brand;
        Model = model;
        Year = year;
        Price = price;
        IsRented = false;
    }

    public void ShowInfo()
    {
        Console.WriteLine("ID: " + Id);
        Console.WriteLine("Марка: " + Brand);
        Console.WriteLine("Модель: " + Model);
        Console.WriteLine("Год: " + Year);
        Console.WriteLine("Цена за сутки: " + Price + " сомони");

        if (IsRented)
            Console.WriteLine("Статус: Арендован");
        else
            Console.WriteLine("Статус: Доступен");

        Console.WriteLine("--------------------------");
    }
}

class Rental
{
    public Car car;

    public Rental(Car car)
    {
        this.car = car;
    }

    public void RentCar(int days)
    {
        if (car.IsRented)
        {
            Console.WriteLine("Этот автомобиль уже арендован!");
        }
        else
        {
            car.IsRented = true;

            double total = car.Price * days;

            Console.WriteLine("Аренда оформлена успешно!");
            Console.WriteLine("Автомобиль: " + car.Brand + " " + car.Model);
            Console.WriteLine("Количество дней: " + days);
            Console.WriteLine("Общая стоимость: " + total + " сомони");
        }
    }

    public void ReturnCar()
    {
        if (car.IsRented)
        {
            car.IsRented = false;
            Console.WriteLine("Автомобиль успешно возвращён!");
        }
        else
        {
            Console.WriteLine("Автомобиль не находится в аренде.");
        }
    }
}

class Program
{
    static void Main()
    {
        Car car1 = new Car(1, "Toyota", "Camry", 2023, 500);
        Car car2 = new Car(2, "BMW", "X5", 2022, 1000);
        Car car3 = new Car(3, "Mercedes", "E-Class", 2023, 900);
        Car car4 = new Car(4, "Hyundai", "Sonata", 2022, 400);

        Car[] cars = { car1, car2, car3, car4 };

        while (true)
        {
            Console.WriteLine("\n===== ПРОКАТ АВТОМОБИЛЕЙ =====");
            Console.WriteLine("1. Список автомобилей");
            Console.WriteLine("2. Арендовать автомобиль");
            Console.WriteLine("3. Вернуть автомобиль");
            Console.WriteLine("0. Выход");
            Console.Write("Выберите действие: ");

            int choice = Convert.ToInt32(Console.ReadLine());

            if (choice == 1)
            {
                Console.WriteLine("\nСПИСОК АВТОМОБИЛЕЙ");

                foreach (Car car in cars)
                {
                    car.ShowInfo();
                }
            }
            else if (choice == 2)
            {
                Console.Write("Введите ID автомобиля: ");
                int id = Convert.ToInt32(Console.ReadLine());

                Console.Write("На сколько дней аренда: ");
                int days = Convert.ToInt32(Console.ReadLine());

                if (days <= 0)
                {
                    Console.WriteLine("Количество дней должно быть больше нуля!");
                    continue;
                }

                bool found = false;

                foreach (Car car in cars)
                {
                    if (car.Id == id)
                    {
                        Rental rental = new Rental(car);
                        rental.RentCar(days);
                        found = true;
                    }
                }

                if (!found)
                {
                    Console.WriteLine("Автомобиль не найден!");
                }
            }
            else if (choice == 3)
            {
                Console.Write("Введите ID автомобиля: ");
                int id = Convert.ToInt32(Console.ReadLine());

                bool found = false;

                foreach (Car car in cars)
                {
                    if (car.Id == id)
                    {
                        Rental rental = new Rental(car);
                        rental.ReturnCar();
                        found = true;
                    }
                }

                if (!found)
                {
                    Console.WriteLine("Автомобиль не найден!");
                }
            }
            else if (choice == 0)
            {
                Console.WriteLine("Спасибо за использование программы!");
                break;
            }
            else
            {
                Console.WriteLine("Неверный выбор!");
            }
        }
    }
}
