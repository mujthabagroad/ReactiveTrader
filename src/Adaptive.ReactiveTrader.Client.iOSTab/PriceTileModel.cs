﻿using System;
using System.Linq;
using System.Reactive.Linq;
using MonoTouch.Foundation;
using Adaptive.ReactiveTrader.Client.Domain.Models.ReferenceData;
using Adaptive.ReactiveTrader.Client.Domain.Models.Pricing;
using Adaptive.ReactiveTrader.Client.Concurrency;
using Adaptive.ReactiveTrader.Shared.DTO.Pricing;
using Adaptive.ReactiveTrader.Shared.Extensions;
using Adaptive.ReactiveTrader.Client.Domain.Models.Execution;
using MonoTouch.SystemConfiguration;
using System.Dynamic;
using Adaptive.ReactiveTrader.Client.iOSTab.Tiles;
using System.IO;
using Adaptive.ReactiveTrader.Shared.DTO.Execution;
using Adaptive.ReactiveTrader.Client.iOSTab.Model;

namespace Adaptive.ReactiveTrader.Client.iOSTab
{
	public class PriceTileModel : NotifyingModel<PriceTileModel> // NSObject
	{
		private readonly ICurrencyPair _currencyPair;
		private readonly IConcurrencyService _concurrencyService;

		IPrice _lastPrice;

		public PriceTileModel (ICurrencyPair currencyPair, IConcurrencyService concurrencyService)
		{
			this._currencyPair = currencyPair;
			this._concurrencyService = concurrencyService;

			// default ui content
			this.Symbol = _currencyPair.BaseCurrency + " / " + _currencyPair.CounterCurrency;
			this.Status = PriceTileStatus.Streaming;
			this.RightSideBigNumber = this.LeftSideBigNumber = "--";
			this.Notional = "1000000";

			_currencyPair.PriceStream
				.ObserveOn(_concurrencyService.Dispatcher)
				.Subscribe (price => OnPrice (price));
		}

		public PriceTileStatus Status { get; set; }

		public string Symbol { get;	set; }

		public string LeftSideNumber { get; set; }
		public string LeftSideBigNumber { get; set; }
		public string LeftSidePips { get; set; }

		public string RightSideNumber  { get; set; }
		public string RightSideBigNumber  { get; set; }
		public string RightSidePips  { get; set; }

		public string Notional { get; set; }
		public TradeDoneModel TradeDone { get; set; }

		public void Bid ()
		{
			var price = _lastPrice;
			long notional;

			if (price != null && long.TryParse (Notional, out notional)) {

				Status = PriceTileStatus.Executing;

				price.Bid.ExecuteRequest ( notional, _currencyPair.BaseCurrency)
					.SubscribeOn(_concurrencyService.TaskPool)
					.ObserveOn(_concurrencyService.Dispatcher)
					.Subscribe(OnTradeResponseUpdate);

				NotifyOnChanged (this);

			}
		}

		public void Ask ()
		{
			var price = _lastPrice;
			long notional;

			if (price != null && long.TryParse (Notional, out notional)) {

				Status = PriceTileStatus.Executing;

				price.Ask.ExecuteRequest ( notional, _currencyPair.BaseCurrency)
					.SubscribeOn(_concurrencyService.TaskPool)
					.ObserveOn(_concurrencyService.Dispatcher)
					.Subscribe(OnTradeResponseUpdate);

				NotifyOnChanged (this);
			}
		}

		public void Done() {
			Status = PriceTileStatus.Streaming;
			NotifyOnChanged (this);
		}

		private void OnTradeResponseUpdate(IStale<ITrade> tradeUpdate) {
			if (tradeUpdate.IsStale) {

			} else {
				var trade = tradeUpdate.Update;
				Status = PriceTileStatus.Done;
				TradeDone = new TradeDoneModel (trade);
			}
			NotifyOnChanged (this);
		}

		void OnPrice (IPrice value)
		{
			_lastPrice = value;

			if (!value.IsStale) {
				var bid = PriceFormatter.GetFormattedPrice (value.Bid.Rate, value.CurrencyPair.RatePrecision, value.CurrencyPair.PipsPosition);
				var ask = PriceFormatter.GetFormattedPrice (value.Ask.Rate, value.CurrencyPair.RatePrecision, value.CurrencyPair.PipsPosition);

				LeftSideNumber = bid.BigFigures;
				LeftSideBigNumber = bid.Pips;
				LeftSidePips = bid.TenthOfPip;

				RightSideNumber = ask.BigFigures;
				RightSideBigNumber = ask.Pips;
				RightSidePips = ask.TenthOfPip;

				this.NotifyOnChanged (this);
			}
		}

	}
}

