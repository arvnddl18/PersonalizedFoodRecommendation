# FoodieSaur Design Reference Guide 🎨

## Restaurant Card Structure

```
┌─────────────────────────────────────────────────┐
│  ╔═══════════════════════════════════════════╗  │
│  ║                                           ║  │
│  ║        FOOD IMAGE CAROUSEL                ║  │
│  ║        (200px height)                     ║  │  ← Large, appetizing photos
│  ║                                           ║  │
│  ║        ● ● ● ●  (carousel indicators)    ║  │
│  ╚═══════════════════════════════════════════╝  │
│                                                 │
│  Restaurant Name (Bold, Brand Color)            │  ← Clear hierarchy
│  ─────────────────────────────────────────────  │
│  ₱₱ • Chinese • American • Deshi food           │  ← Context
│                                                 │
│  4.3 ★  200+ Ratings                           │  ← Social proof
│                                                 │
│  ⏰ 25 Min  •  📍 1.2 km                        │  ← Practical info
│  ─────────────────────────────────────────────  │
│  ┌─────────┐ ┌─────────┐ ┌──────────┐         │
│  │ 🤍 Save │ │ 🔗 Share│ │ 🗺️ Go    │         │  ← Clear CTAs
│  └─────────┘ └─────────┘ └──────────┘         │
└─────────────────────────────────────────────────┘
```

---

## Color Coding by Panel

### Search Results Panel (Green Theme)
```css
Header Background:    linear-gradient(135deg, #47682C 95%, #47682C 85%)
Restaurant Name:      #47682C (Brand Green)
Card Shadow:          rgba(71,104,44,0.08)
Hover Shadow:         rgba(71,104,44,0.15)
Active Indicator:     #FFFFFF (White)
Save Button:          #47682C background when saved
```

### Favorites Panel (Pink Theme)
```css
Header Background:    linear-gradient(135deg, #FE395C 95%, #FE395C 85%)
Restaurant Name:      #FE395C (Brand Pink)
Card Shadow:          rgba(254,57,92,0.1)
Hover Shadow:         rgba(254,57,92,0.2)
Active Indicator:     #FE395C (Brand Pink)
Badge:                #FE395C with ❤️ Favorite
```

### Shared Elements
```css
Star Rating:          #F7B32B (Gold)
Share Button:         #196EF3 (Blue)
Directions Button:    linear-gradient(#F7B32B, #F5A623) (Gold)
Price Level:          #47682C (Green)
Cuisine Types:        #6b7280 (Gray)
Distance/Time:        #6b7280 (Gray)
```

---

## Component Hierarchy

### 1. Panel Header
```
┌──────────────────────────────────────────┐
│ 🔍 Search Results                [Badge] │  ← Glassmorphism
└──────────────────────────────────────────┘
   Gradient • Blur • Shadow • Icons
```

### 2. Card Image Section
```
┌──────────────────────────────────────────┐
│                                          │
│         🍔 FOOD PHOTO                    │  ← Hero Image
│                                          │
│              ● ○ ○ ○                     │  ← Indicators
└──────────────────────────────────────────┘
   Auto-slide • Click to navigate • Smooth transitions
```

### 3. Card Content Section
```
Restaurant Name             ← h3, bold, 1.125rem
────────────────────────
₱₱ • Type • Type • Type    ← Metadata
4.3 ★ Reviews              ← Social proof
⏰ Time • 📍 Distance       ← Practical info
────────────────────────
[Button] [Button] [Button] ← Actions
```

---

## Animation Timings

| Element | Duration | Easing |
|---------|----------|--------|
| Card Hover | 300ms | cubic-bezier(0.4,0,0.2,1) |
| Button Hover | 200ms | ease |
| Carousel Slide | 400ms | ease-in-out |
| Indicator Change | 300ms | ease |
| Auto-slide Interval | 3000ms | - |
| Toast Fade | 300ms | ease-out |

---

## Spacing System

```
Card Padding:           1rem (16px)
Button Padding:         0.625rem (10px)
Gap between buttons:    0.5rem (8px)
Card margin-bottom:     1rem (16px)
Section spacing:        0.625rem to 0.875rem
Border radius (card):   1rem (16px)
Border radius (button): 0.5rem (8px)
```

