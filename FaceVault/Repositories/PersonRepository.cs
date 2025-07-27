using Microsoft.EntityFrameworkCore;
using FaceVault.Data;
using FaceVault.Models;

namespace FaceVault.Repositories;

public class PersonRepository : Repository<Person>, IPersonRepository
{
    public PersonRepository(FaceVaultDbContext context) : base(context)
    {
    }

    public async Task<Person?> GetByNameAsync(string name)
    {
        return await _dbSet.FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task<IEnumerable<Person>> SearchByNameAsync(string searchTerm)
    {
        return await _dbSet
            .Where(p => !p.IsArchived && p.Name.Contains(searchTerm))
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<bool> NameExistsAsync(string name)
    {
        return await _dbSet.AnyAsync(p => p.Name == name);
    }

    public async Task<IEnumerable<Person>> GetUnknownPeopleAsync()
    {
        return await _dbSet
            .Where(p => !p.IsArchived && p.Name.StartsWith("Unknown Person"))
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Person>> GetNamedPeopleAsync()
    {
        return await _dbSet
            .Where(p => !p.IsArchived && !p.Name.StartsWith("Unknown Person"))
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Person>> GetConfirmedPeopleAsync()
    {
        return await _dbSet
            .Where(p => !p.IsArchived && p.IsConfirmed)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Person>> GetUnconfirmedPeopleAsync()
    {
        return await _dbSet
            .Where(p => !p.IsArchived && !p.IsConfirmed)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Person> GetPersonWithMostPhotosAsync()
    {
        return await _dbSet
            .Where(p => !p.IsArchived)
            .OrderByDescending(p => p.ImageCount)
            .FirstAsync();
    }

    public async Task<IEnumerable<Person>> GetPeopleByPhotoCountAsync(int minCount = 1)
    {
        return await _dbSet
            .Where(p => !p.IsArchived && p.ImageCount >= minCount)
            .OrderByDescending(p => p.ImageCount)
            .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetPeoplePhotoCountsAsync()
    {
        return await _dbSet
            .Where(p => !p.IsArchived && p.ImageCount > 0)
            .ToDictionaryAsync(p => p.Name, p => p.ImageCount);
    }

    public async Task<IEnumerable<Person>> GetPeopleWithFacesAsync()
    {
        return await _dbSet
            .Where(p => !p.IsArchived && p.Faces.Any())
            .ToListAsync();
    }

    public async Task<IEnumerable<Person>> GetPeopleWithoutFacesAsync()
    {
        return await _dbSet
            .Where(p => !p.IsArchived && !p.Faces.Any())
            .ToListAsync();
    }

    public async Task<Person?> GetPersonByFaceIdAsync(int faceId)
    {
        return await _dbSet
            .Where(p => p.Faces.Any(f => f.Id == faceId))
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Person>> GetPeopleSeenInDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(p => !p.IsArchived && 
                p.Faces.Any(f => 
                    (f.Image.DateTaken ?? f.Image.DateCreated) >= startDate &&
                    (f.Image.DateTaken ?? f.Image.DateCreated) <= endDate))
            .ToListAsync();
    }

    public async Task<IEnumerable<Person>> GetPeopleSeenInYearAsync(int year)
    {
        return await _dbSet
            .Where(p => !p.IsArchived && 
                p.Faces.Any(f => 
                    (f.Image.DateTaken ?? f.Image.DateCreated).Year == year))
            .ToListAsync();
    }

    public async Task<int> MergePeopleAsync(int sourcePersonId, int targetPersonId)
    {
        var sourcePerson = await _dbSet
            .Include(p => p.Faces)
            .FirstOrDefaultAsync(p => p.Id == sourcePersonId);
        
        var targetPerson = await _dbSet
            .FirstOrDefaultAsync(p => p.Id == targetPersonId);

        if (sourcePerson == null || targetPerson == null)
            return 0;

        // Move all faces from source to target
        foreach (var face in sourcePerson.Faces)
        {
            face.PersonId = targetPersonId;
        }

        // Update target person statistics
        targetPerson.UpdateStatistics();

        // Remove source person
        _dbSet.Remove(sourcePerson);

        return await SaveChangesAsync();
    }

    public async Task<IEnumerable<Person>> SplitPersonAsync(int personId, IEnumerable<int> faceIdsToSplit)
    {
        var originalPerson = await _dbSet
            .Include(p => p.Faces)
            .FirstOrDefaultAsync(p => p.Id == personId);

        if (originalPerson == null)
            return new List<Person>();

        var faceIdsToSplitList = faceIdsToSplit.ToList();
        var facesToSplit = originalPerson.Faces.Where(f => faceIdsToSplitList.Contains(f.Id)).ToList();

        if (!facesToSplit.Any())
            return new List<Person>();

        // Create new person for split faces
        var newPerson = new Person();
        newPerson.SetAsUnknown(await GetNextUnknownPersonNumberAsync());
        
        await AddAsync(newPerson);
        await SaveChangesAsync(); // Save to get the new person ID

        // Move faces to new person
        foreach (var face in facesToSplit)
        {
            face.PersonId = newPerson.Id;
        }

        // Update statistics for both people
        originalPerson.UpdateStatistics();
        newPerson.UpdateStatistics();

        await SaveChangesAsync();

        return new List<Person> { originalPerson, newPerson };
    }

    public async Task<int> BulkArchiveAsync(IEnumerable<int> personIds)
    {
        var people = await _dbSet.Where(p => personIds.Contains(p.Id)).ToListAsync();
        
        foreach (var person in people)
        {
            person.Archive();
        }

        return await SaveChangesAsync();
    }

    public async Task<int> BulkRestoreAsync(IEnumerable<int> personIds)
    {
        var people = await _dbSet.Where(p => personIds.Contains(p.Id)).ToListAsync();
        
        foreach (var person in people)
        {
            person.Restore();
        }

        return await SaveChangesAsync();
    }

    public async Task<int> GetUnknownPersonCountAsync()
    {
        return await _dbSet.CountAsync(p => !p.IsArchived && p.Name.StartsWith("Unknown Person"));
    }

    public async Task<int> GetNamedPersonCountAsync()
    {
        return await _dbSet.CountAsync(p => !p.IsArchived && !p.Name.StartsWith("Unknown Person"));
    }

    public async Task<Dictionary<string, object>> GetPersonStatisticsAsync()
    {
        var stats = new Dictionary<string, object>();

        if (await _dbSet.AnyAsync(p => !p.IsArchived))
        {
            stats["TotalPeople"] = await _dbSet.CountAsync(p => !p.IsArchived);
            stats["NamedPeople"] = await GetNamedPersonCountAsync();
            stats["UnknownPeople"] = await GetUnknownPersonCountAsync();
            stats["ConfirmedPeople"] = await _dbSet.CountAsync(p => !p.IsArchived && p.IsConfirmed);
            stats["PeopleWithPhotos"] = await _dbSet.CountAsync(p => !p.IsArchived && p.ImageCount > 0);
            stats["AveragePhotosPerPerson"] = await _dbSet
                .Where(p => !p.IsArchived && p.ImageCount > 0)
                .AverageAsync(p => (double)p.ImageCount);
        }

        return stats;
    }

    private async Task<int> GetNextUnknownPersonNumberAsync()
    {
        var lastUnknownPerson = await _dbSet
            .Where(p => p.Name.StartsWith("Unknown Person"))
            .OrderByDescending(p => p.Name)
            .FirstOrDefaultAsync();

        if (lastUnknownPerson?.Name.StartsWith("Unknown Person ") == true)
        {
            var numberPart = lastUnknownPerson.Name.Substring("Unknown Person ".Length);
            if (int.TryParse(numberPart, out var lastNumber))
            {
                return lastNumber + 1;
            }
        }

        return 1;
    }
}