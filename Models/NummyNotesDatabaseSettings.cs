namespace NummyNotesApi.Models;

public class NummyNotesDatabaseSettings
{
    public string ConnectionString { get; set; } = null!;

    public string DatabaseName { get; set; } = null!;
    
    public string UsersCollectionName { get; set; } = null!;
}