using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuantTechnologies.API;
using QuantTechnologies.API.Trading;
using Common.Logging;

namespace Strategy
{
    class Imbalances : StrategyBase
    {
        private static readonly ILog log = LogManager.GetCurrentClassLogger();

        public Imbalances() : base()
        {
            this.Universe = new string[] { "AA", "BAC", "GS" };
        }

        Dictionary<string, Tick> _ticks = new Dictionary<string,Tick>();
        Dictionary<string, bool> _entered = new Dictionary<string,bool>();

        /// called at the end of every second
        public override void OnTimer(TimeSpan time)
        {
            //if(time >= new TimeSpan(16,0,0))
            //    log.Debug(time)  test2;
        }

        /// called when we receive a tick from the datafeed
        public override void OnTick(Tick tick)
        {
            _ticks[tick.Symbol] = tick;

            bool temp;
            if (!_entered.TryGetValue(tick.Symbol, out temp))
                _entered[tick.Symbol] = false;
        }

        /// called when we receive an imbalance from the datafeed
        public override void OnImbalance(Imbalance imbalance)
        {           
            // no trading morning imbalances
            if (imbalance.Time < new TimeSpan(15, 30, 00))
                return;

            // log any imbalances greater than 500k
            if (imbalance.NetImbalance >= 100000)
            {
                log.DebugFormat(
                    "{0:MM/dd/yyyy} {1}  Imbalance: {2}  Side: {3}  PairedVolume: {4}  Net: {5}  BuyQty: {6}  SellQty: {7}",
                    imbalance.Date, imbalance.Time, imbalance.Symbol, imbalance.Side, imbalance.PairedVolume,
                    imbalance.NetImbalance, imbalance.BuyVolume, imbalance.SellVolume);
            }

            // we're only going to trade imbalances that we've seen tick data for
            Tick tick;
            if(_ticks.TryGetValue(imbalance.Symbol, out tick))
            {
                bool hasEntered = false;
                _entered.TryGetValue(imbalance.Symbol, out hasEntered);

                if (!hasEntered
                    && (tick.High - tick.Low > 0)
                    && imbalance.NetImbalance >= 100000)
                {
                    if((imbalance.Side == Imbalance.ImbSide.BUY)                // buy imbalance
                    && (tick.Last - tick.Low) / (tick.High - tick.Low) >= .6m)  // stock is in upper 60% of daily range
                    {
                        _entered[tick.Symbol] = true;
                        Order entry = new Order(OrderSide.Buy, OrderType.Market, tick.Symbol, 100);
                        Order exit = new Order(OrderSide.Sell, OrderType.MarketOnClose, tick.Symbol, 100);
                        PlaceOrder(entry);
                        PlaceOrder(exit);
                    }
                    else if((imbalance.Side == Imbalance.ImbSide.SELL)          // sell imbalance
                    && (tick.Last - tick.Low) / (tick.High - tick.Low) <= .4m)  // stock is in lower 40% of daily range
                    {
                        _entered[tick.Symbol] = true;
                        Order entry = new Order(OrderSide.Sell, OrderType.Market, tick.Symbol, 100);
                        Order exit = new Order(OrderSide.Buy, OrderType.MarketOnClose, tick.Symbol, 100);
                        PlaceOrder(entry);
                        PlaceOrder(exit);
                    }
                }
            }
        }

        /// called when broker confirms we got a fill
        public override void GotFill(Fill fill) { }

        /// called when broker confirms we cancelled an order
        public override void GotCancel(ulong id) { }

        public override void OnClose(OHLC closing)
        {
            log.DebugFormat("{0} {1} S: {2} O: {3}  H: {4}  L: {5}  C: {6}",
                closing.Date, closing.Time, closing.Symbol, closing.Open, closing.High, closing.Low, closing.Close);
        }
    }
}