---

## Icon Usage

### SVG Icons (from Heroicons)
- 🔍 Search (panel header)
- 🔗 Share (button)
- 🗺️ Directions (button)
- ⏰ Clock (time indicator)
- 📍 Location (distance indicator)
- ✅ Clipboard success (notification)

### Emoji Icons
- 🤍 Empty heart (not saved)
- ❤️ Filled heart (saved)
- 💔 Broken heart (remove favorite)
- ⭐ Star (rating)

---

## Button States

### Save Button
```
Not Saved:
  Background: rgba(71,104,44,0.05)
  Border: rgba(71,104,44,0.1)
  Text: #47682C
  Icon: 🤍

Saved:
  Background: linear-gradient(rgba(254,57,92,0.1), rgba(254,57,92,0.05))
  Border: rgba(254,57,92,0.2)
  Text: #FE395C
  Icon: ❤️

Hover:
  Transform: translateY(-2px)
  Shadow: 0 4px 8px rgba(71,104,44,0.15)
```

### Share Button
```
Default:
  Background: rgba(25,110,243,0.05)
  Border: rgba(25,110,243,0.1)
  Text: #196EF3

Hover:
  Background: rgba(25,110,243,0.1)
  Shadow: 0 4px 8px rgba(25,110,243,0.15)
  Transform: translateY(-2px)
```

### Directions Button
```
Default:
  Background: linear-gradient(135deg, #F7B32B 0%, #F5A623 100%)
  Border: none
  Text: #FFFFFF
  Shadow: 0 2px 4px rgba(247,179,43,0.2)

Hover:
  Shadow: 0 4px 12px rgba(247,179,43,0.35)
  Transform: translateY(-2px)
```

---

## Responsive Breakpoints

```css
/* Desktop (default) */
Panel width: 26rem (416px)

/* Tablet & Mobile */
Panel width: 90vw (max-width)

/* Touch devices */
Button min-height: 44px (for touch targets)
Increased padding for easier tapping
```

---

## Accessibility Features

✅ **ARIA Labels**: All buttons have descriptive labels  
✅ **Color Contrast**: WCAG AA compliant  
✅ **Focus States**: Visible keyboard navigation  
✅ **Touch Targets**: Minimum 44px for mobile  
✅ **Alt Text**: All images have descriptive alt text  
✅ **Semantic HTML**: Proper heading hierarchy  

---

## Brand Voice in UI

The design embodies FoodieSaur's personality:

🦖 **Friendly**: Rounded corners, soft shadows, emoji use  
🎯 **Intelligent**: Clear hierarchy, organized information  
🗺️ **Explorer-Oriented**: Prominent location/distance info  
🍔 **Food-Focused**: Large, appetizing food images  
✨ **Modern**: Glassmorphism, smooth animations  
❤️ **Caring**: Easy favorites management, sharing  

---

## Quick Reference: Brand Colors

```css
/* Primary Palette */
--brand-green:     #47682C;
--brand-gold:      #F7B32B;
--brand-blue:      #196EF3;
--brand-cream:     #FFF5E1;
--brand-pink:      #FE395C;

/* Neutral Palette */
--gray-50:         #f9fafb;
--gray-100:        #f3f4f6;
--gray-200:        #e5e7eb;
--gray-300:        #d1d5db;
--gray-500:        #6b7280;
--gray-700:        #374151;
--gray-900:        #111827;

/* Semantic Colors */
--text-primary:    #47682C;
--text-secondary:  #6b7280;
--text-tertiary:   #d1d5db;
--bg-primary:      #ffffff;
--bg-secondary:    #f9fafb;
```

---

## Implementation Notes

### Carousel Best Practices
- Load only visible images first
- Preload next/prev images
- Pause on manual interaction
- Resume auto-slide after 3s

### Performance
- Use CSS transforms (not margin/position)
- Debounce auto-slide intervals
- Clean up event listeners
- Optimize image sizes

### Error Handling
- Fallback images for missing photos
- Default values for missing data
- Graceful API failure handling
- User-friendly error messages

---

**Design System Version**: 1.0  
**Last Updated**: October 16, 2025  
**Status**: Production Ready ✅

