# BannerAI demo: start here

In about **10 minutes** you will have BannerAI running on your computer with a small cafe already set up, three people to sign in as, and a
guide for each of them. You do not need to know programming. If you can open a web page and copy a line into a window, you can do this.

**What you get**

| Person | Signs in as | Password | Guide |
|---|---|---|---|
| **Administrator** (you, the product owner) | `demo-admin@bannerai.demo` | `Demo-Admin-2026!` | [Admin walkthrough](walkthroughs/admin.md) |
| **Shop owner** (Olivia, runs *Sunrise Cafe*) | `demo-owner@bannerai.demo` | `Demo-Owner-2026!` | [Owner walkthrough](walkthroughs/owner.md) |
| **Sales executive** (Sam, works for Olivia) | `demo-seller@bannerai.demo` | `Demo-Seller-2026!` | [Executive walkthrough](walkthroughs/executive.md) |

Each guide is a list of small steps, "click this, you will see that", with a picture of the screen. Start with the person you are.
To understand the whole product, read the **owner** guide first, then the other two.

---

## Before you start (once)

1. **Docker Desktop** installed and running (the whale icon is in the taskbar). Download it from docker.com if you do not have it.
2. **Node.js** version 18 or newer (to fill in the demo): download from nodejs.org if `node --version` shows nothing.
3. This folder on your computer (you have it).

## Start it

Open a terminal (on Windows: *PowerShell* or *Git Bash*) **in this folder** and copy the lines one by one.

**1. Make the settings file.** Copy `.env.example` to `.env`, then open `.env` in a text editor and change the two secrets to anything you like
(the password at least 12 characters with letters, numbers and a symbol; the secret key at least 32 characters):
```
copy .env.example .env          (PowerShell)        or        cp .env.example .env     (Git Bash)
```

**2. Start everything** (the first time it downloads and builds: 5 to 10 minutes; later it takes a minute):
```
docker compose up --build -d
```
Wait about a minute after it says *Started*.

**3. Create the administrator login:**
```
docker compose run --rm api --create-admin demo-admin@bannerai.demo "Demo-Admin-2026!"
```
You should see `Administrator demo-admin@bannerai.demo created.`

**4. Fill in the demo cafe:**
```
node demo/seed-demo.mjs
```
It prints a line for each thing it makes and ends with the three logins.

**5. Open the web app:** <http://localhost:3000> and sign in as any of the three people.

> **Tip:** use **three browser windows** (one normal, one private/incognito, one in another browser) so you can be the owner and the executive at the same time and
> watch a message arrive in the bell.

## Start over

To throw everything away and begin again:
```
docker compose down -v
```
then repeat steps 2 to 4.

## Stop it

```
docker compose down
```
(your data stays; `docker compose up -d` starts it again).

## What the demo contains

- **Sunrise Cafe**, in Mumbai, with a *Silver* plan, a default board (message on a dark orange screen) and two logins: Olivia (owner) and Sam (executive).
- **Welcome Offer**: a banner that is live this week. **Weekend Special**: made by Sam, **waiting for Olivia's approval**.
- Three ads: *Fresh bread at 7 am* (booked by Olivia, live), *Open an account today* (booked by you, the administrator, live, with a price) and *Cold juice, 49*
  (booked by Sam, **waiting for Olivia**).
- A price list (100 an hour, 70% to the shop) so administrator ads can be priced.

Open **<http://localhost:3000/display>** signed in as the owner to see the shop screen as a customer would: the banner in the middle and the two ads at the sides.

## If something does not work

| You see | Do |
|---|---|
| `docker` is not recognised, or "cannot connect to the Docker daemon" | Start Docker Desktop and wait until it says *running*. |
| The page does not open | Wait another minute and refresh; check `docker compose ps` shows three services *Up*. |
| Step 4 says "The administrator could not sign in" | Run step 3 again. |
| Step 4 says "The demo shop already exists" | The demo is already there: just sign in. To redo it, use *Start over*. |
| A port is busy (3000, 5000, 1433) | Close the program using it, or stop other Docker projects. |
| You forgot a password | Administrator: run step 3 again with a new password. Others: sign in as the administrator, *Users*, and ask in the guide. |

## What the demo does not do yet

No real e-mails, text messages or payments: those need accounts of yours. The guide **[Providers setup guide](../operations/providers-setup.md)** explains,
step by step, what to sign up for and what to give me so I can connect them. Everything else in the guides works for real.
