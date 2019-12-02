using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using Ddd.Infrastructure;

namespace Ddd.Taxi.Domain
{
    // In real aplication it whould be the place where database is used to find driver by its Id.
    // But in this exercise it is just a mock to simulate database
    public class DriversRepository
    {
        public Driver GetDriverById(int driverId)
        {
            if (driverId == 15)
            {
                var car = new Car("Baklazhan", "Lada sedan", "A123BT 66");
                return new Driver(driverId, new PersonName("Drive", "Driverson"), car);
            }
            else
                throw new Exception("Unknown driver id " + driverId);
        }
    }

    public class TaxiApi : ITaxiApi<TaxiOrder>
    {
        private readonly DriversRepository driversRepo;
        private readonly Func<DateTime> currentTime;

        public TaxiApi(DriversRepository driversRepo, Func<DateTime> currentTime)
        {
            this.driversRepo = driversRepo;
            this.currentTime = currentTime;
        }

        public TaxiOrder CreateOrderWithoutDestination(string firstName,
                                                       string lastName,
                                                       string street,
                                                       string building)
        {
            return TaxiOrder.CreateOrderWithoutDestination(new PersonName(firstName, lastName),
                                                           new Address(street, building), currentTime);
        }

        public void UpdateDestination(TaxiOrder order, string street, string building)
        {
            order.UpdateDestination(new Address(street, building));
        }

        public void AssignDriver(TaxiOrder order, int driverId)
        {
            order.AssignDriver(driversRepo.GetDriverById(driverId));
        }

        public void UnassignDriver(TaxiOrder order)
        {
            order.UnassignDriver();
        }

        public string GetDriverFullInfo(TaxiOrder order)
        {
            return order.GetDriverFullInfo();
        }

        public string GetShortOrderInfo(TaxiOrder order)
        {
            return order.GetShortOrderInfo();
        }

        public void Cancel(TaxiOrder order)
        {
            order.Cancel();
        }

        public void StartRide(TaxiOrder order)
        {
            order.StartRide();
        }

        public void FinishRide(TaxiOrder order)
        {
            order.FinishRide();
        }
    }

    public static class PersonNameExtensions
    {
        public static string ToString(this PersonName person, string format)
        {
            return format.Replace("F", person.FirstName).Replace("L", person.LastName);
        }
    }

    public static class AdressExtensions
    {
        public static string ToString(this Address adress, string format)
        {
            return format.Replace("S", adress.Street).Replace("B", adress.Building);
        }
    }

    public class TaxiOrder : Entity<int>
    {
        public PersonName ClientName { get; }

        public Address Start { get; }

        public Address Destination { get; private set; }

        public bool IsDriverAssign { get => Driver == null ? false : true; }

        public Driver Driver { get; private set; }

        private TaxiOrderStatus status;

        public TaxiOrderStatus Status
        {
            get
            {
                return status;
            }
            private set
            {
                status = value;
                LastProgressTime = time();
            }
        }

        public DateTime CreationTime { get; }

        public DateTime DriverAssignmentTime { get; private set; }

        public DateTime CancelTime { get; private set; }

        public DateTime StartRideTime { get; private set; }

        public DateTime FinishRideTime { get; private set; }

        public DateTime LastProgressTime { get; private set; }


        public TaxiOrder(int id,
                         DateTime creationTime,
                         TaxiOrderStatus status,
                         PersonName clientName,
                         Address startAdress) : base(id)
        {
            ClientName = clientName;
            Start = startAdress;
            CreationTime = creationTime;
            Status = status;
        }

        public static TaxiOrder CreateOrderWithoutDestination(PersonName client,
                                                             Address startAdress,
                                                             Func<DateTime> currentTime)
        {
            time = currentTime;//для прохождения тестов. Чтобы соподало время
            return new TaxiOrder(0,
                                 currentTime(),
                                 TaxiOrderStatus.WaitingForDriver,
                                 client,
                                 startAdress);
        }

