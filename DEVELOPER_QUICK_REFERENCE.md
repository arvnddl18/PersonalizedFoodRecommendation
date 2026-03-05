# FoodieSaur Developer Quick Reference 🛠️

## Key JavaScript Functions

### 1. Share Restaurant Function
```javascript
async function shareRestaurant(placeId, placeName) {
    // Get place details
    const service = new google.maps.places.PlacesService(map);
    service.getDetails(request, async (place, status) => {
        if (status === OK) {
            const shareData = {
                title: `Check out ${place.name} on FoodieSaur! 🦖`,
                text: `I found this amazing restaurant...`,
                url: place.url
            };
            
            // Try Web Share API, fallback to clipboard
            if (navigator.share) {
                await navigator.share(shareData);
            } else {
                fallbackShare(shareData);
            }
        }
    });
}
```

### 2. Carousel Auto-Slide
```javascript
// In card creation
if (imageUrls.length > 1) {
    const carousel = resultDiv.querySelector('.image-carousel');
    let currentIndex = 0;
    
    const updateCarousel = (index) => {
        carousel.style.transform = `translateX(-${index * 100}%)`;
        // Update indicators
    };
    
    const startAutoSlide = () => {
        setInterval(() => {
            currentIndex = (currentIndex + 1) % imageUrls.length;
            updateCarousel(currentIndex);
        }, 3000);
    };
    
    resultDiv.addEventListener('mouseenter', startAutoSlide);
}
```

### 3. Enhanced Card HTML Structure
```javascript
const card = `
    <div style="position: relative; width: 100%; height: 200px;">
        <div class="image-carousel" style="display: flex; transition: transform 0.4s ease-in-out;">
            ${imageUrls.map(url => `<img src="${url}" ... />`).join('')}
        </div>
        <div style="position: absolute; bottom: 0.75rem; left: 50%;">
            ${imageUrls.map((_, idx) => `
                <div class="carousel-indicator" data-img-index="${idx}" 
                     style="background: ${idx === 0 ? '#FFF' : 'rgba(255,255,255,0.5)'};">
                </div>
            `).join('')}
        </div>
    </div>
`;
```

---

## CSS Patterns

### Glassmorphism Effect
```css
background: linear-gradient(135deg, rgba(71,104,44,0.95) 0%, rgba(71,104,44,0.85) 100%);
backdrop-filter: blur(8px);
-webkit-backdrop-filter: blur(8px);
border: 1px solid rgba(255,255,255,0.2);
box-shadow: 0 2px 8px rgba(0,0,0,0.1);
```

### Card Hover Effect
```css
/* Base state */
box-shadow: 0 2px 12px rgba(71,104,44,0.08);
transform: translateY(0);
transition: all 0.3s cubic-bezier(0.4,0,0.2,1);

/* Hover state */
box-shadow: 0 8px 24px rgba(71,104,44,0.15), 0 4px 12px rgba(0,0,0,0.08);
transform: translateY(-4px);
```

### Button Gradient (Directions)
```css
background: linear-gradient(135deg, #F7B32B 0%, #F5A623 100%);
border: none;
border-radius: 0.5rem;
color: #FFFFFF;
box-shadow: 0 2px 4px rgba(247,179,43,0.2);
transition: all 0.2s;
```

---

## Common Patterns

### Extract Cuisine Types from Google Places
```javascript
const cuisineTypes = place.types 
    ? place.types
        .filter(type => !['point_of_interest', 'establishment', 'food', 'store'].includes(type))
        .map(type => type.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase()))
        .slice(0, 3)
    : ['Restaurant'];
```

### Price Level Display
```javascript
const priceLevel = place.price_level ? '₱'.repeat(place.price_level) : '₱₱';
```

### Get Multiple Photos
```javascript
let imageUrls = [];
if (place.photos && place.photos.length > 0) {
    const photosToShow = place.photos.slice(0, 5);
    imageUrls = photosToShow.map(photo => photo.getUrl({
        maxWidth: 500, 
        maxHeight: 300,
        quality: 'high'
    }));
} else {
    imageUrls = ['/images/restaurant-placeholder.svg'];
}
```

---

## HTML Template Patterns

### Action Buttons Row
```html
<div style="display: flex; gap: 0.5rem; padding-top: 0.75rem;">
    <!-- Favorite Button -->
    <button class="favorite-heart-btn action-btn" 
            onclick="event.stopPropagation(); toggleFavorite(...)">
        <span>🤍</span>
        Save
    </button>
    
    <!-- Share Button -->
    <button class="share-btn action-btn" 
            onclick="event.stopPropagation(); shareRestaurant(...)">
        <svg>...</svg>
        Share
    </button>
    
    <!-- Directions Button -->
    <button class="directions-btn action-btn"
            onclick="event.stopPropagation(); getDirections(...)">
        <svg>...</svg>
        Go
    </button>
</div>
```

