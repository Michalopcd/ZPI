using Microsoft.AspNetCore.Mvc;

namespace WebStore.Web.Configuration;

public static class MvcOptionsExtensions
{
    public static void UsePolishValidationMessages(this MvcOptions options)
    {
        var provider = options.ModelBindingMessageProvider;
        provider.SetMissingBindRequiredValueAccessor(name => $"Brak wartości pola {name}.");
        provider.SetMissingKeyOrValueAccessor(() => "Wartość jest wymagana.");
        provider.SetMissingRequestBodyRequiredValueAccessor(() => "Treść żądania jest wymagana.");
        provider.SetValueMustNotBeNullAccessor(_ => "Wartość nie może być pusta.");
        provider.SetAttemptedValueIsInvalidAccessor((_, name) => $"Pole {name} ma niepoprawną wartość.");
        provider.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => "Podana wartość jest niepoprawna.");
        provider.SetUnknownValueIsInvalidAccessor(name => $"Pole {name} ma niepoprawną wartość.");
        provider.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Podana wartość jest niepoprawna.");
        provider.SetValueIsInvalidAccessor(_ => "Podana wartość jest niepoprawna.");
        provider.SetValueMustBeANumberAccessor(name => $"Pole {name} musi być liczbą.");
        provider.SetNonPropertyValueMustBeANumberAccessor(() => "Wartość musi być liczbą.");

        options.ModelMetadataDetailsProviders.Add(new RequiredAttributeMessageProvider());
    }
}
