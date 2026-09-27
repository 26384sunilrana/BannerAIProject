# Banner Creator User Guide - BannerAI Project
**Complete End-to-End Guide for Designing, Creating, and Publishing Digital Banners**

---

## Table of Contents
1. [Getting Started as Banner Creator](#getting-started-as-banner-creator)
2. [Understanding the Editor Interface](#understanding-the-editor-interface)
3. [Creating Your First Banner](#creating-your-first-banner)
4. [Advanced Design Techniques](#advanced-design-techniques)
5. [Banner Components & Customization](#banner-components--customization)
6. [Publishing & Scheduling](#publishing--scheduling)
7. [Version Control & Rollback](#version-control--rollback)
8. [Performance Optimization](#performance-optimization)
9. [Common Scenarios & Workflows](#common-scenarios--workflows)
10. [Design Best Practices](#design-best-practices)
11. [Troubleshooting](#troubleshooting)

---

## Getting Started as Banner Creator

### 1. What is a Banner?

A **banner** is a visual advertisement that appears on your shop's website. Banners can include:
- Images and graphics
- Text with custom fonts
- Colors and animations
- Call-to-action buttons
- Links to specific pages

### 2. Accessing the Banner Editor

**Step 1: Log In**
- Go to: `https://bannerai.com`
- Log in with your credentials

**Step 2: Navigate to Editor**
- Click on a **Shop**
- Click **"Create New Banner"** button
- Or go to: `Dashboard` → `Banners` → `New Banner`

**Step 3: You're Now in the Editor** 🎨
- You'll see the design canvas on the left
- Controls and toolbox on the right
- Properties panel at the bottom

---

## Understanding the Editor Interface

### Main Areas of the Editor

```
┌─────────────────────────────────────────────────────────┐
│          BannerAI Editor - [Banner Name]                 │
├────────┬──────────────────────┬──────────────────────────┤
│ Layers │                      │   Properties Panel       │
│ Panel  │   DESIGN CANVAS      │   (Colors, Fonts, etc)  │
│        │   (Drag & Drop       │                         │
│        │    Design Here)      │   Component Settings    │
│        │                      │                         │
│        │                      │   Layout Options        │
├────────┼──────────────────────┼──────────────────────────┤
│ Toolbox (Bottom) - Add Images, Text, Shapes, Buttons    │
└────────┴──────────────────────┴──────────────────────────┘
```

### Left Panel: Layers & Components

**What it shows**:
- Hierarchical list of all elements in your banner
- Nesting structure (text inside boxes, etc.)
- Quick rename, delete, duplicate
- Visibility toggle (eye icon)

**Example**:
```
🎨 Banner - "Summer Sale 2024"
  ├─ 📦 Background
  │   └─ 🖼️ Image: beach-scene.jpg
  ├─ 📝 Text: "SUMMER SALE"
  ├─ 📝 Text: "50% OFF"
  ├─ 🔘 Button: "Shop Now"
  │   └─ 🔗 Link: https://myshop.com/sale
  └─ 🎬 Animation: Fade In
```

### Center Canvas: Design Area

**This is where you**:
- Create your design visually
- Drag and drop elements
- Resize and position items
- See real-time preview
- Adjust spacing and alignment

**Helpful Tools**:
- **Grid Snapping**: Elements snap to grid for alignment
- **Guides**: Blue lines show alignment
- **Zoom**: Ctrl+Mouse wheel to zoom in/out
- **Pan**: Hold Space and drag to move canvas
- **Undo/Redo**: Ctrl+Z / Ctrl+Shift+Z

### Right Panel: Properties & Settings

**What you can adjust**:
- **Size & Position**: Width, height, X/Y coordinates
- **Colors**: Fill, stroke, shadows
- **Typography**: Font, size, weight, line height
- **Spacing**: Padding, margin, gap
- **Effects**: Opacity, blur, rotate, scale
- **Advanced**: Z-index, blend mode, clipping

### Bottom Toolbox: Components & Assets

**Available Components**:
- 📝 **Text**: Add text blocks
- 🖼️ **Image**: Insert photos/graphics
- 🔲 **Shape**: Rectangles, circles, lines
- 🔘 **Button**: Interactive call-to-action
- 🎬 **Animation**: Add motion effects
- 🎨 **Color Picker**: Custom colors
- 📐 **Alignment**: Distribute elements evenly

---

## Creating Your First Banner

### Step-by-Step: "Welcome to Our Store" Banner

**Goal**: Create a simple, attractive welcome banner for your shop.

**Step 1: Set Up Canvas** (1 minute)
1. Click **"Banner Settings"** (gear icon)
2. Set dimensions:
   - Width: `1200px` (standard desktop width)
   - Height: `300px` (common banner height)
   - Format: `Landscape`
3. Set background:
   - Color: Light blue (`#E3F2FD`)
   - Or upload image: `welcome-bg.jpg`
4. Click **"Apply Settings"**

**Step 2: Add Background Image** (2 minutes)
1. Click 🖼️ **Image** button in toolbox
2. Upload or select: `store-front.jpg`
3. Drag and drop on canvas
4. Resize to fill entire banner:
   - Right panel: Width = 1200, Height = 300
5. Right-click on image → **"Send to Back"** (so text appears on top)
6. Set opacity to 40% (so text is readable):
   - Right panel → Opacity: `40%`

**Step 3: Add Main Heading Text** (2 minutes)
1. Click 📝 **Text** button
2. Click on canvas to place text
3. Type: `"Welcome to Our Store"`
4. Customize:
   - Font: `Arial Bold`
   - Size: `48px`
   - Color: `White`
   - Alignment: `Center`
5. Position:
   - Center horizontally on canvas
   - Place in upper half
   - Use right panel: X = 300, Y = 50

**Step 4: Add Subheading Text** (1 minute)
1. Click 📝 **Text** button again
2. Type: `"Shop quality products at great prices"`
3. Customize:
   - Font: `Arial Regular`
   - Size: `24px`
   - Color: `White`
   - Alignment: `Center`
4. Position below main heading
   - X = 300, Y = 130

**Step 5: Add Call-to-Action Button** (2 minutes)
1. Click 🔘 **Button** button
2. Click on canvas to place button
3. Customize button:
   - Text: `"Shop Now"`
   - Background Color: `#FF6B35` (orange)
   - Text Color: `White`
   - Size: `Width = 150px, Height = 50px`
4. Add button link:
   - Right panel → Link: `https://myshop.com`
   - Target: `New Window` (opens in new tab)
5. Position:
   - Center horizontally
   - Below subheading
   - X = 525, Y = 220

**Step 6: Add Animation** (1 minute)
1. Click on button to select it
2. Click 🎬 **Animation** in toolbox
3. Select: `"Bounce"`
4. Duration: `1 second`
5. Repeat: `Infinite`
6. This makes button bounce to attract attention!

**Step 7: Preview & Save** (1 minute)
1. Click **"Preview"** button (top right)
2. See your banner in full-screen mode
3. Looks good? Click **"Save"**
4. Name your banner: `"Store Welcome Banner 2024"`
5. Click **"Save Banner"**

**Result**: Your first banner is created! ✅

---

## Advanced Design Techniques

### Technique 1: Creating a Gradient Background

**Situation**: You want a professional gradient from blue to purple.

**Steps**:
1. Select the background shape/image
2. Right panel → **"Fill"** section
3. Click **"Color Picker"**
4. Select **"Gradient"** tab
5. Choose:
   - Start Color: `#1E88E5` (blue)
   - End Color: `#6A1B9A` (purple)
   - Angle: `45°`
6. Click **"Apply"**

**Result**: Smooth gradient background ready!

---

### Technique 2: Using Layers for Complex Layouts

**Situation**: You have multiple overlapping elements and need to organize them.

**Steps**:
1. Left panel shows your layers
2. Elements that should appear on top go to the bottom of list
3. Reorder by dragging layers in left panel
4. Example organization:
   ```
   Layer 1 (Top): Text - "Call to Action"
   Layer 2: Button - "Shop Now"
   Layer 3: Image - "Product Photo"
   Layer 4: Background - Base color
   ```

**Tip**: Name each layer clearly:
- Click layer → Rename: `"Button - Shop Now"`
- Later when editing, you'll find it easily

---

### Technique 3: Responsive Design

**Situation**: Your banner should look good on mobile AND desktop.

**Steps**:
1. Design for desktop first (1200 x 300px)
2. Click **"Responsive Design"** option
3. Enable: `"Auto-scale for mobile"`
4. Set breakpoints:
   - Desktop: 1200px
   - Tablet: 768px
   - Mobile: 360px
5. For each breakpoint, adjust:
   - Font sizes (smaller on mobile)
   - Image sizes (scale down)
   - Button sizes (larger tap targets on mobile)
6. Preview on mobile:
   - Click **"Preview on Mobile"**
   - Rotate screen to test
   - Should look good on all sizes!

---

### Technique 4: Using Masks and Clipping

**Situation**: You want a circular profile image, not a square.

**Steps**:
1. Create a circle shape (use **Shape** tool)
2. Add image inside
3. Select image
4. Right panel → **"Clip to Shape"**
5. Select the circle shape
6. Click **"Apply"**
7. Image now appears as a circle! ✅

---

### Technique 5: Creating Animations

**Available Animations**:
- `Fade In`: Opacity goes from 0 to 100%
- `Slide In`: Element moves into view
- `Bounce`: Elastic bounce effect
- `Scale`: Element grows/shrinks
- `Rotate`: Element spins
- `Pulse`: Breathing effect
- `Flash`: Quick appear/disappear

**How to Add**:
1. Select element
2. Click 🎬 **Animation** button
3. Choose animation type
4. Set timing:
   - Duration: 0.5s - 3s
   - Delay: When to start
   - Repeat: Once / N times / Infinite
5. Click **"Apply"**

**Pro Tips**:
- Stagger animations for visual interest
- Keep animations < 3 seconds (not annoying)
- Animate CTAs to draw attention
- Don't animate everything (looks chaotic)

---

## Banner Components & Customization

### Component 1: Text

**Best Practices**:
- **Heading**: Large, bold, high contrast
- **Subheading**: Medium size, readable
- **Body**: Smallest size, high contrast with background
- **Line Height**: 1.5x for readability
- **Max Width**: 800px (prevents long lines)

**Text Styling**:
```
Font Size:      24px - 72px (larger for headers)
Font Weight:    Bold for headers, Regular for body
Color:          High contrast (white on dark, dark on light)
Font Family:    Sans-serif (Arial, Helvetica) for web
Line Height:    1.4 - 1.6
Letter Spacing: 0.5 - 1.5px for headings
```

**Common Font Combinations**:
- Headers: `Montserrat Bold` + Body: `Open Sans Regular`
- Headers: `Playfair Display` + Body: `Lato Regular`
- Headers: `Roboto Bold` + Body: `Roboto Regular`

---

### Component 2: Images

**Image Guidelines**:
- **Format**: JPEG or PNG
- **Size**: Keep under 500KB (fast loading)
- **Resolution**: 72 DPI (web) minimum
- **Optimization**: Use compression tools
- **Aspect Ratio**: Match your banner ratio

**Image Types**:
1. **Background Image**: Full banner size
   - Use low opacity (40-60%) for text readability
   - Blur background to reduce distraction

2. **Product Image**: Showcase item
   - High resolution
   - Good lighting
   - Center in banner
   - Use drop shadow for depth

3. **Decorative Image**: Add visual interest
   - Icons, illustrations, patterns
   - Keep smaller than main content
   - Position to guide eye toward CTA

---

### Component 3: Buttons

**Button Best Practices**:
- **Size**: At least 44x44px (mobile touch target)
- **Color**: High contrast with background
- **Text**: Clear action words ("Shop Now", "Learn More")
- **Hover State**: Visual feedback on mouse over
- **Position**: Near the end of banner

**Button Examples**:
```
Standard Button:
  Color: #FF6B35 (orange)
  Text: "Shop Now"
  Size: 150x50px
  Font: Bold, 18px

Outlined Button:
  Border: 2px white
  Background: Transparent
  Text: "Learn More"
  
Rounded Button:
  Border Radius: 25px
  Creates friendly, modern look
```

---

### Component 4: Shapes

**Available Shapes**:
- **Rectangle**: Backgrounds, containers
- **Circle**: Profile images, decorative
- **Triangle**: Directional indicators
- **Line**: Dividers, accents
- **Star**: Ratings, emphasis

**Shape Styling**:
```
Fill Color:     Solid or gradient
Stroke:         Border thickness & color
Shadow:         Drop shadow for depth
Opacity:        Transparent elements
Border Radius:  Rounded corners (0-50px)
```

---

## Publishing & Scheduling

### Scenario 1: Publishing a Banner Immediately

**Situation**: Your new banner is ready and you want it live NOW.

**Steps**:
1. Click **"Publish"** button (top right)
2. Review banner details:
   - Name: `"Summer Sale Banner"`
   - Status: Draft → Publishing...
   - Display Location: Your shop
3. Click **"Confirm Publish"**
4. Banner goes live immediately! 🎉

**What Happens**:
- ✅ Banner appears on your shop website
- ✅ Customers can see it
- ✅ Analytics tracking starts
- ✅ You can't edit anymore (use version control)

---

### Scenario 2: Scheduling Banner for Specific Dates

**Situation**: You have a "Black Friday Sale" banner that should go live Nov 24 at 10 AM.

**Steps**:
1. Click **"Publish"** button
2. Select: **"Schedule for Later"**
3. Set schedule:
   - Start Date: `November 24, 2024`
   - Start Time: `10:00 AM`
   - End Date: `November 28, 2024`
   - End Time: `11:59 PM`
   - Timezone: Your local timezone
4. Click **"Schedule"**

**What Happens**:
- ✅ Banner is saved but not live yet
- ✅ Countdown shows: "Publishes in 15 days"
- ✅ At scheduled time, banner auto-publishes
- ✅ At end time, banner auto-unpublishes

**Important**:
- Set end dates for seasonal campaigns
- Avoid old banners lingering on your site
- System will remind you: "Banner expires in 3 days"

---

### Scenario 3: A/B Testing Two Banners

**Situation**: You have 2 banner designs and want to test which performs better.

**Steps**:

**Step 1: Create Banner A**
- Design and publish first banner
- Call it: `"Banner A - Design 1"`
- Publish to shop

**Step 2: Create Banner B**
- Design alternative banner
- Call it: `"Banner B - Design 2"`
- Publish to shop

**Step 3: Enable A/B Testing**
1. Go to **Banners** list
2. Find both banners
3. Click **"A/B Test"** button
4. Select:
   - Banner A: `"Banner A - Design 1"`
   - Banner B: `"Banner B - Design 2"`
   - Test Duration: `7 days` or `2 weeks`
   - Split: `50/50` (equal traffic)
5. Click **"Start Test"**

**What Happens**:
- ✅ 50% of visitors see Banner A
- ✅ 50% of visitors see Banner B
- ✅ Analytics track separately
- ✅ After 7 days, winner declared

**Results** (Example):
```
Banner A - Design 1:
  Impressions: 2,500
  Clicks: 125
  CTR: 5.0%

Banner B - Design 2:
  Impressions: 2,400
  Clicks: 192
  CTR: 8.0%

WINNER: Banner B (8.0% CTR vs 5.0%)
```

**Action**:
1. Keep Banner B running
2. Archive Banner A
3. Analyze why B won:
   - Better colors?
   - Clearer CTA?
   - Different image?
   - Use insights for next banner!

---

## Version Control & Rollback

### Scenario 1: Saving Multiple Versions

**Situation**: You've published a banner but want to test a new design without losing the old one.

**Steps**:
1. Go to published banner
2. Click **"Create New Version"**
3. Edit the banner:
   - Change colors
   - Modify text
   - Update images
4. Click **"Save as Draft"**
5. New version created: `"v2 - Different Colors"`

**Version List**:
```
Current Version (Live): v1 - Original Design
├─ v2 - Different Colors (Draft)
├─ v3 - New CTA Text (Draft)
└─ v4 - Mobile Optimized (Draft)
```

**Publishing New Version**:
1. Click on `v2 - Different Colors`
2. Click **"Publish"**
3. `v1` automatically becomes "Previous"
4. `v2` is now "Current"

---

### Scenario 2: Rolling Back to Previous Version

**Situation**: You published a banner update, but the new design performs worse. You want to go back.

**Steps**:
1. Go to banner
2. Click **"Version History"**
3. See all previous versions with performance data:
   ```
   v1 - Original (5% CTR)
   v2 - Updated Design (3% CTR) ← Currently published
   v3 - Mobile Optimized (4.5% CTR)
   ```
4. Click on `v1 - Original`
5. Click **"Restore This Version"**
6. Confirm: "Are you sure? This will unpublish v2"
7. Click **"Yes, Restore"**

**What Happens**:
- ✅ `v1` is now published again
- ✅ `v2` becomes "Previous Version"
- ✅ Old analytics data preserved
- ✅ No data loss!

**Pro Tip**: Always keep version history. Your "old" banner might be your best performer!

---

## Performance Optimization

### Technique 1: Image Optimization

**Why it Matters**: Large images = slow loading = fewer clicks

**Steps**:
1. Use online compression tool (TinyPNG, ImageOptim)
2. Reduce image size:
   - Original: 2 MB → Compressed: 200 KB
3. Use right format:
   - Photos: JPEG
   - Graphics/icons: PNG or SVG
   - Animations: Avoid video if possible

**Result**: Banner loads 10x faster! ⚡

---

### Technique 2: Lazy Loading

**What it does**: Images load only when user scrolls to them.

**Enable in Settings**:
1. Go to **Banner Settings**
2. Enable: `"Lazy Load Images"`
3. This helps if banner is below fold

---

### Technique 3: Minifying Code

**Automatic**: BannerAI automatically minifies your banner code before publishing.

**What this means**:
- Your code is optimized for speed
- You don't need to do anything
- Banners load faster automatically

---

## Common Scenarios & Workflows

### Workflow 1: Monthly Seasonal Banner Update

**Timeline**: 30-minute process, once per month

**Step 1: Plan Next Month** (5 min)
- January: New Year, New You
- February: Valentine's Day
- March: Spring Collection
- (Plan all 12 months)

**Step 2: Design Banner** (15 min)
- Use template from last month
- Update colors/images/text
- Save as draft

**Step 3: Schedule Publication** (5 min)
- Set start date: 1st of month
- Set end date: Last day of month
- Banner auto-publishes and unpublishes

**Step 4: Monitor Performance** (5 min)
- Check analytics
- If CTR low, create A/B test

---

### Workflow 2: Flash Sale Campaign (24-hour)

**Situation**: You want to announce a surprise 24-hour sale.

**Steps**:
1. Create banner quickly:
   - Bold colors (red/orange)
   - Clear message: "24-HOUR FLASH SALE"
   - Large countdown timer
   - Big "SHOP NOW" button

2. Design time: 5 minutes (use existing template)

3. Schedule:
   - Start: Today 12:00 PM
   - End: Tomorrow 12:00 PM
   - Set 12-hour reminder to monitor

4. Monitor:
   - Check analytics every 2 hours
   - Watch click volume
   - Engage if questions come in support

5. Campaign ends:
   - Announcement: "Sale ended" optional thank you banner
   - Archive flash sale banner
   - Analyze: Did you beat targets?

---

### Workflow 3: Split Testing for Optimization

**Timeline**: 2 weeks per test, 3 tests per quarter

**Month 1 Test**: Colors
- Banner A: Blue & White
- Banner B: Orange & Black
- Winner: Orange (higher CTR)

**Month 2 Test**: Copy
- Banner A: "Shop Now"
- Banner B: "Get Yours Today"
- Winner: "Get Yours Today" (more personal)

**Month 3 Test**: Images
- Banner A: Product photo
- Banner B: Lifestyle photo
- Winner: Lifestyle (customers connect emotionally)

**Cumulative Impact**:
- Original Design: 5% CTR
- After 3 months optimization: 9% CTR (80% improvement! 🎉)

---

## Design Best Practices

### Practice 1: Color Psychology

**Colors & Their Meanings**:
- 🔴 **Red**: Urgency, sale, excitement
  - Use for: Flash sales, limited offers
- 🟠 **Orange**: Friendly, approachable, energetic
  - Use for: CTA buttons, friendly brands
- 🟡 **Yellow**: Happy, positive, attention
  - Use for: Highlights, secondary CTAs
- 🟢 **Green**: Growth, nature, healthy
  - Use for: Eco-friendly, health products
- 🔵 **Blue**: Trust, professional, calm
  - Use for: Corporate, financial, professional services
- 🟣 **Purple**: Luxury, creativity, premium
  - Use for: Premium products, creative services
- ⚫ **Black**: Sophisticated, premium
  - Use for: Luxury, tech, modern
- ⚪ **White**: Clean, minimal, space
  - Use for: High-end, minimal designs

---

### Practice 2: Hierarchy & Focus

**Visual Hierarchy** (what users see first to last):
1. **Primary focus**: Biggest text, brightest color
   - Example: "50% OFF"
2. **Secondary focus**: Medium text
   - Example: "This Weekend Only"
3. **CTA Button**: Clear action
   - Example: "Shop Now"
4. **Supporting info**: Smallest text
   - Example: "Offer expires 11:59 PM"

**Wrong Hierarchy**:
```
Small, dark text: "Shop Now"
Medium text: "This Weekend Only"
Giant, bright text: "Valid on selected items"
❌ User is confused about what to do
```

**Right Hierarchy**:
```
Giant, bright text: "50% OFF"
Medium text: "This Weekend Only"
Button: "Shop Now"
Small text: "Valid on selected items"
✅ User knows exactly what matters
```

---

### Practice 3: White Space (Breathing Room)

**What is White Space?**
Empty space between elements. It's NOT wasted space - it's VALUABLE space!

**Benefits**:
- Easier to read
- Less overwhelming
- More professional
- Better mobile experience

**Examples**:
```
CROWDED (Bad):
50% OFF This Weekend Shop Now Limited Stock Available

SPACIOUS (Good):
50% OFF

This Weekend Only

SHOP NOW
```

---

### Practice 4: Contrast for Readability

**Rule**: Text should be easily readable from a distance.

**Good Contrast**:
- Black text on white background (best)
- White text on dark blue background
- Dark text on light yellow background

**Bad Contrast** (Don't do this):
- Gray text on white background
- Light blue text on dark blue
- Black text on dark brown

**Check Your Contrast**:
1. Squint at your banner (pretend you're far away)
2. Can you read it easily?
3. If not, increase contrast!

---

### Practice 5: Mobile-First Design

**Why It Matters**: 75% of users visit on mobile

**Mobile-First Tips**:
- Design for mobile (360px width) first
- Then expand to desktop
- Test on actual phone
- Large tap targets (44px minimum)
- Readable fonts (16px minimum)
- Don't overcrowd

**Mobile Optimization**:
```
Desktop (1200px):
[Image] [Text] [Button]

Mobile (360px):
[Image]
[Text]
[Button]

Text and button stack vertically for readability
```

---

### Practice 6: The 5-Second Rule

**The Test**: Someone sees your banner for 5 seconds. Do they know:
1. What are you selling?
2. What should they do?
3. Why should they care?

**If answer is NO to any - redesign!**

---

## Troubleshooting

### Problem 1: Text Not Visible on Image

**Symptom**: White text on light image = can't read

**Solution**:
1. Add dark overlay:
   - Create rectangle same size as image
   - Fill with black
   - Set opacity to 40-60%
   - Send to back (so text is on top)
2. Or change text color to dark/black

---

### Problem 2: Banner Looks Good on Desktop but Bad on Mobile

**Symptom**: Crowded, unreadable on phone

**Solution**:
1. Enable responsive design (see Advanced Techniques)
2. Adjust for mobile:
   - Reduce font sizes 20-30%
   - Stack elements vertically
   - Reduce image sizes
   - Simplify (remove optional elements)
3. Test on actual phone

---

### Problem 3: Banner Loading Slowly

**Symptom**: Takes 5+ seconds to appear

**Solution**:
1. Compress images (see Performance section)
2. Remove unnecessary animations
3. Check file size of banner code
4. Contact support if still slow

---

### Problem 4: Button Not Clickable

**Symptom**: Click button but nothing happens

**Cause**: Possible reasons:
- Button is behind another element
- Link is not set
- Link is broken

**Solution**:
1. Click button to select it
2. Check right panel: Is **"Link"** field filled?
3. If empty: Enter link URL
4. If filled: Check if link is valid (click it manually)
5. If behind element: Use layers panel, move button forward

---

### Problem 5: Animation Not Working

**Symptom**: Animate button to fade in, but it doesn't work

**Solution**:
1. Check if animation is set correctly:
   - Select element
   - Check 🎬 Animation in properties
2. Check timing:
   - Is duration too short? (Set minimum 0.5s)
   - Is delay too long? (Test with 0s)
3. Preview again (sometimes animations need page refresh)

---

## Getting Help

### Support Resources
- **Help Center**: bannerai.com/help
- **Video Tutorials**: youtube.com/@bannerai
- **Live Chat**: Click icon in bottom-right
- **Email**: support@bannerai.com
- **Community**: discord.com/bannerai

### Common Questions
- **Q**: Can I use my own fonts?
  - **A**: Yes! Upload custom fonts in Settings

- **Q**: What's the maximum banner size?
  - **A**: 4000x4000px, but optimize for web

- **Q**: Can I add videos to banners?
  - **A**: Yes, but they slow load time. Optimize carefully.

- **Q**: How many banners can I create?
  - **A**: Unlimited! Your plan might limit published banners.

- **Q**: Can I export my banner code?
  - **A**: Yes! Go to Publish → Download Code

---

## Quick Reference: Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+Z | Undo |
| Ctrl+Y | Redo |
| Ctrl+S | Save |
| Delete | Delete selected element |
| Ctrl+D | Duplicate selected |
| Ctrl+A | Select all |
| Ctrl++ | Zoom in |
| Ctrl+- | Zoom out |
| Space+Drag | Pan canvas |
| Arrow Keys | Move selected element |

---

**Last Updated**: October 2024
**Version**: 1.0

For video tutorials and templates, visit: https://bannerai.com/learning-center
