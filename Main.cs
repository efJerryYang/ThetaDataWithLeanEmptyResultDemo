#region imports
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Globalization;
    using System.Drawing;
    using QuantConnect;
    using QuantConnect.Algorithm.Framework;
    using QuantConnect.Algorithm.Framework.Selection;
    using QuantConnect.Algorithm.Framework.Alphas;
    using QuantConnect.Algorithm.Framework.Portfolio;
    using QuantConnect.Algorithm.Framework.Portfolio.SignalExports;
    using QuantConnect.Algorithm.Framework.Execution;
    using QuantConnect.Algorithm.Framework.Risk;
    using QuantConnect.Algorithm.Selection;
    using QuantConnect.Api;
    using QuantConnect.Parameters;
    using QuantConnect.Benchmarks;
    using QuantConnect.Brokerages;
    using QuantConnect.Commands;
    using QuantConnect.Configuration;
    using QuantConnect.Util;
    using QuantConnect.Interfaces;
    using QuantConnect.Algorithm;
    using QuantConnect.Indicators;
    using QuantConnect.Data;
    using QuantConnect.Data.Auxiliary;
    using QuantConnect.Data.Consolidators;
    using QuantConnect.Data.Custom;
    using QuantConnect.Data.Custom.IconicTypes;
    using QuantConnect.DataSource;
    using QuantConnect.Data.Fundamental;
    using QuantConnect.Data.Market;
    using QuantConnect.Data.Shortable;
    using QuantConnect.Data.UniverseSelection;
    using QuantConnect.Notifications;
    using QuantConnect.Orders;
    using QuantConnect.Orders.Fees;
    using QuantConnect.Orders.Fills;
    using QuantConnect.Orders.OptionExercise;
    using QuantConnect.Orders.Slippage;
    using QuantConnect.Orders.TimeInForces;
    using QuantConnect.Python;
    using QuantConnect.Scheduling;
    using QuantConnect.Securities;
    using QuantConnect.Securities.Equity;
    using QuantConnect.Securities.Future;
    using QuantConnect.Securities.Option;
    using QuantConnect.Securities.Positions;
    using QuantConnect.Securities.Forex;
    using QuantConnect.Securities.Crypto;
    using QuantConnect.Securities.CryptoFuture;
    using QuantConnect.Securities.IndexOption;
    using QuantConnect.Securities.Interfaces;
    using QuantConnect.Securities.Volatility;
    using QuantConnect.Storage;
    using QuantConnect.Statistics;
    using QCAlgorithmFramework = QuantConnect.Algorithm.QCAlgorithm;
    using QCAlgorithmFrameworkBridge = QuantConnect.Algorithm.QCAlgorithm;
    using Calendar = QuantConnect.Data.Consolidators.Calendar;
    using System.Reflection;
#endregion
using QuantConnect.DataSource;

namespace QuantConnect.Algorithm.CSharp
{
    public class JumpingRedOrangePenguin : QCAlgorithm
    {
        private Symbol underlying;
        private Symbol option;

        public override void Initialize()
        {
            QuantConnect.Logging.Log.DebuggingEnabled = true;
            SetStartDate(2024, 03, 28); //Set Start Date
            Log("[Initialize] Start Date: " + Time.ToString());
            SetEndDate(2024, 03, 29); //Set End Date
            Log("[Initialize] End Date: " + Time.ToString());
            SetCash(100000);
            Log("[Initialize] Cash: " + Portfolio.Cash);
            // before add equity
            Log("[Initialize] Before AddEquity: " + Securities.Count);
            this.underlying = AddEquity("SPY", Resolution.Hour).Symbol;
            // after add equity
            Log("[Initialize] After AddEquity: " + Securities.Count);
            // add option
            Log("[Initialize] Before AddOption: " + Securities.Count);
            this.option = AddOption("SPY", Resolution.Hour, fillForward: false).Symbol;
            // after add option
            Log("[Initialize] After AddOption: " + Securities.Count);
        }

        private static object GetMemberValue(object obj, string memberName)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj), "Object cannot be null");
            }

            Type type = obj.GetType();

            while (type != null)
            {
                // try to get field
                FieldInfo field = type.GetField(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    return field.GetValue(obj);
                }

                // try to get property
                PropertyInfo property = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null)
                {
                    return property.GetValue(obj);
                }

                type = type.BaseType;
            }

            throw new ArgumentException($"Member '{memberName}' does not exist in type '{obj.GetType()}' or its base class");
        }

        /// OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        /// Slice object keyed by symbol containing the stock data
        public override void OnData(Slice data)
        {
            // on data start
            Log("[OnData] ======================== OnData [Start] ========================");
            Log("[OnData] Time: " + Time.ToString());
            // get contract list
            // log the type of the optionChainProvider
            /*
DataFeeds.CachingOptionChainProvider: private readonly IOptionChainProvider _optionChainProvider;
 `-- private IOptionChainProvider _optionChainProvider: BacktestingOptionChainProvider
       `-- BacktestingOptionChainProvider:
             |-- _mapFileProvider
             `-- base: BacktestingChainProvider: protected IdataCacheProvider DataCacheProvider (this is a member, not a type)
                    `-- ZipDataCacheProvider: private readonly IDataProvider _dataProvider;
                         `-- DownloaderDataProvider: private readonly IDataDownloader _dataDownloader;
            */
            try
            {
                // get type of OptionChainProvider
                var optionChainProvider = this.OptionChainProvider;
                Log($"[OnData] type(OptionChainProvider): {optionChainProvider.GetType()}");
                var contracts = OptionChainProvider.GetOptionContractList(this.underlying, new DateTime(2024, 03, 28));
                Log($"[OnData] Available contracts by OptionChainProvider.GetOptionContractList(underlying, ...): {contracts.Count()}");

                Log("[OnData]   ------------------- Reflection [Start] -------------------");
                // get value of _optionChainProvider field (BacktestingOptionChainProvider)
                var backtestingOptionChainProvider = GetMemberValue(optionChainProvider, "_optionChainProvider");
                Log($"[OnData]   type(OptionChainProvider._optionChainProvider): {backtestingOptionChainProvider.GetType()}");
                // get DataCacheProvider property of the base class BacktestingChainProvider of BacktestingOptionChainProvider
                var dataCacheProvider = GetMemberValue(backtestingOptionChainProvider, "DataCacheProvider");
                Log($"[OnData]   type(_optionChainProvider.DataCacheProvider): {dataCacheProvider.GetType()}");
                // get _dataProvider field of ZipDataCacheProvider (DownloaderDataProvider)
                var dataProvider = GetMemberValue(dataCacheProvider, "_dataProvider");
                Log($"[OnData]   type(DataCacheProvider._dataProvider): {dataProvider.GetType()}");
                // get _dataDownloader field of DownloaderDataProvider
                var dataDownloader = GetMemberValue(dataProvider, "_dataDownloader");
                Log($"[OnData]   type(_dataProvider._dataDownloader): {dataDownloader.GetType()}");
                Log("[OnData]   ------------------- Reflection [End] -------------------");
            }
            catch (Exception ex)
            {
                Log($"[OnData] Exception occurred during reflection: {ex.Message}");
            }
            // on data end
            Log("[OnData] ======================== OnData [End] ========================");
        }
    }
}
