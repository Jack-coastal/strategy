using System;
using QuantTechnologies.API;
using QuantTechnologies.API.Trading;
using Common.Logging;

namespace Strategy
{
    class Timestamper : StrategyBase
    {
        private static readonly ILog log = LogManager.GetCurrentClassLogger();
        int TickCount = 0;
        public Timestamper() { }

        /// called at the end of every second
        public override void OnTimer(TimeSpan time)
        {
            log.Debug(time);
        }

        /// called when we receive a tick from the datafeed
        public override void OnTick(Tick tick)
        {
            log.DebugFormat("Tick: {0}|{1}|{2}|{3}  TOTAL TICKS: {4}",
                tick.Time,
                tick.Symbol,
                tick.Last,
                tick.TotalVolume,
                ++TickCount);
        }

        /// called when we receive an imbalance from the datafeed
        public override void OnImbalance(Imbalance imbalance) { }

        /// called when broker confirms we got a fill
        public override void GotFill(Fill fill) {}

        /// called when broker confirms we cancelled an order
        public override void GotCancel(ulong id) {}

        public override void GotSent(Order order) {}
    }
}
