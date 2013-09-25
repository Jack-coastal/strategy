using System;
using System.Collections.Generic;
using Common.Logging;
using QuantTechnologies.API;
using QuantTechnologies.API.Trading;

namespace Strategy
{
    class ManagingOrders : StrategyBase
    {
        /// <summary>
        /// This strategy will send one order for every symbol that comes through.
        /// </summary>

        private static readonly ILog log = LogManager.GetCurrentClassLogger();

        // these keep track of our limits and markets so we don't send multiple orders for the same symbol
        private Dictionary<string, Order> Markets = new Dictionary<string, Order>();
        private Dictionary<string, Order> Limits = new Dictionary<string, Order>();

        public ManagingOrders()
        {
            // note: this will only work in the constructor; ie, you can't dynamically change your
            // universe while execution is occuring.
            string[] uni = { "SPY", "DIA", "GLD", "SLV" }; // null if just want to use the entire universe
            Universe = uni;

            // if you want the OrderManager to report when an order is fully filled, use this.
            // GotFill only reports every single fill (ie, partial fills), and will not keep
            // track of when an order is fully filled.
            OrderManager.OrderFilledLink += UponCompletelyFilled;
        }

        public override void OnTimer(TimeSpan time)
        {
            // cancel any limit order that's been standing for more than 15 seconds
            foreach (Order o in OrderManager.OpenOrders())
                if (o.Timestamp.TimeOfDay + new TimeSpan(0, 0, 15) < time)
                    CancelOrder(o.id);
        }

        public override void OnTick(Tick tick)
        {
            // if pre-market tick, don't do anything
            if (tick.Time < new TimeSpan(9, 30, 00))
                return;

            // send the market order; this should get filled immediately
            Order value;
            if (!Markets.TryGetValue(tick.Symbol, out value))
            {
                Order MktOrder = new Order(OrderSide.Buy, OrderType.Market, tick.Symbol, 100);
                Markets.Add(tick.Symbol, MktOrder);
                PlaceOrder(MktOrder);

                log.DebugFormat("{0}  {1}: Sent market order from within Strategy. Price: {2}  Size: {3}",
                    MktOrder.Timestamp, MktOrder.Symbol, MktOrder.Price, MktOrder.Size);
            }

            // send the limit order; this should be expensive enough that it never gets filled
            if(!Limits.TryGetValue(tick.Symbol, out value))
            {
                Order LimitOrder = new Order(OrderSide.Sell, OrderType.Limit, tick.Symbol, 100, 500);
                Limits.Add(tick.Symbol, LimitOrder);
                PlaceOrder(LimitOrder);

                log.DebugFormat("{0}  {1}: Sent limit order from within Strategy. Price: {2}  Size: {3}",
                    LimitOrder.Timestamp, LimitOrder.Symbol, LimitOrder.Price, LimitOrder.Size);
            }
        }


        void UponCompletelyFilled(ulong id)
        {
            log.DebugFormat("Order {0} was completely filled.  SYM:{1}  SIDE:{2}  SIZE:{3}",
                id, OrderManager[id].Symbol, OrderManager[id].Side, OrderManager[id].Size);
        }


        public override void GotFill(Fill fill)
        {
            log.DebugFormat("{0} {1}: Confirming fill from within Strategy. Price: {2}  Size: {3}",
                fill.Timestamp, fill.Symbol, fill.Price, fill.Size);
        }

        public override void GotCancel(ulong id)
        {
            log.DebugFormat("Confirming cancel from within Strategy.  Id: {0}", id);
        }

        public override void GotSent(Order order)
        {
            log.DebugFormat("{0}  {1}: Confirming that broker received order, from within Strategy.  Type: {2}  Id: {3}  Price: {4}  Size: {5}",
                order.Timestamp, order.Symbol, order.Type, order.id, order.Price, order.Size);
        }
    }
}
