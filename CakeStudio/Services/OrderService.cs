using CakeStudio.Data;
using CakeStudio.Models;
using CakeStudio.Resources;
using CakeStudio.ViewModels;
using CakeStudio.Constant;
using NHibernate;

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

            var orders = session.QueryOver<Order>()
            .Fetch(SelectMode.Fetch, o => o.Customer)
            .OrderBy(o => o.CreatedAt).Desc
            .List();

            // Load each order's cake items before closing the session
            foreach (var order in orders)
            {
                var items = session.QueryOver<OrderItem>()
                    .Where(item => item.OrderId == order.OrderId)
                    .Fetch(SelectMode.Fetch, item => item.Cake)
                    .OrderBy(item => item.OrderItemId).Asc
                    .List();

                order.OrderItems = new List<OrderItem>(items);
            }
            return new List<Order>(orders);
        }
        public List<Payment> GetAllPayments()
        {
            using var session = _nhibernateHelper.OpenSession();

            var payment = session.QueryOver<Payment>()
                .Fetch(SelectMode.Fetch, p => p.Order)
                .OrderBy(p => p.PaidAt).Desc
                .List();
            return new List<Payment>(payment);
        }
        public Order? GetById(int id)
        {
            using var session = _nhibernateHelper.OpenSession();

            return session.Get<Order>(id);
        }

        public OrderListViewModel GetPagedOrders(int page, int pageSize, string sort = "")
        {
            using var session = _nhibernateHelper.OpenSession();

            // Summary cards count all orders
            var model = new OrderListViewModel
            {
                TotalOrders = session.QueryOver<Order>()
                    .RowCount(),

                PreparingOrders = session.QueryOver<Order>()
                    .Where(o => o.OrderStatus == OrderStatus.Preparing)
                    .RowCount(),

                ReadyOrders = session.QueryOver<Order>()
                    .Where(o => o.OrderStatus == OrderStatus.Ready)
                    .RowCount(),

                CompletedOrders = session.QueryOver<Order>()
                    .Where(o => o.OrderStatus == OrderStatus.Completed)
                    .RowCount()
            };

            // Filter and sort the orders
            var query = session.QueryOver<Order>();

            switch (sort)
            {
                case "pending":
                    query.Where(o => o.OrderStatus == OrderStatus.Pending);
                    break;

                case "preparing":
                    query.Where(o => o.OrderStatus == OrderStatus.Preparing);
                    break;

                case "ready":
                    query.Where(o => o.OrderStatus == OrderStatus.Ready);
                    break;

                case "completed":
                    query.Where(o => o.OrderStatus == OrderStatus.Completed);
                    break;

                case "rejected":
                    query.Where(o => o.OrderStatus == OrderStatus.Rejected);
                    break;

                case "nearest":
                    query.Where(o => o.OrderStatus == OrderStatus.Pending || o.OrderStatus == OrderStatus.Preparing);
                    break;
            }

            //sort match order
            if (sort == "nearest")
            {
                query = query
                    .OrderBy(o => o.RequiredDate).Asc
                    .ThenBy(o => o.OrderId).Asc;
            }
            else
            {
                query = query
                    .OrderBy(o => o.RequiredDate).Desc
                    .ThenBy(o => o.OrderId).Asc;
            }

            //Calculate how many pages
            model.FilteredOrderCount = query.Clone().RowCount();
            model.TotalPages = Math.Max(1, (int)Math.Ceiling(model.FilteredOrderCount / (double)pageSize));

            model.CurrentPage = Math.Clamp(page, 1, model.TotalPages);
            model.PageSize = pageSize;
            model.Sort = sort;


            //Retrieve only the orders for the requested
            var orders = query
                .Fetch(SelectMode.Fetch, o => o.Customer)
                 
                .Skip((model.CurrentPage - 1) * pageSize) //nak gi page 2 so ignore the first 5 order on page 1
                .Take(pageSize) // get 6-10 order 
                .List();

            //Load each order’s cake items so the view can display names and quantities
            foreach (var order in orders)
            {
                var items = session.QueryOver<OrderItem>()
                    .Where(item => item.OrderId == order.OrderId)
                    .Fetch(SelectMode.Fetch, item => item.Cake)
                    .OrderBy(item => item.OrderItemId).Asc
                    .List();

                order.OrderItems = new List<OrderItem>(items);
            }

            model.Orders = new List<Order>(orders);

            return model;
        }

        public OrderListViewModel GetPagedOrdersByCustomer(int customerId, int page, int pageSize)
        {
            using var session = _nhibernateHelper.OpenSession();

            var query = session.QueryOver<Order>()
                .Where(o => o.CustId == customerId);

            int totalOrders = query.Clone().RowCount();

            int totalPages = Math.Max(1,(int)Math.Ceiling(totalOrders / (double)pageSize));

            int currentPage = Math.Clamp(page, 1, totalPages);

            var orders = query
                 
                .OrderBy(o => o.CreatedAt).Desc
                .ThenBy(o => o.OrderId).Asc
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .List();

            // Load each order's cakes before closing the session.
            foreach (var order in orders)
            {
                var items = session.QueryOver<OrderItem>()
                    .Where(item => item.OrderId == order.OrderId)
                    .Fetch(SelectMode.Fetch, item => item.Cake)
                    .OrderBy(item => item.OrderItemId).Asc
                    .List();

                order.OrderItems = new List<OrderItem>(items);
            }

            return new OrderListViewModel
            {
                Orders = new List<Order>(orders),
                CurrentPage = currentPage,
                PageSize = pageSize,
                TotalPages = totalPages,
                FilteredOrderCount = totalOrders,
                TotalOrders = totalOrders
            };
        }
        public decimal CalculateTotalPrice(OrderViewModel model)
        {
            if (!model.CakeId.HasValue || !model.SizeId.HasValue || !model.FlavourId.HasValue || !model.TierId.HasValue)
            {
                throw new ArgumentException(Resource.CakeOptionsRequired);
            }

            using var session = _nhibernateHelper.OpenSession();

            var cake = session.Get<Cake>(model.CakeId.Value);
            var size = session.Get<CakeSize>(model.SizeId.Value);
            var flavour = session.Get<CakeFlavour>(model.FlavourId.Value);
            var tier = session.Get<CakeTier>(model.TierId.Value);

            if (cake == null || !cake.IsAvailable)
                throw new ArgumentException(Resource.CakeUnavailable);

            if (size == null || !size.IsAvailable)
                throw new ArgumentException(Resource.SizeUnavailable);

            if (flavour == null || !flavour.IsAvailable)
                throw new ArgumentException(Resource.FlavourUnavailable);

            if (tier == null || !tier.IsAvailable)
                throw new ArgumentException(Resource.TierUnavailable);

            return CalculateUnitPrice(cake, size, flavour, tier);
         
        }

        private static decimal CalculateUnitPrice(Cake cake,CakeSize size,CakeFlavour flavour,CakeTier tier)
        {
            decimal price = cake.BasePrice;

            if (!size.SizeName.Trim().Equals(
                "6 inch", StringComparison.OrdinalIgnoreCase))
            {
                price += size.AdditionalPrice;
            }

            if (flavour.FlavourId != cake.Flavour.FlavourId)
            {
                price += flavour.AdditionalPrice;
            }

            if (!tier.TierName.Trim().Equals(
                "1", StringComparison.OrdinalIgnoreCase))
            {
                price += tier.AdditionalPrice;
            }

            if (price <= 0 || price > 100000)
            {
                throw new ArgumentException(Resource.InvalidOrderPrice);
            }

            return price;
        }

        public int Create(int customerId, OrderViewModel model, string paymentMethod)
        {
            if (model.Cart == null || !model.Cart.Items.Any())
            {
                throw new ArgumentException(Resource.CartEmpty);
            }


            if (!model.RequiredDate.HasValue || !model.RequiredTime.HasValue)
            {
                throw new ArgumentException(Resource.InvalidRequiredDateTime);
            }

            DateTime requiredDateTime = model.RequiredDate.Value.Date
                .Add(model.RequiredTime.Value);

            if (requiredDateTime <= DateTime.Now)
            {
                throw new ArgumentException(Resource.InvalidRequiredDateTime);
            }



            string method = model.CollectionMethod?.Trim() ?? "";


            if (method != "Pickup" && method != "Delivery")
                throw new ArgumentException(Resource.InvalidCollectionMethod);

            if (method == "Delivery" && string.IsNullOrWhiteSpace(model.DeliveryAddress))
            {
                throw new ArgumentException(Resource.DeliveryAddressRequired);
            }

            using var session = _nhibernateHelper.OpenSession();
            using var transaction = session.BeginTransaction(System.Data.IsolationLevel.Serializable);

            try
            {
                // Validate customer
                var customer = session.Get<User>(customerId);

                if (customer == null || customer.GetRoleName() != "Customer")
                {
                    throw new ArgumentException(Resource.InvalidCustomerAccount);
                }

                // >>cart 
                decimal totalPrice = 0m;
                var orderItems = new List<OrderItem>();

                foreach (var item in model.Cart.Items)
                {
                    if (item.Quantity < 1 || item.Quantity > 20)
                        throw new ArgumentException(Resource.InvalidCakeQuantity);

                    if (item.MessageOnCakes?.Length > 500)
                        throw new ArgumentException(Resource.CakeMessageTooLong);

                    // Load this item's cake and options
                    var cake = session.Get<Cake>(item.CakeId);
                    var size = session.Get<CakeSize>(item.SizeId);
                    var flavour = session.Get<CakeFlavour>(item.FlavourId);
                    var tier = session.Get<CakeTier>(item.TierId);

                    if (cake == null || !cake.IsAvailable)
                        throw new ArgumentException(Resource.CakeUnavailable);

                    if (size == null || !size.IsAvailable)
                        throw new ArgumentException(Resource.SizeUnavailable);

                    if (flavour == null || !flavour.IsAvailable)
                        throw new ArgumentException(Resource.FlavourUnavailable);

                    if (tier == null || !tier.IsAvailable)
                        throw new ArgumentException(Resource.TierUnavailable);

                    decimal unitPrice = CalculateUnitPrice(cake, size, flavour, tier);

                    // Require another review if the price has changed
                    if (unitPrice != item.UnitPrice)
                        throw new ArgumentException(Resource.CakePriceChanged);

                    totalPrice += unitPrice * item.Quantity;

                    orderItems.Add(new OrderItem
                    {
                        CakeId = item.CakeId,
                        SizeId = item.SizeId,
                        FlavourId = item.FlavourId,
                        TierId = item.TierId,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice,
                        MessageOnCakes = item.MessageOnCakes?.Trim()
                    });
                }

                decimal deliveryFee = method == "Delivery"
                    ? DeliveryRate.FixedFee
                    : 0m;

                totalPrice += deliveryFee;

                if (totalPrice <= 0 || totalPrice > 100000)
                    throw new ArgumentException(Resource.InvalidOrderPrice);

                if (paymentMethod != PaymentMethod.Card)
                    throw new ArgumentException(Resource.InvalidPaymentMethod);

                // Retrieve the Baker account
                var baker = session.QueryOver<Baker>()
                    .OrderBy(b => b.UserId).Asc
                    .Take(1)
                    .SingleOrDefault();

                if (baker == null)
                {
                    throw new InvalidOperationException(Resource.BakerAccountNotFound);
                }

                // ni nak cek limit baker 

                DateTime startDate = requiredDateTime.Date;
                DateTime endDate = startDate.AddDays(1);

                Order existingOrder = null!;

                int bookedQuantity = session.QueryOver<OrderItem>()
                    .JoinAlias(item => item.Order, () => existingOrder)
                    .Where(() => existingOrder.RequiredDate >= startDate)
                    .And(() => existingOrder.RequiredDate < endDate)
                    .And(() => existingOrder.OrderStatus != OrderStatus.Rejected)
                    .SelectList(list => list.SelectSum(item => item.Quantity))
                    .SingleOrDefault<int?>() ?? 0;

                int requestedQuantity = orderItems.Sum(item => item.Quantity);

                // Calculate remaining capacity, with a minimum of zero.
                int remaining = Math.Max(0, BakingCapacity.DailyLimit - bookedQuantity);

                // Stop the order if its quantity exceeds the remaining capacity.
                if (requestedQuantity > remaining)
                {
                    string message = remaining == 0
                        ? Resource.DateFullyBooked
                        : $"Only {remaining} more {(remaining == 1 ? "cake" : "cakes")} can be booked for this date.";

                    throw new ArgumentException(message);
                }

                // Create Order entity
                var order = new Order
                {
                    CustId = customerId,
                    BakerId = baker.UserId,

                    RequiredDate = requiredDateTime,
                    CollectionMethod = method,

                    DeliveryAddress = method == "Delivery"
                        ? model.DeliveryAddress?.Trim()
                        : null,
                    DeliveryFee = deliveryFee,
                    TotalPrice = totalPrice,
                    OrderStatus = OrderStatus.Pending,
                    CreatedAt = DateTime.Now,
                    CreatedId = customerId
                };

                session.Save(order);

                // Save each cake item under the new order
                foreach (var item in orderItems)
                {
                    item.OrderId = order.OrderId;

                    session.Save(item);
                }

                var payment = new Payment
                {
                    Order = order,
                    Amount = totalPrice,
                    PaymentMethod = PaymentMethod.Card,
                    PaymentStatus = PaymentStatus.Paid,
                    PaidAt = DateTime.Now
                };

                session.Save(payment);

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

        public int GetBookedCakeQuantity(DateTime requiredDate,int? excludeOrderId = null)
        {
            using var session = _nhibernateHelper.OpenSession();

            DateTime startDate = requiredDate.Date;
            DateTime endDate = startDate.AddDays(1);

            Order order = null!;

            var query = session.QueryOver<OrderItem>()
                .JoinAlias(item => item.Order, () => order)
                .Where(() => order.RequiredDate >= startDate)
                .And(() => order.RequiredDate < endDate)
                .And(() => order.OrderStatus != OrderStatus.Rejected);

            if (excludeOrderId.HasValue)
            {
                query.Where(() => order.OrderId != excludeOrderId.Value);
            }

            return query
                .SelectList(list => list.SelectSum(item => item.Quantity))
                .SingleOrDefault<int?>() ?? 0;
        }

        public OrderDetailsViewModel? GetOrderDetails(int id)
        {
            using var session = _nhibernateHelper.OpenSession();

            Order o = null!;
            User u = null!;

            OrderItem oi = null!;
            Cake c = null!;
            CakeSize s = null!;
            CakeFlavour f = null!;
            CakeTier t = null!;

            // Load the order and customer.
            var order = session.QueryOver<Order>(() => o)
                // LEFT JOIN [User] u ON o.CustId = u.UserId
                .Left.JoinAlias(
                    () => o.Customer,
                    () => u)

                .Fetch(SelectMode.Fetch, x => x.Customer)

                .Where(() => o.OrderId == id)
                .SingleOrDefault();

            if (order == null)
                return null;

            // Load each order item and its cake/options.
            var orderItems = session.QueryOver<OrderItem>(() => oi)
                // LEFT JOIN Cake c ON oi.CakeId = c.CakeId
                .Left.JoinAlias(
                    () => oi.Cake,
                    () => c)

                .Left.JoinAlias(
                    () => oi.Size,
                    () => s)

                .Left.JoinAlias(
                    () => oi.Flavour,
                    () => f)

                .Left.JoinAlias(
                    () => oi.Tier,
                    () => t)

                .Fetch(SelectMode.Fetch, x => x.Cake)
                .Fetch(SelectMode.Fetch, x => x.Size)
                .Fetch(SelectMode.Fetch, x => x.Flavour)
                .Fetch(SelectMode.Fetch, x => x.Tier)

                .Where(() => oi.OrderId == id)
                .OrderBy(() => oi.OrderItemId).Asc
                .List();

            var payment = session.QueryOver<Payment>()
                .Where(p => p.Order.OrderId == id)
                .SingleOrDefault();

            return new OrderDetailsViewModel
            {
                Order = order,
                OrderItems = new List<OrderItem>(orderItems),
                CustomerName = order.Customer?.Name ?? "-",
                CustomerEmail = order.Customer?.Email ?? "-",
                CustomerPhone = order.Customer?.PhoneNumber,
                CustomerAddress = order.Customer?.Address,
                Payment = payment
            };
        }
        public void UpdateCustomerOrder(int orderId, int customerId, OrderViewModel model)
        {
            using var session = _nhibernateHelper.OpenSession();
            using var transaction = session.BeginTransaction(System.Data.IsolationLevel.Serializable);

            // Lock the order while checking and updating it
            var order = session.Get<Order>(orderId, LockMode.Upgrade);

            if (order == null)
                throw new ArgumentException(Resource.OrderNotFound);

            // Customer can only edit their own order
            if (order.CustId != customerId)
                throw new UnauthorizedAccessException(Resource.OrderEditForbidden);

            // Editing is allowed only before preparation starts
            if (order.OrderStatus != OrderStatus.Pending)
                throw new InvalidOperationException(Resource.OrderEditNotAllowed);

            if (!model.RequiredDate.HasValue || !model.RequiredTime.HasValue)
            {
                throw new ArgumentException(Resource.InvalidRequiredDateTime);
            }

            if (model.RequiredTime.Value < TimeSpan.Zero ||model.RequiredTime.Value >= TimeSpan.FromDays(1))
            {
                throw new ArgumentException(Resource.InvalidRequiredDateTime);
            }

            DateTime requiredDateTime = model.RequiredDate.Value.Date
                .Add(model.RequiredTime.Value);

            if (requiredDateTime <= DateTime.Now)
                throw new ArgumentException(Resource.InvalidRequiredDateTime);

            string method = model.CollectionMethod?.Trim() ?? "";

            if (method != "Pickup" && method != "Delivery")
                throw new ArgumentException(Resource.InvalidCollectionMethod);

            if (method == "Delivery" && string.IsNullOrWhiteSpace(model.DeliveryAddress))
            {
                throw new ArgumentException(Resource.DeliveryAddressRequired);
            }

            if (model.DeliveryAddress?.Length > 500 )
            {
                throw new ArgumentException(Resource.DeliveryAddressTooLong);
            }

            if (method != order.CollectionMethod)
            {
                throw new InvalidOperationException(Resource.CollectionMethodChangeNotAllowed);
            }

            // Load the order's saved cake quantities.
            var savedItems = session.QueryOver<OrderItem>()
                .Where(item => item.OrderId == orderId)
                .List();

            // Check capacity only when moving to another day.
            if (order.RequiredDate?.Date != requiredDateTime.Date)
            {
                DateTime startDate = requiredDateTime.Date;
                DateTime endDate = startDate.AddDays(1);

                Order existingOrder = null!;

                // Count other orders on the new date.
                int bookedQuantity = session.QueryOver<OrderItem>()
                    .JoinAlias(item => item.Order, () => existingOrder)
                    .Where(() => existingOrder.RequiredDate >= startDate)
                    .And(() => existingOrder.RequiredDate < endDate)
                    .And(() => existingOrder.OrderStatus != OrderStatus.Rejected)
                    .And(() => existingOrder.OrderId != orderId)
                    .SelectList(list => list.SelectSum(item => item.Quantity))
                    .SingleOrDefault<int?>() ?? 0;

                int orderQuantity = savedItems.Sum(item => item.Quantity);

                int remaining = Math.Max(
                    0, BakingCapacity.DailyLimit - bookedQuantity);

                if (orderQuantity > remaining)
                {
                    string message = remaining == 0
                        ? Resource.DateFullyBooked
                        : $"Only {remaining} more {(remaining == 1 ? "cake" : "cakes")} can be booked for this date.";

                    throw new ArgumentException(message);
                }
            }

            // Update only the editable order information
            order.RequiredDate = requiredDateTime;
            order.CollectionMethod = method;
            order.DeliveryAddress = method == "Delivery"
                ? model.DeliveryAddress?.Trim()
                : null;
            order.UpdatedAt = DateTime.Now;
            order.UpdatedId = customerId;


            if (model.OrderItems == null || model.OrderItems.Count != savedItems.Count)
            {
                throw new ArgumentException(Resource.InvalidOrderItems);
            }

            var checkedIds = new HashSet<int>();

            foreach (var submittedItem in model.OrderItems)
            {
                var savedItem = savedItems.SingleOrDefault(item => item.OrderItemId == submittedItem.OrderItemId);

                // Reject items from other orders and duplicate item IDs
                if (savedItem == null || !checkedIds.Add(submittedItem.OrderItemId))
                {
                    throw new ArgumentException(Resource.InvalidOrderItems);
                }

                if (submittedItem.MessageOnCakes?.Length > 500)
                {
                    throw new ArgumentException(Resource.CakeMessageTooLong);
                }

                // Update only the message
                savedItem.MessageOnCakes =
                submittedItem.MessageOnCakes?.Trim();

                session.Update(savedItem);
            }

            session.Update(order);
            transaction.Commit();
        }
        public void UpdateStatus(int orderId,string status,int bakerId, string? rejectionReason = null)
        {

            using var session = _nhibernateHelper.OpenSession();
            using var transaction = session.BeginTransaction();

            try
            {
                var baker = session.Get<User>(bakerId);

                if (baker == null || baker.GetRoleName() != "Baker")
                {
                    throw new UnauthorizedAccessException(Resource.InvalidBakerAccount);
                }

                var order = session.Get<Order>(orderId);

                if (order == null)
                    throw new ArgumentException(Resource.OrderNotFound);

                var allowedTransitions = new Dictionary<string, string[]>
                {
                    { 
                        OrderStatus.Pending, new[] 
                        { 
                            OrderStatus.Preparing, OrderStatus.Rejected
                        }
                    },


                    {
                        OrderStatus.Preparing, new[] 
                        {
                            OrderStatus.Ready,
                        } 
                    },

                    {
                        OrderStatus.Ready, new[]
                        {
                            OrderStatus.Completed,
                        } 
                    },

                    {
                        OrderStatus.Rejected, 
                        Array.Empty<string>() 
                    },

                    {
                        OrderStatus.Completed, 
                        Array.Empty<string>() 
                    }
                };

                // Validate current status
                if (!allowedTransitions.TryGetValue(order.OrderStatus, out var allowedStatuses))
                {
                    throw new InvalidOperationException(Resource.UnknownOrderStatus);
                }


                // Validate requested status
                if (Array.IndexOf(allowedStatuses, status) < 0)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            Resource.InvalidOrderStatusTransition,
                            order.OrderStatus,
                            status
                        )
                     );
                }

                if (status == OrderStatus.Rejected)
                {
                    rejectionReason = rejectionReason?.Trim();

                    if (string.IsNullOrWhiteSpace(rejectionReason))
                    {
                        throw new ArgumentException(Resource.RejectionReasonRequired);
                    }

                    if (rejectionReason.Length > 500)
                    {
                        throw new ArgumentException(Resource.RejectionReasonTooLong);
                    }

                    order.RejectionReason = rejectionReason;
                }

                order.OrderStatus = status;
                order.BakerId = bakerId;
                order.UpdatedAt = DateTime.Now;
                order.UpdatedId = bakerId;

                session.Update(order);

                //if rejected
                
                if (status == OrderStatus.Rejected)
                {
                    var payment = session.QueryOver<Payment>()
                        .Where(p => p.Order.OrderId == orderId)
                        .SingleOrDefault();

                    if (payment == null)
                    {
                        throw new InvalidOperationException(Resource.PaymentNotFound);
                    }

                    if (payment.PaymentStatus != PaymentStatus.Paid)
                    {
                        throw new InvalidOperationException(Resource.PaymentRefundNotAllowed);
                    }

                    payment.PaymentStatus = PaymentStatus.Refunded;
                    payment.RefundedAt = DateTime.Now;

                    session.Update(payment);
                }

                // status change notification , yang lain ikut je
                string statusLabel = status == OrderStatus.Ready
                    ? "Ready for Pickup"
                    : status;

                var notification = new Notification
                {
                    UserId = order.CustId
                           ?? throw new InvalidOperationException(Resource.OrderCustomerMissing),
                    OrderId = order.OrderId,
                    Message = status == OrderStatus.Rejected 
                    ? Resource.OrderRejectedNotification
                    : $"Your order is now {statusLabel}.",
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                session.Save(notification);

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
