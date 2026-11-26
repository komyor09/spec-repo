using System;
using System.Collections.Generic;
using System.Linq;

namespace AirTicketSales
{
    // Абстрактный класс
    abstract class Person
    {
        public string FullName { get; set; }

        public Person(string fullName)
        {
            FullName = fullName;
        }

        public abstract void ShowInfo();
    }

    // Класс пассажира
    class Passenger : Person
    {
        public string Passport { get; set; }

        public Passenger(string fullName, string passport)
            : base(fullName)
        {
            Passport = passport;
        }

        public override void ShowInfo()
        {
            Console.WriteLine("Пассажир: " + FullName);
            Console.WriteLine("Паспорт: " + Passport);
        }
    }

    // Класс авиарейса
    class Flight
    {
        public int Id { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Date { get; set; }
        public decimal Price { get; set; }
        public int Seats { get; set; }

        public Flight(int id, string from, string to,
                      string date, decimal price, int seats)
        {
            Id = id;
            From = from;
            To = to;
            Date = date;
            Price = price;
            Seats = seats;
        }

        public void ShowFlight()
        {
            Console.WriteLine(
                $"{Id}. {From} -> {To} | {Date} | " +
                $"{Price} сомони | Мест: {Seats}");
        }
    }

    // Класс авиабилета
    class Ticket
    {
        public int TicketNumber { get; set; }
        public Passenger Passenger { get; set; }
        public Flight Flight { get; set; }

        public Ticket(int number, Passenger passenger, Flight flight)
        {
            TicketNumber = number;
            Passenger = passenger;
            Flight = flight;
        }

        public void ShowTicket()
        {
            Console.WriteLine("\n========== АВИАБИЛЕТ ==========");
            Console.WriteLine("Номер билета: " + TicketNumber);
            Passenger.ShowInfo();
            Console.WriteLine("Маршрут: " +
                Flight.From + " - " + Flight.To);
            Console.WriteLine("Дата: " + Flight.Date);
            Console.WriteLine("Стоимость: " +
                Flight.Price + " сомони");
            Console.WriteLine("===============================");
        }
    }

    // Главный класс программы
    class AirlineOffice
    {
        private List<Flight> flights = new List<Flight>();
        private List<Ticket> tickets = new List<Ticket>();

        private int nextTicketNumber = 1001;

        public AirlineOffice()
        {
            flights.Add(new Flight(1, "Худжанд", "Душанбе",
                "20.11.2026", 350, 20));

            flights.Add(new Flight(2, "Душанбе", "Москва",
                "21.11.2026", 2500, 15));

            flights.Add(new Flight(3, "Худжанд", "Москва",
                "22.11.2026", 2800, 10));

            flights.Add(new Flight(4, "Душанбе", "Стамбул",
                "25.11.2026", 3200, 12));

            flights.Add(new Flight(5, "Худжанд", "Алматы",
                "27.11.2026", 1800, 8));
        }

        public void ShowFlights()
        {
            Console.WriteLine("\n====== ДОСТУПНЫЕ РЕЙСЫ ======");

            foreach (Flight flight in flights)
            {
                flight.ShowFlight();
            }
        }

        public void SearchFlight()
        {
            Console.Write("\nГород отправления: ");
            string from = Console.ReadLine() ?? "";

            Console.Write("Город прибытия: ");
            string to = Console.ReadLine() ?? "";

            var result = flights.Where(f =>
                f.From.Equals(from, StringComparison.OrdinalIgnoreCase) &&
                f.To.Equals(to, StringComparison.OrdinalIgnoreCase));

            Console.WriteLine("\nРезультаты поиска:");

            if (!result.Any())
            {
                Console.WriteLine("Рейсы не найдены.");
                return;
            }

            foreach (Flight flight in result)
            {
                flight.ShowFlight();
            }
        }

        public void BuyTicket()
        {
            ShowFlights();

            Console.Write("\nВведите номер рейса: ");

            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("Некорректный номер.");
                return;
            }

            Flight flight = flights.FirstOrDefault(f => f.Id == id);

            if (flight == null || flight.Seats <= 0)
            {
                Console.WriteLine("Рейс не найден или мест нет.");
                return;
            }

            Console.Write("Введите ФИО пассажира: ");
            string name = Console.ReadLine() ?? "";

            Console.Write("Введите номер паспорта: ");
            string passport = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(passport))
            {
                Console.WriteLine("Заполните все данные.");
                return;
            }

            Passenger passenger = new Passenger(name, passport);

            Ticket ticket = new Ticket(
                nextTicketNumber++, passenger, flight);

            tickets.Add(ticket);
            flight.Seats--;

            Console.WriteLine("\nБилет успешно приобретён!");
            ticket.ShowTicket();
        }

        public void ShowTickets()
        {
            Console.WriteLine("\n====== КУПЛЕННЫЕ БИЛЕТЫ ======");

            if (tickets.Count == 0)
            {
                Console.WriteLine("Покупок пока нет.");
                return;
            }

            foreach (Ticket ticket in tickets)
            {
                ticket.ShowTicket();
            }
        }

        public void CancelTicket()
        {
            Console.Write("\nВведите номер билета для отмены: ");

            if (!int.TryParse(Console.ReadLine(), out int number))
            {
                Console.WriteLine("Некорректный номер.");
                return;
            }

            Ticket ticket = tickets.FirstOrDefault(
                t => t.TicketNumber == number);

            if (ticket == null)
            {
                Console.WriteLine("Билет не найден.");
                return;
            }

            ticket.Flight.Seats++;
            tickets.Remove(ticket);

            Console.WriteLine("Покупка успешно отменена.");
        }

        public void Start()
        {
            while (true)
            {
                Console.WriteLine("\n");
                Console.WriteLine("================================");
                Console.WriteLine("      ПРОДАЖА АВИАБИЛЕТОВ");
                Console.WriteLine("================================");
                Console.WriteLine("1. Просмотр авиарейсов");
                Console.WriteLine("2. Поиск авиарейса");
                Console.WriteLine("3. Купить авиабилет");
                Console.WriteLine("4. Мои билеты");
                Console.WriteLine("5. Отменить покупку");
                Console.WriteLine("0. Выход");
                Console.WriteLine("================================");

                Console.Write("Выберите действие: ");
                string choice = Console.ReadLine() ?? "";

                switch (choice)
                {
                    case "1":
                        ShowFlights();
                        break;

                    case "2":
                        SearchFlight();
                        break;

                    case "3":
                        BuyTicket();
                        break;

                    case "4":
                        ShowTickets();
                        break;

                    case "5":
                        CancelTicket();
                        break;

                    case "0":
                        Console.WriteLine("Программа завершена.");
                        return;

                    default:
                        Console.WriteLine("Неверный выбор!");
                        break;
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding =
                System.Text.Encoding.UTF8;

            AirlineOffice office = new AirlineOffice();
            office.Start();
        }
    }
}