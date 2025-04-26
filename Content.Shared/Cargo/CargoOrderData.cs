using Robust.Shared.Serialization;
using System.Text;

// Moffstation - Start
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;
// Moffstation - End

namespace Content.Shared.Cargo
{
    [DataDefinition, NetSerializable, Serializable]
    public sealed partial class CargoOrderData
    {
        // Moffstation - Start
        [DataField]
        public ProtoId<CargoAccountPrototype> Account;
        // Moffstation - End

        /// <summary>
        /// Price when the order was added.
        /// </summary>
        [DataField]
        public int Price;

        /// <summary>
        /// A unique (arbitrary) ID which identifies this order.
        /// </summary>
        [DataField]
        public int OrderId { get; private set; }

        /// <summary>
        /// Prototype Id for the item to be created
        /// </summary>
        [DataField]
        public string ProductId { get; private set; }

        /// <summary>
        /// Prototype Name
        /// </summary>
        [DataField]
        public string ProductName { get; private set; }

        /// <summary>
        /// The number of items in the order. Not readonly, as it might change
        /// due to caps on the amount of orders that can be placed.
        /// </summary>
        [DataField]
        public int OrderQuantity;

        /// <summary>
        /// How many instances of this order that we've already dispatched
        /// </summary>
        [DataField]
        public int NumDispatched = 0;

        [DataField]
        public string Requester { get; private set; }
        // public String RequesterRank; // TODO Figure out how to get Character ID card data
        // public int RequesterId;
        [DataField]
        public string Reason { get; private set; }
        public  bool Approved;
        [DataField]
        public string? Approver;

        public CargoOrderData(int orderId, string productId, string productName, int price, int amount, string requester, string reason, ProtoId<CargoAccountPrototype> account) // Moffstation - Cargo Server
        {
            OrderId = orderId;
            ProductId = productId;
            ProductName = productName;
            Price = price;
            OrderQuantity = amount;
            Requester = requester;
            Reason = reason;
            Account = account; // Moffstation - Cargo Server
        }

        public void SetApproverData(string? approver)
        {
            Approver = approver;
        }

        public void SetApproverData(string? fullName, string? jobTitle)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                sb.Append($"{fullName} ");
            }
            if (!string.IsNullOrWhiteSpace(jobTitle))
            {
                sb.Append($"({jobTitle})");
            }
            Approver = sb.ToString();
        }
    }
}
