# Shop owner walkthrough (about 30 minutes)

You are **Olivia**, you run **Sunrise Cafe**. You will look at what is running, approve what Sam made, change a banner, book an ad and learn where everything is.

Sign in at <http://localhost:3000> with `demo-owner@bannerai.demo` / `Demo-Owner-2026!`.
(New to the demo? Start with [Start here](../start-here.md).)

![The sign-in page](images/owner-00-sign-in.png)

---

## Step 1. The Home page: your map
After signing in you land on **Home**. Every card is a place you can go; the same places are in the menu at the top: *Banners, Calendar, Ads, My files, Approvals, Team,
Subscription, My shop, Default board, Takeover, Screens, Shop screen*. A line under the welcome says which banner is **Now showing** and until when.

![Home](images/owner-01-home.png)

**The bell** next to your name shows how many messages you have not read. Sam has already sent you things to look at (a banner and an ad).

## Step 2. See what your customers see: the Shop screen
Click **Shop screen** in the menu (or open <http://localhost:3000/display>). This is what plays on the TV in the shop.
The banner **Welcome Offer** is in the middle, and two **ads** sit at the sides: *Fresh bread at 7 am* (yours) and *Open an account today* (booked by the administrator).
Move the mouse to see the **Full screen** button. Press the browser's back button to return.

![The shop screen](images/owner-17-shop-screen.png)

## Step 3. Your banners
Click **Banners**. You see **Welcome Offer** (approved, live) and **Weekend Special** (made by Sam, *Waiting for approval*). Under each banner is when it runs.

![Banners](images/owner-02-banners.png)

## Step 4. Change a banner in the editor
Click **Open editor** next to *Welcome Offer*.
1. Click the big white text. The **Properties** panel opens on the right.
2. Change its words: find **Content** in the panel (scroll down) and type a new greeting.
3. Drag the text to move it (it snaps to a grid); drag a corner to resize it. A click without moving does not move it.
4. Add something: click **Text**, **Image**, **Video** or **Shape** on the left.
5. Press **Save** (or Ctrl+S). **Preview** plays it as the screen will; the arrows undo and redo.

![The editor](images/owner-03-editor.png)

> A banner that is already approved goes back for approval if you change its schedule, so changing only the picture or words is the safe way to practise.
> Click **← Banners** to leave.

## Step 5. Set when a banner runs
On the banners page click **Change schedule** under *Weekend Special*.
- Choose a start and an end. **Times are your shop's clock**, not your computer's.
- Tick **Only at certain hours each day** to run for example 12:00 to 15:00, and choose the days of the week.
- If another banner already runs at that time BannerAI says which one: two banners cannot play at the same moment.

![Schedule](images/owner-04-schedule.png)

## Step 6. The calendar
Click **Calendar**. A week as blocks: Monday to Sunday, hours down the side. Faded blocks are not approved yet. Use *Next week* to look ahead.

![Calendar](images/owner-05-calendar.png)

## Step 7. Approve Sam's banner
Click **Approvals**. *Weekend Special* is waiting for you.
1. Read what it is and when it will run.
2. Click **Approve** (or **Reject** and write why).
3. Click **Publish**. It will start when its schedule says.

![Approvals](images/owner-06-approvals.png)

## Step 8. Ads on your screen
Click **Ads**. You see the three ads. *Cold juice, 49* was booked by Sam and waits for you: click **Approve**.
*Open an account today* was booked by the administrator: you cannot change it, but you may **Override this ad** if a local advertiser pays you more for that time (it stops
at once, you are still paid for the hours it ran, and the administrator is told). **History** shows who did what and when for any ad.

![Ads](images/owner-07-ads.png)

## Step 9. Book an ad yourself
Click **Book an ad**.
1. Type the advertiser's **business name** and a **headline** (and a short text).
2. Choose the **kind**: *Side strip* (a band along one edge), *Mega ad* (most of the screen), *Popup* (appears for a few seconds now and then), *Small corner tile*.
3. Choose the edge or corner, and for a strip how much of the screen it takes. The **preview** on the right shows it.
4. Choose when it runs, then **Save as draft**, then **Send in**. Your own ads are booked straight away.

![Booking an ad](images/owner-08-book-an-ad.png)

Try typing a phone number in the headline: BannerAI **refuses** it. Ads must not carry personal details. Words about health ("patients") send the ad to the administrator to look at first.

## Step 10. Money from the administrator's ads
Click **Ads → Monthly statement**. For the month it lists the hours each ad ran on your screen and what **you are paid** for the administrator's ads. Your own ads show
hours only: you set your own prices and BannerAI does not track what advertisers pay you.

![Statement](images/owner-09-statement.png)

## Step 11. Your team
Click **Team**. You can add up to **two sales executives** and choose **who approves new and changed banners** (you, and/or an executive). Use **Hand the shop over** only when you really leave.

![Team](images/owner-10-team.png)

## Step 12. Your files
Click **My files**. Upload pictures and videos here once, use them in banners and ads. The bar shows how much of your plan's storage you used; a file in use cannot be deleted.

![My files](images/owner-11-files.png)

## Step 13. The default board
Click **Default board**. This is what the screen shows when no banner is live. Change the message, colours and add your logo (from My files); the preview shows it.

![Default board](images/owner-12-default-board.png)

## Step 14. Your plan
Click **Subscription**. **Your plan** shows the plan, its status, the billing period and price, and the **next renewal** date. **Renew early** renews for another period at once, and
**Renew automatically** lets it renew by itself. Under **Change plan or billing period** pick a period (Monthly, Quarterly, Half yearly, Yearly) and a plan, then press the button under the
plans. Every period costs the same per month, and a plan change takes effect from the next day.

![Subscription](images/owner-13-subscription.png)

## Step 15. Your shop's details
Click **My shop → Edit**: address, **city and group**, and the **time zone** (leave it on automatic to follow your city).

![My shop](images/owner-14-my-shop.png)

## Step 16. Messages
Click the **bell**: banners and ads waiting for you, approvals, takeover requests. Click a message to go straight to the page.

![Messages](images/owner-16-messages.png)

## Step 17. Add the shop television: Screens
Click **Screens**. This is the television or tablet in your shop. Nobody signs in on it.
1. On the television, open the web browser and go to the address shown on the page (`/player` on the BannerAI web address).
2. The television shows a code of six characters.
3. Type the code under **Code on the screen**, give the screen a name such as *Shop window* (optional) and press **Add the screen**.

The television then starts by itself within a few seconds. The page shows its name, **Online** or **Offline** and when it was last heard from; **What was shown** adds up the hours each banner, ad
and the default board were on the screen (it fills in as the screen reports, once a minute). **Rename** and **Remove this screen** are yours; a removed screen shows a new code and can be added again.

![Screens](images/owner-18-screens.png)

## Step 18. Your account and two-step sign-in
Click your name at the top right. **My account** has your details, **Change password**, **Two-step sign-in** and **Signed-in devices**.
- **Two-step sign-in**: press **Set up two-step sign-in**, type your password, scan the picture with an authenticator app on your phone (or type the secret) and type the six-digit code the app shows.
  BannerAI then shows **ten recovery codes, once**: save them somewhere safe, each works once if you lose your phone. From then on, signing in asks for a code after the password.
- **Sign out everywhere** signs every device out, this one included: use it after losing a phone or using a shared computer.

![Account](images/owner-19-account.png)

## Step 19. If a shop changes hands
Click **Takeover**. If you bought or took over a shop, you ask here with the shop's name and address; the old owner agrees with their password and chooses whether the
associates stay. Nothing changes until they agree.

![Takeover](images/owner-15-takeover.png)

---

## You are done: what to try next
- Open the demo in a second window as **Sam** and book an ad as him: watch the bell on your side.
- Open the **administrator** window and book an ad on Sunrise Cafe: it appears on your Ads page and on the shop screen.
- Read the full [Shop Owner Guide](../shop-owner.md) for every detail.
