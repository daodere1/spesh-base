using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using Moonfin.Calendar;
using Moonfin.Server.Services;
using Xunit;

namespace Moonfin.Server.Tests;

public class CalendarLibraryBatchTests
{
    [Fact]
    public void MonthUsesOneLibraryQueryAndMatchesOnlyRequestedProviderIds()
    {
        var queries = 0;
        var matched = new Movie { Id = Guid.NewGuid(), ProviderIds = new() { ["Tmdb"] = "42" } };
        var other = new Movie { Id = Guid.NewGuid(), ProviderIds = new() { ["Tmdb"] = "9999" } };
        var library = RecordingHostProxy.Create<ILibraryManager>((method, args) =>
        {
            if (method.Name == "GetItemsResult")
            {
                queries++;
                var query = Assert.IsType<InternalItemsQuery>(args![0]);
                Assert.False(query.EnableTotalRecordCount);
                return new QueryResult<BaseItem> { Items = [matched, other] };
            }
            if (method.Name == "GetCollectionFolders")
            {
                Assert.Same(matched, args![0]);
                return new List<Folder>();
            }
            throw new NotSupportedException(method.Name);
        });
        var host = new JellyfinCalendarLibrary(library, null);
        var ids = Enumerable.Range(1, 500).Select(i => new CalendarProviderIds(null, i.ToString(), null)).ToArray();
        Assert.Equal(matched.Id.ToString("N"), Assert.Single(host.FindTitles(false, ids)).Id);
        Assert.Equal(1, queries);
        Assert.Empty(host.FindTitles(false, []));
        Assert.Equal(1, queries);
    }
}
