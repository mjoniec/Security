﻿using Data.Model;
using Data.Repositories;
using Mqtt.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Data.Services
{
    public class GoldService : IGoldService
    {
        private readonly bool _mqttConnected;
        private readonly IGoldRepository _goldRepository;
        private readonly IMqttDualTopicClient _mqttDualTopicClient;
        private readonly Dictionary<ushort, string> _goldData;//stores gold data with request key
        private GoldDataJsonSerializer _goldDataJsonSerializer = new GoldDataJsonSerializer();

        public GoldService(IGoldRepository goldRepository, IMqttDualTopicClient mqttDualTopicClient)
        {
            _goldRepository = goldRepository;
            _goldData = new Dictionary<ushort, string>();
            _mqttDualTopicClient = mqttDualTopicClient;

            _mqttDualTopicClient.RaiseMessageReceivedEvent += ResponseReceivedHandler;

            var t = _mqttDualTopicClient.Start();

            _mqttConnected = t.Result;
        }

        //TODO issue #19 create logger and custom Exception for all erroneous cases in ResponseReceivedHandler and GetNewestPrice
        private void ResponseReceivedHandler(object sender, MessageEventArgs e)
        {
            var dataId = GoldDataJsonModifier.GetGoldDataIdFromResponseMessage(e.Message);
            var goldData = GoldDataJsonModifier.GetGoldDataFromResponseMessage(e.Message);

            if (dataId == null || !_goldData.TryGetValue(dataId.Value, out var value)) return;

            _goldData[dataId.Value] = goldData;
        }

        public ushort StartPreparingData()
        {
            if (!_mqttConnected) return ushort.MinValue;

            var dataId = (ushort)new Random().Next(ushort.MinValue + 1, ushort.MaxValue);

            try
            {
                _mqttDualTopicClient.Send(dataId.ToString());
                _goldData.Add(dataId, string.Empty);
            }
            catch
            {
                return ushort.MinValue;
            }

            return dataId;
        }

        //TODO issue #19 create logger and custom Exception for all erroneous cases in ResponseReceivedHandler and GetNewestPrice
        public string GetNewestPrice(string dataId)
        {
            //TODO write unit tests for all ushort input case scenarios and get coverage percantage
            if (string.IsNullOrEmpty(dataId) || !_mqttConnected) return string.Empty;

            var parseResult = ushort.TryParse(dataId, out var dataIdParsed);

            if (!parseResult || dataIdParsed == ushort.MinValue) return string.Empty;

            var isDataPresent = _goldData.TryGetValue(dataIdParsed, out var responseMessage);

            if (!isDataPresent) return string.Empty;

            //TODO get rid of those newlines where they were generated
            var responseMessage2 = responseMessage.Replace(Environment.NewLine, string.Empty);

            if (string.IsNullOrEmpty(responseMessage2)) return string.Empty;

            var goldDataDeserialized = JsonConvert.DeserializeObject<GoldDataModel>(responseMessage2);

            return goldDataDeserialized.NewestAvailaleDate;
        }

        public IEnumerable<string> GetAll(string dataId)
        {
            //TODO write unit tests for all ushort input case scenarios and get coverage percantage
            if (string.IsNullOrEmpty(dataId) || !_mqttConnected) return null;

            var parseResult = ushort.TryParse(dataId, out var dataIdParsed);

            if (!parseResult || dataIdParsed == ushort.MinValue) return null;

            var isDataPresent = _goldData.TryGetValue(dataIdParsed, out var responseMessage);

            if (!isDataPresent) return null;

            //TODO get rid of those newlines where they were generated
            var responseMessage2 = responseMessage.Replace(Environment.NewLine, string.Empty);

            if (string.IsNullOrEmpty(responseMessage2)) return null;

            var goldData = JsonConvert.DeserializeObject<GoldDataModel>(responseMessage2);

            var allPrices = new List<string>();

            //Refactor this functionality into model #10
            foreach (var goldPriceDataValue in goldData.DailyGoldPrices)
            {
                allPrices.Add(goldPriceDataValue.Key.ToString("yyyy-M-d")
                    + ","
                    + goldPriceDataValue.Value.ToString(new CultureInfo("en-US")));
            }

            return allPrices;
        }

        public IEnumerable<string> GetAllPrices()
        {
            var goldData = _goldRepository.Get();
            var allPrices = new List<string>();

            //Refactor this functionality into model #10
            foreach (var goldPriceDataValue in goldData.DailyGoldPrices)
            {
                allPrices.Add(goldPriceDataValue.Key.ToString("yyyy-M-d")
                    + ","
                    + goldPriceDataValue.Value.ToString(new CultureInfo("en-US")));
            }

            return allPrices;
        }
    }
}

