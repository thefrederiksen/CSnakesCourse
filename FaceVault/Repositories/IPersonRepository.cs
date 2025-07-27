using FaceVault.Models;

namespace FaceVault.Repositories;

public interface IPersonRepository : IRepository<Person>
{
    // Name-based queries
    Task<Person?> GetByNameAsync(string name);
    Task<IEnumerable<Person>> SearchByNameAsync(string searchTerm);
    Task<bool> NameExistsAsync(string name);

    // Person management
    Task<IEnumerable<Person>> GetUnknownPeopleAsync();
    Task<IEnumerable<Person>> GetNamedPeopleAsync();
    Task<IEnumerable<Person>> GetConfirmedPeopleAsync();
    Task<IEnumerable<Person>> GetUnconfirmedPeopleAsync();

    // Statistics and counting
    Task<Person> GetPersonWithMostPhotosAsync();
    Task<IEnumerable<Person>> GetPeopleByPhotoCountAsync(int minCount = 1);
    Task<Dictionary<string, int>> GetPeoplePhotoCountsAsync();

    // Face relationships
    Task<IEnumerable<Person>> GetPeopleWithFacesAsync();
    Task<IEnumerable<Person>> GetPeopleWithoutFacesAsync();
    Task<Person?> GetPersonByFaceIdAsync(int faceId);

    // Date-based queries
    Task<IEnumerable<Person>> GetPeopleSeenInDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Person>> GetPeopleSeenInYearAsync(int year);

    // Bulk operations
    Task<int> MergePeopleAsync(int sourcePersonId, int targetPersonId);
    Task<IEnumerable<Person>> SplitPersonAsync(int personId, IEnumerable<int> faceIdsToSplit);
    Task<int> BulkArchiveAsync(IEnumerable<int> personIds);
    Task<int> BulkRestoreAsync(IEnumerable<int> personIds);

    // Statistics
    Task<int> GetUnknownPersonCountAsync();
    Task<int> GetNamedPersonCountAsync();
    Task<Dictionary<string, object>> GetPersonStatisticsAsync();
}