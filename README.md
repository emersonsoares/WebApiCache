# WebApiCache

A package that easily supports the caching actions in AspNet WebAPI.

[Check out the NuGet package](http://nuget.org/packages/WebApiCache)

## Usage

```c#
using WebApiCache;

public class CitiesController : ApiController
{
    [OutputCacheWebApi]
    public IEnumerable<Cities> Get()
    {
        return new List<Cities>();
    }
}
```

By default timespan is 60 seconds, you can change this value as follows:

```c#
using WebApiCache;

public class CitiesController : ApiController
{
    [OutputCacheWebApi(3600)]
    public IEnumerable<Cities> Get()
    {
        return new List<Cities>();
    }
}
```