        public void AssignDriver(Driver driver)
        {
            if (driver == null) throw new ArgumentNullException();
            if (IsDriverAssign)
                throw new InvalidOperationException("Driver already assigned");
            Driver = driver;
            DriverAssignmentTime = time();
            Status = TaxiOrderStatus.WaitingCarArrival;
        }

        public void UnassignDriver()
        {
            if (!IsDriverAssign)
                throw new InvalidOperationException("The order must be in WaitingForDriver status." +
                    "Have no assigned driver for unassign.");
            if (Status > TaxiOrderStatus.WaitingCarArrival)
                throw new InvalidOperationException($"Can not unassign driver. "
                                                    + $"Order status is {Status} status");
            Driver = null;
            Status = TaxiOrderStatus.WaitingForDriver;
        }

        public void UpdateDestination(Address destination)
        {
            if (destination == null) throw new ArgumentNullException();
            Destination = destination;
        }

        public void Cancel()
        {
            if (Status > TaxiOrderStatus.WaitingCarArrival)
                throw new InvalidOperationException($"Can not cancel order in {Status} status");
            if (IsDriverAssign)
                UnassignDriver();
            Status = TaxiOrderStatus.Canceled;
            CancelTime = time();
        }

        public void StartRide()
        {
            if (Status != TaxiOrderStatus.WaitingCarArrival)
                throw new InvalidOperationException("Can not start ride without assigned driver");
            Status = TaxiOrderStatus.InProgress;
            StartRideTime = time();
        }

        public void FinishRide()
        {
            if (Status != TaxiOrderStatus.InProgress)
                throw new InvalidOperationException("The ride must be in progress");
            Status = TaxiOrderStatus.Finished;
            FinishRideTime = time();
        }

        public string GetDriverFullInfo()
        {
            return Driver?.GetDriverFullInfo() ?? null;
        }

        public string GetShortOrderInfo()
        {
            return string.Join(" ",
              "OrderId: " + Id,
              "Status: " + Status,
              "Client: " + ClientName.ToString("F L"),
              "Driver: " + Driver?.Name.ToString("F L") ?? "not assigned",
              "From: " + Start.ToString("S B"),
              "To: " + Destination?.ToString("S B") ?? "unspecified",
              "LastProgressTime: " + GetLastProgressTime().ToString("yyyy-MM-dd HH:mm:ss",
                                                                    CultureInfo.InvariantCulture));
        }

        private DateTime GetLastProgressTime()
        {
            if (Status == TaxiOrderStatus.WaitingForDriver) return CreationTime;
            if (Status == TaxiOrderStatus.WaitingCarArrival) return DriverAssignmentTime;
            if (Status == TaxiOrderStatus.InProgress) return StartRideTime;
            if (Status == TaxiOrderStatus.Finished) return FinishRideTime;
            if (Status == TaxiOrderStatus.Canceled) return CancelTime;
            throw new NotSupportedException(Status.ToString());
        }

        private static int idCounter;
        private static Func<DateTime> time;
        private static int GenerateUniqueId()
        {
            return idCounter++;
        }
    }

    public class Driver : Entity<int>
    {
        public Driver(int id, PersonName name, Car car) : base(id)
        {
            Name = name;
            Car = car;
        }

        public PersonName Name { get; }

        public Car Car { get; }

        public string GetDriverFullInfo()
        {
            return $"{nameof(Id)}: {Id} DriverName: {Name.ToString("F L")} {Car.ToString()}";
        }
    }

    public class Car : ValueType<Car>
    {
        public Car(string color, string model, string plateNumber)
        {
            Color = color;
            CarModel = model;
            PlateNumber = plateNumber;
        }

        public string Color { get; }

        public string CarModel { get; }

        public string PlateNumber { get; }

        public override string ToString()
        {
            return $"{nameof(Color)}: {Color}" +
                   $" {nameof(CarModel)}: {CarModel}" +
                   $" {nameof(PlateNumber)}: {PlateNumber}";
        }
    }
}