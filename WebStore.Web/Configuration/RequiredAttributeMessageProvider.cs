using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace WebStore.Web.Configuration;

public class RequiredAttributeMessageProvider : IValidationMetadataProvider
{
    private const string DefaultMessage = "Pole {0} jest wymagane.";

    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var attribute in context.ValidationMetadata.ValidatorMetadata.OfType<RequiredAttribute>())
        {
            if (attribute.ErrorMessage == null && attribute.ErrorMessageResourceName == null)
                attribute.ErrorMessage = DefaultMessage;
        }
    }
}
