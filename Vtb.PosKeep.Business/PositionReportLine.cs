namespace Vtb.PosKeep.Entity.Business.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Vtb.PosKeep.Entity;
    using Vtb.PosKeep.Entity.Data;
    using Vtb.PosKeep.Entity.Key;

    public class PositionReportLine
    {
        public readonly int id;
        public readonly int place;
        public readonly string short_name;

        public readonly DateTime date;
        public readonly int position;
        public readonly long from;
        public readonly long to;
        public readonly string currency;

        public readonly decimal bquantity;
        public readonly decimal bquote;
        public readonly decimal bvolume;
        public readonly decimal bprice;
        public readonly decimal bcost;
        public readonly decimal quantity;
        public readonly decimal price;
        public readonly decimal cost;
        public readonly decimal quote;
        public readonly decimal volume;
        public readonly decimal comission;
        public readonly decimal qbuy;
        public readonly decimal qsell;
        public readonly decimal vbuy;
        public readonly decimal vsell;
        public readonly decimal pbuy;
        public readonly decimal psell;
        public readonly decimal reprice;
        public readonly decimal profit;
        public readonly decimal allprofit;
        public readonly decimal percent;

        public PositionReportLine(int place_id, TradeInstrumentKey instrument, HD<ConvertPosition, CPR> begin, HD<ConvertPosition, CPR> current)
        {
            Position bpos = begin.Data.Position, cpos = current.Data.Position;

            id = instrument.Instrument;
            place = place_id; 
            position = current.Data.Position;
            short_name = ((Instrument)instrument.Instrument).Code;
            date = current.Timestamp;
            from = begin.Timestamp.AsUtc();
            to = current.Timestamp.AsUtc();
            currency = ((Currency)instrument.Currency).Code;

            if (instrument.Instrument == Instrument.Money.ID)
            {
                // денежная позиция
                bquantity = bpos.Quantity.ForProfit;
                quantity = cpos.Quantity.ForProfit;

                if (begin.Data.Position != current.Data.Position)
                {
                    qbuy = cpos.QuantityBuy;
                    qsell = cpos.QuantitySell;
                    comission = cpos.Comission;
                }
            }
            else
            {
                // инструментальная позиция
                bquantity = bpos.Quantity.ForCost;
                bquote = begin.Data;
                bvolume = bquantity * bquote;
                bcost = bpos.Cost.ForCost;

                if (!bpos.Quantity.IsNull)
                {
                    bprice = bcost / bquantity;
                }

                if (!cpos.Quantity.IsNull)
                {
                    quantity = cpos.Quantity.ForCost;
                    price = cpos.Cost.Value / quantity;
                }
                cost = cpos.Cost.ForCost;

                quote = current.Data;
                volume = cpos.Quantity.ForCost * quote;

                reprice = volume - cost;

                if (begin.Data.Position != current.Data.Position)
                {
                    qbuy = cpos.QuantityBuy;
                    qsell = cpos.QuantitySell;
                    vsell = cpos.VolumeSell;
                    vbuy = cpos.VolumeBuy;
                    comission = cpos.Comission;

                    if (qbuy != 0) pbuy = vbuy / qbuy;
                    if (qsell != 0) psell = vsell / qsell;

                    profit = cpos.Profit.ForProfit;
                }

                //allprofit = volume - bvolume + vsell - vbuy - comission;
                allprofit = cost - bcost + vsell - vbuy - comission;

                percent = (volume != 0) ? 100 * bvolume / volume : 0m;
            }
        }

        public PositionReportLine(long afrom, long ato, string acurrency, decimal abvolume, decimal acost, decimal avolume,
            decimal areprice, decimal avbuy, decimal avsell, decimal acomission, decimal aprofit, decimal apercent)
        {
            from = afrom; to = ato; currency = acurrency; bvolume = abvolume; cost = acost; volume = avolume;
            reprice = areprice; vbuy = avbuy; vsell = avsell; comission = acomission; allprofit = aprofit; percent = apercent;
        }

    }

    public static class PositionReportLineUtils
    {
        public static IEnumerable<PositionReportLine> Aggregate(this IEnumerable<PositionReportLine> lines)
        {
            foreach (var line in lines.GroupBy(l => l.currency))
            {
                using (var ll = line.GetEnumerator())
                {
                    if (ll.MoveNext())
                    {
                        var from = ll.Current.from;
                        var currency = ll.Current.currency;
                        var to = ll.Current.to;

                        var bvolume = 0m;
                        var cost = 0m;
                        var volume = 0m;
                        var reprice = 0m;
                        var vsell = 0m;
                        var vbuy = 0m;
                        var comission = 0m;
                        var profit = 0m;
                        var percent = 0m;

                        do
                        {
                            yield return ll.Current;
                            if (((Instrument)ll.Current.id).ID == Instrument.Money.ID)
                            {
                                // денежная позиция
                                bvolume = ll.Current.bvolume;
                                volume = ll.Current.volume;

                                vbuy = ll.Current.qbuy;
                                vsell = ll.Current.qsell;
                                comission = ll.Current.comission;
                            }
                            else
                            {
                                // инструментальная позиция
                                to = ll.Current.to;

                                bvolume += ll.Current.bvolume;
                                cost += ll.Current.cost;
                                volume += ll.Current.volume;
                                reprice += volume - cost;
                                vsell += ll.Current.vsell;
                                vbuy += ll.Current.vbuy;
                                comission += ll.Current.comission;
                                profit += ll.Current.allprofit;

                                percent = (volume != 0) ? 100 * bvolume / volume : 0m;
                            }
                        } while (ll.MoveNext());

                        yield return new PositionReportLine(from, to, currency, bvolume, cost, volume, reprice, vbuy, vsell, comission, profit, percent);
                    }
                }
            }
        }
    }
}
