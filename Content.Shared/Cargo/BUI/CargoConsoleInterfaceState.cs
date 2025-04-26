using Robust.Shared.Serialization;

namespace Content.Shared.Cargo.BUI;

[NetSerializable, Serializable]
public sealed class CargoConsoleInterfaceState : BoundUserInterfaceState
{
    public string Name;
    public int Count;
    public int Capacity;
    public NetEntity CargoServer; // Moffstation - Cargo Server
    public List<CargoOrderData> Orders;

    public CargoConsoleInterfaceState(string name, int count, int capacity, NetEntity cargoServer, List<CargoOrderData> orders) // Moffstation - Cargo Server
    {
        Name = name;
        Count = count;
        Capacity = capacity;
        CargoServer = cargoServer; // Moffstation - Cargo Server
        Orders = orders;
    }
}