### Info Row with Icons
```html
<div style="display: flex; align-items: center; gap: 1rem;">
    <div style="display: flex; align-items: center; gap: 0.375rem;">
        <svg style="width: 1rem; height: 1rem; color: #6b7280;">...</svg>
        <span style="font-size: 0.875rem;">25 Min</span>
    </div>
    <span style="color: #d1d5db;">•</span>
    <div style="display: flex; align-items: center; gap: 0.375rem;">
        <svg style="width: 1rem; height: 1rem; color: #47682C;">...</svg>
        <span style="font-size: 0.875rem;">1.2 km</span>
    </div>
</div>
```

---

## Event Listeners Pattern

### Button Hover Effects
```javascript
resultDiv.querySelectorAll('.action-btn').forEach(btn => {
    btn.addEventListener('mouseenter', function() {
        this.style.transform = 'translateY(-2px)';
        
        if (this.classList.contains('favorite-heart-btn')) {
            this.style.boxShadow = '0 4px 8px rgba(71,104,44,0.15)';
        } else if (this.classList.contains('share-btn')) {
            this.style.background = 'rgba(25,110,243,0.1)';
            this.style.boxShadow = '0 4px 8px rgba(25,110,243,0.15)';
        } else if (this.classList.contains('directions-btn')) {
            this.style.boxShadow = '0 4px 12px rgba(247,179,43,0.35)';
        }
    });
    
    btn.addEventListener('mouseleave', function() {
        this.style.transform = 'translateY(0)';
        // Reset styles...
    });
});
```

### Carousel Indicator Clicks
```javascript
indicators.forEach((indicator, idx) => {
    indicator.addEventListener('click', (e) => {
        e.stopPropagation();
        stopAutoSlide();
        updateCarousel(idx);
        startAutoSlide();
    });
});
```

---

## Google Places API Integration

### Get Place Details for Card
```javascript
const service = new google.maps.places.PlacesService(map);
const request = {
    placeId: placeId,
    fields: ['photos', 'types', 'price_level', 'rating', 'user_ratings_total']
};

service.getDetails(request, (place, status) => {
    if (status === google.maps.places.PlacesServiceStatus.OK) {
        // Build card with place data
    }
});
```

### Calculate Driving Distance
```javascript
async function calculateDrivingDistance(destination) {
    return new Promise((resolve, reject) => {
        const request = {
            origin: userLocation,
            destination: destination,
            travelMode: google.maps.TravelMode.DRIVING,
            unitSystem: google.maps.UnitSystem.METRIC
        };

        directionsService.route(request, (result, status) => {
            if (status === 'OK') {
                const leg = result.routes[0].legs[0];
                resolve({
                    distance: leg.distance.value,
                    duration: leg.duration.value,
                    distanceText: leg.distance.text,
                    durationText: leg.duration.text
                });
            } else {
                reject(new Error(`Route calculation failed: ${status}`));
            }
        });
    });
}
```

---

## Panel Toggle Functions

### Open Search Panel
```javascript
function openSearch() {
    searchPanel.style.transform = 'translateX(0)';
    toggleSearchBtn.style.left = '26rem';
    toggleSearchBtn.setAttribute('aria-expanded', 'true');
    toggleSearchBtn.querySelector('span').textContent = '‹';
}
```

### Open Favorites Panel
```javascript
function openFavorites() {
    searchPanel.style.transform = 'translateX(-100%)';
    favoritesPanel.style.transform = 'translateX(0)';
    toggleSearchBtn.style.left = '26rem';
    
    displayFavorites();
}
```

---

## Notification Toast Pattern

