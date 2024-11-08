﻿using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Query;
using System.ServiceModel;
using Microsoft.Crm.Sdk.Messages;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk.PluginTelemetry;
using System.Linq;
using System.Diagnostics;
using System.Linq.Expressions;

namespace PowerApps.Samples
{
    /// <summary>
    /// This plugin is used to remove queues from the related list of an email that are no longer in the TO, CC or BCC lists.
    /// It runs syncronously on the create message of the email entity so that the email never ends up on timeline views of the related entities.
    /// </summary>
    /// <remarks>Register this plug-in on the Create message, email entity, and synchronous mode.
    /// </remarks>
    public class RemoveUnreferencedQueues : IPlugin
    {

        public RemoveUnreferencedQueues(string unsecure, string secure)
        {
            // Do nothing
        }

        /// <summary>
        /// Get the activity parties for the email regardless of the participation type mask. 
        /// We will need all of the to/cc/bcc and sender as well as any related activities so we fetch all of them for efficency in processing.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="tracingService"></param>
        /// <param name="email"></param>
        /// <returns>An EntityCollection of all activity parties</returns>
        private EntityCollection GetActivityParties(IOrganizationService service, ITracingService tracingService, Entity email)
        {

            tracingService.Trace("RemoveUnreferencedQueues.GetActivityParties: Searching or activityid: " + email.Id.ToString());
            var query = new QueryExpression("activityparty");

            query.ColumnSet.AllColumns = true;
            query.Criteria.AddCondition("activityid", ConditionOperator.Equal, email.Id);

            return service.RetrieveMultiple(query);
        }

        /// <summary>
        /// Retrieve the related entities that have a queue still present in the to/cc/bcc list.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="tracingService"></param>
        /// <param name="email"></param>
        /// <param name="activityParties"></param>
        /// <returns></returns>
        private EntityCollection GetRelatedItemsToKeep(IOrganizationService service, ITracingService tracingService, Entity email, EntityCollection activityParties)
        {
            tracingService.Trace("RemoveUnreferencedQueues.GetQueuesToRemove: Searching for related objects that are no-longer applicable");
            tracingService.Trace("Num Activity Parties: " + activityParties.Entities.Count.ToString());

            // This query will find all queues that are no-longer applicable by joining the activityparty table and the msdyn_originatingqueue table on the related objects
            // and then filtering out the originatingqueue objects that still have a activity party in the to/cc/bcc list
            var query = new QueryExpression("msdyn_originatingqueue");

            // Add columns to query.ColumnSet
            query.ColumnSet.AddColumns("msdyn_createdentityid", "msdyn_createdentitytype", "msdyn_queueid");

            FilterExpression queues = new FilterExpression(LogicalOperator.Or);
            FilterExpression createdEntities = new FilterExpression(LogicalOperator.Or);
            if (activityParties.Entities.Count > 0)
            {
                foreach (var activityParty in activityParties.Entities)
                {
                    
                    int typemask = activityParty.GetAttributeValue<OptionSetValue>("participationtypemask").Value;
                    // match all of Sender,To,CC,BCC
                    if (typemask >= 1 && typemask <= 4 && activityParty.GetAttributeValue<EntityReference>("partyid").LogicalName == "queue")
                    {
                        tracingService.Trace("RemoveUnreferencedQueues.GetQueuesToRemove: Applicable Queue: " + activityParty.GetAttributeValue<EntityReference>("partyid").Id.ToString());

                        ConditionExpression condition = new ConditionExpression("msdyn_queueid", ConditionOperator.Equal, activityParty.GetAttributeValue<EntityReference>("partyid").Id);
                        queues.Conditions.Add(condition);
                    }

                    // Match Related activity parties
                    if (typemask == 13)
                    {
                        tracingService.Trace("RemoveUnreferencedQueues.GetQueuesToRemove: Applicable Entity: " + activityParty.GetAttributeValue<EntityReference>("partyid").Id.ToString());

                        ConditionExpression condition = new ConditionExpression("msdyn_createdentityid", ConditionOperator.Equal, activityParty.GetAttributeValue<EntityReference>("partyid").Id.ToString());
                        createdEntities.Conditions.Add(condition);
                    }

                }
            }

            // Put all the query conditions together
            query.Criteria.AddFilter(queues);
            query.Criteria.AddFilter(createdEntities);

            tracingService.Trace("RemoveUnreferencedQueues.GetQueuesToRemove: Executing Retrival");

            return service.RetrieveMultiple(query);
        }


