# Setting up e-mail, SMS, payments and Azure: a step-by-step guide

This is for you, the owner of the product, not for a programmer. It tells you **what each service is, what to sign up for, what to collect, and
what to hand over to me** so I can finish the connection. Prices, screens and rules of the providers change: where I say "check", look at the provider's
current page before you pay or promise anything.

Nothing here is needed to **try** BannerAI: the demo ([start-here guide](../guides/start-here.md)) works without any of it. These services are needed to **sell** it.

## What is waiting on what

| You want | Needs | State in the application today |
|---|---|---|
| Real e-mails (reminders, forgot-password, verify e-mail) | An e-mail sending account (SMTP) | **Built.** Fill in the `Email__Smtp__*` values and it works. The forgot-password, reset and verify-e-mail screens exist. |
| Reminders and alerts by SMS | An SMS provider account | **Built for MSG91** (India, registered templates). Fill in the `Sms__*` values. Until then nothing is sent. |
| Owners paying and renewing by themselves | A payment provider account | **Built for Razorpay** (checkout, confirmation, webhook, refunds). Fill in the `Payments__*` values. Until then the Renew button extends the plan without charging. |
| Files, keys and database in the cloud | An Azure account | Runs on disk and SQL Server today. Switched over after your review. |

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
(see [deployment guide](deployment.md)).

**Check it works:** sign in as an administrator, open **Providers**, and press **Send test e-mail**. The page also says whether e-mail is "Connected" and which setting is still empty (never the value).

**Already built:** the *Forgot password* link on the sign-in page, the *reset password* page, and the *verify e-mail* page and reminder. They start working the moment the SMTP values are set.

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

**Set these values** (on the server, in the secret store or `.env`; never in the code folder)

| Setting | What it is |
|---|---|
| `Payments__Provider` | `Razorpay` (the only provider built so far; `None` or empty keeps today's Renew button) |
| `Payments__Razorpay__KeyId` | the Key ID (test first, live later) |
| `Payments__Razorpay__KeySecret` | the Key Secret |
| `Payments__Razorpay__WebhookSecret` | the secret you typed when you made the webhook |
| `Payments__DefaultCurrency` | `INR` (change for another country) |
| `Payments__DefaultCountry` | `IN` |

In the provider's dashboard make a webhook to `https://<your-api-address>/api/payments/webhook` for the events `payment.captured`, `order.paid` and `payment.failed`.
If a provider is named but a value is empty, the application refuses to start in production and says which setting is missing (never its value).

**Already built:** the payment form on the *Subscription* screen ("Pay and renew"), confirmation of the payment, the webhook (repeats are harmless; a payment for the wrong amount is never applied
and the administrators are told), refunds for the administrator (all or part), and payment-failed handling (the grace week already exists). While online payment is on, the automatic-renewal switch is
hidden and ignored: owners pay each period and are reminded by e-mail and text.

**Still to decide before real money** (bolt 044): who pays for a brand-new subscription (today the first period starts without payment), GST and tax invoices, and the first test against your Razorpay test keys.
Other countries or providers: a new provider is one adapter on the server and one small driver in the web app; currency and country are already settings.

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

**Set these values**

| Setting | What it is |
|---|---|
| `Sms__Provider` | `Msg91` (`None` or empty keeps SMS off) |
| `Sms__Msg91__AuthKey` | the MSG91 auth key |
| `Sms__DefaultCountryCode` | `+91` (numbers typed without a country code get this one) |
| `Sms__Templates__renewal-reminder__ProviderTemplateId` | the DLT / MSG91 template id for the reminder text |
| `Sms__Templates__plan-ended__ProviderTemplateId` | the template id for the "plan has ended" text |
| `Sms__Templates__payment-received__ProviderTemplateId` | the template id for the payment text |

The texts are already in the settings file (`Sms:Templates:<name>:Text`); register exactly those words (or change them in both places). If MSG91 names the gaps in a template something other than
`var1`, `var2`, set `Sms__Templates__<name>__VariableNames__0`, `__1` and so on. Only a date, a plan name or an amount ever goes into a text.

**Check it works:** administrator, **Providers**, **Send test text**.

**Already built:** the sender behind the reminder texts, number clean-up (any way of typing an Indian or international number), and a log line (number masked) for every text. Free text is never sent.

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

## Checklist: what to fill in

- [ ] E-mail: `Email__Smtp__Host`, `Port`, `User`, `Password`, `From`, and `AppUrl` (the web address of the app, https in production).
- [ ] Payments: `Payments__Provider=Razorpay`, `Payments__Razorpay__KeyId`, `KeySecret`, `WebhookSecret`; the rules (periods, refunds, GST, company details).
- [ ] SMS: `Sms__Provider=Msg91`, `Sms__Msg91__AuthKey`, the three template ids.
- [ ] Azure resource names (or a decision to start on a simple host first).
- [ ] The three policy pages on your website: terms, refund policy, privacy policy (the payment provider asks for them).

Check each one on the **Providers** screen as you go. Then run the first real test of each with test keys. **Never paste a live secret into a chat or a
file in the code folder**: give test keys while building, and put live keys straight into the production secret store yourself (I will show where).