### Show Copy Success Notification
```javascript
const notification = document.createElement('div');
notification.style.cssText = `
    position: fixed;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    background: linear-gradient(135deg, #47682C 0%, #5a7e36 100%);
    color: white;
    padding: 1.25rem 2rem;
    border-radius: 1rem;
    box-shadow: 0 8px 24px rgba(0,0,0,0.2);
    z-index: 10000;
`;
notification.innerHTML = `
    <svg>...</svg>
    Copied to clipboard!
`;
document.body.appendChild(notification);

setTimeout(() => {
    notification.style.opacity = '0';
    notification.style.transition = 'all 0.3s ease-out';
    setTimeout(() => notification.remove(), 300);
}, 2000);
```

---

## Error Handling Patterns

### Graceful Fallback for Missing Data
```javascript
// Restaurant Name
const name = place.name || 'Restaurant Name Unavailable';

// Rating
const rating = place.rating ? place.rating.toFixed(1) : '4.0';

// Photos
const imageUrls = place.photos?.length > 0 
    ? place.photos.map(photo => photo.getUrl(...))
    : ['/images/restaurant-placeholder.svg'];

// Price Level
const priceLevel = place.price_level ? '₱'.repeat(place.price_level) : '₱₱';

// Distance
const distance = place.distanceText || 'Distance unavailable';
```

### API Call Error Handling
```javascript
try {
    const response = await fetch('/api/Favorites/add', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(favoriteData)
    });
    
    if (response.ok) {
        // Success handling
        addMessage('FoodieSaur Bot', 'Added to favorites! ❤️');
    } else {
        console.error('Failed to add favorite');
        addMessage('FoodieSaur Bot', 'Sorry, couldn\'t add to favorites.');
    }
} catch (error) {
    console.error('Error adding favorite:', error);
    addMessage('FoodieSaur Bot', 'Oops! Something went wrong.');
}
```

---

## Brand Color Variables (for quick reference)

```javascript
const brandColors = {
    green: '#47682C',
    gold: '#F7B32B',
    blue: '#196EF3',
    cream: '#FFF5E1',
    pink: '#FE395C',
    
    // Grays
    gray50: '#f9fafb',
    gray100: '#f3f4f6',
    gray200: '#e5e7eb',
    gray300: '#d1d5db',
    gray500: '#6b7280',
    gray700: '#374151',
    gray900: '#111827'
};
```

---

## SVG Icon Library

### Search Icon
```html
<svg style="width:1.5rem; height:1.5rem; color:#F7B32B;" fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
          d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path>
</svg>
```

### Share Icon
```html
<svg style="width:1rem; height:1rem;" fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
          d="M8.684 13.342C8.886 12.938 9 12.482 9 12c0-.482-.114-.938-.316-1.342m0 2.684a3 3 0 110-2.684m0 2.684l6.632 3.316m-6.632-6l6.632-3.316m0 0a3 3 0 105.367-2.684 3 3 0 00-5.367 2.684zm0 9.316a3 3 0 105.368 2.684 3 3 0 00-5.368-2.684z"></path>
</svg>
```

### Directions Icon
```html
<svg style="width:1rem; height:1rem;" fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
          d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7"></path>
</svg>
```

### Clock Icon
```html
<svg style="width:1rem; height:1rem; color:#6b7280;" fill="none" stroke="currentColor" viewBox="0 0 24 24">
    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
          d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path>
</svg>
```

### Location Icon
```html
<svg style="width:1rem; height:1rem; color:#47682C;" fill="currentColor" viewBox="0 0 20 20">
    <path fill-rule="evenodd" 
          d="M5.05 4.05a7 7 0 119.9 9.9L10 18.9l-4.95-4.95a7 7 0 010-9.9zM10 11a2 2 0 100-4 2 2 0 000 4z" 
          clip-rule="evenodd"></path>
</svg>
```

---

## Testing Checklist

### Visual Testing
- [ ] Cards display correctly on desktop
- [ ] Cards display correctly on mobile
- [ ] Carousel auto-slides on hover
- [ ] Indicators update correctly
- [ ] Hover effects work smoothly
- [ ] Buttons change on hover
- [ ] Panel headers show glassmorphism

### Functional Testing
- [ ] Save/unsave works correctly
- [ ] Share opens native dialog or copies to clipboard
- [ ] Directions opens Google Maps directions
- [ ] Carousel indicators are clickable
- [ ] Auto-slide pauses on manual click
- [ ] Distance calculation works
- [ ] Photos load or show placeholder

### Edge Cases
- [ ] Missing photos (shows placeholder)
- [ ] Missing rating (shows default)
- [ ] Missing price level (shows ₱₱)
- [ ] No cuisine types (shows "Restaurant")
- [ ] API failure (shows error message)
- [ ] Network timeout (handles gracefully)

---

## Performance Tips

1. **Lazy Load Images**: Only load visible images
2. **Debounce Auto-slide**: Clear intervals properly
3. **Event Delegation**: Use event delegation where possible
4. **Cleanup Listeners**: Remove listeners when destroying cards
5. **Optimize Images**: Use appropriate sizes (500x300)
6. **Batch DOM Updates**: Update multiple styles at once
7. **Use Transform**: Prefer transform over position changes

---

**Last Updated**: October 16, 2025  
**Maintainer**: FoodieSaur Development Team  
**Status**: Production Ready ✅

