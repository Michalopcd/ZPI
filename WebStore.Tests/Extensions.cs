using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using WebStore.DAL.EF;
using WebStore.Model.DataModels;

namespace WebStore.Tests
{
    public static class Extensions
    {
        public static void SeedData(this IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

            //Suppliers
            var supplier1 = new Supplier()
            {
                Id = 1,
                FirstName = "Adam",
                LastName = "Bednarski",
                UserName = "supp1@eg.eg",
                Email = "supp1@eg.eg",
                RegistrationDate = new DateTime(2010, 1, 1),
            };
            CreateUser(userManager, supplier1, "User1234");

            //Categories
            var category1 = new Category()
            {
                Id = 1,
                Name = "Computers",
                Tag = "#computer"
            };
            dbContext.Add(category1);

            //Products
            var p1 = new Product()
            {
                Id = 1,
                CategoryId = category1.Id,
                SupplierId = supplier1.Id,
                Description = "Bardzo praktyczny monitor 32 cale",
                ImageBytes = new byte[] { 0xff, 0xff, 0xff, 0x80 },
                Name = "Monitor Dell 32",
                Price = 1000,
                Weight = 20,
            };
            dbContext.Add(p1);

            var p2 = new Product()
            {
                Id = 2,
                CategoryId = category1.Id,
                SupplierId = supplier1.Id,
                Description = "Precyzyjna mysz do pracy",
                ImageBytes = new byte[] { 0xff, 0xff, 0xff, 0x70 },
                Name = "Mysz Logitech",
                Price = 500,
                Weight = 0.5f,
            };
            dbContext.Add(p2);

            //Product stocks
            dbContext.Add(new ProductStock() { Id = 1, ProductId = p1.Id, Quantity = 10 });
            dbContext.Add(new ProductStock() { Id = 2, ProductId = p2.Id, Quantity = 50 });

            dbContext.SaveChanges();

            //Customers
            var customer1 = new Customer()
            {
                Id = 2,
                FirstName = "Jan",
                LastName = "Kowalski",
                UserName = "cust1@eg.eg",
                Email = "cust1@eg.eg",
                RegistrationDate = new DateTime(2015, 6, 1),
            };
            CreateUser(userManager, customer1, "User1234");

            //Customer addresses
            var billingAddress = new Address()
            {
                Id = 1,
                Street = "Akademicka",
                BuildingNumber = "16",
                PostalCode = "44-100",
                City = "Gliwice",
                Country = "Polska",
                CustomerId = customer1.Id
            };
            var shippingAddress = new Address()
            {
                Id = 2,
                Street = "Mariacka",
                BuildingNumber = "5",
                ApartmentNumber = "12",
                PostalCode = "40-014",
                City = "Katowice",
                Country = "Polska",
                CustomerId = customer1.Id
            };
            dbContext.AddRange(billingAddress, shippingAddress);

            //Stationary stores
            var store1 = new StationaryStore()
            {
                Id = 1,
                Name = "WebStore Gliwice"
            };
            dbContext.Add(store1);
            dbContext.Add(new Address()
            {
                Id = 3,
                Street = "Zwycięstwa",
                BuildingNumber = "1",
                PostalCode = "44-100",
                City = "Gliwice",
                Country = "Polska",
                StationaryStoreId = store1.Id
            });

            dbContext.SaveChanges();

            customer1.BillingAddressId = billingAddress.Id;
            customer1.ShippingAddressId = shippingAddress.Id;
            dbContext.SaveChanges();

            //Stationary store employees
            var employee1 = new StationaryStoreEmployee()
            {
                Id = 3,
                FirstName = "Anna",
                LastName = "Nowak",
                UserName = "emp1@eg.eg",
                Email = "emp1@eg.eg",
                RegistrationDate = new DateTime(2018, 3, 1),
                Position = "Sprzedawca",
                StationaryStoreId = store1.Id
            };
            CreateUser(userManager, employee1, "User1234");

            //Orders
            var order1 = new Order()
            {
                Id = 1,
                CustomerId = customer1.Id,
                OrderDate = new DateTime(2023, 1, 10),
                DeliveryDate = new DateTime(2023, 1, 15),
                TrackingNumber = 1000000001
            };
            dbContext.Add(order1);
            dbContext.Add(new OrderProduct() { OrderId = order1.Id, ProductId = p1.Id, Quantity = 1 });
            dbContext.Add(new OrderProduct() { OrderId = order1.Id, ProductId = p2.Id, Quantity = 2 });

            //Invoices
            dbContext.Add(new Invoice()
            {
                Id = 1,
                InvoiceNumber = "FV/2023/0001",
                IssueDate = new DateTime(2023, 1, 10),
                TotalAmount = 2000,
                Orders = new List<Order> { order1 }
            });

            // save changes
            dbContext.SaveChanges();
        }

        private static void CreateUser(UserManager<User> userManager, User user, string password)
        {
            var result = userManager.CreateAsync(user, password).GetAwaiter().GetResult();
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Nie udało się utworzyć użytkownika {user.UserName}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }
    }
}
