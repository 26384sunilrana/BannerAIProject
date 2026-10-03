# Setting up e-mail, SMS, payments and Azure: a step-by-step guide

This is for you, the owner of the product, not for a programmer. It tells you **what each service is, what to sign up for, what to collect, and
what to hand over to me** so I can finish the connection. Prices, screens and rules of the providers change: where I say "check", look at the provider's
current page before you pay or promise anything.

Nothing here is needed to **try** BannerAI: the demo (`DEMO_START_HERE.md`) works without any of it. These services are needed to **sell** it.

## What is waiting on what

| You want | Needs | State in the application today |
|---|---|---|
| Real e-mails (reminders, forgot-password, verify e-mail) | An e-mail sending account (SMTP) | **Already built**: it only needs settings. The forgot-password and verify screens still have to be added (small). |
| Reminders and alerts by SMS | An SMS provider account | A stand-in writes to the log. I connect the real one once you have an account. |
| Owners paying and renewing by themselves | A payment provider account | A stand-in marks payments as done. I connect the real one, add the checkout and the "renew now" button. |
| Files, keys and database in the cloud | An Azure account | Runs on disk and SQL Server today. I switch it over after your review. |

**Suggested order:** 1 e-mail (an hour), 2 payments (starts with paperwork: start it first, it is the slowest), 3 SMS (paperwork in India too), 4 Azure.

---

## 1. E-mail (SMTP) — the quickest

**What it is:** a mailbox the application sends from, such as `noreply@yourcompany.com`.

**Choose one**
- *Your own domain's mail* (Google Workspace, Microsoft 365, Zoho): fine for a few hundred mails a day.
- *A sending service* (Brevo, SendGrid, Amazon SES, Mailgun): made for application mail, has a free or cheap start, and keeps your mail out of spam folders.
  Check each one's free allowance.

**Steps (with a sending service)**
1. Create an account and **verify your domain** (the service shows two or three DNS records: add them where you bought the domain; wait until it says verified).
2. Create an **SMTP login** or API key for SMTP ("SMTP & API" page). Note: **host**, **port** (587), **user**, **password**.
3. Choose the From address, for example `BannerAI <noreply@yourcompany.com>`.

**Steps (with a Gmail or Workspace mailbox, for a first test only)**
1. Turn on 2-step verification for the account, then create an **App password** (Google Account → Security → App passwords).
2. Host `smtp.gmail.com`, port 587, user = the address, password = the app password.

**Give me / set in the system**

| Setting | Example |
|---|---|
| `Email__Smtp__Host` | `smtp.yourprovider.com` |
| `Email__Smtp__Port` | `587` |
| `Email__Smtp__User` | the login |
| `Email__Smtp__Password` | the password (keep it secret: a Kubernetes secret or `.env`, never in the code) |
| `Email__Smtp__From` | `noreply@yourcompany.com` |
| `Email__Smtp__EnableSsl` | `true` |
| `AppUrl` | the address of the web app, for the links in the mails, e.g. `https://banners.yourcompany.com` |

With Docker Compose, add those lines to `.env` and to the `api:` section's `environment:`; in Kubernetes put the password in the secret and the rest in the configmap
(see `DEPLOYMENT.md`).

**Check it works:** I add a "send me a test e-mail" button for administrators when we do this step. Until then, subscription reminders show in the API log
as "sent" or "not sent".

**Then I build:** the *Forgot password* and *Verify e-mail* screens, using the mail that is already written.

---

## 2. Payments (owners pay and renew themselves)

**What it is:** a *payment gateway* that charges the owner's card, UPI or net banking, and tells BannerAI "paid" or "failed".
For India the usual choices are **Razorpay**, **Cashfree**, **PayU**, **PhonePe/Paytm business**; for other countries **Stripe**.
My suggestion for India: **Razorpay** (good test mode and documents, supports subscriptions and UPI). The steps are alike for the others.

**Steps (Razorpay as the example; check their current "Get started")**
1. Create a business account on the provider's website.
2. Use **test mode** first: the dashboard gives a **Key ID** and a **Key Secret** for testing. No paperwork needed for this. I can finish and test everything with test keys.
3. To take real money you complete **KYC / activation**: business type, PAN, bank account, address proof, a website with your policies. This takes days, so start it early.
   You will need pages on your website for: terms and conditions, **refund and cancellation policy**, privacy policy, contact details, and the price list (the plans).
