﻿using Microsoft.Dynamics.Forecasting.Common.Models;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ForecastPublicApiUsageDemo.Utility
{
    static class UsecaseHelper
    {
        public static Dictionary<string, string> attributeDisplayNameMap = new Dictionary<string, string>()
                                                                {
                                                                    { "statecode", "Status" },
                                                                    { "actualclosedate", "Actual Close Date" },
                                                                    { "statuscode", "Status Reason" },
                                                                    { "estimatedclosedate", "Est. close date" },
                                                                    { "transactioncurrencyid", "Currency" },
                                                                    { "msdyn_forecastcategory", "Forecast category" },
                                                                    { "customerid", "Potential Customer" },
                                                                    { "ownerid", "Owner" },
                                                                    { "systemuser1.systemuserid", "System User" },
                                                                    { "actualvalue", "Actual Revenue" },
                                                                    { "opportunityid", "Opportunity" },
                                                                    { "name", "Topic" },
                                                                    { "estimatedvalue", "Est. revenue" }
                                                                };
        public static void CreateCSVOfForecastInstances(ForecastConfiguration forecastConfiguration, List<ForecastInstance> forecastInstances)
        {
            LogWriter.GetLogWriter().LogWrite("Creating CSV for forecasts.");
            // Get all the columns from the forecast configuration
            List<ForecastConfigurationColumn> columns = forecastConfiguration.Columns;

            // Create the header row with the hierarchy record and names of the columns
            string headerRow = CreateHeaderRow(columns);

            // Create the data rows
            string dataRows = CreateDataRows(columns, forecastInstances);

            var csvContent = headerRow + dataRows;

            // Save the CSV file in local disk
            const string fileName = "Forecast.csv";
            File.WriteAllText(fileName, csvContent);

            LogWriter.GetLogWriter().LogWrite($"Created CSV file - {fileName}");
        }

        public static void CreateCSVOfParticipatingRecords(Entity[] opportunities, string fileName)
        {
            var logWriter = LogWriter.GetLogWriter();
            logWriter.LogWrite("Creating CSV for participating records.");

            // Get the keys from the first opportunity's attributes
            var keys = opportunities[0].Attributes.Keys;

            // Create header map and rows
            var headerRowMapForOptys = CreateHeaderMap(keys);
            var csvContent = new StringBuilder();

            // Build CSV content
            csvContent.AppendLine(CreateHeaderRowForOpty(keys, headerRowMapForOptys));
            csvContent.Append(CreateDataRowsForOpty(opportunities, headerRowMapForOptys));

            // Save the CSV file            
            File.WriteAllText(fileName, csvContent.ToString());

            LogWriter.GetLogWriter().LogWrite($"Created CSV file - {fileName}");
        }

        private static Dictionary<string, KeyValuePair<int, string>> CreateHeaderMap(ICollection<string> keys)
        {
            return keys
                .Select((key, index) => new { key, index })
                .ToDictionary(k => k.key, k => new KeyValuePair<int, string>(k.index, attributeDisplayNameMap[k.key]));
        }

        private static string CreateHeaderRowForOpty(ICollection<string> keys, Dictionary<string, KeyValuePair<int, string>> headerRowMapForOptys)
        {
            return string.Join(",", keys.Select(k => headerRowMapForOptys[k].Value));
        }

        private static string CreateDataRowsForOpty(Entity[] opportunities, Dictionary<string, KeyValuePair<int, string>> headerRowMapForOptys)
        {
            var dataRows = new StringBuilder();
            var dictLength = headerRowMapForOptys.Count;

            foreach (var opty in opportunities)
            {
                var values = new string[dictLength];

                foreach (var kvp in opty.Attributes)
                {
                    var key = kvp.Key;
                    var value = kvp.Value;
                    var idx = headerRowMapForOptys[key].Key;

                    if (value is OptionSetValue optionSetValue)
                        values[idx] = optionSetValue.Value.ToString();
                    else if (value is Money money)
                        values[idx] = money.Value.ToString();
                    else if (value is DateTime date)
                        values[idx] = date.ToString("yyyy-MM-dd");
                    else if (value is EntityReference entity)
                        values[idx] = entity.Name;
                    else if (value is AliasedValue aliasedValue)
                        values[idx] = aliasedValue.Value.ToString();
                    else if (value is Guid id)
                        values[idx] = id.ToString();
                    else if (value is string str)
                        values[idx] = str;
                    else
                        values[idx] = string.Empty;

                    if (key == "msdyn_forecastcategory")
                        values[idx] = Constants.OpportunityCategories[values[idx]];

                    if (key == "statecode")
                        values[idx] = Constants.OpportunityState[values[idx]];

                    if (key == "statuscode")
                        values[idx] = Constants.OpportunityStatusReason[values[idx]];
                }

                dataRows.AppendLine(string.Join(",", values.Select(v => v != null && v.Contains(",") ? $"\"{v}\"" : v)));
            }

            return dataRows.ToString();
        }

        private static string CreateHeaderRow(List<ForecastConfigurationColumn> columns)
        {
            return $"HierarchyRecordId,IsGroupRow,${string.Join(",", columns.Select(c => c.DisplayName).ToArray())}\n";
        }

        private static string CreateDataRows(List<ForecastConfigurationColumn> columns, List<ForecastInstance> forecastInstances)
        {
            var dataRows = new StringBuilder();
            foreach (ForecastInstance forecastInstance in forecastInstances)
            {
                var fiColumns = GetForecastInstanceColumnValues(forecastInstance.AggregatedColumns);
                Guid recordId = forecastInstance.HierarchyEntityRecord.RecordId;
                string columnData = string.Join(",", columns.Select(c => GetColumnValue(c, fiColumns)).ToArray());
                dataRows.AppendFormat($"{recordId},false,{columnData}\n");

                // check whether current fi is group node or not and create row for rolled up data if it is group node.
                var isGroupNode = forecastInstances.Exists(fi => fi.ParentInstanceId.Equals(forecastInstance.ForecastInstanceId));
                var isRootForecastInstance = forecastInstance.ParentInstanceId.Equals(Guid.Empty);
                if (isGroupNode || isRootForecastInstance)
                {
                    fiColumns = GetForecastInstanceColumnValues(forecastInstance.RolledUpColumns);
                    if (fiColumns != null)
                    {
                        var rolledUpColumnData = string.Join(",", columns.Select(c => GetColumnValue(c, fiColumns)).ToArray());
                        dataRows.AppendFormat($"{recordId},true,{rolledUpColumnData}\n");
                    }
                }
            }

            return dataRows.ToString();
        }

        private static string GetColumnValue(ForecastConfigurationColumn column, Dictionary<Guid, string> fiColumns)
        {
            return fiColumns.ContainsKey(column.ForecastConfigurationColumnId) ? fiColumns[column.ForecastConfigurationColumnId] : "";
        }

        private static Dictionary<Guid, string> GetForecastInstanceColumnValues(List<ForecastInstanceColumn> fiColumns)
        {
            return fiColumns?.ToDictionary(c => c.ForecastConfigurationColumnId, c => string.IsNullOrEmpty(c.DisplayValue) ? c.Value.ToString() : c.DisplayValue);
        }
    }
}