        /// <summary>
        /// Execute method that is required by the IPlugin interface.
        /// </summary>
        /// <param name="serviceProvider">The service provider from which you can obtain the
        /// tracing service, plug-in execution context, organization service, and more.</param>
        public void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                ExecuteInteral(serviceProvider);
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Unknown Error :"+ ex.Message, ex);
            }
        }
        public void ExecuteInteral(IServiceProvider serviceProvider)
        {  
            //Extract the tracing service for use in debugging sandboxed plug-ins.
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            // Obtain the execution context from the service provider.
            IPluginExecutionContext context = (IPluginExecutionContext)
                serviceProvider.GetService(typeof(IPluginExecutionContext));

            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);
            
            tracingService.Trace("RemoveUnreferencedQueues.Execute: Started");

            // The InputParameters collection contains all the data passed in the message request.
            if (context.InputParameters.Contains("Target") &&
                context.InputParameters["Target"] is Entity)
            {
                // Obtain the target entity from the input parameters.
                Entity entity = (Entity)context.InputParameters["Target"];
                tracingService.Trace("RemoveUnreferencedQueues.Execute: Processing Entity Id: " + entity.Id);

                // Verify that the target entity represents an email.
                // If not, this plug-in was not registered correctly.
                if (entity.LogicalName != "email")
                {
                    tracingService.Trace("RemoveUnreferencedQueues.Execute: Invalid Registration entity is not an email Id: " + entity.Id);
                    return;
                }

                if (entity.GetAttributeValue<bool>("directioncode") == true)
                {
                    tracingService.Trace("RemoveUnreferencedQueues.Execute: Only processing inbound emails");
                    return;
                }

                tracingService.Trace("RemoveUnreferencedQueues.Execute: Verifying Entity is Email Id: " + entity.Id);

                // Fetch the data needed
                var activityparties = GetActivityParties(service,  tracingService, entity);
                var itemsToKeep = GetRelatedItemsToKeep(service,  tracingService, entity, activityparties);

                tracingService.Trace("Items to keep" + itemsToKeep.Entities.Count.ToString());

                EntityCollection newParties = new EntityCollection();

                bool itemRemoved = false;

                // Create our new related object list
                // Unfortunately it is not possible to just delete an activity party instead we must build a new list without the ones we wish to delete
                // ** Note: We still fetch just the removed items above so that any related objects that were not created by ARC will still be in the related object list
                foreach (var party in activityparties.Entities)
                {

                    bool found = false;
                    if (party.GetAttributeValue<OptionSetValue>("participationtypemask").Value != 13)
                    {
                        continue;
                    }
                    String partyId = party.GetAttributeValue<EntityReference>("partyid").Id.ToString();

                    tracingService.Trace("RemoveUnreferencedQueues.Execute: Checking if partyid is in the no-longer applicable list: " + partyId);

                    foreach (var originatingQueueEntity in itemsToKeep.Entities)
                    {
                        EntityReference createdEntityRef = new EntityReference(originatingQueueEntity.GetAttributeValue<String>("msdyn_createdentitytype"), new Guid(originatingQueueEntity.GetAttributeValue<String>("msdyn_createdentityid")));

                        if (party.GetAttributeValue<EntityReference>("partyid").Equals(createdEntityRef))
                        {

                            tracingService.Trace("RemoveUnreferencedQueues.Execute: Keeping Party with ID" + createdEntityRef.Id.ToString() + " == " + partyId);
                            found = true; 
                            break;
                        }
                        tracingService.Trace("RemoveUnreferencedQueues.Execute: " + createdEntityRef.Id.ToString() + " != " + partyId);
                    }

                    if (found)
                    {
                        newParties.Entities.Add(party);
                    }
                    else
                    {
                        itemRemoved = true;
                    }
                }

                if (itemRemoved)
                {
                    tracingService.Trace("RemoveUnreferencedQueues.Execute: Updating email");

                    // Finally perform the update on the email entity
                    Entity newEmail = new Entity("email", entity.Id);
                    newEmail.Attributes.Add("related", newParties);
                    service.Update(newEmail);
                }
                else
                {
                    tracingService.Trace("RemoveUnreferencedQueues.Execute: No changes required");
                }
            }
        }
    }
}
