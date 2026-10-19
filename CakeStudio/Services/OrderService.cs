
using CakeStudio.Data;
using CakeStudio.Models;
using CakeStudio.ViewModels;
using NHibernate.Linq;

namespace CakeStudio.Services
{
    public class OrderService : IOrderService
    {
        private readonly NHibernateHelper _nhibernateHelper;

        public OrderService(NHibernateHelper nhibernateHelper)
        {
            _nhibernateHelper = nhibernateHelper;
        }

        public List<Order> GetAll()
        {
            using var session = _nhibernateHelper.OpenSession();

            return session.Query<Order>()
                .Fetch(x => x.Customer)
                .Fetch(x => x.Cake)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();
        }

        public Order? GetById(int id)
        {
            using var session = _nhibernateHelper.OpenSession();

            return session.Get<Order>(id);
        }




        public OrderDetailsViewModel? GetOrderDetails(int id)
        {
            using var session = _nhibernateHelper.OpenSession();

            Order orderAlias = null!;
            Cake cakeAlias = null!;
            CakeSize sizeAlias = null!;
            CakeFlavour flavourAlias = null!;
            CakeTier tierAlias = null!;

            var order = session.QueryOver<Order>(() => orderAlias)

                .Left.JoinAlias(
                    () => orderAlias.Cake,
                    () => cakeAlias)

                .Left.JoinAlias(
                    () => orderAlias.Size,
                    () => sizeAlias)

                .Left.JoinAlias(
                    () => orderAlias.Flavour,
                    () => flavourAlias)

                .Left.JoinAlias(
                    () => orderAlias.Tier,
                    () => tierAlias)

                .Where(() => orderAlias.OrderId == id)

                .SingleOrDefault();

            if (order == null)
                return null;

            return new OrderDetailsViewModel
            {
                Order = order,
                CakeName = order.Cake?.CakeName ?? "Unknown Cake",
                FlavourName = order.Flavour?.FlavourName ?? "-",
                SizeName = order.Size?.SizeName ?? "-",
                TierName = order.Tier?.TierName ?? "-"
            };
        }

        public List<Order> GetOrdersByCustomer(int customerId)
        {
            using var session = _nhibernateHelper.OpenSession();

            return session.Query<Order>()
                .Where(x => x.CustId == customerId)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();
        }

