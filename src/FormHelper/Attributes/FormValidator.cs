﻿using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FormHelper
{
    [AttributeUsage(AttributeTargets.Method)]
    public class FormValidator : ActionFilterAttribute
    {
        private bool ValidateAntiforgeryToken { get; set; }
        public bool ValidateAjaxRequest { get; set; }

        public FormValidator(bool validateAntiForgeryToken = true, bool validateAjaxRequest = true)
        {
            ValidateAntiforgeryToken = validateAntiForgeryToken;
            ValidateAjaxRequest = validateAjaxRequest;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;

            var antiForgery = context.HttpContext.RequestServices.GetService<IAntiforgery>();
            if (ValidateAntiforgeryToken)
            {
                await antiForgery.ValidateRequestAsync(httpContext);
            }

            await base.OnActionExecutionAsync(context, next);
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;

            if (ValidateAjaxRequest && !httpContext.Request.IsAjaxRequest())
            {
                context.Result = new ContentResult()
                {
                    Content = "The request is not in the expected format",
                    StatusCode = StatusCodes.Status400BadRequest
                };

                return;
            }

            var modelState = context.ModelState;
            if (!modelState.IsValid)
            {
                var errorModel =
                    from x in modelState.Keys
                    where modelState[x].Errors.Count > 0
                    select new
                    {
                        key = x,
                        errors = modelState[x].Errors.
                            Select(y => y.ErrorMessage).
                            ToArray()
                    };

                var webResult = new FormResult(FormResultStatus.Error)
                {
                    ValidationErrors = new List<FormResultValidationError>()
                };

                foreach (var propertyError in errorModel)
                {
                    if (propertyError.key == "")
                    {
                        foreach (var error in propertyError.errors)
                        {
                            webResult.Message += error;

                            if (propertyError.errors.Length > 1 && error != propertyError.errors.Last())
                                webResult.Message += "<br>";
                        }

                        continue;
                    }

                    var errorMessage = new StringBuilder();

                    foreach (var error in propertyError.errors)
                    {
                        errorMessage.Append(error);

                        if (propertyError.errors.Length > 1 && error != propertyError.errors.Last())
                            errorMessage.Append("<br>");
                    }

                    webResult.ValidationErrors.Add(new FormResultValidationError
                    {
                        PropertyName = propertyError.key,
                        Message = errorMessage.ToString()
                    });
                }

                context.Result = new JsonResult(webResult);
            }

            base.OnActionExecuting(context);
        }
    }
}
