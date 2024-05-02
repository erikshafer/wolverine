// <auto-generated/>
#pragma warning disable
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using Wolverine.Http;
using Wolverine.Marten.Publishing;
using Wolverine.Runtime;

namespace Internal.Generated.WolverineHandlers
{
    // START: PUT_todos_id
    public class PUT_todos_id : Wolverine.Http.HttpHandler
    {
        private readonly Wolverine.Http.WolverineHttpOptions _wolverineHttpOptions;
        private readonly Wolverine.Marten.Publishing.OutboxedSessionFactory _outboxedSessionFactory;
        private readonly Wolverine.Runtime.IWolverineRuntime _wolverineRuntime;

        public PUT_todos_id(Wolverine.Http.WolverineHttpOptions wolverineHttpOptions, Wolverine.Marten.Publishing.OutboxedSessionFactory outboxedSessionFactory, Wolverine.Runtime.IWolverineRuntime wolverineRuntime) : base(wolverineHttpOptions)
        {
            _wolverineHttpOptions = wolverineHttpOptions;
            _outboxedSessionFactory = outboxedSessionFactory;
            _wolverineRuntime = wolverineRuntime;
        }



        public override async System.Threading.Tasks.Task Handle(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            var messageContext = new Wolverine.Runtime.MessageContext(_wolverineRuntime);
            // Building the Marten session
            await using var documentSession = _outboxedSessionFactory.OpenSession(messageContext);
            if (!int.TryParse((string)httpContext.GetRouteValue("id"), out var id))
            {
                httpContext.Response.StatusCode = 404;
                return;
            }


            // Reading the request body via JSON deserialization
            var (request, jsonContinue) = await ReadJsonAsync<WolverineWebApi.Samples.UpdateRequest>(httpContext);
            if (jsonContinue == Wolverine.HandlerContinuation.Stop) return;
            var todo = await WolverineWebApi.Samples.UpdateEndpoint.LoadAsync(id, documentSession).ConfigureAwait(false);
            // 404 if this required object is null
            if (todo == null)
            {
                httpContext.Response.StatusCode = 404;
                return;
            }

            
            // The actual HTTP request handler execution
            var storeDoc = WolverineWebApi.Samples.UpdateEndpoint.Put(id, request, todo);

            if (storeDoc != null)
            {
                
                // Placed by Wolverine's ISideEffect policy
                storeDoc.Execute(documentSession);

            }

            
            // Commit any outstanding Marten changes
            await documentSession.SaveChangesAsync(httpContext.RequestAborted).ConfigureAwait(false);

            
            // Have to flush outgoing messages just in case Marten did nothing because of https://github.com/JasperFx/wolverine/issues/536
            await messageContext.FlushOutgoingMessagesAsync().ConfigureAwait(false);

            // Wolverine automatically sets the status code to 204 for empty responses
            if (!httpContext.Response.HasStarted) httpContext.Response.StatusCode = 204;
        }

    }

    // END: PUT_todos_id
    
    
}

