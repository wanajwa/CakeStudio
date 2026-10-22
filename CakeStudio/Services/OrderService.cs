
using CakeStudio.Data;
using CakeStudio.Models;
using CakeStudio.ViewModels;
using NHibernate.Linq;
using CakeStudio.Constant;
using CakeStudio.Resources;

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
                throw new ArgumentException(Resource.CakeRequired);

            if (!model.SizeId.HasValue ||
                !model.FlavourId.HasValue ||
                !model.TierId.HasValue)
            {
                throw new ArgumentException(Resource.CakeOptionsRequired);
            }


            if (!model.RequiredDate.HasValue ||
                !model.RequiredTime.HasValue)
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

            if (method == "Delivery" &&
                string.IsNullOrWhiteSpace(model.DeliveryAddress))
            {
                throw new ArgumentException(Resource.DeliveryAddressRequired);
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
                    throw new ArgumentException(Resource.InvalidCustomerAccount);
                }

                // Validate selected cake
                var cake = session.Get<Cake>(model.CakeId.Value);

                if (cake == null || !cake.IsAvailable)
                    throw new ArgumentException(Resource.CakeUnavailable);

                decimal totalPrice = cake.BasePrice;

                // Validate and calculate size
                if (model.SizeId.HasValue)
                {
                    var size = session.Get<CakeSize>(
                        model.SizeId.Value);

                    if (size == null || !size.IsAvailable)
                        throw new ArgumentException(Resource.SizeUnavailable);


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
                        throw new ArgumentException(Resource.FlavourUnavailable);


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
                        throw new ArgumentException(Resource.TierUnavailable);


                    bool isDefaultTier = tier.TierName.Trim()
                        .Equals("1", StringComparison.OrdinalIgnoreCase);

                    if (!isDefaultTier)
                    {
                        totalPrice += tier.AdditionalPrice;
                    }

                }

                if (totalPrice <= 0 || totalPrice > 100000)
                    throw new ArgumentException(Resource.InvalidOrderPrice);

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
                    OrderStatus = Constant.Constant.OrderStatus.Pending,
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

        public OrderDetailsViewModel? GetOrderDetails(int id)
        {
            using var session = _nhibernateHelper.OpenSession();

            Order o = null!;                                                                      //null , compiler tak warn
            Cake c = null!;
            CakeSize s = null!;
            CakeFlavour f = null!;
            CakeTier t = null!;

            var order = session.QueryOver<Order>(() => o)
            // SELECT o. *
            // FROM Order o

            //LEFT JOIN Cake c
            //ON o.CakeId = c.CakeId
            .Left.JoinAlias(
                () => o.Cake,
                () => c)

            .Left.JoinAlias(
                () => o.Size,
                () => s)

            .Left.JoinAlias(
                () => o.Flavour,
                () => f)

            .Left.JoinAlias(
                () => o.Tier,
                () => t)

            //WHERE o.OrderId = id;
            .Where(() => o.OrderId == id)

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

        public void UpdateStatus(int orderId,string status,int bakerId)
        {

            using var session = _nhibernateHelper.OpenSession();
            using var transaction = session.BeginTransaction();

            try
            {
                var baker = session.Get<User>(bakerId);

                if (baker == null ||
                    baker.GetRoleName() != "Baker")
                {
                    throw new UnauthorizedAccessException(Resource.InvalidBakerAccount);
                }

                var order = session.Get<Order>(orderId);

                if (order == null)
                    throw new ArgumentException(Resource.OrderNotFound);

                var allowedTransitions = new Dictionary<string, string[]>
                {
                    { 
                        Constant.Constant.OrderStatus.Pending, new[] 
                        { 
                            Constant.Constant.OrderStatus.Preparing, Constant.Constant.OrderStatus.Rejected
                        }
                    },


                    {
                        Constant.Constant.OrderStatus.Preparing, new[] 
                        {
                            Constant.Constant.OrderStatus.Ready,
                        } 
                    },

                    {
                        Constant.Constant.OrderStatus.Ready, new[]
                        {
                            Constant.Constant.OrderStatus.Completed,
                        } 
                    },

                    {
                        Constant.Constant.OrderStatus.Rejected, 
                        Array.Empty<string>() 
                    },

                    {
                        Constant.Constant.OrderStatus.Completed, 
                        Array.Empty<string>() 
                    }
                };

                // Validate current status
                if (!allowedTransitions.TryGetValue(
                    order.OrderStatus, out var allowedStatuses))
                {
                    throw new InvalidOperationException(Resource.UnknownOrderStatus);
                }


                // Validate requested status
                if (!allowedStatuses.Contains(status))
                {
                    throw new InvalidOperationException(
                        string.Format(
                            Resource.InvalidOrderStatusTransition,
                            order.OrderStatus,
                            status
                        )
                     );
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
