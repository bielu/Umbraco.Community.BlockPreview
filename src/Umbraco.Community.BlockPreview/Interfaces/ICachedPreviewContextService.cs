using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Umbraco.Community.BlockPreview.Interfaces;

public interface ICachedPreviewContextService
{
    Task<IPublishedRequest?> CreatePublishedRequest(IPublishedContent page, HttpRequest request);
    string? GetCurrentCulture(IPublishedContent page, string culture);
    IPublishedContent GetPublishedContentForPage(int pageId);
}

public class CachedPreviewContextService : ICachedPreviewContextService
{
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;
    private readonly ILocalizationService _localizationService;
    private readonly ISiteDomainMapper _siteDomainMapper;
    private readonly IPublishedRouter _publishedRouter;
    private readonly IAppPolicyCache _runtimeCache;

    public CachedPreviewContextService(IUmbracoContextAccessor umbracoContextAccessor,
        ILocalizationService localizationService, ISiteDomainMapper siteDomainMapper, IPublishedRouter publishedRouter,AppCaches appCaches)
    {
        _umbracoContextAccessor = umbracoContextAccessor;
        _localizationService = localizationService;
        _siteDomainMapper = siteDomainMapper;
        _publishedRouter = publishedRouter;
        _runtimeCache = appCaches.RuntimeCache;
    }

    public async Task<IPublishedRequest?> CreatePublishedRequest(IPublishedContent page, HttpRequest request)
    {
        return await _runtimeCache.GetCacheItem(
            $"Preview_PublishedRequest_{page.Id}",
            async () =>
            {
                // set the published request
                var requestBuilder = await _publishedRouter.CreateRequestAsync(new Uri(request.GetDisplayUrl()));
                requestBuilder.SetPublishedContent(page);
                return requestBuilder.Build();
            }, 
            TimeSpan.FromSeconds(30))!;
      
    }

    public string? GetCurrentCulture(IPublishedContent page, string culture)
    {
        return  _runtimeCache.GetCacheItem(
            $"Preview_Culture_{page.Id}_{culture}",
             () =>
            {
                // if in a culture variant setup also set the correct language.
                var currentCulture = string.IsNullOrWhiteSpace(culture)
                    ? page.GetCultureFromDomains(_umbracoContextAccessor, _siteDomainMapper)
                    : culture;

                if (currentCulture == "undefined")
                {
                    currentCulture = _localizationService.GetDefaultLanguageIsoCode();
                }

                return currentCulture;
            }, 
            TimeSpan.FromSeconds(30))!;
       
    }

    public IPublishedContent GetPublishedContentForPage(int pageId)
    {
        return  _runtimeCache.GetCacheItem(
            $"Preview_PublishedContent_{pageId}",
            () =>
            {
                // if in a culture variant setup also set the correct language.
                if (!_umbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext context))
                    return null;

                // Get page from published cache.
                // If unpublished, then get it from preview
                return context.Content?.GetById(pageId) ?? context.Content?.GetById(true, pageId);
            }, 
            TimeSpan.FromSeconds(30))!;
       
    }
}