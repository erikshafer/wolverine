// <auto-generated/>
#pragma warning disable
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;

namespace Internal.Generated.WolverineHandlers
{
    // START: POST_validate2_user
    public class POST_validate2_user : Wolverine.Http.HttpHandler
    {
        private readonly Wolverine.Http.WolverineHttpOptions _wolverineHttpOptions;
        private readonly FluentValidation.IValidator<Wolverine.Http.Tests.DifferentAssembly.Validation.CreateUser2> _validator_of_CreateUser21719830924;
        private readonly FluentValidation.IValidator<Wolverine.Http.Tests.DifferentAssembly.Validation.CreateUser2> _validator_of_CreateUser2816090777;
        private readonly Wolverine.Http.FluentValidation.IProblemDetailSource<Wolverine.Http.Tests.DifferentAssembly.Validation.CreateUser2> _problemDetailSource;

        public POST_validate2_user(Wolverine.Http.WolverineHttpOptions wolverineHttpOptions, [Lamar.Named("createUserValidator")] FluentValidation.IValidator<Wolverine.Http.Tests.DifferentAssembly.Validation.CreateUser2> validator_of_CreateUser21719830924, [Lamar.Named("passwordValidator")] FluentValidation.IValidator<Wolverine.Http.Tests.DifferentAssembly.Validation.CreateUser2> validator_of_CreateUser2816090777, Wolverine.Http.FluentValidation.IProblemDetailSource<Wolverine.Http.Tests.DifferentAssembly.Validation.CreateUser2> problemDetailSource) : base(wolverineHttpOptions)
        {
            _wolverineHttpOptions = wolverineHttpOptions;
            _validator_of_CreateUser21719830924 = validator_of_CreateUser21719830924;
            _validator_of_CreateUser2816090777 = validator_of_CreateUser2816090777;
            _problemDetailSource = problemDetailSource;
        }



        public override async System.Threading.Tasks.Task Handle(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            var validatorList = new System.Collections.Generic.List<FluentValidation.IValidator<Wolverine.Http.Tests.DifferentAssembly.Validation.CreateUser2>>{_validator_of_CreateUser21719830924, _validator_of_CreateUser2816090777};
            // Reading the request body via JSON deserialization
            var (user, jsonContinue) = await ReadJsonAsync<Wolverine.Http.Tests.DifferentAssembly.Validation.CreateUser2>(httpContext);
            if (jsonContinue == Wolverine.HandlerContinuation.Stop) return;
            var result1 = await Wolverine.Http.FluentValidation.Internals.FluentValidationHttpExecutor.ExecuteMany<Wolverine.Http.Tests.DifferentAssembly.Validation.CreateUser2>(validatorList, _problemDetailSource, user).ConfigureAwait(false);
            // Evaluate whether or not the execution should be stopped based on the IResult value
            if (!(result1 is Wolverine.Http.WolverineContinue))
            {
                await result1.ExecuteAsync(httpContext).ConfigureAwait(false);
                return;
            }


            
            // The actual HTTP request handler execution
            var result_of_Post = Wolverine.Http.Tests.DifferentAssembly.Validation.Validated2Endpoint.Post(user);

            await WriteString(httpContext, result_of_Post);
        }

    }

    // END: POST_validate2_user
    
    
}

