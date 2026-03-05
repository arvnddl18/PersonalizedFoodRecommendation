using Microsoft.AspNetCore.Mvc;
using Capstone.Models;
using System.Text.Json;

namespace Capstone.Controllers
{
    public class WebScrapingController : Controller
    {
        private readonly ScrapingBeeService _scrapingBeeService;
        private readonly ILogger<WebScrapingController> _logger;

        public WebScrapingController(ScrapingBeeService scrapingBeeService, ILogger<WebScrapingController> logger)
        {
            _scrapingBeeService = scrapingBeeService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Generate menu link from overview URL
        [HttpPost]
        public IActionResult GenerateMenuLink(string overviewUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(overviewUrl))
                {
                    return Json(new { success = false, message = "Overview URL is required" });
                }

                _logger.LogInformation($"Generating menu link from overview URL: {overviewUrl}");

                // Use the NavigateToMenuTab method to convert overview URL to menu URL
                var menuUrl = _scrapingBeeService.NavigateToMenuTab(overviewUrl, "");

                return Json(new { 
                    success = true, 
                    menuUrl = menuUrl,
                    originalUrl = overviewUrl,
                    message = "Menu link generated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Menu link generation failed");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Basic URL scraping following the documentation
        [HttpPost]
        public async Task<IActionResult> TestBasicScraping(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    return Json(new { success = false, message = "URL is required" });
                }

                _logger.LogInformation($"Testing basic scraping for URL: {url}");

                // Use basic HttpClient as shown in documentation
                var html = await _scrapingBeeService.CallUrl(url);
                
                return Json(new { 
                    success = true, 
                    contentLength = html.Length,
                    preview = html.Substring(0, Math.Min(500, html.Length)),
                    message = "Basic scraping successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Basic scraping failed");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // ScrapingBee API scraping
        [HttpPost]
        public async Task<IActionResult> TestScrapingBee(string url, bool renderJs = false)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    return Json(new { success = false, message = "URL is required" });
                }

                _logger.LogInformation($"Testing ScrapingBee API for URL: {url}");

                var options = new ScrapingBeeOptions
                {
                    RenderJs = renderJs,
                    Wait = 2000
                };

                var response = await _scrapingBeeService.ScrapeUrlAsync(url, options);

                if (!response.Success)
                {
                    return Json(new { 
                        success = false, 
                        message = $"ScrapingBee API Error: Status {response.StatusCode} - {response.Content}" 
                    });
                }

                return Json(new { 
                    success = true, 
                    statusCode = response.StatusCode,
                    contentLength = response.Content.Length,
                    preview = response.Content.Substring(0, Math.Min(500, response.Content.Length)),
                    message = "ScrapingBee API call successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ScrapingBee API call failed");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Extract restaurant data
        [HttpPost]
        public async Task<IActionResult> ExtractRestaurantData(string url, bool renderJs = true)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    return Json(new { success = false, message = "URL is required" });
                }

                _logger.LogInformation($"Extracting restaurant data from URL: {url}");

                // Check if it's a Google Maps URL and convert to menu URL if needed
                var isGoogleMaps = url.Contains("google.com/maps") || url.Contains("maps.google.com");
                var finalUrl = url;
                
                if (isGoogleMaps)
                {
                    _logger.LogInformation("Google Maps URL detected, converting to menu view...");
                    finalUrl = _scrapingBeeService.NavigateToMenuTab(url, "");
                    _logger.LogInformation($"Converted URL: {finalUrl}");
                }
                
                var options = new ScrapingBeeOptions
                {
                    RenderJs = renderJs,
                    Wait = 3000,
                    CustomGoogle = isGoogleMaps // Use custom_google=true for Google Maps URLs
                };

                var response = await _scrapingBeeService.ScrapeUrlAsync(finalUrl, options);

                if (!response.Success)
                {
                    return Json(new { 
                        success = false, 
                        message = $"ScrapingBee API Error: Status {response.StatusCode} - {response.Content}" 
                    });
                }

                // Parse HTML and extract restaurant data
                var restaurantData = _scrapingBeeService.ExtractRestaurantData(response.Content);

                // If TripAdvisor page, attempt to gather menu images too
                var titleDoc = _scrapingBeeService.ParseHtml(response.Content);
                var isTripAdvisor = (titleDoc.DocumentNode.SelectSingleNode("//title")?.InnerText?.ToLower() ?? "").Contains("tripadvisor");
                if (isTripAdvisor)
                {
                    var images = await _scrapingBeeService.CollectTripAdvisorMenuImagesAsync(url);
                    if (images.Count > 0)
                    {
                        restaurantData.MenuItems.AddRange(images);
                    }
                }
                // If Google Maps page, attempt to gather menu images too
                else if (isGoogleMaps)
                {
                    var images = await _scrapingBeeService.ScrapeGoogleMapsMenuImagesAsync(finalUrl);
                    if (images.Count > 0)
                    {
                        restaurantData.MenuItems.AddRange(images);
                    }
                }

                return Json(new { 
                    success = true, 
                    restaurantData = restaurantData,
                    originalUrl = url,
                    finalUrl = finalUrl,
                    urlConverted = isGoogleMaps && url != finalUrl,
                    message = "Restaurant data extracted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Restaurant data extraction failed");
                return Json(new { success = false, message = ex.Message });
            }
        }

    // Debug method to analyze scraped content
    [HttpPost]
    public async Task<IActionResult> DebugScrapedContent([FromBody] ScrapingRequest request)
    {
        if (string.IsNullOrEmpty(request.Url))
        {
            return Json(new { success = false, message = "URL is required" });
        }

        try
        {
            var response = await _scrapingBeeService.ScrapeUrlAsync(request.Url, new ScrapingBeeOptions { RenderJs = true, Wait = 3000 });

            if (!response.Success)
            {
                return Json(new { success = false, message = $"API Error: Status {response.StatusCode} - {response.Content}" });
            }

            var doc = _scrapingBeeService.ParseHtml(response.Content);

            // Check if it's TripAdvisor and analyze JSON data
            var isTripAdvisor = (doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.ToLower() ?? "").Contains("tripadvisor");
            var jsonAnalysis = "";
            var menuImageAnalysis = "";
            if (isTripAdvisor)
            {
                jsonAnalysis = _scrapingBeeService.DebugExtractJsonFromTripAdvisor(response.Content);
                menuImageAnalysis = _scrapingBeeService.DebugMenuImageExtraction(response.Content);
            }

            // Analyze the content structure
            var analysis = new
            {
                title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim(),
                isTripAdvisor = isTripAdvisor,
                jsonAnalysis = jsonAnalysis,
                menuImageAnalysis = menuImageAnalysis,
                h1Count = doc.DocumentNode.SelectNodes("//h1")?.Count ?? 0,
                h1Texts = doc.DocumentNode.SelectNodes("//h1")?.Select(h => h.InnerText.Trim()).ToArray(),
                ratingElements = doc.DocumentNode.SelectNodes("//*[contains(@class, 'rating')]")?.Count ?? 0,
                bubbleElements = doc.DocumentNode.SelectNodes("//*[contains(@class, 'bubble')]")?.Count ?? 0,
                fzMdvElements = doc.DocumentNode.SelectNodes("//*[contains(@class, 'fzMdv')]")?.Count ?? 0,
                fHvkIElements = doc.DocumentNode.SelectNodes("//*[contains(@class, 'fHvkI')]")?.Count ?? 0,
                fIrGeElements = doc.DocumentNode.SelectNodes("//*[contains(@class, 'fIrGe')]")?.Count ?? 0,
                addressElements = doc.DocumentNode.SelectNodes("//*[contains(@class, 'address')]")?.Count ?? 0,
                phoneElements = doc.DocumentNode.SelectNodes("//*[contains(@class, 'phone')]")?.Count ?? 0,
                contentLength = response.Content.Length,
                preview = response.Content.Substring(0, Math.Min(response.Content.Length, 1000)) + "..."
            };

            return Json(new { success = true, analysis = analysis });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Debug analysis failed");
            return Json(new { success = false, message = ex.Message });
        }
    }

        // Google Maps menu image scraping
        [HttpPost]
        public async Task<IActionResult> ScrapeGoogleMapsMenuImages(string googleMapsUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(googleMapsUrl))
                {
                    return Json(new { success = false, message = "Google Maps URL is required" });
                }

                _logger.LogInformation($"Scraping Google Maps menu images from URL: {googleMapsUrl}");

                var menuImages = await _scrapingBeeService.ScrapeGoogleMapsMenuImagesAsync(googleMapsUrl);

                return Json(new { 
                    success = true, 
                    menuImages = menuImages,
                    count = menuImages.Count,
                    message = $"Found {menuImages.Count} menu images from Google Maps"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google Maps menu image scraping failed");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Debug Google Maps content
        [HttpPost]
        public async Task<IActionResult> DebugGoogleMapsContent(string googleMapsUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(googleMapsUrl))
                {
                    return Json(new { success = false, message = "Google Maps URL is required" });
                }

                _logger.LogInformation($"Debugging Google Maps content from URL: {googleMapsUrl}");

                var debugInfo = await _scrapingBeeService.DebugGoogleMapsContent(googleMapsUrl);

                return Json(new { 
                    success = true, 
                    debugInfo = debugInfo,
                    message = "Debug analysis complete"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google Maps debug analysis failed");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Extract restaurant data with automatic menu URL conversion
        [HttpPost]
        public async Task<IActionResult> ExtractRestaurantDataWithMenuConversion(string url, bool renderJs = true)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    return Json(new { success = false, message = "URL is required" });
                }

                _logger.LogInformation($"Extracting restaurant data with menu conversion from URL: {url}");

                var options = new ScrapingBeeOptions
                {
                    RenderJs = renderJs,
                    Wait = 3000
                };

                var restaurantData = await _scrapingBeeService.ExtractRestaurantDataWithMenuConversion(url, options);

                return Json(new { 
                    success = true, 
                    restaurantData = restaurantData,
                    message = "Restaurant data extracted with automatic menu URL conversion"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Restaurant data extraction with menu conversion failed");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Test the integrated menu URL conversion and extraction
        [HttpPost]
        public async Task<IActionResult> TestMenuConversionAndExtraction(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    return Json(new { success = false, message = "URL is required" });
                }

                _logger.LogInformation($"Testing menu conversion and extraction for URL: {url}");

                // Test URL conversion first
                var isGoogleMaps = url.Contains("google.com/maps") || url.Contains("maps.google.com");
                var convertedUrl = url;
                
                if (isGoogleMaps)
                {
                    convertedUrl = _scrapingBeeService.NavigateToMenuTab(url, "");
                    _logger.LogInformation($"Original URL: {url}");
                    _logger.LogInformation($"Converted URL: {convertedUrl}");
                }

                // Now test extraction with the converted URL
                var options = new ScrapingBeeOptions
                {
                    RenderJs = true,
                    Wait = 3000,
                    CustomGoogle = isGoogleMaps
                };

                var response = await _scrapingBeeService.ScrapeUrlAsync(convertedUrl, options);
                
                if (!response.Success)
                {
                    return Json(new { 
                        success = false, 
                        message = $"Scraping failed: Status {response.StatusCode}",
                        originalUrl = url,
                        convertedUrl = convertedUrl,
                        urlWasConverted = isGoogleMaps && url != convertedUrl
                    });
                }

                // Extract restaurant data
                var restaurantData = _scrapingBeeService.ExtractRestaurantData(response.Content);

                return Json(new { 
                    success = true, 
                    originalUrl = url,
                    convertedUrl = convertedUrl,
                    urlWasConverted = isGoogleMaps && url != convertedUrl,
                    restaurantData = restaurantData,
                    contentLength = response.Content.Length,
                    message = "Menu conversion and extraction test completed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Menu conversion and extraction test failed");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Expand short Google Maps URLs with CID to full URLs
        [HttpPost]
        public async Task<IActionResult> ExpandGoogleMapsUrl(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    return Json(new { success = false, message = "URL is required" });
                }

                _logger.LogInformation($"Expanding Google Maps URL: {url}");

                // Use HttpClient to follow redirects and get the final URL
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                
                var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                var expandedUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;

                _logger.LogInformation($"URL expanded from {url} to {expandedUrl}");

                return Json(new { 
                    success = true, 
                    originalUrl = url,
                    expandedUrl = expandedUrl,
                    message = "URL expanded successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "URL expansion failed");
                return Json(new { 
                    success = false, 
                    originalUrl = url,
                    message = ex.Message 
                });
            }
        }

        // Extract menu using specific Google Maps selector
        [HttpPost]
        public async Task<IActionResult> ExtractMenuFromSelector(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    return Json(new { success = false, message = "URL is required" });
                }

                _logger.LogInformation($"Extracting menu from Google Maps selector for URL: {url}");

                var menuItems = await _scrapingBeeService.ExtractMenuFromGoogleMapsSelector(url);

                return Json(new { 
                    success = true, 
                    menuItems = menuItems,
                    count = menuItems.Count,
                    message = $"Extracted {menuItems.Count} menu items using Google Maps selector"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Menu extraction from selector failed");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Get sample URLs for testing
        [HttpGet]
        public IActionResult GetSampleUrls()
        {
            var sampleUrls = new[]
            {
                "https://www.tripadvisor.com/Restaurant_Review-g298459-d17329235-Reviews-Madayaw_Cafe-Davao_City_Davao_del_Sur_Province_Mindanao.html?m=69573",
                "https://www.thevegandinosaur.com/",
                "https://www.mcdonalds.com.ph/",
                "https://www.jollibee.com.ph/",
                "https://www.kfc.com.ph/",
                "https://www.pizzahut.com.ph/"
            };

            var googleMapsUrls = new[]
            {
                "https://maps.google.com/maps/place/Madayaw+Cafe/@7.0731,125.6128,17z/data=!3m1!4b1!4m6!3m5!1s0x32f96c7e8a8a8a8a:0x1234567890abcdef!8m2!3d7.0731!4d125.6128!16s%2Fg%2F11example",
                "https://maps.google.com/maps/place/Jollibee/@7.0731,125.6128,17z/data=!3m1!4b1!4m6!3m5!1s0x32f96c7e8a8a8a8a:0x1234567890abcdef!8m2!3d7.0731!4d125.6128!16s%2Fg%2F11example"
            };

            return Json(new { 
                urls = sampleUrls,
                googleMapsUrls = googleMapsUrls
            });
        }
    }

    public class ScrapingRequest
    {
        public string Url { get; set; } = string.Empty;
        public bool RenderJs { get; set; } = false;
    }
}
