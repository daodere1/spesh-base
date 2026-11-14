using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace Moonfin.Server.Tests;

/// <summary>Only implements the library calls exercised by the tests; unexpected calls fail.</summary>
internal static class FakeLibraryManager
{
    public static ILibraryManager Create(
        List<VirtualFolderInfo>? folders = null,
        Action? beforeGetVirtualFolders = null,
        Func<InternalItemsQuery, List<BaseItem>>? itemsResultHandler = null,
        Func<BaseItem, List<PersonInfo>>? peopleHandler = null)
    {
        return RecordingHostProxy.Create<ILibraryManager>((method, args) =>
        {
            if (method.Name == "GetVirtualFolders")
            {
                beforeGetVirtualFolders?.Invoke();
                return folders ?? new List<VirtualFolderInfo>();
            }
            if (method.Name == "GetItemsResult" && itemsResultHandler != null)
                return new QueryResult<BaseItem>(itemsResultHandler((InternalItemsQuery)args![0]!));
            if (method.Name == "GetPeople" && args![0] is BaseItem item && peopleHandler != null)
                return peopleHandler(item);
            throw new NotSupportedException(method.Name);
        });
    }
}
