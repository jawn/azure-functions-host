﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using Microsoft.Azure.Documents.ChangeFeedProcessor;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs.Extensions.DocumentDB;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Script.Extensibility;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Script.Binding
{
    /// <summary>
    /// BindingProvider for DocumentDB extensions
    /// </summary>
    internal class DocumentDBScriptBindingProvider : ScriptBindingProvider
    {
        public DocumentDBScriptBindingProvider(JobHostConfiguration config, JObject hostMetadata, TraceWriter traceWriter)
            : base(config, hostMetadata, traceWriter)
        {
        }

        /// <inheritdoc/>
        public override bool TryCreate(ScriptBindingContext context, out ScriptBinding binding)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            binding = null;

            if (string.Compare(context.Type, "documentDB", StringComparison.OrdinalIgnoreCase) == 0
                || string.Compare(context.Type, "cosmosDBTrigger", StringComparison.OrdinalIgnoreCase) == 0)
            {
                binding = new DocumentDBScriptBinding(context);
            }

            return binding != null;
        }

        /// <inheritdoc/>
        public override void Initialize()
        {
            DocumentDBConfiguration documentDBConfiguration = new DocumentDBConfiguration();

            JObject configSection = (JObject)Metadata.GetValue("documentDB", StringComparison.OrdinalIgnoreCase);
            if (configSection != null)
            {
                JToken leaseOptions = configSection["leaseOptions"];
                if (leaseOptions != null)
                {
                    documentDBConfiguration.LeaseOptions = leaseOptions.ToObject<ChangeFeedHostOptions>();
                }

                JToken connectionMode = configSection["connectionMode"];
                if (connectionMode != null)
                {
                    if (Enum.TryParse<ConnectionMode>(connectionMode.Value<string>(), out ConnectionMode connectionModeValue))
                    {
                        documentDBConfiguration.ConnectionMode = connectionModeValue;
                    }
                }
            }

            Config.UseDocumentDB(documentDBConfiguration);
        }

        /// <inheritdoc/>
        public override bool TryResolveAssembly(string assemblyName, out Assembly assembly)
        {
            assembly = null;

            return Utility.TryMatchAssembly(assemblyName, typeof(DocumentClient), out assembly) ||
                   Utility.TryMatchAssembly(assemblyName, typeof(DocumentDBAttribute), out assembly);
        }

        private class DocumentDBScriptBinding : ScriptBinding
        {
            public DocumentDBScriptBinding(ScriptBindingContext context) : base(context)
            {
            }

            private string Id => Context.GetMetadataValue<string>("id");

            public override Type DefaultType
            {
                get
                {
                    if (Context.Access == FileAccess.Read)
                    {
                        if (Id != null)
                        {
                            return typeof(JObject);
                        }

                        return typeof(JArray);
                    }
                    else
                    {
                        return typeof(IAsyncCollector<JObject>);
                    }
                }
            }

            public override Collection<Attribute> GetAttributes()
            {
                Collection<Attribute> attributes = new Collection<Attribute>();

                string databaseName = Context.GetMetadataValue<string>("databaseName");
                string collectionName = Context.GetMetadataValue<string>("collectionName");

                if (Context.IsTrigger)
                {
                    CosmosDBTriggerAttribute attributeTrigger = new CosmosDBTriggerAttribute(databaseName, collectionName);

                    attributeTrigger.ConnectionStringSetting = Context.GetMetadataValue<string>("connectionStringSetting");
                    attributeTrigger.LeaseDatabaseName = Context.GetMetadataValue<string>("leaseDatabaseName");
                    attributeTrigger.LeaseCollectionName = Context.GetMetadataValue<string>("leaseCollectionName");
                    attributeTrigger.LeaseConnectionStringSetting = Context.GetMetadataValue<string>("leaseConnectionStringSetting");
                    attributeTrigger.CreateLeaseCollectionIfNotExists = Context.GetMetadataValue<bool>("createLeaseCollectionIfNotExists", false);
                    attributeTrigger.LeasesCollectionThroughput = Context.GetMetadataValue<int>("leaseCollectionThroughput", 0);
                    attributeTrigger.LeaseCollectionPrefix = Context.GetMetadataValue<string>("leaseCollectionPrefix");
                    attributeTrigger.FeedPollDelay = Context.GetMetadataValue<int>("feedPollDelay", 0);
                    attributeTrigger.LeaseAcquireInterval = Context.GetMetadataValue<int>("leaseAcquireInterval", 0);
                    attributeTrigger.LeaseExpirationInterval = Context.GetMetadataValue<int>("leaseExpirationInterval", 0);
                    attributeTrigger.LeaseRenewInterval = Context.GetMetadataValue<int>("leaseRenewInterval", 0);
                    attributeTrigger.CheckpointInterval = Context.GetMetadataValue<int>("checkpointInterval", 0);
                    attributeTrigger.CheckpointDocumentCount = Context.GetMetadataValue<int>("checkpointDocumentCount", 0);
                    attributeTrigger.MaxItemsPerInvocation = Context.GetMetadataValue<int>("maxItemsPerInvocation", 0);

                    attributes.Add(attributeTrigger);
                }
                else
                {
                    DocumentDBAttribute attribute = null;
                    if (!string.IsNullOrEmpty(databaseName) || !string.IsNullOrEmpty(collectionName))
                    {
                        attribute = new DocumentDBAttribute(databaseName, collectionName);
                    }
                    else
                    {
                        attribute = new DocumentDBAttribute();
                    }

                    attribute.CreateIfNotExists = Context.GetMetadataValue<bool>("createIfNotExists");
                    attribute.ConnectionStringSetting = Context.GetMetadataValue<string>("connection");
                    attribute.Id = Id;
                    attribute.PartitionKey = Context.GetMetadataValue<string>("partitionKey");
                    attribute.CollectionThroughput = Context.GetMetadataValue<int>("collectionThroughput");
                    attribute.SqlQuery = Context.GetMetadataValue<string>("sqlQuery");

                    attributes.Add(attribute);
                }

                return attributes;
            }
        }
    }
}