        public int Create(int customerId, OrderViewModel model)
        {
            if (!model.CakeId.HasValue)
                throw new ArgumentException("Please select a cake.");

            if (!model.SizeId.HasValue ||
                !model.FlavourId.HasValue ||
                !model.TierId.HasValue)
            {
                throw new ArgumentException(
                    "Please select cake size, flavour and tier.");
            }


            if (!model.RequiredDate.HasValue ||
                !model.RequiredTime.HasValue)
            {
                throw new ArgumentException(
                    "Please select a required date and time.");
            }

            DateTime requiredDateTime = model.RequiredDate.Value.Date
                .Add(model.RequiredTime.Value);

            if (requiredDateTime <= DateTime.Now)
            {
                throw new ArgumentException(
                    "Pickup or delivery date and time must be in the future.");
            }



            string method = model.CollectionMethod?.Trim() ?? "";


            if (method != "Pickup" && method != "Delivery")
                throw new ArgumentException(
                    "Invalid collection method.");

            if (method == "Delivery" &&
                string.IsNullOrWhiteSpace(model.DeliveryAddress))
            {
                throw new ArgumentException(
                    "Delivery address is required.");
            }

            using var session = _nhibernateHelper.OpenSession();
            using var transaction = session.BeginTransaction();

            try
            {
                // Validate customer
                var customer = session.Get<User>(customerId);

                if (customer == null ||
                    customer.GetRoleName() != "Customer")
                {
                    throw new ArgumentException(
                        "Invalid customer account.");
                }

                // Validate selected cake
                var cake = session.Get<Cake>(model.CakeId.Value);

                if (cake == null || !cake.IsAvailable)
                    throw new ArgumentException(
                        "Selected cake is unavailable.");

                decimal totalPrice = cake.BasePrice;

                // Validate and calculate size
                if (model.SizeId.HasValue)
                {
                    var size = session.Get<CakeSize>(
                        model.SizeId.Value);

                    if (size == null || !size.IsAvailable)
                        throw new ArgumentException(
                            "Selected size is unavailable.");


                    bool isDefaultSize = size.SizeName.Trim()
                        .Equals("6 inch", StringComparison.OrdinalIgnoreCase);

                    if (!isDefaultSize)
                    {
                        totalPrice += size.AdditionalPrice;
                    }

                }

                // Validate and calculate flavour
                if (model.FlavourId.HasValue)
                {
                    var flavour = session.Get<CakeFlavour>(
                        model.FlavourId.Value);

                    if (flavour == null || !flavour.IsAvailable)
                        throw new ArgumentException(
                            "Selected flavour is unavailable.");


                    bool isDefaultFlavour =
                        flavour.FlavourId == cake.Flavour.FlavourId;

                    if (!isDefaultFlavour)
                    {
                        totalPrice += flavour.AdditionalPrice;
                    }

                }

                // Validate and calculate tier
                if (model.TierId.HasValue)
                {
                    var tier = session.Get<CakeTier>(
                        model.TierId.Value);

                    if (tier == null || !tier.IsAvailable)
                        throw new ArgumentException(
                            "Selected tier is unavailable.");


                    bool isDefaultTier = tier.TierName.Trim()
                        .Equals("1", StringComparison.OrdinalIgnoreCase);

                    if (!isDefaultTier)
                    {
                        totalPrice += tier.AdditionalPrice;
                    }

                }

                if (totalPrice <= 0 || totalPrice > 100000)
                    throw new ArgumentException(
                        "Calculated order price is invalid.");

                // Create Order entity
                var order = new Order
                {
                    CustId = customerId,
                    CakeId = model.CakeId,
                    FlavourId = model.FlavourId,
                    SizeId = model.SizeId,
                    TierId = model.TierId,


                    RequiredDate = requiredDateTime,

                    CollectionMethod = method,

                    DeliveryAddress = method == "Delivery"
                        ? model.DeliveryAddress?.Trim()
                        : null,

                    MessageOnCakes =
                        model.MessageOnCakes?.Trim(),

                    TotalPrice = totalPrice,
                    OrderStatus = "Pending",
                    CreatedAt = DateTime.Now,
                    CreatedId = customerId
                };

                session.Save(order);
                transaction.Commit();

                return order.OrderId;
            }
            catch
            {
                if (transaction.IsActive)
                    transaction.Rollback();

                throw;
            }
        }

        public void UpdateStatus(
            int orderId,
            string status,
            int bakerId)
        {
            string[] allowedStatuses =
            {
                "Pending",
                "Confirmed",
                "Preparing",
                "Ready",
                "Completed",
                "Cancelled"
            };

            if (!allowedStatuses.Contains(status))
                throw new ArgumentException(
                    "Invalid order status.");

            using var session = _nhibernateHelper.OpenSession();
            using var transaction = session.BeginTransaction();

            try
            {
                var baker = session.Get<User>(bakerId);

                if (baker == null ||
                    baker.GetRoleName() != "Baker")
                {
                    throw new UnauthorizedAccessException(
                        "Invalid baker account.");
                }

                var order = session.Get<Order>(orderId);

                if (order == null)
                    throw new ArgumentException(
                        "Order not found.");

                if (order.OrderStatus == "Completed" ||
                    order.OrderStatus == "Cancelled")
                {
                    throw new InvalidOperationException(
                        "This order can no longer be updated.");
                }

                order.OrderStatus = status;
                order.BakerId = bakerId;
                order.UpdatedAt = DateTime.Now;
                order.UpdatedId = bakerId;

                session.Update(order);
                transaction.Commit();
            }
            catch
            {
                if (transaction.IsActive)
                    transaction.Rollback();

                throw;
            }
        }
    }
}
