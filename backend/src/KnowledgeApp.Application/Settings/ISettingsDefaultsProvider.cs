using KnowledgeApp.Contracts.Settings;

namespace KnowledgeApp.Application.Settings;

public interface ISettingsDefaultsProvider
{
    AppSettingsDto GetDefaults();
}
