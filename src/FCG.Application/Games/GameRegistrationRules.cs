namespace FCG.Application.Games;

public static class GameRegistrationRules
{
    public static IReadOnlyDictionary<string, string[]> Validate(CreateGameCommand command)
    {
        var errors = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(command.Title))
        {
            AddError(errors, nameof(command.Title), "O título é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(command.Description))
        {
            AddError(errors, nameof(command.Description), "A descrição é obrigatória.");
        }

        if (string.IsNullOrWhiteSpace(command.Genre))
        {
            AddError(errors, nameof(command.Genre), "O gênero é obrigatório.");
        }

        return errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
    }

    public static string Normalize(string value) => value.Trim();

    private static void AddError(IDictionary<string, List<string>> errors, string field, string message)
    {
        if (!errors.TryGetValue(field, out var fieldErrors))
        {
            fieldErrors = [];
            errors[field] = fieldErrors;
        }

        fieldErrors.Add(message);
    }
}