4. Decide the rules, because I build what you decide:
   - Which periods can be paid: monthly, quarterly, half-yearly, yearly (all exist in BannerAI).
   - **Automatic renewal** or the owner presses "Renew"? Automatic card or UPI renewals need the provider's *subscriptions* or *e-mandate* feature, which has extra rules and
     approvals in India. Starting with "owner presses Renew and pays" is simpler and safe.
   - Refunds: who may refund, and when (the application will show the refund to the administrator).
   - Tax: do you charge GST? On the invoice you need your GSTIN. Tell me the rate and how to show it.
5. Create a **webhook**: the provider calls an address of yours to say "this payment succeeded". You give it `https://<your-api-address>/api/payments/webhook`
   (I build it) and it gives you a **webhook secret**.

**Give me**
- The provider you chose, **Key ID** and **Key Secret** (test first, live later), the **webhook secret**.
- The decisions in step 4.
- Your company name, address, GSTIN and logo for the invoice.

**Then I build:** the real payment connection, a checkout on the *Subscription* screen, the "Renew now" button, payment-failed handling (the grace week already exists),
receipts on invoices, refunds for the administrator, and tests against the provider's test mode. Keys go in secrets, never in code.

**Money safety:** the application never stores card numbers; the provider's own page or form takes them.

---

## 3. SMS

**What it is:** text messages for renewal reminders and important alerts.

**India:** by law, bulk and service SMS must be sent through a provider that is registered, using a **DLT registration**: your company registers with an operator, then
registers a **sender ID** (six letters, e.g. `BNRAIP`) and each **message template**. Without this, messages are blocked. The provider walks you through it
(MSG91, Fast2SMS, Twilio, Kaleyra and others do).

**Steps**
1. Choose a provider (check price per SMS and DLT help). MSG91 and Twilio both have good documents.
2. Create the account; complete the business check.
3. Do the **DLT**: register the entity, the **sender ID**, and templates. Suggested texts to register (change the words to yours):
   - `Your BannerAI plan for {#var#} ends on {#var#}. Renew to keep your banners running.`
   - `Your BannerAI plan has ended. Sign in to renew.`
   - `Your payment of {#var#} was received. Thank you.`
4. Get the **API key / auth key** and the **template IDs**.

**Give me:** provider, API key, sender ID, and the template IDs with the exact texts.

**Then I build:** the real sender behind the existing SMS stand-in, a test button, and a record of delivery. Optionally one-time codes (OTP) for sign-in.

---

## 4. Azure (files, secrets, database)

**What it is:** Microsoft's cloud. For BannerAI it gives: **Blob Storage** for uploaded pictures and videos, **Key Vault** for the encryption keys and secrets,
**Azure SQL** for the database, and **AKS** (or a simpler App Service / Container Apps) to run the containers.

**Steps**
1. Create an Azure account and a **subscription** (a card is needed; there is a free credit for new accounts: check).
2. Install the Azure CLI (`az`) on your computer and run `az login`.
3. Create a **resource group** (a folder for everything), for example `bannerai-prod`, in a region near your users (Central India or South India).
4. Create the services (names must be unique; replace `yourco`):
   ```
   az group create -n bannerai-prod -l centralindia
   az storage account create -g bannerai-prod -n yourcobannerstore --sku Standard_LRS --kind StorageV2 --allow-blob-public-access false
   az keyvault create -g bannerai-prod -n yourco-bannerai-kv
   az sql server create -g bannerai-prod -n yourco-bannerai-sql -u bannersqladmin -p "<a long password>"
   az sql db create -g bannerai-prod -s yourco-bannerai-sql -n BannerServiceDb --service-objective S1
   ```
5. Allow the app to reach them with a **managed identity** (no passwords): I will give you the exact commands once we choose AKS or Container Apps.
6. Turn on backups and a **budget alert** (Cost Management) so a surprise bill cannot happen.

**Give me:** the names of the resources you created, and add me as a collaborator only if you want me to run commands (you stay in control: I will tell you what each
command does first).

**Then I build:** a Blob storage provider behind the file store, Key Vault for the keys, the migration of the existing files, and a deployment guide for your chosen host.

---

## Checklist: what to bring back to me

- [ ] SMTP host, port, user, password, From address, and the web address of the app.
- [ ] Payment provider name, test Key ID and Secret, webhook secret; the rules (periods, auto-renew or not, refunds, GST, company details).
- [ ] SMS provider name, API key, sender ID, template IDs and texts.
- [ ] Azure resource names (or a decision to start on a simple host first).
- [ ] The three policy pages on your website: terms, refund policy, privacy policy (the payment provider asks for them).

Send them one at a time, in the order above, and I will finish each connection and test it before we go on. **Never paste a live secret into a chat or a
file in the code folder**: give test keys while building, and put live keys straight into the production secret store yourself (I will show where).
