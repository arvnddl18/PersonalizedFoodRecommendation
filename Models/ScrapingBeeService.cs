using HtmlAgilityPack;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.IO;
using System.Text.Json;

namespace Capstone.Models
{
    public class ScrapingBeeService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl = "https://app.scrapingbee.com/api/v1/";

        public ScrapingBeeService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["ScrapingBee:ApiKey"] ?? throw new ArgumentException("ScrapingBee API key not found");
        }

        // Basic URL scraping method following the documentation
        public async Task<string> CallUrl(string fullUrl)
        {
            try
            {
                var response = await _httpClient.GetStringAsync(fullUrl);
                return response;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error calling URL: {ex.Message}");
            }
        }

        // ScrapingBee API method
        public async Task<ScrapingBeeResponse> ScrapeUrlAsync(string url, ScrapingBeeOptions? options = null)
        {
            options ??= new ScrapingBeeOptions();

            var queryParams = new List<string>
            {
                $"api_key={_apiKey}",
                $"url={Uri.EscapeDataString(url)}"
            };

            // Add options
            if (options.RenderJs) queryParams.Add("render_js=true");
            if (options.PremiumProxy) queryParams.Add("premium_proxy=true");
            if (!string.IsNullOrEmpty(options.CountryCode)) queryParams.Add($"country_code={options.CountryCode}");
            if (options.CustomGoogle) queryParams.Add("custom_google=true");
            if (!string.IsNullOrEmpty(options.SessionId)) queryParams.Add($"session_id={options.SessionId}");
            if (options.Wait > 0) queryParams.Add($"wait={options.Wait}");

            var requestUrl = $"{_baseUrl}?{string.Join("&", queryParams)}";

            try
            {
                var response = await _httpClient.GetAsync(requestUrl);
                var content = await response.Content.ReadAsStringAsync();

                return new ScrapingBeeResponse
                {
                    Success = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    Content = content,
                    Headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value))
                };
            }
            catch (Exception ex)
            {
                return new ScrapingBeeResponse
                {
                    Success = false,
                    StatusCode = 0,
                    Content = $"Error: {ex.Message}",
                    Headers = new Dictionary<string, string>()
                };
            }
        }

        // Parse HTML using HtmlAgilityPack as shown in documentation
        public HtmlDocument ParseHtml(string html)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            return htmlDoc;
        }

        // Extract restaurant data from HTML
        public RestaurantData ExtractRestaurantData(string html)
        {
            var doc = ParseHtml(html);
            var restaurantData = new RestaurantData();

            // Check if it's TripAdvisor and use specific extraction
            if (IsTripAdvisorPage(doc))
            {
                // Try to extract from JSON first
                var jsonData = ExtractJsonFromTripAdvisor(html);
                if (!string.IsNullOrEmpty(jsonData))
                {
                    try
                    {
                        ExtractFromTripAdvisorJson(jsonData, restaurantData);
                    }
                    catch
                    {
                        // Fallback to HTML parsing if JSON extraction fails
                        ExtractTripAdvisorData(doc, restaurantData);
                    }
                }
                else
                {
                    ExtractTripAdvisorData(doc, restaurantData);
                }
            }
            else if (IsGoogleMapsPage(doc))
            {
                // Extract from Google Maps
                ExtractGoogleMapsData(doc, restaurantData);
            }
            else
            {
                // Extract basic info
                ExtractBasicInfo(doc, restaurantData);

                // Extract menu items
                ExtractMenuItems(doc, restaurantData);

                // Extract contact info
                ExtractContactInfo(doc, restaurantData);
            }

            return restaurantData;
        }

        private void ExtractFromTripAdvisorJson(string jsonData, RestaurantData restaurantData)
        {
            // Parse the JSON and extract ONLY restaurant name and menu data
            // TripAdvisor's JSON structure is complex and deeply nested
            try
            {
                using var doc = JsonDocument.Parse(jsonData);
                var root = doc.RootElement;

                // Try to find restaurant name in various possible locations
                ExtractRestaurantName(root, restaurantData);

                // Try to find images/menu photos in various possible locations
                ExtractMenuImages(root, restaurantData);

                // Set other data to indicate we're only focusing on name and menu
                restaurantData.Rating = 0;
                restaurantData.Description = "Not requested - focusing on name and menu only";
                restaurantData.Address = "Not requested - focusing on name and menu only";
                restaurantData.PhoneNumber = "Not requested - focusing on name and menu only";

                // If we still don't have a name, try to extract from nested structures
                if (string.IsNullOrEmpty(restaurantData.Name))
                {
                    ExtractFromNestedStructures(root, restaurantData);
                }
            }
            catch (Exception ex)
            {
                // If JSON parsing fails, throw to trigger fallback
                throw new Exception($"JSON parsing error: {ex.Message}");
            }
        }

        private void ExtractRestaurantName(JsonElement root, RestaurantData restaurantData)
        {
            var namePaths = new[]
            {
                "name", "title", "restaurantName", "businessName",
                "data.name", "restaurant.name", "business.name",
                "pageManifest.name", "webContext.name"
            };

            foreach (var path in namePaths)
            {
                var element = GetNestedElement(root, path);
                if (element.HasValue && !string.IsNullOrEmpty(element.Value.GetString()))
                {
                    restaurantData.Name = element.Value.GetString() ?? "";
                    return;
                }
            }
        }

        private void ExtractRating(JsonElement root, RestaurantData restaurantData)
        {
            var ratingPaths = new[]
            {
                "aggregateRating.ratingValue", "rating", "averageRating",
                "data.rating", "restaurant.rating", "business.rating",
                "pageManifest.rating", "webContext.rating"
            };

            foreach (var path in ratingPaths)
            {
                var element = GetNestedElement(root, path);
                if (element.HasValue && decimal.TryParse(element.Value.GetString(), out var rating))
                {
                    restaurantData.Rating = rating;
                    return;
                }
            }
        }

        private void ExtractAddress(JsonElement root, RestaurantData restaurantData)
        {
            var addressPaths = new[]
            {
                "address", "location", "address.streetAddress",
                "data.address", "restaurant.address", "business.address",
                "pageManifest.address", "webContext.address"
            };

            foreach (var path in addressPaths)
            {
                var element = GetNestedElement(root, path);
                if (element.HasValue)
                {
                    if (element.Value.ValueKind == JsonValueKind.String)
                    {
                        restaurantData.Address = element.Value.GetString() ?? "";
                        return;
                    }
                    else if (element.Value.ValueKind == JsonValueKind.Object)
                    {
                        var addressParts = new List<string>();
                        if (element.Value.TryGetProperty("streetAddress", out var street))
                            addressParts.Add(street.GetString() ?? "");
                        if (element.Value.TryGetProperty("addressLocality", out var locality))
                            addressParts.Add(locality.GetString() ?? "");
                        if (element.Value.TryGetProperty("addressRegion", out var region))
                            addressParts.Add(region.GetString() ?? "");
                        
                        var address = string.Join(", ", addressParts.Where(s => !string.IsNullOrEmpty(s)));
                        if (!string.IsNullOrEmpty(address))
                        {
                            restaurantData.Address = address;
                            return;
                        }
                    }
                }
            }
        }

        private void ExtractPhone(JsonElement root, RestaurantData restaurantData)
        {
            var phonePaths = new[]
            {
                "telephone", "phone", "phoneNumber",
                "data.phone", "restaurant.phone", "business.phone",
                "pageManifest.phone", "webContext.phone"
            };

            foreach (var path in phonePaths)
            {
                var element = GetNestedElement(root, path);
                if (element.HasValue && !string.IsNullOrEmpty(element.Value.GetString()))
                {
                    restaurantData.PhoneNumber = element.Value.GetString() ?? "";
                    return;
                }
            }
        }

        private void ExtractDescription(JsonElement root, RestaurantData restaurantData)
        {
            var descPaths = new[]
            {
                "description", "about", "summary",
                "data.description", "restaurant.description", "business.description",
                "pageManifest.description", "webContext.description"
            };

            foreach (var path in descPaths)
            {
                var element = GetNestedElement(root, path);
                if (element.HasValue && !string.IsNullOrEmpty(element.Value.GetString()))
                {
                    restaurantData.Description = element.Value.GetString() ?? "";
                    return;
                }
            }
        }

        private void ExtractMenuImages(JsonElement root, RestaurantData restaurantData)
        {
            var imagePaths = new[]
            {
                "image", "images", "photos", "menuImages",
                "data.images", "restaurant.images", "business.images",
                "pageManifest.images", "webContext.images",
                "gallery", "photoGallery", "menuPhotos"
            };

            foreach (var path in imagePaths)
            {
                var element = GetNestedElement(root, path);
                if (element.HasValue)
                {
                    var images = ExtractImagesFromElement(element.Value);
                    foreach (var imgUrl in images.Where(s => !string.IsNullOrEmpty(s)))
                    {
                        restaurantData.MenuItems.Add(new MenuItem
                        {
                            Name = "Menu Image",
                            ImageUrl = imgUrl
                        });
                    }
                    if (images.Any()) return;
                }
            }
        }

        private List<string> ExtractImagesFromElement(JsonElement element)
        {
            var images = new List<string>();

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        images.Add(item.GetString() ?? "");
                    }
                    else if (item.ValueKind == JsonValueKind.Object)
                    {
                        if (item.TryGetProperty("url", out var urlElement))
                            images.Add(urlElement.GetString() ?? "");
                        if (item.TryGetProperty("src", out var srcElement))
                            images.Add(srcElement.GetString() ?? "");
                        if (item.TryGetProperty("imageUrl", out var imageUrlElement))
                            images.Add(imageUrlElement.GetString() ?? "");
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.String)
            {
                images.Add(element.GetString() ?? "");
            }
            else if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("url", out var urlElement))
                    images.Add(urlElement.GetString() ?? "");
                if (element.TryGetProperty("src", out var srcElement))
                    images.Add(srcElement.GetString() ?? "");
                if (element.TryGetProperty("imageUrl", out var imageUrlElement))
                    images.Add(imageUrlElement.GetString() ?? "");
            }

            return images;
        }

        private void ExtractFromNestedStructures(JsonElement root, RestaurantData restaurantData)
        {
            // Try to find data in deeply nested structures
            if (root.TryGetProperty("pageManifest", out var pageManifest))
            {
                ExtractFromPageManifest(pageManifest, restaurantData);
            }

            if (root.TryGetProperty("webContext", out var webContext))
            {
                ExtractFromWebContext(webContext, restaurantData);
            }

            if (root.TryGetProperty("data", out var data))
            {
                ExtractFromData(data, restaurantData);
            }
        }

        private void ExtractFromPageManifest(JsonElement pageManifest, RestaurantData restaurantData)
        {
            // Extract from pageManifest structure
            if (pageManifest.TryGetProperty("restaurant", out var restaurant))
            {
                if (restaurant.TryGetProperty("name", out var name) && string.IsNullOrEmpty(restaurantData.Name))
                    restaurantData.Name = name.GetString() ?? "";
                if (restaurant.TryGetProperty("rating", out var rating) && restaurantData.Rating == 0)
                    if (decimal.TryParse(rating.GetString(), out var ratingValue))
                        restaurantData.Rating = ratingValue;
            }
        }

        private void ExtractFromWebContext(JsonElement webContext, RestaurantData restaurantData)
        {
            // Extract from webContext structure
            if (webContext.TryGetProperty("restaurant", out var restaurant))
            {
                if (restaurant.TryGetProperty("name", out var name) && string.IsNullOrEmpty(restaurantData.Name))
                    restaurantData.Name = name.GetString() ?? "";
                if (restaurant.TryGetProperty("rating", out var rating) && restaurantData.Rating == 0)
                    if (decimal.TryParse(rating.GetString(), out var ratingValue))
                        restaurantData.Rating = ratingValue;
            }
        }

        private void ExtractFromData(JsonElement data, RestaurantData restaurantData)
        {
            // Extract from data structure
            if (data.TryGetProperty("restaurant", out var restaurant))
            {
                if (restaurant.TryGetProperty("name", out var name) && string.IsNullOrEmpty(restaurantData.Name))
                    restaurantData.Name = name.GetString() ?? "";
                if (restaurant.TryGetProperty("rating", out var rating) && restaurantData.Rating == 0)
                    if (decimal.TryParse(rating.GetString(), out var ratingValue))
                        restaurantData.Rating = ratingValue;
            }
        }

        private (bool HasValue, JsonElement Value) GetNestedElement(JsonElement root, string path)
        {
            var parts = path.Split('.');
            var current = root;

            foreach (var part in parts)
            {
                if (current.TryGetProperty(part, out var element))
                {
                    current = element;
                }
                else
                {
                    return (false, default);
                }
            }

            return (true, current);
        }

        // Attempts to collect menu image items from the given TripAdvisor URL by
        // scanning the current page and by following likely "menu" links.
        public async Task<List<MenuItem>> CollectTripAdvisorMenuImagesAsync(string restaurantUrl)
        {
            var collected = new List<MenuItem>();

            // 1) Fetch the restaurant page (ensure JS rendering for lazy images)
            var pageResponse = await ScrapeUrlAsync(restaurantUrl, new ScrapingBeeOptions { RenderJs = true, Wait = 3000 });
            if (!pageResponse.Success || string.IsNullOrEmpty(pageResponse.Content)) return collected;

            var doc = ParseHtml(pageResponse.Content);

            // 2) First, try to find menu images directly on the main page using your exact selector
            var mainPageMenuImages = ExtractMenuImagesFromMenuPage(doc);
            collected.AddRange(mainPageMenuImages);

            // 3) If no images found on main page, look for TripAdvisor menu buttons and follow their URLs
            if (collected.Count == 0)
            {
                var menuButtonUrls = new List<string>();
                var menuButtons = doc.DocumentNode.SelectNodes("//*[contains(@class, 'JYJOz') and contains(@class, 'Gi') and contains(@class, '_S') and contains(@class, 'Wh')]");
                if (menuButtons != null)
                {
                    foreach (var button in menuButtons)
                    {
                        var menuUrl = ExtractMenuUrlFromButton(button);
                        if (!string.IsNullOrEmpty(menuUrl))
                        {
                            menuButtonUrls.Add(menuUrl);
                        }
                    }
                }

                // 4) Follow the menu button URLs to get the actual menu images
                foreach (var menuUrl in menuButtonUrls.Take(3)) // Limit to first 3 menu URLs
                {
                    var fullMenuUrl = menuUrl.StartsWith("http") ? menuUrl : new Uri(new Uri(restaurantUrl), menuUrl).ToString();
                    
                    var menuPageResponse = await ScrapeUrlAsync(fullMenuUrl, new ScrapingBeeOptions { RenderJs = true, Wait = 3000 });
                    if (menuPageResponse.Success && !string.IsNullOrEmpty(menuPageResponse.Content))
                    {
                        var menuDoc = ParseHtml(menuPageResponse.Content);
                        var menuImages = ExtractMenuImagesFromMenuPage(menuDoc);
                        collected.AddRange(menuImages);
                    }
                }
            }

            // 5) Handle additional menu images from buttons
            var additionalMenuUrls = collected.Where(m => m.Name == "Menu Button - Additional Images").ToList();
            foreach (var menuUrl in additionalMenuUrls.Take(3)) // Limit to first 3 additional menu URLs
            {
                var fullMenuUrl = menuUrl.ImageUrl.StartsWith("http") ? menuUrl.ImageUrl : new Uri(new Uri(restaurantUrl), menuUrl.ImageUrl).ToString();
                
                var additionalMenuResponse = await ScrapeUrlAsync(fullMenuUrl, new ScrapingBeeOptions { RenderJs = true, Wait = 3000 });
                if (additionalMenuResponse.Success && !string.IsNullOrEmpty(additionalMenuResponse.Content))
                {
                    var additionalMenuDoc = ParseHtml(additionalMenuResponse.Content);
                    var additionalMenuImages = ExtractMenuImagesFromMenuPage(additionalMenuDoc);
                    collected.AddRange(additionalMenuImages);
                }
            }

            // 6) Remove button URL items and deduplicate by image URL
            var finalItems = collected.Where(m => m.Name != "Menu Button - Additional Images").ToList();
            var unique = finalItems
                .Where(m => !string.IsNullOrWhiteSpace(m.ImageUrl))
                .GroupBy(m => m.ImageUrl)
                .Select(g => g.First())
                .ToList();

            return unique;
        }

        private List<MenuItem> ExtractMenuImagesFromMenuPage(HtmlDocument doc)
        {
            var menuItems = new List<MenuItem>();

            // Use the exact CSS selectors you provided to find menu images
            // Multiple selectors for different menu image locations

            // Selector 1: Main menu images
            var selector1 = "//body/div[contains(@class, 'D') and contains(@class, 't') and contains(@class, '_U') and contains(@class, 's') and contains(@class, 'l') and contains(@class, 'Za') and contains(@class, 'f') and contains(@class, 'e') and contains(@class, 'Q') and contains(@class, 'tDZWM') and contains(@class, 'Ra') and contains(@class, 'M_') and contains(@class, 'o')]/div/div[contains(@class, 'vZwcN') and contains(@class, 'f') and contains(@class, 'e')]/div[contains(@class, 'pXUDb') and contains(@class, 'f')]/div[contains(@class, 'xrxyF') and contains(@class, 'z') and contains(@class, 'Gu')]/div/div/div[contains(@class, 'XKYCB') and contains(@class, 'wSSLS')]/div[contains(@class, 'ZGLUM') and contains(@class, 'w') and contains(@class, 'H0') and contains(@class, 'mCWMf')]/div/div[contains(@class, 'zNMPR') and contains(@class, 'w') and contains(@class, 'f') and contains(@class, 'j') and contains(@class, 'u')]/picture";

            // Selector 2: Menu button that opens additional images
            var selector2 = "//body/div[contains(@class, 'D') and contains(@class, 't') and contains(@class, '_U') and contains(@class, 's') and contains(@class, 'l') and contains(@class, 'Za') and contains(@class, 'f') and contains(@class, 'e') and contains(@class, 'Q') and contains(@class, 'tDZWM') and contains(@class, 'Ra') and contains(@class, 'M_') and contains(@class, 'o')]/div/div/div/div[contains(@class, 'lYaEG')]/div[contains(@class, 'KydbY') and contains(@class, 'Gi') and contains(@class, 'z')]/div[contains(@class, 'kIHZa') and contains(@class, 'kcSdQ')]/div/div[4]/button/div[contains(@class, 'YjpDv') and contains(@class, 'fGzmW') and contains(@class, 'Re') and contains(@class, 'z')]/picture";

            // Try selector 1 first (main menu images)
            var pictures1 = doc.DocumentNode.SelectNodes(selector1);
            if (pictures1 != null)
            {
                foreach (var picture in pictures1)
                {
                    ExtractImagesFromPicture(picture, menuItems);
                }
            }

            // Try selector 2 (menu button images)
            var pictures2 = doc.DocumentNode.SelectNodes(selector2);
            if (pictures2 != null)
            {
                foreach (var picture in pictures2)
                {
                    ExtractImagesFromPicture(picture, menuItems);
                }
            }

            // Also look for the button element itself to get its URL for additional menu images
            var buttonSelector = "//body/div[contains(@class, 'D') and contains(@class, 't') and contains(@class, '_U') and contains(@class, 's') and contains(@class, 'l') and contains(@class, 'Za') and contains(@class, 'f') and contains(@class, 'e') and contains(@class, 'Q') and contains(@class, 'tDZWM') and contains(@class, 'Ra') and contains(@class, 'M_') and contains(@class, 'o')]/div/div/div/div[contains(@class, 'lYaEG')]/div[contains(@class, 'KydbY') and contains(@class, 'Gi') and contains(@class, 'z')]/div[contains(@class, 'kIHZa') and contains(@class, 'kcSdQ')]/div/div[4]/button";
            
            var buttons = doc.DocumentNode.SelectNodes(buttonSelector);
            if (buttons != null)
            {
                foreach (var button in buttons)
                {
                    var buttonUrl = ExtractMenuUrlFromButton(button);
                    if (!string.IsNullOrEmpty(buttonUrl))
                    {
                        // Store the button URL to fetch additional menu images
                        menuItems.Add(new MenuItem
                        {
                            Name = "Menu Button - Additional Images",
                            ImageUrl = buttonUrl
                        });
                    }
                }
            }

            return menuItems;
        }

        private string GetParentClasses(HtmlNode node)
        {
            var classes = new List<string>();
            var current = node;
            
            // Get classes from parent elements up to 5 levels
            for (int i = 0; i < 5 && current != null; i++)
            {
                if (current.NodeType == HtmlNodeType.Element)
                {
                    var classAttr = current.GetAttributeValue("class", "");
                    if (!string.IsNullOrEmpty(classAttr))
                    {
                        classes.Add(classAttr);
                    }
                }
                current = current.ParentNode;
            }
            
            return string.Join(" ", classes);
        }

        private bool IsLikelyMenuImage(HtmlNode picture, string parentClasses, string src, string alt)
        {
            // Check if this is likely a menu image based on various indicators
            var menuKeywords = new[] { "menu", "food", "dish", "restaurant", "meal", "dining" };
            
            // Check parent classes for menu-related keywords
            var hasMenuClass = menuKeywords.Any(keyword => parentClasses.ToLower().Contains(keyword));
            
            // Check image source for menu-related keywords
            var hasMenuSrc = menuKeywords.Any(keyword => src.ToLower().Contains(keyword));
            
            // Check alt text for menu-related keywords
            var hasMenuAlt = menuKeywords.Any(keyword => alt.ToLower().Contains(keyword));
            
            // Check for TripAdvisor media URLs
            var isTripAdvisorMedia = src.Contains("tripadvisor.com") || src.Contains("media-cdn.tripadvisor.com");
            
            // Check for specific class patterns from your selectors
            var hasSpecificClasses = parentClasses.Contains("zNMPR") || parentClasses.Contains("ZGLUM") || 
                                   parentClasses.Contains("XKYCB") || parentClasses.Contains("xrxyF") ||
                                   parentClasses.Contains("pXUDb") || parentClasses.Contains("vZwcN") ||
                                   parentClasses.Contains("lYaEG") || parentClasses.Contains("KydbY") ||
                                   parentClasses.Contains("kIHZa") || parentClasses.Contains("YjpDv");
            
            return hasMenuClass || hasMenuSrc || hasMenuAlt || (isTripAdvisorMedia && hasSpecificClasses);
        }

        private void ExtractImagesFromPicture(HtmlNode picture, List<MenuItem> menuItems)
        {
            if (picture == null) return;

            var imgNode = picture.SelectSingleNode(".//img");
            if (imgNode != null)
            {
                var src = imgNode.GetAttributeValue("src", "");
                if (string.IsNullOrWhiteSpace(src))
                {
                    src = imgNode.GetAttributeValue("data-src", "");
                }
                if (string.IsNullOrWhiteSpace(src))
                {
                    src = imgNode.GetAttributeValue("data-lazy", "");
                }

                if (!string.IsNullOrEmpty(src))
                {
                    var baseUrl = ExtractBaseImageUrl(src);
                    if (!string.IsNullOrEmpty(baseUrl) && !menuItems.Any(item => item.ImageUrl == baseUrl))
                    {
                        var alt = imgNode.GetAttributeValue("alt", "");
                        menuItems.Add(new MenuItem
                        {
                            Name = string.IsNullOrWhiteSpace(alt) ? "Menu Item" : alt,
                            ImageUrl = baseUrl
                        });
                    }
                }
            }

            // Check for srcset in source elements within the picture
            var sourceElements = picture.SelectNodes(".//source");
            if (sourceElements != null)
            {
                foreach (var source in sourceElements)
                {
                    var srcset = source.GetAttributeValue("srcset", "");
                    if (!string.IsNullOrEmpty(srcset))
                    {
                        var baseUrl = ExtractBaseImageUrlFromSrcset(srcset);
                        if (!string.IsNullOrEmpty(baseUrl) && !menuItems.Any(item => item.ImageUrl == baseUrl))
                        {
                            menuItems.Add(new MenuItem
                            {
                                Name = "Menu Item",
                                ImageUrl = baseUrl
                            });
                        }
                    }
                }
            }
        }

        private IEnumerable<MenuItem> ExtractMenuButtonsFromDoc(HtmlDocument doc)
        {
            var items = new List<MenuItem>();

            // Only look for TripAdvisor menu buttons with the specific class you found
            var menuButtons = doc.DocumentNode.SelectNodes("//*[contains(@class, 'JYJOz') and contains(@class, 'Gi') and contains(@class, '_S') and contains(@class, 'Wh')]");
            if (menuButtons != null)
            {
                foreach (var button in menuButtons)
                {
                    // Try to extract the menu link/URL from the button
                    var menuUrl = ExtractMenuUrlFromButton(button);
                    if (!string.IsNullOrEmpty(menuUrl))
                    {
                        // Store the menu URL for later fetching
                        items.Add(new MenuItem
                        {
                            Name = "Menu Button Found",
                            ImageUrl = menuUrl // Store the menu URL here temporarily
                        });
                    }
                }
            }

            return items;
        }

        private string ExtractMenuUrlFromButton(HtmlNode button)
        {
            // Try different ways to extract the menu URL from the button
            var menuUrl = "";

            // Method 1: Check for href attribute
            menuUrl = button.GetAttributeValue("href", "");
            if (!string.IsNullOrEmpty(menuUrl))
            {
                return menuUrl;
            }

            // Method 2: Check for data attributes that might contain the menu URL
            menuUrl = button.GetAttributeValue("data-url", "");
            if (!string.IsNullOrEmpty(menuUrl))
            {
                return menuUrl;
            }

            menuUrl = button.GetAttributeValue("data-href", "");
            if (!string.IsNullOrEmpty(menuUrl))
            {
                return menuUrl;
            }

            menuUrl = button.GetAttributeValue("data-menu-url", "");
            if (!string.IsNullOrEmpty(menuUrl))
            {
                return menuUrl;
            }

            // Method 3: Look for onclick attribute that might contain a URL
            var onclick = button.GetAttributeValue("onclick", "");
            if (!string.IsNullOrEmpty(onclick))
            {
                // Try to extract URL from onclick JavaScript
                var urlMatch = System.Text.RegularExpressions.Regex.Match(onclick, @"['""]([^'""]*menu[^'""]*)['""]");
                if (urlMatch.Success)
                {
                    return urlMatch.Groups[1].Value;
                }
            }

            // Method 4: Check if it's a link element (a tag)
            if (button.Name.ToLower() == "a")
            {
                return button.GetAttributeValue("href", "");
            }

            // Method 5: Look for nested links within the button
            var nestedLink = button.SelectSingleNode(".//a[@href]");
            if (nestedLink != null)
            {
                return nestedLink.GetAttributeValue("href", "");
            }

            return "";
        }

        private string ExtractBaseImageUrl(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return string.Empty;

            try
            {
                // Handle relative URLs
                if (!imageUrl.StartsWith("http"))
                {
                    imageUrl = $"https:{imageUrl}";
                }

                // Remove query parameters to get base URL
                var uri = new Uri(imageUrl);
                return $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";
            }
            catch
            {
                // If URI parsing fails, try simple string manipulation
                var queryIndex = imageUrl.IndexOf('?');
                return queryIndex > 0 ? imageUrl.Substring(0, queryIndex) : imageUrl;
            }
        }

        private string ExtractBaseImageUrlFromSrcset(string srcset)
        {
            if (string.IsNullOrEmpty(srcset)) return string.Empty;

            // srcset format: "url1 1x, url2 2x" or "url1 300w, url2 600w"
            var urls = srcset.Split(',');
            if (urls.Length > 0)
            {
                var firstUrl = urls[0].Trim().Split(' ')[0]; // Get URL part before space
                return ExtractBaseImageUrl(firstUrl);
            }
            return string.Empty;
        }

        private IEnumerable<string> FindLikelyMenuLinks(HtmlDocument doc, string baseUrl)
        {
            var links = new List<string>();
            var anchorNodes = doc.DocumentNode.SelectNodes("//a[@href]");
            if (anchorNodes == null) return links;

            foreach (var a in anchorNodes)
            {
                var href = a.GetAttributeValue("href", "");
                var text = a.InnerText?.Trim().ToLower() ?? "";
                if (string.IsNullOrWhiteSpace(href)) continue;

                var looksLikeMenu = text.Contains("menu") || href.ToLower().Contains("menu");
                if (!looksLikeMenu) continue;

                // Build absolute URL if needed
                if (href.StartsWith("http"))
                {
                    links.Add(href);
                }
                else
                {
                    try
                    {
                        var baseUri = new Uri(baseUrl);
                        var abs = new Uri(baseUri, href).ToString();
                        links.Add(abs);
                    }
                    catch { /* ignore malformed */ }
                }
            }

            return links;
        }

    private bool IsTripAdvisorPage(HtmlDocument doc)
    {
        var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.ToLower() ?? "";
        return title.Contains("tripadvisor") ||
               doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'tripadvisor')]") != null ||
               doc.DocumentNode.SelectSingleNode("//*[contains(@id, 'tripadvisor')]") != null;
    }

    private bool IsGoogleMapsPage(HtmlDocument doc)
    {
        var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.ToLower() ?? "";
        return title.Contains("google maps") ||
               title.Contains("maps.google") ||
               doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'maps')]") != null ||
               doc.DocumentNode.SelectSingleNode("//*[@id='QA0Szd']") != null ||
               doc.DocumentNode.SelectSingleNode("//*[contains(@class, 'google')]") != null;
    }

    private void ExtractGoogleMapsData(HtmlDocument doc, RestaurantData restaurantData)
    {
        // Extract restaurant name from Google Maps
        var nameSelectors = new[]
        {
            "//h1[@data-attrid='title']",
            "//h1[contains(@class, 'x3AX1-LfntMc-header-title-title')]",
            "//h1[contains(@class, 'DUwDvf')]",
            "//h1[contains(@class, 'fontHeadlineLarge')]",
            "//h1",
            "//*[@data-attrid='title']"
        };

        foreach (var selector in nameSelectors)
        {
            var nameNode = doc.DocumentNode.SelectSingleNode(selector);
            if (nameNode != null && !string.IsNullOrWhiteSpace(nameNode.InnerText))
            {
                restaurantData.Name = nameNode.InnerText.Trim();
                break;
            }
        }

        // Set other data to indicate we're only focusing on name and menu
        restaurantData.Rating = 0;
        restaurantData.Description = "Not requested - focusing on name and menu only";
        restaurantData.Address = "Not requested - focusing on name and menu only";
        restaurantData.PhoneNumber = "Not requested - focusing on name and menu only";

        // Extract menu items from Google Maps
        ExtractGoogleMapsMenuItems(doc, restaurantData);
    }

    private void ExtractGoogleMapsMenuItems(HtmlDocument doc, RestaurantData restaurantData)
    {
        var menuItems = new List<MenuItem>();

        // Look for Google Maps menu images using the existing method
        var images = ExtractGoogleMapsMenuImages(doc);
        menuItems.AddRange(images);

        restaurantData.MenuItems = menuItems;
    }

    private string ExtractJsonFromTripAdvisor(string html)
    {
        // TripAdvisor embeds data in <script> tags with JSON
        // Look for patterns like window.__WEB_CONTEXT__={...} or similar
        var patterns = new[]
        {
            @"window\.__WEB_CONTEXT__\s*=\s*(\{.*?\});",
            @"<script[^>]*>window\.__REDUX_STATE__\s*=\s*(\{.*?\})</script>",
            @"<script[^>]*type=""application/ld\+json""[^>]*>(.*?)</script>",
            @"pageManifest\.entryFn\(.*?(\{.*?\})\)",
            @"window\.__INITIAL_STATE__\s*=\s*(\{.*?\});",
            @"window\.__APOLLO_STATE__\s*=\s*(\{.*?\});",
            @"window\.__NEXT_DATA__\s*=\s*(\{.*?\})",
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(html, pattern, 
                System.Text.RegularExpressions.RegexOptions.Singleline);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }
        }

        return string.Empty;
    }

    // Debug method to analyze what JSON data is actually extracted
    public string DebugExtractJsonFromTripAdvisor(string html)
    {
        var patterns = new[]
        {
            @"window\.__WEB_CONTEXT__\s*=\s*(\{.*?\});",
            @"<script[^>]*>window\.__REDUX_STATE__\s*=\s*(\{.*?\})</script>",
            @"<script[^>]*type=""application/ld\+json""[^>]*>(.*?)</script>",
            @"pageManifest\.entryFn\(.*?(\{.*?\})\)",
            @"window\.__INITIAL_STATE__\s*=\s*(\{.*?\});",
            @"window\.__APOLLO_STATE__\s*=\s*(\{.*?\});",
            @"window\.__NEXT_DATA__\s*=\s*(\{.*?\})",
        };

        var results = new List<string>();
        foreach (var pattern in patterns)
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(html, pattern, 
                System.Text.RegularExpressions.RegexOptions.Singleline);
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Success && match.Groups.Count > 1)
                {
                    results.Add($"Pattern: {pattern}");
                    results.Add($"Found JSON (first 500 chars): {match.Groups[1].Value.Substring(0, Math.Min(500, match.Groups[1].Value.Length))}...");
                    results.Add("---");
                }
            }
        }

        return results.Any() ? string.Join("\n", results) : "No JSON patterns found in HTML";
    }

    public string DebugMenuImageExtraction(string htmlContent)
    {
        var doc = ParseHtml(htmlContent);
        var results = new List<string>();

        // Count total pictures
        var allPictures = doc.DocumentNode.SelectNodes("//picture");
        results.Add($"Total pictures found: {allPictures?.Count ?? 0}");

        // Check for specific classes from your selectors
        var classChecks = new[]
        {
            "zNMPR", "ZGLUM", "XKYCB", "xrxyF", "pXUDb", "vZwcN",
            "lYaEG", "KydbY", "kIHZa", "YjpDv", "tDZWM"
        };

        foreach (var className in classChecks)
        {
            var elements = doc.DocumentNode.SelectNodes($"//*[contains(@class, '{className}')]");
            results.Add($"Elements with class '{className}': {elements?.Count ?? 0}");
        }

        // Look for buttons
        var buttons = doc.DocumentNode.SelectNodes("//button");
        results.Add($"Total buttons found: {buttons?.Count ?? 0}");

        // Look for TripAdvisor media URLs
        var tripAdvisorImages = doc.DocumentNode.SelectNodes("//img[contains(@src, 'tripadvisor.com') or contains(@src, 'media')]");
        results.Add($"TripAdvisor media images: {tripAdvisorImages?.Count ?? 0}");

        // Show some examples
        if (allPictures != null && allPictures.Count > 0)
        {
            results.Add("\nFirst few picture elements:");
            for (int i = 0; i < Math.Min(5, allPictures.Count); i++)
            {
                var picture = allPictures[i];
                var img = picture.SelectSingleNode(".//img");
                var src = img?.GetAttributeValue("src", "no src");
                var alt = img?.GetAttributeValue("alt", "no alt");
                var parentClasses = GetParentClasses(picture);
                results.Add($"  Picture {i + 1}: src='{src}', alt='{alt}', parentClasses='{parentClasses.Substring(0, Math.Min(100, parentClasses.Length))}'");
            }
        }

        return string.Join("\n", results);
    }

        private void ExtractTripAdvisorData(HtmlDocument doc, RestaurantData restaurantData)
        {
            // Extract restaurant name from TripAdvisor - Updated selectors based on actual page structure
            var nameSelectors = new[]
            {
                "//h1[@data-test-target='top-info-header']",
                "//h1[contains(@class, 'restaurant')]",
                "//h1[contains(@class, 'title')]",
                "//*[@data-test-target='restaurant-name']",
                "//h1[contains(@class, 'HjBfq')]", // TripAdvisor specific class
                "//h1[contains(@class, 'fHvkI')]", // Another TripAdvisor class
                "//h1"
            };

            foreach (var selector in nameSelectors)
            {
                var nameNode = doc.DocumentNode.SelectSingleNode(selector);
                if (nameNode != null && !string.IsNullOrWhiteSpace(nameNode.InnerText))
                {
                    restaurantData.Name = nameNode.InnerText.Trim();
                    break;
                }
            }

            // Set other data to indicate we're only focusing on name and menu
            restaurantData.Rating = 0;
            restaurantData.Description = "Not requested - focusing on name and menu only";
            restaurantData.Address = "Not requested - focusing on name and menu only";
            restaurantData.PhoneNumber = "Not requested - focusing on name and menu only";

            // Extract menu items from TripAdvisor (if available)
            ExtractTripAdvisorMenuItems(doc, restaurantData);
        }

        private void ExtractTripAdvisorMenuItems(HtmlDocument doc, RestaurantData restaurantData)
        {
            var menuItems = new List<MenuItem>();

            // Only look for TripAdvisor menu buttons with the specific class
            var menuButtons = doc.DocumentNode.SelectNodes("//*[contains(@class, 'JYJOz') and contains(@class, 'Gi') and contains(@class, '_S') and contains(@class, 'Wh')]");
            if (menuButtons != null)
            {
                foreach (var button in menuButtons)
                {
                    var menuUrl = ExtractMenuUrlFromButton(button);
                    if (!string.IsNullOrEmpty(menuUrl))
                    {
                        menuItems.Add(new MenuItem
                        {
                            Name = "Menu Button Found",
                            ImageUrl = menuUrl
                        });
                    }
                }
            }

            restaurantData.MenuItems = menuItems;
        }

        private void ExtractBasicInfo(HtmlDocument doc, RestaurantData restaurantData)
        {
            // Extract restaurant name using proper XPath syntax
            var nameSelectors = new[]
            {
                "//h1", "//h2", "//h3", 
                "//*[@class='restaurant-name']", "//*[@class='business-name']",
                "//*[contains(@data-testid, 'restaurant')]", "//*[contains(@class, 'name')]"
            };

            foreach (var selector in nameSelectors)
            {
                var nameNode = doc.DocumentNode.SelectSingleNode(selector);
                if (nameNode != null && !string.IsNullOrWhiteSpace(nameNode.InnerText))
                {
                    restaurantData.Name = nameNode.InnerText.Trim();
                    break;
                }
            }

            // Extract description using proper XPath syntax
            var descSelectors = new[]
            {
                "//*[@class='description']", "//*[@class='about']", "//*[@class='info']", 
                "//meta[@name='description']",
                "//*[contains(@class, 'description')]", "//*[contains(@class, 'about')]"
            };

            foreach (var selector in descSelectors)
            {
                var descNode = doc.DocumentNode.SelectSingleNode(selector);
                if (descNode != null)
                {
                    var descText = descNode.InnerText?.Trim() ?? descNode.GetAttributeValue("content", "");
                    if (!string.IsNullOrWhiteSpace(descText))
                    {
                        restaurantData.Description = descText;
                        break;
                    }
                }
            }

            // Extract rating using proper XPath syntax
            var ratingSelectors = new[]
            {
                "//*[@class='rating']", "//*[@class='stars']", 
                "//*[contains(@class, 'rating')]", "//*[contains(@class, 'star')]",
                "//*[contains(@data-testid, 'rating')]", "//meta[@property='og:rating']"
            };

            foreach (var selector in ratingSelectors)
            {
                var ratingNode = doc.DocumentNode.SelectSingleNode(selector);
                if (ratingNode != null)
                {
                    var ratingText = ratingNode.InnerText?.Trim() ?? ratingNode.GetAttributeValue("content", "");
                    if (decimal.TryParse(ratingText, out var rating))
                    {
                        restaurantData.Rating = rating;
                        break;
                    }
                }
            }
        }

        private void ExtractMenuItems(HtmlDocument doc, RestaurantData restaurantData)
        {
            var menuItems = new List<MenuItem>();

            // Common menu item selectors - using proper XPath syntax
            var menuSelectors = new[]
            {
                "//*[@class='menu-item']", "//*[@class='food-item']", "//*[@class='dish-item']", "//*[@class='product-item']",
                "//*[@class='item']", "//article", "//*[@class='card']", "//*[@class='tile']", "//*[@class='box']",
                "//*[contains(@data-testid, 'menu')]", "//*[contains(@data-testid, 'item')]", "//*[contains(@data-testid, 'dish')]",
                // TripAdvisor specific selectors
                "//*[contains(@class, 'menu')]", "//*[contains(@class, 'dish')]", "//*[contains(@class, 'food')]",
                "//*[contains(@class, 'item')]", "//*[contains(@class, 'product')]", "//*[contains(@class, 'listing')]"
            };

            foreach (var selector in menuSelectors)
            {
                var menuNodes = doc.DocumentNode.SelectNodes(selector);
                if (menuNodes != null && menuNodes.Count > 0)
                {
                    foreach (var node in menuNodes.Take(20))
                    {
                        var menuItem = ExtractMenuItemFromNode(node);
                        if (!string.IsNullOrEmpty(menuItem.Name) && menuItem.Name.Length > 2)
                        {
                            menuItems.Add(menuItem);
                        }
                    }
                    break; // Use first successful selector
                }
            }

            restaurantData.MenuItems = menuItems;
        }

        private MenuItem ExtractMenuItemFromNode(HtmlNode node)
        {
            var menuItem = new MenuItem();

            // Extract name
            var nameSelectors = new[] { "h1", "h2", "h3", "h4", ".name", ".title", ".item-name", "[class*='name']" };
            foreach (var selector in nameSelectors)
            {
                var nameNode = node.SelectSingleNode($".//{selector}");
                if (nameNode != null && !string.IsNullOrWhiteSpace(nameNode.InnerText))
                {
                    menuItem.Name = nameNode.InnerText.Trim();
                    break;
                }
            }

            // Extract price
            var priceSelectors = new[] { ".price", ".cost", ".amount", "[class*='price']", "[class*='cost']" };
            foreach (var selector in priceSelectors)
            {
                var priceNode = node.SelectSingleNode($".//{selector}");
                if (priceNode != null && !string.IsNullOrWhiteSpace(priceNode.InnerText))
                {
                    var priceText = priceNode.InnerText.Trim();
                    if (decimal.TryParse(priceText.Replace("$", "").Replace("₱", "").Replace(",", ""), out var price))
                    {
                        menuItem.Price = price;
                        break;
                    }
                }
            }

            // Extract description
            var descSelectors = new[] { ".description", ".desc", ".info", ".details", "[class*='description']" };
            foreach (var selector in descSelectors)
            {
                var descNode = node.SelectSingleNode($".//{selector}");
                if (descNode != null && !string.IsNullOrWhiteSpace(descNode.InnerText))
                {
                    menuItem.Description = descNode.InnerText.Trim();
                    break;
                }
            }

            // Extract image URL
            var imgNode = node.SelectSingleNode(".//img");
            if (imgNode != null)
            {
                var imgSrc = imgNode.GetAttributeValue("src", "");
                if (!string.IsNullOrEmpty(imgSrc))
                {
                    menuItem.ImageUrl = imgSrc.StartsWith("http") ? imgSrc : $"https:{imgSrc}";
                }
            }

            return menuItem;
        }

        private void ExtractContactInfo(HtmlDocument doc, RestaurantData restaurantData)
        {
            // Extract phone number
            var phonePattern = @"(\+?[\d\s\-\(\)]{10,})";
            var phoneMatch = System.Text.RegularExpressions.Regex.Match(doc.DocumentNode.InnerText, phonePattern);
            if (phoneMatch.Success)
            {
                restaurantData.PhoneNumber = phoneMatch.Value.Trim();
            }

            // Extract address using proper XPath syntax
            var addressSelectors = new[] 
            { 
                "//*[@class='address']", "//*[@class='location']", 
                "//*[contains(@class, 'address')]", "//*[contains(@class, 'location')]" 
            };
            foreach (var selector in addressSelectors)
            {
                var addressNode = doc.DocumentNode.SelectSingleNode(selector);
                if (addressNode != null && !string.IsNullOrWhiteSpace(addressNode.InnerText))
                {
                    restaurantData.Address = addressNode.InnerText.Trim();
                    break;
                }
            }
        }

        // Google Maps menu image scraping method
        public async Task<List<MenuItem>> ScrapeGoogleMapsMenuImagesAsync(string googleMapsUrl)
        {
            var menuItems = new List<MenuItem>();

            try
            {
                // Use ScrapingBee with JavaScript rendering to get the full page content
                var options = new ScrapingBeeOptions
                {
                    RenderJs = true,
                    Wait = 7000, // Wait 5 seconds for images to load
                    CustomGoogle = true // Use custom Google proxy for better results
                };

                var response = await ScrapeUrlAsync(googleMapsUrl, options);

                if (!response.Success || string.IsNullOrEmpty(response.Content))
                {
                    return menuItems;
                }

                var doc = ParseHtml(response.Content);
                menuItems = ExtractGoogleMapsMenuImages(doc);

                return menuItems;
            }
            catch
            {
                // Log error but return empty list
                return menuItems;
            }
        }

        // Ensure the URL points to the menu view
        private string EnsureMenuViewUrl(string originalUrl)
        {
            try
            {
                // If the URL already contains menu-specific parameters, return as is
                if (originalUrl.Contains("!3e12") || originalUrl.Contains("menu") || originalUrl.Contains("photos"))
                {
                    return originalUrl;
                }

                // Try to construct a menu view URL from a regular Google Maps URL
                return GenerateMenuViewUrl(originalUrl);
            }
            catch
            {
                // If URL parsing fails, return original URL
                return originalUrl;
            }
        }

        // Generate a menu view URL from a regular Google Maps URL
        public string GenerateMenuViewUrl(string regularGoogleMapsUrl)
        {
            try
            {
                // Parse the URL to extract components
                var uri = new Uri(regularGoogleMapsUrl);
                var query = uri.Query;
                
                // Check if it's already a menu view URL
                if (query.Contains("!3e12") || query.Contains("!10e9"))
                {
                    return regularGoogleMapsUrl;
                }
                
                // Extract the data parameter and modify it to include menu view
                if (query.Contains("data="))
                {
                    var dataStart = query.IndexOf("data=") + 5;
                    var dataEnd = query.IndexOf("&", dataStart);
                    if (dataEnd == -1) dataEnd = query.Length;
                    
                    var dataParam = query.Substring(dataStart, dataEnd - dataStart);
                    var decodedData = Uri.UnescapeDataString(dataParam);
                    
                    // Modify the data parameter to include menu view
                    // Add !10e9 parameter which is used for menu/photo view
                    if (!decodedData.Contains("!10e9"))
                    {
                        // Find the position to insert !10e9 (usually after !8m2!3d...!4d...)
                        var insertPos = decodedData.LastIndexOf("!8m2!");
                        if (insertPos > 0)
                        {
                            var beforeInsert = decodedData.Substring(0, insertPos);
                            var afterInsert = decodedData.Substring(insertPos);
                            
                            // Insert !10e9 before the !8m2! part
                            decodedData = beforeInsert + "!10e9" + afterInsert;
                        }
                        else
                        {
                            // If we can't find the right place, append it
                            decodedData += "!10e9";
                        }
                    }
                    
                    // Reconstruct the URL with the modified data parameter
                    var newQuery = query.Substring(0, dataStart) + Uri.EscapeDataString(decodedData) + query.Substring(dataEnd);
                    var newUrl = uri.Scheme + "://" + uri.Host + uri.AbsolutePath + "?" + newQuery;
                    
                    return newUrl;
                }
                
                // If no data parameter, try to add one
                var baseUrl = regularGoogleMapsUrl;
                if (baseUrl.Contains("?"))
                {
                    baseUrl += "&data=!4m11!1m2!2m1!1sRestaurants!10e9";
                }
                else
                {
                    baseUrl += "?data=!4m11!1m2!2m1!1sRestaurants!10e9";
                }
                
                return baseUrl;
            }
            catch
            {
                // If URL parsing fails, return original URL
                return regularGoogleMapsUrl;
            }
        }

        // Debug method to test URL generation
        public string DebugUrlGeneration(string regularGoogleMapsUrl)
        {
            try
            {
                var uri = new Uri(regularGoogleMapsUrl);
                var query = uri.Query;
                
                var result = new List<string>();
                result.Add($"Original URL: {regularGoogleMapsUrl}");
                result.Add($"Query: {query}");
                
                if (query.Contains("data="))
                {
                    var dataStart = query.IndexOf("data=") + 5;
                    var dataEnd = query.IndexOf("&", dataStart);
                    if (dataEnd == -1) dataEnd = query.Length;
                    
                    var dataParam = query.Substring(dataStart, dataEnd - dataStart);
                    var decodedData = Uri.UnescapeDataString(dataParam);
                    
                    result.Add($"Data parameter: {dataParam}");
                    result.Add($"Decoded data: {decodedData}");
                    result.Add($"Contains !10e9: {decodedData.Contains("!10e9")}");
                    
                    if (!decodedData.Contains("!10e9"))
                    {
                        var insertPos = decodedData.LastIndexOf("!8m2!");
                        result.Add($"Insert position: {insertPos}");
                        
                        if (insertPos > 0)
                        {
                            var beforeInsert = decodedData.Substring(0, insertPos);
                            var afterInsert = decodedData.Substring(insertPos);
                            var newData = beforeInsert + "!10e9" + afterInsert;
                            result.Add($"New data: {newData}");
                        }
                    }
                }
                
                var generatedUrl = GenerateMenuViewUrl(regularGoogleMapsUrl);
                result.Add($"Generated URL: {generatedUrl}");
                
                return string.Join("\n", result);
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        // Debug method to analyze Google Maps content
        public async Task<string> DebugGoogleMapsContent(string googleMapsUrl)
        {
            try
            {
                var options = new ScrapingBeeOptions
                {
                    RenderJs = true,
                    Wait = 5000,
                    CustomGoogle = true
                };

                var response = await ScrapeUrlAsync(googleMapsUrl, options);

                if (!response.Success || string.IsNullOrEmpty(response.Content))
                {
                    return "Failed to fetch Google Maps content";
                }

                var doc = ParseHtml(response.Content);
                var results = new List<string>();

                // Check for the main container
                var mainContainer = doc.DocumentNode.SelectSingleNode("//*[@id='QA0Szd']");
                results.Add($"Main container QA0Szd found: {mainContainer != null}");

                // Check for key class elements
                var keyClasses = new[] { "w6VYqd", "e07Vkf", "kA9KIf", "m6QErb", "DxyBCb", "dS8AEf", "XiKgde", "fp2VUc", "cRLbXd", "dryRY" };
                foreach (var className in keyClasses)
                {
                    var elements = doc.DocumentNode.SelectNodes($"//*[contains(@class, '{className}')]");
                    results.Add($"Elements with class '{className}': {elements?.Count ?? 0}");
                }

                // Check for all images
                var allImages = doc.DocumentNode.SelectNodes("//img");
                results.Add($"Total images found: {allImages?.Count ?? 0}");

                // Check for button images
                var buttonImages = doc.DocumentNode.SelectNodes("//button//img");
                results.Add($"Images in buttons: {buttonImages?.Count ?? 0}");

                // Check for Google Maps specific images
                var googleImages = doc.DocumentNode.SelectNodes("//img[contains(@src, 'googleusercontent.com') or contains(@src, 'googleapis.com')]");
                results.Add($"Google Maps images: {googleImages?.Count ?? 0}");

                // Show some sample image sources
                if (allImages != null && allImages.Count > 0)
                {
                    results.Add("\nSample image sources:");
                    for (int i = 0; i < Math.Min(5, allImages.Count); i++)
                    {
                        var src = allImages[i].GetAttributeValue("src", "no src");
                        var alt = allImages[i].GetAttributeValue("alt", "no alt");
                        var width = allImages[i].GetAttributeValue("width", "no width");
                        var height = allImages[i].GetAttributeValue("height", "no height");
                        results.Add($"  Image {i + 1}: src='{src}', alt='{alt}', size={width}x{height}");
                    }
                }

                return string.Join("\n", results);
            }
            catch (Exception ex)
            {
                return $"Debug error: {ex.Message}";
            }
        }

        private List<MenuItem> ExtractGoogleMapsMenuImages(HtmlDocument doc)
        {
            var menuItems = new List<MenuItem>();

            // Convert your CSS selectors to XPath - both the original and updated selectors
            // Original: #QA0Szd > div > div > div.w6VYqd > div:nth-child(2) > div > div.e07Vkf.kA9KIf > div > div > div.m6QErb.DxyBCb.kA9KIf.dS8AEf.XiKgde > div.fp2VUc > div.cRLbXd > div.dryRY > div:nth-child(2) > button > img
            // Updated: #QA0Szd > div > div > div.w6VYqd > div:nth-child(2) > div > div.e07Vkf.kA9KIf > div > div > div.m6QErb.DxyBCb.kA9KIf.dS8AEf.XiKgde > div.fp2VUc > div.cRLbXd > div.dryRY > div:nth-child(1) > button > img
            
            var specificSelectors = new[]
            {
                // Updated selector (nth-child(1))
                "//*[@id='QA0Szd']//div[contains(@class,'w6VYqd')]//div[contains(@class,'e07Vkf') and contains(@class,'kA9KIf')]//div[contains(@class,'m6QErb') and contains(@class,'DxyBCb') and contains(@class,'kA9KIf') and contains(@class,'dS8AEf') and contains(@class,'XiKgde')]//div[contains(@class,'fp2VUc')]//div[contains(@class,'cRLbXd')]//div[contains(@class,'dryRY')]//div[1]//button//img",
                // Original selector (nth-child(2))
                "//*[@id='QA0Szd']//div[contains(@class,'w6VYqd')]//div[contains(@class,'e07Vkf') and contains(@class,'kA9KIf')]//div[contains(@class,'m6QErb') and contains(@class,'DxyBCb') and contains(@class,'kA9KIf') and contains(@class,'dS8AEf') and contains(@class,'XiKgde')]//div[contains(@class,'fp2VUc')]//div[contains(@class,'cRLbXd')]//div[contains(@class,'dryRY')]//div[2]//button//img",
                // More flexible selectors for the same structure
                "//*[@id='QA0Szd']//div[contains(@class,'dryRY')]//button//img",
                "//*[@id='QA0Szd']//div[contains(@class,'cRLbXd')]//button//img",
                "//*[@id='QA0Szd']//div[contains(@class,'fp2VUc')]//button//img"
            };

            // Try the specific selectors first
            foreach (var selector in specificSelectors)
            {
                try
                {
                    var specificImages = doc.DocumentNode.SelectNodes(selector);
                    if (specificImages != null && specificImages.Count > 0)
                    {
                        foreach (var img in specificImages)
                        {
                            var imgUrl = ExtractImageUrl(img);
                            if (!string.IsNullOrEmpty(imgUrl) && IsValidMenuImage(imgUrl, img))
                            {
                                // Avoid duplicates
                                if (!menuItems.Any(item => item.ImageUrl == imgUrl))
                                {
                                    menuItems.Add(new MenuItem
                                    {
                                        Name = GetImageAltText(img) ?? "Google Maps Menu Image",
                                        ImageUrl = imgUrl
                                    });
                                }
                            }
                        }
                        // If we found images with this selector, continue to next selector to get more
                    }
                }
                catch
                {
                    // Skip invalid selectors
                    continue;
                }
            }

            // If no images found with specific selector, try broader selectors
            if (menuItems.Count == 0)
            {
                // Look for menu images in Google Maps structure
                var broaderSelectors = new[]
                {
                    // Look for images in menu-related containers
                    "//div[contains(@class,'m6QErb')]//img",
                    "//div[contains(@class,'DxyBCb')]//img",
                    "//div[contains(@class,'kA9KIf')]//img",
                    "//div[contains(@class,'fp2VUc')]//img",
                    "//div[contains(@class,'cRLbXd')]//img",
                    "//div[contains(@class,'dryRY')]//img",
                    
                    // Look for images in buttons (common for menu items)
                    "//button//img[contains(@src,'googleusercontent.com')]",
                    "//button//img[contains(@src,'googleapis.com')]",
                    "//button//img[contains(@src,'maps.googleapis.com')]",
                    
                    // Look for images with menu-related attributes
                    "//img[contains(@alt,'menu') or contains(@alt,'food') or contains(@alt,'dish')]",
                    "//img[contains(@src,'menu') or contains(@src,'food')]",
                    
                    // Look for images in restaurant detail sections
                    "//div[contains(@class,'restaurant')]//img",
                    "//div[contains(@class,'menu')]//img",
                    "//div[contains(@class,'food')]//img"
                };

                foreach (var selector in broaderSelectors)
                {
                    try
                    {
                        var images = doc.DocumentNode.SelectNodes(selector);
                        if (images != null)
                        {
                            foreach (var img in images)
                            {
                                var imgUrl = ExtractImageUrl(img);
                                if (!string.IsNullOrEmpty(imgUrl) && IsValidMenuImage(imgUrl, img))
                                {
                                    // Avoid duplicates
                                    if (!menuItems.Any(item => item.ImageUrl == imgUrl))
                                    {
                                        menuItems.Add(new MenuItem
                                        {
                                            Name = GetImageAltText(img) ?? "Google Maps Menu Image",
                                            ImageUrl = imgUrl
                                        });
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Skip invalid selectors
                        continue;
                    }
                }
            }

            // If still no images, try to find all images and filter by context
            if (menuItems.Count == 0)
            {
                var allImages = doc.DocumentNode.SelectNodes("//img");
                if (allImages != null)
                {
                    foreach (var img in allImages)
                    {
                        var imgUrl = ExtractImageUrl(img);
                        if (!string.IsNullOrEmpty(imgUrl) && IsValidMenuImage(imgUrl, img))
                        {
                            // Check if this image is in a menu-related context
                            var parentContext = GetParentContext(img);
                            if (IsMenuRelatedContext(parentContext))
                            {
                                if (!menuItems.Any(item => item.ImageUrl == imgUrl))
                                {
                                    menuItems.Add(new MenuItem
                                    {
                                        Name = GetImageAltText(img) ?? "Google Maps Menu Image",
                                        ImageUrl = imgUrl
                                    });
                                }
                            }
                        }
                    }
                }
            }

            return menuItems;
        }

        private string ExtractImageUrl(HtmlNode imgNode)
        {
            if (imgNode == null) return string.Empty;

            // Try different attributes for image URL
            var src = imgNode.GetAttributeValue("src", "");
            if (string.IsNullOrEmpty(src))
            {
                src = imgNode.GetAttributeValue("data-src", "");
            }
            if (string.IsNullOrEmpty(src))
            {
                src = imgNode.GetAttributeValue("data-lazy", "");
            }
            if (string.IsNullOrEmpty(src))
            {
                src = imgNode.GetAttributeValue("data-original", "");
            }

            // Convert relative URLs to absolute
            if (!string.IsNullOrEmpty(src))
            {
                if (src.StartsWith("//"))
                {
                    src = "https:" + src;
                }
                else if (src.StartsWith("/"))
                {
                    src = "https://maps.google.com" + src;
                }
            }

            return src;
        }

        private bool IsValidMenuImage(string imgUrl, HtmlNode imgNode)
        {
            if (string.IsNullOrEmpty(imgUrl)) return false;

            // Check if it's a Google Maps image or any valid image URL
            var isGoogleMapsImage = imgUrl.Contains("googleusercontent.com") || 
                                  imgUrl.Contains("googleapis.com") || 
                                  imgUrl.Contains("maps.googleapis.com") ||
                                  imgUrl.Contains("maps.google.com") ||
                                  imgUrl.Contains("google.com");

            // Also accept any image that looks like it could be a menu image
            var isLikelyImage = imgUrl.StartsWith("http") && 
                               (imgUrl.Contains(".jpg") || imgUrl.Contains(".jpeg") || 
                                imgUrl.Contains(".png") || imgUrl.Contains(".webp") ||
                                imgUrl.Contains("image") || imgUrl.Contains("photo"));

            // Check image dimensions (be more lenient - accept smaller images too)
            var width = imgNode.GetAttributeValue("width", "");
            var height = imgNode.GetAttributeValue("height", "");
            
            // Only reject very small images (less than 50px)
            if (!string.IsNullOrEmpty(width) && int.TryParse(width, out var w) && w < 50) return false;
            if (!string.IsNullOrEmpty(height) && int.TryParse(height, out var h) && h < 50) return false;

            // Check alt text for menu-related keywords (optional)
            var alt = imgNode.GetAttributeValue("alt", "").ToLower();
            var hasMenuKeywords = alt.Contains("menu") || alt.Contains("food") || alt.Contains("dish") || 
                                alt.Contains("restaurant") || alt.Contains("meal") || alt.Contains("cafe");

            // Accept if it's a Google Maps image OR if it looks like a valid image and has menu context
            return isGoogleMapsImage || (isLikelyImage && (hasMenuKeywords || !string.IsNullOrEmpty(alt)));
        }

        private string GetImageAltText(HtmlNode imgNode)
        {
            if (imgNode == null) return string.Empty;
            
            var alt = imgNode.GetAttributeValue("alt", "");
            return !string.IsNullOrEmpty(alt) ? alt : "Google Maps Menu Image";
        }

        private string GetParentContext(HtmlNode imgNode)
        {
            var context = new List<string>();
            var current = imgNode.ParentNode;
            
            // Get context from parent elements up to 5 levels
            for (int i = 0; i < 5 && current != null; i++)
            {
                if (current.NodeType == HtmlNodeType.Element)
                {
                    var className = current.GetAttributeValue("class", "");
                    var id = current.GetAttributeValue("id", "");
                    
                    if (!string.IsNullOrEmpty(className))
                        context.Add($"class:{className}");
                    if (!string.IsNullOrEmpty(id))
                        context.Add($"id:{id}");
                }
                current = current.ParentNode;
            }
            
            return string.Join(" ", context);
        }

        private bool IsMenuRelatedContext(string context)
        {
            var menuKeywords = new[] { "menu", "food", "dish", "restaurant", "meal", "dining", "cafe", "kitchen" };
            return menuKeywords.Any(keyword => context.ToLower().Contains(keyword));
        }

        // Navigate to menu tab by modifying the URL to include menu view parameters
        public string NavigateToMenuTab(string originalUrl, string pageContent)
        {
            try
            {
                // Parse the URL to extract components
                var uri = new Uri(originalUrl);
                var query = uri.Query;
                
                // Check if it's already a menu view URL
                if (query.Contains("!3e12") || query.Contains("!10e9"))
                {
                    return originalUrl;
                }
                
                // Apply specific transformations to convert overview to menu view
                var modifiedUrl = originalUrl;
                
                // Transform 4m10 to 4m11
                if (modifiedUrl.Contains("!4m10"))
                {
                    modifiedUrl = modifiedUrl.Replace("!4m10", "!4m11");
                }
                
                // Transform 3m6 to 3m7
                if (modifiedUrl.Contains("!3m6"))
                {
                    modifiedUrl = modifiedUrl.Replace("!3m6", "!3m7");
                }
                
                // Add !10e9! after !8m2! coordinates
                if (modifiedUrl.Contains("!8m2!") && !modifiedUrl.Contains("!10e9"))
                {
                    var insertPos = modifiedUrl.LastIndexOf("!8m2!");
                    if (insertPos > 0)
                    {
                        // Find the end of the coordinate sequence (!8m2!3d...!4d...)
                        var coordEnd = modifiedUrl.IndexOf("!15s", insertPos);
                        if (coordEnd > 0)
                        {
                            var beforeCoord = modifiedUrl.Substring(0, coordEnd);
                            var afterCoord = modifiedUrl.Substring(coordEnd);
                            modifiedUrl = beforeCoord + "!10e9" + afterCoord;
                        }
                    }
                }
                
                return modifiedUrl;
            }
            catch
            {
                // If URL parsing fails, return original URL
                return originalUrl;
            }
        }

        // Extract menu images using specific Google Maps selector
        public async Task<List<MenuItem>> ExtractMenuFromGoogleMapsSelector(string url)
        {
            try
            {
                var options = new ScrapingBeeOptions
                {
                    RenderJs = true,
                    Wait = 5000, // Wait longer for Google Maps to load
                    CustomGoogle = true
                };

                var response = await ScrapeUrlAsync(url, options);
                
                if (!response.Success)
                {
                    throw new Exception($"Failed to scrape URL: {response.StatusCode} - {response.Content}");
                }

                var doc = ParseHtml(response.Content);
                var menuItems = new List<MenuItem>();

                // Use the specific selector targeting K4UgGe class with Photo aria-label
                var selector = ".K4UgGe[aria-label*='Photo'] img";
                
                var images = doc.DocumentNode.SelectNodes(selector);
                
                Console.WriteLine($"Found {images?.Count ?? 0} images with selector: {selector}");
                
                if (images != null)
                {
                    foreach (var img in images)
                    {
                        var src = img.GetAttributeValue("src", "");
                        var alt = img.GetAttributeValue("alt", "Menu item");
                        var ariaLabel = img.ParentNode?.GetAttributeValue("aria-label", "");
                        
                        Console.WriteLine($"Image found - src: {src}, alt: {alt}, aria-label: {ariaLabel}");
                        
                        if (!string.IsNullOrEmpty(src))
                        {
                            menuItems.Add(new MenuItem
                            {
                                Name = alt,
                                ImageUrl = src,
                                Description = $"Menu item from Google Maps - {ariaLabel}"
                            });
                        }
                    }
                }

                // Also try broader selectors as fallback
                if (menuItems.Count == 0)
                {
                    var fallbackSelectors = new[]
                    {
                       
                        ".K4UgGe[aria-label*='Photo 1 of'] img", // Specific Photo 1 pattern
                        ".K4UgGe[aria-label*='Photo 2 of'] img", // Photo 2 pattern
                        ".K4UgGe[aria-label*='Photo 3 of'] img", // Photo 3 pattern
                        "img[src*='googleusercontent.com']",
                        "img[alt*='menu']",
                        "img[alt*='food']",
                        ".menu img",
                        "[data-value='Menu'] img"
                    };

                    foreach (var fallbackSelector in fallbackSelectors)
                    {
                        var fallbackImages = doc.DocumentNode.SelectNodes(fallbackSelector);
                        if (fallbackImages != null)
                        {
                            foreach (var img in fallbackImages)
                            {
                                var src = img.GetAttributeValue("src", "");
                                var alt = img.GetAttributeValue("alt", "Menu item");
                                
                                if (!string.IsNullOrEmpty(src) && !menuItems.Any(m => m.ImageUrl == src))
                                {
                                    menuItems.Add(new MenuItem
                                    {
                                        Name = alt,
                                        ImageUrl = src,
                                        Description = "Menu item from Google Maps"
                                    });
                                }
                            }
                        }
                    }
                }

                return menuItems;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting menu from Google Maps selector: {ex.Message}");
                return new List<MenuItem>();
            }
        }

        // Convenience method that combines URL conversion and extraction
        public async Task<RestaurantData> ExtractRestaurantDataWithMenuConversion(string url, ScrapingBeeOptions? options = null)
        {
            options ??= new ScrapingBeeOptions();
            
            // Check if it's a Google Maps URL and convert to menu URL
            var isGoogleMaps = url.Contains("google.com/maps") || url.Contains("maps.google.com");
            var finalUrl = url;
            
            if (isGoogleMaps)
            {
                finalUrl = NavigateToMenuTab(url, "");
            }
            
            // Set appropriate options for Google Maps
            if (isGoogleMaps)
            {
                options.CustomGoogle = true;
                options.Wait = Math.Max(options.Wait, 3000); // Ensure sufficient wait time
            }
            
            // Scrape the URL
            var response = await ScrapeUrlAsync(finalUrl, options);
            
            if (!response.Success)
            {
                throw new Exception($"Failed to scrape URL: {response.StatusCode} - {response.Content}");
            }
            
            // Extract restaurant data
            var restaurantData = ExtractRestaurantData(response.Content);
            
            // If it's Google Maps, try to get additional menu images
            if (isGoogleMaps)
            {
                try
                {
                    var menuImages = await ScrapeGoogleMapsMenuImagesAsync(finalUrl);
                    if (menuImages.Count > 0)
                    {
                        restaurantData.MenuItems.AddRange(menuImages);
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail the entire operation
                    Console.WriteLine($"Warning: Failed to extract additional menu images: {ex.Message}");
                }
            }
            
            return restaurantData;
        }
    }

    public class ScrapingBeeOptions
    {
        public bool RenderJs { get; set; } = false;
        public bool PremiumProxy { get; set; } = false;
        public string? CountryCode { get; set; }
        public bool CustomGoogle { get; set; } = false;
        public string? SessionId { get; set; }
        public int Wait { get; set; } = 0;
    }

    public class ScrapingBeeResponse
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Content { get; set; } = "";
        public Dictionary<string, string> Headers { get; set; } = new();
    }

    public class RestaurantData
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal? Rating { get; set; }
        public string PhoneNumber { get; set; } = "";
        public string Address { get; set; } = "";
        public List<MenuItem> MenuItems { get; set; } = new();
    }

    public class MenuItem
    {
        public string Name { get; set; } = "";
        public decimal? Price { get; set; }
        public string Description { get; set; } = "";
        public string ImageUrl { get; set; } = "";
    }
}
