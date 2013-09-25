using System;
using System.Collections.Generic;
using Common.Logging;
using QuantTechnologies.API;
using QuantTechnologies.API.Trading;

namespace Strategy
{
    /// <summary>
    /// This example strategy does a few things:
    /// 1.  Once the market opens, the strategy enters once at market for all symbols.
    /// 1.  At noon, it publishes a list of all outstanding positions to the log.
    /// 2.  At the end of the day, the strategy exits all outstanding positions.
    /// </summary>
    class ManagingPositions : StrategyBase
    {
        private static readonly ILog log = LogManager.GetCurrentClassLogger();
        Dictionary<string, bool> madeAnEntry = new Dictionary<string, bool>();
        Dictionary<string, bool> hasPyramided = new Dictionary<string, bool>(); 
        bool triggeredExits = false;
        bool hasDoneMiddayUpdate = false;

        public ManagingPositions()
        {
            string[] uni = { "SPY", "DIA", "GLD", "SLV" }; // null if just want to use the entire universe
            Universe = uni;

            // if you want the OrderManager to report when an order is fully filled, use this.
            // GotFill only reports every single fill (ie, partial fills), and will not keep
            // track of when an order is fully filled.
            OrderManager.OrderFilledLink += UponCompletelyFilled;
        }

        public override void OnTimer(TimeSpan time)
        {
            if (!hasDoneMiddayUpdate && time >= new TimeSpan(12, 00, 00))
            {
                hasDoneMiddayUpdate = true;
                log.Debug("Publishing mid-day update.");
                foreach (Position p in PositionManager.Positions())
                {
                    log.DebugFormat("Sym: {0}  Side: {1}  Size: {2}  AvgPrice: {3}  OpenPnL: {4}",
                        p.Symbol, p.Side, p.OpenSize, p.AvgPrice, p.OpenPnL);
                }
            }

            // at the end of the day, exit all remaining positions at market.  do this only once.
            if (!triggeredExits && time >= new TimeSpan(15, 58, 00))
            {
                triggeredExits = true;
                log.Debug("Flattening all positions.");
                OrderManager.CancelAllOpenOrders();
                PositionManager.CloseAllOpenPositions();
            }
        }

        public override void OnTick(Tick tick)
        {
            // go long if this is a symbol we haven't entered before
            bool hasSentEntry = false;
            madeAnEntry.TryGetValue(tick.Symbol, out hasSentEntry);

            if (!hasSentEntry && PositionManager[tick.Symbol].isFlat)
            {
                madeAnEntry[tick.Symbol] = true;
                PlaceOrder(new Order(OrderSide.Buy, OrderType.Market, tick.Symbol, 200));
            }

            // pyramid existing positions that we haven't yet; only pyramid for positions that already exist
            bool hasSentPyramidEntry = false;
            hasPyramided.TryGetValue(tick.Symbol, out hasSentPyramidEntry);

            if (!hasSentPyramidEntry && !PositionManager[tick.Symbol].isFlat)
            {
                hasPyramided[tick.Symbol] = true;
                PlaceOrder(new Order(OrderSide.Buy, OrderType.Market, tick.Symbol, 100));
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
            log.DebugFormat("{0}: Confirming cancel from within Strategy.  Id: {1}", DateTime.Now, id);
        }

        public override void GotSent(Order order)
        {
            log.DebugFormat("{0}  {1}: Confirming that broker received order, from within Strategy.  Type: {2}  Id: {3}  Price: {4}  Size: {5}",
                DateTime.Now, order.Symbol, order.Type, order.id, order.Price, order.Size);
        }
    }
}
